using Custom.KCJC.Models;
using HalconDotNet;
using OpenCvSharp;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Services.CustomProject;
using ReeYin_V.Share;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Custom.KCJC.Models.StandardPlate
{
    public abstract class KCJC0_StandardPlateAlgorithm : ICustomAlgo
    {
        private bool _disposed;

        protected KCJC0_StandardPlateMeasureParam _measureParam;

        protected HObject _hoGrayImage = new HObject();
        protected HObject _hoHeightImage = new HObject();
        protected HObject _hoIrregularRegion = new HObject();
        protected HObject _hoValidRegion = new HObject();

        protected HTuple _hvHeightImageMinValue = new HTuple();
        protected HTuple _hvHeightImageMaxValue = new HTuple();
        protected HTuple _hvDepthMapMinValue = new HTuple();
        protected HTuple _hvDepthMapMaxValue = new HTuple();
        protected HTuple _hvImageScaleX = new HTuple();
        protected HTuple _hvImageScaleY = new HTuple();

        protected KCJC0_StandardPlateMeasureResult _measureResult = new KCJC0_StandardPlateMeasureResult();

        static KCJC0_StandardPlateAlgorithm()
        {
            HOperatorSet.SetSystem("global_mem_cache", "idle");
            HOperatorSet.SetSystem("temporary_mem_cache", "idle");
            HOperatorSet.SetSystem("image_cache_capacity", 0);
        }

        protected KCJC0_StandardPlateAlgorithm()
        {
            _measureParam = new KCJC0_StandardPlateMeasureParam();
            InitVariable();
        }

        public virtual int InitVariable()
        {
            DisposeInternal();

            _disposed = false;

            HOperatorSet.GenEmptyObj(out _hoGrayImage);
            HOperatorSet.GenEmptyObj(out _hoHeightImage);
            HOperatorSet.GenEmptyObj(out _hoIrregularRegion);
            HOperatorSet.GenEmptyObj(out _hoValidRegion);

            _hvHeightImageMinValue = new HTuple();
            _hvHeightImageMaxValue = new HTuple();
            _hvDepthMapMinValue = new HTuple();
            _hvDepthMapMaxValue = new HTuple();
            _hvImageScaleX = new HTuple(1.0);
            _hvImageScaleY = new HTuple(1.0);

            _measureResult = new KCJC0_StandardPlateMeasureResult();

            return 0;
        }

        public virtual void Dispose()
        {
            DisposeInternal();
            _disposed = true;
        }

        protected virtual bool PrepareInputImages(List<float[]> grayData, List<float[]> heightData, KCJC0_StandardPlateMeasureParam param, bool applyMedianFilter)
        {
            InitVariable();

            _measureParam = param.DeepCopy();

            if (ConvertListToHObject(grayData, ImageType.Gray, out HObject grayImageRaw) != 0)
            {
                grayImageRaw.Dispose();
                _measureResult.IsOK = false;
                return false;
            }
            HObject? grayImage = grayImageRaw;
            ReplaceHobject(ref _hoGrayImage, ref grayImage);

            if (ConvertListToHObject(heightData, ImageType.Depth, out HObject heightImageRaw) != 0)
            {
                heightImageRaw.Dispose();
                _measureResult.IsOK = false;
                return false;
            }
            HObject? heightImage = heightImageRaw;
            ReplaceHobject(ref _hoHeightImage, ref heightImage);

            HObject? hoTmp = null;
            try
            {
                if (_measureParam.IsFlip)
                {
                    HOperatorSet.MirrorImage(_hoGrayImage, out hoTmp, "row");
                    ReplaceHobject(ref _hoGrayImage, ref hoTmp);
                    HOperatorSet.MirrorImage(_hoHeightImage, out hoTmp, "row");
                    ReplaceHobject(ref _hoHeightImage, ref hoTmp);
                }

                HOperatorSet.MirrorImage(_hoGrayImage, out hoTmp, "column");
                ReplaceHobject(ref _hoGrayImage, ref hoTmp);
                HOperatorSet.MirrorImage(_hoHeightImage, out hoTmp, "column");
                ReplaceHobject(ref _hoHeightImage, ref hoTmp);

                HOperatorSet.ConvertImageType(_hoHeightImage, out hoTmp, "real");
                ReplaceHobject(ref _hoHeightImage, ref hoTmp);

                UpdateScaleFactors();
                ScaleImages();

                if (applyMedianFilter)
                {
                    HOperatorSet.MedianImage(_hoHeightImage, out hoTmp, "circle", 5, "mirrored");
                    ReplaceHobject(ref _hoHeightImage, ref hoTmp);
                }

                HOperatorSet.GetImageSize(_hoGrayImage, out HTuple hvWidth, out HTuple hvHeight);

                hvWidth.Dispose();
                hvHeight.Dispose();

                return true;
            }
            finally
            {
                hoTmp?.Dispose();
            }
        }

        protected virtual void UpdateScaleFactors()
        {
            double intervalX = _measureParam.IntervalX;
            double intervalY = _measureParam.IntervalY;

            double scaleX = 1.0;
            double scaleY = 1.0;
            if (_measureParam.FastModel)
            {
                if (intervalX > intervalY)
                {
                    scaleY = intervalY / intervalX;
                }
                else if (intervalX < intervalY)
                {
                    scaleX = intervalX / intervalY;
                }
            }
            else
            {
                if (intervalX < intervalY)
                {
                    scaleY = intervalY / intervalX;
                }
                else if (intervalX > intervalY)
                {
                    scaleX = intervalX / intervalY;
                }
            }

            _hvImageScaleX = new HTuple(scaleX);
            _hvImageScaleY = new HTuple(scaleY);
        }

        protected virtual void ScaleImages()
        {
            HObject? hoTmp = null;
            try
            {
                HOperatorSet.ZoomImageFactor(_hoGrayImage, out hoTmp, _hvImageScaleX, _hvImageScaleY, "bilinear");
                ReplaceHobject(ref _hoGrayImage, ref hoTmp);
                HOperatorSet.ZoomImageFactor(_hoHeightImage, out hoTmp, _hvImageScaleX, _hvImageScaleY, "nearest_neighbor");
                ReplaceHobject(ref _hoHeightImage, ref hoTmp);
                HOperatorSet.ScaleImage(_hoHeightImage, out hoTmp, _measureParam.IntervalZ, 0);
                ReplaceHobject(ref _hoHeightImage, ref hoTmp);

                _measureParam.IntervalX = (_measureParam.IntervalX / _hvImageScaleX).D;
                _measureParam.IntervalY = (_measureParam.IntervalY / _hvImageScaleY).D;
                _measureParam.IntervalZ = _measureParam.IntervalZ / _measureParam.IntervalZ;
            }
            finally
            {
                hoTmp?.Dispose();
            }
        }

        protected virtual void UpdateDepthMapRange(HObject region, HObject image)
        {
            HTuple hvRange = new HTuple();
            try
            {
                HOperatorSet.MinMaxGray(region, image, 0, out _hvDepthMapMinValue, out _hvDepthMapMaxValue, out hvRange);
            }
            finally
            {
                hvRange.Dispose();
            }
        }

        protected virtual KCJC0_StandardPlateMeasureResult BuildResult(HObject? depthMapSource = null)
        {
            HObject source = depthMapSource ?? _hoHeightImage;
            _measureResult.DepthMap = HobjectToFloatArray(source);

            if (_hvDepthMapMinValue.Length > 0)
            {
                _measureResult.DepthMapMinValue = _hvDepthMapMinValue.D;
            }
            else if (_hvHeightImageMinValue.Length > 0)
            {
                _measureResult.DepthMapMinValue = _hvHeightImageMinValue.D;
            }

            if (_hvDepthMapMaxValue.Length > 0)
            {
                _measureResult.DepthMapMaxValue = _hvDepthMapMaxValue.D;
            }
            else if (_hvHeightImageMaxValue.Length > 0)
            {
                _measureResult.DepthMapMaxValue = _hvHeightImageMaxValue.D;
            }

            _measureResult.ImageScaleW = _hvImageScaleX.Length > 0 ? _hvImageScaleX.D * _measureParam.DepthMapSampleDownSizeW : 1.0;
            _measureResult.ImageScaleH = _hvImageScaleY.Length > 0 ? _hvImageScaleY.D * _measureParam.DepthMapSampleDownSizeH : 1.0;

            KCJC0_StandardPlateMeasureResult result = _measureResult;
            _measureResult = new KCJC0_StandardPlateMeasureResult();

            return result;
        }

        public virtual int ConvertListToHObject(List<float[]> data, ImageType imageType, out HObject hoObject)
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
            finally
            {
                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }
        }

        protected virtual float[][] HobjectToFloatArray(HObject hoImage)
        {
            try
            {
                HOperatorSet.CountChannels(hoImage, out HTuple hvChannels);
                if (hvChannels.Length == 0 || hvChannels[0].I != 1)
                {
                    return Array.Empty<float[]>();
                }

                HObject imageToRead = hoImage;
                HObject? scaledImage = null;
                try
                {
                    if (_measureParam.DepthMapSampleDownSizeW > 1 || _measureParam.DepthMapSampleDownSizeH > 1)
                    {
                        double scaleW = 1.0 / _measureParam.DepthMapSampleDownSizeW;
                        double scaleH = 1.0 / _measureParam.DepthMapSampleDownSizeH;
                        HOperatorSet.ZoomImageFactor(hoImage, out scaledImage, scaleW, scaleH, "nearest_neighbor");
                        imageToRead = scaledImage;
                    }

                    HOperatorSet.GetImagePointer1(imageToRead, out HTuple hvPointer, out HTuple hvType, out HTuple hvWidth, out HTuple hvHeight);
                    int width = hvWidth.I;
                    int height = hvHeight.I;

                    float[] buffer = new float[width * height];
                    Marshal.Copy(hvPointer, buffer, 0, buffer.Length);

                    float[][] result = new float[height][];
                    for (int row = 0; row < height; row++)
                    {
                        result[row] = new float[width];
                        Array.Copy(buffer, row * width, result[row], 0, width);
                    }

                    hvType.Dispose();
                    hvPointer.Dispose();
                    hvWidth.Dispose();
                    hvHeight.Dispose();
                    return result;
                }
                finally
                {
                    scaledImage?.Dispose();
                }
            }
            catch
            {
                return Array.Empty<float[]>();
            }
        }

        public void HobjectToMat(HObject hoImage, out Mat dst)
        {
            dst = new Mat();
            Mat? matRed = null;
            Mat? matGreen = null;
            Mat? matBlue = null;

            try
            {
                HOperatorSet.CountChannels(hoImage, out HTuple hvChannels);
                if (hvChannels.Length == 0)
                {
                    return;
                }

                if (hvChannels[0].I == 1)
                {
                    HOperatorSet.GetImagePointer1(hoImage, out HTuple hvPointer, out HTuple hvType, out HTuple hvWidth, out HTuple hvHeight);
                    using Mat raw = Mat.FromPixelData(hvHeight.I, hvWidth.I, MatType.CV_8UC1, (IntPtr)hvPointer);
                    dst = raw.Clone();
                    hvType.Dispose();
                    hvPointer.Dispose();
                    hvWidth.Dispose();
                    hvHeight.Dispose();
                }
                else if (hvChannels[0].I == 3)
                {
                    HOperatorSet.GetImagePointer3(hoImage, out HTuple hvPtrRed, out HTuple hvPtrGreen, out HTuple hvPtrBlue,
                                                  out HTuple hvType, out HTuple hvWidth, out HTuple hvHeight);
                    matRed = Mat.FromPixelData(hvHeight.I, hvWidth.I, MatType.CV_8UC1, (IntPtr)hvPtrRed);
                    matGreen = Mat.FromPixelData(hvHeight.I, hvWidth.I, MatType.CV_8UC1, (IntPtr)hvPtrGreen);
                    matBlue = Mat.FromPixelData(hvHeight.I, hvWidth.I, MatType.CV_8UC1, (IntPtr)hvPtrBlue);
                    using Mat merged = new Mat();
                    Cv2.Merge(new[] { matBlue, matGreen, matRed }, merged);
                    dst = merged.Clone();
                    hvType.Dispose();
                    hvPtrRed.Dispose();
                    hvPtrGreen.Dispose();
                    hvPtrBlue.Dispose();
                    hvWidth.Dispose();
                    hvHeight.Dispose();
                }
            }
            finally
            {
                matBlue?.Dispose();
                matGreen?.Dispose();
                matRed?.Dispose();
            }
        }

        protected static Point[] GetContourPoints(HObject contour)
        {
            if (contour == null || !contour.IsInitialized())
            {
                return Array.Empty<Point>();
            }
            try
            {
                HObject contourToRead = contour;
                HObject? selectedContour = null;
                HOperatorSet.CountObj(contour, out HTuple contourCount);
                try
                {
                    if (contourCount.Length == 0 || contourCount.I <= 0)
                    {
                        return Array.Empty<Point>();
                    }
                    if (contourCount.I > 1)
                    {
                        HOperatorSet.LengthXld(contour, out HTuple contourLengths);
                        try
                        {
                            if (contourLengths.Length == 0)
                            {
                                return Array.Empty<Point>();
                            }
                            HTuple maxContourLength = contourLengths.TupleMax();
                            HTuple maxContourIndex = contourLengths.TupleFind(maxContourLength);
                            try
                            {
                                if (maxContourIndex.Length == 0)
                                {
                                    return Array.Empty<Point>();
                                }
                                HOperatorSet.SelectObj(contour, out selectedContour, maxContourIndex[0] + 1);
                                contourToRead = selectedContour;
                            }
                            finally
                            {
                                maxContourLength.Dispose();
                                maxContourIndex.Dispose();
                            }
                        }
                        finally
                        {
                            contourLengths.Dispose();
                        }
                    }
                    HOperatorSet.GetContourXld(contourToRead, out HTuple hvRows, out HTuple hvCols);
                    try
                    {
                        int length = Math.Min(hvRows.Length, hvCols.Length);
                        Point[] points = new Point[length];
                        for (int i = 0; i < length; i++)
                        {
                            points[i] = new Point((int)Math.Round(hvCols[i].D), (int)Math.Round(hvRows[i].D));
                        }
                        return points;
                    }
                    finally
                    {
                        hvRows.Dispose();
                        hvCols.Dispose();
                    }
                }
                finally
                {
                    contourCount.Dispose();
                    selectedContour?.Dispose();
                }
            }
            catch (HOperatorException)
            {
                return Array.Empty<Point>();
            }
        }

        protected static Point[] GetRegionContourPoints(HObject region)
        {
            HObject? contour = null;
            try
            {
                HOperatorSet.GenContourRegionXld(region, out contour, "border");
                return GetContourPoints(contour);
            }
            finally
            {
                contour?.Dispose();
            }
        }

        protected static Line CreateLine(HTuple rowBegin, HTuple colBegin, HTuple rowEnd, HTuple colEnd)
        {
            return new Line(new Point2d(colBegin.D, rowBegin.D), new Point2d(colEnd.D, rowEnd.D));
        }

        protected static double Mean(IEnumerable<double> values)
        {
            double[] array = values.ToArray();
            return array.Length == 0 ? -1 : array.Average();
        }

        protected static double Max(IEnumerable<double> values)
        {
            double[] array = values.ToArray();
            return array.Length == 0 ? -1 : array.Max();
        }

        protected static double Min(IEnumerable<double> values)
        {
            double[] array = values.ToArray();
            return array.Length == 0 ? -1 : array.Min();
        }

        protected static double ComputeScriptMedian(double[] sortedValues)
        {
            if (sortedValues.Length == 0)
            {
                return -1;
            }

            int midIndex = sortedValues.Length / 2;
            if (sortedValues.Length % 2 == 0)
            {
                return sortedValues[midIndex];
            }

            if (midIndex <= 0)
            {
                return sortedValues[midIndex];
            }

            return (sortedValues[midIndex - 1] + sortedValues[midIndex]) * 0.5;
        }

        public static void ReplaceHobject(ref HObject target, ref HObject? source)
        {
            HObject current = target;
            if (!ReferenceEquals(current, source))
            {
                current?.Dispose();
            }

            target = source ?? new HObject();
            source = null;
        }

        private void DisposeInternal()
        {
            if (_disposed)
            {
                return;
            }

            _hoGrayImage.Dispose();
            _hoHeightImage.Dispose();
            _hoIrregularRegion.Dispose();
            _hoValidRegion.Dispose();

            _hvHeightImageMinValue.Dispose();
            _hvHeightImageMaxValue.Dispose();
            _hvDepthMapMinValue.Dispose();
            _hvDepthMapMaxValue.Dispose();
            _hvImageScaleX.Dispose();
            _hvImageScaleY.Dispose();

            _measureResult.ClearData();
        }

        public abstract KCJC0_StandardPlateMeasureResult Process(List<float[]> grayData, List<float[]> heightData, KCJC0_StandardPlateMeasureParam param);

        public abstract Mat CvDrawResult(KCJC0_StandardPlateMeasureResult measureResult, bool showGuides = true);

        public enum ImageType
        {
            Gray,
            Depth
        }

        public class Line
        {
            public Point StartPoint { get; }

            public Point EndPoint { get; }

            public double Radian { get; }

            public double Degree { get; }

            public Line(Point2d startPoint, Point2d endPoint)
            {
                StartPoint = new Point((int)Math.Round(startPoint.X), (int)Math.Round(startPoint.Y));
                EndPoint = new Point((int)Math.Round(endPoint.X), (int)Math.Round(endPoint.Y));
                Radian = Math.Atan2(endPoint.Y - startPoint.Y, endPoint.X - startPoint.X);
                Degree = Radian * (180.0 / Math.PI);
            }
        }

        public class Circle
        {
            public Point2d Center { get; set; } = new Point2d(-1, -1);

            public double Radius { get; set; } = -1;

            public Point[] Contour { get; set; } = Array.Empty<Point>();
        }

        [Serializable]
        public class KCJC0_StandardPlateMeasureParam
        {
            /// <summary>
            /// 通用参数：标准片算法类型(0: 刻槽检测 1: 刻点检测 2: 台阶高度检测)
            /// </summary>
            public int AlgorithmType { get; set; }

            /// <summary>
            /// 通用参数：X方向点间隔(>0)
            /// </summary>
            public double IntervalX { get; set; } = 2.9;

            /// <summary>
            /// 通用参数：Y方向点间隔(>0)
            /// </summary>
            [RecipeParam("CalibIntervalY", "标定Y方向像素当量")]
            public double IntervalY { get; set; } = 5.0;

            /// <summary>
            /// 通用参数：Z方向点间隔(>0)
            /// </summary>
            public double IntervalZ { get; set; } = 0.1;

            /// <summary>
            /// 通用参数：传感器参数：图片是否需要翻转(需要翻转：ture; 不需要翻转：false)
            /// </summary>
            public bool IsFlip { get; set; }

            public bool FastModel { get; set; } = false;

            /// <summary>
            /// 通用参数：图片边缘屏蔽区大小(像素)
            /// </summary>
            public int ImageEdgeMaskSize { get; set; } = 20;

            /// <summary>
            /// 通用参数：深度图宽度下采样率
            /// </summary>
            public int DepthMapSampleDownSizeW { get; set; } = 1;

            /// <summary>
            /// 通用参数：深度图高度下采样率
            /// </summary>
            public int DepthMapSampleDownSizeH { get; set; } = 1;

            /// <summary>
            /// 通用参数：高度图边缘梯度阈值
            /// </summary>
            public double HeightAmplitudeThreshold { get; set; } = 5.0;

            [RecipeParam("HeightAmplitudeSigma", "高度图边缘边缘平滑系数")]
            /// <summary>
            /// 通用参数：高度图边缘边缘平滑系数
            /// </summary>
            public double HeightAmplitudeSigma { get; set; } = 25.0;


            //刻槽参数
            public int NumMeasures { get; set; } = 20;

            public double MeasureLength1 { get; set; } = 250.0;

            public double MeasureLength2 { get; set; } = 5.0;

            //刻点参数

            public double StandardEtchingLineWidth { get; set; } = 0.0;
            public double StandardEtchingLineDepth { get; set; } = 0.0;
            public double StandardEtchingLineWidthStdDev { get; set; } = 0.0;
            public double StandardEtchingLineDepthStdDev { get; set; } = 0.0;
            public double StandardPointHeight { get; set; } = 0.0;
            public double StandardPointRadius { get; set; } = 0.0;
            public double StandardPointHeightStdDev { get; set; } = 0.0;
            public double StandardPointDiameterStdDev { get; set; } = 0.0;

            /// <summary>
            /// 所有槽的标准值
            /// </summary>
            public List<GrooveStandardRef> GrooveStandardRefs { get; set; } = new List<GrooveStandardRef>();

            /// <summary>
            /// 所有点的标准值
            /// </summary>
            public List<BumpStandardRef> BumpStandardRefs { get; set; } = new List<BumpStandardRef>();

            /// <summary>
            /// 所有阶梯的标准值
            /// </summary>
            
        }

        public class KCJC0_StandardPlateGrooveResult
        {
            public Point2d Center { get; set; } = new Point2d(-1, -1);

            public double AngleRad { get; set; } = double.NaN;

            public double WidthReal { get; set; } = -1;

            public double DepthReal { get; set; } = -1;

            public double WidthRealV2 { get; set; } = -1;

            public double DepthRealV2 { get; set; } = -1;

            public Line PositiveEdgeLine { get; set; } = new Line(new Point2d(0, 0), new Point2d(0, 0));

            public Line NegativeEdgeLine { get; set; } = new Line(new Point2d(0, 0), new Point2d(0, 0));
        }

        public class KCJC0_StandardPlateBumpResult
        {
            public Point2d Center { get; set; } = new Point2d(-1, -1);

            public double HeightGray { get; set; } = -1;

            public double HeightPhysical { get; set; } = -1;

            public double DiameterPixel { get; set; } = -1;

            public double DiameterPhysical { get; set; } = -1;

            public double MeasureLevelGray { get; set; } = -1;

            public Point[] RegionContour { get; set; } = Array.Empty<Point>();

            public Point[] ThresholdContour { get; set; } = Array.Empty<Point>();

            public Circle FitCircle { get; set; } = new Circle();
        }

        public class KCJC0_StandardPlateStepResult
        {
            public Line StepEdgeLine { get; set; } = new Line(new Point2d(0, 0), new Point2d(0, 0));

            public Point[] StepTopRegionContour { get; set; } = Array.Empty<Point>();

            public Point[] StepDownRegionContour { get; set; } = Array.Empty<Point>();

            public Point2d ReferencePoint { get; set; } = new Point2d(-1, -1);

            public double HeightPhysical { get; set; } = -1;
        }

        public class KCJC0_StandardPlateMeasureResult
        {
            public float[][] DepthMap { get; set; } = Array.Empty<float[]>();

            public double DepthMapMinValue { get; set; } = double.NegativeInfinity;

            public double DepthMapMaxValue { get; set; } = double.PositiveInfinity;

            public double ImageScaleW { get; set; } = 1.0;

            public double ImageScaleH { get; set; } = 1.0;

            public bool IsOK { get; set; } = true;

            public List<KCJC0_StandardPlateGrooveResult> GrooveResults { get; set; } = new List<KCJC0_StandardPlateGrooveResult>();

            /// <summary>
            /// 槽所有的宽度
            /// </summary>
            public double[] GrooveWidthRealList { get; set; } = Array.Empty<double>();

            /// <summary>
            /// 槽所有的深度
            /// </summary>
            public double[] GrooveDepthRealList { get; set; } = Array.Empty<double>();

            /// <summary>
            /// 槽所有的第二种计算方法的宽度
            /// </summary>
            public double[] GrooveWidthRealListV2 { get; set; } = Array.Empty<double>();

            /// <summary>
            /// 槽所有的第二种计算方法的深度
            /// </summary>
            public double[] GrooveDepthRealListV2 { get; set; } = Array.Empty<double>();

            public double GrooveWidthRealMean { get; set; } = -1;

            public double GrooveWidthRealMax { get; set; } = -1;

            public double GrooveWidthRealMin { get; set; } = -1;

            public double GrooveWidthRealMeanV2 { get; set; } = -1;

            public double GrooveWidthRealMaxV2 { get; set; } = -1;

            public double GrooveWidthRealMinV2 { get; set; } = -1;

            public double GrooveDepthRealMean { get; set; } = -1;

            public double GrooveDepthRealMax { get; set; } = -1;

            public double GrooveDepthRealMin { get; set; } = -1;

            public double GrooveDepthRealMeanV2 { get; set; } = -1;

            public double GrooveDepthRealMaxV2 { get; set; } = -1;

            public double GrooveDepthRealMinV2 { get; set; } = -1;

            public List<KCJC0_StandardPlateBumpResult> BumpResults { get; set; } = new List<KCJC0_StandardPlateBumpResult>();

            /// <summary>
            /// 点所有的高度
            /// </summary>
            public double[] BumpHeightPhysicalList { get; set; } = Array.Empty<double>();

            /// <summary>
            /// 点所有的直径
            /// </summary>
            public double[] BumpDiameterPhysicalList { get; set; } = Array.Empty<double>();

            public double BumpHeightPhysicalMean { get; set; } = -1;

            public double BumpHeightPhysicalMax { get; set; } = -1;

            public double BumpHeightPhysicalMin { get; set; } = -1;

            public double BumpDiameterPhysicalMean { get; set; } = -1;

            public double BumpDiameterPhysicalMax { get; set; } = -1;

            public double BumpDiameterPhysicalMin { get; set; } = -1;

            public List<KCJC0_StandardPlateStepResult> StepResults { get; set; } = new List<KCJC0_StandardPlateStepResult>();

            /// <summary>
            /// 台阶所有的高度
            /// </summary>
            public double[] StepHeightPhysicalList { get; set; } = Array.Empty<double>();

            public double StepHeightPhysicalMean { get; set; } = -1;

            public double StepHeightPhysicalMax { get; set; } = -1;

            public double StepHeightPhysicalMin { get; set; } = -1;

            public double FinalHeightThreshold { get; set; } = -1;

            public void ClearData()
            {
                DepthMap = Array.Empty<float[]>();
                GrooveResults.Clear();
                BumpResults.Clear();
                StepResults.Clear();
                GrooveWidthRealList = Array.Empty<double>();
                GrooveDepthRealList = Array.Empty<double>();
                GrooveWidthRealListV2 = Array.Empty<double>();
                GrooveDepthRealListV2 = Array.Empty<double>();
                BumpHeightPhysicalList = Array.Empty<double>();
                BumpDiameterPhysicalList = Array.Empty<double>();
                StepHeightPhysicalList = Array.Empty<double>();
            }
        }
    }
}
