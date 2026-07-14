#include "pch.h"

#include "trt_anomaly_detection.h"

#include "monopoly_allocator.hpp"
#include "tensorrt/common/preprocess_kernel.cuh"
#include "tensorrt/common/trt_infer_controller.hpp"
#include "utils/ilogger.hpp"


namespace AnomalyDetection
{
    using TRTControllerImpl = TRTInferController
        <
        std::pair<cv::Mat, cv::Mat>,
        BoxArray,
        std::tuple<std::string, int>,
        InferenceMeta
        >;

    class TRTInferImpl : public TRTInfer, public TRTControllerImpl
    {
    public:
        virtual ~TRTInferImpl()
        {
            stop();
        }

        virtual bool startup(const std::string& file, std::shared_ptr<Param> param, bool use_multi_preprocess_stream)
        {
            image_channel_ = param->imageChannel;
            input_width_ = param->inputWidth;
            input_height_ = param->inputHeight;
            seg_threshold_ = param->segThreshold;
            score_top_ratio_ = param->scoreTopRatio;

            for (int i = 0; i < param->imageChannel; ++i)
            {
                mean_[i] = param->normalizeMean[i];
                std_[i] = param->normalizeStd[i];
            }

            normalize_ = CUDAKernel::Norm::mean_std(mean_, std_, 1.0f, CUDAKernel::ChannelType::None);
            fill_value_ = param->fillValue;
            use_multi_preprocess_stream_ = use_multi_preprocess_stream;

            return TRTControllerImpl::startup(std::make_tuple(file, param->deviceId));
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

            int max_batch_size = engine->get_max_batch_size();
            auto input = engine->input(0);
            auto output = engine->output(0);

            if (input == nullptr || output == nullptr)
            {
                INFOE("Engine input or output tensor is nullptr.");
                result.set_value(false);
                return;
            }

            input_width_ = input->size(3);
            input_height_ = input->size(2);
            input_channel_ = input->size(1);
            tensor_allocator_ = std::make_shared<MonopolyAllocator<TRT::Tensor>>(max_batch_size * 2);
            stream_ = engine->get_stream();
            gpu_ = gpuid;
            result.set_value(true);

            input->resize_single_dim(0, max_batch_size).to_gpu();

            std::vector<Job> fetch_jobs;
            while (get_jobs_and_wait(fetch_jobs, max_batch_size))
            {
                int infer_batch_size = static_cast<int>(fetch_jobs.size());
                input->resize_single_dim(0, infer_batch_size);

                for (int ibatch = 0; ibatch < infer_batch_size; ++ibatch)
                {
                    auto& job = fetch_jobs[ibatch];
                    auto& mono = job.mono_tensor->data();

                    if (mono->get_stream() != stream_)
                        checkCudaRuntime(cudaStreamSynchronize(mono->get_stream()));

                    input->copy_from_gpu(input->offset(ibatch), mono->gpu(), mono->count());
                    job.mono_tensor->release();
                }

                engine->forward(false);

                int map_h = output->size(2);
                int map_w = output->size(3);
                for (int ibatch = 0; ibatch < infer_batch_size; ++ibatch)
                {
                    const float* pScoreMap = output->cpu<float>(ibatch);
                    auto& job = fetch_jobs[ibatch];
                    auto& based_prob = job.output;

                    cv::Mat restored = RestoreScoreMap(
                        pScoreMap, map_w, map_h, job.additional.affine.d2i, job.additional.originalSize);
                    if (!restored.empty())
                        based_prob.emplace_back(BuildResult(restored, seg_threshold_, score_top_ratio_));

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
            if (tensor_allocator_ == nullptr)
            {
                INFOE("Tensor_allocator_ is nullptr.");
                return false;
            }

            if (image.empty())
            {
                INFOE("Image is empty.");
                return false;
            }

            if (image_channel_ != input_channel_)
            {
                INFOE("Please check the model configuration file. image_channel is not equal to the model input channels.");
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
                tensor = std::make_shared<TRT::Tensor>();
                tensor->set_workspace(std::make_shared<TRT::MixMemory>());

                if (use_multi_preprocess_stream_)
                {
                    checkCudaRuntime(cudaStreamCreate(&preprocess_stream));
                    tensor->set_stream(preprocess_stream, true);
                }
                else
                {
                    preprocess_stream = stream_;
                    tensor->set_stream(preprocess_stream, false);
                }
            }

            cv::Size input_size(input_width_, input_height_);
            job.additional.affine.compute(image.size(), input_size);
            job.additional.originalSize = image.size();

            preprocess_stream = tensor->get_stream();
            tensor->resize(1, image_channel_, input_height_, input_width_);

            size_t size_matrix = iLogger::upbound(sizeof(job.additional.affine.d2i), 32);
            size_t size_image = image.cols * image.rows * image.channels() * sizeof(float);
            auto workspace = tensor->get_workspace();

            uint8_t* gpu_workspace = static_cast<uint8_t*>(workspace->gpu(size_matrix + size_image));
            float* affine_matrix_device = reinterpret_cast<float*>(gpu_workspace);
            float* input_image_device = reinterpret_cast<float*>(gpu_workspace + size_matrix);

            uint8_t* cpu_workspace = static_cast<uint8_t*>(workspace->cpu(size_matrix + size_image));
            float* affine_matrix_host = reinterpret_cast<float*>(cpu_workspace);
            float* input_image_host = reinterpret_cast<float*>(cpu_workspace + size_matrix);

            memcpy(input_image_host, image.data, size_image);
            memcpy(affine_matrix_host, job.additional.affine.d2i, sizeof(job.additional.affine.d2i));

            checkCudaRuntime(cudaMemcpyAsync(input_image_device, input_image_host, size_image,
                                             cudaMemcpyHostToDevice, preprocess_stream));
            checkCudaRuntime(cudaMemcpyAsync(affine_matrix_device, affine_matrix_host, sizeof(job.additional.affine.d2i),
                                             cudaMemcpyHostToDevice, preprocess_stream));

            CUDAKernel::warp_affine_bilinear_and_normalize_plane_general(
                input_image_device, image.cols * image.channels(), image.cols, image.rows,
                tensor->gpu<float>(), input_width_, input_height_, affine_matrix_device, fill_value_,
                normalize_, preprocess_stream);

            return true;
        }

        virtual std::vector<std::shared_future<BoxArray>> commits(const std::vector<std::pair<cv::Mat, cv::Mat>>& imagePairs) override
        {
            return TRTControllerImpl::commits(imagePairs);
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
        int gpu_ = 0;

        float fill_value_ = 114;
        float seg_threshold_ = 0.5f;
        float score_top_ratio_ = 0.01f;
        float mean_[3] = { 0.0f, 0.0f, 0.0f };
        float std_[3] = { 1.0f, 1.0f, 1.0f };

        TRT::CUStream stream_ = nullptr;
        bool use_multi_preprocess_stream_ = false;
        CUDAKernel::Norm normalize_;
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

        std::string model_file = iLogger::format("%s_sm%d%d_%s.engine",
            iLogger::file_name(onnx_name, false).c_str(), smMajor, smMinor, hash.c_str());
        model_file = onnx_dir + "/" + model_file;
        out_model_file = model_file;

        if (iLogger::exists(model_file))
            return true;

        int test_batch_size = param->maxBatch;
        param->inputWidth = iLogger::upbound(param->inputWidth);
        param->inputHeight = iLogger::upbound(param->inputHeight);

        return TRT::compile(
            mode,
            test_batch_size,
            param->onnxPath,
            model_file,
            { TRT::InputDims({ 1, param->imageChannel, param->inputHeight, param->inputWidth }) }
        );
    }

    std::shared_ptr<TRTInfer> create_trt_infer(const std::string& engine_file, std::shared_ptr<Param> param,
                                               bool use_multi_preprocess_stream)
    {
        std::shared_ptr<TRTInferImpl> instance(new TRTInferImpl());
        if (!instance->startup(engine_file, param, use_multi_preprocess_stream))
            instance.reset();

        return instance;
    }
}
