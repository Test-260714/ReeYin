#include "pch.h"

#include "charuco_calib.h"

#include <opencv2/aruco.hpp>
#include <opencv2/imgproc.hpp>

#include <algorithm>
#include <cmath>
#include <unordered_map>
#include <unordered_set>

namespace
{
    constexpr int kCharucoInterpolationMinMarkers = 1;
    constexpr size_t kMinCharucoCornersForCalibration = 11;

    int makeOddWindowSize(int value)
    {
        return value % 2 == 0 ? value + 1 : value;
    }

    cv::Ptr<cv::aruco::DetectorParameters> createCharucoDetectorParameters(
        const CalibrationBoardParams& params,
        const cv::Size& imageSize)
    {
        cv::Ptr<cv::aruco::DetectorParameters> detectorParams = cv::makePtr<cv::aruco::DetectorParameters>();
        if (!(params.squareSizePixel > 0.0 && params.markerSizePixel > 0.0))
        {
            return detectorParams;
        }

        const double squareSizePixel = params.squareSizePixel;
        const double markerSizePixel = params.markerSizePixel;
        const double maxImageDimension = static_cast<double>(std::max(imageSize.width, imageSize.height));
        if (maxImageDimension <= 0.0)
        {
            return detectorParams;
        }

        detectorParams->adaptiveThreshWinSizeMin = 3;
        detectorParams->adaptiveThreshWinSizeMax = makeOddWindowSize(
            static_cast<int>(std::round(std::clamp(squareSizePixel, 23.0, 151.0))));
        detectorParams->adaptiveThreshWinSizeStep = 4;

        detectorParams->markerBorderBits = 1;
        detectorParams->minMarkerPerimeterRate = std::clamp(
            4.0 * markerSizePixel * 0.6 / maxImageDimension,
            0.001,
            4.0);
        detectorParams->maxMarkerPerimeterRate = std::clamp(
            4.0 * markerSizePixel * 1.8 / maxImageDimension,
            0.001,
            4.0);

        detectorParams->errorCorrectionRate = 0.3;
        detectorParams->maxErroneousBitsInBorderRate = 0.2;

        detectorParams->cornerRefinementMethod = cv::aruco::CORNER_REFINE_SUBPIX;
        detectorParams->cornerRefinementWinSize = std::clamp(
            static_cast<int>(std::round(markerSizePixel / 8.0)),
            2,
            10);
        detectorParams->cornerRefinementMaxIterations = 30;
        detectorParams->cornerRefinementMinAccuracy = 0.1;

        return detectorParams;
    }

