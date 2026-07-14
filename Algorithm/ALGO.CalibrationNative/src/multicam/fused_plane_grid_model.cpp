#include "pch.h"

#include "fused_plane_grid_model.h"

#include <cmath>

namespace calib::multicam
{
    namespace
    {
        constexpr double kHomographySingularTolerance = 1.0e-12;
        constexpr double kHomographyInverseTolerance = 1.0e-7;
        constexpr double kPlaneZTolerance = 1.0e-9;

        bool isFinite(double value)
        {
            return std::isfinite(value);
        }

        bool isValidHomography(const cv::Mat& homography)
        {
            if (homography.empty() || homography.rows != 3 || homography.cols != 3 || homography.type() != CV_64F)
            {
                return false;
            }

            for (int row = 0; row < 3; ++row)
            {
                for (int col = 0; col < 3; ++col)
                {
                    if (!isFinite(homography.at<double>(row, col)))
                    {
                        return false;
                    }
                }
            }

            return std::abs(cv::determinant(homography)) > kHomographySingularTolerance;
        }

        bool normalizedProductIsIdentity(const cv::Mat& left, const cv::Mat& right, double& discrepancy)
        {
            cv::Mat product = left * right;
            const double w = product.at<double>(2, 2);
            if (std::abs(w) < kHomographySingularTolerance)
            {
                discrepancy = std::numeric_limits<double>::infinity();
                return false;
            }

            product /= w;
            const cv::Mat identity = cv::Mat::eye(3, 3, CV_64F);
            discrepancy = cv::norm(product - identity, cv::NORM_INF);
            return discrepancy <= kHomographyInverseTolerance;
        }

        bool checkMutualInverse(const cv::Mat& canvasToBoard, const cv::Mat& boardToCanvas, std::string& errorMessage)
        {
            double forwardDiscrepancy = 0.0;
            if (!normalizedProductIsIdentity(canvasToBoard, boardToCanvas, forwardDiscrepancy))
            {
                errorMessage = "H_canvas_to_board * H_board_to_canvas is not identity (inf-norm discrepancy=" +
                    std::to_string(forwardDiscrepancy) + ").";
                return false;
            }

            double reverseDiscrepancy = 0.0;
            if (!normalizedProductIsIdentity(boardToCanvas, canvasToBoard, reverseDiscrepancy))
            {
                errorMessage = "H_board_to_canvas * H_canvas_to_board is not identity (inf-norm discrepancy=" +
                    std::to_string(reverseDiscrepancy) + ").";
                return false;
            }

            return true;
        }

        bool applyHomography(const cv::Mat& homography, double x, double y, cv::Point2d& output)
        {
            const double hx = homography.at<double>(0, 0) * x + homography.at<double>(0, 1) * y + homography.at<double>(0, 2);
            const double hy = homography.at<double>(1, 0) * x + homography.at<double>(1, 1) * y + homography.at<double>(1, 2);
            const double hw = homography.at<double>(2, 0) * x + homography.at<double>(2, 1) * y + homography.at<double>(2, 2);
            if (!isFinite(hx) || !isFinite(hy) || !isFinite(hw) || std::abs(hw) < kHomographySingularTolerance)
            {
                return false;
            }

            output = cv::Point2d(hx / hw, hy / hw);
            return isFinite(output.x) && isFinite(output.y);
        }

