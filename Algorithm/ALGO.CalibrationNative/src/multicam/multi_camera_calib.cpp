#include "pch.h"

#include "multicam/multi_camera_calib.h"

#include "boards/calibration_board_calib_factory.h"
#include "multicam/reference_camera_canvas.h"

#include <opencv2/calib3d.hpp>
#include <opencv2/core/persistence.hpp>
#include <opencv2/imgcodecs.hpp>

#include <algorithm>
#include <cmath>
#include <cctype>
#include <filesystem>
#include <limits>
#include <queue>
#include <set>
#include <sstream>

namespace calib::multicam
{
    namespace
    {
        constexpr const char* kFormatName = "ALGO.CalibrationNative.MultiCamera.v1";
        constexpr const char* kFusedPlaneGridFormatName = "ALGO.CalibrationNative.FusedPlaneGrid.v1";
        constexpr double kTransformEpsilon = 1.0e-7;

        bool isFinite(double value)
        {
            return std::isfinite(value);
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

        bool isNumericNode(const cv::FileNode& node)
        {
            return !node.empty() && (node.isInt() || node.isReal());
        }

        int compareNaturalNumberRun(
            const std::string& left,
            std::size_t& leftIndex,
            const std::string& right,
            std::size_t& rightIndex)
        {
            const std::size_t leftRunStart = leftIndex;
            const std::size_t rightRunStart = rightIndex;
            while (leftIndex < left.size() && std::isdigit(static_cast<unsigned char>(left[leftIndex])))
            {
                ++leftIndex;
            }
            while (rightIndex < right.size() && std::isdigit(static_cast<unsigned char>(right[rightIndex])))
            {
                ++rightIndex;
            }

            std::size_t leftSignificantStart = leftRunStart;
            std::size_t rightSignificantStart = rightRunStart;
            while (leftSignificantStart < leftIndex && left[leftSignificantStart] == '0')
            {
                ++leftSignificantStart;
            }
            while (rightSignificantStart < rightIndex && right[rightSignificantStart] == '0')
            {
                ++rightSignificantStart;
            }

            std::size_t leftSignificantLength = leftIndex - leftSignificantStart;
            std::size_t rightSignificantLength = rightIndex - rightSignificantStart;
            if (leftSignificantLength == 0)
            {
                leftSignificantStart = leftIndex - 1;
                leftSignificantLength = 1;
            }
            if (rightSignificantLength == 0)
            {
                rightSignificantStart = rightIndex - 1;
                rightSignificantLength = 1;
            }

            if (leftSignificantLength != rightSignificantLength)
            {
                return leftSignificantLength < rightSignificantLength ? -1 : 1;
            }
            for (std::size_t offset = 0; offset < leftSignificantLength; ++offset)
            {
                const char leftDigit = left[leftSignificantStart + offset];
                const char rightDigit = right[rightSignificantStart + offset];
                if (leftDigit != rightDigit)
                {
                    return leftDigit < rightDigit ? -1 : 1;
                }
            }

            const std::size_t leftRunLength = leftIndex - leftRunStart;
            const std::size_t rightRunLength = rightIndex - rightRunStart;
            if (leftRunLength != rightRunLength)
            {
                return leftRunLength < rightRunLength ? -1 : 1;
            }
            return 0;
        }

        int compareNatural(const std::string& left, const std::string& right)
        {
            std::size_t leftIndex = 0;
            std::size_t rightIndex = 0;
            while (leftIndex < left.size() && rightIndex < right.size())
            {
                const bool leftDigit = std::isdigit(static_cast<unsigned char>(left[leftIndex])) != 0;
                const bool rightDigit = std::isdigit(static_cast<unsigned char>(right[rightIndex])) != 0;
                if (leftDigit && rightDigit)
                {
                    const int numberCompare = compareNaturalNumberRun(left, leftIndex, right, rightIndex);
                    if (numberCompare != 0)
                    {
                        return numberCompare;
                    }
                    continue;
                }

                if (left[leftIndex] != right[rightIndex])
                {
                    return left[leftIndex] < right[rightIndex] ? -1 : 1;
                }

                ++leftIndex;
                ++rightIndex;
            }

            if (left.size() == right.size())
            {
                return 0;
            }
            return left.size() < right.size() ? -1 : 1;
        }

        std::string selectBestReferenceCaptureId(const ObservationGraph& graph, const std::set<std::string>& captureIds)
        {
            std::string bestCaptureId;
            std::size_t bestCameraCount = 0;
            for (const auto& captureId : captureIds)
            {
                std::set<std::string> observedCameraIds;
                for (const auto& observation : graph.observationsForCapture(captureId))
                {
                    observedCameraIds.insert(observation.cameraId);
                }

                const std::size_t cameraCount = observedCameraIds.size();
                if (bestCaptureId.empty() ||
                    cameraCount > bestCameraCount ||
                    (cameraCount == bestCameraCount && compareNatural(captureId, bestCaptureId) < 0))
                {
                    bestCaptureId = captureId;
                    bestCameraCount = cameraCount;
                }
            }

            return bestCaptureId;
        }

        bool requireTopLevelNode(
            const cv::FileStorage& fs,
            const std::string& name,
            bool expectMap,
            bool expectSequence,
            cv::FileNode& node,
            std::string& errorMessage)
        {
            node = fs[name];
            if (node.empty())
            {
                errorMessage = "Missing required top-level field '" + name + "'.";
                return false;
            }
            if (expectMap && !node.isMap())
            {
                errorMessage = "Top-level field '" + name + "' must be a map.";
                return false;
            }
            if (expectSequence && !node.isSeq())
            {
                errorMessage = "Top-level field '" + name + "' must be a sequence.";
                return false;
            }
            if (expectSequence && node.size() == 0)
            {
                errorMessage = "Top-level sequence '" + name + "' must not be empty.";
                return false;
            }
            return true;
        }

        bool readRequiredString(
            const cv::FileNode& parent,
            const std::string& fieldName,
            std::string& value,
            std::string& errorMessage)
        {
            const cv::FileNode node = parent[fieldName];
            if (node.empty() || !node.isString())
            {
                errorMessage = "Field '" + fieldName + "' must be a string.";
                return false;
            }
            node >> value;
            if (value.empty())
            {
                errorMessage = "Field '" + fieldName + "' must not be empty.";
                return false;
            }
            return true;
        }

        bool readRequiredInt(
            const cv::FileNode& parent,
            const std::string& fieldName,
            int& value,
            std::string& errorMessage)
        {
            const cv::FileNode node = parent[fieldName];
            if (node.empty() || !node.isInt())
            {
                errorMessage = "Field '" + fieldName + "' must be an integer.";
                return false;
            }
            value = static_cast<int>(node);
            return true;
        }

        bool readRequiredDouble(
            const cv::FileNode& parent,
            const std::string& fieldName,
            double& value,
            std::string& errorMessage)
        {
            const cv::FileNode node = parent[fieldName];
            if (!isNumericNode(node))
            {
                errorMessage = "Field '" + fieldName + "' must be numeric.";
                return false;
            }
            value = static_cast<double>(node);
            if (!isFinite(value))
            {
                errorMessage = "Field '" + fieldName + "' must be finite.";
                return false;
            }
            return true;
        }

        bool readOptionalBool(const cv::FileNode& parent, const std::string& fieldName, bool& value, std::string& errorMessage)
        {
            const cv::FileNode node = parent[fieldName];
            if (node.empty())
            {
                return true;
            }
            if (!node.isInt())
            {
                errorMessage = "Field '" + fieldName + "' must be encoded as 0 or 1.";
                return false;
            }
            value = static_cast<int>(node) != 0;
            return true;
        }

        bool readOptionalInt(const cv::FileNode& parent, const std::string& fieldName, int& value, std::string& errorMessage)
        {
            const cv::FileNode node = parent[fieldName];
            if (node.empty())
            {
                return true;
            }
            if (!node.isInt())
            {
                errorMessage = "Field '" + fieldName + "' must be an integer.";
                return false;
            }
            value = static_cast<int>(node);
            return true;
        }

        bool readOptionalDouble(const cv::FileNode& parent, const std::string& fieldName, double& value, std::string& errorMessage)
        {
            const cv::FileNode node = parent[fieldName];
            if (node.empty())
            {
                return true;
            }
            if (!isNumericNode(node))
            {
                errorMessage = "Field '" + fieldName + "' must be numeric.";
                return false;
            }
            value = static_cast<double>(node);
            if (!isFinite(value))
            {
                errorMessage = "Field '" + fieldName + "' must be finite.";
                return false;
            }
            return true;
        }

        bool readRequiredMatrix(const cv::FileNode& parent, const std::string& fieldName, cv::Mat& matrix, std::string& errorMessage)
        {
            const cv::FileNode node = parent[fieldName];
            if (node.empty())
            {
                errorMessage = "Missing matrix field '" + fieldName + "'.";
                return false;
            }
            node >> matrix;
            if (matrix.empty())
            {
                errorMessage = "Matrix field '" + fieldName + "' is empty.";
                return false;
            }
            if (matrix.type() != CV_64F)
            {
                matrix.convertTo(matrix, CV_64F);
            }
            if (!hasFiniteValues(matrix))
            {
                errorMessage = "Matrix field '" + fieldName + "' must be finite CV_64F.";
                return false;
            }
            return true;
        }

        bool readRequiredHomography(const cv::FileNode& parent, const std::string& fieldName, cv::Mat& matrix, std::string& errorMessage)
        {
            if (!readRequiredMatrix(parent, fieldName, matrix, errorMessage))
            {
                return false;
            }
            if (matrix.rows != 3 || matrix.cols != 3)
            {
                errorMessage = "Homography field '" + fieldName + "' must be 3x3.";
                return false;
            }
            return true;
        }

        bool readOptionalValidMask(const cv::FileNode& parent, cv::Mat& validMask, int rows, int cols, std::string& errorMessage)
        {
            const cv::FileNode node = parent["validMask"];
            if (node.empty())
            {
                validMask.release();
                return true;
            }
            node >> validMask;
            if (validMask.empty() || validMask.rows != rows || validMask.cols != cols || validMask.type() != CV_8U)
            {
                errorMessage = "Residual grid validMask must be CV_8U with gridRows x gridCols dimensions.";
                return false;
            }
            return true;
        }

        bool readResidualGrid(
            const cv::FileNode& parent,
            ResidualGridDomain domain,
            const std::string& originXField,
            const std::string& originYField,
            const std::string& stepXField,
            const std::string& stepYField,
            const std::string& dxField,
            const std::string& dyField,
            ResidualGrid& grid,
            std::string& errorMessage)
        {
            if (parent.empty() || !parent.isMap())
            {
                errorMessage = "Residual grid node is missing or not a map.";
                return false;
            }

            std::string type;
            std::string interpolation;
            if (!readRequiredString(parent, "type", type, errorMessage) ||
                !readRequiredString(parent, "interpolation", interpolation, errorMessage))
            {
                return false;
            }
            if (type != "GRID_LUT" || interpolation != "BILINEAR")
            {
                errorMessage = "Residual grid must use type GRID_LUT and interpolation BILINEAR.";
                return false;
            }

            grid = ResidualGrid();
            grid.domain = domain;
            grid.enabled = true;
            if (!readRequiredDouble(parent, originXField, grid.originX, errorMessage) ||
                !readRequiredDouble(parent, originYField, grid.originY, errorMessage) ||
                !readRequiredDouble(parent, stepXField, grid.stepX, errorMessage) ||
                !readRequiredDouble(parent, stepYField, grid.stepY, errorMessage) ||
                !readRequiredInt(parent, "gridCols", grid.cols, errorMessage) ||
                !readRequiredInt(parent, "gridRows", grid.rows, errorMessage) ||
                !readRequiredMatrix(parent, dxField, grid.dx, errorMessage) ||
                !readRequiredMatrix(parent, dyField, grid.dy, errorMessage))
            {
                return false;
            }
            if (grid.dx.rows != grid.rows || grid.dx.cols != grid.cols ||
                grid.dy.rows != grid.rows || grid.dy.cols != grid.cols)
            {
                errorMessage = "Residual grid dx/dy dimensions must match gridRows/gridCols.";
                return false;
            }
            return readOptionalValidMask(parent, grid.validMask, grid.rows, grid.cols, errorMessage);
        }

        bool isValidBoardParams(const CalibrationBoardParams& params, std::string& errorMessage)
        {
            switch (params.type)
            {
            case CHARUCO:
                if (params.width <= 0 || params.height <= 0)
                {
                    errorMessage = "Charuco board width and height must be positive.";
                    return false;
                }
                if (!isFinite(params.squareSize) || params.squareSize <= 0.0)
                {
                    errorMessage = "Charuco squareSize must be positive.";
                    return false;
                }
                if (!isFinite(params.markerSize) || params.markerSize <= 0.0)
                {
                    errorMessage = "Charuco markerSize must be positive.";
                    return false;
                }
                if (params.dictionaryId < 0)
                {
                    errorMessage = "Charuco dictionaryId must be non-negative.";
                    return false;
                }
                if (params.squareSizePixel > 0.0 && params.markerSizePixel > 0.0 &&
                    params.markerSizePixel >= params.squareSizePixel)
                {
                    errorMessage = "Charuco markerSizePixel must be less than squareSizePixel when both are positive.";
                    return false;
                }
                return true;
            case CHESSBOARD:
                if (params.width <= 0 || params.height <= 0)
                {
                    errorMessage = "Chessboard width and height must be positive.";
                    return false;
                }
                if (!isFinite(params.squareSize) || params.squareSize <= 0.0)
                {
                    errorMessage = "Chessboard squareSize must be positive.";
                    return false;
                }
                return true;
            case CIRCLES_GRID:
                errorMessage = "Circles grid calibration not yet implemented.";
                return false;
            case ASYMMETRIC_CIRCLES_GRID:
                errorMessage = "Asymmetric circles grid calibration not yet implemented.";
                return false;
            default:
                errorMessage = "Unsupported calibration board type.";
                return false;
            }
        }

        std::string anchorModeToString(AnchorMode mode)
        {
            switch (mode)
            {
            case AnchorMode::BoardPose:
                return "BOARD_POSE";
            case AnchorMode::Camera:
                return "CAMERA";
            case AnchorMode::External:
                return "EXTERNAL";
            default:
                return "BOARD_POSE";
            }
        }

        AnchorMode anchorModeFromString(const std::string& value)
        {
            if (value == "CAMERA")
            {
                return AnchorMode::Camera;
            }
            if (value == "EXTERNAL")
            {
                return AnchorMode::External;
            }
            return AnchorMode::BoardPose;
        }

        CalibrationBoardType boardTypeFromNode(const cv::FileNode& node)
        {
            if (node.isString())
            {
                std::string value;
                node >> value;
                if (value == "PIXEL_RATIO")
                {
                    return PIXEL_RATIO;
                }
                if (value == "CHARUCO")
                {
                    return CHARUCO;
                }
                if (value == "CHESSBOARD")
                {
                    return CHESSBOARD;
                }
                if (value == "CIRCLES_GRID")
                {
                    return CIRCLES_GRID;
                }
                if (value == "ASYMMETRIC_CIRCLES_GRID")
                {
                    return ASYMMETRIC_CIRCLES_GRID;
                }
                return BOARD_UNKNOWN;
            }
            if (node.isInt())
            {
                return static_cast<CalibrationBoardType>(static_cast<int>(node));
            }
            return BOARD_UNKNOWN;
        }

        std::string boardTypeToString(CalibrationBoardType type)
        {
            switch (type)
            {
            case CHARUCO:
                return "CHARUCO";
            case CHESSBOARD:
                return "CHESSBOARD";
            case CIRCLES_GRID:
                return "CIRCLES_GRID";
            case ASYMMETRIC_CIRCLES_GRID:
                return "ASYMMETRIC_CIRCLES_GRID";
            default:
                return "BOARD_UNKNOWN";
            }
        }

        bool isYamlFilePath(const std::filesystem::path& path)
        {
            std::string extension = path.extension().string();
            std::transform(extension.begin(), extension.end(), extension.begin(),
                [](unsigned char ch) { return static_cast<char>(std::tolower(ch)); });
            return extension == ".yaml" || extension == ".yml";
        }

        std::string boardTypeToFileSegment(CalibrationBoardType type)
        {
            switch (type)
            {
            case PIXEL_RATIO:
                return "PixelRatio";
            case CHESSBOARD:
                return "Chessboard";
            case CHARUCO:
                return "Charuco";
            default:
                return "Board";
            }
        }

        std::string sanitizeFileNameSegment(const std::string& value, const std::string& fallback)
        {
            static const std::string invalidChars = "<>:\"/\\|?*";

            std::string sanitized;
            sanitized.reserve(value.size());
            for (char ch : value)
            {
                unsigned char uch = static_cast<unsigned char>(ch);
                sanitized.push_back(invalidChars.find(ch) != std::string::npos || uch < 32 ? '_' : ch);
            }

            while (!sanitized.empty() && std::isspace(static_cast<unsigned char>(sanitized.front())))
            {
                sanitized.erase(sanitized.begin());
            }
            while (!sanitized.empty() && std::isspace(static_cast<unsigned char>(sanitized.back())))
            {
                sanitized.pop_back();
            }

            return sanitized.empty() ? fallback : sanitized;
        }

        cv::Mat normalizeDistortion(const cv::Mat& distortion)
        {
            cv::Mat normalized = cv::Mat::zeros(1, 8, CV_64F);
            if (distortion.empty())
            {
                return normalized;
            }

            cv::Mat converted;
            distortion.convertTo(converted, CV_64F);
            cv::Mat flat = converted.reshape(1, 1);
            const int count = std::min(8, static_cast<int>(flat.total()));
            for (int index = 0; index < count; ++index)
            {
                normalized.at<double>(0, index) = flat.at<double>(0, index);
            }
            return normalized;
        }

        cv::Mat transformFromRvecTvec(const cv::Mat& rvec, const cv::Mat& tvec)
        {
            cv::Mat rotation;
            cv::Rodrigues(rvec, rotation);

            cv::Mat transform = cv::Mat::eye(4, 4, CV_64F);
            rotation.copyTo(transform(cv::Rect(0, 0, 3, 3)));
            transform.at<double>(0, 3) = tvec.at<double>(0, 0);
            transform.at<double>(1, 3) = tvec.at<double>(1, 0);
            transform.at<double>(2, 3) = tvec.at<double>(2, 0);
            return transform;
        }

        bool isTransform4x4(const cv::Mat& transform)
        {
            return !transform.empty() &&
                transform.rows == 4 &&
                transform.cols == 4 &&
                transform.type() == CV_64FC1 &&
                hasFiniteValues(transform) &&
                std::abs(transform.at<double>(3, 0)) <= kTransformEpsilon &&
                std::abs(transform.at<double>(3, 1)) <= kTransformEpsilon &&
                std::abs(transform.at<double>(3, 2)) <= kTransformEpsilon &&
                std::abs(transform.at<double>(3, 3) - 1.0) <= kTransformEpsilon;
        }

        bool validateTransform(const cv::Mat& transform, const std::string& name, std::string& errorMessage)
        {
            if (!isTransform4x4(transform))
            {
                errorMessage = name + " must be a finite 4x4 CV_64F matrix with homogeneous bottom row [0 0 0 1].";
                return false;
            }
            return true;
        }

        bool invertTransformChecked(
            const cv::Mat& transform,
            const std::string& sourceName,
            cv::Mat& inverse,
            std::string& errorMessage)
        {
            if (!validateTransform(transform, sourceName, errorMessage))
            {
                return false;
            }

            try
            {
                const double determinant = cv::invert(transform, inverse, cv::DECOMP_LU);
                if (std::abs(determinant) <= std::numeric_limits<double>::epsilon() ||
                    !validateTransform(inverse, "inverse of " + sourceName, errorMessage))
                {
                    errorMessage = sourceName + " must be invertible.";
                    return false;
                }
            }
            catch (const cv::Exception& exception)
            {
                errorMessage = "Failed to invert " + sourceName + ": " + exception.what();
                return false;
            }
            return true;
        }

        bool areInverseTransforms(const cv::Mat& commonFromCamera, const cv::Mat& cameraFromCommon)
        {
            try
            {
                const cv::Mat identity = cv::Mat::eye(4, 4, CV_64F);
                return cv::norm(commonFromCamera * cameraFromCommon - identity, cv::NORM_INF) <= kTransformEpsilon &&
                    cv::norm(cameraFromCommon * commonFromCamera - identity, cv::NORM_INF) <= kTransformEpsilon;
            }
            catch (const cv::Exception&)
            {
                return false;
            }
        }

        bool validateIntrinsic(const cv::Mat& intrinsic, const std::string& cameraId, std::string& errorMessage)
        {
            if (intrinsic.empty() ||
                intrinsic.rows != 3 ||
                intrinsic.cols != 3 ||
                intrinsic.type() != CV_64FC1 ||
                !hasFiniteValues(intrinsic) ||
                intrinsic.at<double>(0, 0) <= std::numeric_limits<double>::epsilon() ||
                intrinsic.at<double>(1, 1) <= std::numeric_limits<double>::epsilon())
            {
                errorMessage = "Camera '" + cameraId + "' has an invalid intrinsic matrix.";
                return false;
            }
            return true;
        }

        bool validateDistortion(const cv::Mat& distortion, const std::string& cameraId, std::string& errorMessage)
        {
            if (distortion.empty())
            {
                errorMessage = "Camera '" + cameraId + "' is missing distortion coefficients.";
                return false;
            }
            if (distortion.type() != CV_64FC1 ||
                distortion.channels() != 1 ||
                (distortion.rows != 1 && distortion.cols != 1) ||
                !hasFiniteValues(distortion))
            {
                errorMessage = "Camera '" + cameraId + "' distortion coefficients must be a finite CV_64F vector.";
                return false;
            }

            const int coefficientCount = distortion.rows * distortion.cols;
            if (coefficientCount != 4 &&
                coefficientCount != 5 &&
                coefficientCount != 8 &&
                coefficientCount != 12 &&
                coefficientCount != 14)
            {
                errorMessage = "Camera '" + cameraId + "' distortion coefficients must contain 4, 5, 8, 12, or 14 values.";
                return false;
            }
            return true;
        }

        bool validateCameraSerializable(const CameraRigParams& camera, std::string& errorMessage)
        {
            if (camera.cameraId.empty())
            {
                errorMessage = "Camera entry is missing cameraId.";
                return false;
            }
            if (camera.imageSize.width <= 0 || camera.imageSize.height <= 0)
            {
                errorMessage = "Camera '" + camera.cameraId + "' has invalid image size.";
                return false;
            }
            if (!camera.hasIntrinsic)
            {
                errorMessage = "Camera '" + camera.cameraId + "' is missing intrinsic parameters.";
                return false;
            }
            if (!validateIntrinsic(camera.intrinsic, camera.cameraId, errorMessage) ||
                !validateDistortion(camera.distortion, camera.cameraId, errorMessage) ||
                !validateTransform(camera.transformCommonFromCamera, "Camera '" + camera.cameraId + "' T_common_from_camera", errorMessage) ||
                !validateTransform(camera.transformCameraFromCommon, "Camera '" + camera.cameraId + "' T_camera_from_common", errorMessage))
            {
                return false;
            }
            if (!areInverseTransforms(camera.transformCommonFromCamera, camera.transformCameraFromCommon))
            {
                errorMessage = "Camera '" + camera.cameraId + "' transforms are inconsistent.";
                return false;
            }
            return true;
        }

        bool validateCaptureSerializable(const CapturePose& capture, std::string& errorMessage)
        {
            if (capture.captureId.empty())
            {
                errorMessage = "Capture entry is missing captureId.";
                return false;
            }
            return validateTransform(capture.transformCommonFromBoard, "Capture '" + capture.captureId + "' T_common_from_board", errorMessage);
        }

        bool validateReport(const MultiCameraReport& report, std::string& errorMessage)
        {
            if (report.cameraCount < 0 ||
                report.captureCount < 0 ||
                report.observationCount < 0 ||
                report.residualCount < 0 ||
                report.connectedComponentCount < 0 ||
                !isFinite(report.initialRmsError) ||
                !isFinite(report.finalRmsError) ||
                !isFinite(report.maxReprojectionError))
            {
                errorMessage = "Loaded report contains invalid values.";
                return false;
            }
            return true;
        }

        std::vector<BoardObservation> collectObservations(const ObservationGraph& graph)
        {
            std::vector<BoardObservation> observations;
            for (const auto& [cameraId, cameraObservations] : graph.observationsByCamera())
            {
                observations.insert(observations.end(), cameraObservations.begin(), cameraObservations.end());
            }
            return observations;
        }

        void rebuildGraph(
            ObservationGraph& graph,
            const std::map<std::string, CameraRigParams>& cameras,
            const std::vector<BoardObservation>& observations)
        {
            graph.clear();
            for (const auto& [cameraId, camera] : cameras)
            {
                graph.addCamera(cameraId);
            }
            for (const auto& observation : observations)
            {
                graph.addObservation(observation);
            }
        }

        double computeReprojectionRms(
            const BoardObservation& observation,
            const CameraRigParams& camera,
            const cv::Mat& rvec,
            const cv::Mat& tvec)
        {
            std::vector<cv::Point3d> boardPoints;
            std::vector<cv::Point2d> imagePoints;
            boardPoints.reserve(observation.corners.size());
            imagePoints.reserve(observation.corners.size());
            for (const auto& corner : observation.corners)
            {
                boardPoints.push_back(corner.boardPoint);
                imagePoints.push_back(corner.imagePoint);
            }

            std::vector<cv::Point2d> projectedPoints;
            cv::projectPoints(boardPoints, rvec, tvec, camera.intrinsic, camera.distortion, projectedPoints);

            double squaredErrorSum = 0.0;
            int scalarResidualCount = 0;
            for (std::size_t index = 0; index < projectedPoints.size(); ++index)
            {
                const double dx = projectedPoints[index].x - imagePoints[index].x;
                const double dy = projectedPoints[index].y - imagePoints[index].y;
                squaredErrorSum += dx * dx + dy * dy;
                scalarResidualCount += 2;
            }
            return scalarResidualCount == 0 ? 0.0 : std::sqrt(squaredErrorSum / static_cast<double>(scalarResidualCount));
        }

        void writeReport(cv::FileStorage& fs, const MultiCameraReport& report)
        {
            fs << "report" << "{"
                << "cameraCount" << report.cameraCount
                << "captureCount" << report.captureCount
                << "observationCount" << report.observationCount
                << "residualCount" << report.residualCount
                << "initialRmsError" << report.initialRmsError
                << "finalRmsError" << report.finalRmsError
                << "maxReprojectionError" << report.maxReprojectionError
                << "connectedComponentCount" << report.connectedComponentCount
                << "converged" << (report.converged ? 1 : 0)
                << "ceresTerminationType" << report.ceresTerminationType;
            if (!report.messages.empty())
            {
                fs << "messages" << "[";
                for (const auto& message : report.messages)
                {
                    fs << message;
                }
                fs << "]";
            }
            fs << "}";
        }

        void writeResidualGrid(
            cv::FileStorage& fs,
            const char* name,
            const ResidualGrid& grid,
            const char* originXName,
            const char* originYName,
            const char* stepXName,
            const char* stepYName,
            const char* dxName,
            const char* dyName)
        {
            fs << name << "{"
                << "type" << "GRID_LUT"
                << "interpolation" << "BILINEAR"
                << originXName << grid.originX
                << originYName << grid.originY
                << stepXName << grid.stepX
                << stepYName << grid.stepY
                << "gridCols" << grid.cols
                << "gridRows" << grid.rows
                << dxName << grid.dx
                << dyName << grid.dy;
            if (!grid.validMask.empty())
            {
                fs << "validMask" << grid.validMask;
            }
            fs << "}";
        }

        bool applyHomography(const cv::Mat& homography, const cv::Point2d& input, cv::Point2d& output)
        {
            if (homography.empty() || homography.rows != 3 || homography.cols != 3 || homography.type() != CV_64F)
            {
                return false;
            }

            const double x =
                homography.at<double>(0, 0) * input.x +
                homography.at<double>(0, 1) * input.y +
                homography.at<double>(0, 2);
            const double y =
                homography.at<double>(1, 0) * input.x +
                homography.at<double>(1, 1) * input.y +
                homography.at<double>(1, 2);
            const double w =
                homography.at<double>(2, 0) * input.x +
                homography.at<double>(2, 1) * input.y +
                homography.at<double>(2, 2);
            if (!isFinite(x) || !isFinite(y) || !isFinite(w) || std::abs(w) < 1.0e-12)
            {
                return false;
            }

            output = cv::Point2d(x / w, y / w);
            return isFinite(output.x) && isFinite(output.y);
        }

        double estimateWorldUnitsPerPixel(const FusedPlaneGridModel& model)
        {
            if (model.HCanvasToBoard.empty() || model.canvasWidth <= 1 || model.canvasHeight <= 1)
            {
                return 0.0;
            }

            const cv::Point2d center(
                static_cast<double>(model.canvasWidth - 1) * 0.5,
                static_cast<double>(model.canvasHeight - 1) * 0.5);
            const cv::Point2d xNeighbor(center.x + 1.0 <= model.canvasWidth - 1 ? center.x + 1.0 : center.x - 1.0, center.y);
            const cv::Point2d yNeighbor(center.x, center.y + 1.0 <= model.canvasHeight - 1 ? center.y + 1.0 : center.y - 1.0);

            cv::Point2d centerWorld;
            cv::Point2d xWorld;
            cv::Point2d yWorld;
            if (!applyHomography(model.HCanvasToBoard, center, centerWorld) ||
                !applyHomography(model.HCanvasToBoard, xNeighbor, xWorld) ||
                !applyHomography(model.HCanvasToBoard, yNeighbor, yWorld))
            {
                return 0.0;
            }

            const double dx = cv::norm(xWorld - centerWorld) / std::abs(xNeighbor.x - center.x);
            const double dy = cv::norm(yWorld - centerWorld) / std::abs(yNeighbor.y - center.y);
            if (!isFinite(dx) || !isFinite(dy) || dx <= 0.0 || dy <= 0.0)
            {
                return 0.0;
            }
            return (dx + dy) * 0.5;
        }

        bool computeCanvasWorldBounds(
            const FusedPlaneGridModel& model,
            double& minWorldX,
            double& minWorldY,
            double& maxWorldX,
            double& maxWorldY,
            std::string& errorMessage)
        {
            const std::vector<cv::Point2d> canvasCorners = {
                cv::Point2d(0.0, 0.0),
                cv::Point2d(static_cast<double>(model.canvasWidth - 1), 0.0),
                cv::Point2d(static_cast<double>(model.canvasWidth - 1), static_cast<double>(model.canvasHeight - 1)),
                cv::Point2d(0.0, static_cast<double>(model.canvasHeight - 1))
            };

            bool hasBounds = false;
            for (const cv::Point2d& canvasCorner : canvasCorners)
            {
                cv::Point2d worldPoint;
                if (!applyHomography(model.HCanvasToBoard, canvasCorner, worldPoint))
                {
                    errorMessage = "Failed to compute fused canvas world bounds.";
                    return false;
                }

                if (!hasBounds)
                {
                    minWorldX = worldPoint.x;
                    minWorldY = worldPoint.y;
                    maxWorldX = worldPoint.x;
                    maxWorldY = worldPoint.y;
                    hasBounds = true;
                }
                else
                {
                    minWorldX = std::min(minWorldX, worldPoint.x);
                    minWorldY = std::min(minWorldY, worldPoint.y);
                    maxWorldX = std::max(maxWorldX, worldPoint.x);
                    maxWorldY = std::max(maxWorldY, worldPoint.y);
                }
            }

            if (!hasBounds ||
                !isFinite(minWorldX) || !isFinite(minWorldY) ||
                !isFinite(maxWorldX) || !isFinite(maxWorldY) ||
                maxWorldX <= minWorldX || maxWorldY <= minWorldY)
            {
                errorMessage = "Fused canvas world bounds are invalid.";
                return false;
            }
            return true;
        }

        void writeSingleCompatibility(cv::FileStorage& fs, const FusedPlaneGridModel& model)
        {
            cv::Point2d canvasOriginWorld;
            const bool hasCanvasOriginWorld = applyHomography(model.HCanvasToBoard, cv::Point2d(0.0, 0.0), canvasOriginWorld);

            fs << "singleCompatibility" << "{"
                << "format" << kFusedPlaneGridFormatName
                << "modelType" << "FUSED_PLANE_GRID"
                << "virtualCameraId" << model.virtualCameraId
                << "coordinateFrame" << model.coordinateFrame
                << "heightCompensation" << model.heightCompensation
                << "canvas" << "{"
                << "width" << model.canvasWidth
                << "height" << model.canvasHeight
                << "originWorldX" << (hasCanvasOriginWorld ? canvasOriginWorld.x : 0.0)
                << "originWorldY" << (hasCanvasOriginWorld ? canvasOriginWorld.y : 0.0)
                << "worldUnitsPerPixel" << estimateWorldUnitsPerPixel(model)
                << "}"
                << "H_canvas_to_board" << model.HCanvasToBoard
                << "H_board_to_canvas" << model.HBoardToCanvas;

            writeResidualGrid(
                fs,
                "canvasToBoardResidual",
                model.canvasToBoardResidual,
                "gridOriginPixelX",
                "gridOriginPixelY",
                "gridStepPixelX",
                "gridStepPixelY",
                "dxWorld",
                "dyWorld");
            writeResidualGrid(
                fs,
                "boardToCanvasResidual",
                model.boardToCanvasResidual,
                "gridOriginWorldX",
                "gridOriginWorldY",
                "gridStepWorldX",
                "gridStepWorldY",
                "dxPixel",
                "dyPixel");
            fs << "}";
        }

        bool readSingleCompatibility(const cv::FileStorage& fs, FusedPlaneGridModel& model, std::string& errorMessage)
        {
            const cv::FileNode node = fs["singleCompatibility"];
            if (node.empty())
            {
                return true;
            }
            if (!node.isMap())
            {
                errorMessage = "singleCompatibility must be a map.";
                return false;
            }

            std::string format;
            std::string modelType;
            if (!readRequiredString(node, "format", format, errorMessage) ||
                !readRequiredString(node, "modelType", modelType, errorMessage))
            {
                return false;
            }
            if (format != kFusedPlaneGridFormatName || modelType != "FUSED_PLANE_GRID")
            {
                errorMessage = "Unsupported singleCompatibility fused-plane format or modelType.";
                return false;
            }

            FusedPlaneGridModel loadedModel;
            if (!readRequiredString(node, "virtualCameraId", loadedModel.virtualCameraId, errorMessage) ||
                !readRequiredString(node, "coordinateFrame", loadedModel.coordinateFrame, errorMessage) ||
                !readRequiredDouble(node, "heightCompensation", loadedModel.heightCompensation, errorMessage))
            {
                return false;
            }

            const cv::FileNode canvasNode = node["canvas"];
            if (canvasNode.empty() || !canvasNode.isMap())
            {
                errorMessage = "singleCompatibility.canvas must be a map.";
                return false;
            }
            if (!readRequiredInt(canvasNode, "width", loadedModel.canvasWidth, errorMessage) ||
                !readRequiredInt(canvasNode, "height", loadedModel.canvasHeight, errorMessage) ||
                !readRequiredHomography(node, "H_canvas_to_board", loadedModel.HCanvasToBoard, errorMessage) ||
                !readRequiredHomography(node, "H_board_to_canvas", loadedModel.HBoardToCanvas, errorMessage))
            {
                return false;
            }
            if (!readResidualGrid(
                    node["canvasToBoardResidual"],
                    ResidualGridDomain::CanvasPixel,
                    "gridOriginPixelX",
                    "gridOriginPixelY",
                    "gridStepPixelX",
                    "gridStepPixelY",
                    "dxWorld",
                    "dyWorld",
                    loadedModel.canvasToBoardResidual,
                    errorMessage) ||
                !readResidualGrid(
                    node["boardToCanvasResidual"],
                    ResidualGridDomain::BoardWorld,
                    "gridOriginWorldX",
                    "gridOriginWorldY",
                    "gridStepWorldX",
                    "gridStepWorldY",
                    "dxPixel",
                    "dyPixel",
                    loadedModel.boardToCanvasResidual,
                    errorMessage))
            {
                return false;
            }

            if (!loadedModel.validate(errorMessage))
            {
                return false;
            }
            model = loadedModel;
            return true;
        }

        std::string disconnectedGraphMessage(const GraphConnectivityResult& connectivity)
        {
            std::ostringstream message;
            message << "Observation graph is disconnected. componentCount=" << connectivity.componentCount;
            if (!connectivity.disconnectedCameraIds.empty())
            {
                message << " disconnectedCameras=";
                for (const auto& cameraId : connectivity.disconnectedCameraIds)
                {
                    message << cameraId << " ";
                }
            }
            if (!connectivity.disconnectedCaptureIds.empty())
            {
                message << " disconnectedCaptures=";
                for (const auto& captureId : connectivity.disconnectedCaptureIds)
                {
                    message << captureId << " ";
                }
            }
            return message.str();
        }
    }

