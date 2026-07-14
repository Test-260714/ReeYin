using DryIoc.ImTools;
using HalconDotNet;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;
using OpenCvSharp;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.CustomProject;
using ReeYin_V.Logger;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace Custom.KBTBox
{
    using DeepLearningHandle = System.IntPtr;

    public class KBTDispensing_Algorithm : ICustomAlgo
    {

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

        internal class InstanceSegSDK
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

        private DeepLearningHandle _deepLearningHandle;
        private NativeModelConfig _config;

        private KBTDispensing_MeasureParam _measureParam;

        private HObject? _hoValidMaskL0 = new HObject();
        private HObject? _hoValidMaskL1 = new HObject();

        private HObject? _hoGlueRegions = new HObject();
        private HObject? _hoGlueValidMask = new HObject();
        private HObject? _hoGrayGlueRegions = new HObject();
        private HObject? _hoFrameRegions = new HObject();

        private HTuple _hvHeightImageGlobalMinValue = new HTuple();
        private HTuple _hvHeightImageGlobalMaxValue = new HTuple();

        private List<SideResult> _imageData = new List<SideResult>();

        private KBTDispensing_MeasureResult _measureResult = new KBTDispensing_MeasureResult();

        private bool _disposed = false;

        private static readonly System.Threading.SemaphoreSlim _sdkGate = new(1, 1);


        public KBTDispensing_Algorithm(KBTDispensing_MeasureParam param)
        {
            _disposed = false;
            _measureParam = param;

            try
            {
                HOperatorSet.SetSystem("global_mem_cache", "idle");
                HOperatorSet.SetSystem("temporary_mem_cache", "idle");
                HOperatorSet.SetSystem("image_cache_capacity", 0);

                InitVariable();
                Console.WriteLine("算法路径：" + param.ModelPath);

                LoadModelConfig(param);
                _deepLearningHandle = InstanceSegSDK.CreateModel(ref _config);
                int state = InstanceSegSDK.InitRuntime(_deepLearningHandle, ref _config);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
        }


        public void Dispose()
        {
            if (!_disposed)
            {
                if (_imageData != null)
                {
                    foreach (var data in _imageData)
                    {
                        data?.Dispose();
                    }
                    _imageData.Clear();
                }

                _hoValidMaskL0?.Dispose();
                _hoValidMaskL1?.Dispose();
                _hoGlueRegions?.Dispose();
                _hoGlueValidMask?.Dispose();
                _hoGrayGlueRegions?.Dispose();
                _hoFrameRegions?.Dispose();

                _measureResult?.Dispose();
            }

            _disposed = true;

            GC.SuppressFinalize(this);

        }


        public void DestroyModel()
        {
            int state = InstanceSegSDK.DestroyModel(_deepLearningHandle);
        }


        ~KBTDispensing_Algorithm()
        {
            int state = InstanceSegSDK.DestroyModel(_deepLearningHandle);

            Dispose();
        }

        private void LoadModelConfig(KBTDispensing_MeasureParam config)
        {
            _config.ModelPath = config.ModelPath;
            _config.BatchSize = config.BatchSize;
            _config.DeviceType = (DeviceType)config.DeviceType;
            _config.ModelType = (ModelType)config.ModelType;
            _config.ConfidenceThreshold = (float)config.ConfidenceThreshold;
            _config.IoUThreshold = (float)config.IoUThreshold;
            _config.SegmentationThreshold = (float)config.SegmentationThreshold;
            _config.KeypointThreshold = 0.5f;
        }


        /// <summary>
        /// 初始化
        /// </summary>
        public int InitVariable()
        {
            HOperatorSet.GenEmptyObj(out _hoValidMaskL0);
            HOperatorSet.GenEmptyObj(out _hoValidMaskL1);
            HOperatorSet.GenEmptyObj(out _hoGlueRegions);
            HOperatorSet.GenEmptyObj(out _hoGlueValidMask);
            HOperatorSet.GenEmptyObj(out _hoGrayGlueRegions);
            HOperatorSet.GenEmptyObj(out _hoFrameRegions);

            return 0;
        }

        public enum ImageType
        {
            Gray,    // 灰度图
            Depth,   // 深度图
            RGB,     // 三通道RGB图
            BGR      // 三通道BGR图
        }


        private static int GetCvTypeFromHalconType(string halconType)
        {
            int cvDepth = 0;
            // HALCON类型到OpenCV类型的映射
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
                    cvDepth = 6; // CV_64F (虽然不完全匹配，但这是最接近的)
                    break;
                case "complex": // 复数(实部和虚部均为32位浮点数)
                                // 复数类型需要特殊处理，这里暂时不支持
                    throw new NotSupportedException("Complex image type is not supported");
                default:
                    throw new NotSupportedException($"Unsupported image type: {halconType}");
            }
            return cvDepth;
        }

        /// <summary>
        /// OpenCVSharp Mat转List<float[]>
        /// </summary>
        public List<float[]> ConvertMatToList(Mat mat)
        {
            List<float[]> data = new List<float[]>();

            if (mat.Empty())
                return data;

            if (!mat.IsContinuous())
                mat = mat.Clone();

            int channels = mat.Channels();
            if (channels != 1)
                throw new InvalidOperationException("Only single-channel matrices are supported");

            int rows = mat.Rows;
            int cols = mat.Cols;
            MatType type = mat.Type();

            try
            {
                if (type == MatType.CV_8UC1)
                {
                    ProcessByteMat(mat, rows, cols, data);
                }
                else if (type == MatType.CV_32FC1)
                {
                    ProcessFloatMat(mat, rows, cols, data);
                }
                else if (type == MatType.CV_32SC1)
                {
                    ProcessIntMat(mat, rows, cols, data);
                }
                else if (type == MatType.CV_16SC1)
                {
                    ProcessShortMat(mat, rows, cols, data);
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported matrix type: {type}");
                }
            }
            finally
            {
                if (!mat.IsContinuous())
                    if (mat.Data != IntPtr.Zero)
                        mat.Dispose();
            }

            return data;
        }


        private static void ProcessByteMat(Mat mat, int rows, int cols, List<float[]> data)
        {
            byte[] buffer = new byte[rows * cols];
            Marshal.Copy(mat.Data, buffer, 0, buffer.Length);

            for (int i = 0; i < rows; i++)
            {
                float[] row = new float[cols];
                int offset = i * cols;
                for (int j = 0; j < cols; j++)
                {
                    row[j] = buffer[offset + j];
                }
                data.Add(row);
            }
        }

        private static void ProcessFloatMat(Mat mat, int rows, int cols, List<float[]> data)
        {
            float[] buffer = new float[rows * cols];
            Marshal.Copy(mat.Data, buffer, 0, buffer.Length);

            for (int i = 0; i < rows; i++)
            {
                float[] row = new float[cols];
                Array.Copy(buffer, i * cols, row, 0, cols);
                data.Add(row);
            }
        }

        private static void ProcessIntMat(Mat mat, int rows, int cols, List<float[]> data)
        {
            int[] buffer = new int[rows * cols];
            Marshal.Copy(mat.Data, buffer, 0, buffer.Length);

            for (int i = 0; i < rows; i++)
            {
                float[] row = new float[cols];
                for (int j = 0; j < cols; j++)
                {
                    row[j] = buffer[i * cols + j];  // 转为 float 存入结果
                }
                data.Add(row);
            }
        }

        private static void ProcessShortMat(Mat mat, int rows, int cols, List<float[]> data)
        {
            short[] buffer = new short[rows * cols];
            Marshal.Copy(mat.Data, buffer, 0, buffer.Length);

            for (int i = 0; i < rows; i++)
            {
                float[] row = new float[cols];
                for (int j = 0; j < cols; j++)
                {
                    row[j] = buffer[i * cols + j];  // 转 float 存储
                }
                data.Add(row);
            }
        }


        /// <summary>
        /// halcon HObject类型图片转OpenCVSharp Mat类型
        /// </summary>
        public Mat HobjectToMat(HObject? hoImage, ImageType imageType)
        {
            if (hoImage is null)
                return new Mat();

            HOperatorSet.CountChannels(hoImage, out HTuple hvChannels);
            if (hvChannels.Length == 0)
                return new Mat();

            var matType = (imageType == ImageType.Gray) ? MatType.CV_8UC1 : MatType.CV_32FC1;

            if (hvChannels[0].I == 1)
            {
                HOperatorSet.GetImagePointer1(hoImage, out HTuple hvPtr, out _, out HTuple hvW, out HTuple hvH);
                using var header = Mat.FromPixelData(hvH, hvW, matType, (IntPtr)hvPtr);
                return header.Clone();
            }
            else if (hvChannels[0].I == 3)
            {
                HOperatorSet.GetImagePointer3(hoImage, out HTuple hvR, out HTuple hvG, out HTuple hvB,
                                              out _, out HTuple hvW, out HTuple hvH);

                using var r = Mat.FromPixelData(hvH, hvW, matType, (IntPtr)hvR);
                using var g = Mat.FromPixelData(hvH, hvW, matType, (IntPtr)hvG);
                using var b = Mat.FromPixelData(hvH, hvW, matType, (IntPtr)hvB);

                var result = new Mat();
                Cv2.Merge(new[] { b, g, r }, result);
                return result;
            }

            return new Mat();
        }


        /// <summary>
        /// 将List<float[]>数组转换为halcon图片对象
        /// </summary>
        /// <param name="data">输入的List<float[]>数组</param>
        /// <param name="imageType">输入图片数据类型</param>
        /// <param name="hoObject">输出的halcon图片对象</param>
        /// <returns>状态标志</returns>
        public int ConvertListToHObject(List<float[]> data, ImageType imageType, out HObject hoObject)
        {
            int height = data.Count;
            if (height == 0)
            {
                HOperatorSet.GenEmptyObj(out hoObject);
                return -1;
            }

            int width = data[0].Length;
            GCHandle handle = default;

            try
            {
                if (imageType == ImageType.Gray)
                {
                    byte[] imageData = data.SelectMany(row => row.Select(value => (byte)Math.Clamp(value, 0, 255))).ToArray();
                    handle = GCHandle.Alloc(imageData, GCHandleType.Pinned);
                    HOperatorSet.GenImage1(out hoObject, "byte", width, height, handle.AddrOfPinnedObject());
                }
                else if (imageType == ImageType.Depth)
                {
                    float[] imageData = data.SelectMany(row => row).ToArray();
                    handle = GCHandle.Alloc(imageData, GCHandleType.Pinned);
                    HOperatorSet.GenImage1(out hoObject, "real", width, height, handle.AddrOfPinnedObject());
                }
                else
                {
                    HOperatorSet.GenEmptyObj(out hoObject);
                    return -1;
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                HOperatorSet.GenEmptyObj(out hoObject);
                return -1;
            }
            finally
            {
                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }
        }


        static void ReplaceHobject(ref HObject? target, HObject next)
        {
            target?.Dispose();
            target = next;
        }


        static void ReplaceHobject(ref HObject? target, ref HObject? source)
        {
            if (!ReferenceEquals(target, source))
                target?.Dispose();

            target = source;
            source = null;
        }


        /// <summary>
        /// 去除深度图异常点
        /// </summary>
        private HObject GetDepthValidMask(HObject? hoHeightImage, out HObject? hoValidMask)
        {
            using (var dh = new HDevDisposeHelper())
            {
                HTuple hvTmp;

                HObject? hoTmp = null;
                HObject? hoIrregularRegion = null;
                HObject? hoReducedImage = null;

                try
                {
                    HOperatorSet.Threshold(hoHeightImage, out hoValidMask, _measureParam.MinDepth, _measureParam.MaxDepth);
                    HOperatorSet.Threshold(hoHeightImage, out hoIrregularRegion, 8888880, 8888880);
                    HOperatorSet.Difference(hoValidMask, hoIrregularRegion, out hoTmp);
                    ReplaceHobject(ref hoValidMask, ref hoTmp);
                    HOperatorSet.MinMaxGray(hoValidMask, hoHeightImage, 0, out _hvHeightImageGlobalMinValue, out _hvHeightImageGlobalMaxValue, out hvTmp);
                    HOperatorSet.ReduceDomain(hoHeightImage, hoValidMask, out hoReducedImage);

                    return hoReducedImage.Clone();
                }
                catch(Exception ex)
                {
                    hoValidMask = new HObject();
                    Logs.LogError(ex.StackTrace.ToString());
                    return null;
                }
                finally
                {
                    hoTmp?.Dispose();
                    hoIrregularRegion?.Dispose();
                    hoReducedImage?.Dispose();
                }
            }
        }


        private SideResult GetGlueWidthAndThickness(HObject? hoGrayImage, HObject? hoHeightL0, HObject? hoHeightL1, HObject? hoHeightImage, SideResult result)
        {
            HObject? hoTmp = null;

            HObject? hoGlueRegionsConnected = null, hoTmpSelectedRegions = null;
            HObject? hoGlueRegion = null, hoGlueSkeleton = null;
            HObject? hoRegionDilation = null, hoRegionErosion = null;
            HObject? hoGlueEdgeRegion = null, hoMaskGlueEdge = null, hoMaskGlueSkeleton = null;
            HObject? hoEdgeSkeleton = null, hoEdgeSkeletons = null;
            HObject? hoEdgeContours = null, hoTmpEdgeSkeleton = null, hoTmpEdgeContours = null;
            HObject? hoEdgeContour1 = null, hoEdgeContour2 = null;
            HObject? hoGlueSkeletonContours = null;
            HObject? hoEdgeWithDistances1 = null, hoEdgeWithDistances2 = null;
            HObject? hoSampleRegion = null, hoGlueSampleRegion = null, hoFrameSampleRegion = null;

            HTuple hvGlueSurfaceXList = new HTuple(), hvGlueSurfaceYList = new HTuple(), hvGlueSurfaceZList = new HTuple();
            HTuple hvFrameSurfaceXList = new HTuple(), hvFrameSurfaceYList = new HTuple(), hvFrameSurfaceZList = new HTuple();

            HTuple hvGlueContourRows = new HTuple(), hvGlueContourCols = new HTuple();
            HTuple hvGlueWidthList = new HTuple(), hvGlueWidthRealList = new HTuple();
            HTuple hvGlueThicknessList = new HTuple(), hvGlueThicknessRealList = new HTuple();

            HTuple hvIndices = new HTuple();
            HTuple hvIndicesView = new HTuple();

            HTuple hvAngle = new HTuple();
            double gluePathTiltWeightedSum = 0.0;
            double gluePathTiltWeightSum = 0.0;

            try
            {
                // 计算胶宽、胶高
                //HOperatorSet.Connection(_hoGlueRegions, out hoGlueRegionsConnected);
                //HOperatorSet.SelectShapeStd(hoGlueRegionsConnected, out hoTmpSelectedRegions, "max_area", 70);
                //HOperatorSet.RegionFeatures(hoTmpSelectedRegions, "inner_radius", out HTuple hvTmpInnerRadius);
                //HOperatorSet.SelectShape(hoGlueRegionsConnected, out hoTmp, "inner_radius", "and", hvTmpInnerRadius*0.25, hvTmpInnerRadius*2);
                //ReplaceHobject(ref hoGlueRegionsConnected, ref hoTmp);
                //HOperatorSet.CountObj(hoGlueRegionsConnected, out HTuple hvGlueRegionNum);

                HOperatorSet.Connection(_hoGrayGlueRegions, out hoGlueRegionsConnected);
                HOperatorSet.SelectShapeStd(hoGlueRegionsConnected, out hoTmpSelectedRegions, "max_area", 70);
                HOperatorSet.RegionFeatures(hoTmpSelectedRegions, "inner_radius", out HTuple hvTmpInnerRadius);
                HOperatorSet.SelectShape(hoGlueRegionsConnected, out hoTmp, "inner_radius", "and", hvTmpInnerRadius * 0.6, hvTmpInnerRadius * 2);
                ReplaceHobject(ref hoGlueRegionsConnected, ref hoTmp);
                HOperatorSet.CountObj(hoGlueRegionsConnected, out HTuple hvGlueRegionNum);

                for (int idx = 1; idx <= hvGlueRegionNum.I; idx++)
                {
                    hoGlueRegion?.Dispose(); hoGlueSkeleton?.Dispose();
                    hoRegionDilation?.Dispose(); hoRegionErosion?.Dispose(); hoGlueEdgeRegion?.Dispose();
                    hoMaskGlueEdge?.Dispose(); hoMaskGlueSkeleton?.Dispose(); hoEdgeSkeleton?.Dispose();
                    hoEdgeSkeletons?.Dispose(); hoEdgeContours?.Dispose(); hoTmpEdgeSkeleton?.Dispose();
                    hoTmpEdgeContours?.Dispose(); hoEdgeContour1?.Dispose(); hoEdgeContour2?.Dispose();
                    hoGlueSkeletonContours?.Dispose(); hoEdgeWithDistances1?.Dispose(); hoEdgeWithDistances2?.Dispose();
                    hoSampleRegion?.Dispose(); hoGlueSampleRegion?.Dispose(); hoFrameSampleRegion?.Dispose();


                    HTuple hvTmpGlueContourRows = new HTuple(), hvTmpGlueContourCols = new HTuple();
                    HTuple hvTmpDistance1 = new HTuple(), hvTmpDistance2 = new HTuple(), hvTmpGlueWidth = new HTuple(), hvTmpGlueWidthReal = new HTuple();
                    HTuple hvTmpGlueThickness = new HTuple(), hvTmpGlueThicknessReal = new HTuple();
                    HTuple hvTmpIndices = new HTuple();
                    HTuple hvTmpIndicesView = new HTuple();
                    HTuple hvTmpAngle = new HTuple();
                    double tmpGluePathTiltAngle = double.NaN;
                    double tmpGluePathTiltWeight = 0.0;

                    HOperatorSet.SelectObj(hoGlueRegionsConnected, out hoGlueRegion, idx);
                    HOperatorSet.ClosingCircle(hoGlueRegion, out hoTmp, 101);
                    ReplaceHobject(ref hoGlueRegion, ref hoTmp);

                    HOperatorSet.RegionFeatures(hoGlueRegion, "inner_radius", out HTuple hvGlueRegionInnerRadius);

                    // 胶面骨架与胶面两侧边缘提取
                    HOperatorSet.Skeleton(hoGlueRegion, out hoGlueSkeleton);
                    HOperatorSet.DilationCircle(hoGlueRegion, out hoRegionDilation, 2);
                    HOperatorSet.ErosionCircle(hoGlueRegion, out hoRegionErosion, 2);
                    HOperatorSet.Difference(hoRegionDilation, hoRegionErosion, out hoGlueEdgeRegion);
                    HOperatorSet.SmallestRectangle2(hoGlueEdgeRegion, out HTuple hvRow, out HTuple hvColumn,
                                                    out HTuple hvPhi, out HTuple hvLength1, out HTuple hvLength2);

                    ////HOperatorSet.GetImageSize(hoGrayImage, out HTuple hvImageWidth, out HTuple hvImageHeight);
                    //if (hvGlueRegionInnerRadius.Length == 1 && hvGlueRegionInnerRadius.D > 0)
                    //{
                    //    HOperatorSet.GenRectangle2(out hoMaskGlueEdge, hvRow, hvColumn - 8, hvPhi, hvLength1, hvLength2 - 8);
                    //    HOperatorSet.GenRectangle2(out hoMaskGlueSkeleton, hvRow, hvColumn - hvGlueRegionInnerRadius * 0.8,
                    //                               hvPhi, hvLength1, hvLength2 - hvGlueRegionInnerRadius * 0.8);
                    //    //hvPhi = new HTuple(90).TupleRad();
                    //    //HOperatorSet.GenRectangle2(out hoMaskGlueEdge, hvImageHeight * 0.5, hvImageWidth * 0.5, hvPhi, hvImageHeight * 0.5 - 10, hvImageWidth * 0.5 - 10);
                    //    //HOperatorSet.GenRectangle2(out hoMaskGlueSkeleton, hvImageHeight * 0.5, hvImageWidth * 0.5, hvPhi,
                    //    //                               hvImageHeight * 0.5 - hvGlueRegionInnerRadius * 0.8, hvImageWidth * 0.5 - hvGlueRegionInnerRadius * 0.8);
                    //}
                    //else if (hvLength1.D > 440)
                    if (hvLength1.D > 440)
                    {
                        HOperatorSet.GenRectangle2(out hoMaskGlueEdge, hvRow, hvColumn, hvPhi, hvLength1 - 200, hvLength2);
                        HOperatorSet.GenRectangle2(out hoMaskGlueSkeleton, hvRow, hvColumn, hvPhi, hvLength1 - 320, hvLength2);
                    }
                    else
                    {
                        HOperatorSet.GenRectangle2(out hoMaskGlueEdge, hvRow, hvColumn, hvPhi, hvLength1 * 0.8, hvLength2);
                        HOperatorSet.GenRectangle2(out hoMaskGlueSkeleton, hvRow, hvColumn, hvPhi, hvLength1 * 0.7, hvLength2);
                    }

                    HOperatorSet.Intersection(hoGlueSkeleton, hoMaskGlueSkeleton, out hoTmp);
                    ReplaceHobject(ref hoGlueSkeleton, ref hoTmp);
                    HOperatorSet.Intersection(hoGlueEdgeRegion, hoMaskGlueEdge, out hoTmp);
                    ReplaceHobject(ref hoGlueEdgeRegion, ref hoTmp);

                    HOperatorSet.Skeleton(hoGlueEdgeRegion, out hoEdgeSkeleton);
                    HOperatorSet.Connection(hoEdgeSkeleton, out hoEdgeSkeletons);
                    HOperatorSet.CountObj(hoEdgeSkeletons, out HTuple hvEdgeSkeletonsNum);

                    HOperatorSet.GenEmptyObj(out hoEdgeContours);
                    if (hvEdgeSkeletonsNum.I == 2)
                    {
                        for (int edgeIdx = 1; edgeIdx <= hvEdgeSkeletonsNum.I; edgeIdx++)
                        {
                            HOperatorSet.SelectObj(hoEdgeSkeletons, out hoTmpEdgeSkeleton, edgeIdx);
                            HOperatorSet.GenContoursSkeletonXld(hoTmpEdgeSkeleton, out hoTmpEdgeContours, 25, "filter");
                            HOperatorSet.UnionAdjacentContoursXld(hoTmpEdgeContours, out hoTmp, 50, 1, "attr_keep");
                            ReplaceHobject(ref hoTmpEdgeContours, ref hoTmp);

                            HOperatorSet.CountObj(hoTmpEdgeContours, out HTuple hvTmpNum);
                            if (hvTmpNum.I > 1)
                            {
                                HTuple hvEps = 1e-6;
                                HOperatorSet.LengthXld(hoTmpEdgeContours, out HTuple hvTmpLength);
                                HOperatorSet.TupleMax(hvTmpLength, out HTuple hvMaxLen);
                                HOperatorSet.SelectShapeXld(hoTmpEdgeContours, out hoTmp, "contlength", "and", hvMaxLen - hvEps, hvMaxLen + hvEps);
                                ReplaceHobject(ref hoTmpEdgeContours, ref hoTmp);
                            }
                            HOperatorSet.SmoothContoursXld(hoTmpEdgeContours, out hoTmp, 11);
                            ReplaceHobject(ref hoTmpEdgeContours, ref hoTmp);
                            HOperatorSet.ConcatObj(hoEdgeContours, hoTmpEdgeContours, out hoTmp);
                            ReplaceHobject(ref hoEdgeContours, ref hoTmp);
                        }
                    }

                    HOperatorSet.CountObj(hoEdgeContours, out HTuple hvEdgeContoursNum);
                    if (hvEdgeContoursNum.I == 2)
                    {
                        HOperatorSet.SelectObj(hoEdgeContours, out hoEdgeContour1, 1);
                        HOperatorSet.SelectObj(hoEdgeContours, out hoEdgeContour2, 2);

                        HOperatorSet.GenContoursSkeletonXld(hoGlueSkeleton, out hoGlueSkeletonContours, 25, "filter");
                        HOperatorSet.UnionAdjacentContoursXld(hoGlueSkeletonContours, out hoTmp, 50, 1, "attr_keep");
                        ReplaceHobject(ref hoGlueSkeletonContours, ref hoTmp);
                        HOperatorSet.CountObj(hoGlueSkeletonContours, out HTuple hvGlueSkeletonContoursNum);
                        if (hvGlueSkeletonContoursNum.I > 1)
                        {
                            HTuple hvEps = 1e-6;
                            HOperatorSet.LengthXld(hoGlueSkeletonContours, out HTuple hvTmpLength);
                            HOperatorSet.TupleMax(hvTmpLength, out HTuple hvMaxLen);
                            HOperatorSet.SelectShapeXld(hoGlueSkeletonContours, out hoTmp, "contlength", "and", hvMaxLen - hvEps, hvMaxLen + hvEps);
                            ReplaceHobject(ref hoGlueSkeletonContours, ref hoTmp);
                        }
                        HOperatorSet.SmoothContoursXld(hoGlueSkeletonContours, out hoTmp, 101);
                        ReplaceHobject(ref hoGlueSkeletonContours, ref hoTmp);

                        //计算采样点
                        HOperatorSet.GetContourXld(hoGlueSkeletonContours, out hvTmpGlueContourRows, out hvTmpGlueContourCols);
                        if (hvTmpGlueContourRows.Length >= 2)
                        {
                            try
                            {
                                HOperatorSet.FitLineContourXld(hoGlueSkeletonContours, "tukey", -1, 0, 5, 2,
                                                               out HTuple hvLineRowBegin, out HTuple hvLineColBegin,
                                                               out HTuple hvLineRowEnd, out HTuple hvLineColEnd,
                                                               out HTuple hvLineNr, out HTuple hvLineNc, out HTuple hvLineDist);

                                double rowBegin = hvLineRowBegin.D;
                                double colBegin = hvLineColBegin.D;
                                double rowEnd = hvLineRowEnd.D;
                                double colEnd = hvLineColEnd.D;

                                // 固定为从上到下方向，保证偏转角方向一致（右偏为正、左偏为负）
                                if (rowBegin > rowEnd)
                                {
                                    (rowBegin, rowEnd) = (rowEnd, rowBegin);
                                    (colBegin, colEnd) = (colEnd, colBegin);
                                }

                                double deltaRow = (rowEnd - rowBegin) * _measureParam.IntervalY;
                                double deltaCol = (colEnd - colBegin) * _measureParam.IntervalX;
                                double lineLength = Math.Sqrt(deltaRow * deltaRow + deltaCol * deltaCol);
                                if (lineLength > 1e-9)
                                {
                                    tmpGluePathTiltAngle = Math.Atan2(deltaCol, deltaRow) * 180.0 / Math.PI;
                                    tmpGluePathTiltWeight = lineLength;
                                }

                                hvLineRowBegin?.Dispose(); hvLineColBegin?.Dispose();
                                hvLineRowEnd?.Dispose(); hvLineColEnd?.Dispose();
                                hvLineNr?.Dispose(); hvLineNc?.Dispose(); hvLineDist?.Dispose();
                            }
                            catch (HalconException)
                            {
                                // 线拟合失败时保留默认值，避免影响后续胶宽/胶高计算
                            }
                        }
                        
                        //采样点重采样

                        // 计算采样点v0
                        //HTuple hvStep = 10;
                        //for (int tmpIdx = 0; tmpIdx < hvTmpGlueContourRows.Length; tmpIdx += hvStep)
                        //{
                        //    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        //    {
                        //        HTuple ExpTmpLocalVarIndices = hvTmpIndices.TupleConcat(tmpIdx);
                        //        hvTmpIndices.Dispose();
                        //        hvTmpIndices = ExpTmpLocalVarIndices;
                        //    }
                        //}

                        int tmpCnt = 0;          // 可视化间隔

                        // 计算采样点v1
                        //double tmpDistance = 0;  // 采样间隔
                        //if (hvTmpGlueContourRows.Length > 0)
                        //{
                        //    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        //    {
                        //        HTuple ExpTmpLocalVarIndices = hvTmpIndices.TupleConcat(0);
                        //        hvTmpIndices.Dispose();
                        //        hvTmpIndices = ExpTmpLocalVarIndices;
                        //    }
                        //    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        //    {
                        //        HTuple ExpTmpLocalVarIndices = hvIndicesView.TupleConcat(1);
                        //        hvIndicesView.Dispose();
                        //        hvIndicesView = ExpTmpLocalVarIndices;
                        //    }

                        //    for (int tmpIdx = 1; tmpIdx < (hvTmpGlueContourRows.Length); tmpIdx++)
                        //    {
                        //        HTuple PointRow1 = hvTmpGlueContourRows.TupleSelect(tmpIdx - 1);
                        //        HTuple PointCol1 = hvTmpGlueContourCols.TupleSelect(tmpIdx - 1);
                        //        HTuple PointRow2 = hvTmpGlueContourRows.TupleSelect(tmpIdx);
                        //        HTuple PointCol2 = hvTmpGlueContourCols.TupleSelect(tmpIdx);

                        //        double offsetRow = (PointRow2.D - PointRow1.D) * _measureParam.IntervalY;
                        //        double offsetCol = (PointCol2.D - PointCol1.D) * _measureParam.IntervalX;
                        //        double d = Math.Sqrt(Math.Pow(offsetRow, 2) + Math.Pow(offsetCol, 2));
                        //        tmpDistance += d;
                        //        tmpCnt++;
                        //        if (tmpDistance > _measureParam.SamplingInterval)
                        //        {
                        //            using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        //            {
                        //                HTuple ExpTmpLocalVarIndices = hvTmpIndices.TupleConcat(tmpIdx);
                        //                hvTmpIndices.Dispose();
                        //                hvTmpIndices = ExpTmpLocalVarIndices;
                        //            }
                        //            if (tmpCnt > _measureParam.SamplingViewInterval)
                        //            {
                        //                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        //                {
                        //                    HTuple ExpTmpLocalVarIndices = hvIndicesView.TupleConcat(1);
                        //                    hvIndicesView.Dispose();
                        //                    hvIndicesView = ExpTmpLocalVarIndices;
                        //                }
                        //            }
                        //            else
                        //            {
                        //                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        //                {
                        //                    HTuple ExpTmpLocalVarIndices = hvIndicesView.TupleConcat(0);
                        //                    hvIndicesView.Dispose();
                        //                    hvIndicesView = ExpTmpLocalVarIndices;
                        //                }
                        //            }
                        //            tmpCnt = 0;
                        //            tmpDistance = 0;
                        //        }
                        //    }
                        //    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        //    {
                        //        HTuple ExpTmpLocalVarGlueContourRows = hvTmpGlueContourRows.TupleSelect(hvTmpIndices);
                        //        hvTmpGlueContourRows.Dispose();
                        //        hvTmpGlueContourRows = ExpTmpLocalVarGlueContourRows;
                        //    }
                        //    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        //    {
                        //        HTuple ExpTmpLocalVarGlueContourCols = hvTmpGlueContourCols.TupleSelect(hvTmpIndices);
                        //        hvTmpGlueContourCols.Dispose();
                        //        hvTmpGlueContourCols = ExpTmpLocalVarGlueContourCols;
                        //    }
                        //}

                        // 计算采样点v2
                        if (hvTmpGlueContourRows.Length > 0)
                        {
                            HOperatorSet.LengthXld(hoGlueSkeletonContours, out HTuple hvGlueSkeletonLength);

                            HTuple hvSamplePixelStep = _measureParam.SamplingInterval / _measureParam.IntervalY;
                            HTuple hvNumSamples = ((hvGlueSkeletonLength / hvSamplePixelStep).TupleFloor()) + 1;
                            HTuple hvNumSkeletonPoint = hvTmpGlueContourRows.Length;

                            HTuple hvCumDist = new HTuple(0.0);
                            for (int tmpIdx = 1; tmpIdx < hvNumSkeletonPoint; tmpIdx++)
                            {
                                HTuple hvdRow = hvTmpGlueContourRows.TupleSelect(tmpIdx) - hvTmpGlueContourRows.TupleSelect(tmpIdx - 1);
                                HTuple hvdCol = hvTmpGlueContourCols.TupleSelect(tmpIdx) - hvTmpGlueContourCols.TupleSelect(tmpIdx - 1);
                                HTuple hvDist = (hvdRow * hvdRow + hvdCol * hvdCol).TupleSqrt();
                                HTuple hvLast = hvCumDist.TupleSelect(tmpIdx - 1);
                                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                {
                                    HTuple ExpTmpLocalVarCumDist = hvCumDist.TupleConcat(hvLast + hvDist);
                                    hvCumDist.Dispose();
                                    hvCumDist = ExpTmpLocalVarCumDist;
                                }
                            }
                            HTuple hvTotalLen = hvCumDist.TupleSelect(hvNumSkeletonPoint - 1);

                            HTuple hvSampleRows = new HTuple();
                            HTuple hvSampleCols = new HTuple();
                            HTuple hvIndex = 1;
                            for (int tmpIdx = 0; tmpIdx < hvNumSamples; tmpIdx++)
                            {
                                HTuple hvT = tmpIdx * hvSamplePixelStep;
                                if (hvT > hvTotalLen)
                                {
                                    hvT = hvTotalLen;
                                }
                                //找到第一个 CumDist[Index] >= T 的索引
                                while (hvIndex < hvNumSkeletonPoint && hvCumDist.TupleSelect(hvIndex).D < hvT.D)
                                {
                                    hvIndex += 1;
                                }

                                HTuple hvTmpRowS = new HTuple();
                                HTuple hvTmpColS = new HTuple();
                                if (hvCumDist.TupleSelect(hvIndex).D == hvT.D)
                                {
                                    hvTmpRowS = hvTmpGlueContourRows.TupleSelect(hvIndex);
                                    hvTmpColS = hvTmpGlueContourCols.TupleSelect(hvIndex);
                                }
                                else
                                {
                                    HTuple hvSegLen = hvCumDist.TupleSelect(hvIndex) - hvCumDist.TupleSelect(hvIndex - 1);
                                    if (hvSegLen.D == 0)
                                    {
                                        hvTmpRowS = hvTmpGlueContourRows.TupleSelect(hvIndex);
                                        hvTmpColS = hvTmpGlueContourCols.TupleSelect(hvIndex);
                                    }
                                    else
                                    {
                                        HTuple hvAlpha = (hvT - hvCumDist.TupleSelect(hvIndex - 1)) / hvSegLen;
                                        hvTmpRowS = ((1.0 - hvAlpha) * hvTmpGlueContourRows.TupleSelect(hvIndex - 1)) +
                                                     (hvAlpha * hvTmpGlueContourRows.TupleSelect(hvIndex));
                                        hvTmpColS = ((1.0 - hvAlpha) * hvTmpGlueContourCols.TupleSelect(hvIndex - 1)) +
                                                     (hvAlpha * hvTmpGlueContourCols.TupleSelect(hvIndex));
                                    }
                                }

                                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                {
                                    HTuple ExpTmpLocalVarSampleRows = hvSampleRows.TupleConcat(hvTmpRowS);
                                    hvSampleRows.Dispose();
                                    hvSampleRows = ExpTmpLocalVarSampleRows;
                                }
                                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                {
                                    HTuple ExpTmpLocalVarSampleCols = hvSampleCols.TupleConcat(hvTmpColS);
                                    hvSampleCols.Dispose();
                                    hvSampleCols = ExpTmpLocalVarSampleCols;
                                }

                                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                {
                                    HTuple ExpTmpLocalVarIndices = hvTmpIndices.TupleConcat(hvIndex - 1);
                                    hvTmpIndices.Dispose();
                                    hvTmpIndices = ExpTmpLocalVarIndices;
                                }

                                tmpCnt++;

                                if (tmpCnt > _measureParam.SamplingViewInterval)
                                {
                                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                    {
                                        HTuple ExpTmpLocalVarIndices = hvTmpIndicesView.TupleConcat(1);
                                        hvTmpIndicesView.Dispose();
                                        hvTmpIndicesView = ExpTmpLocalVarIndices;
                                    }
                                    tmpCnt = 0;
                                }
                                else
                                {
                                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                    {
                                        HTuple ExpTmpLocalVarIndices = hvTmpIndicesView.TupleConcat(0);
                                        hvTmpIndicesView.Dispose();
                                        hvTmpIndicesView = ExpTmpLocalVarIndices;
                                    }
                                }
                            }
                            hvTmpGlueContourRows = new HTuple(hvSampleRows);
                            hvTmpGlueContourCols = new HTuple(hvSampleCols);

                            hvSampleRows.Dispose(); hvSampleCols.Dispose();
                        }

                        if (hvTmpIndices.Length > 0)
                        {
                            HOperatorSet.GenContourPolygonXld(out hoTmp, hvTmpGlueContourRows, hvTmpGlueContourCols);
                            ReplaceHobject(ref hoGlueSkeletonContours, ref hoTmp);

                            // 计算胶宽
                            HOperatorSet.DistanceContoursXld(hoGlueSkeletonContours, hoEdgeContour1, out hoEdgeWithDistances1, "point_to_segment");
                            HOperatorSet.GetContourAttribXld(hoEdgeWithDistances1, "distance", out hvTmpDistance1);
                            HOperatorSet.DistanceContoursXld(hoGlueSkeletonContours, hoEdgeContour2, out hoEdgeWithDistances2, "point_to_segment");
                            HOperatorSet.GetContourAttribXld(hoEdgeWithDistances2, "distance", out hvTmpDistance2);

                            hvTmpGlueWidth = hvTmpDistance1 + hvTmpDistance2;
                            hvTmpGlueWidthReal = hvTmpGlueWidth * _measureParam.IntervalX;

                            // 计算胶高
                            HTuple hvDelta = 1;
                            for (int sampleId = 0; sampleId < hvTmpGlueContourRows.Length; sampleId++)
                            {
                                HOperatorSet.TupleMax2(0, sampleId - hvDelta, out HTuple hvId1);
                                HOperatorSet.TupleMin2((new HTuple(hvTmpGlueContourRows.TupleLength())) - 1, sampleId + hvDelta, out HTuple hvId2);

                                double dC = hvTmpGlueContourCols.TupleSelect(hvId2) - hvTmpGlueContourCols.TupleSelect(hvId1);
                                double dR = hvTmpGlueContourRows.TupleSelect(hvId2) - hvTmpGlueContourRows.TupleSelect(hvId1);

                                double nC = -dR;
                                double nR = dC;
                                double norm = Math.Sqrt(nC * nC + nR * nR) + 1.0e-9;
                                nC = nC / norm;
                                nR = nR / norm;

                                double R1 = hvTmpGlueContourRows.TupleSelect(sampleId) - hvTmpGlueWidth.TupleSelect(sampleId) * 2 * nR;
                                double C1 = hvTmpGlueContourCols.TupleSelect(sampleId) - hvTmpGlueWidth.TupleSelect(sampleId) * 2 * nC;
                                double R2 = hvTmpGlueContourRows.TupleSelect(sampleId) + hvTmpGlueWidth.TupleSelect(sampleId) * 2 * nR;
                                double C2 = hvTmpGlueContourCols.TupleSelect(sampleId) + hvTmpGlueWidth.TupleSelect(sampleId) * 2 * nC;

                                double phiN = Math.Atan2(C2 - C1, R2 - R1);
                                HOperatorSet.DistancePp(R1, C1, R2, C2, out HTuple TmpW);
                                HTuple hvSampleLength1, hvSampleLength2, hvPhiUse;
                                if (TmpW.D > 5)
                                {
                                    hvSampleLength1 = new HTuple(TmpW);
                                    hvSampleLength2 = 5;
                                    hvPhiUse = new HTuple(phiN);
                                }
                                else
                                {
                                    hvSampleLength1 = 5;
                                    hvSampleLength2 = new HTuple(TmpW);
                                    hvPhiUse = new HTuple(phiN);
                                }

                                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                {
                                    HTuple ExpTmpLocalVar_Angle = hvTmpAngle.TupleConcat(hvPhiUse.TupleDeg());
                                    hvTmpAngle.Dispose();
                                    hvTmpAngle = ExpTmpLocalVar_Angle;
                                }

                                HOperatorSet.GenRectangle2(out hoSampleRegion, hvTmpGlueContourRows.TupleSelect(sampleId),
                                                           hvTmpGlueContourCols.TupleSelect(sampleId), hvPhiUse + ((new HTuple(90)).TupleRad()),
                                                           hvSampleLength1, hvSampleLength2);

                                HOperatorSet.Intersection(hoSampleRegion, _hoGlueValidMask, out hoGlueSampleRegion);
                                //HOperatorSet.Intersection(hoGlueSampleRegion, _hoValidMaskL1, out hoTmp);
                                //ReplaceHobject(ref hoGlueSampleRegion, ref hoTmp);
                                //HOperatorSet.Intersection(hoSampleRegion, _hoFrameRegions, out hoFrameSampleRegion);

                                HOperatorSet.RegionFeatures(hoGlueSampleRegion, "area", out HTuple hvGlueSampleRegionArea);
                                if (hvGlueSampleRegionArea.D > 0)
                                {
                                    HTuple hvTmpGlueHeight = new HTuple(), hvTmpGlueSurface = new HTuple(), hvTmpFrameSurface = new HTuple();

                                    HOperatorSet.GrayFeatures(hoGlueSampleRegion, hoHeightImage, "median", out hvTmpGlueHeight);
                                    //HOperatorSet.GrayFeatures(hoGlueSampleRegion, hoHeightImage, "mean", out hvTmpGlueHeight);
                                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                    {
                                        HTuple ExpTmpLocalVar_GlueHeight = hvTmpGlueThickness.TupleConcat(hvTmpGlueHeight);
                                        hvTmpGlueThickness.Dispose();
                                        hvTmpGlueThickness = ExpTmpLocalVar_GlueHeight;
                                    }

                                    HOperatorSet.GrayFeatures(hoGlueSampleRegion, hoHeightImage, "median", out hvTmpGlueSurface);
                                    //HOperatorSet.GrayFeatures(hoGlueSampleRegion, hoHeightImage, "mean", out hvTmpGlueSurface);
                                    HOperatorSet.RegionFeatures(hoGlueSampleRegion, "column", out HTuple hvTmpX);
                                    HOperatorSet.RegionFeatures(hoGlueSampleRegion, "row", out HTuple hvTmpY);
                                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                    {
                                        HTuple ExpTmpLocalVar_GlueSurfaceXList = hvGlueSurfaceXList.TupleConcat(hvTmpX * _measureParam.IntervalX);
                                        hvGlueSurfaceXList.Dispose();
                                        hvGlueSurfaceXList = ExpTmpLocalVar_GlueSurfaceXList;
                                    }
                                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                    {
                                        HTuple ExpTmpLocalVar_GlueSurfaceYList = hvGlueSurfaceYList.TupleConcat(hvTmpY * _measureParam.IntervalY);
                                        hvGlueSurfaceYList.Dispose();
                                        hvGlueSurfaceYList = ExpTmpLocalVar_GlueSurfaceYList;
                                    }
                                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                    {
                                        HTuple ExpTmpLocalVar_GlueSurfaceZList = hvGlueSurfaceZList.TupleConcat(hvTmpGlueSurface * _measureParam.IntervalZ * _measureParam.RefractiveIndex);
                                        hvGlueSurfaceZList.Dispose();
                                        hvGlueSurfaceZList = ExpTmpLocalVar_GlueSurfaceZList;
                                    }

                                    HOperatorSet.GrayFeatures(hoGlueSampleRegion, hoHeightL0, "median", out hvTmpFrameSurface);
                                    //HOperatorSet.GrayFeatures(hoGlueSampleRegion, hoHeightL0, "mean", out hvTmpFrameSurface);
                                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                    {
                                        HTuple ExpTmpLocalVar_FrameSurfaceXList = hvFrameSurfaceXList.TupleConcat(hvTmpX * _measureParam.IntervalX);
                                        hvFrameSurfaceXList.Dispose();
                                        hvFrameSurfaceXList = ExpTmpLocalVar_FrameSurfaceXList;
                                    }
                                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                    {
                                        HTuple ExpTmpLocalVar_FrameSurfaceYList = hvFrameSurfaceYList.TupleConcat(hvTmpY * _measureParam.IntervalY);
                                        hvFrameSurfaceYList.Dispose();
                                        hvFrameSurfaceYList = ExpTmpLocalVar_FrameSurfaceYList;
                                    }
                                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                    {
                                        HTuple ExpTmpLocalVar_FrameSurfaceZList = hvFrameSurfaceZList.TupleConcat(hvTmpFrameSurface * _measureParam.IntervalZ * _measureParam.RefractiveIndex);
                                        hvFrameSurfaceZList.Dispose();
                                        hvFrameSurfaceZList = ExpTmpLocalVar_FrameSurfaceZList;
                                    }
                                }
                                else
                                {
                                    // 无效高度值
                                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                    {
                                        HTuple ExpTmpLocalVar_GlueHeight = hvTmpGlueThickness.TupleConcat(-10);
                                        hvTmpGlueThickness.Dispose();
                                        hvTmpGlueThickness = ExpTmpLocalVar_GlueHeight;
                                    }
                                }
                            }

                            hvTmpGlueThicknessReal = hvTmpGlueThickness * _measureParam.IntervalZ * _measureParam.RefractiveIndex;
                        }

                    }

                    if (!double.IsNaN(tmpGluePathTiltAngle) && tmpGluePathTiltWeight > 0)
                    {
                        gluePathTiltWeightedSum += tmpGluePathTiltAngle * tmpGluePathTiltWeight;
                        gluePathTiltWeightSum += tmpGluePathTiltWeight;
                    }

                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        HTuple ExpTmpLocalVar_Angle = hvAngle.TupleConcat(hvTmpAngle);
                        hvAngle.Dispose();
                        hvAngle = ExpTmpLocalVar_Angle;
                    }
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        HTuple ExpTmpLocalVar_GlueContourRows = hvGlueContourRows.TupleConcat(hvTmpGlueContourRows);
                        hvGlueContourRows.Dispose();
                        hvGlueContourRows = ExpTmpLocalVar_GlueContourRows;
                    }
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        HTuple ExpTmpLocalVar_GlueContourCols = hvGlueContourCols.TupleConcat(hvTmpGlueContourCols);
                        hvGlueContourCols.Dispose();
                        hvGlueContourCols = ExpTmpLocalVar_GlueContourCols;
                    }
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        HTuple ExpTmpLocalVar_GlueWidthList = hvGlueWidthList.TupleConcat(hvTmpGlueWidth);
                        hvGlueWidthList.Dispose();
                        hvGlueWidthList = ExpTmpLocalVar_GlueWidthList;
                    }
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        HTuple ExpTmpLocalVar_GlueThicknessList = hvGlueThicknessList.TupleConcat(hvTmpGlueThickness);
                        hvGlueThicknessList.Dispose();
                        hvGlueThicknessList = ExpTmpLocalVar_GlueThicknessList;
                    }
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        HTuple ExpTmpLocalVar_GlueWidthRealList = hvGlueWidthRealList.TupleConcat(hvTmpGlueWidthReal);
                        hvGlueWidthRealList.Dispose();
                        hvGlueWidthRealList = ExpTmpLocalVar_GlueWidthRealList;
                    }
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        HTuple ExpTmpLocalVar_GlueThicknessRealList = hvGlueThicknessRealList.TupleConcat(hvTmpGlueThicknessReal);
                        hvGlueThicknessRealList.Dispose();
                        hvGlueThicknessRealList = ExpTmpLocalVar_GlueThicknessRealList;
                    }
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        HTuple ExpTmpLocalVar_hvIndices = hvIndices.TupleConcat(hvTmpIndices);
                        hvIndices.Dispose();
                        hvIndices = ExpTmpLocalVar_hvIndices;
                    }
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        HTuple ExpTmpLocalVar_hvIndicesView = hvIndicesView.TupleConcat(hvTmpIndicesView);
                        hvIndicesView.Dispose();
                        hvIndicesView = ExpTmpLocalVar_hvIndicesView;
                    }

                    hvTmpGlueContourRows?.Dispose(); hvTmpGlueContourCols?.Dispose();
                    hvTmpDistance1?.Dispose(); hvTmpDistance2?.Dispose(); hvTmpGlueWidth?.Dispose(); hvTmpGlueWidthReal?.Dispose();
                    hvTmpGlueThickness?.Dispose(); hvTmpGlueThicknessReal.Dispose(); hvTmpIndices.Dispose(); hvTmpIndicesView.Dispose();
                    hvTmpAngle?.Dispose();
                }

                if (hvGlueContourRows.Length > 0)
                {
                    result.MeasurePointXList = hvGlueContourCols.DArr;
                    result.MeasurePointYList = hvGlueContourRows.DArr;
                }
                if (hvGlueWidthRealList.Length > 0)
                {
                    result.GlueWidthList = hvGlueWidthRealList.DArr;
                    result.GlueWidthPixelList = hvGlueWidthList.DArr;
                    result.GlueWidthAngleList = hvAngle.DArr;

                    HTuple tmpAllIdx;
                    result.GlueWidthMax = hvGlueWidthRealList.TupleMax().D;
                    result.GlueWidthPixelMax = hvGlueWidthList.TupleMax().D;
                    tmpAllIdx = hvGlueWidthRealList.TupleFind(result.GlueWidthMax);
                    result.GlueWidthMaxPointX = hvGlueContourCols[tmpAllIdx.I].DArr;
                    result.GlueWidthMaxPointY = hvGlueContourRows[tmpAllIdx.I].DArr;
                    result.GlueWidthMaxAngle = hvAngle[tmpAllIdx.I].DArr;

                    result.GlueWidthMin = hvGlueWidthRealList.TupleMin().D;
                    result.GlueWidthPixelMin = hvGlueWidthList.TupleMin().D;
                    tmpAllIdx = hvGlueWidthRealList.TupleFind(result.GlueWidthMin);
                    result.GlueWidthMinPointX = hvGlueContourCols[tmpAllIdx.I].DArr;
                    result.GlueWidthMinPointY = hvGlueContourRows[tmpAllIdx.I].DArr;
                    result.GlueWidthMinAngle = hvAngle[tmpAllIdx.I].DArr;

                    result.GlueWidthAvg = hvGlueWidthRealList.TupleMean().D;
                }
                if (hvGlueThicknessList.Length > 0 && hvGlueThicknessRealList.Length > 0)
                {
                    result.GlueThicknessList = hvGlueThicknessRealList.DArr;

                    HTuple hvValidMask = hvGlueThicknessList.TupleGreaterElem(0);
                    HTuple hvValidThicknessList = hvGlueThicknessRealList.TupleSelectMask(hvValidMask);
                    HTuple hvValidContourRows = hvGlueContourRows.TupleSelectMask(hvValidMask);
                    HTuple hvValidContourCols = hvGlueContourCols.TupleSelectMask(hvValidMask);

                    HTuple tmpAllIdx;
                    if (hvValidThicknessList.Length > 0)
                    {
                        result.GlueThicknessMax = hvValidThicknessList.TupleMax().D;
                        tmpAllIdx = hvValidThicknessList.TupleFind(result.GlueThicknessMax);
                        result.GlueThicknessMaxPointX = hvValidContourCols[tmpAllIdx.I].DArr;
                        result.GlueThicknessMaxPointY = hvValidContourRows[tmpAllIdx.I].DArr;

                        result.GlueThicknessMin = hvValidThicknessList.TupleMin().D;
                        tmpAllIdx = hvValidThicknessList.TupleFind(result.GlueThicknessMin);
                        result.GlueThicknessMinPointX = hvValidContourCols[tmpAllIdx.I].DArr;
                        result.GlueThicknessMinPointY = hvValidContourRows[tmpAllIdx.I].DArr;

                        result.GlueThicknessAvg = hvValidThicknessList.TupleMean().D;
                    }
                    else
                    {
                        result.GlueThicknessMax = -1;
                        result.GlueThicknessMaxPointX = new double[] { };
                        result.GlueThicknessMaxPointY = new double[] { };

                        result.GlueThicknessMin = -1;
                        result.GlueThicknessMinPointX = new double[] { };
                        result.GlueThicknessMinPointY = new double[] { };

                        result.GlueThicknessAvg = -1;
                    }
                }
                if (hvGlueSurfaceXList.Length > 0)
                {
                    result.GlueSurfaceXList = hvGlueSurfaceXList.DArr;
                    result.GlueSurfaceYList = hvGlueSurfaceYList.DArr;
                    result.GlueSurfaceZList = hvGlueSurfaceZList.DArr;
                }
                if (hvFrameSurfaceXList.Length > 0)
                {
                    result.FrameSurfaceXList = hvFrameSurfaceXList.DArr;
                    result.FrameSurfaceYList = hvFrameSurfaceYList.DArr;
                    result.FrameSurfaceZList = hvFrameSurfaceZList.DArr;
                }
                if (hvIndices.Length > 0)
                {
                    result.SampleIdx = hvIndices.IArr;
                    result.SampleViewIdx = hvIndicesView.IArr;
                }
                if (gluePathTiltWeightSum > 0)
                {
                    result.GluePathTiltAngle = gluePathTiltWeightedSum / gluePathTiltWeightSum;
                }
            }
            finally
            {
                hoGlueRegionsConnected?.Dispose(); hoTmpSelectedRegions?.Dispose(); hoGlueRegion?.Dispose(); hoGlueSkeleton?.Dispose();
                hoRegionDilation?.Dispose(); hoRegionErosion?.Dispose(); hoGlueEdgeRegion?.Dispose();
                hoMaskGlueEdge?.Dispose(); hoMaskGlueSkeleton?.Dispose(); hoEdgeSkeleton?.Dispose();
                hoEdgeSkeletons?.Dispose(); hoEdgeContours?.Dispose(); hoTmpEdgeSkeleton?.Dispose();
                hoTmpEdgeContours?.Dispose(); hoEdgeContour1?.Dispose(); hoEdgeContour2?.Dispose();
                hoGlueSkeletonContours?.Dispose(); hoEdgeWithDistances1?.Dispose(); hoEdgeWithDistances2?.Dispose();
                hoSampleRegion?.Dispose(); hoGlueSampleRegion?.Dispose();
                hoFrameSampleRegion?.Dispose();
            }

            return result;
        }


        public static List<Point3d> ToCvPoint3d(double[] X, double[] Y, double[] Z)
        {
            if (X == null || Y == null || Z == null)
                throw new ArgumentNullException("X/Y/Z cannot be null.");

            int n = Math.Min(X.Length, Math.Min(Y.Length, Z.Length));
            var pts = new List<Point3d>(n);
            for (int i = 0; i < n; i++)
                pts.Add(new Point3d(X[i], Y[i], Z[i]));
            return pts;
        }


        public static double[] SignedResiduals(List<Point3d> pts, Plane plane)
        {
            double den = Math.Sqrt(plane.A * plane.A + plane.B * plane.B + plane.C * plane.C) + 1e-12;
            var r = new double[pts.Count];
            for (int i = 0; i < pts.Count; i++)
            {
                var p = pts[i];
                r[i] = (plane.A * p.X + plane.B * p.Y + plane.C * p.Z + plane.D) / den; // 有符号距离
            }
            return r;
        }


        // 1.4826 * MAD 作为 sigma 的一致性估计（正态分布下常用）
        public static int MadStats(List<double> values, out double median, out double mad, out double sigmaHat)
        {
            double tmpMed = values.Median();

            List<double> abs = values.Select(v => Math.Abs(v - tmpMed)).ToList();

            median = tmpMed;
            mad = abs.Median();
            sigmaHat = 1.4826 * (mad + 1e-12);

            return 0;
        }


        /// <summary>
        /// 基于残差的统计剔除
        /// </summary>
        public List<Point3d> TrimByMad(List<Point3d> pts, double k = 3.0)
        {
            var keep = new List<Point3d>();

            if (pts.Count < 3)
                return pts;

            Plane plane = FitPlane(pts);

            List<double> r = pts.Select(p => plane.DistanceTo(p)).ToList();
            MadStats(r, out double med, out double mad, out double sigmaHat);


            for (int i = 0; i < pts.Count; i++)
                if (Math.Abs(r[i] - med) <= k * sigmaHat)
                    keep.Add(pts[i]);

            return keep;
        }



        // IRLS(Huber)+加权PCA的鲁棒平面拟合
        public Plane FitPlaneIrlsPCA(List<Point3d> pts, int maxIter = 30, double tol = 1e-6)
        {
            var plane = FitPlane(pts);

            for (int it = 0; it < maxIter; it++)
            {
                List<double> r = pts.Select(p => plane.DistanceTo(p)).ToList();
                MadStats(r, out double med, out double mad, out double sigmaHat);
                double delta = 1.345 * sigmaHat + 1e-12;   // Huber阈值

                // 权重
                var w = r.Select(a => { var t = Math.Abs(a); return (t <= delta) ? 1.0 : (delta / t); }).ToArray();

                // 加权质心
                double sw = w.Sum();
                double mx = 0, my = 0, mz = 0;
                for (int i = 0; i < pts.Count; i++)
                {
                    mx += w[i] * pts[i].X;
                    my += w[i] * pts[i].Y;
                    mz += w[i] * pts[i].Z;
                }
                mx /= sw;
                my /= sw;
                mz /= sw;

                // 计算加权协方差矩阵
                double sxx = 0, sxy = 0, sxz = 0, syy = 0, syz = 0, szz = 0;
                for (int i = 0; i < pts.Count; i++)
                {
                    double dx = pts[i].X - mx, dy = pts[i].Y - my, dz = pts[i].Z - mz, wi = w[i];
                    sxx += wi * dx * dx;
                    sxy += wi * dx * dy;
                    sxz += wi * dx * dz;
                    syy += wi * dy * dy;
                    syz += wi * dy * dz;
                    szz += wi * dz * dz;
                }
                var M = Matrix<double>.Build.DenseOfArray(new double[,] { { sxx, sxy, sxz }, { sxy, syy, syz }, { sxz, syz, szz } });

                // 最小特征向量等于法向
                var evd = M.Evd(Symmetricity.Symmetric);
                var evals = evd.EigenValues.Select(z => z.Real).ToArray();
                int k = Array.IndexOf(evals, evals.Min());
                var n = evd.EigenVectors.Column(k).Normalize(2);

                var newPlane = new Plane(n[0], n[1], n[2], -(n[0] * mx + n[1] * my + n[2] * mz));

                if (Math.Abs(newPlane.A - plane.A) + Math.Abs(newPlane.B - plane.B) +
                    Math.Abs(newPlane.C - plane.C) + Math.Abs(newPlane.D - plane.D) < tol)
                    return newPlane;

                plane = newPlane;
            }

            return plane;
        }



        /// <summary>
        /// 最小二乘法拟合平面
        /// </summary>
        /// <param name="points"></param>
        /// <returns>平面参数 [a, b, c, d] 对应于平面方程 ax + by + cz + d = 0</returns>
        public Plane FitPlane(List<Point3d> points)
        {
            var matrix = Matrix<double>.Build;
            var vector = Vector<double>.Build;

            var data = matrix.Dense(points.Count, 3, (i, j) =>
            {
                return j switch
                {
                    0 => points[i].X,
                    1 => points[i].Y,
                    2 => points[i].Z,
                    _ => 0
                };
            });

            var centroid = vector.DenseOfEnumerable(new[] { points.Average(p => p.X), points.Average(p => p.Y), points.Average(p => p.Z) });
            var centered = data - matrix.Dense(data.RowCount, 3, (i, j) => centroid[j]);

            // 奇异值分解
            var svd = centered.Svd();
            // 平面法向量
            var normal = svd.VT.Row(2);
            normal = normal.Normalize(2);

            double d = -normal.DotProduct(centroid);

            return new Plane(normal[0], normal[1], normal[2], d);
        }


        /// <summary>
        /// 计算平面度（平面上各点到拟合平面的最大距离与最小距离之差）
        /// </summary>
        /// <param name="points"></param>
        /// <param name="plane"></param>
        /// <returns></returns>
        private double CalculateFlatness(List<Point3d> points, Plane plane)
        {
            var distances = points.Select(p => plane.DistanceTo(p)).ToList();
            return distances.Max() - distances.Min();
        }


        /// <summary>
        /// 平面度测量(简易版)
        /// </summary>
        private SideResult GetFlatnessSimple(SideResult result)
        {
            List<Point3d> glueSurfacePoints = ToCvPoint3d(result.GlueSurfaceXList, result.GlueSurfaceYList,
                                                          result.GlueSurfaceZList);
            List<Point3d> frameSurfacePoints = ToCvPoint3d(result.FrameSurfaceXList, result.FrameSurfaceYList,
                                                           result.FrameSurfaceZList);

            result.GlueFlatness = glueSurfacePoints.Select(p => p.Z).Max() - glueSurfacePoints.Select(p => p.Z).Min();
            result.FrameFlatness = frameSurfacePoints.Select(p => p.Z).Max() - frameSurfacePoints.Select(p => p.Z).Min();

            return result;
        }


        /// <summary>
        /// 平面度测量
        /// </summary>
        private SideResult GetFlatness(SideResult result)
        {
            List<Point3d> glueSurfacePoints = ToCvPoint3d(result.GlueSurfaceXList, result.GlueSurfaceYList,
                                                          result.GlueSurfaceZList);
            List<Point3d> frameSurfacePoints = ToCvPoint3d(result.FrameSurfaceXList, result.FrameSurfaceYList,
                                                           result.FrameSurfaceZList);

            Plane fitGlueSurfacePlane = FitPlane(glueSurfacePoints);
            Plane fitFrameSurfacePlane = FitPlane(frameSurfacePoints);

            result.GlueFlatness = CalculateFlatness(glueSurfacePoints, fitGlueSurfacePlane);
            result.FrameFlatness = CalculateFlatness(frameSurfacePoints, fitFrameSurfacePlane);

            return result;
        }

        /// <summary>
        /// 将 Point3d 列表按列存储到 CSV 文件
        /// </summary>
        /// <param name="points">点坐标列表</param>
        /// <param name="filePath">CSV 文件路径</param>
        public static void SavePoint3dToCsv(List<Point3d> points, string filePath)
        {
            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                foreach (var pt in points)
                {
                    writer.WriteLine($"{pt.X},{pt.Y},{pt.Z}");
                }
            }
        }


        /// <summary>
        /// 平面度测量(剔除离群点，鲁棒平面度)
        /// </summary>
        private SideResult GetFlatnessRobust(SideResult result)
        {
            List<Point3d> glueSurfacePoints = ToCvPoint3d(result.GlueSurfaceXList, result.GlueSurfaceYList,
                                                          result.GlueSurfaceZList);
            List<Point3d> frameSurfacePoints = ToCvPoint3d(result.FrameSurfaceXList, result.FrameSurfaceYList,
                                                           result.FrameSurfaceZList);

            if (glueSurfacePoints.Count > 3)
            {
                List<Point3d> gluePointsIn = TrimByMad(glueSurfacePoints, 3.0);
                var fitGlueSurfacePlane = FitPlaneIrlsPCA(gluePointsIn);
                result.GlueFlatness = CalculateFlatness(gluePointsIn, fitGlueSurfacePlane);
            }

            if (frameSurfacePoints.Count > 3)
            {
                List<Point3d> framePointsIn = TrimByMad(frameSurfacePoints, 3.0);
                var fitFrameSurfacePlane = FitPlaneIrlsPCA(framePointsIn);
                result.FrameFlatness = CalculateFlatness(framePointsIn, fitFrameSurfacePlane);
            }

            return result;
        }


        /// <summary>
        /// 胶高胶宽平面度测量过程
        /// </summary>
        private SideResult MeasureProcess(HObject? hoGrayImage, HObject? hoHeightL0, HObject? hoHeightL1,
                                          HObject? hoGlueHeight, HTuple hvScaleX, HTuple hvScaleY)
        {
            SideResult result = new SideResult();
            using (var dh = new HDevDisposeHelper())
            {
                HObject? hoTmp = null;

                try
                {
                    // 提取胶面区域
                    HOperatorSet.Threshold(hoGlueHeight, out hoTmp, _measureParam.GlueLowThresh, _measureParam.GlueUpThresh);
                    ReplaceHobject(ref _hoGlueRegions, ref hoTmp);
                    //_hoGlueValidMask = _hoGlueRegions.Clone();
                    HOperatorSet.OpeningCircle(_hoGlueRegions, out hoTmp, 5);
                    ReplaceHobject(ref _hoGlueRegions, ref hoTmp);
                    HOperatorSet.ClosingCircle(_hoGlueRegions, out hoTmp, 5);
                    ReplaceHobject(ref _hoGlueRegions, ref hoTmp);
                    HOperatorSet.Connection(_hoGlueRegions, out hoTmp);
                    ReplaceHobject(ref _hoGlueRegions, ref hoTmp);
                    HOperatorSet.SelectShape(_hoGlueRegions, out hoTmp, "area", "and", 2500000.0 / _measureParam.IntervalX, 9999999999);
                    ReplaceHobject(ref _hoGlueRegions, ref hoTmp);
                    HOperatorSet.Union1(_hoGlueRegions, out hoTmp);
                    ReplaceHobject(ref _hoGlueRegions, ref hoTmp);
                    HOperatorSet.FillUp(_hoGlueRegions, out hoTmp);
                    ReplaceHobject(ref _hoGlueRegions, ref hoTmp);

                    // 从灰度图中提取胶面区域
                    HOperatorSet.Threshold(hoGrayImage, out hoTmp, 250, 255);
                    ReplaceHobject(ref _hoGrayGlueRegions, ref hoTmp);
                    HOperatorSet.FillUp(_hoGrayGlueRegions, out hoTmp);
                    ReplaceHobject(ref _hoGrayGlueRegions, ref hoTmp);
                    HOperatorSet.OpeningCircle(_hoGrayGlueRegions, out hoTmp, 10);
                    ReplaceHobject(ref _hoGrayGlueRegions, ref hoTmp);
                    HOperatorSet.Connection(_hoGrayGlueRegions, out hoTmp);
                    ReplaceHobject(ref _hoGrayGlueRegions, ref hoTmp);
                    HOperatorSet.SelectShape(_hoGrayGlueRegions, out hoTmp, "area", "and", 2500000.0 / _measureParam.IntervalX, 9999999999);
                    ReplaceHobject(ref _hoGrayGlueRegions, ref hoTmp);

                    HObject hoTmpSelectedRegions;
                    HOperatorSet.SelectShapeStd(_hoGrayGlueRegions, out hoTmpSelectedRegions, "max_area", 70);
                    HOperatorSet.RegionFeatures(hoTmpSelectedRegions, "inner_radius", out HTuple hvTmpInnerRadius);
                    //HOperatorSet.SelectShape(_hoGrayGlueRegions, out hoTmp, "inner_radius", "and", hvTmpInnerRadius * 0.6, hvTmpInnerRadius * 2);
                    HOperatorSet.SelectShape(_hoGrayGlueRegions, out hoTmp, (new HTuple("inner_radius")).TupleConcat("dist_deviation"), "and",
                                             (hvTmpInnerRadius * 0.6).TupleConcat(4000.0 / _measureParam.IntervalX), (hvTmpInnerRadius * 2).TupleConcat(9999999999));
                    ReplaceHobject(ref _hoGrayGlueRegions, ref hoTmp);
                    hoTmpSelectedRegions.Dispose();

                    HOperatorSet.Union1(_hoGrayGlueRegions, out hoTmp);
                    ReplaceHobject(ref _hoGrayGlueRegions, ref hoTmp);
                    HOperatorSet.ClosingCircle(_hoGrayGlueRegions, out hoTmp, 5);
                    ReplaceHobject(ref _hoGrayGlueRegions, ref hoTmp);
                    HOperatorSet.FillUp(_hoGrayGlueRegions, out hoTmp);
                    ReplaceHobject(ref _hoGrayGlueRegions, ref hoTmp);

                    // 提取框面区域
                    HOperatorSet.Threshold(hoGlueHeight, out hoTmp, _measureParam.FrameLowThresh, _measureParam.FrameUpThresh);
                    ReplaceHobject(ref _hoFrameRegions, ref hoTmp);
                    HOperatorSet.OpeningCircle(_hoFrameRegions, out hoTmp, 5);
                    ReplaceHobject(ref _hoFrameRegions, ref hoTmp);
                    HOperatorSet.ErosionCircle(_hoFrameRegions, out hoTmp, 15);
                    ReplaceHobject(ref _hoFrameRegions, ref hoTmp);

                    // 计算胶宽、胶高
                    result = GetGlueWidthAndThickness(hoGrayImage, hoHeightL0, hoHeightL1, hoGlueHeight, result);

                    // 计算平面度
                    //result = GetFlatness(result);
                    //result = GetFlatnessRobust(result);
                    result = GetFlatnessSimple(result);

                }
                finally
                {
                    hoTmp?.Dispose();
                }

                return result;
            }
        }


        public static int Sigmoid(HObject? hoInImage, out HObject? hoOutImage)
        {
            HObject? hoOnes, hoTmp;

            HOperatorSet.GetImageSize(hoInImage, out HTuple hvCracksWidth, out HTuple hvCracksHeight);
            HOperatorSet.ScaleImage(hoInImage, out hoTmp, -1, 0);
            ReplaceHobject(ref hoInImage, ref hoTmp);
            HOperatorSet.ExpImage(hoInImage, out hoTmp, "e");
            ReplaceHobject(ref hoInImage, ref hoTmp);
            HOperatorSet.ScaleImage(hoInImage, out hoTmp, 1, 1);
            ReplaceHobject(ref hoInImage, ref hoTmp);
            HOperatorSet.GenImageConst(out hoOnes, "real", hvCracksWidth, hvCracksHeight);
            HOperatorSet.ScaleImage(hoOnes, out hoTmp, 1, 1);
            ReplaceHobject(ref hoOnes, ref hoTmp);
            HOperatorSet.DivImage(hoOnes, hoInImage, out hoOutImage, 1, 0);

            hoOnes?.Dispose(); hoTmp?.Dispose();

            return 0;
        }


        /// <summary>
        /// 组装测量结果objInfo, objectNum
        /// </summary>
        private List<DefectResult> PourObjectInfo(IntPtr objInfo, int objectNum, HObject hoGrayImage, HObject hoHeightImage, double scaleX, double scaleY)
        {
            List<DefectResult> defects = new List<DefectResult>();

            HObject? hoTmp = null;
            HObject? hoDefectMask = null, hoDefectMasks = null, hoTmpRegion = null;
            HObject? hoDefectMaskSelect = null, hoDefectZ = null;

            HOperatorSet.GenEmptyObj(out hoTmp);
            HOperatorSet.GenEmptyObj(out hoDefectMask);
            HOperatorSet.GenEmptyObj(out hoDefectMasks);
            HOperatorSet.GenEmptyObj(out hoDefectMaskSelect);
            HOperatorSet.GenEmptyObj(out hoDefectZ);
            HOperatorSet.GenEmptyObj(out hoTmpRegion);

            try
            {
                //HObject? hoMaskOut = null;
                //HOperatorSet.GenEmptyObj(out hoMaskOut);
                int NativeResultSize = Marshal.SizeOf<NativeResult>();
                for (int i = 0; i < objectNum; i++)
                {
                    IntPtr currentBoxPtr = IntPtr.Add(objInfo, i * NativeResultSize);
                    NativeResult nativeBox = Marshal.PtrToStructure<NativeResult>(currentBoxPtr);

                    DefectResult defect = new DefectResult();
                    defect.IsOk = false;
                    defect.InstanceId = i;
                    defect.Left = (nativeBox.Cx - 0.5 * nativeBox.Width) * scaleX;
                    defect.Top = (nativeBox.Cy - 0.5 * nativeBox.Height) * scaleY;
                    defect.Right = (nativeBox.Cx + 0.5 * nativeBox.Width) * scaleX;
                    defect.Bottom = (nativeBox.Cy + 0.5 * nativeBox.Height) * scaleY;
                    defect.Confidence = nativeBox.Confidence;
                    defect.ClassName = Marshal.PtrToStringUTF8(nativeBox.ClassName) ?? string.Empty;

                    // 获取缺陷区域特征
                    if (nativeBox.Segmentation.FloatData != IntPtr.Zero)
                    {
                        HTuple hvaffineMatrix = new HTuple(nativeBox.Segmentation.AffineMatrix);
                        // 修正halcon仿射变换与OpenCV的差异
                        hvaffineMatrix[0] = nativeBox.Segmentation.AffineMatrix[4];
                        hvaffineMatrix[1] = nativeBox.Segmentation.AffineMatrix[3];
                        hvaffineMatrix[2] = nativeBox.Segmentation.AffineMatrix[5];
                        hvaffineMatrix[3] = nativeBox.Segmentation.AffineMatrix[1];
                        hvaffineMatrix[4] = nativeBox.Segmentation.AffineMatrix[0];
                        hvaffineMatrix[5] = nativeBox.Segmentation.AffineMatrix[2];

                        HTuple hvthreshold = new HTuple(nativeBox.Segmentation.Thresh);

                        IntPtr pointer = nativeBox.Segmentation.FloatData;
                        HOperatorSet.GenImage1(out hoTmp, "real", nativeBox.Segmentation.Width, nativeBox.Segmentation.Height, pointer);
                        ReplaceHobject(ref hoDefectMask, ref hoTmp);

                        // Sigmoid(hoDefectMask, out hoTmp);
                        // ReplaceHobject(ref hoDefectMask, ref hoTmp);

                        HOperatorSet.AffineTransImage(hoDefectMask, out hoTmp, hvaffineMatrix, "bilinear", "true");
                        ReplaceHobject(ref hoDefectMask, ref hoTmp);

                        HOperatorSet.Threshold(hoDefectMask, out hoTmp, hvthreshold, 255);
                        ReplaceHobject(ref hoDefectMask, ref hoTmp);

                        HObject hoBoxMask;
                        HOperatorSet.GenRectangle1(out hoBoxMask, 
                                                   nativeBox.Cy - 0.5 * nativeBox.Height, 
                                                   nativeBox.Cx - 0.5 * nativeBox.Width,
                                                   nativeBox.Cy + 0.5 * nativeBox.Height,
                                                   nativeBox.Cx + 0.5 * nativeBox.Width);
                        HOperatorSet.Intersection(hoBoxMask, hoDefectMask, out hoTmp);
                        ReplaceHobject(ref hoDefectMask, ref hoTmp);
                        hoBoxMask.Dispose();
                    }

                    HOperatorSet.RegionFeatures(hoDefectMask, "area", out HTuple TmpArea);
                    if (TmpArea < 0 || nativeBox.Segmentation.FloatData == IntPtr.Zero)
                    {
                        HOperatorSet.GenRectangle1(out hoTmp,
                                                   nativeBox.Cy - 0.5 * nativeBox.Height,
                                                   nativeBox.Cx - 0.5 * nativeBox.Width,
                                                   nativeBox.Cy + 0.5 * nativeBox.Height,
                                                   nativeBox.Cx + 0.5 * nativeBox.Width);
                        ReplaceHobject(ref hoDefectMask, ref hoTmp);
                    }

                    // 计算宽高与面积
                    HOperatorSet.ZoomRegion(hoDefectMask, out hoTmp, scaleX, scaleY);
                    ReplaceHobject(ref hoDefectMask, ref hoTmp);

                    HOperatorSet.Intersection(hoDefectMask, _hoValidMaskL1, out hoTmp);
                    ReplaceHobject(ref hoDefectMask, ref hoTmp);

                    HOperatorSet.RegionFeatures(hoDefectMask, "outer_radius", out HTuple hvOuterRadius);
                    HOperatorSet.RegionFeatures(hoDefectMask, "area", out HTuple hvArea);

                    if (hvArea.D == 0)
                        continue;

                    // 计算缺陷深度
                    HOperatorSet.Connection(hoDefectMask, out hoDefectMasks);
                    //HOperatorSet.SelectShape(hoDefectMasks, out hoTmp, "area", "and", 9, 9999999999999999999);
                    //ReplaceHobject(ref hoDefectMasks, ref hoTmp);

                    //HOperatorSet.ConcatObj(hoMaskOut, hoDefectMasks, out hoMaskOut);

                    HOperatorSet.CountObj(hoDefectMasks, out HTuple hvMaskNum);

                    HTuple hvDepthFeature = new HTuple();
                    for (int j = 0; j < hvMaskNum; j++)
                    {
                        HOperatorSet.SelectObj(hoDefectMasks, out hoDefectMaskSelect, j + 1);
                        HOperatorSet.FillUp(hoDefectMaskSelect, out hoTmp);
                        ReplaceHobject(ref hoDefectMaskSelect, ref hoTmp);

                        HOperatorSet.RegionFeatures(hoDefectMaskSelect, "area", out HTuple hvTmpArea);

                        if (hvTmpArea.D == 0)
                            continue;

                        //Console.WriteLine($"i:{i}  j:{j}");
                        //if (i == 145 && j == 0)
                        //{
                        //    Console.WriteLine("--->");

                        //    HOperatorSet.GetImageSize(hoGrayImage, out HTuple hvWidth, out HTuple hvHeight);

                        //    HOperatorSet.RegionToBin(_hoValidMaskL1, out HObject hoValidMaskBinImage, 255, 0, hvWidth, hvHeight);
                        //    HOperatorSet.RegionToBin(hoDefectMasks, out HObject hoDefectMaskBinImage, 255, 0, hvWidth, hvHeight);
                        //    HOperatorSet.RegionToBin(hoDefectMaskSelect, out HObject hoDefectMaskSelectBinImage, 255, 0, hvWidth, hvHeight);


                        //    HOperatorSet.WriteImage(hoHeightImage, "tiff", 0, "E:\\dataset\\02_KBTDispensing\\20251207_result\\hoHeightImage.tif");

                        //    HOperatorSet.WriteImage(hoValidMaskBinImage, "png", 0, "E:\\dataset\\02_KBTDispensing\\20251207_result\\hoValidMaskBinImage.png");
                        //    HOperatorSet.WriteImage(hoDefectMaskBinImage, "png", 0, "E:\\dataset\\02_KBTDispensing\\20251207_result\\hoDefectMaskBinImage.png");
                        //    HOperatorSet.WriteImage(hoDefectMaskSelectBinImage, "png", 0, "E:\\dataset\\02_KBTDispensing\\20251207_result\\hoDefectMaskSelectBinImage.png");
                        //}

                        HOperatorSet.ReduceDomain(hoHeightImage, hoDefectMaskSelect, out hoDefectZ);
                        HTuple hvRows, hvCols;
                        HOperatorSet.GetRegionPoints(hoDefectZ, out hvRows, out hvCols);
                        HTuple hvX = hvCols * _measureParam.IntervalX;
                        HTuple hvY = hvRows * _measureParam.IntervalY;
                        HTuple hvZ;
                        HOperatorSet.GetGrayval(hoDefectZ, hvRows, hvCols, out hvZ);

                        if (hvTmpArea.D > 0 && hvTmpArea.D < 9)
                        {
                            if (hvTmpArea.D == 1)
                            {
                                hvDepthFeature = hvDepthFeature.TupleConcat(0);
                            }
                            else
                            {
                                hvDepthFeature = hvZ.TupleMax() - hvZ.TupleMin();
                            }
                        }
                        else
                        {
                            HOperatorSet.GenObjectModel3dFromPoints(hvX, hvY, hvZ, out HTuple hvDefectCloud);

                            //调整姿态
                            HTuple hvPlane;
                            HTuple hvPose, hvNormal;
                            HTuple hvPoseMat;
                            HOperatorSet.FitPrimitivesObjectModel3d(hvDefectCloud, (new HTuple("primitive_type")).TupleConcat("fitting_algorithm"),
                                                                                   (new HTuple("plane")).TupleConcat("least_squares"), out hvPlane);
                            HOperatorSet.GetObjectModel3dParams(hvPlane, "primitive_parameter_pose", out hvPose);
                            HOperatorSet.PoseInvert(hvPose, out hvPose);
                            HOperatorSet.GetObjectModel3dParams(hvPlane, "primitive_parameter", out hvNormal);
                            if ((int)(new HTuple(((hvNormal.TupleSelect(2))).TupleLess(0))) != 0)
                            {
                                HTuple hvFlip;
                                HOperatorSet.CreatePose(0, 0, 0, 180, 0, 0, "Rp+T", "gba", "point", out hvFlip);
                                HOperatorSet.PoseCompose(hvFlip, hvPose, out hvPose);
                            }
                            HOperatorSet.PoseToHomMat3d(hvPose, out hvPoseMat);
                            HOperatorSet.ConnectionObjectModel3d(hvDefectCloud, "distance_3d", 2 * _measureParam.IntervalX, out HTuple hvConnCloud);
                            HOperatorSet.SelectObjectModel3d(hvConnCloud, "num_points", "and", 9, 9999999999999999999, out HTuple hvSeleCloud);
                            HOperatorSet.UnionObjectModel3d(hvSeleCloud, "points_surface", out HTuple hvUnionCloud);
                            HOperatorSet.GetObjectModel3dParams(hvUnionCloud, "num_points", out HTuple hvPointNum);
                            if (hvPointNum.I == 0)
                            {
                                continue;
                            }
                            HOperatorSet.SmoothObjectModel3d(hvUnionCloud, "mls", "mls_kNN", 199, out HTuple hvSmthCloud);
                            HOperatorSet.AffineTransObjectModel3d(hvSmthCloud, hvPoseMat, out HTuple hvAffdCloud);
                            //计算高度
                            HOperatorSet.GetObjectModel3dParams(hvAffdCloud, "point_coord_z", out HTuple hvValueZ);
                            HOperatorSet.TupleSort(hvValueZ, out HTuple hvSortedZ);
                            HOperatorSet.TupleInverse(hvSortedZ, out hvSortedZ);
                            HOperatorSet.TupleMean(hvSortedZ, out HTuple hvAvgZ);
                            HOperatorSet.TupleMin(hvSortedZ, out HTuple hvMinZ);
                            HOperatorSet.TupleMax(hvSortedZ, out HTuple hvMaxZ);
                            HOperatorSet.TupleLessElem(hvSortedZ, hvAvgZ, out HTuple hvMark0);
                            HOperatorSet.TupleFindFirst(hvMark0, 1, out HTuple hvB0);

                            HTuple hvTemp = 0;
                            if (hvB0 != -1)
                            {
                                HOperatorSet.TupleSelectRange(hvSortedZ, 0, hvB0, out HTuple hvTop0);
                                HOperatorSet.TupleMean(hvTop0, out HTuple hvDiv0);
                                HOperatorSet.TupleLessElem(hvTop0, hvDiv0, out HTuple hvMark1);
                                HOperatorSet.TupleFindFirst(hvMark1, 1, out HTuple hvB1);
                                HOperatorSet.TupleSelectRange(hvTop0, 0, hvB1, out HTuple hvTop2);
                                HOperatorSet.TupleMean(hvTop2, out HTuple hvDiv1);
                                hvTemp = hvDiv1 - hvMinZ;
                            }
                            hvDepthFeature = hvDepthFeature.TupleConcat(hvTemp);

                            hvDefectCloud.Dispose(); hvPlane.Dispose(); hvPose.Dispose(); hvNormal.Dispose();
                            hvConnCloud.Dispose(); hvSeleCloud.Dispose(); hvUnionCloud.Dispose(); hvPointNum.Dispose();
                            hvSmthCloud.Dispose(); hvAffdCloud.Dispose(); hvValueZ.Dispose(); hvSortedZ.Dispose();
                        }

                    }

                    if (hvOuterRadius.Length > 0)
                        defect.DiameterFeature = (hvOuterRadius.D * 2) * _measureParam.IntervalX;
                    if (hvArea.Length > 0)
                        defect.AreaFeature = hvArea.D * _measureParam.IntervalX * _measureParam.IntervalY;
                    if (hvDepthFeature.Length > 0)
                        defect.DepthFeature = hvDepthFeature.TupleMax().D * _measureParam.IntervalZ * _measureParam.RefractiveIndex;

                    List<Polygon> polygons = new List<Polygon>();
                    if (nativeBox.Segmentation.FloatData != IntPtr.Zero)
                    {
                        HOperatorSet.CountObj(hoDefectMasks, out HTuple hvNum);
                        for (int regionIdx = 0; regionIdx < hvNum; regionIdx++)
                        {
                            HOperatorSet.SelectObj(hoDefectMasks, out hoTmpRegion, regionIdx + 1);

                            //获取缺陷轮廓
                            Polygon polygon = new Polygon(hoTmpRegion);
                            polygons.Add(polygon);
                        }
                    }
                    defect.DefectPolygons = polygons;

                    defects.Add(defect);
                }
                //HOperatorSet.GetImageSize(hoGrayImage, out HTuple hvWidth, out HTuple hvHeight);
                //HOperatorSet.RegionToBin(hoMaskOut, out HObject hoDefectBin, 255, 0, hvWidth, hvHeight);
                //HOperatorSet.WriteImage(hoDefectBin, "png", 0, "D:\\dataset\\6_KBTDispensing\\00_20251106\\output\\02.png");
                //hoMaskOut.Dispose(); hoDefectBin.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                hoTmp?.Dispose(); hoDefectMask?.Dispose(); hoDefectMasks?.Dispose(); hoDefectMaskSelect?.Dispose(); hoDefectZ.Dispose();
                hoTmpRegion?.Dispose();
            }

            return defects;
        }


        /// <summary>
        /// 缺陷检测过程
        /// </summary>
        private List<DefectResult> DetectionProcess(HObject hoGrayImage, HObject hoHeightImage,
                                                    HObject hoGrayScaled, HObject hoHeightScaled, HTuple hvScaleX, HTuple hvScaleY)
        {
            IntPtr grayImagePtr = IntPtr.Zero;
            IntPtr heightImagePtr = IntPtr.Zero;
            HOperatorSet.GetImagePointer1(hoGrayImage, out HTuple hvGPointer, out HTuple hvGType, out HTuple hvGWidth, out HTuple hvGHeight);
            HOperatorSet.GetImagePointer1(hoHeightImage, out HTuple hvHPointer, out HTuple hvHType, out HTuple hvHWidth, out HTuple hvHHeight);

            int inGw = hvGWidth;
            int inGh = hvGHeight;
            int inGc = 1;
            grayImagePtr = hvGPointer;

            int inDw = hvHWidth;
            int inDh = hvHHeight;
            int inDc = 1;
            heightImagePtr = hvHPointer;

            int inGtype = GetCvTypeFromHalconType(hvGType);
            int inDtype = GetCvTypeFromHalconType(hvHType);

            IntPtr objInfo = IntPtr.Zero;
            int objNum = 0;

            _sdkGate.Wait();
            try
            {
                int state = InstanceSegSDK.Pipeline(_deepLearningHandle,
                                                    grayImagePtr, inGw, inGh, inGc, inGtype,
                                                    heightImagePtr, inDw, inDh, inDc, inDtype,
                                                    out objInfo, out objNum);

                List<DefectResult> defects = PourObjectInfo(objInfo, objNum, hoGrayScaled, hoHeightScaled, hvScaleX.D, hvScaleY.D);

                state = InstanceSegSDK.CleanUpResult(_deepLearningHandle, ref objInfo);
                objInfo = IntPtr.Zero;

                return defects;
            }
            finally
            {
                if (objInfo != IntPtr.Zero)
                    InstanceSegSDK.CleanUpResult(_deepLearningHandle, ref objInfo);

                _sdkGate.Release();
            }
        }


        /// <summary>
        /// 测量过程
        /// </summary>
        /// <param name="grayDate">输入的灰度图数据</param>
        /// <param name="heightData">输入深度图数据</param>
        /// <param name="param">测量配置参数</param>
        /// <returns>result</returns>
        public async Task<int> Process(List<float[]> grayDate, List<float[]> heightDataL0, List<float[]> heightDataL1,
                                       KBTDispensing_MeasureParam param)
        {
            _disposed = false;

            try
            {
                _measureParam = param.DeepCopy();

                HObject hoGrayImage, hoHeightImageL0, hoHeightImageL1, hoGlueHeight;
                HObject? hoValidMask;

                HOperatorSet.GenEmptyObj(out hoValidMask);

                int statusGrayDate, statusHeightDataL0, statusHeightDataL1;
                statusGrayDate = ConvertListToHObject(grayDate, ImageType.Gray, out hoGrayImage);
                statusHeightDataL0 = ConvertListToHObject(heightDataL0, ImageType.Depth, out hoHeightImageL0);
                statusHeightDataL1 = ConvertListToHObject(heightDataL1, ImageType.Depth, out hoHeightImageL1);

                SideResult result = new SideResult();
                if (statusGrayDate == 0 && statusHeightDataL0 == 0 && statusHeightDataL1 == 0)
                {
                    bool fastModel = true;
                    HTuple hvScaleX, hvScaleY;
                    if (fastModel)
                    {
                        if (_measureParam.IntervalX > _measureParam.IntervalY)
                        {
                            hvScaleX = 1;
                            hvScaleY = _measureParam.IntervalY / _measureParam.IntervalX;
                        }
                        else
                        {
                            hvScaleX = _measureParam.IntervalX / _measureParam.IntervalY;
                            hvScaleY = 1;
                        }
                    }
                    else
                    {
                        if (_measureParam.IntervalX < _measureParam.IntervalY)
                        {
                            hvScaleX = 1;
                            hvScaleY = _measureParam.IntervalY / _measureParam.IntervalX;
                        }
                        else
                        {
                            hvScaleX = _measureParam.IntervalX / _measureParam.IntervalY;
                            hvScaleY = 1;
                        }
                    }
                    _measureParam.IntervalX = _measureParam.IntervalX / hvScaleX;
                    _measureParam.IntervalY = _measureParam.IntervalY / hvScaleY;
                    _measureParam.IntervalZ = _measureParam.IntervalZ / (_measureParam.IntervalZ * 10);

                    HOperatorSet.SubImage(hoHeightImageL1, hoHeightImageL0, out hoGlueHeight, 1, 0);

                    HObject hoGrayScaled, hoHeightL0Scaled, hoHeightL1Scaled, hoGlueHeightScaled;
                    HOperatorSet.ZoomImageFactor(hoGrayImage, out hoGrayScaled, hvScaleX, hvScaleY, "bilinear");
                    HOperatorSet.ZoomImageFactor(hoHeightImageL0, out hoHeightL0Scaled, hvScaleX, hvScaleY, "nearest_neighbor");
                    HOperatorSet.ZoomImageFactor(hoHeightImageL1, out hoHeightL1Scaled, hvScaleX, hvScaleY, "nearest_neighbor");
                    HOperatorSet.ZoomImageFactor(hoGlueHeight, out hoGlueHeightScaled, hvScaleX, hvScaleY, "nearest_neighbor");

                    // 去除深度图异常区域
                    //hoGlueHeightScaled = GetDepthValidMask(hoGlueHeightScaled, out hoValidMask);
                    hoHeightL0Scaled = GetDepthValidMask(hoHeightL0Scaled, out _hoValidMaskL0);
                    hoHeightL1Scaled = GetDepthValidMask(hoHeightL1Scaled, out _hoValidMaskL1);
                    HOperatorSet.Intersection(_hoValidMaskL0, _hoValidMaskL1, out _hoGlueValidMask);
                    HOperatorSet.ReduceDomain(hoGlueHeightScaled, _hoGlueValidMask, out hoGlueHeightScaled);

                    // 胶宽、胶高、平面度测量
                    var tMeasure = Task.Run(() =>
                    {
                        try
                        {
                            using (var dh = new HDevDisposeHelper())
                            {
                                var measure = MeasureProcess(hoGrayScaled, hoHeightL0Scaled, hoHeightL1Scaled, hoGlueHeightScaled, hvScaleX, hvScaleY);

                                return measure;
                            }
                        }
                        catch
                        {
                            return new SideResult();
                        }
                    });

                    // 缺陷检测
                    var tDetect = Task.Run(() =>
                    {
                        try
                        {
                            using (var dh = new HDevDisposeHelper())
                            {
                                var detect = DetectionProcess(hoGrayImage, hoHeightImageL1, hoGrayScaled, hoHeightL1Scaled, hvScaleX, hvScaleY);
                                return detect;
                            }
                        }
                        catch
                        {
                            return new List<DefectResult>();
                        }

                    });

                    Task.WhenAll(tMeasure, tDetect);

                    SideResult measureResult = tMeasure.Result;
                    measureResult.GrayImage = HobjectToMat(hoGrayScaled, ImageType.Gray);
                    measureResult.HeightImage = HobjectToMat(hoHeightL1Scaled, ImageType.Depth);
                    measureResult.Defects = tDetect.Result;

                    _imageData.Add(measureResult);

                    hoGrayImage.Dispose(); hoHeightImageL0.Dispose(); hoHeightImageL1.Dispose(); hoGlueHeight.Dispose();
                    hoGrayScaled.Dispose(); hoHeightL0Scaled.Dispose(); hoHeightL1Scaled.Dispose(); hoGlueHeightScaled.Dispose();
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }

            return 0;
        }


        public static void DrawRotatedRect(Mat img, Point2f center, Size2f size, float angleDeg, Scalar color, int thickness = 2)
        {
            var rRect = new RotatedRect(center, size, angleDeg);

            Point2f[] verticesF = Cv2.BoxPoints(rRect);

            Point[] vertices = Array.ConvertAll(verticesF, p => new Point((int)Math.Round(p.X), (int)Math.Round(p.Y)));

            Cv2.Polylines(
                img,
                new[] { vertices },
                isClosed: true,
                color: color,
                thickness: thickness,
                lineType: LineTypes.AntiAlias
            );
        }


        /// <summary>
        /// 绘制结果
        /// </summary>
        public int CvDrawResult(KBTDispensing_MeasureResult measureResult, out Mat grayImage, out Mat HeightImage, bool showGuides = false)
        {
            grayImage = measureResult.GrayImage.Clone();
            HeightImage = measureResult.HeightImage.Clone();

            try
            {
                double scale = _measureParam.VisualScaleFactor;

                Cv2.CvtColor(grayImage, grayImage, ColorConversionCodes.GRAY2BGR);

                double defectSizeMax = 0;
                double defectNumTotal = 0;
                double defectdepthMax = 0;
                for (int sideId = 0; sideId < measureResult.SideResults.Count; sideId++)
                {
                    // 绘制测量结果
                    SideResult sideResult = measureResult.SideResults[sideId];
                    for (int i = 0; i < sideResult.GlueWidthMaxPointX.Length; i++)
                    {
                        int x = (int)sideResult.GlueWidthMaxPointX[i];
                        int y = (int)sideResult.GlueWidthMaxPointY[i];
                        float angleDeg = 90 - (float)sideResult.GlueWidthMaxAngle[i];
                        float w, h;
                        if (sideId == 0 || sideId == 2)
                        {
                            w = (float)(sideResult.GlueWidthPixelMax);
                            h = (float)(20 * scale);
                        }
                        else
                        {
                            w = (float)(20 * scale);
                            h = (float)(sideResult.GlueWidthPixelMax);
                        }
                        Cv2.Circle(grayImage, x, y, (int)(10 * scale + 1), new Scalar(0, 0, 255), -1);
                        // Cv2.Rectangle(grayImage, new Point(x - w * 0.5, y - h * 0.5), new Point(x + w * 0.5, y + h * 0.5), new Scalar(0, 0, 255), 2);
                        DrawRotatedRect(grayImage, new Point2f((float)(x), (float)(y)), new Size2f(w, h), angleDeg, new Scalar(0, 0, 255), (int)(2 * scale + 1));
                        string text = $"{sideId}-W-Max:{sideResult.GlueWidthMax:F1}um";
                        if (sideId == 2)
                        {
                            Cv2.PutText(grayImage, text, new OpenCvSharp.Point(x - 3500 * scale, y - 250 * scale), HersheyFonts.HersheyDuplex, (int)(12 * scale + 1), new Scalar(0, 0, 255), (int)(18 * scale + 1));
                        }
                        else
                        {
                            Cv2.PutText(grayImage, text, new OpenCvSharp.Point(x, y - 250 * scale), HersheyFonts.HersheyDuplex, (int)(12 * scale + 1), new Scalar(0, 0, 255), (int)(18 * scale + 1));
                        }

                    }
                    for (int i = 0; i < sideResult.GlueWidthMinPointX.Length; i++)
                    {
                        int x = (int)sideResult.GlueWidthMinPointX[i];
                        int y = (int)sideResult.GlueWidthMinPointY[i];
                        float angleDeg = 90 - (float)sideResult.GlueWidthMinAngle[i];
                        float w, h;
                        if (sideId == 0 || sideId == 2)
                        {
                            w = (float)(sideResult.GlueWidthPixelMin);
                            h = (float)(20 * scale);
                        }
                        else
                        {
                            w = (float)(20 * scale);
                            h = (float)(sideResult.GlueWidthPixelMin);
                        }
                        Cv2.Circle(grayImage, x, y, (int)(10 * scale + 1), new Scalar(0, 0, 255), -1);
                        // Cv2.Rectangle(grayImage, new Point(x - w * 0.5, y - h * 0.5), new Point(x + w * 0.5, y + h * 0.5), new Scalar(0, 0, 255), 2);
                        DrawRotatedRect(grayImage, new Point2f((float)(x), (float)(y)), new Size2f(w, h), angleDeg, new Scalar(0, 0, 255), (int)(2 * scale + 1));
                        string text = $"{sideId}-W-Min:{sideResult.GlueWidthMin:F1}um";
                        if (sideId == 2)
                        {
                            Cv2.PutText(grayImage, text, new OpenCvSharp.Point(x - 3500 * scale, y - 250 * scale), HersheyFonts.HersheyDuplex, (int)(12 * scale + 1), new Scalar(0, 0, 255), (int)(18 * scale + 1));
                        }
                        else
                        {
                            Cv2.PutText(grayImage, text, new OpenCvSharp.Point(x, y - 250 * scale), HersheyFonts.HersheyDuplex, (int)(12 * scale + 1), new Scalar(0, 0, 255), (int)(18 * scale + 1));
                        }

                    }

                    for (int i = 0; i < sideResult.GlueThicknessMaxPointX.Length; i++)
                    {
                        int x = (int)sideResult.GlueThicknessMaxPointX[i];
                        int y = (int)sideResult.GlueThicknessMaxPointY[i];
                        Cv2.Circle(grayImage, x, y, (int)(10 * scale + 1), new Scalar(255, 0, 255), -1);
                        string text = $"{sideId}-T-Max:{sideResult.GlueThicknessMax:F1}um";
                        if (sideId == 2)
                        {
                            Cv2.PutText(grayImage, text, new OpenCvSharp.Point(x - 3500 * scale, y + 250 * scale), HersheyFonts.HersheyDuplex, (int)(12 * scale + 1), new Scalar(255, 0, 255), (int)(18 * scale + 1));
                        }
                        else
                        {
                            Cv2.PutText(grayImage, text, new OpenCvSharp.Point(x, y + 250 * scale), HersheyFonts.HersheyDuplex, (int)(12 * scale + 1), new Scalar(255, 0, 255), (int)(18 * scale + 1));
                        }

                    }
                    for (int i = 0; i < sideResult.GlueThicknessMinPointX.Length; i++)
                    {
                        int x = (int)sideResult.GlueThicknessMinPointX[i];
                        int y = (int)sideResult.GlueThicknessMinPointY[i];
                        Cv2.Circle(grayImage, x, y, (int)(10 * scale + 1), new Scalar(255, 0, 255), -1);
                        string text = $"{sideId}-T-Min:{sideResult.GlueThicknessMin:F1}um";
                        if (sideId == 2)
                        {
                            Cv2.PutText(grayImage, text, new OpenCvSharp.Point(x - 3500 * scale, y + 250 * scale), HersheyFonts.HersheyDuplex, (int)(12 * scale + 1), new Scalar(255, 0, 255), (int)(18 * scale + 1));
                        }
                        else
                        {
                            Cv2.PutText(grayImage, text, new OpenCvSharp.Point(x, y + 250 * scale), HersheyFonts.HersheyDuplex, (int)(12 * scale + 1), new Scalar(255, 0, 255), (int)(18 * scale + 1));
                        }
                    }

                    int tmpV = 0;
                    for (int i = 0; i < sideResult.MeasurePointXList.Length; i++)
                    {
                        int x = (int)sideResult.MeasurePointXList[i];
                        int y = (int)sideResult.MeasurePointYList[i];
                        Cv2.Circle(grayImage, x, y, (int)(2 * scale + 1), new Scalar(128, 128, 255), -1);

                        //float angleDeg = 90 - (float)sideResult.GlueWidthAngleList[i];
                        //float w, h;
                        //if (sideId == 0 || sideId == 2)
                        //{
                        //    w = (float)sideResult.GlueWidthPixelList[i];
                        //    h = 10;
                        //}
                        //else
                        //{
                        //    w = 10;
                        //    h = (float)sideResult.GlueWidthPixelList[i];
                        //}
                        //DrawRotatedRect(grayImage, new Point2f((float)(x), (float)(y)), new Size2f(w, h), angleDeg, new Scalar(128, 128, 255), 2);

                        // 可视化的采样点
                        if (sideResult.SampleViewIdx[i] == 1)
                        {
                            Cv2.Circle(grayImage, x, y, (int)(2 * scale + 1), new Scalar(255, 128, 128), -1);

                            string text;
                            if (sideResult.GlueThicknessList[i] < 0)
                            {
                                text = $"{sideId}-W:{sideResult.GlueWidthList[i]:F1}um-T:?um";
                            }
                            else
                            {
                                text = $"{sideId}-W:{sideResult.GlueWidthList[i]:F1}um-T:{sideResult.GlueThicknessList[i]:F1}um";
                            }

                            if (sideId == 0 || sideId == 2)
                            {
                                Cv2.PutText(grayImage, text, new OpenCvSharp.Point(x, y - 8 * scale), HersheyFonts.HersheyDuplex, (int)(2 * scale + 1), new Scalar(255, 128, 128), (int)(3 * scale + 1));
                            }
                            else
                            {
                                if (tmpV == 1)
                                {
                                    Cv2.PutText(grayImage, text, new OpenCvSharp.Point(x, y - 50 * scale), HersheyFonts.HersheyDuplex, (int)(2 * scale + 1), new Scalar(255, 128, 128), (int)(3 * scale + 1));
                                    tmpV = 0;
                                }
                                else
                                {
                                    Cv2.PutText(grayImage, text, new OpenCvSharp.Point(x, y + 50 * scale), HersheyFonts.HersheyDuplex, (int)(2 * scale + 1), new Scalar(255, 128, 128), (int)(3 * scale + 1));
                                    tmpV = 1;
                                }

                            }
                        }
                    }

                    // 绘制缺陷
                    int defectNum = sideResult.Defects.Count;
                    for (int i = 0; i < defectNum; i++)
                    {
                        DefectResult defect = measureResult.SideResults[sideId].Defects[i];

                        if (defect.IsOk)
                            continue;

                        if (defect.DiameterFeature > defectSizeMax)
                            defectSizeMax = defect.DiameterFeature;

                        if (defect.DepthFeature > defectdepthMax)
                            defectdepthMax = defect.DepthFeature;

                        if (defect.Left != Single.NegativeInfinity && defect.Top != Single.NegativeInfinity &&
                           defect.Right != Single.NegativeInfinity && defect.Bottom != Single.NegativeInfinity &&
                           defect.InstanceId != -1 && defect.Confidence != Single.NegativeInfinity)
                        {
                            Cv2.Rectangle(grayImage, new OpenCvSharp.Point((int)defect.Left, (int)defect.Top),
                                                 new OpenCvSharp.Point((int)defect.Right, (int)defect.Bottom), new Scalar(255, 0, 0), (int)(3 * scale + 1));

                            for (int j = 0; j < defect.DefectPolygons.Count; j++)
                            {
                                if (defect.DefectPolygons[j].Contours.Length > 0)
                                {
                                    Cv2.DrawContours(grayImage, defect.DefectPolygons[j].Contours, -1, new Scalar(0, 255, 255), 1);
                                }
                            }

                            string text = $"ID:{defect.InstanceId}";
                            //Cv2.PutText(image, text, new OpenCvSharp.Point(defect.Left, defect.Top - 8), HersheyFonts.HersheyDuplex, 3, new Scalar(255, 0, 0), 6);
                        }

                        defectNumTotal++;
                    }
                }

                string glueWidthMax = $"GlobelWidthMax:{measureResult.GlueWidthMax:F3}um";
                string glueWidthMin = $"GlobelWidthMin:{measureResult.GlueWidthMin:F3}um";
                string glueWidthAvg = $"GlobelWidthAvg:{measureResult.GlueWidthAvg:F3}um";

                string glueThicknessMax = $"GlobelThicknessMax:{measureResult.GlueThicknessMax:F3}um";
                string glueThicknessMin = $"GlobelThicknessMin:{measureResult.GlueThicknessMin:F3}um";
                string glueThicknessAvg = $"GlobelThicknessAvg:{measureResult.GlueThicknessAvg:F3}um";

                string glueFlatness = $"GlobelGlueFlatness:{measureResult.GlueFlatness:F3}um";
                //string frameFlatness = $"GlobelFrameFlatness:{measureResult.FrameFlatness:F3}um";
                //string frameFlatness = $"GlobelFrameFlatness:-um";

                string globelDefectNumber = $"GlobelDefectNumber:{defectNumTotal}";
                string globelDefectSizeMax = $"GlobelDefectSizeMax:{defectSizeMax:F3}um";
                string globelDefectdepthMax = $"GlobelDefectHeightDiffMax:{defectdepthMax:F3}um";

                string glueWidthConsistency = $"GlueWidthConsistency:{(measureResult.GlueWidthMax - measureResult.GlueWidthMin):F3}um";
                string glueThicknessConsistency = $"GlueThicknessConsistency:{(measureResult.GlueThicknessMax - measureResult.GlueThicknessMin):F3}um";

                double drawScale = (5 / _measureParam.IntervalY) * _measureParam.VisualScaleFactor;
                Cv2.PutText(grayImage, glueWidthMax, new OpenCvSharp.Point(4096 * drawScale, (4096 + 800) * drawScale), HersheyFonts.HersheyDuplex, 24 * drawScale, new Scalar(50, 250, 100), (int)(40 * drawScale));
                Cv2.PutText(grayImage, glueWidthMin, new OpenCvSharp.Point(4096 * drawScale, (4096 + 1600) * drawScale), HersheyFonts.HersheyDuplex, 24 * drawScale, new Scalar(50, 250, 100), (int)(40 * drawScale));
                Cv2.PutText(grayImage, glueWidthAvg, new OpenCvSharp.Point(4096 * drawScale, (4096 + 2400) * drawScale), HersheyFonts.HersheyDuplex, 24 * drawScale, new Scalar(50, 250, 100), (int)(40 * drawScale));
                Cv2.PutText(grayImage, glueThicknessMax, new OpenCvSharp.Point(4096 * drawScale, (4096 + 4000) * drawScale), HersheyFonts.HersheyDuplex, 24 * drawScale, new Scalar(50, 250, 100), (int)(40 * drawScale));
                Cv2.PutText(grayImage, glueThicknessMin, new OpenCvSharp.Point(4096 * drawScale, (4096 + 4800) * drawScale), HersheyFonts.HersheyDuplex, 24 * drawScale, new Scalar(50, 250, 100), (int)(40 * drawScale));
                Cv2.PutText(grayImage, glueThicknessAvg, new OpenCvSharp.Point(4096 * drawScale, (4096 + 5600) * drawScale), HersheyFonts.HersheyDuplex, 24 * drawScale, new Scalar(50, 250, 100), (int)(40 * drawScale));

                Cv2.PutText(grayImage, glueFlatness, new OpenCvSharp.Point(4096 * drawScale, (4096 + 7200) * drawScale), HersheyFonts.HersheyDuplex, 24 * drawScale, new Scalar(50, 250, 100), (int)(40 * drawScale));
                //Cv2.PutText(grayImage, frameFlatness, new OpenCvSharp.Point(4096 * drawScale, (4096 + 8000) * drawScale), HersheyFonts.HersheyDuplex, 24 * drawScale, new Scalar(50, 250, 100), (int)(40 * drawScale));

                Cv2.PutText(grayImage, globelDefectNumber, new OpenCvSharp.Point(4096 * drawScale, (4096 + 8800) * drawScale), HersheyFonts.HersheyDuplex, 24 * drawScale, new Scalar(50, 250, 100), (int)(40 * drawScale));
                Cv2.PutText(grayImage, globelDefectSizeMax, new OpenCvSharp.Point(4096 * drawScale, (4096 + 9600) * drawScale), HersheyFonts.HersheyDuplex, 24 * drawScale, new Scalar(50, 250, 100), (int)(40 * drawScale));
                Cv2.PutText(grayImage, globelDefectdepthMax, new OpenCvSharp.Point(4096 * drawScale, (4096 + 10400) * drawScale), HersheyFonts.HersheyDuplex, 24 * drawScale, new Scalar(50, 250, 100), (int)(40 * drawScale));

                Cv2.PutText(grayImage, glueWidthConsistency, new OpenCvSharp.Point(4096 * drawScale, (4096 + 12000) * drawScale), HersheyFonts.HersheyDuplex, 24 * drawScale, new Scalar(50, 250, 100), (int)(40 * drawScale));
                Cv2.PutText(grayImage, glueThicknessConsistency, new OpenCvSharp.Point(4096 * drawScale, (4096 + 12800) * drawScale), HersheyFonts.HersheyDuplex, 24 * drawScale, new Scalar(50, 250, 100), (int)(40 * drawScale));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);

                grayImage = Mat.Zeros(new OpenCvSharp.Size(128, 128), MatType.CV_8UC3);
                HeightImage = Mat.Zeros(new OpenCvSharp.Size(128, 128), MatType.CV_32FC1);
            }

            return 0;
        }

        public enum Side { Top, Bottom, Left, Right }


        /// <summary>
        /// 逆时针扫描顺序：imgs[0]=左，imgs[1]=下，imgs[2]=右，imgs[3]=上
        /// </summary>
        public int BuildAffineMatrix(int mode, double scale)
        {
            int imageNum = _imageData.Count;

            if (imageNum != 4)
            {
                throw new ArgumentException("需要采集完4张图片才能拼图。");
            }

            int[] W = _imageData.Select(d => d.GrayImage.Width).ToArray();
            int[] H = _imageData.Select(d => d.GrayImage.Height).ToArray();
            int[] L = _imageData.Select((d, i) => Math.Max(W[i], H[i])).ToArray();
            int[] S = _imageData.Select((d, i) => Math.Min(W[i], H[i])).ToArray();

            int leftIdx = 0, bottomIdx = 1, rightIdx = 2, topIdx = 3;

            int T = S.Max();
            int Wout = (int)Math.Floor((Math.Max(L[topIdx], L[bottomIdx]) + T) * scale);
            int Hout = (int)Math.Floor((Math.Max(L[leftIdx], L[rightIdx]) + T) * scale);

            (Side side, bool m, bool anchorIsBottomLeft)[] mapping;
            if (mode == 1)
            {
                mapping = new[]
                {
                    (Side.Left,   true,  false),
                    (Side.Bottom, true,  false),
                    (Side.Right,  true, false),
                    (Side.Top,    true, false),
                };
            }
            else if (mode == 2)
            {
                mapping = new[]
                {
                    (Side.Left,   false, true),
                    (Side.Bottom, false, true),
                    (Side.Right,  false,  true),
                    (Side.Top,    false,  true),
                };
            }
            else
            {
                throw new ArgumentException("拼图 mode 只能是 1 或 2。");
            }

            int[] order = new[] { leftIdx, bottomIdx, rightIdx, topIdx };

            List<double> globelGlueWidthMax = new List<double>();
            List<double> globelGlueWidthMin = new List<double>();
            List<double> globelGlueWidthAvg = new List<double>();
            List<double> globelGlueThicknessMax = new List<double>();
            List<double> globelGlueThicknessMin = new List<double>();
            List<double> globelGlueThicknessAvg = new List<double>();
            List<double> globelGlueFlatness = new List<double>();
            List<double> globelFrameFlatness = new List<double>();

            Mat canvasGray = new Mat(new Size(Wout, Hout), MatType.CV_8UC1, Scalar.Black);
            Mat canvasHeight = new Mat(new Size(Wout, Hout), MatType.CV_32FC1, new Scalar(8888880));

            for (int k = 0; k < 4; ++k)
            {
                int i = order[k];

                var (side, m, anchorBL) = mapping[i];

                using Mat M = BuildForSide(_imageData[i].GrayImage, side, m, anchorBL, Wout, Hout, scale);

                Cv2.WarpAffine(_imageData[i].GrayImage, canvasGray, M, new Size(Wout, Hout), InterpolationFlags.Linear, BorderTypes.Transparent);
                Cv2.WarpAffine(_imageData[i].HeightImage, canvasHeight, M, new Size(Wout, Hout), InterpolationFlags.Nearest, BorderTypes.Transparent);
                _imageData[i].GrayImage?.Dispose();
                _imageData[i].HeightImage?.Dispose();

                // 测量采样点坐标映射
                double m00 = M.At<double>(0, 0), m01 = M.At<double>(0, 1), m02 = M.At<double>(0, 2);
                double m10 = M.At<double>(1, 0), m11 = M.At<double>(1, 1), m12 = M.At<double>(1, 2);
                for (int idx = 0; idx < _imageData[i].MeasurePointXList.Length; idx++)
                {
                    double x = _imageData[i].MeasurePointXList[idx];
                    double y = _imageData[i].MeasurePointYList[idx];
                    double xp = m00 * x + m01 * y + m02;
                    double yp = m10 * x + m11 * y + m12;
                    _imageData[i].MeasurePointXList[idx] = xp;
                    _imageData[i].MeasurePointYList[idx] = yp;
                }

                globelGlueWidthMax.Add(_imageData[i].GlueWidthMax);
                globelGlueWidthMin.Add(_imageData[i].GlueWidthMin);
                globelGlueWidthAvg.Add(_imageData[i].GlueWidthAvg);
                for (int idx = 0; idx < _imageData[i].GlueWidthMaxPointX.Length; idx++)
                {
                    double x = _imageData[i].GlueWidthMaxPointX[idx];
                    double y = _imageData[i].GlueWidthMaxPointY[idx];
                    double xp = m00 * x + m01 * y + m02;
                    double yp = m10 * x + m11 * y + m12;
                    _imageData[i].GlueWidthMaxPointX[idx] = xp;
                    _imageData[i].GlueWidthMaxPointY[idx] = yp;
                }
                _imageData[i].GlueWidthPixelMax = _imageData[i].GlueWidthPixelMax * scale;

                for (int idx = 0; idx < _imageData[i].GlueWidthMinPointX.Length; idx++)
                {
                    double x = _imageData[i].GlueWidthMinPointX[idx];
                    double y = _imageData[i].GlueWidthMinPointY[idx];
                    double xp = m00 * x + m01 * y + m02;
                    double yp = m10 * x + m11 * y + m12;
                    _imageData[i].GlueWidthMinPointX[idx] = xp;
                    _imageData[i].GlueWidthMinPointY[idx] = yp;
                }
                _imageData[i].GlueWidthPixelMin = _imageData[i].GlueWidthPixelMin * scale;

                globelGlueThicknessMax.Add(_imageData[i].GlueThicknessMax);
                globelGlueThicknessMin.Add(_imageData[i].GlueThicknessMin);
                globelGlueThicknessAvg.Add(_imageData[i].GlueThicknessAvg);
                for (int idx = 0; idx < _imageData[i].GlueThicknessMaxPointX.Length; idx++)
                {
                    double x = _imageData[i].GlueThicknessMaxPointX[idx];
                    double y = _imageData[i].GlueThicknessMaxPointY[idx];
                    double xp = m00 * x + m01 * y + m02;
                    double yp = m10 * x + m11 * y + m12;
                    _imageData[i].GlueThicknessMaxPointX[idx] = xp;
                    _imageData[i].GlueThicknessMaxPointY[idx] = yp;
                }
                for (int idx = 0; idx < _imageData[i].GlueThicknessMinPointX.Length; idx++)
                {
                    double x = _imageData[i].GlueThicknessMinPointX[idx];
                    double y = _imageData[i].GlueThicknessMinPointY[idx];
                    double xp = m00 * x + m01 * y + m02;
                    double yp = m10 * x + m11 * y + m12;
                    _imageData[i].GlueThicknessMinPointX[idx] = xp;
                    _imageData[i].GlueThicknessMinPointY[idx] = yp;
                }

                globelGlueFlatness.Add(_imageData[i].GlueFlatness);
                globelFrameFlatness.Add(_imageData[i].FrameFlatness);

                // 缺陷坐标映射
                for (int idx = 0; idx < _imageData[i].Defects.Count; idx++)
                {
                    DefectResult defect = _imageData[i].Defects[idx];

                    double tmpLeft = m00 * defect.Left + m01 * defect.Top + m02;
                    double tmpTop = m10 * defect.Left + m11 * defect.Top + m12;
                    double tmpRight = m00 * defect.Right + m01 * defect.Bottom + m02;
                    double tmpBottom = m10 * defect.Right + m11 * defect.Bottom + m12;

                    defect.Left = Math.Min(tmpLeft, tmpRight);
                    defect.Top = Math.Min(tmpTop, tmpBottom);
                    defect.Right = Math.Max(tmpLeft, tmpRight);
                    defect.Bottom = Math.Max(tmpTop, tmpBottom);

                    for (int pIdx = 0; pIdx < defect.DefectPolygons.Count; pIdx++)
                    {
                        Polygon polygon = defect.DefectPolygons[pIdx];

                        foreach (var contour in polygon.Contours)
                        {
                            for (int c_i = 0; c_i < contour.Length; c_i++)
                            {
                                double x = contour[c_i].X;
                                double y = contour[c_i].Y;

                                double xp = m00 * x + m01 * y + m02;
                                double yp = m10 * x + m11 * y + m12;

                                contour[c_i].X = (int)Math.Round(xp);
                                contour[c_i].Y = (int)Math.Round(yp);
                            }
                        }

                        defect.DefectPolygons[pIdx] = polygon;
                    }

                    _imageData[i].Defects[idx] = defect;
                }

                _measureResult.SideResults.Add(_imageData[i]);
            }

            _measureResult.GlueWidthMax = globelGlueWidthMax.Max();
            _measureResult.GlueWidthMin = globelGlueWidthMin.Min();
            _measureResult.GlueWidthAvg = globelGlueWidthAvg.Average();

            _measureResult.GlueThicknessMax = globelGlueThicknessMax.Where(x => x > 0).DefaultIfEmpty(0).Max();
            _measureResult.GlueThicknessMin = globelGlueThicknessMin.Where(x => x > 0).DefaultIfEmpty(0).Min();
            _measureResult.GlueThicknessAvg = globelGlueThicknessAvg.Where(x => x > 0).DefaultIfEmpty(0).Average();
            //_measureResult.GlueFlatness = globelGlueFlatness.Average();
            //_measureResult.FrameFlatness = globelFrameFlatness.Average();
            //_measureResult.GlueFlatness = globelGlueFlatness.Max();
            _measureResult.GlueFlatness = _measureResult.GlueThicknessMax - _measureResult.GlueThicknessMin;
            _measureResult.FrameFlatness = globelFrameFlatness.Max();

            _measureResult.GrayImage = canvasGray;
            _measureResult.HeightImage = canvasHeight;

            return 0;
        }


        public int BuildAffineMatrix2(double scale)
        {
            int imageNum = _imageData.Count;

            if (imageNum != 4)
            {
                throw new ArgumentException("需要采集完4张图片才能拼图。");
            }

            int[] W = _imageData.Select(d => d.GrayImage.Width).ToArray();
            int[] H = _imageData.Select(d => d.GrayImage.Height).ToArray();
            int[] L = _imageData.Select((d, i) => Math.Max(W[i], H[i])).ToArray();
            int[] S = _imageData.Select((d, i) => Math.Min(W[i], H[i])).ToArray();

            int leftIdx = 0, bottomIdx = 1, rightIdx = 2, topIdx = 3;

            int Wout = (int)Math.Floor((Math.Max(L[topIdx], L[bottomIdx])) * scale);
            int Hout = (int)Math.Floor((Math.Max(L[leftIdx], L[rightIdx])) * scale);

            Side[] mapping;

            mapping = new[]
                {
                    Side.Left,
                    Side.Bottom,
                    Side.Right,
                    Side.Top,
                };

            int[] order = new[] { leftIdx, bottomIdx, rightIdx, topIdx };

            List<double> globelGlueWidthMax = new List<double>();
            List<double> globelGlueWidthMin = new List<double>();
            List<double> globelGlueWidthAvg = new List<double>();
            List<double> globelGlueThicknessMax = new List<double>();
            List<double> globelGlueThicknessMin = new List<double>();
            List<double> globelGlueThicknessAvg = new List<double>();
            List<double> globelGlueFlatness = new List<double>();
            List<double> globelFrameFlatness = new List<double>();

            Mat canvasGray = new Mat(new Size(Wout, Hout), MatType.CV_8UC1, Scalar.Black);
            Mat canvasHeight = new Mat(new Size(Wout, Hout), MatType.CV_32FC1, new Scalar(8888880));

            for (int k = 0; k < 4; ++k)
            {
                int i = order[k];

                var side = mapping[i];

                using Mat M = BuildForSide2(_imageData[i].GrayImage, side, Wout, Hout, scale);

                Cv2.WarpAffine(_imageData[i].GrayImage, canvasGray, M, new Size(Wout, Hout), InterpolationFlags.Linear, BorderTypes.Transparent);
                Cv2.WarpAffine(_imageData[i].HeightImage, canvasHeight, M, new Size(Wout, Hout), InterpolationFlags.Nearest, BorderTypes.Transparent);
                _imageData[i].GrayImage?.Dispose();
                _imageData[i].HeightImage?.Dispose();

                // 测量采样点坐标映射
                double m00 = M.At<double>(0, 0), m01 = M.At<double>(0, 1), m02 = M.At<double>(0, 2);
                double m10 = M.At<double>(1, 0), m11 = M.At<double>(1, 1), m12 = M.At<double>(1, 2);
                for (int idx = 0; idx < _imageData[i].MeasurePointXList.Length; idx++)
                {
                    double x = _imageData[i].MeasurePointXList[idx];
                    double y = _imageData[i].MeasurePointYList[idx];
                    double xp = m00 * x + m01 * y + m02;
                    double yp = m10 * x + m11 * y + m12;
                    _imageData[i].MeasurePointXList[idx] = xp;
                    _imageData[i].MeasurePointYList[idx] = yp;
                }

                globelGlueWidthMax.Add(_imageData[i].GlueWidthMax);
                globelGlueWidthMin.Add(_imageData[i].GlueWidthMin);
                globelGlueWidthAvg.Add(_imageData[i].GlueWidthAvg);
                for (int idx = 0; idx < _imageData[i].GlueWidthMaxPointX.Length; idx++)
                {
                    double x = _imageData[i].GlueWidthMaxPointX[idx];
                    double y = _imageData[i].GlueWidthMaxPointY[idx];
                    double xp = m00 * x + m01 * y + m02;
                    double yp = m10 * x + m11 * y + m12;
                    _imageData[i].GlueWidthMaxPointX[idx] = xp;
                    _imageData[i].GlueWidthMaxPointY[idx] = yp;
                }
                _imageData[i].GlueWidthPixelMax = _imageData[i].GlueWidthPixelMax * scale;

                for (int idx = 0; idx < _imageData[i].GlueWidthMinPointX.Length; idx++)
                {
                    double x = _imageData[i].GlueWidthMinPointX[idx];
                    double y = _imageData[i].GlueWidthMinPointY[idx];
                    double xp = m00 * x + m01 * y + m02;
                    double yp = m10 * x + m11 * y + m12;
                    _imageData[i].GlueWidthMinPointX[idx] = xp;
                    _imageData[i].GlueWidthMinPointY[idx] = yp;
                }
                _imageData[i].GlueWidthPixelMax = _imageData[i].GlueWidthPixelMax * scale;

                globelGlueThicknessMax.Add(_imageData[i].GlueThicknessMax);
                globelGlueThicknessMin.Add(_imageData[i].GlueThicknessMin);
                globelGlueThicknessAvg.Add(_imageData[i].GlueThicknessAvg);
                for (int idx = 0; idx < _imageData[i].GlueThicknessMaxPointX.Length; idx++)
                {
                    double x = _imageData[i].GlueThicknessMaxPointX[idx];
                    double y = _imageData[i].GlueThicknessMaxPointY[idx];
                    double xp = m00 * x + m01 * y + m02;
                    double yp = m10 * x + m11 * y + m12;
                    _imageData[i].GlueThicknessMaxPointX[idx] = xp;
                    _imageData[i].GlueThicknessMaxPointY[idx] = yp;
                }
                for (int idx = 0; idx < _imageData[i].GlueThicknessMinPointX.Length; idx++)
                {
                    double x = _imageData[i].GlueThicknessMinPointX[idx];
                    double y = _imageData[i].GlueThicknessMinPointY[idx];
                    double xp = m00 * x + m01 * y + m02;
                    double yp = m10 * x + m11 * y + m12;
                    _imageData[i].GlueThicknessMinPointX[idx] = xp;
                    _imageData[i].GlueThicknessMinPointY[idx] = yp;
                }

                globelGlueFlatness.Add(_imageData[i].GlueFlatness);
                globelFrameFlatness.Add(_imageData[i].FrameFlatness);

                // 缺陷坐标映射
                for (int idx = 0; idx < _imageData[i].Defects.Count; idx++)
                {
                    DefectResult defect = _imageData[i].Defects[idx];

                    double tmpLeft = m00 * defect.Left + m01 * defect.Top + m02;
                    double tmpTop = m10 * defect.Left + m11 * defect.Top + m12;
                    double tmpRight = m00 * defect.Right + m01 * defect.Bottom + m02;
                    double tmpBottom = m10 * defect.Right + m11 * defect.Bottom + m12;

                    defect.Left = Math.Min(tmpLeft, tmpRight);
                    defect.Top = Math.Min(tmpTop, tmpBottom);
                    defect.Right = Math.Max(tmpLeft, tmpRight);
                    defect.Bottom = Math.Max(tmpTop, tmpBottom);

                    for (int pIdx = 0; pIdx < defect.DefectPolygons.Count; pIdx++)
                    {
                        Polygon polygon = defect.DefectPolygons[pIdx];

                        foreach (var contour in polygon.Contours)
                        {
                            for (int c_i = 0; c_i < contour.Length; c_i++)
                            {
                                double x = contour[c_i].X;
                                double y = contour[c_i].Y;

                                double xp = m00 * x + m01 * y + m02;
                                double yp = m10 * x + m11 * y + m12;

                                contour[c_i].X = (int)Math.Round(xp);
                                contour[c_i].Y = (int)Math.Round(yp);
                            }
                        }

                        defect.DefectPolygons[pIdx] = polygon;
                    }

                    _imageData[i].Defects[idx] = defect;
                }

                _measureResult.SideResults.Add(_imageData[i]);
            }

            _measureResult.GlueWidthMax = globelGlueWidthMax.Max();
            _measureResult.GlueWidthMin = globelGlueWidthMin.Min();
            _measureResult.GlueWidthAvg = globelGlueWidthAvg.Average();
            _measureResult.GlueThicknessMax = globelGlueThicknessMax.Where(x => x > 0).DefaultIfEmpty(0).Max();
            _measureResult.GlueThicknessMin = globelGlueThicknessMin.Where(x => x > 0).DefaultIfEmpty(0).Min();
            _measureResult.GlueThicknessAvg = globelGlueThicknessAvg.Where(x => x > 0).DefaultIfEmpty(0).Average();
            //_measureResult.GlueFlatness = globelGlueFlatness.Average();
            //_measureResult.FrameFlatness = globelFrameFlatness.Average();
            //_measureResult.GlueFlatness = globelGlueFlatness.Max();
            _measureResult.GlueFlatness = _measureResult.GlueThicknessMax - _measureResult.GlueThicknessMin;
            _measureResult.FrameFlatness = globelFrameFlatness.Max();

            _measureResult.GrayImage = canvasGray;
            _measureResult.HeightImage = canvasHeight;

            return 0;
        }


        /// <summary>
        /// 以三点对应构造仿射矩阵：源条带(s0=锚点，s1沿长轴，s2沿短轴) → 目标边(d0=锚点，d1沿边正/反向，d2厚度方向)
        /// </summary>
        private static Mat BuildForSide(Mat strip, Side side, bool forward, bool anchorIsBottomLeft, int Wout, int Hout, double scale)
        {
            int w = strip.Width;
            int h = strip.Height;
            bool longIsWidth = w >= h;

            int newW = (int)Math.Floor(w * scale);
            int newH = (int)Math.Floor(h * scale);

            Point2f s0, s1, s2;
            if (!anchorIsBottomLeft)
            {
                s0 = new Point2f(0, 0); // 左上
                if (longIsWidth)
                {
                    s1 = new Point2f(w - 1, 0);
                    s2 = new Point2f(0, h - 1);
                }
                else
                {
                    s1 = new Point2f(0, h - 1);
                    s2 = new Point2f(w - 1, 0);
                }
            }
            else
            {
                s0 = new Point2f(0, h - 1); // 左下
                if (longIsWidth)
                {
                    s1 = new Point2f(w - 1, h - 1);
                    s2 = new Point2f(0, 0);
                }
                else
                {
                    s1 = new Point2f(0, 0);
                    s2 = new Point2f(w - 1, h - 1);
                }
            }

            Point2f d0, d1, d2;
            switch (side)
            {
                case Side.Top:
                    if (forward)
                    {
                        d0 = new Point2f(Wout - 1, 0);
                        d1 = new Point2f(Wout - newH - 1, 0);
                        d2 = new Point2f(Wout - 1, newW);
                    }
                    else
                    {
                        d0 = new Point2f(newH - 1, 0);
                        d1 = new Point2f(0, 0);
                        d2 = new Point2f(newH - 1, newW);
                    }
                    break;
                case Side.Bottom:
                    if (forward)
                    {
                        d0 = new Point2f(0, Hout - 1);
                        d1 = new Point2f(newH - 1, Hout - 1);
                        d2 = new Point2f(0, Hout - newW - 1);
                    }
                    else
                    {
                        d0 = new Point2f(Wout - newH - 1, Hout - 1);
                        d1 = new Point2f(Wout - 1, Hout - 1);
                        d2 = new Point2f(Wout - newH - 1, Hout - newW);
                    }
                    break;
                case Side.Left:
                    if (forward)
                    {
                        d0 = new Point2f(0, 0);
                        d1 = new Point2f(0, newH - 1);
                        d2 = new Point2f(newW - 1, 0);
                    }
                    else
                    {
                        d0 = new Point2f(0, Hout - newH - 1);
                        d1 = new Point2f(0, Hout - 1);
                        d2 = new Point2f(newW - 1, Hout - newH - 1);
                    }
                    break;
                case Side.Right:
                    if (forward)
                    {
                        d0 = new Point2f(Wout - 1, Hout - 1);
                        d1 = new Point2f(Wout - 1, Hout - newH - 1);
                        d2 = new Point2f(Wout - newW - 1, Hout - 1);
                    }
                    else
                    {
                        d0 = new Point2f(Wout - 1, newH - 1);
                        d1 = new Point2f(Wout - 1, 0);
                        d2 = new Point2f(Wout - newW - 1, newH - 1);
                    }
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(side));
            }

            return Cv2.GetAffineTransform(new[] { s0, s1, s2 }, new[] { d0, d1, d2 });
        }


        private static Mat BuildForSide2(Mat strip, Side side, int Wout, int Hout, double scale)
        {
            int w = strip.Width, h = strip.Height;
            bool longIsWidth = w >= h;

            int newW = (int)Math.Floor(w * scale);
            int newH = (int)Math.Floor(h * scale);

            Point2f s0, s1, s2;
            s0 = new Point2f(0, 0); // 左上
            if (longIsWidth)
            {
                s1 = new Point2f(w - 1, 0);
                s2 = new Point2f(0, h - 1);
            }
            else
            {
                s1 = new Point2f(0, h - 1);
                s2 = new Point2f(w - 1, 0);
            }

            Point2f d0, d1, d2;
            switch (side)
            {
                case Side.Top:
                    d0 = new Point2f(Wout - 1, 0);
                    d1 = new Point2f(0, 0);
                    d2 = new Point2f(Wout - 1, newW);
                    break;
                case Side.Bottom:
                    d0 = new Point2f(0, Hout - 1);
                    d1 = new Point2f(Wout - 1, Hout - 1);
                    d2 = new Point2f(0, Hout - newW - 1);
                    break;
                case Side.Left:
                    d0 = new Point2f(0, 0);
                    d1 = new Point2f(0, Hout - 1);
                    d2 = new Point2f(newW - 1, 0);
                    break;
                case Side.Right:
                    d0 = new Point2f(Wout - 1, Hout - 1);
                    d1 = new Point2f(Wout - 1, 0);
                    d2 = new Point2f(Wout - newW - 1, Hout - 1);
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(side));
            }

            return Cv2.GetAffineTransform(new[] { s0, s1, s2 }, new[] { d0, d1, d2 });
        }


        /// <summary>
        /// 获取测量结果
        /// </summary>
        public KBTDispensing_MeasureResult GetMeasureResult()
        {
            try
            {
                _measureResult = new KBTDispensing_MeasureResult();

                BuildAffineMatrix(1, _measureParam.VisualScaleFactor);
                //BuildAffineMatrix2();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                for (int i = 0; i < _imageData.Count; ++i)
                {
                    _imageData[i].GrayImage?.Dispose();
                    _imageData[i].HeightImage?.Dispose();
                }
            }

            return _measureResult;
        }



        /// <summary>
        /// 算法配置参数
        /// </summary>
        public class KBTDispensing_MeasureParam
        {
            //模型参数
            private string _modelPath = PrismProvider.AppBasePath + "\\InstanceSegSDK\\models\\model.kmodel";
            private int _batchSize = 1;
            private DeviceType _deviceType = DeviceType.DEVICE_GPU;
            private ModelType _modelType = ModelType.MODEL_DETECTION_SEG;
            private double _confidenceThreshold = 0.35;
            private double _ioUThreshold = 0.45;
            private double _segmentationThreshold = 0.5;

            //传感器参数
            private double _intervalX = 2.875;    //X方向的像素当量(μm)
            private double _intervalY = 5;      //Y方向的像素当量(μm)
            private double _intervalZ = 1;      //Y方向的像素当量(μm)
            private double _minDepth = -50000;  //深度图深度值下限(μm * 10)
            private double _maxDepth = 50000;   //深度图深度值上限(μm * 10)

            // 测量参数
            private double _glueLowThresh = 200;      // 胶高分割阈值下限(μm * 10)
            private double _glueUpThresh = 50000;     // 胶高分割阈值上限(μm * 10)
            private double _frameLowThresh = -50000;    // 框面分割阈值下限(μm * 10)
            private double _frameUpThresh = -9000;      // 框面分割阈值上限(μm * 10)

            private double _samplingInterval = 100;       // 胶高胶宽采样点间隔(μm)
            private double _samplingViewInterval = 50;    // 采样点可视化间隔(个)
            private double _visualScaleFactor = 0.5;      // 可视化缩放因子

            private double _refractiveIndex = 1.5;  // 折射率

            private bool _isSaveImage = false;     //是否存储图片
            private string _saveImagePath = "";    //存储图片路径


            /// <summary>
            /// 模型参数：密封钉检测模型配置文件路径
            /// </summary>
            public string ModelPath
            {
                get { return _modelPath; }
                set { _modelPath = value; }
            }

            /// <summary>
            /// 模型参数：batchSize
            /// </summary>
            public int BatchSize
            {
                get { return _batchSize; }
                set { _batchSize = value; }
            }

            /// <summary>
            /// 模型参数：设备类型
            /// </summary>
            public DeviceType DeviceType
            {
                get { return _deviceType; }
                set { _deviceType = value; }
            }

            /// <summary>
            /// 模型参数：模型类型
            /// </summary>
            public ModelType ModelType
            {
                get { return _modelType; }
                set { _modelType = value; }
            }

            /// <summary>
            /// 模型参数：置信度阈值
            /// </summary>
            public double ConfidenceThreshold
            {
                get { return _confidenceThreshold; }
                set
                {
                    if (value > 0 && value <= 1)
                    {
                        _confidenceThreshold = value;
                    }
                    else
                    {
                        _confidenceThreshold = 0.5;
                    }
                }
            }

            /// <summary>
            /// 模型参数：IOU阈值
            /// </summary>
            public double IoUThreshold
            {
                get { return _ioUThreshold; }
                set
                {
                    if (value > 0 && value <= 1)
                    {
                        _ioUThreshold = value;
                    }
                    else
                    {
                        _ioUThreshold = 0.5;
                    }
                }
            }

            /// <summary>
            /// 模型参数：分割阈值
            /// </summary>
            public double SegmentationThreshold
            {
                get { return _segmentationThreshold; }
                set
                {
                    if (value > 0 && value <= 1)
                    {
                        _segmentationThreshold = value;
                    }
                    else
                    {
                        _segmentationThreshold = 0.5;
                    }
                }
            }


            /// <summary>
            /// 传感器参数：X方向点间隔
            /// </summary>
            public double IntervalX
            {
                get { return _intervalX; }
                set
                {
                    if (value > 0)
                    {
                        _intervalX = value;
                    }
                    else
                    {
                        _intervalX = 1;
                    }
                }
            }

            /// <summary>
            /// 传感器参数：Y方向点间隔
            /// </summary>
            public double IntervalY
            {
                get { return _intervalY; }
                set
                {
                    if (value > 0)
                    {
                        _intervalY = value;
                    }
                    else
                    {
                        _intervalY = 1;
                    }
                }
            }

            /// <summary>
            /// 传感器参数：Z方向点间隔
            /// </summary>
            public double IntervalZ
            {
                get { return _intervalZ; }
                set
                {
                    if (value > 0)
                    {
                        _intervalZ = value;
                    }
                    else
                    {
                        _intervalZ = 1;
                    }
                }
            }

            /// <summary>
            /// 传感器参数：深度图深度有效值下限(μm)
            /// </summary>
            public double MinDepth
            {
                get { return _minDepth; }
                set
                {
                    _minDepth = value;
                }
            }

            /// <summary>
            /// 传感器参数：深度图深度有效值上限(μm)
            /// </summary>
            public double MaxDepth
            {
                get { return _maxDepth; }
                set
                {
                    _maxDepth = value;
                }
            }

            /// <summary>
            /// 测量参数：胶面分割阈值下限(μm)
            /// </summary>
            public double GlueLowThresh
            {
                get { return _glueLowThresh; }
                set { _glueLowThresh = value; }
            }

            /// <summary>
            /// 测量参数：胶面分割阈值上限(μm)
            /// </summary>
            public double GlueUpThresh
            {
                get { return _glueUpThresh; }
                set { _glueUpThresh = value; }
            }

            /// <summary>
            /// 测量参数：框面分割阈值下限(μm)
            /// </summary>
            public double FrameLowThresh
            {
                get { return _frameLowThresh; }
                set { _frameLowThresh = value; }
            }

            /// <summary>
            /// 测量参数：框面分割阈值上限(μm)
            /// </summary>
            public double FrameUpThresh
            {
                get { return _frameUpThresh; }
                set { _frameUpThresh = value; }
            }

            /// <summary>
            /// 测量参数：胶高胶宽采样点间隔(μm)
            /// </summary>
            public double SamplingInterval
            {
                get { return _samplingInterval; }
                set
                {
                    if (value < _intervalZ)
                        _samplingInterval = _intervalZ;
                    else
                        _samplingInterval = value;
                }
            }

            /// <summary>
            /// 测量参数：采样点可视化间隔(每间隔n个采样点进行可视化)
            /// </summary>
            public double SamplingViewInterval
            {
                get { return _samplingViewInterval; }
                set { _samplingViewInterval = value; }
            }

            /// <summary>
            /// 测量参数：可视化缩放因子
            /// </summary>
            public double VisualScaleFactor
            {
                get { return _visualScaleFactor; }
                set { _visualScaleFactor = value; }
            }

            /// <summary>
            /// 测量参数：折射率
            /// </summary>
            public double RefractiveIndex
            {
                get { return _refractiveIndex; }
                set { _refractiveIndex = value; }
            }

            /// <summary>
            /// 是否存图
            /// </summary>
            public bool IsSaveImage
            {
                get { return _isSaveImage; }
                set { _isSaveImage = value; }
            }

            /// <summary>
            /// 存图位置
            /// </summary>
            public string SaveImagePath
            {
                get { return _saveImagePath; }
                set { _saveImagePath = value; }
            }

        }


        public class Polygon
        {
            // 轮廓点集
            public OpenCvSharp.Point[][] Contours { get; }

            public Polygon(HObject region, int offsetX = 0, int offsetY = 0)
            {
                if (region != null && region.IsInitialized() && region.CountObj() > 0)
                {
                    // 根据轮廓拟合
                    HOperatorSet.GenContourRegionXld(region, out HObject hoRegionContour, "border");

                    // 提取轮廓点集
                    HOperatorSet.GenPolygonsXld(hoRegionContour, out HObject hoRegionPolygon, "ramer", 2);
                    HOperatorSet.GetPolygonXld(hoRegionPolygon, out HTuple hvPolygonRows, out HTuple hvPolygonCols,
                                               out HTuple hvTmpLength, out HTuple hvTmpPhi);

                    List<OpenCvSharp.Point[]> tmpContours = new List<OpenCvSharp.Point[]>();
                    OpenCvSharp.Point[] tmpContour = new OpenCvSharp.Point[hvPolygonRows.Length];
                    for (int i = 0; i < hvPolygonRows.Length; i++)
                    {
                        tmpContour[i] = new OpenCvSharp.Point((int)hvPolygonCols.TupleSelect(i).D + offsetX,
                                                              (int)hvPolygonRows.TupleSelect(i).D + offsetY);
                    }
                    if (tmpContour.Length > 0)
                    {
                        tmpContours.Add(tmpContour);
                    }
                    Contours = tmpContours.ToArray();

                    hoRegionContour.Dispose();
                    hoRegionPolygon.Dispose();
                }
                else
                {
                    Contours = new OpenCvSharp.Point[][] { };
                }
            }


            public Polygon(OpenCvSharp.Point[][] contours)
            {
                Contours = contours ?? Array.Empty<OpenCvSharp.Point[]>();
            }


            public Polygon()
            {
                Contours = new OpenCvSharp.Point[][] { };
            }
        }


        public class Plane
        {
            public double A { get; set; }
            public double B { get; set; }
            public double C { get; set; }
            public double D { get; set; }

            public Plane()
            {

            }

            public Plane(double a, double b, double c, double d)
            {
                A = a;
                B = b;
                C = c;
                D = d;
            }

            public double AbsDistanceTo(Point3d point)
            {
                return Math.Abs(A * point.X + B * point.Y + C * point.Z + D) / (Math.Sqrt(A * A + B * B + C * C) + 1e-12);
            }

            public double DistanceTo(Point3d point)
            {
                return (A * point.X + B * point.Y + C * point.Z + D) / (Math.Sqrt(A * A + B * B + C * C) + 1e-12);
            }
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
            public IntPtr ClassName; // const char*
            public NativeKpts Keypoints;
            public NativeSeg Segmentation;
        }


        /// <summary>
        /// 瑕疵检测结果
        /// </summary>
        public class DefectResult
        {
            public bool IsOk { get; set; }

            public int InstanceId { get; set; }

            public double Left { get; set; }

            public double Top { get; set; }

            public double Right { get; set; }

            public double Bottom { get; set; }

            public double Confidence { get; set; }

            public string ClassName { get; set; }
            /// <summary>
            /// 面积
            /// </summary>
            public double AreaFeature { get; set; }
            /// <summary>
            /// 直径
            /// </summary>
            public double DiameterFeature { get; set; }
            /// <summary>
            /// 深度
            /// </summary>
            public double DepthFeature { get; set; }

            public List<Polygon> DefectPolygons { get; set; }

            public DefectResult()
            {
                IsOk = false;
                InstanceId = -1;
                Left = Single.NegativeInfinity;
                Top = Single.NegativeInfinity;
                Right = Single.NegativeInfinity;
                Bottom = Single.NegativeInfinity;
                Confidence = Single.NegativeInfinity;
                ClassName = "";
                AreaFeature = Single.NegativeInfinity;
                DiameterFeature = Single.NegativeInfinity;
                DepthFeature = Single.NegativeInfinity;
                DefectPolygons = new List<Polygon>();

            }
        }


        /// <summary>
        /// 测量结果
        /// </summary>
        public class SideResult : IDisposable
        {
            private bool _disposed = false;

            /// <summary>
            /// 比例还原后的灰度图
            /// </summary>
            public Mat GrayImage { get; set; }

            /// <summary>
            /// 比例还原后的高度图
            /// </summary>
            public Mat HeightImage { get; set; }

            /// <summary>
            /// 采样点索引
            /// </summary>
            public int[] SampleIdx { get; set; }

            /// <summary>
            /// 采样点可视化索引(0:不可视化, 1:可视化)
            /// </summary>
            public int[] SampleViewIdx { get; set; }

            /// <summary>
            /// 测量点坐标X(像素)
            /// </summary>
            public double[] MeasurePointXList { get; set; }

            /// <summary>
            /// 测量点坐标Y(像素)
            /// </summary>
            public double[] MeasurePointYList { get; set; }

            /// <summary>
            /// 胶宽(微米)
            /// </summary>
            public double[] GlueWidthList { get; set; }

            /// <summary>
            /// 胶宽(像素)
            /// </summary>
            public double[] GlueWidthPixelList { get; set; }

            /// <summary>
            /// 胶路角度(度)
            /// </summary>
            public double[] GlueWidthAngleList { get; set; }

            /// <summary>
            /// 胶厚(微米)
            /// </summary>
            public double[] GlueThicknessList { get; set; }

            /// <summary>
            /// 胶宽最大值(微米)
            /// </summary>
            public double GlueWidthMax { get; set; }

            /// <summary>
            /// 胶宽最大值(像素)
            /// </summary>
            public double GlueWidthPixelMax { get; set; }

            /// <summary>
            /// 胶宽最大值胶路角度(度)
            /// </summary>
            public double[] GlueWidthMaxAngle { get; set; }

            /// <summary>
            /// 胶宽最大值坐标X(像素)
            /// </summary>
            public double[] GlueWidthMaxPointX { get; set; }

            /// <summary>
            /// 胶宽最大值坐标Y(像素)
            /// </summary>
            public double[] GlueWidthMaxPointY { get; set; }

            /// <summary>
            /// 胶宽最小值(微米)
            /// </summary>
            public double GlueWidthMin { get; set; }

            /// <summary>
            /// 胶宽最小值(像素)
            /// </summary>
            public double GlueWidthPixelMin { get; set; }

            /// <summary>
            /// 胶宽最大值胶路角度(度)
            /// </summary>
            public double[] GlueWidthMinAngle { get; set; }

            /// <summary>
            /// 胶宽最小值坐标X(像素)
            /// </summary>
            public double[] GlueWidthMinPointX { get; set; }

            /// <summary>
            /// 胶宽最小值坐标Y(像素)
            /// </summary>
            public double[] GlueWidthMinPointY { get; set; }

            /// <summary>
            /// 胶宽平均值(微米)
            /// </summary>
            public double GlueWidthAvg { get; set; }

            /// <summary>
            /// 胶厚最大值(微米)
            /// </summary>
            public double GlueThicknessMax { get; set; }

            /// <summary>
            /// 胶厚最大值坐标X(像素)
            /// </summary>
            public double[] GlueThicknessMaxPointX { get; set; }

            /// <summary>
            /// 胶厚最大值坐标Y(像素)
            /// </summary>
            public double[] GlueThicknessMaxPointY { get; set; }

            /// <summary>
            /// 胶厚最小值(微米)
            /// </summary>
            public double GlueThicknessMin { get; set; }

            /// <summary>
            /// 胶厚最小值坐标X(像素)
            /// </summary>
            public double[] GlueThicknessMinPointX { get; set; }

            /// <summary>
            /// 胶厚最小值坐标Y(像素)
            /// </summary>
            public double[] GlueThicknessMinPointY { get; set; }

            /// <summary>
            /// 胶厚平均值(微米)
            /// </summary>
            public double GlueThicknessAvg { get; set; }

            /// <summary>
            /// 胶面点云采样X坐标(微米)
            /// </summary>
            public double[] GlueSurfaceXList { get; set; }

            /// <summary>
            /// 胶面点云采样Y坐标(微米)
            /// </summary>
            public double[] GlueSurfaceYList { get; set; }

            /// <summary>
            /// 胶面点云采样Z坐标(微米)
            /// </summary>
            public double[] GlueSurfaceZList { get; set; }

            /// <summary>
            /// 框面点云采样X坐标(微米)
            /// </summary>
            public double[] FrameSurfaceXList { get; set; }

            /// <summary>
            /// 框面点云采样Y坐标(微米)
            /// </summary>
            public double[] FrameSurfaceYList { get; set; }

            /// <summary>
            /// 框面点云采样Z坐标(微米)
            /// </summary>
            public double[] FrameSurfaceZList { get; set; }

            /// <summary>
            /// 胶平面度
            /// </summary>
            public double GlueFlatness { get; set; }

            /// <summary>
            /// 框表面平面度(不要了)
            /// </summary>
            public double FrameFlatness { get; set; }

            /// <summary>
            /// 胶路相对于垂直方向的偏转角度(度:右偏为正、左偏为负)
            /// </summary>
            public double GluePathTiltAngle { get; set; }

            /// <summary>
            /// 缺陷结果
            /// </summary>
            public List<DefectResult> Defects { get; set; }


            public SideResult()
            {
                GrayImage = new Mat();
                HeightImage = new Mat();

                SampleIdx = new int[] { };
                SampleViewIdx = new int[] { };
                MeasurePointXList = new double[] { };
                MeasurePointYList = new double[] { };
                GlueWidthList = new double[] { };
                GlueWidthPixelList = new double[] { };
                GlueWidthAngleList = new double[] { };
                GlueThicknessList = new double[] { };
                GlueWidthMax = Single.NegativeInfinity;
                GlueWidthPixelMax = Single.NegativeInfinity;
                GlueWidthMaxAngle = new double[] { };
                GlueWidthMaxPointX = new double[] { };
                GlueWidthMaxPointY = new double[] { };
                GlueWidthMin = Single.NegativeInfinity;
                GlueWidthPixelMin = Single.NegativeInfinity;
                GlueWidthMinAngle = new double[] { };
                GlueWidthMinPointX = new double[] { };
                GlueWidthMinPointY = new double[] { };
                GlueWidthAvg = Single.NegativeInfinity;
                GlueThicknessMax = Single.NegativeInfinity;
                GlueThicknessMaxPointX = new double[] { };
                GlueThicknessMaxPointY = new double[] { };
                GlueThicknessMin = Single.NegativeInfinity;
                GlueThicknessMinPointX = new double[] { };
                GlueThicknessMinPointY = new double[] { };
                GlueThicknessAvg = Single.NegativeInfinity;
                GlueSurfaceXList = new double[] { };
                GlueSurfaceYList = new double[] { };
                GlueSurfaceZList = new double[] { };
                FrameSurfaceXList = new double[] { };
                FrameSurfaceYList = new double[] { };
                FrameSurfaceZList = new double[] { };
                GlueFlatness = Single.NegativeInfinity;
                FrameFlatness = Single.NegativeInfinity;

                Defects = new List<DefectResult>();
            }


            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (_disposed)
                    return;

                if (disposing)
                {
                    GrayImage?.Dispose();
                    HeightImage?.Dispose();

                    MeasurePointXList = new double[] { };
                    MeasurePointYList = new double[] { };
                    GlueWidthList = new double[] { };
                    GlueThicknessList = new double[] { };
                    GlueWidthMax = Single.NegativeInfinity;
                    GlueWidthMaxPointX = new double[] { };
                    GlueWidthMaxPointY = new double[] { };
                    GlueWidthMin = Single.NegativeInfinity;
                    GlueWidthMinPointX = new double[] { };
                    GlueWidthMinPointY = new double[] { };
                    GlueWidthAvg = Single.NegativeInfinity;
                    GlueThicknessMax = Single.NegativeInfinity;
                    GlueThicknessMaxPointX = new double[] { };
                    GlueThicknessMaxPointY = new double[] { };
                    GlueThicknessMin = Single.NegativeInfinity;
                    GlueThicknessMinPointX = new double[] { };
                    GlueThicknessMinPointY = new double[] { };
                    GlueThicknessAvg = Single.NegativeInfinity;
                    GlueSurfaceXList = new double[] { };
                    GlueSurfaceYList = new double[] { };
                    GlueSurfaceZList = new double[] { };
                    FrameSurfaceXList = new double[] { };
                    FrameSurfaceYList = new double[] { };
                    FrameSurfaceZList = new double[] { };
                    GlueFlatness = Single.NegativeInfinity;
                    FrameFlatness = Single.NegativeInfinity;

                    Defects.Clear();
                }

                _disposed = true;
            }


            ~SideResult()
            {
                Dispose(false);
            }
        }


        public class KBTDispensing_MeasureResult : IDisposable
        {
            private bool _disposed = false;

            /// <summary>
            /// 比例还原后的灰度图
            /// </summary>
            public Mat GrayImage { get; set; }

            /// <summary>
            /// 比例还原后的高度图
            /// </summary>
            public Mat HeightImage { get; set; }

            /// <summary>
            /// 胶宽最大值(微米)
            /// </summary>
            public double GlueWidthMax { get; set; }

            /// <summary>
            /// 胶宽最小值(微米)
            /// </summary>
            public double GlueWidthMin { get; set; }

            /// <summary>
            /// 胶宽平均值(微米)
            /// </summary>
            public double GlueWidthAvg { get; set; }

            /// <summary>
            /// 胶厚最大值(微米)
            /// </summary>
            public double GlueThicknessMax { get; set; }

            /// <summary>
            /// 胶厚最小值(微米)
            /// </summary>
            public double GlueThicknessMin { get; set; }

            /// <summary>
            /// 胶厚平均值(微米)
            /// </summary>
            public double GlueThicknessAvg { get; set; }

            /// <summary>
            /// 胶平面度
            /// </summary>
            public double GlueFlatness { get; set; }

            /// <summary>
            /// 框表面平面度
            /// </summary>
            public double FrameFlatness { get; set; }

            /// <summary>
            /// 各边详细测量结果
            /// </summary>
            public List<SideResult> SideResults { get; set; }

            public KBTDispensing_MeasureResult()
            {
                GrayImage = new Mat();
                HeightImage = new Mat();

                GlueWidthMax = Single.NegativeInfinity;
                GlueWidthMin = Single.NegativeInfinity;
                GlueWidthAvg = Single.NegativeInfinity;
                GlueThicknessMax = Single.NegativeInfinity;
                GlueThicknessMin = Single.NegativeInfinity;
                GlueThicknessAvg = Single.NegativeInfinity;
                GlueFlatness = Single.NegativeInfinity;
                FrameFlatness = Single.NegativeInfinity;

                SideResults = new List<SideResult>();
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (_disposed)
                    return;

                if (disposing)
                {
                    GrayImage?.Dispose();
                    HeightImage?.Dispose();

                    GlueWidthMax = Single.NegativeInfinity;
                    GlueWidthMin = Single.NegativeInfinity;
                    GlueWidthAvg = Single.NegativeInfinity;
                    GlueThicknessMax = Single.NegativeInfinity;
                    GlueThicknessMin = Single.NegativeInfinity;
                    GlueThicknessAvg = Single.NegativeInfinity;
                    GlueFlatness = Single.NegativeInfinity;
                    FrameFlatness = Single.NegativeInfinity;

                    if (SideResults != null)
                    {
                        foreach (var data in SideResults)
                        {
                            data?.Dispose();
                        }
                        SideResults.Clear();
                    }
                }

                _disposed = true;
            }


            ~KBTDispensing_MeasureResult()
            {
                Dispose(false);
            }

        }


    }
}
