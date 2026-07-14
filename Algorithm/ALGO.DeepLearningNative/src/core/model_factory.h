#pragma once

#include "interface/interface.h"
#include "model_base.h"

#include <memory>
#include <string>



class ModelFactory 
{
public:
    ~ModelFactory() = default;

    static std::shared_ptr<ModelBase> createModel(const ModelConfig* config);

};