    MultiCameraCalibrationFramework::MultiCameraCalibrationFramework() = default;

    bool MultiCameraCalibrationFramework::hasSingleCompatibility() const
    {
        return singleCompatibility_.has_value();
    }

    bool MultiCameraCalibrationFramework::setMeasurementPlaneParams(
        const MeasurementPlaneParams& params,
        std::string& errorMessage)
    {
        if (!isFinite(params.heightCompensation))
        {
            errorMessage = "heightCompensation must be finite.";
            return false;
        }

        measurementPlaneParams_ = params;
        return true;
    }

    MeasurementPlaneParams MultiCameraCalibrationFramework::getMeasurementPlaneParams() const
    {
        return measurementPlaneParams_;
    }

    bool MultiCameraCalibrationFramework::setBoardParams(const CalibrationBoardParams& params, std::string& errorMessage)
    {
        errorMessage.clear();
        if (!isValidBoardParams(params, errorMessage))
        {
            return false;
        }

        boardParams_ = params;
        hasBoardParams_ = true;
        boardCalibrator_ = calib::boards::createCalibrationBoardCalibrator(params.type, errorMessage);
        if (!boardCalibrator_)
        {
            return false;
        }
        isReady_ = false;
        return true;
    }

    void MultiCameraCalibrationFramework::setOptions(const MultiCameraOptions& options)
    {
        options_ = options;
        if (options_.maxIterations <= 0)
        {
            options_.maxIterations = 100;
        }
    }

