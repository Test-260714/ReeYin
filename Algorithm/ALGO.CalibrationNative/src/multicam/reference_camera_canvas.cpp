#include "pch.h"

#include "multicam/reference_camera_canvas.h"

#include "multicam/common_plane_projector.h"

#include <opencv2/calib3d.hpp>

#include <algorithm>
#include <cctype>
#include <cmath>
#include <limits>
#include <sstream>

namespace calib::multicam
{
    namespace
    {
        constexpr double kMaxReferenceCanvasGeometryPixels = 10000000000.0;
        constexpr double kTieTolerance = 1.0e-9;
        constexpr double kHomographyTolerance = 1.0e-12;
        constexpr double kInverseTolerance = 1.0e-7;

        struct ProjectedCorner
        {
            std::string cameraId;
            cv::Point2d sourcePixel;
            cv::Point3d worldPoint;
            cv::Point2d referencePixel;
        };

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
                const unsigned char leftChar = static_cast<unsigned char>(left[leftIndex]);
                const unsigned char rightChar = static_cast<unsigned char>(right[rightIndex]);
                if (std::isdigit(leftChar) && std::isdigit(rightChar))
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

        bool naturalLess(const std::string& left, const std::string& right)
        {
            return compareNatural(left, right) < 0;
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

        CameraRigParams makeUndistortedCamera(const CameraRigParams& camera)
        {
            CameraRigParams undistorted = camera;
            undistorted.distortion.release();
            return undistorted;
        }

        std::vector<cv::Point2d> imageCorners(const cv::Size& size)
        {
            return {
                cv::Point2d(0.0, 0.0),
                cv::Point2d(static_cast<double>(size.width - 1), 0.0),
                cv::Point2d(static_cast<double>(size.width - 1), static_cast<double>(size.height - 1)),
                cv::Point2d(0.0, static_cast<double>(size.height - 1))
            };
        }

        cv::Point2d imageCenter(const cv::Size& size)
        {
            return cv::Point2d(
                static_cast<double>(size.width - 1) * 0.5,
                static_cast<double>(size.height - 1) * 0.5);
        }

        void initializeBounds(const cv::Point3d& point, ReferenceCameraCanvas& canvas)
        {
            canvas.minWorldX = point.x;
            canvas.minWorldY = point.y;
            canvas.maxWorldX = point.x;
            canvas.maxWorldY = point.y;
        }

        void updateWorldBounds(const cv::Point3d& point, ReferenceCameraCanvas& canvas)
        {
            canvas.minWorldX = std::min(canvas.minWorldX, point.x);
            canvas.minWorldY = std::min(canvas.minWorldY, point.y);
            canvas.maxWorldX = std::max(canvas.maxWorldX, point.x);
            canvas.maxWorldY = std::max(canvas.maxWorldY, point.y);
        }

        bool applyHomography(const cv::Mat& homography, const cv::Point2d& input, cv::Point2d& output)
        {
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
            if (!isFinite(x) || !isFinite(y) || !isFinite(w) || std::abs(w) < kHomographyTolerance)
            {
                return false;
            }

            output = cv::Point2d(x / w, y / w);
            return isFinitePoint(output);
        }

        bool normalizedProductIsIdentity(const cv::Mat& left, const cv::Mat& right)
        {
            cv::Mat product = left * right;
            const double w = product.at<double>(2, 2);
            if (std::abs(w) < kHomographyTolerance)
            {
                return false;
            }
            product /= w;
            return cv::norm(product - cv::Mat::eye(3, 3, CV_64F), cv::NORM_INF) <= kInverseTolerance;
        }

        bool validateHomographyPair(const cv::Mat& boardToCanvas, const cv::Mat& canvasToBoard)
        {
            if (boardToCanvas.empty() || canvasToBoard.empty() ||
                boardToCanvas.rows != 3 || boardToCanvas.cols != 3 ||
                canvasToBoard.rows != 3 || canvasToBoard.cols != 3 ||
                boardToCanvas.type() != CV_64F || canvasToBoard.type() != CV_64F ||
                !hasFiniteValues(boardToCanvas) || !hasFiniteValues(canvasToBoard))
            {
                return false;
            }
            return std::abs(cv::determinant(boardToCanvas)) > kHomographyTolerance &&
                std::abs(cv::determinant(canvasToBoard)) > kHomographyTolerance &&
                normalizedProductIsIdentity(boardToCanvas, canvasToBoard) &&
                normalizedProductIsIdentity(canvasToBoard, boardToCanvas);
        }

        bool estimateLocalWorldUnitsPerPixel(const cv::Mat& canvasToBoard, const cv::Size& outputSize, double& scale)
        {
            scale = 0.0;
            if (outputSize.width <= 1 || outputSize.height <= 1)
            {
                return false;
            }

            const cv::Point2d center(
                static_cast<double>(outputSize.width - 1) * 0.5,
                static_cast<double>(outputSize.height - 1) * 0.5);
            const cv::Point2d xNeighbor(center.x + 1.0 <= outputSize.width - 1 ? center.x + 1.0 : center.x - 1.0, center.y);
            const cv::Point2d yNeighbor(center.x, center.y + 1.0 <= outputSize.height - 1 ? center.y + 1.0 : center.y - 1.0);

            cv::Point2d centerWorld;
            cv::Point2d xWorld;
            cv::Point2d yWorld;
            if (!applyHomography(canvasToBoard, center, centerWorld) ||
                !applyHomography(canvasToBoard, xNeighbor, xWorld) ||
                !applyHomography(canvasToBoard, yNeighbor, yWorld))
            {
                return false;
            }

            const double dx = cv::norm(xWorld - centerWorld) / std::abs(xNeighbor.x - center.x);
            const double dy = cv::norm(yWorld - centerWorld) / std::abs(yNeighbor.y - center.y);
            if (!isFinite(dx) || !isFinite(dy) || dx <= 0.0 || dy <= 0.0)
            {
                return false;
            }

            scale = (dx + dy) * 0.5;
            return isFinite(scale) && scale > 0.0;
        }

        bool projectImageCornersToWorld(
            const StitchImageInput& imageInput,
            const CameraRigParams& camera,
            double heightCompensation,
            CommonPlaneProjector& projector,
            std::vector<ProjectedCorner>& projectedCorners,
            ReferenceCameraCanvas& canvas,
            bool& hasBounds,
            std::string& errorMessage)
        {
            const CameraRigParams undistortedCamera = makeUndistortedCamera(camera);
            for (const cv::Point2d& corner : imageCorners(imageInput.image.size()))
            {
                cv::Point3d worldPoint;
                std::string projectionError;
                if (!projector.pixelToCommonWorld(
                    undistortedCamera,
                    corner.x,
                    corner.y,
                    heightCompensation,
                    worldPoint,
                    projectionError))
                {
                    errorMessage = "Failed to project image corner for camera '" + imageInput.cameraId + "': " + projectionError;
                    return false;
                }
                if (!isFinitePoint(worldPoint))
                {
                    errorMessage = "Projection for camera '" + imageInput.cameraId + "' produced non-finite world coordinates.";
                    return false;
                }

                if (!hasBounds)
                {
                    initializeBounds(worldPoint, canvas);
                    hasBounds = true;
                }
                else
                {
                    updateWorldBounds(worldPoint, canvas);
                }
                projectedCorners.push_back({ imageInput.cameraId, corner, worldPoint, cv::Point2d() });
            }
            return true;
        }
    }

