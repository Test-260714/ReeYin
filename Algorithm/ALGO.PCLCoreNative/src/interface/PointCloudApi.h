#pragma once

#include <pcl/PointIndices.h>
#include <pcl/point_cloud.h>
#include <pcl/point_types.h>

#include <vector>

#ifndef EXTERNC
#define EXTERNC extern "C"
#endif

#ifndef PCLCORENATIVE_API
#define PCLCORENATIVE_API EXTERNC __declspec(dllexport)
#endif

#ifndef CallingConvention
#define CallingConvention __stdcall
#endif

struct PointCloudNativeHandle;

PCLCORENATIVE_API PointCloudNativeHandle* CallingConvention CreatePointCloud();
PCLCORENATIVE_API PointCloudNativeHandle* CallingConvention loadPcFile(char* path);
PCLCORENATIVE_API int CallingConvention loadPlyFile(char* path, PointCloudNativeHandle* handle);
PCLCORENATIVE_API int CallingConvention loadPcdFile(char* path, PointCloudNativeHandle* handle);
PCLCORENATIVE_API int CallingConvention loadObjFile(char* path, PointCloudNativeHandle* handle);
PCLCORENATIVE_API int CallingConvention loadTxtFile(char* path, PointCloudNativeHandle* handle);
PCLCORENATIVE_API int CallingConvention loadPointCloudFile(char* path, PointCloudNativeHandle* handle);
PCLCORENATIVE_API int CallingConvention loadDepthTiffFile(
    char* path,
    PointCloudNativeHandle* handle,
    double spacingX,
    double spacingY,
    double spacingZ,
    double invalidValue,
    int useInvalidValue);
PCLCORENATIVE_API void CallingConvention savePcdFile(char* path, PointCloudNativeHandle* handle, int binaryMode);
PCLCORENATIVE_API void CallingConvention savePlyFile(char* path, PointCloudNativeHandle* handle, int binaryMode);
PCLCORENATIVE_API void CallingConvention stl2PointCloud(char* path, PointCloudNativeHandle* handle);
PCLCORENATIVE_API void CallingConvention saveObjFile(char* path, PointCloudNativeHandle* handle);
PCLCORENATIVE_API void CallingConvention DeletePointCloud(PointCloudNativeHandle* handle);
PCLCORENATIVE_API int CallingConvention CountPointCloud(PointCloudNativeHandle* handle);
PCLCORENATIVE_API int CallingConvention getPointCloudH(PointCloudNativeHandle* handle);
PCLCORENATIVE_API int CallingConvention getPointCloudW(PointCloudNativeHandle* handle);
PCLCORENATIVE_API void CallingConvention getMinMaxXYZ(PointCloudNativeHandle* handle, double* out_res);
PCLCORENATIVE_API double CallingConvention getX(PointCloudNativeHandle* handle, int index);
PCLCORENATIVE_API double CallingConvention getY(PointCloudNativeHandle* handle, int index);
PCLCORENATIVE_API double CallingConvention getZ(PointCloudNativeHandle* handle, int index);
PCLCORENATIVE_API void CallingConvention setX(PointCloudNativeHandle* handle, int index, double x);
PCLCORENATIVE_API void CallingConvention setY(PointCloudNativeHandle* handle, int index, double y);
PCLCORENATIVE_API void CallingConvention setZ(PointCloudNativeHandle* handle, int index, double z);
PCLCORENATIVE_API void CallingConvention reSize(PointCloudNativeHandle* handle, int size);
PCLCORENATIVE_API void CallingConvention push(PointCloudNativeHandle* handle, double x, double y, double z);
PCLCORENATIVE_API void CallingConvention pop(PointCloudNativeHandle* handle);
PCLCORENATIVE_API void CallingConvention clear(PointCloudNativeHandle* handle);
PCLCORENATIVE_API const float* CallingConvention getPointCloudInterleavedF32Ptr(PointCloudNativeHandle* handle);
PCLCORENATIVE_API int CallingConvention getPointCloudInterleavedStrideBytes();
PCLCORENATIVE_API void CallingConvention copyPointCloudToSplitF64(
    PointCloudNativeHandle* handle,
    double* outX,
    double* outY,
    double* outZ,
    int count);
PCLCORENATIVE_API void CallingConvention setPointCloudFromSplitF64(
    PointCloudNativeHandle* handle,
    const double* inX,
    const double* inY,
    const double* inZ,
    int count);
PCLCORENATIVE_API void CallingConvention setPointCloudFromInterleavedF32(
    PointCloudNativeHandle* handle,
    const float* xyzInterleaved,
    int count,
    int strideBytes);

PCLCORENATIVE_API std::vector<pcl::PointIndices>* CallingConvention CreatePointIndices();
PCLCORENATIVE_API void CallingConvention DeletePointIndices(std::vector<pcl::PointIndices>* in_indice);
PCLCORENATIVE_API int CallingConvention CountPointIndices(std::vector<pcl::PointIndices>* in_indice);
PCLCORENATIVE_API pcl::PointIndices* CallingConvention getPointIndice(std::vector<pcl::PointIndices>* in_indice, int pos);
PCLCORENATIVE_API int CallingConvention getSizeOfIndice(std::vector<pcl::PointIndices>* in_indice, int pos);
