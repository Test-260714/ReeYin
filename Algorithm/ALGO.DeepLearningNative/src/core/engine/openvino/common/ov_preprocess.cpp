#include "pch.h"

#include "ov_preprocess.h"



namespace OVPreprocess 
{ 
    cv::Mat clip_gray_value(const cv::Mat& input, float min_clip, float max_clip, float& min_val, float& max_val)
    {
        // Step 1: 创建有效像素掩码
        cv::Mat mask_ge, mask_le, finite_mask, final_mask;
        cv::compare(input, min_clip, mask_ge, cv::CMP_GE);  // >= min_clip
        cv::compare(input, max_clip, mask_le, cv::CMP_LE);  // <= max_clip
        cv::compare(input, input, finite_mask, cv::CMP_EQ); // finite 像素满足 x == x
        cv::bitwise_and(mask_ge, mask_le, final_mask);      // 范围交集
        cv::bitwise_and(final_mask, finite_mask, final_mask);

        if (cv::countNonZero(final_mask) == 0)
        {
            min_val = 0.0f;
            max_val = 0.0f;
            return cv::Mat::zeros(input.size(), CV_32F);
        }

        cv::Mat invalid_mask;
        cv::bitwise_not(final_mask, invalid_mask);

        // Step 2: 计算有效像素极值
        cv::Mat temp_min, temp_max;
        input.copyTo(temp_min);
        input.copyTo(temp_max);

        // 无效像素设为极值以排除影响
        temp_min.setTo(std::numeric_limits<float>::max(), invalid_mask);
        temp_max.setTo(std::numeric_limits<float>::lowest(), invalid_mask);

        double tmp_min_val, tmp_max_val;
        cv::minMaxLoc(temp_min, &tmp_min_val, nullptr, nullptr, nullptr);
        cv::minMaxLoc(temp_max, nullptr, &tmp_max_val, nullptr, nullptr);

        min_val = static_cast<float>(tmp_min_val);
        max_val = static_cast<float>(tmp_max_val);

        // Step 3: 调整像素值
        cv::Mat adjusted;
        input.copyTo(adjusted);
        adjusted.setTo(tmp_min_val, invalid_mask);

        // Step 4: 归一化处理
        cv::Mat normalized;
        float range = tmp_max_val - tmp_min_val;
        if (range > 0)
        {
            adjusted.convertTo(normalized, CV_32F, 1.0 / range, -tmp_min_val / range);
        }
        else
        {
            normalized = cv::Mat::zeros(input.size(), CV_32F);
        }

        return normalized;
    }
    cv::Mat clip_gray_value_with_mask(const cv::Mat& input, float min_clip, float max_clip, float& min_val, float& max_val, cv::Mat& mask)
    {
        // Step 1: 创建有效像素掩码
        cv::Mat mask_ge, mask_le, finite_mask, final_mask;
        cv::compare(input, min_clip, mask_ge, cv::CMP_GE);  // >= min_clip
        cv::compare(input, max_clip, mask_le, cv::CMP_LE);  // <= max_clip
        cv::compare(input, input, finite_mask, cv::CMP_EQ); // finite 像素满足 x == x
        cv::bitwise_and(mask_ge, mask_le, final_mask);      // 范围交集
        cv::bitwise_and(final_mask, finite_mask, final_mask);

        if (cv::countNonZero(final_mask) == 0)
        {
            min_val = 0.0f;
            max_val = 0.0f;
            mask = cv::Mat::zeros(input.size(), CV_32F);
            return cv::Mat::zeros(input.size(), CV_32F);
        }

        cv::Mat invalid_mask;
        cv::bitwise_not(final_mask, invalid_mask);

        // Step 2: 计算有效像素极值
        cv::Mat temp_min, temp_max;
        input.copyTo(temp_min);
        input.copyTo(temp_max);

        // 无效像素设为极值以排除影响
        temp_min.setTo(std::numeric_limits<float>::max(), invalid_mask);
        temp_max.setTo(std::numeric_limits<float>::lowest(), invalid_mask);

        double tmp_min_val, tmp_max_val;
        cv::minMaxLoc(temp_min, &tmp_min_val, nullptr, nullptr, nullptr);
        cv::minMaxLoc(temp_max, nullptr, &tmp_max_val, nullptr, nullptr);

        min_val = static_cast<float>(tmp_min_val);
        max_val = static_cast<float>(tmp_max_val);

        // Step 3: 调整像素值
        cv::Mat adjusted;
        input.copyTo(adjusted);
        adjusted.setTo(tmp_min_val, invalid_mask);

        // Step 4: 归一化处理
        cv::Mat normalized;
        float range = tmp_max_val - tmp_min_val;
        if (range > 0)
        {
            adjusted.convertTo(normalized, CV_32F, 1.0 / range, -tmp_min_val / range);
        }
        else
        {
            normalized = cv::Mat::zeros(input.size(), CV_32F);
        }

        final_mask.convertTo(mask, CV_32F, 1.0 / 255.0);

        return normalized;
    }
    cv::Mat normalizeByCV(const cv::Mat& img, const float norn_mean[3], const float norn_std[3])
    {
        cv::Mat normalized;

        if (img.channels() == 3)
        {
            std::vector<cv::Mat> channels(3);
            cv::split(img, channels);

            for (int c = 0; c < 3; c++)
            {
                channels[c] = (channels[c] - norn_mean[c]) / norn_std[c];
            }

            cv::merge(channels, normalized);
        }
        else
        {
            normalized = (img - norn_mean[0]) / norn_std[0];
        }

        return normalized;
    }


