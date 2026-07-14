#pragma once
#include "interface/PointCloudNativeHandle.h"

#ifndef EXTERNC
#define EXTERNC extern "C"
#endif
#ifndef HEAD
#define HEAD EXTERNC __declspec(dllexport)
#endif
#ifndef CallingConvention
#define CallingConvention __stdcall
#endif

HEAD int CallingConvention constrainedIcpRegistration(
    PointCloudNativeHandle* source,
    PointCloudNativeHandle* target,
    double initial_x,
    double initial_y,
    double initial_z,
    double initial_yaw_deg,
    int max_iterations,
    double max_correspondence_distance,
    double transformation_epsilon,
    double fitness_epsilon,
    int optimization_mask,
    double* out_transform,
    double* out_metrics);
