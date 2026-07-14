#pragma once

#include "interface/interface.h"

#include "common/anomalyDetection/common_anomaly_detection.h"

#include "tensorrt/builder/trt_builder.hpp"
#include "tensorrt/common/cuda_tools.hpp"
#include "tensorrt/infer/trt_infer.hpp"

#include <opencv2/opencv.hpp>

#include <future>
#include <memory>
#include <string>
#include <vector>


namespace AnomalyDetection
{
    class TRTInfer
    {
    public:
        virtual std::shared_future<BoxArray> commit(const std::pair<cv::Mat, cv::Mat>& imagePair) = 0;
        virtual std::vector<std::shared_future<BoxArray>> commits(const std::vector<std::pair<cv::Mat, cv::Mat>>& imagePairs) = 0;
    };

    bool compile_trt_model(std::shared_ptr<Param> param, std::string& outModelFile, TRT::Mode mode = TRT::Mode::FP32);

    std::shared_ptr<TRTInfer> create_trt_infer(const std::string& engineFile, std::shared_ptr<Param> param,
                                               bool useMultiPreprocessStream = false);
}
