#pragma once

#ifdef _WIN32
#define CAMERA_CALIBRATION_SDK_API __declspec(dllexport)
#else
#define CAMERA_CALIBRATION_SDK_API
#endif


#define CAMERA_CALIBRATION_SDK_C_API extern "C" CAMERA_CALIBRATION_SDK_API

// 标定板类型枚举
typedef enum CalibrationBoardType 
{
    BOARD_UNKNOWN = -1, 
    PIXEL_RATIO = 0,
    CHESSBOARD = 1,           // OpenCV棋盘格
    CHARUCO,                  // Charuco标定板
    CIRCLES_GRID,             // OpenCV圆点标定板
    ASYMMETRIC_CIRCLES_GRID   // OpenCV非对称圆点标定板
} CalibrationBoardType;


// 标定板参数基类
struct CalibrationBoardParams
{
    // 必填参数
    CalibrationBoardType type = BOARD_UNKNOWN;
    int width = -1;
    int height = -1;

    // 可选参数（按类型解释）
    double squareSize = -1;  // Chessboard / Charuco 才用
    int dictionaryId = -1;  // Charuco / ArUco 才用
    double markerSize = -1;  // Charuco 才用
    double squareSizePixel = -1;  // Charuco 棋盘格在图像中的预计像素边长；<=0 时不改检测器默认参数
    double markerSizePixel = -1;  // Charuco Marker 在图像中的预计像素边长；<=0 时不改检测器默认参数
    
    // 基于像素比的标定
    double distanceReal = -1;
    double distancePixel = -1;

};

struct MeasurementPlaneParams
{
    double heightCompensation = 0.0;
};

typedef enum MultiCameraAnchorMode
{
    MULTICAM_ANCHOR_BOARD_POSE = 0,
    MULTICAM_ANCHOR_CAMERA = 1,
    MULTICAM_ANCHOR_EXTERNAL = 2
} MultiCameraAnchorMode;

typedef enum MultiCameraBlendMode
{
    MULTICAM_BLEND_OVERLAY = 0,
    MULTICAM_BLEND_AVERAGE = 1
} MultiCameraBlendMode;


struct CameraParams
{ 
    // 标定误差
    double error;

    // X方向像素当量
    double intervalX;

    // Y方向像素当量
    double intervalY;

    // 相机ID
    char cameraId[256];

    // 内参矩阵 (3x3)
    double intrinsic[9];

    // 畸变参数
    double distortion[8]; // 最多支持8个畸变参数

    // 旋转向量 (3x1)
    double rvec[3];

    // 平移向量 (3x1)
    double tvec[3];

    // 外参矩阵 (4x4)
    double extrinsic[16];

    // 单应性矩阵 (3x3)
    double homographyMatrix[9];

    // 矩阵的有效标识
    int hasError = 0;
    int hasIntervalX = 0;
    int hasIntervalY = 0;
    int hasIntrinsic = 0;
    int hasDistortion = 0;
    int hasRvec = 0;
    int hasTvec = 0;
    int hasExtrinsic = 0;
    int hasHomographyMatrix = 0;
};



// 多相机联合标定选项
struct MultiCameraCalibrationOptions
{
    int anchorMode;                         // 锚点模式 (0=标定板位姿, 1=参考相机, 2=外部给定)
    char referenceCameraId[256];            // 参考相机标识
    char referenceCaptureId[256];           // 参考捕获帧标识
    int refineIntrinsics;                   // 是否优化内参
    int refineDistortion;                   // 是否优化畸变系数
    int minCornersPerObservation;           // 每次观测最少角点数
    double maxReprojectionErrorForInit;     // 初始化阶段最大重投影误差阈值
    double robustLossScale;                 // 鲁棒损失函数尺度参数
    int maxIterations;                      // 最大迭代次数
};

// 多相机标定结果中的单相机参数
struct MultiCameraCameraParams
{
    char cameraId[256];                     // 相机标识
    int imageWidth;                         // 图像宽度
    int imageHeight;                        // 图像高度
    double rmsError;                        // 重投影均方根误差
    double intrinsic[9];                    // 内参矩阵 (3x3)
    double distortion[8];                   // 畸变系数
    double rvecCommonFromCamera[3];         // 相机坐标系到公共坐标系的旋转向量
    double tvecCommonFromCamera[3];         // 相机坐标系到公共坐标系的平移向量
    double extrinsicCommonFromCamera[16];   // 相机坐标系到公共坐标系的外参矩阵 (4x4)
    int hasIntrinsic;                       // 内参是否有效
    int hasDistortion;                      // 畸变系数是否有效
    int hasExtrinsicCommonFromCamera;       // 外参是否有效
};

