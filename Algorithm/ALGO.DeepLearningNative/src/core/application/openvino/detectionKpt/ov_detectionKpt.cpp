#include "pch.h"

#include "ov_detectionKpt.h"

#include "utils/ilogger.hpp"
#include "monopoly_allocator.hpp"

#include "openvino/common/ov_infer_controller.hpp"
#include "openvino/common/ov_preprocess.h"
#include "openvino/common/ov_model_loader.h"

namespace DetectionKpt
{

    using OVControllerImpl = OVInferController
        <
        std::pair<cv::Mat, cv::Mat>,      // input
        BoxArray,                         // output
        std::tuple<std::string, int>,     // start param
        AffineMatrix                      // additional
        >;
    class OVInferImpl : public OVInfer, public OVControllerImpl
    {
    public:

        /** 要求在TRTInferImpl里面执行stop，而不是在基类执行stop **/
        virtual ~OVInferImpl()
        {
            stop();
        }

        virtual bool startup(std::shared_ptr<Param> param)
        {
            image_channel_ = param->imageChannel;
            depth_channel_ = param->depthChannel;

            for (int i = 0; i < param->imageChannel; ++i)
            {
                mean_[i] = param->normalizeMean[i];
                std_[i] = param->normalizeStd[i];
            }

            input_width_ = param->inputWidth;
            input_height_ = param->inputHeight;

            enable_gray_range_ = param->enableClipGray;
            gray_range_min_ = param->clipGrayRangeMin;
            gray_range_max_ = param->clipGrayRangeMax;

            fill_value_ = param->fillValue;

            confidence_threshold_ = param->confidenceThreshold;
            nms_threshold_ = param->nmsThreshold;
            kpt_threshold_ = param->kptThreshold;

            max_objects_ = param->maxObjects;

            use_yolo_decode_ = param->useYoloDecode;

            categories_ = param->categories;

            categoryToKptName_ = param->categoryToKptName;

            for (const auto& [cat, skeleton] : param->categoryToSkeleton)
            {
                int connectionNum = 0;
                size_t total = 0;
                for (const std::vector<int>& pair : skeleton)
                {
                    total += pair.size();
                    connectionNum++;
                }

                Skeleton* points = (Skeleton*)std::malloc(connectionNum * sizeof(Skeleton));

                size_t offset = 0;
                for (int i = 0; i < connectionNum; ++i)
                {
                    points[i].startKptId = skeleton[i][0];
                    points[i].endKptId = skeleton[i][1];
                }

                categoryToSkeleton_[cat] = points;
                categoryToConnectionNum_[cat] = connectionNum;
            }

            max_numkpts_ = param->maxNumKeypoints;

            return OVControllerImpl::startup(make_tuple(param->onnxPath, 0));
        }


        void affine_project(float* matrix, float x, float y, float* ox, float* oy)
        {
            *ox = matrix[0] * x + matrix[1] * y + matrix[2];
            *oy = matrix[3] * x + matrix[4] * y + matrix[5];
        }


        inline void yolo_post_process(float* predict, int num_anchor, int num_classes, float confidence_threshold,
            float* invert_affine_matrix, int max_objects, std::vector<std::string> categories,
            BoxArray& objects)
        {
            objects.clear();
            objects.reserve(max_objects);

            int index = 0;
            for (int anchor_idx = 0; anchor_idx < num_anchor; anchor_idx++)
            {
                float* pitem = predict + (4 + num_classes + 3 * max_numkpts_) * anchor_idx;
                float* class_confidence = pitem + 4;

                float confidence = *class_confidence++;
                int label = 0;
                for (int i = 1; i < num_classes; ++i, ++class_confidence)
                {
                    if (*class_confidence > confidence)
                    {
                        confidence = *class_confidence;
                        label = i;
                    }
                }
                //float* max_it = std::max_element(class_confidence, class_confidence + num_classes);
                //float confidence = *max_it;
                //int label = int(max_it - class_confidence);

                if (confidence < confidence_threshold)
                    continue;

                index++;

                if (index >= max_objects)
                    break;

                float cx = *pitem++;
                float cy = *pitem++;
                float width = *pitem++;
                float height = *pitem++;

                float half_w = width * 0.5f;
                float half_h = height * 0.5f;

                float left = cx - half_w;
                float right = cx + half_w;
                float top = cy - half_h;
                float bottom = cy + half_h;

                affine_project(invert_affine_matrix, left, top, &left, &top);
                affine_project(invert_affine_matrix, right, bottom, &right, &bottom);

                Result r;
                r.cx = (left + right) * 0.5;
                r.cy = (top + bottom) * 0.5;
                r.width = right - left;
                r.height = bottom - top;
                r.confidence = confidence;
                r.class_id = label;
                if (label < categories_.size())
                {
                    r.class_name = categories_[label].c_str();
                }
                else
                {
                    r.class_name = "unknown";
                }

                pitem++;
                Point* p = (Point*)std::malloc(max_numkpts_ * sizeof(Point));
                for (int kIdx = 0; kIdx < max_numkpts_; ++kIdx)
                {
                    float keypoint_x = *pitem++;
                    float keypoint_y = *pitem++;
                    float keypoint_confidence = *pitem++;

                    affine_project(invert_affine_matrix, keypoint_x, keypoint_y, &keypoint_x, &keypoint_y);

                    p[kIdx].x = keypoint_x;
                    p[kIdx].y = keypoint_y;
                    p[kIdx].confidence = keypoint_confidence;

                    int connectionNum = 0;
                    Skeleton* skeletonData = nullptr;
                    if (categoryToConnectionNum_.find(label) != categoryToConnectionNum_.end() &&
                        categoryToSkeleton_.find(label) != categoryToSkeleton_.end())
                    {
                        connectionNum = categoryToConnectionNum_[label];
                        skeletonData = categoryToSkeleton_[label];
                    }
                    r.keypoints = Kpts(p, max_numkpts_, skeletonData, connectionNum, kpt_threshold_);
                }

                objects.emplace_back(r);
            }

            objects = cpu_nms(objects, nms_threshold_);
        }


