#include "pch.h"

#include "multicam/multi_camera_stitcher.h"

#include "multicam/common_plane_projector.h"

#include <opencv2/calib3d.hpp>
#include <opencv2/imgproc.hpp>

#include <cmath>
#include <sstream>

namespace calib::multicam
{
    namespace
    {
        constexpr double kMaxOverlayOutputPixels = 20000000000.0;
        constexpr double kMaxAverageOutputPixels = 10000000000.0;

        bool isFinite(double value)
        {
            return std::isfinite(value);
        }

        bool hasFiniteValues(const cv::Mat& matrix)
        {
            if (matrix.empty() || matrix.type() != CV_64F)
            {
                return false;
            }
            for (int row = 0; row < matrix.rows; ++row)
            {
                for (int col = 0; col < matrix.cols; ++col)
                {
                    if (!isFinite(matrix.at<double>(row, col)))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        bool isValidHomography(const cv::Mat& homography)
        {
            return !homography.empty() &&
                homography.rows == 3 &&
                homography.cols == 3 &&
                homography.type() == CV_64F &&
                hasFiniteValues(homography) &&
                std::abs(cv::determinant(homography)) > 1.0e-12;
        }

        bool validateOptions(const StitchingOptions& options, std::string& errorMessage)
        {
            if (!isFinite(options.heightCompensation) ||
                !isFinite(options.worldUnitsPerPixel) ||
                !isFinite(options.originWorldX) ||
                !isFinite(options.originWorldY))
            {
                errorMessage = "Stitching options must contain finite world values.";
                return false;
            }
            if (options.worldUnitsPerPixel <= 0.0)
            {
                errorMessage = "worldUnitsPerPixel must be positive.";
                return false;
            }
            if (options.outputWidth <= 0 || options.outputHeight <= 0)
            {
                errorMessage = "Output dimensions must be positive.";
                return false;
            }

            const double outputPixels = static_cast<double>(options.outputWidth) * static_cast<double>(options.outputHeight);
            double maxOutputPixels = kMaxOverlayOutputPixels;
            const char* blendModeName = "Overlay";
            if (options.blendMode == BlendMode::Average)
            {
                maxOutputPixels = kMaxAverageOutputPixels;
                blendModeName = "Average";
            }
            if (!isFinite(outputPixels) || outputPixels > maxOutputPixels)
            {
                std::ostringstream message;
                message << "Stitching output size " << options.outputWidth << "x" << options.outputHeight
                    << " exceeds native " << blendModeName << " limit of "
                    << static_cast<long long>(maxOutputPixels) << " pixels.";
                errorMessage = message.str();
                return false;
            }

            if (!isValidHomography(options.HBoardToCanvas))
            {
                errorMessage = "Stitching options must contain a finite non-singular HBoardToCanvas homography.";
                return false;
            }
            if (!options.HCanvasToBoard.empty() && !isValidHomography(options.HCanvasToBoard))
            {
                errorMessage = "Stitching options HCanvasToBoard homography is invalid.";
                return false;
            }
            return true;
        }

        bool isSupportedChannelCount(int channels)
        {
            return channels == 1 || channels == 3 || channels == 4;
        }

        bool isSupportedDepth(int depth)
        {
            return depth == CV_8U ||
                depth == CV_16U ||
                depth == CV_16S ||
                depth == CV_32F ||
                depth == CV_64F;
        }

        bool isValidDistortion(const cv::Mat& distortion, std::string& errorMessage)
        {
            if (distortion.empty())
            {
                return true;
            }
            if (distortion.type() != CV_64FC1 ||
                (distortion.rows != 1 && distortion.cols != 1))
            {
                errorMessage = "Camera distortion coefficients must be a finite CV_64F vector.";
                return false;
            }

            for (int row = 0; row < distortion.rows; ++row)
            {
                for (int col = 0; col < distortion.cols; ++col)
                {
                    if (!isFinite(distortion.at<double>(row, col)))
                    {
                        errorMessage = "Camera distortion coefficients must be finite.";
                        return false;
                    }
                }
            }

            const int coefficientCount = distortion.rows * distortion.cols;
            if (coefficientCount != 4 &&
                coefficientCount != 5 &&
                coefficientCount != 8 &&
                coefficientCount != 12 &&
                coefficientCount != 14)
            {
                errorMessage = "Camera distortion coefficients must contain 4, 5, 8, 12, or 14 values.";
                return false;
            }
            return true;
        }

        bool hasNonZeroDistortion(const cv::Mat& distortion)
        {
            if (distortion.empty())
            {
                return false;
            }
            for (int row = 0; row < distortion.rows; ++row)
            {
                for (int col = 0; col < distortion.cols; ++col)
                {
                    if (std::abs(distortion.at<double>(row, col)) > 0.0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        void erodeMaskBorder(cv::Mat& mask)
        {
            if (mask.cols > 2 && mask.rows > 2)
            {
                cv::erode(mask, mask, cv::Mat());
            }
        }

        bool undistortImageAndMask(
            const StitchImageInput& imageInput,
            const CameraRigParams& camera,
            const cv::Mat& sourceMask,
            cv::Mat& undistortedImage,
            cv::Mat& undistortedMask,
            std::string& errorMessage)
        {
            if (!hasNonZeroDistortion(camera.distortion))
            {
                undistortedImage = imageInput.image;
                undistortedMask = sourceMask;
                return true;
            }

            try
            {
                cv::Mat mapX;
                cv::Mat mapY;
                cv::initUndistortRectifyMap(
                    camera.intrinsic,
                    camera.distortion,
                    cv::Mat(),
                    camera.intrinsic,
                    imageInput.image.size(),
                    CV_32FC1,
                    mapX,
                    mapY);
                cv::remap(
                    imageInput.image,
                    undistortedImage,
                    mapX,
                    mapY,
                    cv::INTER_LINEAR,
                    cv::BORDER_CONSTANT,
                    cv::Scalar::all(0));
                cv::remap(
                    sourceMask,
                    undistortedMask,
                    mapX,
                    mapY,
                    cv::INTER_NEAREST,
                    cv::BORDER_CONSTANT,
                    cv::Scalar(0));
                erodeMaskBorder(undistortedMask);
            }
            catch (const cv::Exception& exception)
            {
                errorMessage = "cv::undistort/remap failed for camera '" + imageInput.cameraId + "': " + exception.what();
                return false;
            }
            return true;
        }

        bool worldToCanvas(const cv::Point3d& worldPoint, const StitchingOptions& options, cv::Point2d& canvasPoint)
        {
            const cv::Mat& h = options.HBoardToCanvas;
            const double x =
                h.at<double>(0, 0) * worldPoint.x +
                h.at<double>(0, 1) * worldPoint.y +
                h.at<double>(0, 2);
            const double y =
                h.at<double>(1, 0) * worldPoint.x +
                h.at<double>(1, 1) * worldPoint.y +
                h.at<double>(1, 2);
            const double w =
                h.at<double>(2, 0) * worldPoint.x +
                h.at<double>(2, 1) * worldPoint.y +
                h.at<double>(2, 2);
            if (!isFinite(x) || !isFinite(y) || !isFinite(w) || std::abs(w) < 1.0e-12)
            {
                return false;
            }

            canvasPoint = cv::Point2d(x / w, y / w);
            return isFinite(canvasPoint.x) && isFinite(canvasPoint.y);
        }

        void accumulateAverage(const cv::Mat& warped, const cv::Mat& warpedMask, cv::Mat& accumulator, cv::Mat& weights)
        {
            cv::Mat warpedFloat;
            warped.convertTo(warpedFloat, accumulator.type());

            const int channels = accumulator.channels();
            for (int row = 0; row < accumulator.rows; ++row)
            {
                const uchar* maskRow = warpedMask.ptr<uchar>(row);
                const double* sourceRow = warpedFloat.ptr<double>(row);
                double* accumulatorRow = accumulator.ptr<double>(row);
                int* weightRow = weights.ptr<int>(row);

                for (int col = 0; col < accumulator.cols; ++col)
                {
                    if (maskRow[col] == 0)
                    {
                        continue;
                    }

                    const int offset = col * channels;
                    for (int channel = 0; channel < channels; ++channel)
                    {
                        accumulatorRow[offset + channel] += sourceRow[offset + channel];
                    }
                    ++weightRow[col];
                }
            }
        }

        cv::Mat finalizeAverage(const cv::Mat& accumulator, const cv::Mat& weights, int outputType)
        {
            cv::Mat averaged = cv::Mat::zeros(accumulator.size(), accumulator.type());
            const int channels = accumulator.channels();
            for (int row = 0; row < accumulator.rows; ++row)
            {
                const double* accumulatorRow = accumulator.ptr<double>(row);
                const int* weightRow = weights.ptr<int>(row);
                double* averagedRow = averaged.ptr<double>(row);

                for (int col = 0; col < accumulator.cols; ++col)
                {
                    const int weight = weightRow[col];
                    if (weight <= 0)
                    {
                        continue;
                    }

                    const int offset = col * channels;
                    for (int channel = 0; channel < channels; ++channel)
                    {
                        averagedRow[offset + channel] = accumulatorRow[offset + channel] / static_cast<double>(weight);
                    }
                }
            }

            cv::Mat output;
            averaged.convertTo(output, outputType);
            return output;
        }
    }

    bool MultiCameraStitcher::stitch(
        const std::map<std::string, CameraRigParams>& cameras,
        const std::vector<StitchImageInput>& images,
        const StitchingOptions& options,
        cv::Mat& output,
        std::string& errorMessage) const
    {
        output.release();
        errorMessage.clear();

        if (!validateOptions(options, errorMessage))
        {
            return false;
        }
        if (cameras.empty())
        {
            errorMessage = "No cameras were provided for stitching.";
            return false;
        }
        if (images.empty())
        {
            errorMessage = "No images were provided for stitching.";
            return false;
        }

        const cv::Size outputSize(options.outputWidth, options.outputHeight);
        const int outputType = images.front().image.type();
        const int outputChannels = images.front().image.channels();
        if (!isSupportedDepth(images.front().image.depth()))
        {
            errorMessage = "Stitching supports only CV_8U, CV_16U, CV_16S, CV_32F, or CV_64F image depths.";
            return false;
        }
        if (!isSupportedChannelCount(outputChannels))
        {
            errorMessage = "Stitching supports only 1, 3, or 4 channel images.";
            return false;
        }

        if (options.blendMode == BlendMode::Overlay)
        {
            output = cv::Mat::zeros(outputSize, outputType);
        }

        cv::Mat accumulator;
        cv::Mat weights;
        if (options.blendMode == BlendMode::Average)
        {
            accumulator = cv::Mat::zeros(outputSize, CV_MAKETYPE(CV_64F, outputChannels));
            weights = cv::Mat::zeros(outputSize, CV_32SC1);
        }

        CommonPlaneProjector projector;
        for (const auto& imageInput : images)
        {
            if (imageInput.image.empty())
            {
                errorMessage = "Image for camera '" + imageInput.cameraId + "' is empty.";
                return false;
            }
            if (imageInput.image.cols < 2 || imageInput.image.rows < 2)
            {
                errorMessage = "Image for camera '" + imageInput.cameraId + "' must be at least 2x2.";
                return false;
            }
            if (imageInput.image.type() != outputType)
            {
                errorMessage = "All stitched images must have the same OpenCV type.";
                return false;
            }
            if (!isSupportedDepth(imageInput.image.depth()))
            {
                errorMessage = "Image for camera '" + imageInput.cameraId + "' has an unsupported OpenCV depth.";
                return false;
            }
            if (!isSupportedChannelCount(imageInput.image.channels()))
            {
                errorMessage = "Image for camera '" + imageInput.cameraId + "' has an unsupported channel count.";
                return false;
            }

            const auto camera = cameras.find(imageInput.cameraId);
            if (camera == cameras.end())
            {
                errorMessage = "Image references missing camera '" + imageInput.cameraId + "'.";
                return false;
            }
            if (!isValidDistortion(camera->second.distortion, errorMessage))
            {
                errorMessage = "Camera '" + imageInput.cameraId + "' has invalid distortion parameters: " + errorMessage;
                return false;
            }

            cv::Mat undistortedImage;
            cv::Mat undistortedMask;
            cv::Mat sourceMask(imageInput.image.size(), CV_8UC1, cv::Scalar(255));
            if (!undistortImageAndMask(
                imageInput,
                camera->second,
                sourceMask,
                undistortedImage,
                undistortedMask,
                errorMessage))
            {
                return false;
            }

            CameraRigParams undistortedCamera = camera->second;
            undistortedCamera.distortion.release();

            const std::vector<cv::Point2d> sourceCorners = {
                cv::Point2d(0.0, 0.0),
                cv::Point2d(static_cast<double>(undistortedImage.cols - 1), 0.0),
                cv::Point2d(static_cast<double>(undistortedImage.cols - 1), static_cast<double>(undistortedImage.rows - 1)),
                cv::Point2d(0.0, static_cast<double>(undistortedImage.rows - 1))
            };

            std::vector<cv::Point2d> canvasCorners;
            canvasCorners.reserve(sourceCorners.size());
            for (const auto& sourceCorner : sourceCorners)
            {
                cv::Point3d worldPoint;
                std::string projectionError;
                if (!projector.pixelToCommonWorld(
                    undistortedCamera,
                    sourceCorner.x,
                    sourceCorner.y,
                    options.heightCompensation,
                    worldPoint,
                    projectionError))
                {
                    errorMessage = "Failed to project image corner for camera '" + imageInput.cameraId + "': " + projectionError;
                    return false;
                }
                cv::Point2d canvasCorner;
                if (!worldToCanvas(worldPoint, options, canvasCorner))
                {
                    errorMessage = "Failed to project image corner to reference camera canvas for camera '" + imageInput.cameraId + "'.";
                    return false;
                }
                canvasCorners.push_back(canvasCorner);
            }

            cv::Mat homography;
            try
            {
                homography = cv::findHomography(sourceCorners, canvasCorners);
            }
            catch (const cv::Exception& exception)
            {
                errorMessage = "cv::findHomography failed for camera '" + imageInput.cameraId + "': " + exception.what();
                return false;
            }
            if (homography.empty())
            {
                errorMessage = "cv::findHomography failed for camera '" + imageInput.cameraId + "'.";
                return false;
            }

            cv::Mat warped;
            cv::Mat warpedMask;
            try
            {
                cv::warpPerspective(
                    undistortedImage,
                    warped,
                    homography,
                    outputSize,
                    cv::INTER_LINEAR,
                    cv::BORDER_CONSTANT,
                    cv::Scalar::all(0));
                cv::warpPerspective(
                    undistortedMask,
                    warpedMask,
                    homography,
                    outputSize,
                    cv::INTER_NEAREST,
                    cv::BORDER_CONSTANT,
                    cv::Scalar(0));
                erodeMaskBorder(warpedMask);
            }
            catch (const cv::Exception& exception)
            {
                errorMessage = "cv::warpPerspective failed for camera '" + imageInput.cameraId + "': " + exception.what();
                return false;
            }

            if (options.blendMode == BlendMode::Overlay)
            {
                warped.copyTo(output, warpedMask);
            }
            else if (options.blendMode == BlendMode::Average)
            {
                accumulateAverage(warped, warpedMask, accumulator, weights);
            }
            else
            {
                std::ostringstream message;
                message << "Unsupported blend mode: " << static_cast<int>(options.blendMode);
                errorMessage = message.str();
                return false;
            }
        }

        if (options.blendMode == BlendMode::Average)
        {
            output = finalizeAverage(accumulator, weights, outputType);
        }

        return true;
    }
}
