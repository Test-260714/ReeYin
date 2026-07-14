#pragma once

#include "multi_camera_types.h"
#include <opencv2/core.hpp>
#include <map>
#include <string>
#include <vector>

namespace calib::multicam
{
    enum class BlendMode
    {
        Overlay = 0,
        Average = 1
    };

    struct StitchImageInput
    {
        std::string cameraId;
        cv::Mat image;
    };

    struct StitchingOptions
    {
        double heightCompensation = 0.0;
        double worldUnitsPerPixel = 0.01;
        double originWorldX = 0.0;
        double originWorldY = 0.0;
        int outputWidth = 1000;
        int outputHeight = 1000;
        BlendMode blendMode = BlendMode::Average;
        std::string referenceCameraId;
        cv::Mat HBoardToCanvas;
        cv::Mat HCanvasToBoard;
    };

    class MultiCameraStitcher
    {
    public:
        bool stitch(
            const std::map<std::string, CameraRigParams>& cameras,
            const std::vector<StitchImageInput>& images,
            const StitchingOptions& options,
            cv::Mat& output,
            std::string& errorMessage) const;
    };
}