    bool buildReferenceCameraCanvas(
        const std::map<std::string, CameraRigParams>& cameras,
        const std::vector<StitchImageInput>& images,
        double heightCompensation,
        ReferenceCameraCanvas& canvas,
        std::string& errorMessage)
    {
        canvas = ReferenceCameraCanvas{};
        errorMessage.clear();
        if (cameras.empty())
        {
            errorMessage = "No cameras are available for reference camera canvas.";
            return false;
        }
        if (images.empty())
        {
            errorMessage = "No stitch images are available for reference camera canvas.";
            return false;
        }
        if (!isFinite(heightCompensation))
        {
            errorMessage = "heightCompensation must be finite.";
            return false;
        }

        CommonPlaneProjector projector;
        std::vector<ProjectedCorner> projectedCorners;
        projectedCorners.reserve(images.size() * 4);

        bool hasBounds = false;
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
            const auto camera = cameras.find(imageInput.cameraId);
            if (camera == cameras.end())
            {
                errorMessage = "Image references missing camera '" + imageInput.cameraId + "'.";
                return false;
            }
            if (!projectImageCornersToWorld(
                imageInput,
                camera->second,
                heightCompensation,
                projector,
                projectedCorners,
                canvas,
                hasBounds,
                errorMessage))
            {
                return false;
            }
        }