// 多相机标定报告
struct MultiCameraCalibrationReport
{
    int cameraCount;                // 相机数量
    int captureCount;               // 捕获帧数
    int observationCount;           // 观测点数量
    int residualCount;              // 残差数量
    double initialRmsError;         // 初始均方根误差
    double finalRmsError;           // 最终均方根误差
    double maxReprojectionError;    // 最大重投影误差
    int connectedComponentCount;    // 连通分量数量
    int converged;                  // 是否收敛
    int ceresTerminationType;       // Ceres求解器终止类型
};

// 多相机图像输入
struct MultiCameraImageInput
{
    char cameraId[256];     // 相机标识
    void* imageData;        // 图像数据指针
    int width;              // 图像宽度
    int height;             // 图像高度
    int channels;           // 图像通道数
    int cvType;             // OpenCV图像类型
};

// 多相机图像拼接输出
struct MultiCameraImageOutput
{
    void* imageData;        // 输出图像数据指针
    int width;              // 输出图像宽度
    int height;             // 输出图像高度
    int channels;           // 输出图像通道数
    int cvType;             // OpenCV图像类型
};

// 单相机标定句柄
typedef struct CameraCalibrationFrameworkImpl* CameraCalibrationFrameworkHandle;

// 创建标定框架实例
CAMERA_CALIBRATION_SDK_C_API CameraCalibrationFrameworkHandle createCalibrationFramework();

// 销毁标定框架实例
CAMERA_CALIBRATION_SDK_C_API void destroyCalibrationFramework(CameraCalibrationFrameworkHandle handle);

// 添加相机
CAMERA_CALIBRATION_SDK_C_API int addCamera(CameraCalibrationFrameworkHandle handle, const char* cameraId);

// 设置标定板参数
CAMERA_CALIBRATION_SDK_C_API int setCalibrationBoardParams(CameraCalibrationFrameworkHandle handle, const CalibrationBoardParams* params);

// 获取标定板参数
CAMERA_CALIBRATION_SDK_C_API int getCalibrationBoardParams(CameraCalibrationFrameworkHandle handle, CalibrationBoardParams* params);

CAMERA_CALIBRATION_SDK_C_API int setMeasurementPlaneParams(CameraCalibrationFrameworkHandle handle, const MeasurementPlaneParams* params);

CAMERA_CALIBRATION_SDK_C_API int getMeasurementPlaneParams(CameraCalibrationFrameworkHandle handle, MeasurementPlaneParams* params);

// 获取相机内参外参
CAMERA_CALIBRATION_SDK_C_API int getCameraParams(CameraCalibrationFrameworkHandle handle, CameraParams* params);

// 添加相机标定图片 (传入图片路径)
CAMERA_CALIBRATION_SDK_C_API int addCalibrationImagePath(CameraCalibrationFrameworkHandle handle, const char* cameraId,
                                                         const char* imagePath);

// 执行标定
CAMERA_CALIBRATION_SDK_C_API int calibrate(CameraCalibrationFrameworkHandle handle);

// 保存标定结果到yaml文件
CAMERA_CALIBRATION_SDK_C_API int saveCalibrationResults(CameraCalibrationFrameworkHandle handle, const char* outputPath);

// 加载标定文件
CAMERA_CALIBRATION_SDK_C_API int loadCalibrationFile(CameraCalibrationFrameworkHandle handle, const char* filePath);

// 像素坐标转世界坐标
CAMERA_CALIBRATION_SDK_C_API int pixelToWorld(CameraCalibrationFrameworkHandle handle, const char* cameraId,
    double pixelX, double pixelY, double* worldX, double* worldY, double* worldZ);

CAMERA_CALIBRATION_SDK_C_API int pixelToWorldByHomography(CameraCalibrationFrameworkHandle handle, const char* cameraId,
    double pixelX, double pixelY, double* worldX, double* worldY, double* worldZ);

// 世界坐标转像素坐标
CAMERA_CALIBRATION_SDK_C_API int worldToPixel(CameraCalibrationFrameworkHandle handle, const char* cameraId,
    double worldX, double worldY, double worldZ, double* pixelX, double* pixelY);

// 图像校正
CAMERA_CALIBRATION_SDK_C_API int imageCorrection(CameraCalibrationFrameworkHandle handle, const char* cameraId,
    void* inImageData, int inW, int inH, int inC, int inType,
    void** outImageData, int& outW, int& outH, int& outC, int& outType);

// 释放指针
CAMERA_CALIBRATION_SDK_C_API int freePtr(void* ptr);

// 获取错误信息
CAMERA_CALIBRATION_SDK_C_API const char* getLastError();


// 多相机标定句柄
typedef struct MultiCameraCalibrationFrameworkImpl* MultiCameraCalibrationFrameworkHandle;

// 创建多相机标定框架实例
CAMERA_CALIBRATION_SDK_C_API MultiCameraCalibrationFrameworkHandle createMultiCameraCalibrationFramework();