    bool MultiCameraCalibrationFramework::addCamera(const std::string& cameraId, int imageWidth, int imageHeight, std::string& errorMessage)
    {
        errorMessage.clear();
        if (cameraId.empty())
        {
            errorMessage = "cameraId must not be empty.";
            return false;
        }
        if (imageWidth <= 0 || imageHeight <= 0)
        {
            errorMessage = "Camera imageWidth and imageHeight must be positive.";
            return false;
        }
        if (cameras_.find(cameraId) != cameras_.end())
        {
            errorMessage = "Duplicate cameraId '" + cameraId + "'.";
            return false;
        }

        CameraRigParams camera;
        camera.cameraId = cameraId;
        camera.imageSize = cv::Size(imageWidth, imageHeight);
        camera.distortion = cv::Mat::zeros(1, 8, CV_64F);
        cameras_.emplace(cameraId, camera);
        graph_.addCamera(cameraId);
        isReady_ = false;
        return true;
    }

    bool MultiCameraCalibrationFramework::setInitialCameraParams(
        const std::string& cameraId,
        const CameraParams& params,
        bool fixedIntrinsic,
        std::string& errorMessage)
    {
        errorMessage.clear();
        const auto found = cameras_.find(cameraId);
        if (found == cameras_.end())
        {
            errorMessage = "Camera '" + cameraId + "' does not exist.";
            return false;
        }

        cv::Mat intrinsic(3, 3, CV_64F);
        for (int index = 0; index < 9; ++index)
        {
            intrinsic.at<double>(index / 3, index % 3) = params.intrinsic[index];
        }
        if (intrinsic.at<double>(0, 0) <= std::numeric_limits<double>::epsilon() ||
            intrinsic.at<double>(1, 1) <= std::numeric_limits<double>::epsilon())
        {
            errorMessage = "Initial camera intrinsic matrix must have positive focal lengths.";
            return false;
        }

        cv::Mat distortion(1, 8, CV_64F);
        for (int index = 0; index < 8; ++index)
        {
            distortion.at<double>(0, index) = params.distortion[index];
        }

        found->second.intrinsic = intrinsic;
        found->second.distortion = distortion;
        found->second.hasIntrinsic = true;
        found->second.fixedIntrinsic = fixedIntrinsic;
        isReady_ = false;
        return true;
    }

