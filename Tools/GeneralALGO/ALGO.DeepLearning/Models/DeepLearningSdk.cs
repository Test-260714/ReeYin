using HalconDotNet;
using OpenCvSharp;
using ReeYin_V.Core.Enums;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ALGO.DeepLearning.Models
{
    using static ALGO.DeepLearning.Models.DeepLearningSdk;
    using DeepLearningHandle = System.IntPtr;

    public class DeepLearningSdk : IDisposable
    {
        #region 原生类型定义
        public enum DeviceType
        {
            DEVICE_CPU = 0,
            DEVICE_GPU = 1
        }

        public enum ModelType
        {
            MODEL_CLASSIFICATION = 0,
            MODEL_DETECTION_BBOX = 1,
            MODEL_DETECTION_SEG = 2,
            MODEL_DETECTION_OBB = 3,
            MODEL_DETECTION_KPT = 4,
            MODEL_SEGMENTATION = 5,
            MODEL_ANOMALY_DETECTION = 6
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct NativeModelConfig
        {
            [MarshalAs(UnmanagedType.LPStr)]
            public string ModelPath;
            public int BatchSize;
            public DeviceType DeviceType;
            public ModelType ModelType;

            public float ConfidenceThreshold;
            public float IoUThreshold;
            public float SegmentationThreshold;
            public float KeypointThreshold;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NativePoint
        {
            public float X;
            public float Y;
            public float Confidence;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NativeSkeleton
        {
            public int StartKptId;
            public int EndKptId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NativeKpts
        {
            public IntPtr Point;      // 指向 NativePoint 数组
            public int PointNum;
            public IntPtr Skeleton;
            public int ConnectionNum;
            public float Thresh;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NativeSeg
        {
            public int Width;
            public int Height;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public float[] AffineMatrix;

            public float Thresh;
            public IntPtr IntData;   // 指向 int 数组
            public IntPtr FloatData;

            [MarshalAs(UnmanagedType.I1)]
            public bool IsIntData;   // 标记当前数据类型
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct NativeResult
        {
            public float Cx, Cy, Width, Height, Angle;
            public float Confidence;
            public int ClassId;
            public IntPtr ClassName; // 原生类别名称指针
            public NativeKpts Keypoints;
            public NativeSeg Segmentation;
        }


        public class ModelConfig
        {
            public string ModelPath = string.Empty;
            public int BatchSize = 1;
            public eDeepLearningDeviceType DeviceType = eDeepLearningDeviceType.CPU;
            public eDeepLearningModelType ModelType = eDeepLearningModelType.分类模型;

            public double ConfidenceThreshold = 0.5;
            public double IoUThreshold = 0.5;
            public double SegmentationThreshold = 0.5;
            public double KeypointThreshold = 0.5;
        }
        #endregion

        #region 原生接口声明


       


        internal class DeepLearningNative
        {
            [DllImport("ALGO.DeepLearningNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern DeepLearningHandle CreateModel(ref NativeModelConfig config);

            [DllImport("ALGO.DeepLearningNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int DestroyModel(DeepLearningHandle handle);

            [DllImport("ALGO.DeepLearningNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int InitRuntime(DeepLearningHandle handle, ref NativeModelConfig config);

            [DllImport("ALGO.DeepLearningNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public extern static int Pipeline(DeepLearningHandle handle,
                                              IntPtr inImageData, int inIw, int inIh, int inIc, int inItype,
                                              IntPtr inDepthData, int inDw, int inDh, int inDc, int inDtype,
                                              out IntPtr objInfo, out int objectNum);

            [DllImport("ALGO.DeepLearningNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int CleanUpResult(DeepLearningHandle handle, ref IntPtr objInfo);
        }
        #endregion

        #region 字段与构造


        private DeepLearningHandle _deepLearningHandle;
        private NativeModelConfig _config;
        private bool _disposed;


        public DeepLearningSdk(ModelConfig config)
        {
            _disposed = false;

            LoadModelConfig(config);
            _deepLearningHandle = DeepLearningNative.CreateModel(ref _config);
        }
        #endregion

        #region 资源释放


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (_deepLearningHandle != IntPtr.Zero)
                {
                    DeepLearningNative.DestroyModel(_deepLearningHandle);
                    _deepLearningHandle = IntPtr.Zero;
                }

                _disposed = true;
            }
        }


        ~DeepLearningSdk()
        {
            Dispose(false);
        }
        #endregion

        #region 图像类型转换


        private static int GetCvTypeFromHalconType(string halconType)
        {
            int cvDepth = 0;
            // 建立 HALCON 图像类型到 OpenCV 深度类型的映射。
            switch (halconType)
            {
                case "byte": // 8位无符号整数
                    cvDepth = 0; // CV_8U
                    break;
                case "int1": // 8位有符号整数
                    cvDepth = 1; // CV_8S
                    break;
                case "uint2": // 16位无符号整数
                    cvDepth = 2; // CV_16U
                    break;
                case "int2": // 16位有符号整数
                    cvDepth = 3; // CV_16S
                    break;
                case "int4": // 32位有符号整数
                    cvDepth = 4; // CV_32S
                    break;
                case "real": // 32位浮点数
                    cvDepth = 5; // CV_32F
                    break;
                case "long": // 64位整数
                    cvDepth = 6; // 使用最接近的 64 位浮点类型
                    break;
                case "complex": // 复数类型，实部和虚部均为 32 位浮点数
                    // 复数图像需要额外拆分处理，当前推理链路暂不支持。
                    throw new NotSupportedException("Complex image type is not supported");
                default:
                    throw new NotSupportedException($"Unsupported image type: {halconType}");
            }
            return cvDepth;
        }


        private void LoadModelConfig(ModelConfig config)
        {
            _config.ModelPath = config.ModelPath;
            _config.BatchSize = config.BatchSize;
            _config.DeviceType = (DeviceType)config.DeviceType;
            _config.ModelType = (ModelType)config.ModelType;
            _config.ConfidenceThreshold = (float)config.ConfidenceThreshold;
            _config.IoUThreshold = (float)config.IoUThreshold;
            _config.SegmentationThreshold = (float)config.SegmentationThreshold;
            _config.KeypointThreshold = (float)config.KeypointThreshold;
        }
        #endregion

        #region 模型生命周期


        /// <summary>
        /// 模型接口初始化
        /// </summary>
        /// <param name="config">模型配置参数</param>
        public int InitRuntime(ModelConfig config)
        {
            LoadModelConfig(config);

            int ret = DeepLearningNative.InitRuntime(_deepLearningHandle, ref _config);

            return ret;
        }


        public static bool IsValidImage(HImage img)
        {
            HObject empty = null;
            try
            {
                // 判断图像对象是否已经初始化。
                if (img == null || !img.IsInitialized())
                    return false;

                // 构造空对象用于判断输入是否为空。
                HOperatorSet.GenEmptyObj(out empty);

                HTuple isEmpty;
                HOperatorSet.TestEqualObj(img, empty, out isEmpty);
                if (isEmpty == 1)
                    return false;

                // 判断对象数量是否大于 0。
                HTuple count;
                HOperatorSet.CountObj(img, out count);
                return count > 0;
            }
            catch
            {
                return false;
            }
            finally
            {
                empty?.Dispose();
            }
        }
        #endregion

        #region 推理流程

        /// <summary>
        /// 模型接口运行
        /// </summary>
        /// <param name="hoImage">输入图像。</param>
        /// <param name="hoDepthMap">输入深度图，可为空。</param>
        /// <param name="result">模型输出结果。</param>
        public int Pipeline(HImage hoImage, HImage hoDepthMap, out List<ReeYin_V.Core.DeepLearning.Result> result)
        {
            result = new List<ReeYin_V.Core.DeepLearning.Result>();

            if (hoImage == null || !IsValidImage(hoImage))
            {
                return -1001;
            }

            // 获取输入图像的基础信息。
            string type;
            int cvDepth;

            IntPtr inImageData = IntPtr.Zero;
            IntPtr inDepthData = IntPtr.Zero;

            Mat dstImage = null;
            IntPtr objInfo = IntPtr.Zero;
            int ret = 0;

            try
            {
                // 获取灰度图或彩色图的图像信息。
                int inIw = 0, inIh = 0;
                int inIc = 0;
                // 解析 OpenCV 图像类型。
                int inIType = 0;

                int restoreWidth = 0, restoreHeight = 0;

                if (hoImage != null && IsValidImage(hoImage))
                {
                    inIc = hoImage.CountChannels();
                    if (inIc == 1)
                    {
                        inImageData = hoImage.GetImagePointer1(out type, out inIw, out inIh);
                        cvDepth = GetCvTypeFromHalconType(type);
                    }
                    else if (inIc == 3)
                    {
                        IntPtr ptrRed = IntPtr.Zero;
                        IntPtr ptrGreen = IntPtr.Zero;
                        IntPtr ptrBlue = IntPtr.Zero;

                        hoImage.GetImagePointer3(out ptrRed, out ptrGreen, out ptrBlue, out type, out inIw, out inIh);

                        cvDepth = GetCvTypeFromHalconType(type);

                        // 分别构造红、绿、蓝三个单通道图像。
                        Mat matRed = new Mat();
                        Mat matGreen = new Mat();
                        Mat matBlue = new Mat();

                        matRed = Mat.FromPixelData(inIh, inIw, MatType.MakeType(cvDepth, 1), ptrRed);
                        matGreen = Mat.FromPixelData(inIh, inIw, MatType.MakeType(cvDepth, 1), ptrGreen);
                        matBlue = Mat.FromPixelData(inIh, inIw, MatType.MakeType(cvDepth, 1), ptrBlue);

                        // 合并为 OpenCV 可识别的三通道图像。
                        dstImage = new Mat();
                        Mat[] multi = new Mat[] { matRed, matGreen, matBlue };
                        Cv2.Merge(multi, dstImage);

                        inImageData = dstImage.Data;

                        // 释放单通道临时图像。
                        matBlue.Dispose();
                        matGreen.Dispose();
                        matRed.Dispose();
                    }
                    else
                    {
                        throw new Exception("不支持的图像通道数");
                    }

                    // 按 OpenCV 规则计算图像类型编码。
                    inIType = cvDepth + ((inIc - 1) << 3);

                    restoreWidth = inIw;
                    restoreHeight = inIh;
                }

                // 获取深度图的图像信息。
                int inDw = 0, inDh = 0;
                int inDc = 0;
                int inDType = 0;

                if (hoDepthMap != null && IsValidImage(hoDepthMap))
                {
                    inDc = hoDepthMap.CountChannels();
                    if (inDc == 1)
                    {
                        inDepthData = hoDepthMap.GetImagePointer1(out type, out inDw, out inDh);
                        cvDepth = GetCvTypeFromHalconType(type);
                    }
                    else
                    {
                        throw new Exception("不支持的深度图通道数");
                    }
                    inDType = cvDepth + ((inDc - 1) << 3);

                    restoreWidth = inDw;
                    restoreHeight = inDh;
                }

                int objectNum = 0;

                ret = DeepLearningNative.Pipeline(_deepLearningHandle,
                                                  inImageData, inIw, inIh, inIc, inIType,
                                                  inDepthData, inDw, inDh, inDc, inDType,
                                                  out objInfo, out objectNum);
                if (ret == 0)
                {
                    int NativeResultSize = Marshal.SizeOf<NativeResult>();
                    for (int i = 0; i < objectNum; i++)
                    {
                        IntPtr currentResultPtr = IntPtr.Add(objInfo, i * NativeResultSize);
                        NativeResult nativeResult = Marshal.PtrToStructure<NativeResult>(currentResultPtr);

                        ReeYin_V.Core.DeepLearning.Result r = new ReeYin_V.Core.DeepLearning.Result();

                        r.Cx = nativeResult.Cx;
                        r.Cy = nativeResult.Cy;
                        r.Width = nativeResult.Width;
                        r.Height = nativeResult.Height;
                        r.Angle = nativeResult.Angle;
                        r.Confidence = nativeResult.Confidence;
                        r.ClassId = nativeResult.ClassId;

                        r.ClassName = Marshal.PtrToStringUTF8(nativeResult.ClassName) ?? string.Empty;

                        r.ModelType = (eDeepLearningModelType)_config.ModelType;

                        // 解析关键点结果。
                        r.Kpt = new ReeYin_V.Core.DeepLearning.Keypoints();
                        r.Kpt.Thresh = nativeResult.Keypoints.Thresh;
                        if (nativeResult.Keypoints.PointNum > 0 && nativeResult.Keypoints.Point != IntPtr.Zero)
                        {
                            int pointNum = nativeResult.Keypoints.PointNum;
                            int pointSize = Marshal.SizeOf<NativePoint>();
                            for (int idx = 0; idx < pointNum; idx++)
                            {
                                IntPtr pj = IntPtr.Add(nativeResult.Keypoints.Point, idx * pointSize);
                                NativePoint nativePoint = Marshal.PtrToStructure<NativePoint>(pj);
                                ReeYin_V.Core.DeepLearning.Point point = new ReeYin_V.Core.DeepLearning.Point();
                                point.X = nativePoint.X;
                                point.Y = nativePoint.Y;
                                point.Confidence = nativePoint.Confidence;
                                r.Kpt.Points.Add(point);
                            }

                            // 解析骨架连接结果。
                            if (nativeResult.Keypoints.ConnectionNum > 0 && nativeResult.Keypoints.Point != IntPtr.Zero)
                            {
                                int connectionNum = nativeResult.Keypoints.ConnectionNum;
                                int skeletonSize = Marshal.SizeOf<NativeSkeleton>();
                                for (int idx = 0; idx < connectionNum; idx++)
                                {
                                    IntPtr pj = IntPtr.Add(nativeResult.Keypoints.Skeleton, idx * skeletonSize);
                                    NativeSkeleton nativeSkeleton = Marshal.PtrToStructure<NativeSkeleton>(pj);
                                    ReeYin_V.Core.DeepLearning.Skeleton skeleton = new ReeYin_V.Core.DeepLearning.Skeleton();
                                    skeleton.StartKptId = nativeSkeleton.StartKptId;
                                    skeleton.EndKptId = nativeSkeleton.EndKptId;
                                    r.Kpt.Skeletons.Add(skeleton);
                                }
                            }
                        }

                        // 解析分割结果。
                        if (nativeResult.Segmentation.Width > 0 && nativeResult.Segmentation.Height > 0)
                        {
                            HTuple hvaffineMatrix = new HTuple(nativeResult.Segmentation.AffineMatrix);
                            // 修正 HALCON 仿射变换与 OpenCV 的坐标差异。
                            hvaffineMatrix[0] = nativeResult.Segmentation.AffineMatrix[4];
                            hvaffineMatrix[1] = nativeResult.Segmentation.AffineMatrix[3];
                            hvaffineMatrix[2] = nativeResult.Segmentation.AffineMatrix[5];
                            hvaffineMatrix[3] = nativeResult.Segmentation.AffineMatrix[1];
                            hvaffineMatrix[4] = nativeResult.Segmentation.AffineMatrix[0];
                            hvaffineMatrix[5] = nativeResult.Segmentation.AffineMatrix[2];

                            HObject hoMaskImage = null;
                            if (nativeResult.Segmentation.IsIntData)
                            {
                                HObject rawSeg = null;
                                HObject transformedSeg = null;
                                try
                                {
                                    HOperatorSet.GenImage1(out hoMaskImage, "int4", nativeResult.Segmentation.Width, nativeResult.Segmentation.Height,
                                                           nativeResult.Segmentation.IntData);

                                    HOperatorSet.Threshold(hoMaskImage, out rawSeg, nativeResult.Segmentation.Thresh - 1, 256);
                                    HOperatorSet.AffineTransRegion(rawSeg, out transformedSeg, hvaffineMatrix, "constant");
                                    r.Seg = transformedSeg;
                                    transformedSeg = null;
                                }
                                finally
                                {
                                    transformedSeg?.Dispose();
                                    rawSeg?.Dispose();
                                    hoMaskImage?.Dispose();
                                }
                            }
                            else
                            {
                                HObject transformedMaskImage = null;
                                try
                                {
                                    HOperatorSet.GenImage1(out hoMaskImage, "real", nativeResult.Segmentation.Width, nativeResult.Segmentation.Height,
                                                           nativeResult.Segmentation.FloatData);

                                    if (restoreWidth != 0 && restoreHeight != 0)
                                    {
                                        HOperatorSet.AffineTransImageSize(hoMaskImage, out transformedMaskImage, hvaffineMatrix, "bilinear", restoreWidth, restoreHeight);
                                    }
                                    else
                                    {
                                        HOperatorSet.AffineTransImage(hoMaskImage, out transformedMaskImage, hvaffineMatrix, "bilinear", "true");
                                    }

                                    HOperatorSet.Threshold(transformedMaskImage, out r.Seg, nativeResult.Segmentation.Thresh, 256);
                                }
                                finally
                                {
                                    transformedMaskImage?.Dispose();
                                    hoMaskImage?.Dispose();
                                }
                            }

                        }

                        result.Add(r);
                    }
                }
            }
            finally
            {
                if (objInfo != IntPtr.Zero)
                {
                    try
                    {
                        DeepLearningNative.CleanUpResult(_deepLearningHandle, ref objInfo);
                    }
                    catch
                    {
                        // 清理原生结果失败不能覆盖推理原始错误码。
                    }
                }

                dstImage?.Dispose();  // 确保推理完成后释放临时图像。
            }

            return ret;
            
        }
        #endregion



    }
}
