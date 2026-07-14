#include "pch.h"

#include "multicam/bundle_adjuster.h"

#include "multicam/observation_graph.h"

#ifdef ERROR
#undef ERROR
#endif

#include <ceres/ceres.h>
#include <ceres/rotation.h>
#include <opencv2/calib3d.hpp>

#include <array>
#include <cmath>
#include <limits>
#include <sstream>

namespace calib::multicam
{
    namespace
    {
        constexpr double kMinProjectionZ = 1.0e-2;
        constexpr double kMinRadialDenominator = 1.0e-12;

        using PoseBlock = std::array<double, 6>;
        using IntrinsicBlock = std::array<double, 4>;
        using DistortionBlock = std::array<double, 8>;

        struct CharucoReprojectionResidual
        {
            CharucoReprojectionResidual(const cv::Point3d& boardPoint, const cv::Point2d& imagePoint)
                : boardPoint_{ boardPoint.x, boardPoint.y, boardPoint.z },
                  imagePoint_{ imagePoint.x, imagePoint.y }
            {
            }

            template <typename T>
            bool operator()(
                const T* const cameraPose,
                const T* const capturePose,
                const T* const intrinsic,
                const T* const distortion,
                T* residual) const
            {
                const T boardPoint[3] = {
                    T(boardPoint_[0]),
                    T(boardPoint_[1]),
                    T(boardPoint_[2])
                };

                T commonPoint[3];
                ceres::AngleAxisRotatePoint(capturePose, boardPoint, commonPoint);
                commonPoint[0] += capturePose[3];
                commonPoint[1] += capturePose[4];
                commonPoint[2] += capturePose[5];

                T cameraPoint[3];
                ceres::AngleAxisRotatePoint(cameraPose, commonPoint, cameraPoint);
                cameraPoint[0] += cameraPose[3];
                cameraPoint[1] += cameraPose[4];
                cameraPoint[2] += cameraPose[5];

                if (cameraPoint[2] <= T(kMinProjectionZ))
                {
                    return false;
                }

                const T x = cameraPoint[0] / cameraPoint[2];
                const T y = cameraPoint[1] / cameraPoint[2];
                const T r2 = x * x + y * y;
                const T r4 = r2 * r2;
                const T r6 = r4 * r2;

                const T k1 = distortion[0];
                const T k2 = distortion[1];
                const T p1 = distortion[2];
                const T p2 = distortion[3];
                const T k3 = distortion[4];
                const T k4 = distortion[5];
                const T k5 = distortion[6];
                const T k6 = distortion[7];

                const T radialNumerator = T(1.0) + k1 * r2 + k2 * r4 + k3 * r6;
                const T radialDenominator = T(1.0) + k4 * r2 + k5 * r4 + k6 * r6;
                if (radialDenominator >= -T(kMinRadialDenominator) &&
                    radialDenominator <= T(kMinRadialDenominator))
                {
                    return false;
                }

                const T radial = radialNumerator / radialDenominator;
                const T xDistorted = x * radial + T(2.0) * p1 * x * y + p2 * (r2 + T(2.0) * x * x);
                const T yDistorted = y * radial + p1 * (r2 + T(2.0) * y * y) + T(2.0) * p2 * x * y;

                const T u = intrinsic[0] * xDistorted + intrinsic[2];
                const T v = intrinsic[1] * yDistorted + intrinsic[3];

                residual[0] = u - T(imagePoint_[0]);
                residual[1] = v - T(imagePoint_[1]);
                return true;
            }

            double boardPoint_[3];
            double imagePoint_[2];
        };

        bool isTransform4x4(const cv::Mat& transform)
        {
            return !transform.empty() &&
                transform.rows == 4 &&
                transform.cols == 4 &&
                transform.type() == CV_64FC1;
        }