        inline void detr_post_process(float* predict, int num_anchor, int num_classes, float confidence_threshold,
            int input_width, int input_height, float* invert_affine_matrix,
            int max_objects, std::vector<std::string> categories, BoxArray& objects)
        {
            objects.clear();
            objects.reserve(max_objects);

            int index = 0;
            for (int anchor_idx = 0; anchor_idx < num_anchor; anchor_idx++)
            {
                float* pitem = predict + (4 + 1 + 1) * anchor_idx;

                float confidence = *(pitem + 4);
                int label = *(pitem + 5);

                if (confidence < confidence_threshold)
                    continue;

                index++;

                if (index >= max_objects)
                    break;

                float left = (*pitem++) * input_width;
                float top = (*pitem++) * input_height;
                float right = (*pitem++) * input_width;
                float bottom = (*pitem++) * input_height;

                float aff_left, aff_right, aff_top, aff_bottom;
                affine_project(invert_affine_matrix, left, top, &aff_left, &aff_top);
                affine_project(invert_affine_matrix, right, bottom, &aff_right, &aff_bottom);

                Result r;
                r.cx = (aff_left + aff_right) * 0.5;
                r.cy = (aff_top + aff_bottom) * 0.5;
                r.width = aff_right - aff_left;
                r.height = aff_bottom - aff_top;
                r.confidence = confidence;
                r.class_id = label;
                if (label < categories_.size())
                {
                    r.class_name = categories_[label].c_str();
                }
                else
                {
                    r.class_name = "unknown";
                }

                pitem++;
                Point* p = (Point*)std::malloc(max_numkpts_ * sizeof(Point));
                for (int kIdx = 0; kIdx < max_numkpts_; ++kIdx)
                {
                    float keypoint_x = (*pitem++) * input_width;
                    float keypoint_y = (*pitem++) * input_height;
                    float keypoint_confidence = *pitem++;

                    affine_project(invert_affine_matrix, keypoint_x, keypoint_y, &keypoint_x, &keypoint_y);

                    p[kIdx].x = keypoint_x;
                    p[kIdx].y = keypoint_y;
                    p[kIdx].confidence = keypoint_confidence;

                    if (categoryToConnectionNum_.find(label) != categoryToConnectionNum_.end() &&
                        categoryToSkeleton_.find(label) != categoryToSkeleton_.end())
                    {
                        int connectionNum = categoryToConnectionNum_[label];
                        Skeleton* skeletonData = categoryToSkeleton_[label];

                        r.keypoints = Kpts(p, max_numkpts_, skeletonData, connectionNum, kpt_threshold_);
                    }
                }

                objects.emplace_back(r);
            }

            objects = cpu_nms(objects, nms_threshold_);

        }


