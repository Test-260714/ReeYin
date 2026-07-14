#pragma once

#include "multi_camera_types.h"

namespace calib::multicam
{
    class CommonPlaneProjector
    {
    public:
        bool pixelToCommonWorld(
            const CameraRigParams& camera,
            double pixelX,
            double pixelY,
            double heightCompensation,
            cv::Point3d& worldPoint,
            std::string& errorMessage) const;

        bool commonWorldToPixel(
            const CameraRigParams& camera,
            const cv::Point3d& worldPoint,
            cv::Point2d& pixelPoint,
            std::string& errorMessage) const;
    };
}