    bool MultiCameraCalibrationFramework::addObservationImagePath(
        const std::string& cameraId,
        const std::string& captureId,
        const std::string& imagePath,
        std::string& errorMessage)
    {
        errorMessage.clear();
        if (cameras_.find(cameraId) == cameras_.end())
        {
            errorMessage = "Camera '" + cameraId + "' does not exist.";
            return false;
        }
        if (!hasBoardParams_ || !boardCalibrator_)
        {
            errorMessage = "Calibration board params must be set before adding observation images.";
            return false;
        }
        if (captureId.empty())
        {
            errorMessage = "captureId must not be empty.";
            return false;
        }

        cv::Mat image = cv::imread(imagePath, cv::IMREAD_UNCHANGED);
        if (image.empty())
        {
            errorMessage = "Failed to load observation image: " + imagePath;
            return false;
        }

        BoardObservation observation;
        observation.cameraId = cameraId;
        observation.captureId = captureId;
        observation.imagePath = imagePath;
        observation.imageSize = image.size();

        if (!boardCalibrator_->detect(image, boardParams_, observation, errorMessage))
        {
            return false;
        }
        observation.cameraId = cameraId;
        observation.captureId = captureId;
        observation.imagePath = imagePath;
        observation.imageSize = image.size();

        if (static_cast<int>(observation.corners.size()) < options_.minCornersPerObservation)
        {
            std::ostringstream message;
            message << "Observation for camera '" << cameraId << "' capture '" << captureId
                << "' has " << observation.corners.size()
                << " corners, below minCornersPerObservation=" << options_.minCornersPerObservation << ".";
            errorMessage = message.str();
            return false;
        }

        captures_.try_emplace(captureId, CapturePose{ captureId, cv::Mat(), false });
        graph_.addObservation(observation);
        isReady_ = false;
        return true;
    }