        virtual void worker(std::promise<bool>& result) override
        {
            std::string file = std::get<0>(start_param_);
            int device_id = std::get<1>(start_param_);

            ov::Core core;
            //std::shared_ptr<ov::Model> model = core.read_model(file);
            std::shared_ptr<ov::Model> model = OVLoader::LoadModel(core, file);

            auto input_node = model->input();
            std::string input_tensor_name = input_node.get_any_name();

            ov::CompiledModel compiled_model;
            if (device_id == 0)
            {
                compiled_model = core.compile_model(model, "CPU");
            }
            else if (device_id == 1)
            {
                compiled_model = core.compile_model(model, "GPU");
            }
            else
            {
                compiled_model = core.compile_model(model, "AUTO");
            }

            auto input_port = compiled_model.input("inputs");
            auto output_port = compiled_model.output("bbox_outputs");

            ov::PartialShape input_shape = input_port.get_partial_shape();
            if (input_shape.rank().get_length() > 3)
            {
                if (!input_shape[1].is_dynamic())
                {
                    input_channel_ = input_shape[1].get_length();
                }
                if (!input_shape[2].is_dynamic())
                {
                    input_height_ = input_shape[2].get_length();
                }
                if (!input_shape[3].is_dynamic())
                {
                    input_width_ = input_shape[3].get_length();
                }
            }
            else
            {
                input_channel_ = image_channel_ + depth_channel_;
            }

            ov::InferRequest infer_request = compiled_model.create_infer_request();

            // 非CPU推理需要warmup
            if (device_id != 0)
            {
                for (int i = 0; i < 10; i++)
                {
                    ov::Tensor warmup_tensor = infer_request.get_tensor(input_port);
                    std::fill_n(warmup_tensor.data<float>(), warmup_tensor.get_size(), 0.0f);
                    infer_request.set_tensor(input_port, warmup_tensor);
                    infer_request.infer();
                }
            }

            tensor_allocator_ = std::make_shared<MonopolyAllocator<ov::Tensor>>(1 * 8);

            result.set_value(true);

            Job fetch_job;
            while (get_job_and_wait(fetch_job))
            {
                auto& job = fetch_job;
                auto& mono = job.mono_tensor->data();
                ov::Tensor input(ov::element::f32, mono->get_shape());
                std::memcpy(input.data<float>(), mono->data<float>(), mono->get_byte_size());
                job.mono_tensor->release();

                infer_request.set_tensor(input_port, input);
                infer_request.infer();
                infer_request.wait();

                const ov::Tensor& output_tensor = infer_request.get_tensor(output_port);

                ov::Shape output_shape = output_tensor.get_shape();

                int num_classes = output_shape[2] - 4 - 3 * max_numkpts_;

#pragma warning(push)
#pragma warning(disable: 4996)
                const float* pArray = output_tensor.data<float>();
#pragma warning(pop)

                auto& image_based_boxes = job.output;
                float* affine_matrix = job.additional.d2i;

                if (use_yolo_decode_)
                {
                    yolo_post_process((float*)pArray, output_shape[1], num_classes, confidence_threshold_,
                        affine_matrix, max_objects_, categories_, image_based_boxes);
                }
                else
                {
                    throw std::runtime_error("Unsupported bbox decoding mode.");
                }

                job.pro->set_value(image_based_boxes);

            }

            for (auto& [cat, skeleton] : categoryToSkeleton_)
            {
                free(skeleton);
                skeleton = nullptr;
            }

            tensor_allocator_.reset();
            INFO("Engine destroy.");
        }

