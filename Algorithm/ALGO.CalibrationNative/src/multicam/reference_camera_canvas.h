#pragma once

#include "multi_camera_stitcher.h"
#include "multi_camera_types.h"

#include <opencv2/core.hpp>

#include <map>
#include <string>
#include <vector>

namespace calib::multicam
{
    struct ReferenceCameraCanvas
    {
        std::string referenceCameraId;
        cv::Size outputSize;
        double offsetX = 0.0;
        double offsetY = 0.0;
        double approximateWorldUnitsPerPixel = 0.0;
        double minWorldX = 0.0;
        double minWorldY = 0.0;
        double maxWorldX = 0.0;
        double maxWorldY = 0.0;
        cv::Mat HBoardToCanvas;
        cv::Mat HCanvasToBoard;
    };

    bool buildReferenceCameraCanvas(
        const std::map<std::string, CameraRigParams>& cameras,
        const std::vector<StitchImageInput>& images,
        double heightCompensation,
        ReferenceCameraCanvas& canvas,
        std::string& errorMessage);
}
