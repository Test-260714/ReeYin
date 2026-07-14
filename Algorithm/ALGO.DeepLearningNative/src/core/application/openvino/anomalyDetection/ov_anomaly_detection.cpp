#include "pch.h"

#include "ov_anomaly_detection.h"

#include "monopoly_allocator.hpp"
#include "openvino/common/ov_infer_controller.hpp"
#include "openvino/common/ov_model_loader.h"
#include "openvino/common/ov_preprocess.h"
#include "utils/ilogger.hpp"


namespace AnomalyDetection
{
    using OVControllerImpl = OVInferController
        <
        std::pair<cv::Mat, cv::Mat>,
        BoxArray,
        std::tuple<std::string, int>,
        InferenceMeta
        >;

    class OVInferImpl : public OVInfer, public OVControllerImpl
    {
    public:
        virtual ~OVInferImpl()
        {
            stop();
        }

        virtual bool startup(std::shared_ptr<Param> param)
        {
            image_channel_ = param->imageChannel;
            input_width_ = param->inputWidth;
            input_height_ = param->inputHeight;
            fill_value_ = param->fillValue;
            seg_threshold_ = param->segThreshold;
            score_top_ratio_ = param->scoreTopRatio;

            for (int i = 0; i < param->imageChannel; ++i)
            {
                mean_[i] = param->normalizeMean[i];
                std_[i] = param->normalizeStd[i];
            }

            return OVControllerImpl::startup(std::make_tuple(param->onnxPath, param->deviceId));
        }

        virtual void worker(std::promise<bool>& result) override
        {
            bool startup_notified = false;
            auto notify_startup = [&](bool value)
            {
                if (!startup_notified)
                {
                    result.set_value(value);
                    startup_notified = true;
                }
            };

            try
            {
                std::string file = std::get<0>(start_param_);
                int device_id = std::get<1>(start_param_);

                ov::Core core;
                std::shared_ptr<ov::Model> model = OVLoader::LoadModel(core, file);
                if (model == nullptr)
                {
                    INFOE("Failed to load OpenVINO anomaly detection model: %s", file.c_str());
                    notify_startup(false);
                    return;
                }

                model->reshape(ov::PartialShape{
                    1,
                    image_channel_,
                    input_height_,
                    input_width_
                });

                ov::CompiledModel compiled_model;
                if (device_id == 0)
                    compiled_model = core.compile_model(model, "CPU");
                else if (device_id == 1)
                    compiled_model = core.compile_model(model, "GPU");
                else
                    compiled_model = core.compile_model(model, "AUTO");

                auto input_port = compiled_model.input();
                auto output_port = compiled_model.output();

                ov::PartialShape compiled_input_shape = input_port.get_partial_shape();
                if (compiled_input_shape.rank().get_length() > 3)
                {
                    if (!compiled_input_shape[1].is_dynamic())
                        input_channel_ = static_cast<int>(compiled_input_shape[1].get_length());
                    if (!compiled_input_shape[2].is_dynamic())
                        input_height_ = static_cast<int>(compiled_input_shape[2].get_length());
                    if (!compiled_input_shape[3].is_dynamic())
                        input_width_ = static_cast<int>(compiled_input_shape[3].get_length());
                }
                else
                {
                    input_channel_ = image_channel_;
                }

                ov::InferRequest infer_request = compiled_model.create_infer_request();
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
                notify_startup(true);

                Job fetch_job;
                while (get_job_and_wait(fetch_job))
                {
                    auto& job = fetch_job;
                    try
                    {
                        auto& mono = job.mono_tensor->data();
                        ov::Tensor input(ov::element::f32, mono->get_shape());
                        std::memcpy(input.data<float>(), mono->data<float>(), mono->get_byte_size());
                        job.mono_tensor->release();

                        infer_request.set_tensor(input_port, input);
                        infer_request.infer();
                        infer_request.wait();

                        const ov::Tensor& output_tensor = infer_request.get_tensor(output_port);
                        ov::Shape output_shape = output_tensor.get_shape();
                        if (output_shape.size() < 4)
                        {
                            INFOE("Unexpected anomaly_map output rank.");
                            job.pro->set_value(BoxArray());
                            continue;
                        }

#pragma warning(push)
#pragma warning(disable: 4996)
                        const float* pScoreMap = output_tensor.data<float>();
#pragma warning(pop)

                        int map_h = static_cast<int>(output_shape[2]);
                        int map_w = static_cast<int>(output_shape[3]);
                        auto& based_prob = job.output;

                        cv::Mat restored = RestoreScoreMap(
                            pScoreMap, map_w, map_h, job.additional.affine.d2i, job.additional.originalSize);
                        if (!restored.empty())
                            based_prob.emplace_back(BuildResult(restored, seg_threshold_, score_top_ratio_));

                        job.pro->set_value(based_prob);
                    }
                    catch (const std::exception& e)
                    {
                        INFOE("OpenVINO anomaly detection inference exception: %s", e.what());
                        job.pro->set_value(BoxArray());
                    }
                    catch (...)
                    {
                        INFOE("OpenVINO anomaly detection inference caught an unknown exception.");
                        job.pro->set_value(BoxArray());
                    }
                }

                tensor_allocator_.reset();
                INFO("Engine destroy.");
            }
            catch (const std::exception& e)
            {
                INFOE("OpenVINO anomaly detection startup exception: %s", e.what());
                notify_startup(false);
                tensor_allocator_.reset();
            }
            catch (...)
            {
                INFOE("OpenVINO anomaly detection startup caught an unknown exception.");
                notify_startup(false);
                tensor_allocator_.reset();
            }
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

            auto& tensor = job.mono_tensor->data();
            if (tensor == nullptr)
                tensor = std::make_shared<ov::Tensor>();

            cv::Size input_size(input_width_, input_height_);
            job.additional.affine.compute(image.size(), input_size);
            job.additional.originalSize = image.size();

            ov::Shape input_shape = { 1,
                                      static_cast<size_t>(image_channel_),
                                      static_cast<size_t>(input_height_),
                                      static_cast<size_t>(input_width_) };
            tensor = std::make_shared<ov::Tensor>(ov::element::f32, input_shape);

            cv::Mat affineImage;
            cv::Mat affineMatrix = job.additional.affine.i2d_mat();
            OVPreprocess::warpAffineAndNormalize(
                image, affineImage, input_width_, input_height_, affineMatrix, mean_, std_, fill_value_);

            std::memcpy(tensor->data<float>(), affineImage.data, affineImage.total() * affineImage.elemSize());
            return true;
        }

        virtual std::vector<std::shared_future<BoxArray>> commits(const std::vector<std::pair<cv::Mat, cv::Mat>>& imagePairs) override
        {
            return OVControllerImpl::commits(imagePairs);
        }

        virtual std::shared_future<BoxArray> commit(const std::pair<cv::Mat, cv::Mat>& imagePair) override
        {
            return OVControllerImpl::commit(imagePair);
        }

    private:
        int input_width_ = 0;
        int input_height_ = 0;
        int input_channel_ = 0;
        int image_channel_ = 0;

        float fill_value_ = 114;
        float seg_threshold_ = 0.5f;
        float score_top_ratio_ = 0.01f;
        float mean_[3] = { 0.0f, 0.0f, 0.0f };
        float std_[3] = { 1.0f, 1.0f, 1.0f };
    };

    std::shared_ptr<OVInfer> create_ov_infer(std::shared_ptr<Param> param)
    {
        std::shared_ptr<OVInferImpl> instance(new OVInferImpl());
        if (!instance->startup(param))
            instance.reset();

        return instance;
    }
}
