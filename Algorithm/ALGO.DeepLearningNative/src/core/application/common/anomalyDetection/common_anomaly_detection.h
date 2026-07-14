#pragma once

#include "interface/interface.h"
#include "common/utils.h"

#include <opencv2/opencv.hpp>

#include <future>
#include <memory>
#include <string>
#include <vector>


namespace AnomalyDetection
{
    struct Param
    {
        int deviceId = 0;
        int maxBatch = 1;
        int inputWidth = 448;
        int inputHeight = 448;
        int imageChannel = 3;
        int depthChannel = 0;

        bool enableClipGray = false;
        float clipGrayRangeMin = 0;
        float clipGrayRangeMax = 0;

        float fillValue = 114;
        std::string onnxPath;

        std::vector<float> normalizeMean;
        std::vector<float> normalizeStd;

        bool splitImage = false;
        float accelerate = 1;
        float overlapRate = 0;

        float segThreshold = 0.5f;
        float scoreTopRatio = 0.01f;
    };

    struct AffineMatrix
    {
        float i2d[6];
        float d2i[6];

        void compute(const cv::Size& from, const cv::Size& to)
        {
            float scale_x = to.width / static_cast<float>(from.width);
            float scale_y = to.height / static_cast<float>(from.height);
            float scale = std::min(scale_x, scale_y);

            i2d[0] = scale;
            i2d[1] = 0;
            i2d[2] = -scale * from.width * 0.5f + to.width * 0.5f + scale * 0.5f - 0.5f;
            i2d[3] = 0;
            i2d[4] = scale;
            i2d[5] = -scale * from.height * 0.5f + to.height * 0.5f + scale * 0.5f - 0.5f;

            cv::Mat m2x3_i2d(2, 3, CV_32F, i2d);
            cv::Mat m2x3_d2i(2, 3, CV_32F, d2i);
            cv::invertAffineTransform(m2x3_i2d, m2x3_d2i);
        }

        cv::Mat i2d_mat()
        {
            return cv::Mat(2, 3, CV_32F, i2d);
        }

        cv::Mat d2i_mat()
        {
            return cv::Mat(2, 3, CV_32F, d2i);
        }
    };

    struct InferenceMeta
    {
        AffineMatrix affine;
        cv::Size originalSize;
    };

    bool GetParam(std::string modelPath, std::shared_ptr<AnomalyDetection::Param>& param);

    cv::Mat RestoreScoreMap(const float* scoreMap, int mapWidth, int mapHeight,
                            const float* affineMatrix, const cv::Size& outputSize);

    float ComputeImageScore(const cv::Mat& scoreMap, float topRatio);

    Result BuildResult(const cv::Mat& scoreMap, float segThreshold, float scoreTopRatio);

    int MergeScorePatches(std::vector<std::shared_future<BoxArray>> resultPtrs,
                          const std::vector<cv::Rect>& patchRois,
                          const cv::Size& outputSize,
                          float segThreshold,
                          float scoreTopRatio,
                          BoxArray& result);

    int LoadResult(std::vector<Result> bboxes, Result** objInfo, int& num);
}
