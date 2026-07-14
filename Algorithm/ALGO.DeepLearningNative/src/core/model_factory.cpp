#include "pch.h"
#include "model_factory.h"

#include "tensorrt/classification/trt_app_classification.h"
#include "tensorrt/segmentation/trt_app_segmentation.h"
#include "tensorrt/detectionBbox/trt_app_detectionBbox.h"
#include "tensorrt/detectionSeg/trt_app_detectionSeg.h"
#include "tensorrt/detectionObbox/trt_app_detectionObbox.h"
#include "tensorrt/detectionKpt/trt_app_detectionKpt.h"
#include "tensorrt/anomalyDetection/trt_app_anomaly_detection.h"

#include "openvino/classification/ov_app_classification.h"
#include "openvino/segmentation/ov_app_segmentation.h"
#include "openvino/detectionBbox/ov_app_detectionBbox.h"
#include "openvino/detectionSeg/ov_app_detectionSeg.h"
#include "openvino/detectionObbox/ov_app_detectionObbox.h"
#include "openvino/detectionKpt/ov_app_detectionKpt.h"
#include "openvino/anomalyDetection/ov_app_anomaly_detection.h"


std::shared_ptr<ModelBase> ModelFactory::createModel(const ModelConfig* config)
{
	try
	{
		if (config->device_type == DEVICE_GPU)
		{
			if (config->model_type == ModelType::MODEL_CLASSIFICATION)
			{
				return std::make_shared<Classification::TRTModel>();
			}
			if (config->model_type == ModelType::MODEL_SEGMENTATION)
			{
                return std::make_shared<Segmentation::TRTModel>();
			}
			if (config->model_type == ModelType::MODEL_DETECTION_BBOX)
			{
				return std::make_shared<DetectionBbox::TRTModel>();
			}
			if (config->model_type == ModelType::MODEL_DETECTION_SEG)
			{
				return std::make_shared<DetectionSeg::TRTModel>();
			}
			if (config->model_type == ModelType::MODEL_DETECTION_OBB)
			{
				return std::make_shared<DetectionObbox::TRTModel>();
			}
			if (config->model_type == ModelType::MODEL_DETECTION_KPT)
			{
                return std::make_shared<DetectionKpt::TRTModel>();
			}
			if (config->model_type == ModelType::MODEL_ANOMALY_DETECTION)
			{
				return std::make_shared<AnomalyDetection::TRTModel>();
			}
		}
		else if (config->device_type == DEVICE_CPU)
        {
            if (config->model_type == ModelType::MODEL_CLASSIFICATION)
            {
                return std::make_shared<Classification::OVModel>();
            }
			if(config->model_type == ModelType::MODEL_SEGMENTATION)
            {
                return std::make_shared<Segmentation::OVModel>();
            }
			if (config->model_type == ModelType::MODEL_DETECTION_BBOX)
			{
				return std::make_shared<DetectionBbox::OVModel>();
			}
			if (config->model_type == ModelType::MODEL_DETECTION_SEG)
			{
				return std::make_shared<DetectionSeg::OVModel>();
			}
			if (config->model_type == ModelType::MODEL_DETECTION_OBB)
			{
				return std::make_shared<DetectionObbox::OVModel>();
			}
			if (config->model_type == ModelType::MODEL_DETECTION_KPT)
			{
				return std::make_shared<DetectionKpt::OVModel>();
			}
			if (config->model_type == ModelType::MODEL_ANOMALY_DETECTION)
			{
				return std::make_shared<AnomalyDetection::OVModel>();
			}
        }
		return nullptr;
	}
	catch (...)
	{
		return nullptr;
	}
}




