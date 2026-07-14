#include "pch.h"
#include "trt_detectionSeg.h"

#include "utils/ilogger.hpp"

#include "monopoly_allocator.hpp"
#include "tensorrt/common/cuda_tools.hpp"
#include "tensorrt/common/trt_infer_controller.hpp"
#include "tensorrt/common/preprocess_kernel.cuh"


namespace DetectionSeg
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

            confidence_threshold_ = param->confidenceThreshold;
            nms_threshold_ = param->nmsThreshold;
            seg_threshold_ = param->segThreshold;

            max_objects_ = param->maxObjects;

            use_yolo_decode_ = param->useYoloDecode;

            categories_ = param->categories;
            use_multi_preprocess_stream_ = use_multi_preprocess_stream;

            return TRTControllerImpl::startup(make_tuple(file, param->deviceId));
        }


        inline void yolo_result_loader(std::vector<Job>& fetch_jobs, TRT::Tensor& output_boxarray_device, TRT::Tensor& output_maskarray_device,
                                       std::shared_ptr<TRT::Tensor> bbox_head_output, std::shared_ptr<TRT::Tensor> mask_head_output,
                                       const int MAX_IMAGE_BBOX, const int NUM_BOX_ELEMENT, int infer_batch_size, int num_classes)
        {

            output_boxarray_device.to_cpu();
            for (int ibatch = 0; ibatch < infer_batch_size; ++ibatch)
            {
                float* parray = output_boxarray_device.cpu<float>(ibatch);
                int count = std::min(MAX_IMAGE_BBOX, (int)*parray);
                auto& job = fetch_jobs[ibatch];
                auto& image_based_boxes = job.output;
                for (int i = 0; i < count; ++i)
                {
                    float* pbox = parray + 1 + i * NUM_BOX_ELEMENT;

                    int label = pbox[5];
                    int keepflag = pbox[6];
                    if (keepflag == 1)
                    {
                        Result r;
                        r.width = pbox[2] - pbox[0];
                        r.height = pbox[3] - pbox[1];
                        r.cx = pbox[0] + r.width * 0.5;
                        r.cy = pbox[1] + r.height * 0.5;
                        r.confidence = pbox[4];
                        r.class_id = label;
                        r.class_name = label < categories_.size() ? categories_[label].c_str() : "unknown";

                        // decode mask
                        int anchor_index = pbox[7];
                        int mask_dim = mask_head_output->size(1);

                        float* mask_weights = bbox_head_output->gpu<float>(ibatch) + anchor_index * bbox_head_output->size(2) + num_classes + 4;
                        float* mask_head_predict = mask_head_output->gpu<float>(ibatch);
                        float left, top, right, bottom;
                        float* i2d = job.additional.i2d;
                        affine_project(i2d, pbox[0], pbox[1], &left, &top);
                        affine_project(i2d, pbox[2], pbox[3], &right, &bottom);

                        float box_width = right - left;
                        float box_height = bottom - top;
                        float scale_to_predict_x = mask_head_output->size(3) / (float)input_width_;
                        float scale_to_predict_y = mask_head_output->size(2) / (float)input_height_;
                        int mask_out_width = box_width * scale_to_predict_x + 0.5f;
                        int mask_out_height = box_height * scale_to_predict_y + 0.5f;

                        if (mask_out_width > 0 && mask_out_height > 0)
                        {
                            int bytes_of_mask_out = mask_out_width * mask_out_height;
                            output_maskarray_device.resize(bytes_of_mask_out).to_gpu();
                            output_maskarray_device.to_gpu(false);

                            // 构造还原矩阵
                            float affine_matrix[6]{ 1, 0, 0, 0, 1, 0 };

                            std::vector<cv::Point2f> src = {
                                                    {0, 0},
                                                    {static_cast<float>(mask_out_width), 0},
                                                    {static_cast<float>(mask_out_width), static_cast<float>(mask_out_height)},
                                                    {0, static_cast<float>(mask_out_height)}
                            };

                            std::vector<cv::Point2f> dst = {
                                                    {pbox[0], pbox[1]},
                                                    {pbox[2], pbox[1]},
                                                    {pbox[2], pbox[3]},
                                                    {pbox[0], pbox[3]}
                            };
                            cv::Mat M = cv::estimateAffine2D(src, dst);
                            if (!M.empty() && M.rows == 2 && M.cols == 3)
                            {
                                cv::Mat Mf;
                                M.convertTo(Mf, CV_32F);

                                if (Mf.isContinuous())
                                    std::memcpy(affine_matrix, Mf.data, 6 * sizeof(float));
                                else
                                    std::memcpy(affine_matrix, Mf.clone().data, 6 * sizeof(float));
                            }


                            r.segmentation = Seg(mask_out_width, mask_out_height, affine_matrix, seg_threshold_, false);

                            float* mask_out_device = output_maskarray_device.gpu<float>();
                            float* mask_out_host = r.segmentation.floatData;

                            yolo_mask_decode_single(left * scale_to_predict_x, top * scale_to_predict_y, mask_weights,
                                                    mask_head_predict, mask_head_output->size(3), mask_head_output->size(2),
                                                    mask_out_device, mask_dim, mask_out_width, mask_out_height, stream_);

                            checkCudaRuntime(cudaMemcpyAsync(mask_out_host, mask_out_device, output_maskarray_device.bytes(), cudaMemcpyDeviceToHost, stream_));
                            checkCudaRuntime(cudaStreamSynchronize(stream_));
                        }


                        image_based_boxes.emplace_back(r);
                    }
                }

                if (nms_method_ == NMSMethod::CPU)
                {
                    image_based_boxes = cpu_nms(image_based_boxes, nms_threshold_);
                }

                job.pro->set_value(image_based_boxes);
            }
        }


        inline void yolo_process(int gpuid, std::shared_ptr<TRT::Infer> engine, std::promise<bool>& result)
        {
            const int MAX_IMAGE_BBOX = max_objects_;
            const int NUM_BOX_ELEMENT = 8;   // left, top, right, bottom, confidence, class, keepflag, anchor_index

            TRT::Tensor affine_matrix_device(TRT::DataType::Float);
            TRT::Tensor output_boxarray_device(TRT::DataType::Float);
            TRT::Tensor output_maskarray_device(TRT::DataType::Float);

            int max_batch_size = engine->get_max_batch_size();
            auto input = engine->tensor("inputs");
            auto bbox_head_output = engine->tensor("bbox_outputs");
            auto mask_head_output = engine->tensor("mask_outputs");

            int num_classes = bbox_head_output->size(2) - 4 - mask_head_output->size(1);

            input_width_ = input->size(3);
            input_height_ = input->size(2);
            input_channel_ = input->size(1);
            tensor_allocator_ = std::make_shared<MonopolyAllocator<TRT::Tensor>>(max_batch_size * 2);
            stream_ = engine->get_stream();
            gpu_ = gpuid;
            result.set_value(true);

            input->resize_single_dim(0, max_batch_size).to_gpu();
            affine_matrix_device.set_stream(stream_);

            affine_matrix_device.resize(max_batch_size, 8).to_gpu();
            output_boxarray_device.resize(max_batch_size, 1 + 31 + MAX_IMAGE_BBOX * NUM_BOX_ELEMENT).to_gpu();

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

                    affine_matrix_device.copy_from_gpu(affine_matrix_device.offset(ibatch), mono->get_workspace()->gpu(), 6);
                    input->copy_from_gpu(input->offset(ibatch), mono->gpu(), mono->count());
                    job.mono_tensor->release();
                }

                engine->forward(false);

                output_boxarray_device.to_gpu(false);
                for (int ibatch = 0; ibatch < infer_batch_size; ++ibatch)
                {
                    auto& job = fetch_jobs[ibatch];
                    float* image_based_output = bbox_head_output->gpu<float>(ibatch);
                    float* output_array_ptr = output_boxarray_device.gpu<float>(ibatch);
                    auto affine_matrix = affine_matrix_device.gpu<float>(ibatch);
                    checkCudaRuntime(cudaMemsetAsync(output_array_ptr, 0, sizeof(int), stream_));

                    yolo_bbox_decode_kernel_invoker(image_based_output, bbox_head_output->size(1), num_classes, confidence_threshold_,
                                                    affine_matrix, output_array_ptr, MAX_IMAGE_BBOX, stream_);

                    if (nms_method_ == NMSMethod::FastGPU)
                    {
                        nms_kernel_invoker(output_array_ptr, nms_threshold_, MAX_IMAGE_BBOX, stream_);
                    }
                }

                output_boxarray_device.to_cpu();
                for (int ibatch = 0; ibatch < infer_batch_size; ++ibatch)
                {
                    float* parray = output_boxarray_device.cpu<float>(ibatch);
                    int count = std::min(MAX_IMAGE_BBOX, (int)*parray);
                    auto& job = fetch_jobs[ibatch];
                    auto& image_based_boxes = job.output;
                    for (int i = 0; i < count; ++i)
                    {
                        float* pbox = parray + 1 + i * NUM_BOX_ELEMENT;

                        int label = pbox[5];
                        int keepflag = pbox[6];
                        if (keepflag == 1)
                        {
                            Result r;
                            r.width = pbox[2] - pbox[0];
                            r.height = pbox[3] - pbox[1];
                            r.cx = pbox[0] + r.width * 0.5;
                            r.cy = pbox[1] + r.height * 0.5;
                            r.confidence = pbox[4];
                            r.class_id = label;
                            r.class_name = label < categories_.size() ? categories_[label].c_str() : "unknown";

                            // decode mask
                            int anchor_index = pbox[7];
                            int mask_dim = mask_head_output->size(1);

                            float* mask_weights = bbox_head_output->gpu<float>(ibatch) + anchor_index * bbox_head_output->size(2) + num_classes + 4;
                            float* mask_head_predict = mask_head_output->gpu<float>(ibatch);
                            float left, top, right, bottom;
                            float* i2d = job.additional.i2d;
                            affine_project(i2d, pbox[0], pbox[1], &left, &top);
                            affine_project(i2d, pbox[2], pbox[3], &right, &bottom);

                            float box_width = right - left;
                            float box_height = bottom - top;
                            float scale_to_predict_x = mask_head_output->size(3) / (float)input_width_;
                            float scale_to_predict_y = mask_head_output->size(2) / (float)input_height_;
                            int mask_out_width = box_width * scale_to_predict_x + 0.5f;
                            int mask_out_height = box_height * scale_to_predict_y + 0.5f;

                            if (mask_out_width > 0 && mask_out_height > 0)
                            {
                                int bytes_of_mask_out = mask_out_width * mask_out_height;
                                output_maskarray_device.resize(bytes_of_mask_out).to_gpu();
                                output_maskarray_device.to_gpu(false);

                                // 构造还原矩阵
                                float affine_matrix[6]{ 1, 0, 0, 0, 1, 0 };

                                std::vector<cv::Point2f> src = {
                                                        {0, 0},
                                                        {static_cast<float>(mask_out_width), 0},
                                                        {static_cast<float>(mask_out_width), static_cast<float>(mask_out_height)},
                                                        {0, static_cast<float>(mask_out_height)}
                                };

                                std::vector<cv::Point2f> dst = {
                                                        {pbox[0], pbox[1]},
                                                        {pbox[2], pbox[1]},
                                                        {pbox[2], pbox[3]},
                                                        {pbox[0], pbox[3]}
                                };
                                cv::Mat M = cv::estimateAffine2D(src, dst);
                                if (!M.empty() && M.rows == 2 && M.cols == 3)
                                {
                                    cv::Mat Mf;
                                    M.convertTo(Mf, CV_32F);

                                    if (Mf.isContinuous())
                                        std::memcpy(affine_matrix, Mf.data, 6 * sizeof(float));
                                    else
                                        std::memcpy(affine_matrix, Mf.clone().data, 6 * sizeof(float));
                                }

                                r.segmentation = Seg(mask_out_width, mask_out_height, affine_matrix, seg_threshold_, false);

                                float* mask_out_device = output_maskarray_device.gpu<float>();
                                float* mask_out_host = r.segmentation.floatData;

                                yolo_mask_decode_single(left * scale_to_predict_x, top * scale_to_predict_y, mask_weights,
                                                        mask_head_predict, mask_head_output->size(3), mask_head_output->size(2),
                                                        mask_out_device, mask_dim, mask_out_width, mask_out_height, stream_);

                                checkCudaRuntime(cudaMemcpyAsync(mask_out_host, mask_out_device, output_maskarray_device.bytes(), cudaMemcpyDeviceToHost, stream_));
                                checkCudaRuntime(cudaStreamSynchronize(stream_));
                            }


                            image_based_boxes.emplace_back(r);
                        }
                    }

                    if (nms_method_ == NMSMethod::CPU)
                    {
                        image_based_boxes = cpu_nms(image_based_boxes, nms_threshold_);
                    }

                    job.pro->set_value(image_based_boxes);
                }

                fetch_jobs.clear();
            }
            stream_ = nullptr;
            tensor_allocator_.reset();
            INFO("Engine destroy.");
        }

        inline void detr_process(int gpuid, std::shared_ptr<TRT::Infer> engine, std::promise<bool>& result)
        { 
            const int MAX_IMAGE_BBOX = max_objects_;
            const int NUM_BOX_ELEMENT = 8;      // left, top, right, bottom, confidence, class, keepflag, bboxIdx
            TRT::Tensor affine_matrix_device(TRT::DataType::Float);
            TRT::Tensor output_boxarray_device(TRT::DataType::Float);
            TRT::Tensor output_maskarray_device(TRT::DataType::Float);
            TRT::Tensor output_labelarray_device(TRT::DataType::Int32);
            int opt_batch_size = engine->get_opt_batch_size();
            int max_batch_size = engine->get_max_batch_size();

            auto input = engine->tensor("input");
            auto labels = engine->tensor("labels");
            auto boxes = engine->tensor("boxes");
            auto scores = engine->tensor("scores");
            auto masks = engine->tensor("masks");

            input_width_ = input->size(3);
            input_height_ = input->size(2);
            input_channel_ = input->size(1);
            tensor_allocator_ = std::make_shared<MonopolyAllocator<TRT::Tensor>>(max_batch_size * 4);
            stream_ = engine->get_stream();
            gpu_ = gpuid;
            result.set_value(true);

            input->resize_single_dim(0, opt_batch_size).to_gpu();
            affine_matrix_device.set_stream(stream_);
            output_labelarray_device.set_stream(stream_);

            // 这里8个值的目的是保证 8 * sizeof(float) % 32 == 0
            affine_matrix_device.resize(opt_batch_size, 8).to_gpu();

            int mask_w = masks->size(3);
            int mask_h = masks->size(2);
            int NUM_MASK_ELEMENT = mask_w * mask_h;
            // 这里的 1 + MAX_IMAGE_BBOX结构是，counter + bboxes ...
            output_boxarray_device.resize(opt_batch_size, 1 + MAX_IMAGE_BBOX * NUM_BOX_ELEMENT).to_gpu();
            output_maskarray_device.resize(opt_batch_size, MAX_IMAGE_BBOX * NUM_MASK_ELEMENT).to_gpu();

            std::vector<Job> fetch_jobs;
            while (get_jobs_and_wait(fetch_jobs, opt_batch_size))
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

                    affine_matrix_device.copy_from_gpu(affine_matrix_device.offset(ibatch), mono->get_workspace()->gpu(), 6);
                    input->copy_from_gpu(input->offset(ibatch), mono->gpu(), mono->count());
                    job.mono_tensor->release();
                }

                engine->forward(false);

                output_boxarray_device.to_gpu(false);

                const int output_label_count = labels->size(1);
                const auto labels_type = labels->type();
                if (labels_type == TRT::DataType::Int64)
                {
                    output_labelarray_device.resize(infer_batch_size, output_label_count).to_gpu(false);
                }
                else if (labels_type != TRT::DataType::Int32)
                {
                    INFOE("Unsupported labels tensor dtype: %d", (int)labels_type);
                }

                for (int ibatch = 0; ibatch < infer_batch_size; ++ibatch)
                {
                    int* output_label_array = nullptr;
                    float* output_box_array = boxes->gpu<float>(ibatch);
                    float* output_score_array = scores->gpu<float>(ibatch);

                    float* output_boxarray_ptr = output_boxarray_device.gpu<float>(ibatch);
                    auto affine_matrix = affine_matrix_device.gpu<float>(ibatch);
                    checkCudaRuntime(cudaMemsetAsync(output_boxarray_ptr, 0, sizeof(int), stream_));

                    if (labels_type == TRT::DataType::Int64)
                    {
                        output_label_array = output_labelarray_device.gpu<int>(ibatch);
                        cast_int64_labels_to_int32_kernel_invoker(labels->gpu<int64_t>(ibatch), output_label_array, output_label_count, stream_);
                    }
                    else if (labels_type == TRT::DataType::Int32)
                    {
                        output_label_array = labels->gpu<int>(ibatch);
                    }
                    else
                    {
                        continue;
                    }

                    detr_bbox_decode_kernel_invoker(output_label_array, output_box_array, output_score_array, output_label_count,
                        confidence_threshold_, input_width_, input_height_, affine_matrix, output_boxarray_ptr,
                        MAX_IMAGE_BBOX, stream_);

                    if (nms_method_ == NMSMethod::FastGPU)
                    {
                        nms_kernel_invoker(output_boxarray_ptr, nms_threshold_, MAX_IMAGE_BBOX, stream_);
                    }
                }

                output_boxarray_device.to_cpu();

                int pending_mask_copies = 0;

                for (int ibatch = 0; ibatch < infer_batch_size; ++ibatch)
                {
                    float* pBoxArray = output_boxarray_device.cpu<float>(ibatch);
                    float* pMaskArrayDevice = masks->gpu<float>(ibatch);

                    int count = std::min(MAX_IMAGE_BBOX, (int)*pBoxArray);
                    auto& job = fetch_jobs[ibatch];
                    auto& image_based_boxes = job.output;
                    image_based_boxes.reserve(image_based_boxes.size() + count);

                    float mask_affine_matrix[6];
                    std::memcpy(mask_affine_matrix, job.additional.d2i, sizeof(mask_affine_matrix));
                    mask_affine_matrix[0] *= input_width_ / (float)mask_w;
                    mask_affine_matrix[4] *= input_height_ / (float)mask_h;

                    for (int i = 0; i < count; ++i)
                    {
                        float* pbox = pBoxArray + 1 + i * NUM_BOX_ELEMENT;

                        int label = (int)pbox[5];
                        int keepflag = (int)pbox[6];
                        if (keepflag != 1)
                            continue;

                        int bboxId = (int)pbox[7];
                        if (bboxId < 0 || bboxId >= output_label_count)
                            continue;

                        image_based_boxes.emplace_back();
                        Result& r = image_based_boxes.back();

                        r.width = pbox[2] - pbox[0];
                        r.height = pbox[3] - pbox[1];
                        r.cx = pbox[0] + r.width * 0.5f;
                        r.cy = pbox[1] + r.height * 0.5f;
                        r.confidence = pbox[4];
                        r.class_id = label;
                        r.class_name = label < categories_.size() ? categories_[label].c_str() : "unknown";
                        r.angle = (float)bboxId;
                    }

                    if (nms_method_ == NMSMethod::CPU)
                    {
                        image_based_boxes = cpu_nms(image_based_boxes, nms_threshold_);
                    }

                    for (auto& r : image_based_boxes)
                    {
                        int bboxId = (int)r.angle;
                        r.angle = -1;
                        r.segmentation = Seg(mask_w, mask_h, mask_affine_matrix, seg_threshold_, false);

                        float* pmaskDevice = pMaskArrayDevice + bboxId * NUM_MASK_ELEMENT;
                        float* pmaskHost = r.segmentation.floatData;

                        int mask_task_index = pending_mask_copies++;
                        float* pmaskScratch = output_maskarray_device.gpu<float>() + mask_task_index * NUM_MASK_ELEMENT;

                        detr_mask_sigmoid_single(pmaskDevice, pmaskScratch, NUM_MASK_ELEMENT, stream_);

                        checkCudaRuntime(cudaMemcpyAsync(pmaskHost, pmaskScratch, NUM_MASK_ELEMENT * sizeof(float),
                            cudaMemcpyDeviceToHost, stream_));
                    }
                }

                if (pending_mask_copies > 0)
                {
                    checkCudaRuntime(cudaStreamSynchronize(stream_));
                }

                for (int ibatch = 0; ibatch < infer_batch_size; ++ibatch)
                {
                    auto& job = fetch_jobs[ibatch];
                    job.pro->set_value(std::move(job.output));
                }

                fetch_jobs.clear();
            }
            stream_ = nullptr;
            tensor_allocator_.reset();
            INFO("Engine destroy.");
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

            if (use_yolo_decode_)
            {
                yolo_process(gpuid, engine, result);
            }
            else
            {
                detr_process(gpuid, engine, result);
            }
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

        float confidence_threshold_ = 0;
        float nms_threshold_ = 0;
        float seg_threshold_ = 0;

        int max_objects_ = 1024;

        bool use_yolo_decode_= true;

        NMSMethod nms_method_ = NMSMethod::FastGPU;

        std::vector<std::string> categories_;

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
            param->onnxPath,            // source
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