        bool validateMatrixFinite(const cv::Mat& matrix)
        {
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

        bool validateGrid(
            const ResidualGrid& grid,
            ResidualGridDomain expectedDomain,
            const char* gridName,
            std::string& errorMessage)
        {
            if (grid.domain != expectedDomain)
            {
                errorMessage = std::string(gridName) + " has an unexpected residual grid domain.";
                return false;
            }

            if (!grid.enabled)
            {
                return true;
            }

            if (grid.cols < 2 || grid.rows < 2)
            {
                errorMessage = std::string(gridName) + " must have at least 2 rows and 2 columns.";
                return false;
            }
            if (!isFinite(grid.originX) || !isFinite(grid.originY) ||
                !isFinite(grid.stepX) || !isFinite(grid.stepY) ||
                grid.stepX <= 0.0 || grid.stepY <= 0.0)
            {
                errorMessage = std::string(gridName) + " origin and step must be finite and positive.";
                return false;
            }
            if (grid.dx.rows != grid.rows || grid.dx.cols != grid.cols ||
                grid.dy.rows != grid.rows || grid.dy.cols != grid.cols)
            {
                errorMessage = std::string(gridName) + " dx/dy dimensions do not match grid rows/cols.";
                return false;
            }
            if (grid.dx.type() != CV_64F || grid.dy.type() != CV_64F)
            {
                errorMessage = std::string(gridName) + " dx/dy matrices must be CV_64F.";
                return false;
            }
            if (!validateMatrixFinite(grid.dx) || !validateMatrixFinite(grid.dy))
            {
                errorMessage = std::string(gridName) + " dx/dy matrices must contain finite values.";
                return false;
            }
            if (!grid.validMask.empty() &&
                (grid.validMask.rows != grid.rows || grid.validMask.cols != grid.cols || grid.validMask.type() != CV_8U))
            {
                errorMessage = std::string(gridName) + " validMask must be CV_8U with the same grid dimensions.";
                return false;
            }
            return true;
        }

        bool getBilinearCell(const ResidualGrid& grid, double x, double y, int& x0, int& y0, double& tx, double& ty)
        {
            const double gx = (x - grid.originX) / grid.stepX;
            const double gy = (y - grid.originY) / grid.stepY;
            if (!isFinite(gx) || !isFinite(gy) || gx < 0.0 || gy < 0.0 || gx > grid.cols - 1.0 || gy > grid.rows - 1.0)
            {
                return false;
            }

            x0 = static_cast<int>(std::floor(gx));
            y0 = static_cast<int>(std::floor(gy));
            if (x0 == grid.cols - 1)
            {
                x0 = grid.cols - 2;
            }
            if (y0 == grid.rows - 1)
            {
                y0 = grid.rows - 2;
            }

            tx = gx - x0;
            ty = gy - y0;
            return true;
        }

        bool sampleGrid(const ResidualGrid& grid, double x, double y, cv::Point2d& residual, std::string& errorMessage)
        {
            residual = cv::Point2d(0.0, 0.0);
            if (!grid.enabled)
            {
                return true;
            }

            int x0 = 0;
            int y0 = 0;
            double tx = 0.0;
            double ty = 0.0;
            if (!getBilinearCell(grid, x, y, x0, y0, tx, ty))
            {
                errorMessage = "Point is outside residual grid.";
                return false;
            }

            const int x1 = x0 + 1;
            const int y1 = y0 + 1;
            if (!grid.validMask.empty())
            {
                if (grid.validMask.at<unsigned char>(y0, x0) == 0 ||
                    grid.validMask.at<unsigned char>(y0, x1) == 0 ||
                    grid.validMask.at<unsigned char>(y1, x0) == 0 ||
                    grid.validMask.at<unsigned char>(y1, x1) == 0)
                {
                    errorMessage = "Residual grid sample uses invalid mask cells.";
                    return false;
                }
            }

            const double w00 = (1.0 - tx) * (1.0 - ty);
            const double w10 = tx * (1.0 - ty);
            const double w01 = (1.0 - tx) * ty;
            const double w11 = tx * ty;

            residual.x =
                w00 * grid.dx.at<double>(y0, x0) +
                w10 * grid.dx.at<double>(y0, x1) +
                w01 * grid.dx.at<double>(y1, x0) +
                w11 * grid.dx.at<double>(y1, x1);
            residual.y =
                w00 * grid.dy.at<double>(y0, x0) +
                w10 * grid.dy.at<double>(y0, x1) +
                w01 * grid.dy.at<double>(y1, x0) +
                w11 * grid.dy.at<double>(y1, x1);

            if (!isFinite(residual.x) || !isFinite(residual.y))
            {
                errorMessage = "Residual grid sample produced non-finite values.";
                return false;
            }
            return true;
        }
    }

    FusedPlaneGridModel::FusedPlaneGridModel()
    {
        canvasToBoardResidual.domain = ResidualGridDomain::CanvasPixel;
        boardToCanvasResidual.domain = ResidualGridDomain::BoardWorld;
    }

    bool FusedPlaneGridModel::validate(std::string& errorMessage) const
    {
        if (virtualCameraId.empty())
        {
            errorMessage = "virtualCameraId must not be empty.";
            return false;
        }
        if (coordinateFrame != "BOARD_POSE")
        {
            errorMessage = "Only coordinateFrame BOARD_POSE is supported.";
            return false;
        }
        if (!isFinite(heightCompensation) || canvasWidth <= 0 || canvasHeight <= 0)
        {
            errorMessage = "Fused plane heightCompensation and canvas size are invalid.";
            return false;
        }
        if (!isValidHomography(HCanvasToBoard) || !isValidHomography(HBoardToCanvas))
        {
            errorMessage = "Fused plane homography matrices must be valid 3x3 finite CV_64F matrices.";
            return false;
        }
        if (!checkMutualInverse(HCanvasToBoard, HBoardToCanvas, errorMessage))
        {
            return false;
        }
        if (!validateGrid(canvasToBoardResidual, ResidualGridDomain::CanvasPixel, "canvasToBoardResidual", errorMessage))
        {
            return false;
        }
        return validateGrid(boardToCanvasResidual, ResidualGridDomain::BoardWorld, "boardToCanvasResidual", errorMessage);
    }

    bool FusedPlaneGridModel::pixelToWorld(const cv::Point2d& pixel, cv::Point3d& world, std::string& errorMessage) const
    {
        if (!validate(errorMessage))
        {
            return false;
        }
        if (pixel.x < 0.0 || pixel.y < 0.0 || pixel.x > canvasWidth - 1.0 || pixel.y > canvasHeight - 1.0)
        {
            errorMessage = "Pixel is outside fused canvas.";
            return false;
        }

        cv::Point2d base;
        if (!applyHomography(HCanvasToBoard, pixel.x, pixel.y, base))
        {
            errorMessage = "Failed to apply H_canvas_to_board.";
            return false;
        }

        cv::Point2d residual;
        if (!sampleGrid(canvasToBoardResidual, pixel.x, pixel.y, residual, errorMessage))
        {
            return false;
        }

        world = cv::Point3d(base.x + residual.x, base.y + residual.y, heightCompensation);
        return true;
    }

    bool FusedPlaneGridModel::worldToPixel(const cv::Point3d& world, cv::Point2d& pixel, std::string& errorMessage) const
    {
        if (!validate(errorMessage))
        {
            return false;
        }
        if (std::abs(world.z - heightCompensation) > kPlaneZTolerance)
        {
            errorMessage = "World Z does not match fused heightCompensation.";
            return false;
        }

        cv::Point2d base;
        if (!applyHomography(HBoardToCanvas, world.x, world.y, base))
        {
            errorMessage = "Failed to apply H_board_to_canvas.";
            return false;
        }

        cv::Point2d residual;
        if (!sampleGrid(boardToCanvasResidual, world.x, world.y, residual, errorMessage))
        {
            return false;
        }

        pixel = cv::Point2d(base.x + residual.x, base.y + residual.y);
        return true;
    }
}
