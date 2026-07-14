#pragma once
#include <cstdlib>
#include <string.h>

#ifdef DEEP_LEARNING_SDK_EXPORTS
#define DEEP_LEARNING_SDK_API __declspec(dllexport)
#else
#define DEEP_LEARNING_SDK_API __declspec(dllimport)
#endif

extern "C" 
{
    // 句柄类型
    typedef void* ModelHandle;


    // 推理设备类型
    typedef enum
    {
        DEVICE_CPU = 0,    // CPU推理 (OpenVINO)
        DEVICE_GPU = 1     // GPU推理 (TensorRT)
    } DeviceType;


    // 模型类型
    typedef enum
    {
        MODEL_CLASSIFICATION = 0,      // 分类模型
        MODEL_DETECTION_BBOX = 1,      // 常规目标检测
        MODEL_DETECTION_SEG = 2,       // 实例分割
        MODEL_DETECTION_OBB = 3,       // 旋转框检测
        MODEL_DETECTION_KPT = 4,       // 关键点检测
        MODEL_SEGMENTATION = 5,        // 语义分割模型
        MODEL_ANOMALY_DETECTION = 6    // 异常检测模型
    }ModelType;


    // 配置参数结构体
    typedef struct 
    {
        const char* model_path;        // 模型文件路径
        int batch_size;                // batch size
        DeviceType device_type;        // 推理设备类型
        ModelType model_type;          // 模型类型

        float confidence_threshold;
        float IoU_threshold;
        float segmentation_threshold;
        float keypoint_threshold;

    } ModelConfig;


    // 点
    typedef struct
    {
        float x;
        float y;
        float confidence;
    } Point;


    // 关键点骨架
    typedef struct
    {
        int startKptId;
        int endKptId;
    } Skeleton;


    // 关键点
    struct Kpts
    {
        Point* point = nullptr;        // 关键点数组
        int pointNum = 0;              // 关键点数量
        Skeleton* skeleton = nullptr;  // 关键点骨架
        int connectionNum = 0;         // 骨架连接数
        float thresh = 0;

        Kpts() = default;

        Kpts(const Point* src, int pointNum, Skeleton* skeleton, int connectionNum, float thresh)
            : pointNum(pointNum), connectionNum(connectionNum), thresh(thresh)
        {
            if (pointNum > 0 && src != nullptr)
            {
                point = (Point*)malloc(pointNum * sizeof(Point));
                memcpy(point, src, pointNum * sizeof(Point));
            }

            if (connectionNum > 0 && skeleton != nullptr)
            {
                this->skeleton = (Skeleton*)malloc(connectionNum * sizeof(Skeleton));
                memcpy(this->skeleton, skeleton, connectionNum * sizeof(Skeleton));
            }
        }

        // 深拷贝构造
        Kpts(const Kpts& other) : Kpts(other.point, other.pointNum, other.skeleton, other.connectionNum, other.thresh) {}

        // 移动构造
        Kpts(Kpts&& other) noexcept : point(other.point), pointNum(other.pointNum), skeleton(other.skeleton), 
                                      connectionNum(other.connectionNum), thresh(other.thresh)
        {
            other.point = nullptr;
            other.pointNum = 0;
            other.skeleton = nullptr;
            other.connectionNum = 0;
            other.thresh = 0;
        }

        // 拷贝赋值
        Kpts& operator=(const Kpts& other)
        {
            if (this != &other)
            {
                this->~Kpts();
                pointNum = other.pointNum;
                if (pointNum > 0 && other.point != nullptr)
                {
                    point = (Point*)malloc(pointNum * sizeof(Point));
                    memcpy(point, other.point, pointNum * sizeof(Point));
                }
                connectionNum = other.connectionNum;
                if (connectionNum > 0 && other.skeleton != nullptr)
                {
                    skeleton = (Skeleton*)malloc(connectionNum * sizeof(Skeleton));
                    memcpy(skeleton, other.skeleton, connectionNum * sizeof(Skeleton));
                }
                thresh = other.thresh;
            }
            return *this;
        }

        // 移动赋值
        Kpts& operator=(Kpts&& other) noexcept
        {
            if (this != &other)
            {
                this->~Kpts();
                point = other.point;
                pointNum = other.pointNum;
                skeleton = other.skeleton;
                connectionNum = other.connectionNum;
                thresh = other.thresh;

                other.point = nullptr;
                other.pointNum = 0;
                other.skeleton = nullptr;
                other.connectionNum = 0;
                other.thresh = 0;
            }
            return *this;
        }


        ~Kpts()
        {
            if (this->point != nullptr)
            {
                free(this->point);
                point = nullptr;
            }
            if (this->skeleton != nullptr)
            {
                free(this->skeleton);
                skeleton = nullptr;
            }
        }
    };

    
    // 分割结果
    struct Seg
    {
        int width = 0;
        int height = 0;
        float affine_matrix[6]{};
        float thresh = 0;
        int* intData = nullptr;
        float* floatData = nullptr;        // 分割结果数组
        bool isIntData = false;            // 标记当前数据类型

        Seg() = default;

        
        Seg(int width, int height, const float* affine_matrix, float thresh, bool isIntData)
            : width(width), height(height), thresh(thresh), isIntData(isIntData)
        {
            memcpy(this->affine_matrix, affine_matrix, sizeof(this->affine_matrix));
            size_t size = static_cast<size_t>(width) * height;
            if (size > 0)
            {
                if (this->isIntData)
                {
                    intData = static_cast<int*>(malloc(size * sizeof(int)));
                    memset(intData, 0, size * sizeof(int));
                }
                else
                {
                    floatData = static_cast<float*>(malloc(size * sizeof(float)));
                    memset(floatData, 0, size * sizeof(float));
                } 
            }
        }


        Seg(const float* src, int width, int height, const float* affine_matrix, float thresh)
            : width(width), height(height), thresh(thresh), isIntData(false)
        {
            memcpy(this->affine_matrix, affine_matrix, sizeof(this->affine_matrix));
            size_t size = static_cast<size_t>(width) * height;
            if (size > 0 && src != nullptr) 
            {
                floatData = static_cast<float*>(malloc(size * sizeof(float)));
                memcpy(floatData, src, size * sizeof(float));
            }
        }

        Seg(const int* src, int width, int height, const float* affine_matrix, float thresh)
            : width(width), height(height), thresh(thresh), isIntData(true)
        {
            memcpy(this->affine_matrix, affine_matrix, sizeof(this->affine_matrix));
            size_t size = static_cast<size_t>(width) * height;
            if (size > 0 && src != nullptr) 
            {
                intData = static_cast<int*>(malloc(size * sizeof(int)));
                memcpy(intData, src, size * sizeof(int));
            }
        }

        // 深拷贝构造
        Seg(const Seg& other): width(other.width), height(other.height), thresh(other.thresh), isIntData(other.isIntData)
        {
            memcpy(this->affine_matrix, other.affine_matrix, 6 * sizeof(float));
            size_t size = (size_t)width * height;

            if (size > 0 && other.intData != nullptr)
            {
                intData = static_cast<int*>(malloc(size * sizeof(int)));
                memcpy(intData, other.intData, size * sizeof(int));
            }

            if (size > 0 && other.floatData != nullptr)
            {
                floatData = static_cast<float*>(malloc(size * sizeof(float)));
                memcpy(floatData, other.floatData, size * sizeof(float));
            }
        }

        // 移动构造
        Seg(Seg&& other) noexcept : width(other.width), height(other.height), thresh(other.thresh), 
            intData(other.intData), floatData(other.floatData), isIntData(other.isIntData)
        {
            memcpy(this->affine_matrix, other.affine_matrix, sizeof(this->affine_matrix));
            other.intData = nullptr;
            other.floatData = nullptr;
            other.width = 0;
            other.height = 0;
        }

        // 拷贝赋值
        Seg& operator=(const Seg& other)
        {
            if (this != &other)
            {
                this->~Seg();
                width = other.width;
                height = other.height;
                thresh = other.thresh;
                isIntData = other.isIntData;
                memcpy(this->affine_matrix, other.affine_matrix, sizeof(this->affine_matrix));
                size_t size = (size_t)width * height;

                if (size > 0 && other.intData != nullptr)
                {
                    intData = static_cast<int*>(malloc(size * sizeof(int)));
                    memcpy(intData, other.intData, size * sizeof(int));
                }

                if (size > 0 && other.floatData != nullptr)
                {
                    floatData = static_cast<float*>(malloc(size * sizeof(float)));
                    memcpy(floatData, other.floatData, size * sizeof(float));
                }
            }
            return *this;
        }

        // 移动赋值
        Seg& operator=(Seg&& other) noexcept
        {
            if (this != &other)
            {
                this->~Seg();
                width = other.width;
                height = other.height;
                thresh = other.thresh;
                isIntData = other.isIntData;
                memcpy(this->affine_matrix, other.affine_matrix, sizeof(this->affine_matrix));
                intData = other.intData;
                floatData = other.floatData;

                other.intData = nullptr;
                other.floatData = nullptr;
                other.width = 0;
                other.height = 0;
            }
            return *this;
        }

        ~Seg()
        {
            if (this->intData != nullptr)
            {
                free(this->intData);
                intData = nullptr;
            }

            if (this->floatData != nullptr)
            {
                free(this->floatData);
                floatData = nullptr;
            }
        }
    };
    
    
    // 检测结果
    struct Result
    {
        // 中心点坐标、宽高、旋转角度
        float cx = -1;
        float cy = -1;
        float width = -1;
        float height = -1;
        float angle = -1;
        float confidence = -1;
        int class_id = -1;
        const char* class_name = nullptr;
        Kpts keypoints;
        Seg segmentation;

        Result() = default;

        ~Result() = default;
    };


    // 创建模型实例
    DEEP_LEARNING_SDK_API ModelHandle CreateModel(const ModelConfig* config);

    // 销毁模型实例
    DEEP_LEARNING_SDK_API int DestroyModel(ModelHandle handle);

    // 模型初始化
    DEEP_LEARNING_SDK_API int InitRuntime(ModelHandle handle, const ModelConfig* config);

    // 模型推理
    DEEP_LEARNING_SDK_API int Pipeline(ModelHandle handle, 
                                       void* imageData, int im_w, int im_h, int im_c, int im_type, 
                                       void* depthData, int d_w, int d_h, int d_c, int d_type,
                                       Result** objInfo, int& objectNum);

    // 清除推理结果
    DEEP_LEARNING_SDK_API int CleanUpResult(ModelHandle handle, Result*& objInfo);

}