    void warpAffineAndNormalize(cv::Mat srcImg, cv::Mat& dstImg, int dstW, int dstH, cv::Mat affineMatrix,
        float norn_mean[3], float norn_std[3], float fill_value)
    {
        cv::Mat tmp_img;

        int image_channels = srcImg.channels();

        if (image_channels == 3)
        {
            cv::warpAffine(srcImg, tmp_img, affineMatrix, cv::Size(dstW, dstH), cv::INTER_LINEAR, cv::BORDER_CONSTANT,
                cv::Scalar(fill_value, fill_value, fill_value));
        }
        else
        {
            cv::warpAffine(srcImg, tmp_img, affineMatrix, cv::Size(dstW, dstH), cv::INTER_LINEAR, cv::BORDER_CONSTANT, cv::Scalar(fill_value));
        }

        tmp_img = normalizeByCV(tmp_img, norn_mean, norn_std);

        // Change data layout from HWC to CHW
        cv::dnn::blobFromImage(tmp_img, dstImg);

    }


    void warpAffineAndNormalizeImageMixDepth(cv::Mat srcImg, cv::Mat srcDepth, cv::Mat& dstImg, int dstW, int dstH, cv::Mat affineMatrix,
        float norn_mean[3], float norn_std[3], float fill_value)
    {
        cv::Mat tmp_image, tmp_depth;

        int image_channels = srcImg.channels();
        if (image_channels == 3)
        {
            cv::warpAffine(srcImg, tmp_image, affineMatrix, cv::Size(dstW, dstH), cv::INTER_LINEAR, cv::BORDER_CONSTANT,
                cv::Scalar(fill_value, fill_value, fill_value));
        }
        else
        {
            cv::warpAffine(srcImg, tmp_image, affineMatrix, cv::Size(dstW, dstH), cv::INTER_LINEAR, cv::BORDER_CONSTANT, cv::Scalar(fill_value));
        }
        tmp_image = normalizeByCV(tmp_image, norn_mean, norn_std);
        
        cv::warpAffine(srcDepth, tmp_depth, affineMatrix, cv::Size(dstW, dstH), cv::INTER_LINEAR, cv::BORDER_CONSTANT, cv::Scalar(0));

        if (tmp_image.type() != CV_32F)
            tmp_image.convertTo(tmp_image, CV_32F);

        if (tmp_depth.type() != CV_32F)
            tmp_depth.convertTo(tmp_depth, CV_32F);

        std::vector<cv::Mat> channels;
        cv::split(tmp_image, channels);
        channels.push_back(tmp_depth);

        //cv::Mat merged;
        //cv::merge(channels, merged);

        //// Change data layout from HWC to CHW
        //cv::dnn::blobFromImage(merged, dstImg);

        int H = tmp_image.rows;
        int W = tmp_image.cols;
        int C = static_cast<int>(channels.size());
        dstImg.create(1, C * H * W, CV_32F);
        float* dst_data = dstImg.ptr<float>();
        for (int c = 0; c < C; ++c)
        {
            std::memcpy(dst_data + c * H * W, channels[c].ptr<float>(), H * W * sizeof(float));
        }
    }


