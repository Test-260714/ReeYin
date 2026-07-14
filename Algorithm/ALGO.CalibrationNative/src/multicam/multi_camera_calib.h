#pragma once

#include "../interface.h"
#include "../boards/calibration_board_calib.h"
#include "bundle_adjuster.h"
#include "common_plane_projector.h"
#include "fused_plane_grid_model.h"
#include "multi_camera_stitcher.h"
#include "observation_graph.h"

#include <map>
#include <memory>
#include <optional>
#include <string>
#include <vector>

namespace calib::multicam
{
    class MultiCameraCalibrationFramework
    {
    public:
        MultiCameraCalibrationFramework();

        bool setBoardParams(const CalibrationBoardParams& params, std::string& errorMessage);
        bool setMeasurementPlaneParams(const MeasurementPlaneParams& params, std::string& errorMessage);
        MeasurementPlaneParams getMeasurementPlaneParams() const;
        void setOptions(const MultiCameraOptions& options);
        bool addCamera(const std::string& cameraId, int imageWidth, int imageHeight, std::string& errorMessage);
        bool setInitialCameraParams(const std::string& cameraId, const CameraParams& params, bool fixedIntrinsic, std::string& errorMessage);
        bool addObservationImagePath(const std::string& cameraId, const std::string& captureId, const std::string& imagePath, std::string& errorMessage);
        bool calibrate(std::string& errorMessage);
        std::vector<std::string> cameraIds() const;
        bool getCameraParams(const std::string& cameraId, CameraRigParams& params) const;
        MultiCameraReport report() const;
        bool pixelToCommonWorld(const std::string& cameraId, double pixelX, double pixelY, cv::Point3d& worldPoint, std::string& errorMessage) const;
        bool commonWorldToPixel(const std::string& cameraId, const cv::Point3d& worldPoint, cv::Point2d& pixelPoint, std::string& errorMessage) const;
        bool stitch(const std::vector<StitchImageInput>& images, BlendMode blendMode, cv::Mat& output, std::string& errorMessage) const;
        bool hasSingleCompatibility() const;
        bool save(const std::string& outputFile, std::string& errorMessage) const;
        bool load(const std::string& filePath, std::string& errorMessage);

    private:
        bool initializeIntrinsics(std::string& errorMessage);
        bool initializeObservationPoses(std::string& errorMessage);
        bool initializeCommonPoses(std::string& errorMessage);
        bool estimateStitchingOptions(const std::vector<StitchImageInput>& images, double heightCompensation, BlendMode blendMode, StitchingOptions& options, std::string& errorMessage) const;
        bool createDefaultSingleCompatibility(double heightCompensation, FusedPlaneGridModel& model, std::string& errorMessage) const;

        CalibrationBoardParams boardParams_{};
        MeasurementPlaneParams measurementPlaneParams_{};
        bool hasBoardParams_ = false;
        MultiCameraOptions options_;
        std::map<std::string, CameraRigParams> cameras_;
        std::map<std::string, CapturePose> captures_;
        ObservationGraph graph_;
        std::unique_ptr<calib::boards::ICalibrationBoardCalibrator> boardCalibrator_;
        BundleAdjuster bundleAdjuster_;
        CommonPlaneProjector projector_;
        MultiCameraStitcher stitcher_;
        MultiCameraReport report_;
        std::optional<FusedPlaneGridModel> singleCompatibility_;
        bool isReady_ = false;
    };
}
