#pragma once

#include "interface/interface.h"
#include <memory>
#include <string>



class ModelBase
{
public:
    virtual ~ModelBase() = default;

    virtual int InitRuntime(const ModelConfig* modelConfig) = 0;

    virtual int CleanUpRuntime() = 0;

    virtual int Pipeline(void* imageData, int im_w, int im_h, int im_c, int im_type, 
                         void* depthData, int d_w, int d_h, int d_c, int d_type, 
                         Result** objInfo, int& objectNum) = 0;
    
    virtual int CleanUpResult(Result*& objInfo) = 0;
};
