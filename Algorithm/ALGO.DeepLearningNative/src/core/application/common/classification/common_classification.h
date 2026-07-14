#pragma once

#include "interface/interface.h"
#include "common/utils.h"

#include <opencv2/opencv.hpp>

#include <vector>
#include <string>
#include <future>


namespace Classification
{
    struct Param
    {
        int deviceId = 0;
        int maxBatch = 8;
        int inputWidth = 640;
        int inputHeight = 640;
        int imageChannel = 3;
        int depthChannel = 0;

        bool enableClipGray = false;
        float clipGrayRangeMin = 0;
        float clipGrayRangeMax = 0;

        float fillValue = 0;

        std::string onnxPath = "";

        std::vector<float> normalizeMean;
        std::vector<float> normalizeStd;

        int numClasses = 0;
        std::vector<std::string> categories;

        void SetDeviceId(int value) { deviceId = value; }
        int GetDeviceId() { return deviceId; }

        void SetMaxBatch(int value) { maxBatch = value; }
        int GetMaxBatch() { return maxBatch; }

        void SetInputWidth(int value) { inputWidth = value; }
        int GetInputWidth() { return inputWidth; }
        void SetInputHeight(int value) { inputHeight = value; }
        int GetInputHeight() { return inputHeight; }
        void SetImageChannel(int value) { imageChannel = value; }
        int GetImageChannel() { return imageChannel; }
        void SetDepthChannel(int value) { depthChannel = value; }
        int GetDepthChannel() { return depthChannel; }

        void SetEnableClipGray(bool value) { enableClipGray = value; }
        bool GetEnableClipGray() { return enableClipGray; }
        void SetClipGrayRangeMin(float value) { clipGrayRangeMin = value; }
        float GetClipGrayRangeMin() { return clipGrayRangeMin; }
        void SetClipGrayRangeMax(float value) { clipGrayRangeMax = value; }
        float GetClipGrayRangeMax() { return clipGrayRangeMax; }

        void SetFillValue(float value) { fillValue = value; }
        float GetFillValue() { return fillValue; }

        void SetOnnxPath(std::string value) { onnxPath = value; }
        std::string GetOnnxPath() { return onnxPath; }

        void SetNormalizeMean(std::vector<float> value) { normalizeMean = value; }
        std::vector<float> GetNormalizeMean() { return normalizeMean; }
        void SetNormalizeStd(std::vector<float> value) { normalizeStd = value; }
        std::vector<float> GetNormalizeStd() { return normalizeStd; }

        void SetNumClasses(int value) { numClasses = value; }
        int GetNumClasses() { return numClasses; }

        void SetCategories(std::vector<std::string> value) { categories = value; }
        std::vector<std::string> GetCategories() { return categories; }
    };


    struct AffineMatrix
    {
        float i2d[6];       // 原图到模型输入变换矩阵, 2x3 matrix
        float d2i[6];       // 模型输入到原图变化矩阵, 2x3 matrix

        void compute(const cv::Size& from, const cv::Size& to)
        {
            float scale_x = to.width / (float)from.width;
            float scale_y = to.height / (float)from.height;
            float scale = std::min(scale_x, scale_y);

            i2d[0] = scale;  i2d[1] = 0;  i2d[2] = -scale * from.width * 0.5 + to.width * 0.5 + scale * 0.5 - 0.5;
            i2d[3] = 0;  i2d[4] = scale;  i2d[5] = -scale * from.height * 0.5 + to.height * 0.5 + scale * 0.5 - 0.5;

            cv::Mat m2x3_i2d(2, 3, CV_32F, i2d);
            cv::Mat m2x3_d2i(2, 3, CV_32F, d2i);
            cv::invertAffineTransform(m2x3_i2d, m2x3_d2i);
        }

        cv::Mat i2d_mat()
        {
            return cv::Mat(2, 3, CV_32F, i2d);
        }
    };


    bool GetParam(std::string configFile, std::shared_ptr<Classification::Param>& param);

    int LoadResult(std::vector<Result> bboxes, Result** objInfo, int& num);

}