        if (!hasBounds || projectedCorners.size() < 4)
        {
            errorMessage = "Reference camera canvas requires at least four projected image corners.";
            return false;
        }

        const cv::Point2d worldCenter(
            (canvas.minWorldX + canvas.maxWorldX) * 0.5,
            (canvas.minWorldY + canvas.maxWorldY) * 0.5);

        bool hasReference = false;
        double bestScore = std::numeric_limits<double>::infinity();
        for (const StitchImageInput& imageInput : images)
        {
            const auto camera = cameras.find(imageInput.cameraId);
            if (camera == cameras.end())
            {
                continue;
            }

            cv::Point3d centerWorld;
            std::string projectionError;
            const CameraRigParams undistortedCamera = makeUndistortedCamera(camera->second);
            const cv::Point2d centerPixel = imageCenter(imageInput.image.size());
            if (!projector.pixelToCommonWorld(
                undistortedCamera,
                centerPixel.x,
                centerPixel.y,
                heightCompensation,
                centerWorld,
                projectionError))
            {
                errorMessage = "Failed to project image center for camera '" + imageInput.cameraId + "': " + projectionError;
                return false;
            }

            const double dx = centerWorld.x - worldCenter.x;
            const double dy = centerWorld.y - worldCenter.y;
            const double score = dx * dx + dy * dy;
            if (!isFinite(score))
            {
                errorMessage = "Reference camera selection produced a non-finite score for camera '" + imageInput.cameraId + "'.";
                return false;
            }
            if (!hasReference ||
                score + kTieTolerance < bestScore ||
                (std::abs(score - bestScore) <= kTieTolerance && naturalLess(imageInput.cameraId, canvas.referenceCameraId)))
            {
                canvas.referenceCameraId = imageInput.cameraId;
                bestScore = score;
                hasReference = true;
            }
        }

        if (!hasReference)
        {
            errorMessage = "Failed to select a reference camera for stitching.";
            return false;
        }

        const auto referenceCameraFound = cameras.find(canvas.referenceCameraId);
        if (referenceCameraFound == cameras.end())
        {
            errorMessage = "Selected reference camera does not exist.";
            return false;
        }
        const CameraRigParams referenceCamera = makeUndistortedCamera(referenceCameraFound->second);

        bool hasReferenceBounds = false;
        double minRefU = 0.0;
        double minRefV = 0.0;
        double maxRefU = 0.0;
        double maxRefV = 0.0;
        std::vector<cv::Point2d> boardPoints;
        std::vector<cv::Point2d> canvasPoints;
        boardPoints.reserve(projectedCorners.size());
        canvasPoints.reserve(projectedCorners.size());

