#include "pch.h"

#include "trt_detectionKpt.h"
#include "trt_app_detectionKpt.h"

#include "utils/json.hpp"
#include "utils/ilogger.hpp"

namespace DetectionKpt
{
    bool ConfigModel(std::shared_ptr<Param> param, std::shared_ptr<TRTInfer>& net)
    {
        TRT::set_device(param->deviceId);

        // 模型序列化
        std::string trtEngineFile;
        bool sta = compile_trt_model(param, trtEngineFile, TRT::Mode::FP16);

        if (!sta)
        {
            INFOE("Failed to compile model trt engine file.");
            return false;
        }

        net = create_trt_infer(trtEngineFile, param);

        if (net == nullptr)
        {
            INFOE("Engine is nullptr");
            return false;
        }

        return true;
    }


    int TRTModel::InitRuntime(const ModelConfig* modelConfig)
    {
        try
        {
            CleanUpRuntime();

            _trt_param = std::make_shared<Param>();

            bool sta = GetParam(modelConfig->model_path, _trt_param);
            if (!sta)
            {
                INFOE("Failed to parse the json file. Check whether the file path and field format are correct.");
                return -1;
            }

            _trt_param->maxBatch = modelConfig->batch_size;
            _trt_param->confidenceThreshold = modelConfig->confidence_threshold;
            _trt_param->nmsThreshold = modelConfig->IoU_threshold;
            _trt_param->kptThreshold = modelConfig->keypoint_threshold;

            sta = ConfigModel(_trt_param, _trt_net);
            if (!sta)
                return -1;

            return 0;
        }
        catch (const std::exception& e)
        {
            INFOE("InitRuntime caught an exception: %s", e.what());

            return -1;
        }
    }


    int TRTModel::CleanUpRuntime()
    {
        _trt_param.reset();
        _trt_net.reset();

        return 0;
    }


    int TRTModel::Pipeline(void* imageData, int im_w, int im_h, int im_c, int im_type,
                           void* depthData, int d_w, int d_h, int d_c, int d_type,
                           Result** objInfo, int& objectNum)
    {
        try
        {
            if (*objInfo != nullptr)
            {
                CleanUpResult(*objInfo);
                *objInfo = nullptr;
            }

            if (_trt_param == nullptr || _trt_net == nullptr)
            {
                INFOE("Runtime is not initialized.");
                return -1;
            }

            cv::Mat image, depth;
            if (imageData != nullptr && _trt_param->imageChannel > 0)
            {
                image = cv::Mat(im_h, im_w, CV_MAKETYPE(im_type, im_c), imageData);
                if (image.channels() != _trt_param->imageChannel)
                {
                    cv::Mat temp;
                    if (image.channels() == 3 && _trt_param->imageChannel == 1)
                    {
                        cv::cvtColor(image, temp, cv::COLOR_BGR2GRAY);
                        image = temp;
                    }
                    else if (image.channels() == 1 && _trt_param->imageChannel == 3)
                    {
                        cv::cvtColor(image, temp, cv::COLOR_GRAY2BGR);
                        image = temp;
                    }
                    else
                    {
                        throw std::runtime_error("Unsupported channel conversion");
                    }
                }
                image.convertTo(image, CV_32F);
            }
            if (depthData != nullptr && _trt_param->depthChannel > 0)
            {
                depth = cv::Mat(d_h, d_w, CV_MAKETYPE(d_type, d_c), depthData);
                depth.convertTo(depth, CV_32F);
            }

            std::pair<cv::Mat, cv::Mat> imagePair = { image, depth };


            std::chrono::time_point<std::chrono::high_resolution_clock> m_start = std::chrono::high_resolution_clock::now();

            auto bboxes = _trt_net->commit(imagePair).get();

            auto t = std::chrono::duration_cast<std::chrono::microseconds>(std::chrono::high_resolution_clock::now() - m_start).count();

            INFO("Keypoint detection model inference time: %.3f ms", t * 0.001);

            int ret = LoadResult(bboxes, objInfo, objectNum);
            if (ret != 0)
            {
                objectNum = 0;
                if (*objInfo != nullptr)
                {
                    CleanUpResult(*objInfo);
                    *objInfo = nullptr;
                }
                return -1;
            }

            return 0;
        }
        catch (const std::exception& e)
        {
            INFOE("Pipeline catches an exception: %s", e.what());

            return -1;
        }

        return 0;
    }


    int TRTModel::CleanUpResult(Result*& objInfo)
    {
        if (objInfo != nullptr)
        {
            delete[] objInfo;
            objInfo = nullptr;
        }

        return 0;
    }
}







