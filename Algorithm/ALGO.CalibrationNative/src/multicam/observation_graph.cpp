#include "pch.h"

#include "multicam/observation_graph.h"

#include <queue>

namespace calib::multicam
{
    namespace
    {
        std::string cameraNode(const std::string& id)
        {
            return "C:" + id;
        }

        std::string captureNode(const std::string& id)
        {
            return "P:" + id;
        }

        std::string nodeId(const std::string& node)
        {
            return node.substr(2);
        }
    }

    void ObservationGraph::clear()
    {
        cameraIds_.clear();
        captureIds_.clear();
        observationsByCamera_.clear();
        observationsByCapture_.clear();
    }

    void ObservationGraph::addCamera(const std::string& cameraId)
    {
        cameraIds_.insert(cameraId);
    }

    void ObservationGraph::addObservation(const BoardObservation& observation)
    {
        cameraIds_.insert(observation.cameraId);
        captureIds_.insert(observation.captureId);
        observationsByCamera_[observation.cameraId].push_back(observation);
        observationsByCapture_[observation.captureId].push_back(observation);
    }

    const std::map<std::string, std::vector<BoardObservation>>& ObservationGraph::observationsByCamera() const
    {
        return observationsByCamera_;
    }

    std::vector<BoardObservation> ObservationGraph::observationsForCamera(const std::string& cameraId) const
    {
        const auto found = observationsByCamera_.find(cameraId);
        if (found == observationsByCamera_.end())
        {
            return {};
        }
        return found->second;
    }

    std::vector<BoardObservation> ObservationGraph::observationsForCapture(const std::string& captureId) const
    {
        const auto found = observationsByCapture_.find(captureId);
        if (found == observationsByCapture_.end())
        {
            return {};
        }
        return found->second;
    }

    std::set<std::string> ObservationGraph::cameraIds() const
    {
        return cameraIds_;
    }

    std::set<std::string> ObservationGraph::captureIds() const
    {
        return captureIds_;
    }

    GraphConnectivityResult ObservationGraph::validateConnected() const
    {
        std::map<std::string, std::vector<std::string>> adjacency;
        int edgeCount = 0;

        for (const auto& cameraId : cameraIds_)
        {
            adjacency[cameraNode(cameraId)];
        }
        for (const auto& captureId : captureIds_)
        {
            adjacency[captureNode(captureId)];
        }
        for (const auto& [cameraId, observations] : observationsByCamera_)
        {
            for (const auto& observation : observations)
            {
                adjacency[cameraNode(observation.cameraId)].push_back(captureNode(observation.captureId));
                adjacency[captureNode(observation.captureId)].push_back(cameraNode(observation.cameraId));
                ++edgeCount;
            }
        }

        GraphConnectivityResult result;
        std::set<std::string> visited;
        std::vector<std::set<std::string>> components;

        for (const auto& [startNode, _] : adjacency)
        {
            if (visited.find(startNode) != visited.end())
            {
                continue;
            }

            std::set<std::string> currentComponent;
            std::queue<std::string> pending;
            pending.push(startNode);
            visited.insert(startNode);

            while (!pending.empty())
            {
                const auto current = pending.front();
                pending.pop();
                currentComponent.insert(current);

                const auto neighbors = adjacency.find(current);
                if (neighbors == adjacency.end())
                {
                    continue;
                }

                for (const auto& neighbor : neighbors->second)
                {
                    if (visited.insert(neighbor).second)
                    {
                        pending.push(neighbor);
                    }
                }
            }

            components.push_back(currentComponent);
        }

        result.componentCount = static_cast<int>(components.size());
        if (components.empty())
        {
            result.connected = false;
            return result;
        }

        if (edgeCount == 0)
        {
            for (const auto& component : components)
            {
                for (const auto& node : component)
                {
                    if (node.rfind("C:", 0) == 0)
                    {
                        result.disconnectedCameraIds.push_back(nodeId(node));
                    }
                    else if (node.rfind("P:", 0) == 0)
                    {
                        result.disconnectedCaptureIds.push_back(nodeId(node));
                    }
                }
            }
            result.connected = false;
            return result;
        }

        std::size_t largestComponentIndex = 0;
        for (std::size_t index = 1; index < components.size(); ++index)
        {
            if (components[index].size() > components[largestComponentIndex].size())
            {
                largestComponentIndex = index;
            }
        }

        const auto& largestComponent = components[largestComponentIndex];
        for (const auto& component : components)
        {
            for (const auto& node : component)
            {
                if (largestComponent.find(node) != largestComponent.end())
                {
                    continue;
                }

                if (node.rfind("C:", 0) == 0)
                {
                    result.disconnectedCameraIds.push_back(nodeId(node));
                }
                else if (node.rfind("P:", 0) == 0)
                {
                    result.disconnectedCaptureIds.push_back(nodeId(node));
                }
            }
        }

        result.connected = result.componentCount == 1;
        return result;
    }
}