    void warpAffineAndNormalizeDepthMixMask(cv::Mat srcDepth, cv::Mat srcMask, cv::Mat& dstImg, int dstW, int dstH, cv::Mat affineMatrix,
        float norn_mean[3], float norn_std[3], float fill_value)
    {
        cv::Mat tmp_depth, tmp_mask;

        cv::warpAffine(srcDepth, tmp_depth, affineMatrix, cv::Size(dstW, dstH), cv::INTER_LINEAR, cv::BORDER_CONSTANT, cv::Scalar(0));
        cv::warpAffine(srcMask, tmp_mask, affineMatrix, cv::Size(dstW, dstH), cv::INTER_NEAREST, cv::BORDER_CONSTANT, cv::Scalar(0));

        if (tmp_depth.type() != CV_32F)
            tmp_depth.convertTo(tmp_depth, CV_32F);

        if (tmp_mask.type() != CV_32F)
            tmp_mask.convertTo(tmp_mask, CV_32F);

        std::vector<cv::Mat> channels = { tmp_depth, tmp_mask };
        
        //cv::Mat merged;
        //cv::merge(channels, merged);

        //// Change data layout from HWC to CHW
        //cv::dnn::blobFromImage(merged, dstImg);

        int H = tmp_depth.rows;
        int W = tmp_depth.cols;
        int C = static_cast<int>(channels.size());
        dstImg.create(1, C * H * W, CV_32F);
        float* dst_data = dstImg.ptr<float>();
        for (int c = 0; c < C; ++c)
        {
            std::memcpy(dst_data + c * H * W, channels[c].ptr<float>(), H * W * sizeof(float));
        }
    }


    void warpAffineAndNormalizeImageMixDepthMixDepth(cv::Mat srcImg, cv::Mat srcDepth, cv::Mat srcMask, cv::Mat& dstImg, int dstW, int dstH,
        cv::Mat affineMatrix, float norn_mean[3], float norn_std[3], float fill_value)
    {
        cv::Mat tmp_image, tmp_depth, tmp_mask;

        int image_channels = srcImg.channels();
        if (image_channels == 3)
        {
            cv::warpAffine(srcImg, tmp_image, affineMatrix, cv::Size(dstW, dstH), cv::INTER_LINEAR, cv::BORDER_CONSTANT,
                cv::Scalar(fill_value, fill_value, fill_value));
        }
        else
        {
            cv::warpAffine(srcImg, tmp_image, affineMatrix, cv::Size(dstW, dstH), cv::INTER_LINEAR, cv::BORDER_CONSTANT, cv::Scalar(fill_value));
        }
        tmp_image = normalizeByCV(tmp_image, norn_mean, norn_std);

        cv::warpAffine(srcDepth, tmp_depth, affineMatrix, cv::Size(dstW, dstH), cv::INTER_LINEAR, cv::BORDER_CONSTANT, cv::Scalar(0));
        cv::warpAffine(srcMask, tmp_mask, affineMatrix, cv::Size(dstW, dstH), cv::INTER_NEAREST, cv::BORDER_CONSTANT, cv::Scalar(0));

        if (tmp_image.type() != CV_32F)
            tmp_image.convertTo(tmp_image, CV_32F);
        
        if (tmp_depth.type() != CV_32F)
            tmp_depth.convertTo(tmp_depth, CV_32F);

        if (tmp_mask.type() != CV_32F)
            tmp_mask.convertTo(tmp_mask, CV_32F);

        std::vector<cv::Mat> channels;
        cv::split(tmp_image, channels);
        channels.push_back(tmp_depth);
        channels.push_back(tmp_mask);

        //cv::Mat merged;
        //cv::merge(channels, merged);

        //// Change data layout from HWC to CHW
        //cv::dnn::blobFromImage(merged, dstImg);

        int H = tmp_image.rows;
        int W = tmp_image.cols;
        int C = static_cast<int>(channels.size());
        dstImg.create(1, C * H * W, CV_32F);
        float* dst_data = dstImg.ptr<float>();
        for (int c = 0; c < C; ++c)
        {
            std::memcpy(dst_data + c * H * W, channels[c].ptr<float>(), H * W * sizeof(float));
        }
    }


}
