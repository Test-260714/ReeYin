#pragma once

#include "interface/interface.h"

#include "common/segmentation/common_segmentation.h"

#include "tensorrt/builder/trt_builder.hpp"
#include "tensorrt/infer/trt_infer.hpp"
#include "tensorrt/common/cuda_tools.hpp"

#include <opencv2/opencv.hpp>

#include <vector>
#include <string>
#include <future>


namespace Segmentation
{
	

	class TRTInfer
	{
	public:
		virtual std::shared_future<BoxArray> commit(const std::pair<cv::Mat, cv::Mat>& imagePair) = 0;
		virtual std::vector<std::shared_future<BoxArray>> commits(const std::vector<std::pair<cv::Mat, cv::Mat>>& imageParis) = 0;
	};

	bool compile_trt_model(std::shared_ptr<Param> param, std::string& outModelFile, TRT::Mode mode = TRT::Mode::FP32);

	std::shared_ptr<TRTInfer> create_trt_infer(const std::string& engineFile, std::shared_ptr<Param> param, bool useMultiPreprocessStream = false);

	// 将模型的输出掩码缩放到原图尺寸
	void warp_affine_nearest_neighbor_general(int* src, int src_line_size, int src_width, int src_height,
		                                      int* dst, int dst_width, int dst_height, float* matrix_2_3, cudaStream_t stream);

}