// 销毁多相机标定框架实例
CAMERA_CALIBRATION_SDK_C_API void destroyMultiCameraCalibrationFramework(MultiCameraCalibrationFrameworkHandle handle);

// 设置标定板参数
CAMERA_CALIBRATION_SDK_C_API int multiSetCalibrationBoardParams(MultiCameraCalibrationFrameworkHandle handle, 
    const CalibrationBoardParams* params);

CAMERA_CALIBRATION_SDK_C_API int multiSetMeasurementPlaneParams(MultiCameraCalibrationFrameworkHandle handle,
    const MeasurementPlaneParams* params);

CAMERA_CALIBRATION_SDK_C_API int multiGetMeasurementPlaneParams(MultiCameraCalibrationFrameworkHandle handle,
    MeasurementPlaneParams* params);

// 设置标定选项
CAMERA_CALIBRATION_SDK_C_API int multiSetCalibrationOptions(MultiCameraCalibrationFrameworkHandle handle, 
    const MultiCameraCalibrationOptions* options);

// 添加相机
CAMERA_CALIBRATION_SDK_C_API int multiAddCamera(MultiCameraCalibrationFrameworkHandle handle, 
    const char* cameraId, int imageWidth, int imageHeight);

// 设置相机初始参数
CAMERA_CALIBRATION_SDK_C_API int multiSetInitialCameraParams(MultiCameraCalibrationFrameworkHandle handle, 
    const char* cameraId, const CameraParams* params, int fixIntrinsic);

// 添加观测图像路径
CAMERA_CALIBRATION_SDK_C_API int multiAddObservationImagePath(MultiCameraCalibrationFrameworkHandle handle,
    const char* cameraId, const char* captureId, const char* imagePath);

// 执行多相机标定
CAMERA_CALIBRATION_SDK_C_API int multiCalibrate(MultiCameraCalibrationFrameworkHandle handle);

// 获取相机标定参数
CAMERA_CALIBRATION_SDK_C_API int multiGetCameraParams(MultiCameraCalibrationFrameworkHandle handle, 
    const char* cameraId, MultiCameraCameraParams* params);

// 获取已加载或已标定的相机数量
CAMERA_CALIBRATION_SDK_C_API int multiGetCameraCount(MultiCameraCalibrationFrameworkHandle handle,
    int* cameraCount);

// 按索引获取已加载或已标定的相机 ID
CAMERA_CALIBRATION_SDK_C_API int multiGetCameraId(MultiCameraCalibrationFrameworkHandle handle,
    int index, char* cameraId, int cameraIdCapacity);

// 获取标定报告
CAMERA_CALIBRATION_SDK_C_API int multiGetCalibrationReport(MultiCameraCalibrationFrameworkHandle handle, 
    MultiCameraCalibrationReport* report);

// 像素坐标转世界坐标
CAMERA_CALIBRATION_SDK_C_API int multiPixelToCommonWorld(MultiCameraCalibrationFrameworkHandle handle, 
    const char* cameraId, double pixelX, double pixelY, double* worldX, double* worldY, double* worldZ);

// 世界坐标转像素坐标
CAMERA_CALIBRATION_SDK_C_API int multiCommonWorldToPixel(MultiCameraCalibrationFrameworkHandle handle, 
    const char* cameraId, double worldX, double worldY, double worldZ, double* pixelX, double* pixelY);

// 多相机图像拼接
CAMERA_CALIBRATION_SDK_C_API int multiStitchImages(MultiCameraCalibrationFrameworkHandle handle, 
    const MultiCameraImageInput* images, int imageCount, MultiCameraBlendMode blendMode, MultiCameraImageOutput* output);

// 保存标定结果到文件
CAMERA_CALIBRATION_SDK_C_API int multiSaveCalibrationResults(MultiCameraCalibrationFrameworkHandle handle, const char* outputFile);

// 从文件加载标定结果
CAMERA_CALIBRATION_SDK_C_API int multiLoadCalibrationFile(MultiCameraCalibrationFrameworkHandle handle, const char* filePath);

// 错误码
#define CAMCALIB_OK            0
#define CAMCALIB_ERR_NULL     -1  // 关键参数为空
#define CAMCALIB_ERR_UNSUP    -2  // 不支持的类型
#define CAMCALIB_ERR_INVALID  -3  // 参数无效
#define CAMCALIB_ERR_UNEXP    -4  // 意外错误
#define CAMCALIB_ERR_IO       -5  // 文件读写错误
#define CAMCALIB_ERR_GRAPH_DISCONNECTED -6  // 相机拓扑图不连通
#define CAMCALIB_ERR_NOT_CALIBRATED     -7  // 尚未执行标定
#define CAMCALIB_ERR_OPTIMIZATION       -8  // 优化求解失败
#define CAMCALIB_ERR_MEMORY             -9  // 内存不足
