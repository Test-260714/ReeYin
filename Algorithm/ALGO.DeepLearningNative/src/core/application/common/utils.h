#pragma once

#include <vector>
#include <future>

#include <opencv2/opencv.hpp>

#include "interface/interface.h"

#include "utils/json.hpp"


struct ImagePatch
{
    cv::Rect patchRoi;
    cv::Rect validRoi;
    cv::Mat image;
    cv::Mat depth;
};


typedef std::vector<Result> BoxArray;


bool file2json(const std::string& file, Json::Value& v);


BoxArray cpu_nms(BoxArray& boxes, float threshold);


BoxArray cpu_obb_nms(BoxArray& boxes, float threshold);


int GetImagePatches(const cv::Mat& image, const cv::Mat& depth, int patchWidth, int patchHeight, float overlapRate, std::vector<ImagePatch>& patches);


int MergePatches(std::vector<std::shared_future<BoxArray>> insPtrs, std::vector<cv::Rect> patchRois,
                 std::vector<cv::Rect> validRois, float nmsThresh, ModelType modelType, BoxArray& result);

