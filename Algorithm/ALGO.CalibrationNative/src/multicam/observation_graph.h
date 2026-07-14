#pragma once

#include "multi_camera_types.h"

#include <map>
#include <set>
#include <string>
#include <vector>

namespace calib::multicam
{
    struct GraphConnectivityResult
    {
        bool connected = false;
        int componentCount = 0;
        std::vector<std::string> disconnectedCameraIds;
        std::vector<std::string> disconnectedCaptureIds;
    };

    class ObservationGraph
    {
    public:
        void clear();
        void addCamera(const std::string& cameraId);
        void addObservation(const BoardObservation& observation);

        const std::map<std::string, std::vector<BoardObservation>>& observationsByCamera() const;
        std::vector<BoardObservation> observationsForCamera(const std::string& cameraId) const;
        std::vector<BoardObservation> observationsForCapture(const std::string& captureId) const;
        std::set<std::string> cameraIds() const;
        std::set<std::string> captureIds() const;
        GraphConnectivityResult validateConnected() const;

    private:
        std::set<std::string> cameraIds_;
        std::set<std::string> captureIds_;
        std::map<std::string, std::vector<BoardObservation>> observationsByCamera_;
        std::map<std::string, std::vector<BoardObservation>> observationsByCapture_;
    };
}
