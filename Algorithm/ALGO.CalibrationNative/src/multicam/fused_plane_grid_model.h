#pragma once

#include <opencv2/core.hpp>

#include <string>

namespace calib::multicam
{
    enum class ResidualGridDomain
    {
        CanvasPixel = 0,
        BoardWorld = 1
    };

    struct ResidualGrid
    {
        ResidualGridDomain domain = ResidualGridDomain::CanvasPixel;
        double originX = 0.0;
        double originY = 0.0;
        double stepX = 1.0;
        double stepY = 1.0;
        int cols = 0;
        int rows = 0;
        cv::Mat dx;
        cv::Mat dy;
        cv::Mat validMask;
        bool enabled = false;
    };

    struct FusedPlaneGridModel
    {
        FusedPlaneGridModel();

        std::string virtualCameraId = "FUSED_CANVAS";
        std::string coordinateFrame = "BOARD_POSE";
        double heightCompensation = 0.0;
        int canvasWidth = 0;
        int canvasHeight = 0;
        cv::Mat HCanvasToBoard;
        cv::Mat HBoardToCanvas;
        ResidualGrid canvasToBoardResidual;
        ResidualGrid boardToCanvasResidual;

        bool validate(std::string& errorMessage) const;
        bool pixelToWorld(const cv::Point2d& pixel, cv::Point3d& world, std::string& errorMessage) const;
        bool worldToPixel(const cv::Point3d& world, cv::Point2d& pixel, std::string& errorMessage) const;
    };
}
