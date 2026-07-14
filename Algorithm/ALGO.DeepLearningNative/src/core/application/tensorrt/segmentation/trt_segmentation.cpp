#include "pch.h"
#include "trt_segmentation.h"

#include "utils/ilogger.hpp"

#include "monopoly_allocator.hpp"
#include "tensorrt/common/cuda_tools.hpp"
#include "tensorrt/common/trt_infer_controller.hpp"
#include "tensorrt/common/preprocess_kernel.cuh"


namespace Segmentation
{

    using TRTControllerImpl = TRTInferController
        <
        std::pair<cv::Mat, cv::Mat>,      // input
        BoxArray,                         // output
        std::tuple<std::string, int>,     // start param
        AffineMatrix                      // additional
        >;
    class TRTInferImpl : public TRTInfer, public TRTControllerImpl
    {
    public:

        /** 要求在TRTInferImpl里面执行stop，而不是在基类执行stop **/
        virtual ~TRTInferImpl()
        {
            stop();
        }

        virtual bool startup(const std::string& file, std::shared_ptr<Param> param, bool use_multi_preprocess_stream)
        {
            image_channel_ = param->imageChannel;
            depth_channel_ = param->depthChannel;

            for (int i = 0; i < param->imageChannel; ++i)
            {
                mean_[i] = param->normalizeMean[i];
                std_[i] = param->normalizeStd[i];
            }

            normalize_ = CUDAKernel::Norm::mean_std(mean_, std_, 1.0f, CUDAKernel::ChannelType::None);

            enable_gray_range_ = param->enableClipGray;
            gray_range_min_ = param->clipGrayRangeMin;
            gray_range_max_ = param->clipGrayRangeMax;

            fill_value_ = param->fillValue;
            categories_ = param->categories;
            use_multi_preprocess_stream_ = use_multi_preprocess_stream;

            return TRTControllerImpl::startup(make_tuple(file, param->deviceId));
        }

        virtual void worker(std::promise<bool>& result) override
        {

            std::string file = std::get<0>(start_param_);
            int gpuid = std::get<1>(start_param_);

            TRT::set_device(gpuid);
            auto engine = TRT::load_infer(file);
            if (engine == nullptr)
            {
                INFOE("Engine %s load failed", file.c_str());
                result.set_value(false);
                return;
            }

            engine->print();

            //TRT::Tensor affine_matrix_device_i2d(TRT::DataType::Float);
            TRT::Tensor affine_matrix_device_d2i(TRT::DataType::Float);
            TRT::Tensor output_maskarray_device(TRT::DataType::Int64);
            int max_batch_size = engine->get_max_batch_size();
            auto input = engine->tensor("inputs");
            auto output = engine->tensor("outputs");

            input_width_ = input->size(3);
            input_height_ = input->size(2);
            input_channel_ = input->size(1);
            tensor_allocator_ = std::make_shared<MonopolyAllocator<TRT::Tensor>>(max_batch_size * 2);
            stream_ = engine->get_stream();
            gpu_ = gpuid;
            result.set_value(true);

            input->resize_single_dim(0, max_batch_size).to_gpu();

            //affine_matrix_device_i2d.set_stream(stream_);
            affine_matrix_device_d2i.set_stream(stream_);

            //affine_matrix_device_i2d.resize(max_batch_size, 8).to_gpu();
            affine_matrix_device_d2i.resize(max_batch_size, 8).to_gpu();

            int mask_w = output->size(2);
            int mask_h = output->size(1);

            std::vector<Job> fetch_jobs;
            while (get_jobs_and_wait(fetch_jobs, max_batch_size))
            {
                int infer_batch_size = fetch_jobs.size();
                input->resize_single_dim(0, infer_batch_size);

                for (int ibatch = 0; ibatch < infer_batch_size; ++ibatch)
                {
                    auto& job = fetch_jobs[ibatch];
                    auto& mono = job.mono_tensor->data();

                    if (mono->get_stream() != stream_)
                    {
                        checkCudaRuntime(cudaStreamSynchronize(mono->get_stream()));
                    }

                    uint8_t* workspace = (uint8_t*)mono->get_workspace()->gpu();
                    //affine_matrix_device_i2d.copy_from_gpu(affine_matrix_device_i2d.offset(ibatch), (float*)(workspace), 6);
                    //affine_matrix_device_d2i.copy_from_gpu(affine_matrix_device_d2i.offset(ibatch), (float*)(workspace + 32), 6);
                    affine_matrix_device_d2i.copy_from_gpu(affine_matrix_device_d2i.offset(ibatch), (float*)(workspace), 6);

                    input->copy_from_gpu(input->offset(ibatch), mono->gpu(), mono->count());
                    job.mono_tensor->release();
                }


                engine->forward(false);


                for (int ibatch = 0; ibatch < infer_batch_size; ++ibatch)
                {
                    int* pMaskArray = output->cpu<int>(ibatch);

                    auto& job = fetch_jobs[ibatch];
                    auto& based_prob = job.output;

                    affine_matrix_device_d2i.to_cpu();
                    float* affine_matrix = affine_matrix_device_d2i.cpu<float>(ibatch);

                    cv::Mat pMaskMat = cv::Mat(mask_h, mask_w, CV_32SC1);
                    memcpy(pMaskMat.data, pMaskArray, 1 * mask_w * mask_h * sizeof(int));

                    for (int labelId = 1; labelId < categories_.size(); labelId++)
                    {
                        cv::Mat maskMat;
                        cv::compare(pMaskMat, labelId, maskMat, cv::CMP_EQ);
                        maskMat.convertTo(maskMat, CV_32SC1);

                        Result r;
                        r.class_id = labelId;
                        r.class_name = categories_[labelId].c_str();
                        r.segmentation = Seg((int*)maskMat.data, mask_w, mask_h, affine_matrix, 255);

                        based_prob.emplace_back(r);
                    }

                    job.pro->set_value(based_prob);

                }
                fetch_jobs.clear();

            }
            stream_ = nullptr;
            tensor_allocator_.reset();
            INFO("Engine destroy.");
        }

        

