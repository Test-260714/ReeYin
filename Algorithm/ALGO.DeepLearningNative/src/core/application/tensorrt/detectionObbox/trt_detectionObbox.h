#pragma once

#include "interface/interface.h"

#include "common/detectionObbox/common_detectionObbox.h"

#include "tensorrt/builder/trt_builder.hpp"
#include "tensorrt/infer/trt_infer.hpp"
#include "tensorrt/common/cuda_tools.hpp"

#include <opencv2/opencv.hpp>

#include <vector>
#include <string>
#include <future>

namespace DetectionObbox
{
	enum NMSMethod
	{
		CPU = 0,
		FastGPU = 1
	};

	class TRTInfer
	{
	public:
		virtual std::shared_future<BoxArray> commit(const std::pair<cv::Mat, cv::Mat>& imagePair) = 0;
		virtual std::vector<std::shared_future<BoxArray>> commits(const std::vector<std::pair<cv::Mat, cv::Mat>>& imageParis) = 0;
	};

	bool compile_trt_model(std::shared_ptr<Param> param, std::string& outModelFile, TRT::Mode mode = TRT::Mode::FP32);

	std::shared_ptr<TRTInfer> create_trt_infer(const std::string& engineFile, std::shared_ptr<Param> param, bool useMultiPreprocessStream = false);


	void yolo_bbox_decode_kernel_invoker(float* predict, int num_bboxes, int num_classes, float confidence_threshold,
		float* invert_affine_matrix, float* parray, int max_objects, cudaStream_t stream);


	void nms_kernel_invoker(float* parray, float nms_threshold, int max_objects, cudaStream_t stream);

}
