#pragma once

#include "interface/interface.h"

#include "common/anomalyDetection/common_anomaly_detection.h"

#include <opencv2/opencv.hpp>

#include <future>
#include <memory>
#include <string>
#include <vector>


namespace AnomalyDetection
{
    class OVInfer
    {
    public:
        virtual std::shared_future<BoxArray> commit(const std::pair<cv::Mat, cv::Mat>& imagePair) = 0;
        virtual std::vector<std::shared_future<BoxArray>> commits(const std::vector<std::pair<cv::Mat, cv::Mat>>& imagePairs) = 0;
    };

    std::shared_ptr<OVInfer> create_ov_infer(std::shared_ptr<Param> param);
}
