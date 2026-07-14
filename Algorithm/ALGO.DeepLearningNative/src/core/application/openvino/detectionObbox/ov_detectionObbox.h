#pragma once

#include "interface/interface.h"

#include "common/detectionObbox/common_detectionObbox.h"

#include <opencv2/opencv.hpp>

#include <vector>
#include <string>
#include <future>

namespace DetectionObbox
{

	class OVInfer
	{
	public:
		virtual std::shared_future<BoxArray> commit(const std::pair<cv::Mat, cv::Mat>& imagePair) = 0;
		virtual std::vector<std::shared_future<BoxArray>> commits(const std::vector<std::pair<cv::Mat, cv::Mat>>& imageParis) = 0;
	};


	std::shared_ptr<OVInfer> create_ov_infer(std::shared_ptr<Param> param);

}