    bool MultiCameraCalibrationFramework::initializeIntrinsics(std::string& errorMessage)
    {
        errorMessage.clear();
        if (!hasBoardParams_)
        {
            errorMessage = "Calibration board params are missing.";
            return false;
        }

        for (auto& [cameraId, camera] : cameras_)
        {
            if (camera.hasIntrinsic)
            {
                camera.distortion = normalizeDistortion(camera.distortion);
                continue;
            }

            const std::vector<BoardObservation> observations = graph_.observationsForCamera(cameraId);
            if (observations.empty())
            {
                errorMessage = "Camera '" + cameraId + "' has no observations for intrinsic initialization.";
                return false;
            }

            cv::Mat intrinsic;
            cv::Mat distortion;
            std::vector<cv::Mat> rvecs;
            std::vector<cv::Mat> tvecs;
            double rmsError = 0.0;
            if (!boardCalibrator_->calibrate(
                cameraId,
                boardParams_,
                observations,
                camera.imageSize,
                intrinsic,
                distortion,
                rvecs,
                tvecs,
                rmsError,
                errorMessage))
            {
                return false;
            }
            camera.intrinsic = intrinsic;
            camera.distortion = normalizeDistortion(distortion);
            camera.hasIntrinsic = true;
        }

        return true;
    }

    bool MultiCameraCalibrationFramework::initializeObservationPoses(std::string& errorMessage)
    {
        errorMessage.clear();
        std::vector<BoardObservation> observations = collectObservations(graph_);
        for (auto& observation : observations)
        {
            const auto camera = cameras_.find(observation.cameraId);
            if (camera == cameras_.end())
            {
                errorMessage = "Observation references missing camera '" + observation.cameraId + "'.";
                return false;
            }

            std::vector<cv::Point3d> boardPoints;
            std::vector<cv::Point2d> imagePoints;
            boardPoints.reserve(observation.corners.size());
            imagePoints.reserve(observation.corners.size());
            for (const auto& corner : observation.corners)
            {
                boardPoints.push_back(corner.boardPoint);
                imagePoints.push_back(corner.imagePoint);
            }
            if (boardPoints.size() < 4)
            {
                errorMessage = "Observation for camera '" + observation.cameraId +
                    "' capture '" + observation.captureId + "' has fewer than 4 corners for cv::solvePnP.";
                return false;
            }

            cv::Mat rvec;
            cv::Mat tvec;
            try
            {
                const bool ok = cv::solvePnP(
                    boardPoints,
                    imagePoints,
                    camera->second.intrinsic,
                    camera->second.distortion,
                    rvec,
                    tvec,
                    false,
                    cv::SOLVEPNP_ITERATIVE);
                if (!ok)
                {
                    errorMessage = "cv::solvePnP failed for camera '" + observation.cameraId +
                        "' capture '" + observation.captureId + "'.";
                    return false;
                }
                rvec.convertTo(observation.rvecCameraFromBoard, CV_64F);
                tvec.convertTo(observation.tvecCameraFromBoard, CV_64F);
                observation.initialReprojectionError = computeReprojectionRms(
                    observation,
                    camera->second,
                    observation.rvecCameraFromBoard,
                    observation.tvecCameraFromBoard);
                observation.hasInitialPose = true;
            }
            catch (const cv::Exception& exception)
            {
                errorMessage = "cv::solvePnP failed for camera '" + observation.cameraId +
                    "' capture '" + observation.captureId + "': " + exception.what();
                return false;
            }

            if (observation.initialReprojectionError > options_.maxReprojectionErrorForInit)
            {
                std::ostringstream message;
                message << "Initial reprojection RMS for camera '" << observation.cameraId
                    << "' capture '" << observation.captureId
                    << "' is " << observation.initialReprojectionError
                    << ", above maxReprojectionErrorForInit=" << options_.maxReprojectionErrorForInit << ".";
                errorMessage = message.str();
                return false;
            }
        }

        rebuildGraph(graph_, cameras_, observations);
        return true;
    }

    bool MultiCameraCalibrationFramework::initializeCommonPoses(std::string& errorMessage)
    {
        errorMessage.clear();
        if (options_.anchorMode != AnchorMode::BoardPose)
        {
            errorMessage = "Only BoardPose anchor mode is supported by the first multi-camera calibration implementation.";
            return false;
        }

        std::set<std::string> captureIds = graph_.captureIds();
        if (captureIds.empty())
        {
            errorMessage = "No capture can be selected as the BoardPose anchor.";
            return false;
        }

        std::string referenceCaptureId = options_.referenceCaptureId;
        if (referenceCaptureId.empty())
        {
            referenceCaptureId = selectBestReferenceCaptureId(graph_, captureIds);
            options_.referenceCaptureId = referenceCaptureId;
        }
        if (captureIds.find(referenceCaptureId) == captureIds.end())
        {
            errorMessage = "Reference capture '" + referenceCaptureId + "' does not exist.";
            return false;
        }

        for (const auto& captureId : captureIds)
        {
            captures_.try_emplace(captureId, CapturePose{ captureId, cv::Mat(), false });
        }

        captures_[referenceCaptureId].transformCommonFromBoard = cv::Mat::eye(4, 4, CV_64F);
        captures_[referenceCaptureId].fixed = true;

        std::set<std::string> knownCaptures;
        std::set<std::string> knownCameras;
        std::queue<std::pair<char, std::string>> pending;
        knownCaptures.insert(referenceCaptureId);
        pending.emplace('P', referenceCaptureId);

        while (!pending.empty())
        {
            const auto [nodeType, nodeId] = pending.front();
            pending.pop();

            if (nodeType == 'P')
            {
                const cv::Mat commonFromBoard = captures_.at(nodeId).transformCommonFromBoard;
                for (const auto& observation : graph_.observationsForCapture(nodeId))
                {
                    if (!observation.hasInitialPose)
                    {
                        errorMessage = "Observation for camera '" + observation.cameraId +
                            "' capture '" + observation.captureId + "' is missing an initial pose.";
                        return false;
                    }
                    if (knownCameras.find(observation.cameraId) != knownCameras.end())
                    {
                        continue;
                    }

                    const cv::Mat cameraFromBoard = transformFromRvecTvec(
                        observation.rvecCameraFromBoard,
                        observation.tvecCameraFromBoard);
                    cv::Mat cameraFromCommon = cameraFromBoard * commonFromBoard.inv();
                    cameras_.at(observation.cameraId).transformCameraFromCommon = cameraFromCommon;
                    cameras_.at(observation.cameraId).transformCommonFromCamera = cameraFromCommon.inv();
                    knownCameras.insert(observation.cameraId);
                    pending.emplace('C', observation.cameraId);
                }
            }
            else
            {
                const cv::Mat cameraFromCommon = cameras_.at(nodeId).transformCameraFromCommon;
                for (const auto& observation : graph_.observationsForCamera(nodeId))
                {
                    if (knownCaptures.find(observation.captureId) != knownCaptures.end())
                    {
                        continue;
                    }

                    const cv::Mat cameraFromBoard = transformFromRvecTvec(
                        observation.rvecCameraFromBoard,
                        observation.tvecCameraFromBoard);
                    cv::Mat commonFromBoard = cameraFromCommon.inv() * cameraFromBoard;
                    captures_.at(observation.captureId).transformCommonFromBoard = commonFromBoard;
                    captures_.at(observation.captureId).fixed = false;
                    knownCaptures.insert(observation.captureId);
                    pending.emplace('P', observation.captureId);
                }
            }
        }

        for (const auto& [cameraId, camera] : cameras_)
        {
            if (!isTransform4x4(camera.transformCameraFromCommon) ||
                !isTransform4x4(camera.transformCommonFromCamera))
            {
                errorMessage = "Failed to initialize common pose for camera '" + cameraId + "'.";
                return false;
            }
        }
        for (const auto& captureId : captureIds)
        {
            if (!isTransform4x4(captures_.at(captureId).transformCommonFromBoard))
            {
                errorMessage = "Failed to initialize common pose for capture '" + captureId + "'.";
                return false;
            }
        }

        return true;
    }

