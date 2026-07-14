#pragma once

#include "model_base.h"
#include "interface/interface.h"

#include <memory>
#include <vector>
#include <string>


namespace DetectionSeg
{
	struct Param;
	class TRTInfer;

	class TRTModel : public ModelBase
	{
	public:

		int InitRuntime(const ModelConfig* modelConfig) override;

		int CleanUpRuntime() override;

		int Pipeline(void* imageData, int im_w, int im_h, int im_c, int im_type,
			         void* depthData, int d_w, int d_h, int d_c, int d_type, Result** objInfo, int& objectNum) override;

		int CleanUpResult(Result*& objInfo) override;

	private:
		std::shared_ptr<Param> _trt_param;
		std::shared_ptr<TRTInfer> _trt_net;

	};
}


