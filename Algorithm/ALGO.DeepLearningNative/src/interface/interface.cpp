#include "pch.h"
#include "interface.h"
#include "ilogger.hpp"

#include "model_factory.h"

#include <map>
#include <memory>
#include <mutex>
#include <unordered_map>
#include <utility>

namespace
{
	struct ManagedModel
	{
		explicit ManagedModel(std::shared_ptr<ModelBase> model)
			: model(std::move(model))
		{
		}

		std::shared_ptr<ModelBase> model;
		std::mutex operationMutex;
	};
}

static std::unordered_map<ModelHandle, std::shared_ptr<ManagedModel>> g_modelMap;
static std::mutex g_modelMutex;


extern "C"
{
	DEEP_LEARNING_SDK_API ModelHandle CreateModel(const ModelConfig* config)
	{
		try
		{
			auto model = ModelFactory::createModel(config);

			if (!model)
			{
				return nullptr;
			}

			auto managedModel = std::make_shared<ManagedModel>(model);
			ModelHandle handle = reinterpret_cast<ModelHandle>(model.get());

			{
				std::lock_guard<std::mutex> lock(g_modelMutex);
				g_modelMap[handle] = managedModel;
			}

			return handle;

		}
		catch (...) 
		{
			return nullptr;
		}
	}


	DEEP_LEARNING_SDK_API int DestroyModel(ModelHandle handle)
	{
		try
		{
			std::shared_ptr<ManagedModel> managedModel;
			{
				std::lock_guard<std::mutex> lock(g_modelMutex);
				auto it = g_modelMap.find(handle);
				if (it == g_modelMap.end())
				{
					return -1;
				}

				managedModel = it->second;
				g_modelMap.erase(it);
			}

			std::lock_guard<std::mutex> operationLock(managedModel->operationMutex);
			if (managedModel->model != nullptr)
			{
				managedModel->model->CleanUpRuntime();
				managedModel->model.reset();
				return 0;
			}
			return -1;
		}
		catch (...)
		{
			return -1;
		}
	}


	DEEP_LEARNING_SDK_API int InitRuntime(ModelHandle handle, const ModelConfig* config)
	{
		try
		{
			std::shared_ptr<ManagedModel> managedModel;
			{
				std::lock_guard<std::mutex> lock(g_modelMutex);
				auto it = g_modelMap.find(handle);
				if (it == g_modelMap.end())
				{
					return -1;
				}

				managedModel = it->second;
			}

			std::lock_guard<std::mutex> operationLock(managedModel->operationMutex);
			if (managedModel->model != nullptr)
			{
				return managedModel->model->InitRuntime(config);
			}
			return -1;
		}
		catch (...)
		{
			return -1;
		}
	}


	DEEP_LEARNING_SDK_API int Pipeline(ModelHandle handle, 
		                               void* imageData, int im_w, int im_h, int im_c, int im_type, 
		                               void* depthData, int d_w, int d_h, int d_c, int d_type,
		                               Result** objInfo, int& objectNum)
	{
		try
		{
			std::shared_ptr<ManagedModel> managedModel;
			{
				std::lock_guard<std::mutex> lock(g_modelMutex);
				auto it = g_modelMap.find(handle);
				if (it == g_modelMap.end())
				{
					return -1;
				}

				managedModel = it->second;
			}

			std::lock_guard<std::mutex> operationLock(managedModel->operationMutex);
			if (managedModel->model != nullptr)
			{
				return managedModel->model->Pipeline(imageData, im_w, im_h, im_c, im_type,
					                        depthData, d_w, d_h, d_c, d_type, 
					                        objInfo, objectNum);
			}
			return -1;
		}
		catch (...)
		{
			return -1;
		}
	}

	DEEP_LEARNING_SDK_API int CleanUpResult(ModelHandle handle, Result*& objInfo)
	{
		try
		{
			std::shared_ptr<ManagedModel> managedModel;
			{
				std::lock_guard<std::mutex> lock(g_modelMutex);
				auto it = g_modelMap.find(handle);
				if (it == g_modelMap.end())
				{
					return -1;
				}

				managedModel = it->second;
			}

			std::lock_guard<std::mutex> operationLock(managedModel->operationMutex);
			if (managedModel->model != nullptr)
			{
				return managedModel->model->CleanUpResult(objInfo);
			}
			return -1;
		}
		catch (...)
		{
			return -1;
		}
	}

}





