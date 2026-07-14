#pragma once

#include "opencv2/opencv.hpp"

namespace OVPreprocess
{
	cv::Mat clip_gray_value(const cv::Mat& input, float min_clip, float max_clip, float& min_val, float& max_val);

	cv::Mat clip_gray_value_with_mask(const cv::Mat& input, float min_clip, float max_clip, float& min_val, float& max_val, cv::Mat& mask);

	cv::Mat normalizeByCV(const cv::Mat& img, const float norn_mean[3], const float norn_std[3]);

	void warpAffineAndNormalize(cv::Mat srcImg, cv::Mat& dstImg, int dstW, int dstH, cv::Mat affineMatrix,
		                        float norn_mean[3], float norn_std[3], float fill_value);

	void warpAffineAndNormalizeImageMixDepth(cv::Mat srcImg, cv::Mat srcDepth, cv::Mat& dstImg, int dstW, int dstH, cv::Mat affineMatrix,
		                                     float norn_mean[3], float norn_std[3], float fill_value);

	void warpAffineAndNormalizeDepthMixMask(cv::Mat srcDepth, cv::Mat srcMask, cv::Mat& dstImg, int dstW, int dstH, cv::Mat affineMatrix,
		                                    float norn_mean[3], float norn_std[3], float fill_value);

	void warpAffineAndNormalizeImageMixDepthMixDepth(cv::Mat srcImg, cv::Mat srcDepth, cv::Mat srcMask, cv::Mat& dstImg, int dstW, int dstH, 
		                                             cv::Mat affineMatrix, float norn_mean[3], float norn_std[3], float fill_value);
}


