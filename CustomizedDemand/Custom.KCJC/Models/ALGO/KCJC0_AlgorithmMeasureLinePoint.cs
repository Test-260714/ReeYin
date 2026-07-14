using OpenCvSharp;
using ReeYin_V.Core.Extension;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Custom.KCJC.Models.ALGO
{
    public class KCJC0_AlgorithmMeasureLinePoint : KCJC0_Algorithm
    {
        private readonly KCJC0_AlgorithmMeasureLine _measureLineAlgorithm;
        private readonly KCJC0_AlgorithmMeasurePoint _measurePointAlgorithm;
        private bool _disposed = false;


        public KCJC0_AlgorithmMeasureLinePoint()
        {
            _measureLineAlgorithm = new KCJC0_AlgorithmMeasureLine();
            _measurePointAlgorithm = new KCJC0_AlgorithmMeasurePoint();

            InitVariable();
        }


        public override void Dispose()
        {
            base.Dispose();

            if (!_disposed)
            {
                _measureLineAlgorithm.Dispose();
                _measurePointAlgorithm.Dispose();

                _disposed = true;
            }
        }


        /// <summary>
        /// 变量初始化
        /// </summary>
        public override int InitVariable()
        {
            base.InitVariable();

            _disposed = false;

            return 0;
        }


        /// <summary>
        /// 测量过程
        /// </summary>
        /// <param name="grayDate">输入的灰度图数据</param>
        /// <param name="heightData">输入深度图数据</param>
        /// <param name="param">测量配置参数</param>
        /// <returns>result</returns>
        public override KCJC0_MeasureResult Process(List<float[]> grayDate, List<float[]> heightData, KCJC0_MeasureParam param)
        {
            KCJC0_MeasureResult result = new KCJC0_MeasureResult();

            try
            {
                Dispose();
                InitVariable();

                _measureParam = param.DeepCopy();

                KCJC0_MeasureParam lineParam = _measureParam.DeepCopy();
                lineParam.AlgorithmType = 0;
                KCJC0_MeasureParam pointParam = _measureParam.DeepCopy();
                pointParam.AlgorithmType = 1;

                KCJC0_MeasureResult measureLineResult = ProcessMeasureLine(grayDate, heightData, lineParam);
                KCJC0_MeasureResult measurePointResult = ProcessMeasurePoint(grayDate, heightData, pointParam);

                bool hasEtchingLine = HasEtchingLineResult(measureLineResult);
                bool hasEtchingPoint = HasEtchingPointResult(measurePointResult);

                result = MergeMeasureResult(measureLineResult, measurePointResult, hasEtchingLine, hasEtchingPoint);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }

            return result;
        }


        /// <summary>
        /// 绘制结果
        /// </summary>
        public override Mat CvDrawResult(KCJC0_MeasureResult measureResult, bool showGuides = false)
        {
            Mat image = new Mat();

            try
            {
                bool hasEtchingLine = HasEtchingLineResult(measureResult);
                bool hasEtchingPoint = HasEtchingPointResult(measureResult);

                if (hasEtchingLine)
                {
                    image = _measureLineAlgorithm.CvDrawResult(measureResult, showGuides);
                }
                else
                {
                    image = _measureLineAlgorithm.CvDrawResult(new KCJC0_MeasureResult(), false);
                    if (image.Empty())
                    {
                        image = _measurePointAlgorithm.CvDrawResult(new KCJC0_MeasureResult(), false);
                    }
                }

                if (hasEtchingPoint)
                {
                    DrawPointResults(image, measureResult.ConvexResultsList, new Scalar(255, 128, 0), new Scalar(0, 255, 0));
                    DrawPointResults(image, measureResult.ConcaveResultsList, new Scalar(255, 0, 255), new Scalar(0, 255, 255));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }

            return image;
        }


        /// <summary>
        /// 执行刻槽测量流程
        /// </summary>
        private KCJC0_MeasureResult ProcessMeasureLine(List<float[]> grayDate, List<float[]> heightData, KCJC0_MeasureParam param)
        {
            try
            {
                return _measureLineAlgorithm.Process(grayDate, heightData, param);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return new KCJC0_MeasureResult();
            }
        }


        /// <summary>
        /// 执行压花测量流程
        /// </summary>
        private KCJC0_MeasureResult ProcessMeasurePoint(List<float[]> grayDate, List<float[]> heightData, KCJC0_MeasureParam param)
        {
            try
            {
                return _measurePointAlgorithm.Process(grayDate, heightData, param);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return new KCJC0_MeasureResult();
            }
        }


        /// <summary>
        /// 判断是否存在刻槽测量结果
        /// </summary>
        private bool HasEtchingLineResult(KCJC0_MeasureResult? measureResult)
        {
            if (measureResult == null)
            {
                return false;
            }

            if (measureResult.PartitionResults != null &&
                measureResult.PartitionResults.Any(partition => partition != null &&
                                                                (partition.EtchingLineNum > 0 ||
                                                                 partition.EtchingLineNeg.Count > 0 ||
                                                                 partition.EtchingLinePos.Count > 0)))
            {
                return true;
            }

            if (measureResult.EtchingLineWidthRealMeanList != null &&
                measureResult.EtchingLineWidthRealMeanList.Any(value => value > 0))
            {
                return true;
            }

            if (measureResult.EtchingLineDepthMeanList != null &&
                measureResult.EtchingLineDepthMeanList.Any(value => value > 0))
            {
                return true;
            }

            if (measureResult.GlobalEtchingLineWidthRealMean > 0 ||
                measureResult.GlobalEtchingLineDepthMean > 0)
            {
                return true;
            }

            return IsValidLine(measureResult.FitEtchingRegionTopEdgeLine) ||
                   IsValidLine(measureResult.FitEtchingRegionBottomEdgeLine) ||
                   IsValidLine(measureResult.FitEtchingRegionStartEdgeLine) ||
                   IsValidLine(measureResult.FitEtchingRegionEndEdgeLine);
        }


        /// <summary>
        /// 判断是否存在压花测量结果
        /// </summary>
        private bool HasEtchingPointResult(KCJC0_MeasureResult? measureResult)
        {
            if (measureResult == null)
            {
                return false;
            }

            bool hasConvex = measureResult.ConvexResultsList != null && measureResult.ConvexResultsList.Count > 0;
            bool hasConcave = measureResult.ConcaveResultsList != null && measureResult.ConcaveResultsList.Count > 0;

            return hasConvex || hasConcave;
        }


        /// <summary>
        /// 合并刻槽与压花测量结果
        /// </summary>
        private KCJC0_MeasureResult MergeMeasureResult(KCJC0_MeasureResult measureLineResult,
                                                       KCJC0_MeasureResult measurePointResult,
                                                       bool hasEtchingLine,
                                                       bool hasEtchingPoint)
        {
            KCJC0_MeasureResult result = measureLineResult ?? new KCJC0_MeasureResult();

            if (!hasEtchingLine)
            {
                ResetMeasureLineResult(result);
            }

            if (hasEtchingPoint)
            {
                result.ConvexResultsList = measurePointResult.ConvexResultsList ?? new List<KCJC0_ConvexConcaveResult>();
                result.ConcaveResultsList = measurePointResult.ConcaveResultsList ?? new List<KCJC0_ConvexConcaveResult>();
            }
            else
            {
                result.ConvexResultsList = new List<KCJC0_ConvexConcaveResult>();
                result.ConcaveResultsList = new List<KCJC0_ConvexConcaveResult>();
            }

            if ((result.DepthMap == null || result.DepthMap.Length == 0) &&
                measurePointResult.DepthMap != null && measurePointResult.DepthMap.Length > 0)
            {
                result.DepthMap = measurePointResult.DepthMap.DeepCopy();
            }

            if (double.IsNegativeInfinity(result.DepthMapMinValue) &&
                !double.IsNegativeInfinity(measurePointResult.DepthMapMinValue))
            {
                result.DepthMapMinValue = measurePointResult.DepthMapMinValue;
            }

            if (double.IsPositiveInfinity(result.DepthMapMaxValue) &&
                !double.IsPositiveInfinity(measurePointResult.DepthMapMaxValue))
            {
                result.DepthMapMaxValue = measurePointResult.DepthMapMaxValue;
            }

            if (result.ImageScaleW <= 0 && measurePointResult.ImageScaleW > 0)
            {
                result.ImageScaleW = measurePointResult.ImageScaleW;
            }

            if (result.ImageScaleH <= 0 && measurePointResult.ImageScaleH > 0)
            {
                result.ImageScaleH = measurePointResult.ImageScaleH;
            }

            result.IsOK = (!hasEtchingLine || measureLineResult.IsOK) &&
                          (!hasEtchingPoint || measurePointResult.IsOK);

            return result;
        }


        /// <summary>
        /// 重置刻槽测量结果
        /// </summary>
        private void ResetMeasureLineResult(KCJC0_MeasureResult measureResult)
        {
            measureResult.FitEtchingRegionTopEdgeLine = new Line(new OpenCvSharp.Point2d(0, 0), new OpenCvSharp.Point2d(0, 0));
            measureResult.FitEtchingRegionBottomEdgeLine = new Line(new OpenCvSharp.Point2d(0, 0), new OpenCvSharp.Point2d(0, 0));
            measureResult.FitEtchingRegionStartEdgeLine = new Line(new OpenCvSharp.Point2d(0, 0), new OpenCvSharp.Point2d(0, 0));
            measureResult.FitEtchingRegionEndEdgeLine = new Line(new OpenCvSharp.Point2d(0, 0), new OpenCvSharp.Point2d(0, 0));

            measureResult.PartitionResults = new List<KCJC0_PartitionResult>();

            measureResult.EtchingLineWidthRealMeanList = Array.Empty<double>();
            measureResult.EtchingLineWidthRealMaxList = Array.Empty<double>();
            measureResult.EtchingLineWidthRealMinList = Array.Empty<double>();

            measureResult.EtchingLineDepthMeanList = Array.Empty<double>();
            measureResult.EtchingLineDepthMaxList = Array.Empty<double>();
            measureResult.EtchingLineDepthMinList = Array.Empty<double>();

            measureResult.EtchingPointDistRealMeanList = Array.Empty<double>();
            measureResult.EtchingPointDistRealMaxList = Array.Empty<double>();
            measureResult.EtchingPointDistRealMinList = Array.Empty<double>();

            measureResult.EtchingRegionLeftGapList = Array.Empty<double>();
            measureResult.EtchingRegionRightGapList = Array.Empty<double>();

            measureResult.EtchingRegionLeftGapRealList = Array.Empty<double>();
            measureResult.EtchingRegionRightGapRealList = Array.Empty<double>();

            measureResult.GlobalEtchingRegionLeftGap = -1;
            measureResult.GlobalEtchingRegionRightGap = -1;
            measureResult.GlobalEtchingRegionTopGap = -1;
            measureResult.GlobalEtchingRegionBottomGap = -1;

            measureResult.GlobalEtchingLineWidthRealMean = -1;
            measureResult.GlobalEtchingLineWidthRealMax = -1;
            measureResult.GlobalEtchingLineWidthRealMin = -1;
            measureResult.GlobalEtchingLineWidthRealRange = -1;

            measureResult.GlobalEtchingLineDepthMean = -1;
            measureResult.GlobalEtchingLineDepthMax = -1;
            measureResult.GlobalEtchingLineDepthMin = -1;
            measureResult.GlobalEtchingLineDepthRange = -1;

            measureResult.EtchingRegionLeftGapReal = -1;
            measureResult.EtchingRegionRightGapReal = -1;
            measureResult.EtchingRegionTopGapReal = -1;
            measureResult.EtchingRegionBottomGapReal = -1;

            measureResult.EtchingRegionTopGapRealMean = -1;
            measureResult.EtchingRegionTopGapRealMax = -1;
            measureResult.EtchingRegionTopGapRealMin = -1;

            measureResult.EtchingRegionBottomGapRealMean = -1;
            measureResult.EtchingRegionBottomGapRealMax = -1;
            measureResult.EtchingRegionBottomGapRealMin = -1;

            measureResult.EtchingLineAngle = Single.NegativeInfinity;
        }


        /// <summary>
        /// 绘制压花结果
        /// </summary>
        private void DrawPointResults(Mat image, List<KCJC0_ConvexConcaveResult> results, Scalar markerColor, Scalar fitColor)
        {
            if (image.Empty() || results == null || results.Count == 0)
            {
                return;
            }

            for (int i = 0; i < results.Count; i++)
            {
                KCJC0_ConvexConcaveResult result = results[i];
                if (!IsDrawableResult(image, result))
                {
                    continue;
                }

                Polygon polygon = result.FitPolygon;
                string heightDiff = result.HeightDiff.ToString("F3");
                string radius = result.Radius.ToString("F3");

                Cv2.DrawMarker(image, new OpenCvSharp.Point((int)result.MeasurePointX, (int)result.MeasurePointY),
                               markerColor, MarkerTypes.Cross, markerSize: 30, thickness: 4);

                Cv2.DrawContours(image, polygon.Contours, -1, fitColor, 1);
                Cv2.Circle(image, new OpenCvSharp.Point((int)polygon.Center.X, (int)polygon.Center.Y),
                           (int)polygon.Radius, fitColor, 4);

                OpenCvSharp.Point hp = new OpenCvSharp.Point(20 + (int)result.MeasurePointX, 20 + (int)result.MeasurePointY);
                Cv2.PutText(image, "h:" + heightDiff + " um", hp, HersheyFonts.HersheySimplex, 1, new Scalar(0, 0, 255), 2);

                OpenCvSharp.Point rp = new OpenCvSharp.Point(20 + (int)result.MeasurePointX, 50 + (int)result.MeasurePointY);
                Cv2.PutText(image, "r:" + radius + " um", rp, HersheyFonts.HersheySimplex, 1, new Scalar(0, 0, 255), 2);
            }
        }


        /// <summary>
        /// 判断线是否有效
        /// </summary>
        private bool IsValidLine(Line? line)
        {
            if (line == null)
            {
                return false;
            }

            return line.StartPoint.X != 0 || line.StartPoint.Y != 0 ||
                   line.EndPoint.X != 0 || line.EndPoint.Y != 0;
        }


        /// <summary>
        /// 判断测点是否可绘制
        /// </summary>
        private bool IsDrawablePoint(Mat image, double x, double y)
        {
            return double.IsFinite(x) &&
                   double.IsFinite(y) &&
                   x >= 0 && x < image.Width &&
                   y >= 0 && y < image.Height;
        }


        /// <summary>
        /// 判断区域是否可绘制
        /// </summary>
        private bool IsDrawablePolygon(Polygon? candidate)
        {
            return candidate != null &&
                   double.IsFinite(candidate.Center.X) &&
                   double.IsFinite(candidate.Center.Y) &&
                   double.IsFinite(candidate.Radius) &&
                   candidate.Radius > 0 &&
                   candidate.Contours != null &&
                   candidate.Contours.Length > 0 &&
                   candidate.Contours.Any(contour => contour != null && contour.Length > 0);
        }


        /// <summary>
        /// 判断压花结果是否可绘制
        /// </summary>
        private bool IsDrawableResult(Mat image, KCJC0_ConvexConcaveResult? candidate)
        {
            return candidate != null &&
                   IsDrawablePoint(image, candidate.MeasurePointX, candidate.MeasurePointY) &&
                   double.IsFinite(candidate.HeightDiff) &&
                   candidate.HeightDiff > 0 &&
                   double.IsFinite(candidate.Radius) &&
                   candidate.Radius > 0 &&
                   IsDrawablePolygon(candidate.FitPolygon);
        }
    }
}
