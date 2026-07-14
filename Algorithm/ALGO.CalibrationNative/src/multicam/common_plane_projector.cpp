#include "pch.h"

#include "multicam/common_plane_projector.h"

#include <opencv2/calib3d.hpp>

#include <cmath>
#include <limits>

namespace calib::multicam
{
    namespace
    {
        constexpr double kParallelEpsilon = 1.0e-12;
        constexpr double kTransformEpsilon = 1.0e-9;

        bool isFinite(double value)
        {
            return std::isfinite(value);
        }

        bool isFinitePoint(const cv::Point2d& point)
        {
            return isFinite(point.x) && isFinite(point.y);
        }

        bool isFinitePoint(const cv::Point3d& point)
        {
            return isFinite(point.x) && isFinite(point.y) && isFinite(point.z);
        }

        bool hasFiniteValues(const cv::Mat& mat)
        {
            if (mat.empty() || mat.type() != CV_64FC1)
            {
                return false;
            }

            for (int row = 0; row < mat.rows; ++row)
            {
                for (int col = 0; col < mat.cols; ++col)
                {
                    if (!isFinite(mat.at<double>(row, col)))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        bool isValidIntrinsic(const CameraRigParams& camera, std::string& errorMessage)
        {
            if (!camera.hasIntrinsic)
            {
                errorMessage = "Camera '" + camera.cameraId + "' is missing intrinsic parameters.";
                return false;
            }
            if (camera.intrinsic.empty() ||
                camera.intrinsic.rows != 3 ||
                camera.intrinsic.cols != 3 ||
                camera.intrinsic.type() != CV_64FC1 ||
                !hasFiniteValues(camera.intrinsic) ||
                camera.intrinsic.at<double>(0, 0) <= std::numeric_limits<double>::epsilon() ||
                camera.intrinsic.at<double>(1, 1) <= std::numeric_limits<double>::epsilon())
            {
                errorMessage = "Camera '" + camera.cameraId + "' has an invalid intrinsic matrix.";
                return false;
            }
            return true;
        }

        bool isValidDistortion(const cv::Mat& distortion, std::string& errorMessage)
        {
            if (distortion.empty())
            {
                return true;
            }
            if (distortion.type() != CV_64FC1 ||
                distortion.channels() != 1 ||
                (distortion.rows != 1 && distortion.cols != 1) ||
                !hasFiniteValues(distortion))
            {
                errorMessage = "Camera distortion coefficients must be a finite CV_64F vector.";
                return false;
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

        bool isValidTransform(const cv::Mat& transform, const std::string& name, std::string& errorMessage)
        {
            if (transform.empty() ||
                transform.rows != 4 ||
                transform.cols != 4 ||
                transform.type() != CV_64FC1 ||
                !hasFiniteValues(transform))
            {
                errorMessage = name + " must be a finite 4x4 CV_64F matrix.";
                return false;
            }

            if (std::abs(transform.at<double>(3, 0)) > kTransformEpsilon ||
                std::abs(transform.at<double>(3, 1)) > kTransformEpsilon ||
                std::abs(transform.at<double>(3, 2)) > kTransformEpsilon ||
                std::abs(transform.at<double>(3, 3) - 1.0) > kTransformEpsilon)
            {
                errorMessage = name + " has an invalid homogeneous bottom row.";
                return false;
            }
            return true;
        }

        cv::Point3d transformPoint(const cv::Mat& transform, const cv::Point3d& point)
        {
            return cv::Point3d(
                transform.at<double>(0, 0) * point.x +
                    transform.at<double>(0, 1) * point.y +
                    transform.at<double>(0, 2) * point.z +
                    transform.at<double>(0, 3),
                transform.at<double>(1, 0) * point.x +
                    transform.at<double>(1, 1) * point.y +
                    transform.at<double>(1, 2) * point.z +
                    transform.at<double>(1, 3),
                transform.at<double>(2, 0) * point.x +
                    transform.at<double>(2, 1) * point.y +
                    transform.at<double>(2, 2) * point.z +
                    transform.at<double>(2, 3));
        }
    }

    bool CommonPlaneProjector::pixelToCommonWorld(
        const CameraRigParams& camera,
        double pixelX,
        double pixelY,
        double heightCompensation,
        cv::Point3d& worldPoint,
        std::string& errorMessage) const
    {
        worldPoint = cv::Point3d();
        errorMessage.clear();

        if (!isFinite(pixelX) || !isFinite(pixelY) || !isFinite(heightCompensation))
        {
            errorMessage = "Pixel coordinates and heightCompensation must be finite.";
            return false;
        }
        if (!isValidIntrinsic(camera, errorMessage) ||
            !isValidDistortion(camera.distortion, errorMessage) ||
            !isValidTransform(camera.transformCommonFromCamera, "T_common_from_camera", errorMessage))
        {
            return false;
        }

        try
        {
            std::vector<cv::Point2d> distortedPoints = { cv::Point2d(pixelX, pixelY) };
            std::vector<cv::Point2d> normalizedPoints;
            cv::undistortPoints(distortedPoints, normalizedPoints, camera.intrinsic, camera.distortion);
            if (normalizedPoints.size() != 1 || !isFinitePoint(normalizedPoints[0]))
            {
                errorMessage = "cv::undistortPoints returned an invalid normalized ray.";
                return false;
            }

            const cv::Point3d rayOriginCommon = transformPoint(camera.transformCommonFromCamera, cv::Point3d(0.0, 0.0, 0.0));
            const cv::Point3d rayPointCamera(normalizedPoints[0].x, normalizedPoints[0].y, 1.0);
            const cv::Point3d rayPointCommon = transformPoint(camera.transformCommonFromCamera, rayPointCamera);
            const cv::Point3d rayDirectionCommon(
                rayPointCommon.x - rayOriginCommon.x,
                rayPointCommon.y - rayOriginCommon.y,
                rayPointCommon.z - rayOriginCommon.z);

            if (!isFinitePoint(rayOriginCommon) || !isFinitePoint(rayDirectionCommon))
            {
                errorMessage = "Camera ray contains non-finite values in CommonWorld.";
                return false;
            }
            if (std::abs(rayDirectionCommon.z) < kParallelEpsilon)
            {
                errorMessage = "Camera ray is nearly parallel to the target plane.";
                return false;
            }

            const double distanceAlongRay = (heightCompensation - rayOriginCommon.z) / rayDirectionCommon.z;
            if (!isFinite(distanceAlongRay) || distanceAlongRay < 0.0)
            {
                errorMessage = "Ray-plane intersection is behind the camera.";
                return false;
            }

            worldPoint = cv::Point3d(
                rayOriginCommon.x + distanceAlongRay * rayDirectionCommon.x,
                rayOriginCommon.y + distanceAlongRay * rayDirectionCommon.y,
                heightCompensation);
            if (!isFinitePoint(worldPoint))
            {
                errorMessage = "Ray-plane intersection produced a non-finite CommonWorld point.";
                return false;
            }
        }
        catch (const cv::Exception& exception)
        {
            errorMessage = std::string("cv::undistortPoints failed: ") + exception.what();
            return false;
        }

        return true;
    }

    bool CommonPlaneProjector::commonWorldToPixel(
        const CameraRigParams& camera,
        const cv::Point3d& worldPoint,
        cv::Point2d& pixelPoint,
        std::string& errorMessage) const
    {
        pixelPoint = cv::Point2d();
        errorMessage.clear();

        if (!isFinitePoint(worldPoint))
        {
            errorMessage = "CommonWorld point must be finite.";
            return false;
        }
        if (!isValidIntrinsic(camera, errorMessage) ||
            !isValidDistortion(camera.distortion, errorMessage) ||
            !isValidTransform(camera.transformCameraFromCommon, "T_camera_from_common", errorMessage))
        {
            return false;
        }

        const cv::Point3d cameraPoint = transformPoint(camera.transformCameraFromCommon, worldPoint);
        if (!isFinitePoint(cameraPoint))
        {
            errorMessage = "Projected camera point contains non-finite values.";
            return false;
        }
        if (cameraPoint.z <= 0.0)
        {
            errorMessage = "CommonWorld point projects to camera Z <= 0.";
            return false;
        }

        try
        {
            const std::vector<cv::Point3d> cameraPoints = { cameraPoint };
            std::vector<cv::Point2d> projectedPoints;
            cv::projectPoints(
                cameraPoints,
                cv::Vec3d(0.0, 0.0, 0.0),
                cv::Vec3d(0.0, 0.0, 0.0),
                camera.intrinsic,
                camera.distortion,
                projectedPoints);

            if (projectedPoints.size() != 1 || !isFinitePoint(projectedPoints[0]))
            {
                errorMessage = "cv::projectPoints returned an invalid pixel point.";
                return false;
            }

            pixelPoint = projectedPoints[0];
        }
        catch (const cv::Exception& exception)
        {
            errorMessage = std::string("cv::projectPoints failed: ") + exception.what();
            return false;
        }

        return true;
    }
}
