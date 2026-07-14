#include "pch.h"

#include "common_detectionBbox.h"

#include "utils/json.hpp"
#include "utils/ilogger.hpp"

#include <fstream>


namespace DetectionBbox
{
    bool GetParam(std::string modelPath, std::shared_ptr<DetectionBbox::Param>& param)
    {
        std::string configFile = iLogger::replace_extension(modelPath, ".json");

        Json::Value v;
        bool sta = file2json(configFile, v);
        if (sta)
        {
            param->SetDeviceId(v["param"]["device_id"].asInt());
            param->SetMaxBatch(v["param"]["max_batch"].asInt());
            param->SetInputWidth(v["param"]["input_width"].asInt());
            param->SetInputHeight(v["param"]["input_height"].asInt());
            param->SetImageChannel(v["param"]["image_channel"].asInt());
            param->SetDepthChannel(v["param"]["depth_channel"].asInt());

            param->SetEnableClipGray(v["param"]["enable_gray_range"].asBool());
            param->SetClipGrayRangeMin(v["param"]["gray_range_min"].asFloat());
            param->SetClipGrayRangeMax(v["param"]["gray_range_max"].asFloat());
            param->SetOnnxPath(modelPath);
            param->SetFillValue(v["param"]["fill_value"].asFloat());

            // normalize_mean
            std::vector<float> normalize_mean;
            Json::Value tmp_mean = v["param"]["normalize_mean"];
            for (int i = 0; i < tmp_mean.size(); i++)
            {
                float value = tmp_mean[i].asFloat();
                normalize_mean.push_back(value);
                if (i > param->imageChannel)
                    break;
            }
            if (param->imageChannel > normalize_mean.size() && normalize_mean.size() > 0)
            {
                while (normalize_mean.size() < param->imageChannel)
                {
                    normalize_mean.push_back(normalize_mean[0]);
                }
            }
            else if (normalize_mean.size() == 0)
            {
                return false;
            }
            param->SetNormalizeMean(normalize_mean);

            // normalize_std
            std::vector<float> normalize_std;
            Json::Value tmp_std = v["param"]["normalize_std"];
            for (int i = 0; i < tmp_std.size(); i++)
            {
                float value = tmp_std[i].asFloat();
                if (value == 0)
                {
                    INFOE("normalize_std can not be zero");
                    return false;
                }
                normalize_std.push_back(value);
                if (i > param->imageChannel)
                    break;
            }
            if (param->imageChannel > normalize_std.size() && normalize_std.size() > 0)
            {
                while (normalize_std.size() < param->imageChannel)
                {
                    normalize_std.push_back(normalize_std[0]);
                }
            }
            else if (normalize_std.size() == 0)
            {
                return false;
            }
            param->SetNormalizeStd(normalize_std);

            param->SetConfidenceThreshold(v["param"]["conf_thresh"].asFloat());
            param->SetNmsThreshold(v["param"]["nms_thresh"].asFloat());
            param->SetSplitImage(v["param"]["split_image"].asBool());
            param->SetAccelerate(v["param"]["accelerate"].asFloat());
            param->SetoverlapRate(v["param"]["overlap_rate"].asFloat());
            param->SetMaxObjects(v["param"]["max_objects"].asInt());
            param->SetUseYoloDecode(v["param"]["use_yolo_decode"].asBool());

            int detNumClasses = v["param"]["categories"].size();
            param->SetNumClasses(detNumClasses);

            // 解析类别信息
            std::vector<std::string> categories;
            Json::Value categoriesArray = v["param"]["categories"];
            for (int i = 0; i < detNumClasses; i++)
            {
                auto category = categoriesArray[i];
                categories.push_back(category["name"].asString());
            }
            param->SetCategories(categories);
        }
        else
            return false;

        return true;
    }

    int LoadResult(std::vector<Result> bboxes, Result** objInfo, int& num)
    {
        if (objInfo == nullptr)
        {
            INFOE("LoadResult: output pointer kptInfo is nullptr");
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
        {
            arr[i] = bboxes[i];
        }
        *objInfo = arr;

        return 0;
    }

}


