#pragma once

#include "multi_camera_types.h"

#include <map>
#include <string>

namespace calib::multicam
{
    struct BundleAdjustmentInput
    {
        MultiCameraOptions options;
        std::map<std::string, CameraRigParams> cameras;
        std::map<std::string, CapturePose> captures;
        std::vector<BoardObservation> observations;
    };

    struct BundleAdjustmentOutput
    {
        std::map<std::string, CameraRigParams> cameras;
        std::map<std::string, CapturePose> captures;
        MultiCameraReport report;
    };

    class BundleAdjuster
    {
    public:
        bool optimize(const BundleAdjustmentInput& input, BundleAdjustmentOutput& output, std::string& errorMessage) const;
    };
}
