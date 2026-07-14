#include "pch.h"

#include "ov_app_detectionSeg.h"
#include "ov_detectionSeg.h"

#include "utils/json.hpp"
#include "utils/ilogger.hpp"


namespace DetectionSeg
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
            _ov_param->confidenceThreshold = modelConfig->confidence_threshold;
            _ov_param->nmsThreshold = modelConfig->IoU_threshold;
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

            cv::Mat image, depth;
            if (imageData != nullptr && _ov_param->imageChannel > 0)
            {
                image = cv::Mat(im_h, im_w, CV_MAKETYPE(im_type, im_c), imageData);
                if (image.channels() != _ov_param->imageChannel)
                {
                    cv::Mat temp;
                    if (image.channels() == 3 && _ov_param->imageChannel == 1)
                    {
                        cv::cvtColor(image, temp, cv::COLOR_BGR2GRAY);
                        image = temp;
                    }
                    else if (image.channels() == 1 && _ov_param->imageChannel == 3)
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
            if (depthData != nullptr && _ov_param->depthChannel > 0)
            {
                depth = cv::Mat(d_h, d_w, CV_MAKETYPE(d_type, d_c), depthData);
                depth.convertTo(depth, CV_32F);
            }

            BoxArray bboxes;

            if (_ov_param->splitImage)
            {
                int accelerate = _ov_param->accelerate;
                float overlapRate = _ov_param->overlapRate;

                std::vector<ImagePatch> patches;
                GetImagePatches(image, depth, _ov_param->inputWidth * accelerate, _ov_param->inputHeight * accelerate, overlapRate, patches);

                std::vector<std::pair<cv::Mat, cv::Mat>> imagePairs;
                std::vector<cv::Rect> patchRois;
                std::vector<cv::Rect> validRois;

                for (size_t i = 0; i < patches.size(); i++)
                {
                    imagePairs.push_back({ patches[i].image, patches[i].depth });
                    patchRois.push_back(patches[i].patchRoi);
                    validRois.push_back(patches[i].validRoi);
                }

                std::chrono::time_point<std::chrono::high_resolution_clock> m_start = std::chrono::high_resolution_clock::now();

                auto tmp_bboxes_ptr = _ov_net->commits(imagePairs);
                MergePatches(tmp_bboxes_ptr, patchRois, validRois, _ov_param->nmsThreshold, ModelType::MODEL_DETECTION_SEG, bboxes);

                auto t = std::chrono::duration_cast<std::chrono::microseconds>(std::chrono::high_resolution_clock::now() - m_start).count();

                INFO("Instance segmentation model inference time: %.3f ms", t * 0.001);
            }
            else
            {
                std::pair<cv::Mat, cv::Mat> imagePair = { image, depth };

                std::chrono::time_point<std::chrono::high_resolution_clock> m_start = std::chrono::high_resolution_clock::now();

                bboxes = _ov_net->commit(imagePair).get();

                auto t = std::chrono::duration_cast<std::chrono::microseconds>(std::chrono::high_resolution_clock::now() - m_start).count();

                INFO("Instance segmentation model inference time: %.3f ms", t * 0.001);
            }

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