        virtual bool preprocess(Job& job, const std::pair<cv::Mat, cv::Mat>& imagePair) override
        {
            cv::Mat image = imagePair.first;
            cv::Mat depth = imagePair.second;

            if (tensor_allocator_ == nullptr)
            {
                INFOE("Tensor_allocator_ is nullptr.");
                return false;
            }

            if (image_channel_ + depth_channel_ != input_channel_)
            {
                INFOE("Please check the model configuration file. The image and depth map channels are not equal to the model input channels.");
                return false;
            }

            if (image_channel_ != 0 && depth_channel_ != 0)
            {
                if (image.empty() || depth.empty())
                {
                    INFOE("Image and depth is empty.");
                    return false;
                }
            }
            else if (image_channel_ == 0 && depth_channel_ != 0)
            {
                if (depth.empty())
                {
                    INFOE("Depth is empty.");
                    return false;
                }
            }
            else if (image_channel_ != 0 && depth_channel_ == 0)
            {
                if (image.empty())
                {
                    INFOE("Image is empty.");
                    return false;
                }
            }
            else
            {
                INFOE("Please check the model configuration file, image_channel and depth_channel cannot both be 0.");
                return false;
            }

            job.mono_tensor = tensor_allocator_->query();
            if (job.mono_tensor == nullptr)
            {
                INFOE("Tensor allocator query failed.");
                return false;
            }

            CUDATools::AutoDevice auto_device(gpu_);
            auto& tensor = job.mono_tensor->data();
            TRT::CUStream preprocess_stream = nullptr;

            if (tensor == nullptr)
            {
                // not init
                tensor = std::make_shared<TRT::Tensor>();
                tensor->set_workspace(std::make_shared<TRT::MixMemory>());

                if (use_multi_preprocess_stream_)
                {
                    checkCudaRuntime(cudaStreamCreate(&preprocess_stream));

                    // owner = true, stream needs to be free during deconstruction
                    tensor->set_stream(preprocess_stream, true);
                }
                else
                {
                    preprocess_stream = stream_;

                    // owner = false, tensor ignored the stream
                    tensor->set_stream(preprocess_stream, false);
                }
            }

            cv::Size input_size(input_width_, input_height_);

            if (image_channel_ != 0)
            {
                job.additional.compute(image.size(), input_size);
            }
            else
            {
                job.additional.compute(depth.size(), input_size);
            }

            preprocess_stream = tensor->get_stream();

            size_t size_matrix = iLogger::upbound(sizeof(job.additional.d2i), 32);
            auto workspace = tensor->get_workspace();

            // 图片与深度图混合输入模式
            if (image_channel_ != 0 && depth_channel_ != 0)
            {
                tensor->resize(1, image_channel_ + depth_channel_, input_height_, input_width_);

                size_t size_image = image.cols * image.rows * image.channels() * sizeof(float);
                size_t size_depth = depth.cols * depth.rows * depth.channels() * sizeof(float);

                uint8_t* gpu_workspace;
                if (depth_channel_ == 1)
                    gpu_workspace = (uint8_t*)workspace->gpu(size_matrix + size_image + size_depth);
                else
                    gpu_workspace = (uint8_t*)workspace->gpu(size_matrix + size_image + size_depth + size_depth);
                float* affine_matrix_device = (float*)gpu_workspace;
                float* input_image_device = (float*)(gpu_workspace + size_matrix);
                float* input_depth_device = (float*)(gpu_workspace + size_matrix + size_image);

                uint8_t* cpu_workspace;
                if (depth_channel_ == 1)
                    cpu_workspace = (uint8_t*)workspace->cpu(size_matrix + size_image + size_depth);
                else
                    cpu_workspace = (uint8_t*)workspace->cpu(size_matrix + size_image + size_depth + size_depth);
                float* affine_matrix_host = (float*)cpu_workspace;
                float* input_image_host = (float*)(cpu_workspace + size_matrix);
                float* input_depth_host = (float*)(cpu_workspace + size_matrix + size_image);

                memcpy(input_image_host, image.data, size_image);
                memcpy(input_depth_host, depth.data, size_depth);
                memcpy(affine_matrix_host, job.additional.d2i, sizeof(job.additional.d2i));

                checkCudaRuntime(cudaMemcpyAsync(input_image_device, input_image_host, size_image, cudaMemcpyHostToDevice, preprocess_stream));
                checkCudaRuntime(cudaMemcpyAsync(input_depth_device, input_depth_host, size_depth, cudaMemcpyHostToDevice, preprocess_stream));
                checkCudaRuntime(cudaMemcpyAsync(affine_matrix_device, affine_matrix_host, sizeof(job.additional.d2i), cudaMemcpyHostToDevice, preprocess_stream));

                float min_val, max_val;
                // 深度图不包含有效值mask
                if (depth_channel_ == 1)
                {
                    if (enable_gray_range_)
                    {
                        CUDAKernel::clip_gray_value(input_depth_device, depth.cols, depth.rows, gray_range_min_, gray_range_max_, min_val, max_val, preprocess_stream);
                        checkCudaRuntime(cudaStreamSynchronize(preprocess_stream));
                    }
                    CUDAKernel::warp_affine_bilinear_and_normalize_image_mix_depth(input_image_device, image.cols * image.channels(), input_depth_device, depth.cols, depth.rows,
                        tensor->gpu<float>(), input_width_, input_height_, affine_matrix_device, fill_value_, normalize_, preprocess_stream);

                }
                // 深度图包含有效值mask
                else
                {
                    float* input_mask_device = (float*)(gpu_workspace + size_matrix + size_image + size_depth);
                    float* input_mask_host = (float*)(cpu_workspace + size_matrix + size_image + size_depth);
                    if (enable_gray_range_)
                    {
                        CUDAKernel::clip_gray_value_with_mask(input_depth_device, input_mask_device, image.cols, image.rows, gray_range_min_, gray_range_max_, min_val, max_val, preprocess_stream);
                        checkCudaRuntime(cudaStreamSynchronize(preprocess_stream));
                    }
                    else
                    {
                        for (size_t i = 0; i < size_depth / sizeof(float); ++i)
                            input_mask_host[i] = 1.0f;
                        checkCudaRuntime(cudaMemcpyAsync(input_mask_device, input_mask_host, size_depth, cudaMemcpyHostToDevice, preprocess_stream));
                    }
                    CUDAKernel::warp_affine_bilinear_and_normalize_image_mix_depth_mix_mask(input_image_device, image.cols * image.channels(), input_depth_device, input_mask_device, image.cols, image.rows,
                        tensor->gpu<float>(), input_width_, input_height_, affine_matrix_device, fill_value_, normalize_, preprocess_stream);

                }

                return true;

            }
            // 仅普通图片或仅深度图输入模式
            else
            {
                cv::Mat input_data;
                if (image_channel_ == 0 && depth_channel_ != 0)
                {
                    input_data = depth;
                    tensor->resize(1, depth_channel_, input_height_, input_width_);
                }
                else
                {
                    input_data = image;
                    tensor->resize(1, image_channel_, input_height_, input_width_);
                }
                size_t size_input_data = input_data.cols * input_data.rows * input_data.channels() * sizeof(float);

                uint8_t* gpu_workspace;
                if (depth_channel_ == 2)
                    gpu_workspace = (uint8_t*)workspace->gpu(size_matrix + size_input_data + size_input_data);
                else
                    gpu_workspace = (uint8_t*)workspace->gpu(size_matrix + size_input_data);
                float* affine_matrix_device = (float*)gpu_workspace;
                float* input_data_device = (float*)(gpu_workspace + size_matrix);

                uint8_t* cpu_workspace;
                if (depth_channel_ == 2)
                    cpu_workspace = (uint8_t*)workspace->cpu(size_matrix + size_input_data + size_input_data);
                else
                    cpu_workspace = (uint8_t*)workspace->cpu(size_matrix + size_input_data);
                float* affine_matrix_host = (float*)cpu_workspace;
                float* input_data_host = (float*)(cpu_workspace + size_matrix);

                // 仅深度图
                if (image_channel_ == 0 && depth_channel_ != 0)
                {
                    //size_t size_image = input_data.cols * input_data.rows * input_data.channels() * sizeof(float);

                    memcpy(input_data_host, input_data.data, size_input_data);
                    memcpy(affine_matrix_host, job.additional.d2i, sizeof(job.additional.d2i));

                    checkCudaRuntime(cudaMemcpyAsync(input_data_device, input_data_host, size_input_data, cudaMemcpyHostToDevice, preprocess_stream));
                    checkCudaRuntime(cudaMemcpyAsync(affine_matrix_device, affine_matrix_host, sizeof(job.additional.d2i), cudaMemcpyHostToDevice, preprocess_stream));

                    float min_val, max_val;
                    // 深度图不包含有效值mask
                    if (depth_channel_ == 1)
                    {
                        if (enable_gray_range_)
                        {
                            CUDAKernel::clip_gray_value(input_data_device, input_data.cols, input_data.rows, gray_range_min_, gray_range_max_, min_val, max_val, preprocess_stream);
                            checkCudaRuntime(cudaStreamSynchronize(preprocess_stream));
                        }

                        CUDAKernel::warp_affine_bilinear_and_normalize_plane_general(input_data_device, input_data.cols * input_data.channels(), input_data.cols, input_data.rows,
                            tensor->gpu<float>(), input_width_, input_height_, affine_matrix_device, fill_value_, normalize_, preprocess_stream);
                    }
                    // 深度图包含有效值mask
                    else
                    {
                        float* input_mask_device = (float*)(gpu_workspace + size_matrix + size_input_data);
                        float* input_mask_host = (float*)(cpu_workspace + size_matrix + size_input_data);

                        if (enable_gray_range_)
                        {
                            CUDAKernel::clip_gray_value_with_mask(input_data_device, input_mask_device, input_data.cols, input_data.rows, gray_range_min_, gray_range_max_, min_val, max_val, preprocess_stream);
                            checkCudaRuntime(cudaStreamSynchronize(preprocess_stream));
                        }
                        else
                        {
                            for (size_t i = 0; i < size_input_data / sizeof(float); ++i)
                                input_mask_host[i] = 1.0f;
                            checkCudaRuntime(cudaMemcpyAsync(input_mask_device, input_mask_host, size_input_data, cudaMemcpyHostToDevice, preprocess_stream));
                        }
                        CUDAKernel::warp_affine_bilinear_and_normalize_depth_mix_mask(input_data_device, input_mask_device, input_data.cols, input_data.rows,
                            tensor->gpu<float>(), input_width_, input_height_, affine_matrix_device, fill_value_, normalize_, preprocess_stream);
                    }
                }
                // 仅普通图片
                else
                {
                    memcpy(input_data_host, input_data.data, size_input_data);
                    memcpy(affine_matrix_host, job.additional.d2i, sizeof(job.additional.d2i));

                    checkCudaRuntime(cudaMemcpyAsync(input_data_device, input_data_host, size_input_data, cudaMemcpyHostToDevice, preprocess_stream));
                    checkCudaRuntime(cudaMemcpyAsync(affine_matrix_device, affine_matrix_host, sizeof(job.additional.d2i), cudaMemcpyHostToDevice, preprocess_stream));

                    CUDAKernel::warp_affine_bilinear_and_normalize_plane_general(input_data_device, input_data.cols * input_data.channels(), input_data.cols, input_data.rows,
                        tensor->gpu<float>(), input_width_, input_height_, affine_matrix_device, fill_value_, normalize_, preprocess_stream);
                }

                return true;
            }
        }