        bool isIntrinsicValid(const cv::Mat& intrinsic)
        {
            return !intrinsic.empty() &&
                intrinsic.rows == 3 &&
                intrinsic.cols == 3 &&
                intrinsic.type() == CV_64FC1 &&
                intrinsic.at<double>(0, 0) > std::numeric_limits<double>::epsilon() &&
                intrinsic.at<double>(1, 1) > std::numeric_limits<double>::epsilon();
        }

        bool hasEightDistortionCoefficients(const cv::Mat& distortion)
        {
            return !distortion.empty() &&
                distortion.total() >= 8 &&
                distortion.type() == CV_64FC1 &&
                (distortion.rows == 1 || distortion.cols == 1);
        }

        bool hasFinitePoint(const cv::Point2d& point)
        {
            return std::isfinite(point.x) && std::isfinite(point.y);
        }

        bool hasFinitePoint(const cv::Point3d& point)
        {
            return std::isfinite(point.x) && std::isfinite(point.y) && std::isfinite(point.z);
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
                    if (!std::isfinite(mat.at<double>(row, col)))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        double distortionAt(const cv::Mat& distortion, int index)
        {
            if (distortion.rows == 1)
            {
                return distortion.at<double>(0, index);
            }
            return distortion.at<double>(index, 0);
        }

        cv::Point3d transformPoint(const cv::Mat& transform, const cv::Point3d& point)
        {
            const double x =
                transform.at<double>(0, 0) * point.x +
                transform.at<double>(0, 1) * point.y +
                transform.at<double>(0, 2) * point.z +
                transform.at<double>(0, 3);
            const double y =
                transform.at<double>(1, 0) * point.x +
                transform.at<double>(1, 1) * point.y +
                transform.at<double>(1, 2) * point.z +
                transform.at<double>(1, 3);
            const double z =
                transform.at<double>(2, 0) * point.x +
                transform.at<double>(2, 1) * point.y +
                transform.at<double>(2, 2) * point.z +
                transform.at<double>(2, 3);
            return cv::Point3d(x, y, z);
        }

        PoseBlock transformToPoseBlock(const cv::Mat& transform)
        {
            cv::Mat rotation = transform(cv::Rect(0, 0, 3, 3)).clone();
            cv::Mat rvec;
            cv::Rodrigues(rotation, rvec);

            PoseBlock pose{};
            pose[0] = rvec.at<double>(0, 0);
            pose[1] = rvec.at<double>(1, 0);
            pose[2] = rvec.at<double>(2, 0);
            pose[3] = transform.at<double>(0, 3);
            pose[4] = transform.at<double>(1, 3);
            pose[5] = transform.at<double>(2, 3);
            return pose;
        }

        cv::Mat poseBlockToTransform(const PoseBlock& pose)
        {
            const cv::Mat rvec = (cv::Mat_<double>(3, 1) << pose[0], pose[1], pose[2]);
            cv::Mat rotation;
            cv::Rodrigues(rvec, rotation);

            cv::Mat transform = cv::Mat::eye(4, 4, CV_64F);
            rotation.copyTo(transform(cv::Rect(0, 0, 3, 3)));
            transform.at<double>(0, 3) = pose[3];
            transform.at<double>(1, 3) = pose[4];
            transform.at<double>(2, 3) = pose[5];
            return transform;
        }

        IntrinsicBlock intrinsicToBlock(const cv::Mat& intrinsic)
        {
            return {
                intrinsic.at<double>(0, 0),
                intrinsic.at<double>(1, 1),
                intrinsic.at<double>(0, 2),
                intrinsic.at<double>(1, 2)
            };
        }

        cv::Mat intrinsicBlockToMat(const IntrinsicBlock& intrinsic)
        {
            return (cv::Mat_<double>(3, 3) <<
                intrinsic[0], 0.0, intrinsic[2],
                0.0, intrinsic[1], intrinsic[3],
                0.0, 0.0, 1.0);
        }

        DistortionBlock distortionToBlock(const cv::Mat& distortion)
        {
            DistortionBlock block{};
            for (int index = 0; index < 8; ++index)
            {
                block[index] = distortionAt(distortion, index);
            }
            return block;
        }

        cv::Mat distortionBlockToMat(const DistortionBlock& distortion)
        {
            cv::Mat mat(1, 8, CV_64F);
            for (int index = 0; index < 8; ++index)
            {
                mat.at<double>(0, index) = distortion[index];
            }
            return mat;
        }

        cv::Point2d projectPoint(
            const PoseBlock& cameraPose,
            const PoseBlock& capturePose,
            const IntrinsicBlock& intrinsic,
            const DistortionBlock& distortion,
            const cv::Point3d& boardPoint,
            double& cameraZ)
        {
            double commonPoint[3];
            const double boardPointData[3] = { boardPoint.x, boardPoint.y, boardPoint.z };
            ceres::AngleAxisRotatePoint(capturePose.data(), boardPointData, commonPoint);
            commonPoint[0] += capturePose[3];
            commonPoint[1] += capturePose[4];
            commonPoint[2] += capturePose[5];

            double cameraPoint[3];
            ceres::AngleAxisRotatePoint(cameraPose.data(), commonPoint, cameraPoint);
            cameraPoint[0] += cameraPose[3];
            cameraPoint[1] += cameraPose[4];
            cameraPoint[2] += cameraPose[5];
            cameraZ = cameraPoint[2];

            const double x = cameraPoint[0] / cameraPoint[2];
            const double y = cameraPoint[1] / cameraPoint[2];
            const double r2 = x * x + y * y;
            const double r4 = r2 * r2;
            const double r6 = r4 * r2;

            const double radialNumerator = 1.0 + distortion[0] * r2 + distortion[1] * r4 + distortion[4] * r6;
            const double radialDenominator = 1.0 + distortion[5] * r2 + distortion[6] * r4 + distortion[7] * r6;
            const double radial = radialNumerator / radialDenominator;
            const double xDistorted = x * radial + 2.0 * distortion[2] * x * y + distortion[3] * (r2 + 2.0 * x * x);
            const double yDistorted = y * radial + distortion[2] * (r2 + 2.0 * y * y) + 2.0 * distortion[3] * x * y;

            return cv::Point2d(
                intrinsic[0] * xDistorted + intrinsic[2],
                intrinsic[1] * yDistorted + intrinsic[3]);
        }

        bool validateInput(const BundleAdjustmentInput& input, MultiCameraReport& report, std::string& errorMessage)
        {
            report.cameraCount = static_cast<int>(input.cameras.size());
            report.captureCount = static_cast<int>(input.captures.size());
            report.observationCount = static_cast<int>(input.observations.size());

            if (input.cameras.empty())
            {
                errorMessage = "Bundle adjustment input has no cameras.";
                return false;
            }
            if (input.captures.empty())
            {
                errorMessage = "Bundle adjustment input has no captures.";
                return false;
            }
            if (input.observations.empty())
            {
                errorMessage = "Bundle adjustment input has no observations.";
                return false;
            }

            ObservationGraph graph;
            for (const auto& [cameraId, camera] : input.cameras)
            {
                graph.addCamera(cameraId);
                if (!camera.hasIntrinsic || !isIntrinsicValid(camera.intrinsic) || !hasFiniteValues(camera.intrinsic))
                {
                    errorMessage = "Camera '" + cameraId + "' is missing a valid intrinsic matrix.";
                    return false;
                }
                if (!hasEightDistortionCoefficients(camera.distortion) || !hasFiniteValues(camera.distortion))
                {
                    errorMessage = "Camera '" + cameraId + "' is missing a valid distortion vector with at least 8 coefficients.";
                    return false;
                }
                if (!isTransform4x4(camera.transformCameraFromCommon) || !hasFiniteValues(camera.transformCameraFromCommon))
                {
                    errorMessage = "Camera '" + cameraId + "' is missing T_camera_from_common.";
                    return false;
                }
            }

            for (const auto& [captureId, capture] : input.captures)
            {
                if (!isTransform4x4(capture.transformCommonFromBoard) || !hasFiniteValues(capture.transformCommonFromBoard))
                {
                    errorMessage = "Capture '" + captureId + "' is missing T_common_from_board.";
                    return false;
                }
            }

            int scalarResidualCount = 0;
            for (const auto& observation : input.observations)
            {
                const auto camera = input.cameras.find(observation.cameraId);
                if (camera == input.cameras.end())
                {
                    errorMessage = "Observation references missing camera '" + observation.cameraId + "'.";
                    return false;
                }

                const auto capture = input.captures.find(observation.captureId);
                if (capture == input.captures.end())
                {
                    errorMessage = "Observation references missing capture '" + observation.captureId + "'.";
                    return false;
                }

                if (observation.corners.empty())
                {
                    errorMessage = "Observation for camera '" + observation.cameraId + "' capture '" + observation.captureId + "' has no corners.";
                    return false;
                }

                graph.addObservation(observation);
                scalarResidualCount += static_cast<int>(observation.corners.size()) * 2;

                for (const auto& corner : observation.corners)
                {
                    if (!hasFinitePoint(corner.boardPoint) || !hasFinitePoint(corner.imagePoint))
                    {
                        errorMessage = "Observation for camera '" + observation.cameraId + "' capture '" + observation.captureId + "' has non-finite corner data.";
                        return false;
                    }

                    const cv::Point3d commonPoint = transformPoint(capture->second.transformCommonFromBoard, corner.boardPoint);
                    const cv::Point3d cameraPoint = transformPoint(camera->second.transformCameraFromCommon, commonPoint);
                    if (cameraPoint.z <= kMinProjectionZ)
                    {
                        errorMessage = "Observation projects a board point with non-positive or near-zero camera Z.";
                        return false;
                    }
                }
            }

            const GraphConnectivityResult connectivity = graph.validateConnected();
            report.connectedComponentCount = connectivity.componentCount;
            if (!connectivity.connected)
            {
                errorMessage = "Observation graph is disconnected.";
                return false;
            }

            report.residualCount = scalarResidualCount;
            return true;
        }

        void computeReportErrors(
            const BundleAdjustmentInput& input,
            const std::map<std::string, PoseBlock>& cameraPoses,
            const std::map<std::string, PoseBlock>& capturePoses,
            const std::map<std::string, IntrinsicBlock>& intrinsics,
            const std::map<std::string, DistortionBlock>& distortions,
            double& rmsError,
            double& maxReprojectionError)
        {
            double squaredErrorSum = 0.0;
            int scalarResidualCount = 0;
            maxReprojectionError = 0.0;

            for (const auto& observation : input.observations)
            {
                const auto cameraPose = cameraPoses.find(observation.cameraId);
                const auto capturePose = capturePoses.find(observation.captureId);
                const auto intrinsic = intrinsics.find(observation.cameraId);
                const auto distortion = distortions.find(observation.cameraId);
                if (cameraPose == cameraPoses.end() ||
                    capturePose == capturePoses.end() ||
                    intrinsic == intrinsics.end() ||
                    distortion == distortions.end())
                {
                    continue;
                }

                for (const auto& corner : observation.corners)
                {
                    double cameraZ = 0.0;
                    const cv::Point2d projected = projectPoint(
                        cameraPose->second,
                        capturePose->second,
                        intrinsic->second,
                        distortion->second,
                        corner.boardPoint,
                        cameraZ);

                    const double dx = projected.x - corner.imagePoint.x;
                    const double dy = projected.y - corner.imagePoint.y;
                    const double error = std::sqrt(dx * dx + dy * dy);
                    squaredErrorSum += dx * dx + dy * dy;
                    scalarResidualCount += 2;
                    maxReprojectionError = std::max(maxReprojectionError, error);
                }
            }

            rmsError = scalarResidualCount == 0 ? 0.0 : std::sqrt(squaredErrorSum / static_cast<double>(scalarResidualCount));
        }

        std::map<std::string, double> computeCameraRmsErrors(
            const BundleAdjustmentInput& input,
            const std::map<std::string, PoseBlock>& cameraPoses,
            const std::map<std::string, PoseBlock>& capturePoses,
            const std::map<std::string, IntrinsicBlock>& intrinsics,
            const std::map<std::string, DistortionBlock>& distortions)
        {
            std::map<std::string, double> squaredErrorSums;
            std::map<std::string, int> scalarResidualCounts;
            for (const auto& cameraEntry : input.cameras)
            {
                const std::string& cameraId = cameraEntry.first;
                squaredErrorSums[cameraId] = 0.0;
                scalarResidualCounts[cameraId] = 0;
            }

            for (const auto& observation : input.observations)
            {
                const auto cameraPose = cameraPoses.find(observation.cameraId);
                const auto capturePose = capturePoses.find(observation.captureId);
                const auto intrinsic = intrinsics.find(observation.cameraId);
                const auto distortion = distortions.find(observation.cameraId);
                if (cameraPose == cameraPoses.end() ||
                    capturePose == capturePoses.end() ||
                    intrinsic == intrinsics.end() ||
                    distortion == distortions.end())
                {
                    continue;
                }

                for (const auto& corner : observation.corners)
                {
                    double cameraZ = 0.0;
                    const cv::Point2d projected = projectPoint(
                        cameraPose->second,
                        capturePose->second,
                        intrinsic->second,
                        distortion->second,
                        corner.boardPoint,
                        cameraZ);

                    const double dx = projected.x - corner.imagePoint.x;
                    const double dy = projected.y - corner.imagePoint.y;
                    squaredErrorSums[observation.cameraId] += dx * dx + dy * dy;
                    scalarResidualCounts[observation.cameraId] += 2;
                }
            }

            std::map<std::string, double> rmsErrors;
            for (const auto& [cameraId, squaredErrorSum] : squaredErrorSums)
            {
                const int scalarResidualCount = scalarResidualCounts[cameraId];
                rmsErrors[cameraId] = scalarResidualCount == 0
                    ? 0.0
                    : std::sqrt(squaredErrorSum / static_cast<double>(scalarResidualCount));
            }

            return rmsErrors;
        }
    }

