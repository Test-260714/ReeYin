#include "pch.h"

#include "utils/ilogger.hpp"

#include "ov_model_loader.h"
#include "interface/encryption.h"

namespace OVLoader
{ 
    std::shared_ptr<ov::Model> LoadModel(ov::Core& core, const std::string& onnxPath)
    {
        int isCompressed = 0;
        /*int ret = File7zIsCompressed(onnxPath.c_str(), isCompressed);*/
        int ret = File7zIsCompressed2(onnxPath.c_str(), isCompressed);
        if (ret == 0 && isCompressed == 1)
        {
            unsigned char* raw = nullptr;
            size_t rawSize = 0;
            if (File7zExtract(onnxPath.c_str(), &raw, &rawSize) == 0)
            {
                std::string model_str(reinterpret_cast<const char*>(raw), rawSize);

                File7zFreeBuffer(raw);

                try
                {
                    ov::Tensor dummy_weights;
                    auto model = core.read_model(model_str, dummy_weights);
                    return model;
                }
                catch (const std::exception& e)
                {
                    INFOE("Exception: %s", e.what());
                    return nullptr;
                }
            }
            else
            {
                try
                {
                    return core.read_model(onnxPath);
                }
                catch (const std::exception& e)
                {
                    INFOE("Exception: %s", e.what());
                    return nullptr;
                }
            }
        }
        else
        {
            try
            {
                return core.read_model(onnxPath);
            }
            catch (const std::exception& e)
            {
                INFOE("Exception: %s", e.what());
                return nullptr;
            }
        }
    }
}