        virtual bool preprocess(Job& job, const std::pair<cv::Mat, cv::Mat>& imagePair) override
        {
            cv::Mat image = imagePair.first;
            cv::Mat depth = imagePair.second;

            if (tensor_allocator_ == nullptr)
            {
                INFOE("tensor_allocator_ is nullptr");
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

            auto& tensor = job.mono_tensor->data();

            if (tensor == nullptr)
            {
                // not init
                tensor = std::make_shared<ov::Tensor>();
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

            ov::Shape input_shape = { 1,
                                      static_cast<size_t>(image_channel_ + depth_channel_),
                                      static_cast<size_t>(input_height_),
                                      static_cast<size_t>(input_width_) };

            ov::element::Type input_type = ov::element::f32;
            tensor = std::make_shared<ov::Tensor>(input_type, input_shape);

            cv::Mat affineMatrix = job.additional.i2d_mat();

            // 图片与深度图混合输入模式
            if (image_channel_ != 0 && depth_channel_ != 0)
            {
                cv::Mat affineImage, affineDepth, affineMix;

                float min_val, max_val;
                // 深度图不包含有效值mask
                if (depth_channel_ == 1)
                {
                    affineDepth = depth;
                    if (enable_gray_range_)
                    {
                        affineDepth = OVPreprocess::clip_gray_value(depth, gray_range_min_, gray_range_max_, min_val, max_val);
                    }

                    OVPreprocess::warpAffineAndNormalizeImageMixDepth(image, affineDepth, affineMix, input_width_, input_height_, affineMatrix, mean_, std_, fill_value_);

                    std::memcpy(tensor->data<float>(), affineMix.data, affineMix.total() * affineMix.elemSize());

                }
                // 深度图包含有效值mask
                else
                {
                    cv::Mat validMask;
                    affineDepth = depth;
                    validMask = cv::Mat::ones(depth.size(), CV_32F);
                    if (enable_gray_range_)
                    {
                        affineDepth = OVPreprocess::clip_gray_value_with_mask(depth, gray_range_min_, gray_range_max_, min_val, max_val, validMask);
                    }

                    OVPreprocess::warpAffineAndNormalizeImageMixDepthMixDepth(image, affineDepth, validMask, affineMix, input_width_, input_height_, affineMatrix, mean_, std_, fill_value_);

                    std::memcpy(tensor->data<float>(), affineMix.data, affineMix.total() * affineMix.elemSize());
                }

                return true;
            }

            // 仅普通图片或仅深度图输入模式
            else
            {
                cv::Mat affineImage;

                // 仅深度图
                if (image_channel_ == 0 && depth_channel_ != 0)
                {
                    float min_val, max_val;
                    // 深度图不包含有效值mask
                    if (depth_channel_ == 1)
                    {
                        affineImage = depth;
                        if (enable_gray_range_)
                        {
                            affineImage = OVPreprocess::clip_gray_value(depth, gray_range_min_, gray_range_max_, min_val, max_val);
                        }

                        OVPreprocess::warpAffineAndNormalize(affineImage, affineImage, input_width_, input_height_, affineMatrix, mean_, std_, fill_value_);

                        std::memcpy(tensor->data<float>(), affineImage.data, affineImage.total() * affineImage.elemSize());
                    }
                    // 深度图包含有效值mask
                    else
                    {
                        cv::Mat validMask;
                        affineImage = depth;
                        validMask = cv::Mat::ones(depth.size(), CV_32F);
                        if (enable_gray_range_)
                        {
                            affineImage = OVPreprocess::clip_gray_value_with_mask(depth, gray_range_min_, gray_range_max_, min_val, max_val, validMask);
                        }

                        OVPreprocess::warpAffineAndNormalizeDepthMixMask(affineImage, validMask, affineImage, input_width_, input_height_, affineMatrix, mean_, std_, fill_value_);

                        std::memcpy(tensor->data<float>(), affineImage.data, affineImage.total() * affineImage.elemSize());
                    }

                }
                // 仅普通图片
                else
                {
                    OVPreprocess::warpAffineAndNormalize(image, affineImage, input_width_, input_height_, affineMatrix, mean_, std_, fill_value_);

                    std::memcpy(tensor->data<float>(), affineImage.data, affineImage.total() * affineImage.elemSize());
                }

                return true;
            }
        }

        virtual std::vector<std::shared_future<BoxArray>> commits(const std::vector<std::pair<cv::Mat, cv::Mat>>& imageParis) override
        {
            return OVControllerImpl::commits(imageParis);
        }

        virtual std::shared_future<BoxArray> commit(const const std::pair<cv::Mat, cv::Mat>& imagePair) override
        {
            return OVControllerImpl::commit(imagePair);
        }

    private:
        int input_width_ = 0;
        int input_height_ = 0;
        int input_channel_ = 0;

        int image_channel_ = 0;
        int depth_channel_ = 0;

        bool enable_gray_range_ = false;
        float gray_range_min_ = 0;
        float gray_range_max_ = 0;

        float fill_value_ = 0;

        float mean_[3] = { 0.0f, 0.0f, 0.0f };
        float std_[3] = { 1.0f, 1.0f, 1.0f };

        float confidence_threshold_ = 0;
        float nms_threshold_ = 0;
        float kpt_threshold_ = 0;

        int max_objects_ = 1024;

        bool use_yolo_decode_ = true;

        std::map<int, std::vector<std::string>> categoryToKptName_;
        std::map<int, Skeleton*> categoryToSkeleton_;
        std::map<int, int> categoryToConnectionNum_;

        int max_numkpts_ = 0;

        std::vector<std::string> categories_;


    };


    std::shared_ptr<OVInfer> create_ov_infer(std::shared_ptr<Param> param)
    {
        std::shared_ptr<OVInferImpl> instance(new OVInferImpl());

        if (!instance->startup(param))
        {
            instance.reset();
        }

        return instance;
    }



}