        virtual std::vector<std::shared_future<BoxArray>> commits(const std::vector<std::pair<cv::Mat, cv::Mat>>& imageParis) override
        {
            return TRTControllerImpl::commits(imageParis);
        }

        virtual std::shared_future<BoxArray> commit(const std::pair<cv::Mat, cv::Mat>& imagePair) override
        {
            return TRTControllerImpl::commit(imagePair);
        }

    private:
        int input_width_ = 0;
        int input_height_ = 0;
        int input_channel_ = 0;

        int image_channel_ = 0;
        int depth_channel_ = 0;

        int gpu_ = 0;

        bool enable_gray_range_ = false;
        float gray_range_min_ = 0;
        float gray_range_max_ = 0;

        float fill_value_ = 0;

        float mean_[3] = { 0.0f, 0.0f, 0.0f };
        float std_[3] = { 1.0f, 1.0f, 1.0f };

        std::vector<std::string> categories_;

        TRT::CUStream stream_ = nullptr;
        bool use_multi_preprocess_stream_ = false;
        CUDAKernel::Norm normalize_;

        int ori_width_ = 0;
        int ori_height_ = 0;
    };


    bool compile_trt_model(std::shared_ptr<Param> param, std::string& out_model_file, TRT::Mode mode)
    {
        std::string hash = TRT::get_file_fnv1a_hash(param->onnxPath);

        int device = TRT::get_device();
        cudaDeviceProp prop;
        cudaGetDeviceProperties(&prop, device);
        int smMajor = prop.major;
        int smMinor = prop.minor;

        std::string onnx_name = iLogger::file_name(param->onnxPath, true);
        std::vector<std::string> path_split = iLogger::split_string(param->onnxPath, onnx_name);
        std::string onnx_dir = path_split[0];

        std::string model_file = iLogger::format("%s_sm%d%d_%s.engine", iLogger::file_name(onnx_name, false).c_str(), smMajor, smMinor, hash.c_str());
        model_file = onnx_dir + "/" + model_file;
        out_model_file = model_file;

        if (iLogger::exists(model_file))
            return true;

        int test_batch_size = param->maxBatch;

        param->inputWidth = iLogger::upbound(param->inputWidth);
        param->inputHeight = iLogger::upbound(param->inputHeight);
        int index_of_reshape_layer = 0;

        return TRT::compile(
            mode,                       // FP32、FP16、INT8
            test_batch_size,            // max batch size
            param->onnxPath,             // source
            model_file,                 // save to
            { TRT::InputDims({1, param->imageChannel + param->depthChannel, param->inputHeight, param->inputWidth }) }
        );

    }


    std::shared_ptr<TRTInfer> create_trt_infer(const std::string& engine_file, std::shared_ptr<Param> param, bool use_multi_preprocess_stream)
    {
        std::shared_ptr<TRTInferImpl> instance(new TRTInferImpl());

        if (!instance->startup(engine_file, param, use_multi_preprocess_stream))
        {
            instance.reset();
        }

        return instance;
    }

};