    bool MultiCameraCalibrationFramework::calibrate(std::string& errorMessage)
    {
        errorMessage.clear();
        isReady_ = false;
        report_ = MultiCameraReport{};
        report_.cameraCount = static_cast<int>(cameras_.size());
        report_.captureCount = static_cast<int>(captures_.size());
        report_.observationCount = static_cast<int>(collectObservations(graph_).size());

        if (cameras_.empty())
        {
            errorMessage = "No cameras were added.";
            return false;
        }

        const GraphConnectivityResult connectivity = graph_.validateConnected();
        report_.connectedComponentCount = connectivity.componentCount;
        if (!connectivity.connected)
        {
            errorMessage = disconnectedGraphMessage(connectivity);
            report_.messages.push_back(errorMessage);
            return false;
        }

        if (!initializeIntrinsics(errorMessage) ||
            !initializeObservationPoses(errorMessage) ||
            !initializeCommonPoses(errorMessage))
        {
            report_.messages.push_back(errorMessage);
            return false;
        }

        BundleAdjustmentInput input;
        input.options = options_;
        input.cameras = cameras_;
        input.captures = captures_;
        input.observations = collectObservations(graph_);

        BundleAdjustmentOutput output;
        if (!bundleAdjuster_.optimize(input, output, errorMessage))
        {
            report_ = output.report;
            report_.messages.push_back(errorMessage);
            return false;
        }

        cameras_ = output.cameras;
        captures_ = output.captures;
        report_ = output.report;
        report_.cameraCount = static_cast<int>(cameras_.size());
        report_.captureCount = static_cast<int>(captures_.size());
        report_.observationCount = static_cast<int>(input.observations.size());
        isReady_ = true;
        return true;
    }

    bool MultiCameraCalibrationFramework::getCameraParams(const std::string& cameraId, CameraRigParams& params) const
    {
        const auto found = cameras_.find(cameraId);
        if (found == cameras_.end())
        {
            return false;
        }

        params = found->second;
        params.intrinsic = found->second.intrinsic.clone();
        params.distortion = found->second.distortion.clone();
        params.transformCameraFromCommon = found->second.transformCameraFromCommon.clone();
        params.transformCommonFromCamera = found->second.transformCommonFromCamera.clone();
        return true;
    }

    std::vector<std::string> MultiCameraCalibrationFramework::cameraIds() const
    {
        std::vector<std::string> ids;
        ids.reserve(cameras_.size());
        for (const auto& cameraEntry : cameras_)
        {
            ids.push_back(cameraEntry.first);
        }
        return ids;
    }

    MultiCameraReport MultiCameraCalibrationFramework::report() const
    {
        return report_;
    }

    bool MultiCameraCalibrationFramework::pixelToCommonWorld(
        const std::string& cameraId,
        double pixelX,
        double pixelY,
        cv::Point3d& worldPoint,
        std::string& errorMessage) const
    {
        if (!isReady_)
        {
            errorMessage = "Multi-camera calibration must be successfully loaded or calibrated before projection.";
            return false;
        }
        const auto found = cameras_.find(cameraId);
        if (found == cameras_.end())
        {
            errorMessage = "Camera '" + cameraId + "' does not exist.";
            return false;
        }
        return projector_.pixelToCommonWorld(
            found->second,
            pixelX,
            pixelY,
            measurementPlaneParams_.heightCompensation,
            worldPoint,
            errorMessage);
    }

    bool MultiCameraCalibrationFramework::commonWorldToPixel(
        const std::string& cameraId,
        const cv::Point3d& worldPoint,
        cv::Point2d& pixelPoint,
        std::string& errorMessage) const
    {
        if (!isReady_)
        {
            errorMessage = "Multi-camera calibration must be successfully loaded or calibrated before projection.";
            return false;
        }
        const auto found = cameras_.find(cameraId);
        if (found == cameras_.end())
        {
            errorMessage = "Camera '" + cameraId + "' does not exist.";
            return false;
        }
        return projector_.commonWorldToPixel(found->second, worldPoint, pixelPoint, errorMessage);
    }

    bool MultiCameraCalibrationFramework::estimateStitchingOptions(
        const std::vector<StitchImageInput>& images,
        double heightCompensation,
        BlendMode blendMode,
        StitchingOptions& options,
        std::string& errorMessage) const
    {
        errorMessage.clear();
        options = StitchingOptions();
        if (!isReady_ || cameras_.empty())
        {
            errorMessage = "No loaded or calibrated cameras are available for stitching.";
            return false;
        }
        if (!isFinite(heightCompensation))
        {
            errorMessage = "heightCompensation must be finite.";
            return false;
        }
        if (images.empty())
        {
            errorMessage = "No images were provided for stitching.";
            return false;
        }

        for (const StitchImageInput& imageInput : images)
        {
            if (imageInput.cameraId.empty())
            {
                errorMessage = "Stitch image input camera id must not be empty.";
                return false;
            }
            if (imageInput.image.empty() || imageInput.image.cols < 2 || imageInput.image.rows < 2)
            {
                errorMessage = "Stitch image input for camera '" + imageInput.cameraId + "' must be at least 2x2.";
                return false;
            }

            const auto camera = cameras_.find(imageInput.cameraId);
            if (camera == cameras_.end())
            {
                errorMessage = "Image references missing camera '" + imageInput.cameraId + "'.";
                return false;
            }
        }

        ReferenceCameraCanvas canvas;
        if (!buildReferenceCameraCanvas(cameras_, images, heightCompensation, canvas, errorMessage))
        {
            return false;
        }

        cv::Point2d originWorld;
        if (!applyHomography(canvas.HCanvasToBoard, cv::Point2d(0.0, 0.0), originWorld))
        {
            errorMessage = "Failed to compute reference camera canvas origin metadata.";
            return false;
        }

        options.heightCompensation = heightCompensation;
        options.worldUnitsPerPixel = canvas.approximateWorldUnitsPerPixel;
        options.originWorldX = originWorld.x;
        options.originWorldY = originWorld.y;
        options.outputWidth = canvas.outputSize.width;
        options.outputHeight = canvas.outputSize.height;
        options.blendMode = blendMode;
        options.referenceCameraId = canvas.referenceCameraId;
        options.HBoardToCanvas = canvas.HBoardToCanvas.clone();
        options.HCanvasToBoard = canvas.HCanvasToBoard.clone();
        return true;
    }

    bool MultiCameraCalibrationFramework::createDefaultSingleCompatibility(
        double heightCompensation,
        FusedPlaneGridModel& model,
        std::string& errorMessage) const
    {
        errorMessage.clear();
        model = FusedPlaneGridModel();
        if (!isReady_ || cameras_.empty())
        {
            errorMessage = "No loaded or calibrated cameras are available for fused-plane compatibility.";
            return false;
        }

        std::vector<StitchImageInput> cameraImages;
        cameraImages.reserve(cameras_.size());
        for (const auto& [cameraId, camera] : cameras_)
        {
            if (camera.imageSize.width < 2 || camera.imageSize.height < 2)
            {
                errorMessage = "Camera '" + cameraId + "' image size must be at least 2x2.";
                return false;
            }
            cameraImages.push_back({ cameraId, cv::Mat::zeros(camera.imageSize, CV_8UC1) });
        }

        StitchingOptions options;
        if (!estimateStitchingOptions(cameraImages, heightCompensation, BlendMode::Average, options, errorMessage))
        {
            return false;
        }

        const double canvasStepX = static_cast<double>(options.outputWidth - 1);
        const double canvasStepY = static_cast<double>(options.outputHeight - 1);
        if (canvasStepX <= 0.0 || canvasStepY <= 0.0)
        {
            errorMessage = "Fused plane canvas size must be at least 2x2.";
            return false;
        }

        model.virtualCameraId = "FUSED_CANVAS";
        model.coordinateFrame = "BOARD_POSE";
        model.heightCompensation = options.heightCompensation;
        model.canvasWidth = options.outputWidth;
        model.canvasHeight = options.outputHeight;
        model.HCanvasToBoard = options.HCanvasToBoard.clone();
        model.HBoardToCanvas = options.HBoardToCanvas.clone();

        double minWorldX = 0.0;
        double minWorldY = 0.0;
        double maxWorldX = 0.0;
        double maxWorldY = 0.0;
        if (!computeCanvasWorldBounds(model, minWorldX, minWorldY, maxWorldX, maxWorldY, errorMessage))
        {
            return false;
        }

        model.canvasToBoardResidual.domain = ResidualGridDomain::CanvasPixel;
        model.canvasToBoardResidual.enabled = true;
        model.canvasToBoardResidual.originX = 0.0;
        model.canvasToBoardResidual.originY = 0.0;
        model.canvasToBoardResidual.stepX = canvasStepX;
        model.canvasToBoardResidual.stepY = canvasStepY;
        model.canvasToBoardResidual.cols = 2;
        model.canvasToBoardResidual.rows = 2;
        model.canvasToBoardResidual.dx = cv::Mat::zeros(2, 2, CV_64F);
        model.canvasToBoardResidual.dy = cv::Mat::zeros(2, 2, CV_64F);
        model.canvasToBoardResidual.validMask = cv::Mat(2, 2, CV_8U, cv::Scalar(255));

        model.boardToCanvasResidual.domain = ResidualGridDomain::BoardWorld;
        model.boardToCanvasResidual.enabled = true;
        model.boardToCanvasResidual.originX = minWorldX;
        model.boardToCanvasResidual.originY = minWorldY;
        model.boardToCanvasResidual.stepX = maxWorldX - minWorldX;
        model.boardToCanvasResidual.stepY = maxWorldY - minWorldY;
        model.boardToCanvasResidual.cols = 2;
        model.boardToCanvasResidual.rows = 2;
        model.boardToCanvasResidual.dx = cv::Mat::zeros(2, 2, CV_64F);
        model.boardToCanvasResidual.dy = cv::Mat::zeros(2, 2, CV_64F);
        model.boardToCanvasResidual.validMask = cv::Mat(2, 2, CV_8U, cv::Scalar(255));

        return model.validate(errorMessage);
    }

