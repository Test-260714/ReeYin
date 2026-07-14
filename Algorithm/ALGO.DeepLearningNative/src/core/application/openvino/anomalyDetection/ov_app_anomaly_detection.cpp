#include "pch.h"

#include "ov_app_anomaly_detection.h"
#include "ov_anomaly_detection.h"

#include "utils/ilogger.hpp"


namespace AnomalyDetection
{
    bool ConfigModel(std::shared_ptr<Param> param, std::shared_ptr<OVInfer>& net)
    {
        net = create_ov_infer(param);
        if (net == nullptr)
        {
            INFOE("Engine is nullptr");
            return false;
        }

        return true;
    }

    int OVModel::InitRuntime(const ModelConfig* modelConfig)
    {
        try
        {
            CleanUpRuntime();

            _ov_param = std::make_shared<Param>();
            bool sta = GetParam(modelConfig->model_path, _ov_param);
            if (!sta)
            {
                INFOE("Failed to parse the json file. Check whether the file path and field format are correct.");
                return -1;
            }

            _ov_param->maxBatch = modelConfig->batch_size;
            _ov_param->segThreshold = modelConfig->segmentation_threshold;  

            sta = ConfigModel(_ov_param, _ov_net);
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

    int OVModel::CleanUpRuntime()
    {
        _ov_param.reset();
        _ov_net.reset();
        return 0;
    }

    int OVModel::Pipeline(void* imageData, int im_w, int im_h, int im_c, int im_type,
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

            if (_ov_param == nullptr || _ov_net == nullptr)
            {
                INFOE("Runtime is not initialized.");
                return -1;
            }

            cv::Mat image;
            if (imageData != nullptr && _ov_param->imageChannel > 0)
            {
                image = cv::Mat(im_h, im_w, CV_MAKETYPE(im_type, im_c), imageData);
                if (image.channels() != _ov_param->imageChannel)
                {
                    cv::Mat temp;
                    if (image.channels() == 1 && _ov_param->imageChannel == 3)
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

            if (image.empty())
            {
                INFOE("Image is empty.");
                return -1;
            }

            BoxArray results;
            if (_ov_param->splitImage)
            {
                int patchWidth = std::max(1, static_cast<int>(std::round(_ov_param->inputWidth * _ov_param->accelerate)));
                int patchHeight = std::max(1, static_cast<int>(std::round(_ov_param->inputHeight * _ov_param->accelerate)));

                std::vector<ImagePatch> patches;
                GetImagePatches(image, cv::Mat(), patchWidth, patchHeight, _ov_param->overlapRate, patches);

                std::vector<std::pair<cv::Mat, cv::Mat>> imagePairs;
                std::vector<cv::Rect> patchRois;
                imagePairs.reserve(patches.size());
                patchRois.reserve(patches.size());

                for (size_t i = 0; i < patches.size(); i++)
                {
                    imagePairs.push_back({ patches[i].image, cv::Mat() });
                    patchRois.push_back(patches[i].patchRoi);
                }

                auto m_start = std::chrono::high_resolution_clock::now();
                auto tmpResults = _ov_net->commits(imagePairs);
                MergeScorePatches(tmpResults, patchRois, image.size(), _ov_param->segThreshold,
                                  _ov_param->scoreTopRatio, results);
                auto t = std::chrono::duration_cast<std::chrono::microseconds>(
                    std::chrono::high_resolution_clock::now() - m_start).count();
                INFO("Anomaly detection model inference time: %.3f ms", t * 0.001);
            }
            else
            {
                std::pair<cv::Mat, cv::Mat> imagePair = { image, cv::Mat() };
                auto m_start = std::chrono::high_resolution_clock::now();
                results = _ov_net->commit(imagePair).get();
                auto t = std::chrono::duration_cast<std::chrono::microseconds>(
                    std::chrono::high_resolution_clock::now() - m_start).count();
                INFO("Anomaly detection model inference time: %.3f ms", t * 0.001);
            }

            int ret = LoadResult(results, objInfo, objectNum);
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
    }

    int OVModel::CleanUpResult(Result*& objInfo)
    {
        if (objInfo != nullptr)
        {
            delete[] objInfo;
            objInfo = nullptr;
        }
        return 0;
    }
}
