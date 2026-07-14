#pragma once

#include "../interface.h"
#include "../multicam/multi_camera_types.h"

#include <opencv2/core.hpp>

#include <string>
#include <vector>

namespace calib::boards
{
    class ICalibrationBoardCalibrator
    {
    public:
        virtual ~ICalibrationBoardCalibrator() = default;

        virtual bool detect(
            const cv::Mat& image,
            const CalibrationBoardParams& boardParams,
            calib::multicam::BoardObservation& observation,
            std::string& errorMessage) const = 0;

        virtual bool calibrate(
            const std::string& cameraId,
            const CalibrationBoardParams& boardParams,
            const std::vector<cv::Mat>& images,
            cv::Mat& intrinsic,
            cv::Mat& distortion,
            std::vector<cv::Mat>& rvecs,
            std::vector<cv::Mat>& tvecs,
            double& rmsError,
            std::string& errorMessage) const = 0;

        virtual bool calibrate(
            const std::string& cameraId,
            const CalibrationBoardParams& boardParams,
            const std::vector<calib::multicam::BoardObservation>& observations,
            const cv::Size& imageSize,
            cv::Mat& intrinsic,
            cv::Mat& distortion,
            std::vector<cv::Mat>& rvecs,
            std::vector<cv::Mat>& tvecs,
            double& rmsError,
            std::string& errorMessage) const = 0;

        virtual bool computePhysicalCoords(
            const CalibrationBoardParams& boardParams,
            const std::vector<int>& pointIds,
            std::vector<cv::Point3d>& physicalCoords,
            std::string& errorMessage) const = 0;

        virtual bool getHomographyMatrix(
            const CalibrationBoardParams& boardParams,
            const cv::Mat& image,
            const cv::Mat& cameraMatrix,
            const cv::Mat& distCoeffs,
            cv::Mat& homography,
            std::string& errorMessage) const = 0;
    };
}