    bool MultiCameraCalibrationFramework::stitch(
        const std::vector<StitchImageInput>& images,
        BlendMode blendMode,
        cv::Mat& output,
        std::string& errorMessage) const
    {
        StitchingOptions options;
        if (!estimateStitchingOptions(images, measurementPlaneParams_.heightCompensation, blendMode, options, errorMessage))
        {
            return false;
        }
        return stitcher_.stitch(cameras_, images, options, output, errorMessage);
    }

    bool MultiCameraCalibrationFramework::save(const std::string& outputFile, std::string& errorMessage) const
    {
        errorMessage.clear();
        if (!isReady_)
        {
            errorMessage = "Multi-camera calibration must be successfully loaded or calibrated before save.";
            return false;
        }
        if (!hasBoardParams_ || !isValidBoardParams(boardParams_, errorMessage))
        {
            return false;
        }
        if (cameras_.empty())
        {
            errorMessage = "No loaded or calibrated cameras are available for save.";
            return false;
        }
        if (captures_.empty())
        {
            errorMessage = "No loaded or calibrated captures are available for save.";
            return false;
        }
        for (const auto& [cameraId, camera] : cameras_)
        {
            if (cameraId != camera.cameraId)
            {
                errorMessage = "Camera map key does not match cameraId.";
                return false;
            }
            if (!validateCameraSerializable(camera, errorMessage))
            {
                return false;
            }
        }
        for (const auto& [captureId, capture] : captures_)
        {
            if (captureId != capture.captureId)
            {
                errorMessage = "Capture map key does not match captureId.";
                return false;
            }
            if (!validateCaptureSerializable(capture, errorMessage))
            {
                return false;
            }
        }
        if (!validateReport(report_, errorMessage))
        {
            return false;
        }
        if (!isFinite(measurementPlaneParams_.heightCompensation))
        {
            errorMessage = "heightCompensation must be finite.";
            return false;
        }

        FusedPlaneGridModel singleCompatibility;
        if (!createDefaultSingleCompatibility(measurementPlaneParams_.heightCompensation, singleCompatibility, errorMessage))
        {
            return false;
        }

        std::filesystem::path requestedPath(outputFile);
        std::filesystem::path resolvedOutputFile;
        if (isYamlFilePath(requestedPath))
        {
            std::filesystem::path parentPath = requestedPath.parent_path();
            if (!parentPath.empty() && !std::filesystem::exists(parentPath))
            {
                std::filesystem::create_directories(parentPath);
            }
            resolvedOutputFile = requestedPath;
        }
        else
        {
            if (!std::filesystem::exists(requestedPath))
            {
                std::filesystem::create_directories(requestedPath);
            }

            std::vector<std::string> cameraIds;
            cameraIds.reserve(cameras_.size());
            for (const auto& [cameraId, _] : cameras_)
            {
                cameraIds.push_back(sanitizeFileNameSegment(cameraId, "camera"));
            }
            std::sort(cameraIds.begin(), cameraIds.end());

            std::ostringstream defaultFileName;
            defaultFileName << "multi_camera";
            for (const std::string& cameraId : cameraIds)
            {
                defaultFileName << "_" << cameraId;
            }
            defaultFileName << "_" << boardTypeToFileSegment(boardParams_.type) << ".yaml";
            resolvedOutputFile = requestedPath / defaultFileName.str();
        }

        cv::FileStorage fs(resolvedOutputFile.string(), cv::FileStorage::WRITE);
        if (!fs.isOpened())
        {
            errorMessage = "Failed to open output YAML file: " + resolvedOutputFile.string();
            return false;
        }

        fs << "format" << kFormatName;
        fs << "board" << "{"
            << "type" << boardTypeToString(boardParams_.type)
            << "width" << boardParams_.width
            << "height" << boardParams_.height
            << "squareSize" << boardParams_.squareSize;
        if (boardParams_.type == CHARUCO)
        {
            fs << "markerSize" << boardParams_.markerSize
                << "dictionaryId" << boardParams_.dictionaryId;
            fs << "squareSizePixel" << boardParams_.squareSizePixel;
            fs << "markerSizePixel" << boardParams_.markerSizePixel;
        }
        fs << "}";
        fs << "measurementPlane" << "{"
            << "heightCompensation" << measurementPlaneParams_.heightCompensation
            << "}";
        fs << "anchor" << "{"
            << "mode" << anchorModeToString(options_.anchorMode)
            << "referenceCameraId" << options_.referenceCameraId
            << "referenceCaptureId" << options_.referenceCaptureId
            << "refineIntrinsics" << (options_.refineIntrinsics ? 1 : 0)
            << "refineDistortion" << (options_.refineDistortion ? 1 : 0)
            << "minCornersPerObservation" << options_.minCornersPerObservation
            << "maxReprojectionErrorForInit" << options_.maxReprojectionErrorForInit
            << "robustLossScale" << options_.robustLossScale
            << "maxIterations" << options_.maxIterations
            << "}";

        fs << "cameras" << "[";
        for (const auto& [cameraId, camera] : cameras_)
        {
            fs << "{"
                << "cameraId" << camera.cameraId
                << "imageWidth" << camera.imageSize.width
                << "imageHeight" << camera.imageSize.height
                << "intrinsic" << camera.intrinsic
                << "distortion" << normalizeDistortion(camera.distortion)
                << "T_common_from_camera" << camera.transformCommonFromCamera
                << "T_camera_from_common" << camera.transformCameraFromCommon
                << "fixedIntrinsic" << (camera.fixedIntrinsic ? 1 : 0)
                << "rmsError" << camera.rmsError
                << "}";
        }
        fs << "]";

        fs << "captures" << "[";
        for (const auto& [captureId, capture] : captures_)
        {
            fs << "{"
                << "captureId" << capture.captureId
                << "T_common_from_board" << capture.transformCommonFromBoard
                << "fixed" << (capture.fixed ? 1 : 0)
                << "}";
        }
        fs << "]";

        writeReport(fs, report_);
        writeSingleCompatibility(fs, singleCompatibility);
        return true;
    }