    bool toGray8ForCharuco(const cv::Mat& image, cv::Mat& gray, std::string& errorMessage)
    {
        if (image.empty())
        {
            errorMessage = "Charuco detection failed: image is empty.";
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
            errorMessage = "Charuco detection failed: unsupported image channel count.";
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

    void filterDetectedMarkersForBoard(
        const cv::aruco::CharucoBoard& board,
        std::vector<std::vector<cv::Point2f>>& markerCorners,
        std::vector<int>& markerIds)
    {
        const std::vector<int>& boardMarkerIds = board.getIds();
        const std::unordered_set<int> boardMarkerIdSet(boardMarkerIds.begin(), boardMarkerIds.end());
        std::unordered_map<int, size_t> markerIdCounts;
        std::vector<std::vector<cv::Point2f>> filteredMarkerCorners;
        std::vector<int> filteredMarkerIds;
        const size_t detectionCount = std::min(markerIds.size(), markerCorners.size());

        for (size_t index = 0; index < detectionCount; ++index)
        {
            const int markerId = markerIds[index];
            if (boardMarkerIdSet.find(markerId) != boardMarkerIdSet.end())
            {
                ++markerIdCounts[markerId];
            }
        }

        filteredMarkerCorners.reserve(detectionCount);
        filteredMarkerIds.reserve(detectionCount);

        for (size_t index = 0; index < detectionCount; ++index)
        {
            const int markerId = markerIds[index];
            if (boardMarkerIdSet.find(markerId) == boardMarkerIdSet.end())
            {
                continue;
            }
            const auto countIt = markerIdCounts.find(markerId);
            if (countIt == markerIdCounts.end() || countIt->second != 1)
            {
                continue;
            }

            filteredMarkerCorners.push_back(markerCorners[index]);
            filteredMarkerIds.push_back(markerId);
        }

        markerCorners.swap(filteredMarkerCorners);
        markerIds.swap(filteredMarkerIds);
    }
}

cv::aruco::CharucoBoard CharucoCalibration::createCharucoBoard(const CalibrationBoardParams& params)
{
    cv::aruco::Dictionary dictionary =
        cv::aruco::getPredefinedDictionary(static_cast<cv::aruco::PredefinedDictionaryType>(params.dictionaryId));
    return cv::aruco::CharucoBoard(
        cv::Size(params.width, params.height),
        static_cast<float>(params.squareSize),
        static_cast<float>(params.markerSize),
        dictionary);
}

bool CharucoCalibration::detect(
    const cv::Mat& image,
    const CalibrationBoardParams& boardParams,
    calib::multicam::BoardObservation& observation,
    std::string& errorMessage) const
{
    cv::Mat gray;
    if (!toGray8ForCharuco(image, gray, errorMessage))
    {
        return false;
    }

    cv::aruco::CharucoBoard board = createCharucoBoard(boardParams);
    cv::Ptr<cv::aruco::CharucoBoard> boardPtr = cv::makePtr<cv::aruco::CharucoBoard>(board);
    cv::Ptr<cv::aruco::Dictionary> dictionary = cv::makePtr<cv::aruco::Dictionary>(boardPtr->getDictionary());
    cv::Ptr<cv::aruco::DetectorParameters> detectorParams = createCharucoDetectorParameters(boardParams, gray.size());

    std::vector<int> markerIds;
    std::vector<std::vector<cv::Point2f>> markerCorners;
    cv::aruco::detectMarkers(gray, dictionary, markerCorners, markerIds, detectorParams);
    if (markerIds.empty())
    {
        errorMessage = "Charuco detection failed: no ArUco markers found.";
        return false;
    }

    // 为了防止误检出来的marker造成插值异常。把marker重复 markerIds 整组丢掉，同时只保留 markerIds 刚好属于当前 board
    filterDetectedMarkersForBoard(board, markerCorners, markerIds);
    if (markerIds.empty())
    {
        errorMessage = "Charuco detection failed: no ArUco markers belonging to the current board found.";
        return false;
    }

    std::vector<cv::Point2f> charucoCorners;
    std::vector<int> charucoIds;
    cv::aruco::interpolateCornersCharuco(
        markerCorners,
        markerIds,
        gray,
        boardPtr,
        charucoCorners,
        charucoIds,
        cv::noArray(),
        cv::noArray(),
        kCharucoInterpolationMinMarkers);
    if (charucoCorners.empty() || charucoIds.empty())
    {
        errorMessage = "Charuco detection failed: no interpolated corners found.";
        return false;
    }

    const std::vector<cv::Point3f> boardCorners = boardPtr->getChessboardCorners();
    observation.imageSize = gray.size();
    observation.corners.clear();
    observation.corners.reserve(charucoIds.size());

    for (size_t index = 0; index < charucoIds.size(); ++index)
    {
        const int pointId = charucoIds[index];
        if (pointId < 0 || pointId >= static_cast<int>(boardCorners.size()))
        {
            continue;
        }

        calib::multicam::BoardCornerObservation corner;
        corner.pointId = pointId;
        corner.boardPoint = cv::Point3d(boardCorners[pointId].x, boardCorners[pointId].y, boardCorners[pointId].z);
        corner.imagePoint = cv::Point2d(charucoCorners[index].x, charucoCorners[index].y);
        observation.corners.push_back(corner);
    }

    if (observation.corners.empty())
    {
        errorMessage = "Charuco detection failed: all interpolated corner ids were invalid.";
        return false;
    }

    errorMessage.clear();
    return true;
}

bool CharucoCalibration::calibrate(
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
        errorMessage = "Camera '" + cameraId + "' has no images for Charuco calibration.";
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
        errorMessage = "Camera '" + cameraId + "' has no detectable Charuco images for calibration.";
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

bool CharucoCalibration::calibrate(
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
    if (observations.empty())
    {
        errorMessage = "Camera '" + cameraId + "' has no observations for Charuco calibration.";
        return false;
    }

    cv::aruco::CharucoBoard board = createCharucoBoard(boardParams);
    cv::Ptr<cv::aruco::CharucoBoard> boardPtr = cv::makePtr<cv::aruco::CharucoBoard>(board);

    std::vector<std::vector<cv::Point2f>> allCorners;
    std::vector<std::vector<int>> allIds;
    allCorners.reserve(observations.size());
    allIds.reserve(observations.size());
    for (const auto& observation : observations)
    {
        std::vector<cv::Point2f> corners;
        std::vector<int> ids;
        corners.reserve(observation.corners.size());
        ids.reserve(observation.corners.size());
        for (const auto& corner : observation.corners)
        {
            corners.emplace_back(
                static_cast<float>(corner.imagePoint.x),
                static_cast<float>(corner.imagePoint.y));
            ids.push_back(corner.pointId);
        }
        if (corners.size() >= kMinCharucoCornersForCalibration)
        {
            allCorners.push_back(corners);
            allIds.push_back(ids);
        }
    }

    if (allCorners.empty())
    {
        errorMessage = "Camera '" + cameraId + "' has no usable Charuco observations for calibration with at least 11 corners.";
        return false;
    }

    try
    {
        std::vector<cv::Mat> localRvecs;
        std::vector<cv::Mat> localTvecs;
        intrinsic = cv::Mat::eye(3, 3, CV_64F);
        const double error = cv::aruco::calibrateCameraCharuco(
            allCorners,
            allIds,
            boardPtr,
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
        errorMessage = "cv::aruco::calibrateCameraCharuco failed for camera '" + cameraId + "': " + exception.what();
        return false;
    }

    errorMessage.clear();
    return true;
}

bool CharucoCalibration::computePhysicalCoords(
    const CalibrationBoardParams& boardParams,
    const std::vector<int>& pointIds,
    std::vector<cv::Point3d>& physicalCoords,
    std::string& errorMessage) const
{
    cv::aruco::CharucoBoard board = createCharucoBoard(boardParams);
    const std::vector<cv::Point3f> boardCorners = board.getChessboardCorners();
    physicalCoords.clear();
    physicalCoords.reserve(pointIds.size());

    for (const int pointId : pointIds)
    {
        if (pointId < 0 || pointId >= static_cast<int>(boardCorners.size()))
        {
            errorMessage = "Charuco physical coordinate computation failed: point id is out of range.";
            physicalCoords.clear();
            return false;
        }
        physicalCoords.emplace_back(boardCorners[pointId].x, boardCorners[pointId].y, boardCorners[pointId].z);
    }

    errorMessage.clear();
    return true;
}

bool CharucoCalibration::getHomographyMatrix(
    const CalibrationBoardParams& boardParams,
    const cv::Mat& image,
    const cv::Mat& cameraMatrix,
    const cv::Mat& distCoeffs,
    cv::Mat& homography,
    std::string& errorMessage) const
{
    if (image.empty())
    {
        errorMessage = "Charuco homography computation failed: image is empty.";
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
        errorMessage = "Charuco homography computation failed: at least 5 corners are required.";
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
        errorMessage = "Charuco homography computation failed: cv::findHomography returned an empty matrix.";
        return false;
    }

    errorMessage.clear();
    return true;
}
