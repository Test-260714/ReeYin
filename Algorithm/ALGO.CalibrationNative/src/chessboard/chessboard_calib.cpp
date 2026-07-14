#include "pch.h"

#include "chessboard_calib.h"

#include <opencv2/calib3d.hpp>
#include <opencv2/imgproc.hpp>

namespace
{
    bool toGray8ForChessboard(const cv::Mat& image, cv::Mat& gray, std::string& errorMessage)
    {
        if (image.empty())
        {
            errorMessage = "Chessboard detection failed: image is empty.";
            return false;
        }

        const int channels = image.channels();
        if (channels == 1)
        {
            gray = image;
        }
        else if (channels == 3)
        {
            cv::cvtColor(image, gray, cv::COLOR_BGR2GRAY);
        }
        else if (channels == 4)
        {
            cv::cvtColor(image, gray, cv::COLOR_BGRA2GRAY);
        }
        else
        {
            errorMessage = "Chessboard detection failed: unsupported image channel count.";
            return false;
        }

        if (gray.depth() != CV_8U)
        {
            cv::Mat converted;
            gray.convertTo(converted, CV_8U);
            gray = converted;
        }
        return true;
    }
}

std::vector<cv::Point3d> ChessboardCalibration::createObjectPoints(const CalibrationBoardParams& boardParams)
{
    std::vector<cv::Point3d> objectPoints;
    objectPoints.reserve(static_cast<size_t>(boardParams.width) * static_cast<size_t>(boardParams.height));
    for (int row = 0; row < boardParams.height; ++row)
    {
        for (int column = 0; column < boardParams.width; ++column)
        {
            objectPoints.emplace_back(
                static_cast<double>(column) * boardParams.squareSize,
                static_cast<double>(row) * boardParams.squareSize,
                0.0);
        }
    }
    return objectPoints;
}

bool ChessboardCalibration::detect(
    const cv::Mat& image,
    const CalibrationBoardParams& boardParams,
    calib::multicam::BoardObservation& observation,
    std::string& errorMessage) const
{
    cv::Mat gray;
    if (!toGray8ForChessboard(image, gray, errorMessage))
    {
        return false;
    }

    const cv::Size patternSize(boardParams.width, boardParams.height);
    std::vector<cv::Point2f> corners;
    const bool found = cv::findChessboardCornersSB(
        gray,
        patternSize,
        corners,
        cv::CALIB_CB_EXHAUSTIVE | cv::CALIB_CB_ACCURACY);
    if (!found)
    {
        errorMessage = "Chessboard detection failed: no chessboard corners found.";
        return false;
    }

    cv::cornerSubPix(
        gray,
        corners,
        cv::Size(11, 11),
        cv::Size(-1, -1),
        cv::TermCriteria(cv::TermCriteria::EPS + cv::TermCriteria::MAX_ITER, 30, 0.001));

    const std::vector<cv::Point3d> objectPoints = createObjectPoints(boardParams);
    if (corners.size() != objectPoints.size())
    {
        errorMessage = "Chessboard detection failed: detected corner count does not match board dimensions.";
        return false;
    }

    observation.imageSize = gray.size();
    observation.corners.clear();
    observation.corners.reserve(corners.size());
    for (size_t index = 0; index < corners.size(); ++index)
    {
        calib::multicam::BoardCornerObservation corner;
        corner.pointId = static_cast<int>(index);
        corner.boardPoint = objectPoints[index];
        corner.imagePoint = cv::Point2d(corners[index].x, corners[index].y);
        observation.corners.push_back(corner);
    }

    errorMessage.clear();
    return true;
}

bool ChessboardCalibration::calibrate(
    const std::string& cameraId,
    const CalibrationBoardParams& boardParams,
    const std::vector<cv::Mat>& images,
    cv::Mat& intrinsic,
    cv::Mat& distortion,
    std::vector<cv::Mat>& rvecs,
    std::vector<cv::Mat>& tvecs,
    double& rmsError,
    std::string& errorMessage) const
{
    if (images.empty())
    {
        errorMessage = "Camera '" + cameraId + "' has no images for Chessboard calibration.";
        return false;
    }

    std::vector<calib::multicam::BoardObservation> observations;
    cv::Size imageSize;
    observations.reserve(images.size());
    for (const auto& image : images)
    {
        calib::multicam::BoardObservation observation;
        std::string detectionError;
        if (detect(image, boardParams, observation, detectionError))
        {
            observations.push_back(observation);
            if (imageSize.empty())
            {
                imageSize = image.size();
            }
        }
    }

    if (observations.empty())
    {
        errorMessage = "Camera '" + cameraId + "' has no detectable Chessboard images for calibration.";
        return false;
    }

    return calibrate(
        cameraId,
        boardParams,
        observations,
        imageSize,
        intrinsic,
        distortion,
        rvecs,
        tvecs,
        rmsError,
        errorMessage);
}