    bool MultiCameraCalibrationFramework::load(const std::string& filePath, std::string& errorMessage)
    {
        errorMessage.clear();
        cv::FileStorage fs(filePath, cv::FileStorage::READ);
        if (!fs.isOpened())
        {
            errorMessage = "Failed to open calibration YAML file: " + filePath;
            return false;
        }

        try
        {
            std::string format;
            const cv::FileNode formatNode = fs["format"];
            if (formatNode.empty() || !formatNode.isString())
            {
                errorMessage = "Missing required top-level string field 'format'.";
                return false;
            }
            formatNode >> format;
            if (format != kFormatName)
            {
                errorMessage = "Unsupported multi-camera calibration format: " + format;
                return false;
            }

            cv::FileNode boardNode;
            cv::FileNode anchorNode;
            cv::FileNode camerasNode;
            cv::FileNode capturesNode;
            cv::FileNode reportNode;
            if (!requireTopLevelNode(fs, "board", true, false, boardNode, errorMessage) ||
                !requireTopLevelNode(fs, "anchor", true, false, anchorNode, errorMessage) ||
                !requireTopLevelNode(fs, "cameras", false, true, camerasNode, errorMessage) ||
                !requireTopLevelNode(fs, "captures", false, true, capturesNode, errorMessage) ||
                !requireTopLevelNode(fs, "report", true, false, reportNode, errorMessage))
            {
                return false;
            }

            CalibrationBoardParams loadedBoard{};
            const cv::FileNode boardTypeNode = boardNode["type"];
            if (boardTypeNode.empty() || (!boardTypeNode.isString() && !boardTypeNode.isInt()))
            {
                errorMessage = "Field 'type' must be a board type string or integer.";
                return false;
            }
            loadedBoard.type = boardTypeFromNode(boardNode["type"]);
            if (!readRequiredInt(boardNode, "width", loadedBoard.width, errorMessage) ||
                !readRequiredInt(boardNode, "height", loadedBoard.height, errorMessage) ||
                !readRequiredDouble(boardNode, "squareSize", loadedBoard.squareSize, errorMessage))
            {
                return false;
            }
            if (loadedBoard.type == CHARUCO &&
                (!readRequiredDouble(boardNode, "markerSize", loadedBoard.markerSize, errorMessage) ||
                    !readRequiredInt(boardNode, "dictionaryId", loadedBoard.dictionaryId, errorMessage)))
            {
                return false;
            }
            if (loadedBoard.type == CHARUCO &&
                (!readOptionalDouble(boardNode, "squareSizePixel", loadedBoard.squareSizePixel, errorMessage) ||
                    !readOptionalDouble(boardNode, "markerSizePixel", loadedBoard.markerSizePixel, errorMessage)))
            {
                return false;
            }
            if (!isValidBoardParams(loadedBoard, errorMessage))
            {
                return false;
            }

            MeasurementPlaneParams loadedMeasurementPlane{};
            const cv::FileNode measurementPlaneNode = fs["measurementPlane"];
            if (!measurementPlaneNode.empty())
            {
                if (!measurementPlaneNode.isMap())
                {
                    errorMessage = "measurementPlane must be a map.";
                    return false;
                }
                if (!readRequiredDouble(
                        measurementPlaneNode,
                        "heightCompensation",
                        loadedMeasurementPlane.heightCompensation,
                        errorMessage))
                {
                    return false;
                }
            }

            MultiCameraOptions loadedOptions;
            const cv::FileNode modeNode = anchorNode["mode"];
            if (!modeNode.empty())
            {
                if (modeNode.isString())
                {
                    std::string mode;
                    modeNode >> mode;
                    if (mode != "BOARD_POSE" && mode != "CAMERA" && mode != "EXTERNAL")
                    {
                        errorMessage = "Field 'mode' contains an unsupported anchor mode.";
                        return false;
                    }
                    loadedOptions.anchorMode = anchorModeFromString(mode);
                }
                else if (modeNode.isInt())
                {
                    const int modeValue = static_cast<int>(modeNode);
                    if (modeValue < static_cast<int>(AnchorMode::BoardPose) ||
                        modeValue > static_cast<int>(AnchorMode::External))
                    {
                        errorMessage = "Field 'mode' contains an unsupported anchor mode.";
                        return false;
                    }
                    loadedOptions.anchorMode = static_cast<AnchorMode>(modeValue);
                }
                else
                {
                    errorMessage = "Field 'mode' must be a string or integer.";
                    return false;
                }
            }
            if (!anchorNode["referenceCameraId"].empty() && !anchorNode["referenceCameraId"].isString())
            {
                errorMessage = "Field 'referenceCameraId' must be a string.";
                return false;
            }
            anchorNode["referenceCameraId"] >> loadedOptions.referenceCameraId;
            if (!anchorNode["referenceCaptureId"].empty() && !anchorNode["referenceCaptureId"].isString())
            {
                errorMessage = "Field 'referenceCaptureId' must be a string.";
                return false;
            }
            anchorNode["referenceCaptureId"] >> loadedOptions.referenceCaptureId;
            if (!readOptionalBool(anchorNode, "refineIntrinsics", loadedOptions.refineIntrinsics, errorMessage) ||
                !readOptionalBool(anchorNode, "refineDistortion", loadedOptions.refineDistortion, errorMessage) ||
                !readOptionalInt(anchorNode, "minCornersPerObservation", loadedOptions.minCornersPerObservation, errorMessage) ||
                !readOptionalDouble(anchorNode, "maxReprojectionErrorForInit", loadedOptions.maxReprojectionErrorForInit, errorMessage) ||
                !readOptionalDouble(anchorNode, "robustLossScale", loadedOptions.robustLossScale, errorMessage) ||
                !readOptionalInt(anchorNode, "maxIterations", loadedOptions.maxIterations, errorMessage))
            {
                return false;
            }
            if (loadedOptions.minCornersPerObservation <= 0 ||
                loadedOptions.maxReprojectionErrorForInit <= 0.0 ||
                loadedOptions.robustLossScale <= 0.0)
            {
                errorMessage = "Anchor numeric options must be positive.";
                return false;
            }
            if (loadedOptions.maxIterations <= 0)
            {
                loadedOptions.maxIterations = 100;
            }

            std::map<std::string, CameraRigParams> loadedCameras;
            for (auto iterator = camerasNode.begin(); iterator != camerasNode.end(); ++iterator)
            {
                const cv::FileNode node = *iterator;
                if (!node.isMap())
                {
                    errorMessage = "Each cameras entry must be a map.";
                    return false;
                }

                CameraRigParams camera;
                int imageWidth = 0;
                int imageHeight = 0;
                if (!readRequiredString(node, "cameraId", camera.cameraId, errorMessage) ||
                    !readRequiredInt(node, "imageWidth", imageWidth, errorMessage) ||
                    !readRequiredInt(node, "imageHeight", imageHeight, errorMessage))
                {
                    return false;
                }
                camera.imageSize = cv::Size(imageWidth, imageHeight);
                node["intrinsic"] >> camera.intrinsic;
                if (!validateIntrinsic(camera.intrinsic, camera.cameraId, errorMessage))
                {
                    return false;
                }
                cv::Mat distortion;
                node["distortion"] >> distortion;
                if (!validateDistortion(distortion, camera.cameraId, errorMessage))
                {
                    return false;
                }
                camera.distortion = normalizeDistortion(distortion);

                const bool hasCommonFromCamera = !node["T_common_from_camera"].empty();
                const bool hasCameraFromCommon = !node["T_camera_from_common"].empty();
                if (!hasCommonFromCamera && !hasCameraFromCommon)
                {
                    errorMessage = "Loaded camera '" + camera.cameraId + "' is missing a transform.";
                    return false;
                }
                if (hasCommonFromCamera)
                {
                    node["T_common_from_camera"] >> camera.transformCommonFromCamera;
                    if (!validateTransform(camera.transformCommonFromCamera, "Camera '" + camera.cameraId + "' T_common_from_camera", errorMessage))
                    {
                        return false;
                    }
                }
                if (hasCameraFromCommon)
                {
                    node["T_camera_from_common"] >> camera.transformCameraFromCommon;
                    if (!validateTransform(camera.transformCameraFromCommon, "Camera '" + camera.cameraId + "' T_camera_from_common", errorMessage))
                    {
                        return false;
                    }
                }
                if (hasCommonFromCamera && hasCameraFromCommon)
                {
                    if (!areInverseTransforms(camera.transformCommonFromCamera, camera.transformCameraFromCommon))
                    {
                        errorMessage = "Loaded camera '" + camera.cameraId + "' has inconsistent transforms.";
                        return false;
                    }
                    if (!invertTransformChecked(
                        camera.transformCommonFromCamera,
                        "Camera '" + camera.cameraId + "' T_common_from_camera",
                        camera.transformCameraFromCommon,
                        errorMessage))
                    {
                        return false;
                    }
                }
                else if (hasCommonFromCamera)
                {
                    if (!invertTransformChecked(
                        camera.transformCommonFromCamera,
                        "Camera '" + camera.cameraId + "' T_common_from_camera",
                        camera.transformCameraFromCommon,
                        errorMessage))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!invertTransformChecked(
                        camera.transformCameraFromCommon,
                        "Camera '" + camera.cameraId + "' T_camera_from_common",
                        camera.transformCommonFromCamera,
                        errorMessage))
                    {
                        return false;
                    }
                }

                if (!readOptionalBool(node, "fixedIntrinsic", camera.fixedIntrinsic, errorMessage) ||
                    !readOptionalDouble(node, "rmsError", camera.rmsError, errorMessage))
                {
                    return false;
                }
                if (!isFinite(camera.rmsError) || camera.rmsError < 0.0)
                {
                    errorMessage = "Loaded camera '" + camera.cameraId + "' has invalid rmsError.";
                    return false;
                }
                camera.hasIntrinsic = true;

                if (!validateCameraSerializable(camera, errorMessage))
                {
                    return false;
                }
                if (!loadedCameras.emplace(camera.cameraId, camera).second)
                {
                    errorMessage = "Duplicate cameraId '" + camera.cameraId + "' in calibration YAML.";
                    return false;
                }
            }

            std::map<std::string, CapturePose> loadedCaptures;
            for (auto iterator = capturesNode.begin(); iterator != capturesNode.end(); ++iterator)
            {
                const cv::FileNode node = *iterator;
                if (!node.isMap())
                {
                    errorMessage = "Each captures entry must be a map.";
                    return false;
                }

                CapturePose capture;
                if (!readRequiredString(node, "captureId", capture.captureId, errorMessage))
                {
                    return false;
                }
                node["T_common_from_board"] >> capture.transformCommonFromBoard;
                if (!validateCaptureSerializable(capture, errorMessage) ||
                    !readOptionalBool(node, "fixed", capture.fixed, errorMessage))
                {
                    return false;
                }
                if (!loadedCaptures.emplace(capture.captureId, capture).second)
                {
                    errorMessage = "Duplicate captureId '" + capture.captureId + "' in calibration YAML.";
                    return false;
                }
            }

            MultiCameraReport loadedReport;
            loadedReport.cameraCount = !reportNode["cameraCount"].empty() ? static_cast<int>(reportNode["cameraCount"]) : static_cast<int>(loadedCameras.size());
            loadedReport.captureCount = !reportNode["captureCount"].empty() ? static_cast<int>(reportNode["captureCount"]) : static_cast<int>(loadedCaptures.size());
            loadedReport.observationCount = !reportNode["observationCount"].empty() ? static_cast<int>(reportNode["observationCount"]) : 0;
            loadedReport.residualCount = !reportNode["residualCount"].empty() ? static_cast<int>(reportNode["residualCount"]) : 0;
            loadedReport.initialRmsError = !reportNode["initialRmsError"].empty() ? static_cast<double>(reportNode["initialRmsError"]) : 0.0;
            loadedReport.finalRmsError = !reportNode["finalRmsError"].empty() ? static_cast<double>(reportNode["finalRmsError"]) : 0.0;
            loadedReport.maxReprojectionError = !reportNode["maxReprojectionError"].empty() ? static_cast<double>(reportNode["maxReprojectionError"]) : 0.0;
            loadedReport.connectedComponentCount = !reportNode["connectedComponentCount"].empty() ? static_cast<int>(reportNode["connectedComponentCount"]) : 0;
            loadedReport.converged = !reportNode["converged"].empty() && static_cast<int>(reportNode["converged"]) != 0;
            loadedReport.ceresTerminationType = !reportNode["ceresTerminationType"].empty() ? static_cast<int>(reportNode["ceresTerminationType"]) : 0;
            if (!validateReport(loadedReport, errorMessage))
            {
                return false;
            }
            if (loadedReport.cameraCount != static_cast<int>(loadedCameras.size()) ||
                loadedReport.captureCount != static_cast<int>(loadedCaptures.size()))
            {
                errorMessage = "Loaded report camera/capture counts do not match calibration entries.";
                return false;
            }

            std::optional<FusedPlaneGridModel> loadedSingleCompatibility;
            if (!fs["singleCompatibility"].empty())
            {
                FusedPlaneGridModel loadedCompatibility;
                if (!readSingleCompatibility(fs, loadedCompatibility, errorMessage))
                {
                    return false;
                }
                loadedSingleCompatibility = loadedCompatibility;
            }

            boardParams_ = loadedBoard;
            measurementPlaneParams_ = loadedMeasurementPlane;
            hasBoardParams_ = true;
            boardCalibrator_ = calib::boards::createCalibrationBoardCalibrator(boardParams_.type, errorMessage);
            if (!boardCalibrator_)
            {
                return false;
            }
            options_ = loadedOptions;
            cameras_ = std::move(loadedCameras);
            captures_ = std::move(loadedCaptures);
            report_ = loadedReport;
            singleCompatibility_ = std::move(loadedSingleCompatibility);
            graph_.clear();
            for (const auto& [cameraId, camera] : cameras_)
            {
                graph_.addCamera(cameraId);
            }
            isReady_ = true;
            return true;
        }
        catch (const cv::Exception& exception)
        {
            errorMessage = "Failed to parse calibration YAML file: " + std::string(exception.what());
            return false;
        }
    }
}