    bool BundleAdjuster::optimize(const BundleAdjustmentInput& input, BundleAdjustmentOutput& output, std::string& errorMessage) const
    {
        output = BundleAdjustmentOutput{};
        errorMessage.clear();

        MultiCameraReport report;
        if (!validateInput(input, report, errorMessage))
        {
            output.report = report;
            return false;
        }

        std::map<std::string, PoseBlock> cameraPoses;
        std::map<std::string, PoseBlock> capturePoses;
        std::map<std::string, IntrinsicBlock> intrinsics;
        std::map<std::string, DistortionBlock> distortions;

        for (const auto& [cameraId, camera] : input.cameras)
        {
            cameraPoses.emplace(cameraId, transformToPoseBlock(camera.transformCameraFromCommon));
            intrinsics.emplace(cameraId, intrinsicToBlock(camera.intrinsic));
            distortions.emplace(cameraId, distortionToBlock(camera.distortion));
        }

        for (const auto& [captureId, capture] : input.captures)
        {
            capturePoses.emplace(captureId, transformToPoseBlock(capture.transformCommonFromBoard));
        }

        computeReportErrors(
            input,
            cameraPoses,
            capturePoses,
            intrinsics,
            distortions,
            report.initialRmsError,
            report.maxReprojectionError);

        std::string referenceCaptureId = input.options.referenceCaptureId;
        if (referenceCaptureId.empty())
        {
            referenceCaptureId = input.captures.begin()->first;
        }

        auto referenceCapturePose = capturePoses.find(referenceCaptureId);
        if (referenceCapturePose == capturePoses.end())
        {
            errorMessage = "Reference capture '" + referenceCaptureId + "' does not exist.";
            output.report = report;
            return false;
        }

        ceres::Problem problem;
        for (auto& [cameraId, cameraPose] : cameraPoses)
        {
            problem.AddParameterBlock(cameraPose.data(), 6);
            problem.AddParameterBlock(intrinsics.at(cameraId).data(), 4);
            problem.AddParameterBlock(distortions.at(cameraId).data(), 8);

            if (!input.options.refineIntrinsics)
            {
                problem.SetParameterBlockConstant(intrinsics.at(cameraId).data());
            }
            if (!input.options.refineDistortion)
            {
                problem.SetParameterBlockConstant(distortions.at(cameraId).data());
            }
        }

        for (auto& [captureId, capturePose] : capturePoses)
        {
            problem.AddParameterBlock(capturePose.data(), 6);
        }
        problem.SetParameterBlockConstant(referenceCapturePose->second.data());

        for (const auto& observation : input.observations)
        {
            auto& cameraPose = cameraPoses.at(observation.cameraId);
            auto& capturePose = capturePoses.at(observation.captureId);
            auto& intrinsic = intrinsics.at(observation.cameraId);
            auto& distortion = distortions.at(observation.cameraId);

            for (const auto& corner : observation.corners)
            {
                ceres::LossFunction* lossFunction = input.options.robustLossScale > 0.0 ?
                    static_cast<ceres::LossFunction*>(new ceres::HuberLoss(input.options.robustLossScale)) :
                    nullptr;

                problem.AddResidualBlock(
                    new ceres::AutoDiffCostFunction<CharucoReprojectionResidual, 2, 6, 6, 4, 8>(
                        new CharucoReprojectionResidual(corner.boardPoint, corner.imagePoint)),
                    lossFunction,
                    cameraPose.data(),
                    capturePose.data(),
                    intrinsic.data(),
                    distortion.data());
            }
        }

        ceres::Solver::Options solverOptions;
        solverOptions.max_num_iterations = input.options.maxIterations > 0 ? input.options.maxIterations : 100;
        solverOptions.linear_solver_type = ceres::DENSE_SCHUR;
        solverOptions.minimizer_progress_to_stdout = false;
        solverOptions.logging_type = ceres::SILENT;

        ceres::Solver::Summary summary;
        ceres::Solve(solverOptions, &problem, &summary);

        computeReportErrors(
            input,
            cameraPoses,
            capturePoses,
            intrinsics,
            distortions,
            report.finalRmsError,
            report.maxReprojectionError);

        const std::map<std::string, double> cameraRmsErrors = computeCameraRmsErrors(
            input,
            cameraPoses,
            capturePoses,
            intrinsics,
            distortions);

        report.converged = summary.termination_type == ceres::CONVERGENCE;
        report.ceresTerminationType = static_cast<int>(summary.termination_type);

        output.cameras = input.cameras;
        output.captures = input.captures;
        output.report = report;

        for (auto& [cameraId, camera] : output.cameras)
        {
            const cv::Mat cameraFromCommon = poseBlockToTransform(cameraPoses.at(cameraId));
            camera.transformCameraFromCommon = cameraFromCommon;
            camera.transformCommonFromCamera = cameraFromCommon.inv();
            camera.intrinsic = intrinsicBlockToMat(intrinsics.at(cameraId));
            camera.distortion = distortionBlockToMat(distortions.at(cameraId));
            const auto rmsError = cameraRmsErrors.find(cameraId);
            if (rmsError != cameraRmsErrors.end())
            {
                camera.rmsError = rmsError->second;
            }
        }

        for (auto& [captureId, capture] : output.captures)
        {
            capture.transformCommonFromBoard = poseBlockToTransform(capturePoses.at(captureId));
            if (captureId == referenceCaptureId)
            {
                capture.fixed = true;
            }
        }

        if (!summary.IsSolutionUsable())
        {
            std::ostringstream message;
            message << "Ceres bundle adjustment failed: " << summary.BriefReport();
            errorMessage = message.str();
            output.report.messages.push_back(errorMessage);
            return false;
        }

        errorMessage.clear();
        return true;
    }
}
