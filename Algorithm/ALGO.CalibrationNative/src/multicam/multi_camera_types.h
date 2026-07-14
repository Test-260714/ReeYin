#pragma once

#include <opencv2/core.hpp>
#include <map>
#include <string>
#include <vector>

namespace calib::multicam
{
    enum class AnchorMode
    {
        BoardPose = 0,
        Camera = 1,
        External = 2
    };

    struct MultiCameraOptions
    {
        AnchorMode anchorMode = AnchorMode::BoardPose;
        std::string referenceCameraId;
        std::string referenceCaptureId;
        bool refineIntrinsics = false;
        bool refineDistortion = false;
        int minCornersPerObservation = 6;
        double maxReprojectionErrorForInit = 5.0;
        double robustLossScale = 1.0;
        int maxIterations = 100;
    };

    struct BoardCornerObservation
    {
        int pointId = -1;
        cv::Point3d boardPoint;
        cv::Point2d imagePoint;
        double confidence = 1.0;
    };

    struct BoardObservation
    {
        std::string cameraId;
        std::string captureId;
        std::string boardId = "main";
        std::string imagePath;
        cv::Size imageSize;
        std::vector<BoardCornerObservation> corners;
        cv::Mat rvecCameraFromBoard;
        cv::Mat tvecCameraFromBoard;
        double initialReprojectionError = 0.0;
        bool hasInitialPose = false;
    };

    struct CameraRigParams
    {
        std::string cameraId;
        cv::Size imageSize;
        cv::Mat intrinsic;
        cv::Mat distortion;
        cv::Mat transformCameraFromCommon;
        cv::Mat transformCommonFromCamera;
        bool hasIntrinsic = false;
        bool fixedIntrinsic = false;
        double rmsError = 0.0;
    };

    struct CapturePose
    {
        std::string captureId;
        cv::Mat transformCommonFromBoard;
        bool fixed = false;
    };

    struct MultiCameraReport
    {
        int cameraCount = 0;
        int captureCount = 0;
        int observationCount = 0;
        int residualCount = 0;
        double initialRmsError = 0.0;
        double finalRmsError = 0.0;
        double maxReprojectionError = 0.0;
        int connectedComponentCount = 0;
        bool converged = false;
        int ceresTerminationType = 0;
        std::vector<std::string> messages;
    };

    inline cv::Mat makeIdentityTransform()
    {
        return cv::Mat::eye(4, 4, CV_64F);
    }

    inline cv::Mat composeTransform(const cv::Mat& a, const cv::Mat& b)
    {
        return a * b;
    }

    inline cv::Mat invertTransform(const cv::Mat& transform)
    {
        return transform.inv();
    }
}