        for (ProjectedCorner& corner : projectedCorners)
        {
            std::string projectionError;
            if (!projector.commonWorldToPixel(referenceCamera, corner.worldPoint, corner.referencePixel, projectionError))
            {
                errorMessage = "Failed to project world corner into reference camera '" +
                    canvas.referenceCameraId + "' for source camera '" + corner.cameraId + "': " + projectionError;
                return false;
            }
            if (!isFinitePoint(corner.referencePixel))
            {
                errorMessage = "Reference camera projection produced a non-finite pixel for source camera '" + corner.cameraId + "'.";
                return false;
            }

            if (!hasReferenceBounds)
            {
                minRefU = corner.referencePixel.x;
                minRefV = corner.referencePixel.y;
                maxRefU = corner.referencePixel.x;
                maxRefV = corner.referencePixel.y;
                hasReferenceBounds = true;
            }
            else
            {
                minRefU = std::min(minRefU, corner.referencePixel.x);
                minRefV = std::min(minRefV, corner.referencePixel.y);
                maxRefU = std::max(maxRefU, corner.referencePixel.x);
                maxRefV = std::max(maxRefV, corner.referencePixel.y);
            }
        }

        if (!hasReferenceBounds ||
            !isFinite(minRefU) || !isFinite(minRefV) ||
            !isFinite(maxRefU) || !isFinite(maxRefV) ||
            maxRefU <= minRefU || maxRefV <= minRefV)
        {
            errorMessage = "Reference camera canvas bounds are invalid.";
            return false;
        }

        const double widthPixels = std::ceil(maxRefU - minRefU) + 1.0;
        const double heightPixels = std::ceil(maxRefV - minRefV) + 1.0;
        if (!isFinite(widthPixels) || !isFinite(heightPixels) ||
            widthPixels <= 1.0 || heightPixels <= 1.0 ||
            widthPixels > static_cast<double>(std::numeric_limits<int>::max()) ||
            heightPixels > static_cast<double>(std::numeric_limits<int>::max()) ||
            widthPixels * heightPixels > kMaxReferenceCanvasGeometryPixels)
        {
            std::ostringstream message;
            message << "Reference camera canvas output size is invalid or too large: "
                << widthPixels << "x" << heightPixels << ".";
            errorMessage = message.str();
            return false;
        }

        canvas.offsetX = minRefU;
        canvas.offsetY = minRefV;
        canvas.outputSize = cv::Size(static_cast<int>(widthPixels), static_cast<int>(heightPixels));

        for (const ProjectedCorner& corner : projectedCorners)
        {
            boardPoints.push_back(cv::Point2d(corner.worldPoint.x, corner.worldPoint.y));
            canvasPoints.push_back(cv::Point2d(
                corner.referencePixel.x - canvas.offsetX,
                corner.referencePixel.y - canvas.offsetY));
        }

        try
        {
            canvas.HBoardToCanvas = cv::findHomography(boardPoints, canvasPoints, 0);
        }
        catch (const cv::Exception& exception)
        {
            errorMessage = std::string("cv::findHomography failed for reference camera canvas: ") + exception.what();
            return false;
        }
        if (canvas.HBoardToCanvas.empty())
        {
            errorMessage = "cv::findHomography failed for reference camera canvas.";
            return false;
        }
        if (canvas.HBoardToCanvas.type() != CV_64F)
        {
            canvas.HBoardToCanvas.convertTo(canvas.HBoardToCanvas, CV_64F);
        }
        canvas.HCanvasToBoard = canvas.HBoardToCanvas.inv();
        if (!validateHomographyPair(canvas.HBoardToCanvas, canvas.HCanvasToBoard))
        {
            errorMessage = "Reference camera canvas homographies must be finite, non-singular, and mutually inverse.";
            return false;
        }
        if (!estimateLocalWorldUnitsPerPixel(canvas.HCanvasToBoard, canvas.outputSize, canvas.approximateWorldUnitsPerPixel))
        {
            errorMessage = "Failed to estimate reference camera canvas worldUnitsPerPixel metadata.";
            return false;
        }

        return true;
    }
}
