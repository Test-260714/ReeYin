#include "pch.h"

#include "common_anomaly_detection.h"

#include "utils/ilogger.hpp"

#include <algorithm>
#include <cmath>
#include <cstring>


namespace AnomalyDetection
{
    namespace
    {
        const float kIdentityAffine[6] = { 1, 0, 0, 0, 1, 0 };

        bool ReadNormalizeValues(const Json::Value& values, int channel, bool rejectZero,
                                 std::vector<float>& out)
        {
            out.clear();
            for (Json::ArrayIndex i = 0; i < values.size(); i++)
            {
                float value = values[i].asFloat();
                if (rejectZero && value == 0)
                {
                    INFOE("normalize_std can not be zero");
                    return false;
                }
                out.push_back(value);
                if (static_cast<int>(out.size()) >= channel)
                    break;
            }

            if (out.size() < static_cast<size_t>(channel) && out.size() > 0)
            {
                while (out.size() < static_cast<size_t>(channel))
                    out.push_back(out[0]);
            }
            else if (out.size() == 0)
            {
                return false;
            }

            return true;
        }

        int ReplaceNonFinite(cv::Mat& scoreMap)
        {
            if (scoreMap.empty() || scoreMap.type() != CV_32FC1)
                return 0;

            cv::Mat continuous = scoreMap.isContinuous() ? scoreMap : scoreMap.clone();
            int replaced = 0;
            const int total = static_cast<int>(continuous.total());
            float* ptr = continuous.ptr<float>(0);
            for (int i = 0; i < total; ++i)
            {
                if (!std::isfinite(ptr[i]))
                {
                    ptr[i] = 0.0f;
                    ++replaced;
                }
            }

            if (!scoreMap.isContinuous() && replaced > 0)
                continuous.copyTo(scoreMap);

            return replaced;
        }
    }

    bool GetParam(std::string modelPath, std::shared_ptr<AnomalyDetection::Param>& param)
    {
        std::string configFile = iLogger::replace_extension(modelPath, ".json");

        Json::Value v;
        bool sta = file2json(configFile, v);
        if (!sta)
            return false;

        const Json::Value& p = v["param"];

        param->deviceId = p["device_id"].asInt();
        param->maxBatch = p["max_batch"].asInt();
        param->inputWidth = p["input_width"].asInt();
        param->inputHeight = p["input_height"].asInt();
        param->imageChannel = p["image_channel"].asInt();
        param->depthChannel = p["depth_channel"].asInt();

        if (param->imageChannel != 3 || param->depthChannel != 0)
        {
            INFOE("Dinomaly2 inference only supports RGB image_channel=3 and depth_channel=0.");
            return false;
        }

        param->enableClipGray = p["enable_gray_range"].asBool();
        param->clipGrayRangeMin = p["gray_range_min"].asFloat();
        param->clipGrayRangeMax = p["gray_range_max"].asFloat();
        param->fillValue = p["fill_value"].asFloat();
        param->onnxPath = modelPath;

        if (!ReadNormalizeValues(p["normalize_mean"], param->imageChannel, false, param->normalizeMean))
            return false;
        if (!ReadNormalizeValues(p["normalize_std"], param->imageChannel, true, param->normalizeStd))
            return false;

        param->splitImage = p["split_image"].asBool();
        param->accelerate = p["accelerate"].asFloat();
        param->overlapRate = p["overlap_rate"].asFloat();
        param->segThreshold = p["seg_thresh"].asFloat();
        param->scoreTopRatio = p.isMember("score_top_ratio") ? p["score_top_ratio"].asFloat() : 0.01f;

        if (param->inputWidth <= 0 || param->inputHeight <= 0)
        {
            INFOE("input_width and input_height must be positive.");
            return false;
        }
        if (param->maxBatch < 1)
        {
            INFOE("max_batch must be >= 1.");
            return false;
        }
        if (param->accelerate < 1)
        {
            INFOE("accelerate must be >= 1.");
            return false;
        }
        if (param->overlapRate < 0 || param->overlapRate >= 1)
        {
            INFOE("overlap_rate must be in [0, 1).");
            return false;
        }

        return true;
    }

    cv::Mat RestoreScoreMap(const float* scoreMap, int mapWidth, int mapHeight,
                            const float* affineMatrix, const cv::Size& outputSize)
    {
        if (scoreMap == nullptr || mapWidth <= 0 || mapHeight <= 0 ||
            outputSize.width <= 0 || outputSize.height <= 0)
        {
            return cv::Mat();
        }

        cv::Mat modelMap(mapHeight, mapWidth, CV_32FC1, const_cast<float*>(scoreMap));
        cv::Mat affine(2, 3, CV_32F, const_cast<float*>(affineMatrix));
        cv::Mat restored;
        cv::warpAffine(modelMap, restored, affine, outputSize, cv::INTER_LINEAR);
        return restored;
    }

