#pragma once

#include <memory>
#include <string>

#include <openvino/openvino.hpp>


namespace OVLoader
{
    std::shared_ptr<ov::Model> LoadModel(ov::Core& core, const std::string& onnxPath);
}