bool ChessboardCalibration::calibrate(
    const std::string& cameraId,
    const CalibrationBoardParams& boardParams,
    const std::vector<calib::multicam::BoardObservation>& observations,
    const cv::Size& imageSize,
    cv::Mat& intrinsic,
    cv::Mat& distortion,
    std::vector<cv::Mat>& rvecs,
    std::vector<cv::Mat>& tvecs,
    double& rmsError,
    std::string& errorMessage) const
{
    (void)boardParams;
    if (observations.empty())
    {
        errorMessage = "Camera '" + cameraId + "' has no observations for Chessboard calibration.";
        return false;
    }

    std::vector<std::vector<cv::Point3f>> allObjectPoints;
    std::vector<std::vector<cv::Point2f>> allImagePoints;
    allObjectPoints.reserve(observations.size());
    allImagePoints.reserve(observations.size());

    for (const auto& observation : observations)
    {
        std::vector<cv::Point3f> objectPoints;
        std::vector<cv::Point2f> imagePoints;
        objectPoints.reserve(observation.corners.size());
        imagePoints.reserve(observation.corners.size());
        for (const auto& corner : observation.corners)
        {
            objectPoints.emplace_back(
                static_cast<float>(corner.boardPoint.x),
                static_cast<float>(corner.boardPoint.y),
                static_cast<float>(corner.boardPoint.z));
            imagePoints.emplace_back(
                static_cast<float>(corner.imagePoint.x),
                static_cast<float>(corner.imagePoint.y));
        }
        if (!objectPoints.empty())
        {
            allObjectPoints.push_back(objectPoints);
            allImagePoints.push_back(imagePoints);
        }
    }

    if (allObjectPoints.empty())
    {
        errorMessage = "Camera '" + cameraId + "' has no usable Chessboard corners for calibration.";
        return false;
    }

    try
    {
        std::vector<cv::Mat> localRvecs;
        std::vector<cv::Mat> localTvecs;
        intrinsic = cv::Mat::eye(3, 3, CV_64F);
        const double error = cv::calibrateCamera(
            allObjectPoints,
            allImagePoints,
            imageSize,
            intrinsic,
            distortion,
            localRvecs,
            localTvecs);
        rvecs = localRvecs;
        tvecs = localTvecs;
        rmsError = error;
    }
    catch (const cv::Exception& exception)
    {
        errorMessage = "cv::calibrateCamera failed for CHESSBOARD camera '" + cameraId + "': " + exception.what();
        return false;
    }

    errorMessage.clear();
    return true;
}

bool ChessboardCalibration::computePhysicalCoords(
    const CalibrationBoardParams& boardParams,
    const std::vector<int>& pointIds,
    std::vector<cv::Point3d>& physicalCoords,
    std::string& errorMessage) const
{
    const std::vector<cv::Point3d> objectPoints = createObjectPoints(boardParams);
    physicalCoords.clear();
    physicalCoords.reserve(pointIds.size());

    for (const int pointId : pointIds)
    {
        if (pointId < 0 || pointId >= static_cast<int>(objectPoints.size()))
        {
            errorMessage = "Chessboard physical coordinate computation failed: point id is out of range.";
            physicalCoords.clear();
            return false;
        }
        physicalCoords.push_back(objectPoints[pointId]);
    }

    errorMessage.clear();
    return true;
}

bool ChessboardCalibration::getHomographyMatrix(
    const CalibrationBoardParams& boardParams,
    const cv::Mat& image,
    const cv::Mat& cameraMatrix,
    const cv::Mat& distCoeffs,
    cv::Mat& homography,
    std::string& errorMessage) const
{
    if (image.empty())
    {
        errorMessage = "Chessboard homography computation failed: image is empty.";
        homography = cv::Mat();
        return false;
    }

    calib::multicam::BoardObservation observation;
    if (!detect(image, boardParams, observation, errorMessage))
    {
        homography = cv::Mat();
        return false;
    }
    if (observation.corners.size() <= 4)
    {
        errorMessage = "Chessboard homography computation failed: at least 5 corners are required.";
        homography = cv::Mat();
        return false;
    }

    std::vector<int> pointIds;
    std::vector<cv::Point2f> imagePoints;
    pointIds.reserve(observation.corners.size());
    imagePoints.reserve(observation.corners.size());
    for (const auto& corner : observation.corners)
    {
        pointIds.push_back(corner.pointId);
        imagePoints.emplace_back(
            static_cast<float>(corner.imagePoint.x),
            static_cast<float>(corner.imagePoint.y));
    }

    std::vector<cv::Point3d> physicalPoints3d;
    if (!computePhysicalCoords(boardParams, pointIds, physicalPoints3d, errorMessage))
    {
        homography = cv::Mat();
        return false;
    }

    std::vector<cv::Point2f> undistorted;
    cv::undistortPoints(imagePoints, undistorted, cameraMatrix, distCoeffs);

    std::vector<cv::Point2f> physicalPoints;
    physicalPoints.reserve(physicalPoints3d.size());
    for (const auto& point : physicalPoints3d)
    {
        physicalPoints.emplace_back(static_cast<float>(point.x), static_cast<float>(point.y));
    }

    homography = cv::findHomography(undistorted, physicalPoints);
    if (homography.empty())
    {
        errorMessage = "Chessboard homography computation failed: cv::findHomography returned an empty matrix.";
        return false;
    }

    errorMessage.clear();
    return true;
}