    float ComputeImageScore(const cv::Mat& scoreMap, float topRatio)
    {
        if (scoreMap.empty())
            return 0;

        cv::Mat continuous;
        if (scoreMap.type() != CV_32FC1)
            scoreMap.convertTo(continuous, CV_32FC1);
        else if (!scoreMap.isContinuous())
            continuous = scoreMap.clone();
        else
            continuous = scoreMap;

        const int total = static_cast<int>(continuous.total());
        if (total <= 0)
            return 0;

        const float* ptr = continuous.ptr<float>(0);

        std::vector<float> values;
        values.reserve(total);
        for (int i = 0; i < total; ++i)
        {
            if (std::isfinite(ptr[i]))
                values.push_back(ptr[i]);
        }

        const int finiteTotal = static_cast<int>(values.size());
        if (finiteTotal <= 0)
            return 0;

        if (topRatio <= 0)
            return *std::max_element(values.begin(), values.end());

        int k = std::max(1, static_cast<int>(finiteTotal * topRatio));
        k = std::min(k, finiteTotal);

        auto firstTop = values.begin() + (finiteTotal - k);
        std::nth_element(values.begin(), firstTop, values.end());

        double sum = 0;
        for (auto it = firstTop; it != values.end(); ++it)
            sum += *it;

        return static_cast<float>(sum / k);
    }

    Result BuildResult(const cv::Mat& scoreMap, float segThreshold, float scoreTopRatio)
    {
        cv::Mat score;
        if (scoreMap.type() != CV_32FC1)
            scoreMap.convertTo(score, CV_32FC1);
        else
            score = scoreMap.clone();

        int replaced = ReplaceNonFinite(score);
        if (replaced > 0)
            INFOW("Anomaly score map contains %d non-finite values, replaced with 0.", replaced);

        Result r;
        r.class_id = 0;
        r.class_name = "anomaly";
        r.confidence = ComputeImageScore(score, scoreTopRatio);
        r.segmentation = Seg(score.ptr<float>(), score.cols, score.rows, kIdentityAffine, segThreshold);
        return r;
    }

    int MergeScorePatches(std::vector<std::shared_future<BoxArray>> resultPtrs,
                          const std::vector<cv::Rect>& patchRois,
                          const cv::Size& outputSize,
                          float segThreshold,
                          float scoreTopRatio,
                          BoxArray& result)
    {
        result.clear();
        if (outputSize.width <= 0 || outputSize.height <= 0)
            return -1;

        cv::Mat sumMap = cv::Mat::zeros(outputSize, CV_32FC1);
        cv::Mat weightMap = cv::Mat::zeros(outputSize, CV_32FC1);

        for (int i = 0; i < resultPtrs.size(); i++)
        {
            BoxArray boxes = resultPtrs[i].get();
            if (boxes.empty() || boxes[0].segmentation.floatData == nullptr)
                continue;

            cv::Rect roi = patchRois[i] & cv::Rect(0, 0, outputSize.width, outputSize.height);
            if (roi.empty())
                continue;

            const Seg& seg = boxes[0].segmentation;
            cv::Mat patchMap(seg.height, seg.width, CV_32FC1, seg.floatData);
            if (patchMap.size() != roi.size())
                cv::resize(patchMap, patchMap, roi.size(), 0, 0, cv::INTER_LINEAR);

            cv::Mat dstSum = sumMap(roi);
            cv::Mat dstWeight = weightMap(roi);
            dstSum += patchMap;
            dstWeight += 1.0f;
        }

        cv::Mat merged = cv::Mat::zeros(outputSize, CV_32FC1);
        cv::divide(sumMap, weightMap, merged, 1.0, CV_32F);
        merged.setTo(0, weightMap <= 0);

        result.emplace_back(BuildResult(merged, segThreshold, scoreTopRatio));
        return 0;
    }

    int LoadResult(std::vector<Result> bboxes, Result** objInfo, int& num)
    {
        if (objInfo == nullptr)
        {
            INFOE("LoadResult: output pointer objInfo is nullptr");
            return -1;
        }

        num = static_cast<int>(bboxes.size());
        if (num == 0)
        {
            *objInfo = nullptr;
            return 0;
        }

        Result* arr = new Result[num];
        for (int i = 0; i < num; i++)
            arr[i] = bboxes[i];

        *objInfo = arr;
        return 0;
    }
}
