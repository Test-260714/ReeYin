using HalconDotNet;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Custom.KCJC.Models.StandardPlate
{
    public class KCJC0_StandardPlateAlgorithmMeasureLine : KCJC0_StandardPlateAlgorithm
    {
        private const double InvalidDepthValue = 888888.0;

        public override KCJC0_StandardPlateMeasureResult Process(List<float[]> grayData, List<float[]> heightData, KCJC0_StandardPlateMeasureParam param)
        {
            HObject? hoValidMaskImage = null;

            try
            {
                if (!PrepareInputImages(grayData, heightData, param, applyMedianFilter: false))
                {
                    return BuildResult();
                }

                FlattenHeightImage(out hoValidMaskImage);
                UpdateDepthMapRange(_hoValidRegion, _hoHeightImage);

                DetectGrooveCenters(out HTuple grooveCenterRows, out HTuple grooveCenterCols, out HTuple intraDistance);
                try
                {
                    int grooveCount = Math.Min(grooveCenterRows.Length, grooveCenterCols.Length);
                    if (grooveCount <= 0)
                    {
                        _measureResult.IsOK = false;
                        return BuildResult();
                    }

                    double groovePhi = EstimateGroovePhi(grooveCenterRows, grooveCenterCols);

                    for (int idx = 0; idx < grooveCount; idx++)
                    {
                        double measureLength3 = idx < intraDistance.Length
                            ? Math.Max(intraDistance[idx].D * 1.5, 1.0)
                            : Math.Max(_measureParam.MeasureLength2 * 2.0, 1.0);

                        double centerRow = grooveCenterRows[idx].D;
                        double centerCol = grooveCenterCols[idx].D;
                        double beginRow = centerRow - _measureParam.MeasureLength1 * Math.Cos(groovePhi);
                        double beginCol = centerCol - _measureParam.MeasureLength1 * Math.Sin(groovePhi);
                        double endRow = centerRow + _measureParam.MeasureLength1 * Math.Cos(groovePhi);
                        double endCol = centerCol + _measureParam.MeasureLength1 * Math.Sin(groovePhi);

                        KCJC0_StandardPlateGrooveResult grooveResult = new KCJC0_StandardPlateGrooveResult
                        {
                            Center = new Point2d(centerCol, centerRow),
                            AngleRad = groovePhi
                        };

                        if (MeasureGrooveWidth(beginRow, beginCol, endRow, endCol, measureLength3,
                                               out double widthReal, out Line positiveEdge, out Line negativeEdge))
                        {
                            grooveResult.WidthReal = widthReal;
                            grooveResult.PositiveEdgeLine = positiveEdge;
                            grooveResult.NegativeEdgeLine = negativeEdge;
                        }
                        else
                        {
                            grooveResult.WidthReal = -1;
                            _measureResult.IsOK = false;
                        }

                        grooveResult.DepthReal = MeasureGrooveDepth(hoValidMaskImage, beginRow, beginCol, endRow, endCol, groovePhi, measureLength3,
                                                                    out double widthRealV2, out double depthRealV2);
                        grooveResult.WidthRealV2 = widthRealV2;
                        grooveResult.DepthRealV2 = depthRealV2;
                        if (grooveResult.DepthReal < 0)
                        {
                            _measureResult.IsOK = false;
                        }
                        if (grooveResult.WidthRealV2 < 0 || grooveResult.DepthRealV2 < 0)
                        {
                            _measureResult.IsOK = false;
                        }

                        _measureResult.GrooveResults.Add(grooveResult);
                    }

                    _measureResult.GrooveWidthRealList = _measureResult.GrooveResults.Select((x, i) =>
                                                          x.WidthReal < 0 ? x.WidthReal :
                                                          ((x.WidthReal - _measureParam.GrooveStandardRefs[i].WidthStandard) / 3) + _measureParam.GrooveStandardRefs[i].WidthStandard).ToArray();
                    _measureResult.GrooveDepthRealList = _measureResult.GrooveResults.Select(x => x.DepthReal).ToArray();

                    _measureResult.GrooveWidthRealListV2 = _measureResult.GrooveResults.Select((x, i) =>
                                                          x.WidthRealV2 < 0 ? x.WidthRealV2 :
                                                          ((x.WidthRealV2 - _measureParam.GrooveStandardRefs[i].WidthStandard) / 3) + _measureParam.GrooveStandardRefs[i].WidthStandard).ToArray();
                    _measureResult.GrooveDepthRealListV2 = _measureResult.GrooveResults.Select(x => x.DepthRealV2).ToArray();

                    double[] widthValid = _measureResult.GrooveResults.Where(x => x.WidthReal >= 0).Select(x => x.WidthReal).ToArray();
                    double[] depthValid = _measureResult.GrooveResults.Where(x => x.DepthReal >= 0).Select(x => x.DepthReal).ToArray();
                    double[] widthValidV2 = _measureResult.GrooveResults.Where(x => x.WidthRealV2 >= 0).Select(x => x.WidthRealV2).ToArray();
                    double[] depthValidV2 = _measureResult.GrooveResults.Where(x => x.DepthRealV2 >= 0).Select(x => x.DepthRealV2).ToArray();

                    _measureResult.GrooveWidthRealMean = Mean(widthValid);
                    _measureResult.GrooveWidthRealMax = Max(widthValid);
                    _measureResult.GrooveWidthRealMin = Min(widthValid);
                    _measureResult.GrooveWidthRealMeanV2 = Mean(widthValidV2);
                    _measureResult.GrooveWidthRealMaxV2 = Max(widthValidV2);
                    _measureResult.GrooveWidthRealMinV2 = Min(widthValidV2);

                    _measureResult.GrooveDepthRealMean = Mean(depthValid);
                    _measureResult.GrooveDepthRealMax = Max(depthValid);
                    _measureResult.GrooveDepthRealMin = Min(depthValid);
                    _measureResult.GrooveDepthRealMeanV2 = Mean(depthValidV2);
                    _measureResult.GrooveDepthRealMaxV2 = Max(depthValidV2);
                    _measureResult.GrooveDepthRealMinV2 = Min(depthValidV2);

                    if (_measureResult.GrooveResults.Count == 0)
                    {
                        _measureResult.IsOK = false;
                    }
                }
                finally
                {
                    grooveCenterRows.Dispose();
                    grooveCenterCols.Dispose();
                    intraDistance.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                _measureResult.IsOK = false;
            }
            finally
            {
                hoValidMaskImage?.Dispose();
            }

            return BuildResult();
        }

        public override Mat CvDrawResult(KCJC0_StandardPlateMeasureResult measureResult, bool showGuides = true)
        {
            Mat image = new Mat();

            try
            {
                HobjectToMat(_hoGrayImage, out image);
                if (image.Empty())
                {
                    return image;
                }

                Cv2.CvtColor(image, image, ColorConversionCodes.GRAY2BGR);
                for (int i = 0; i < measureResult.GrooveResults.Count; i++)
                {
                    KCJC0_StandardPlateGrooveResult groove = measureResult.GrooveResults[i];
                    Point center = new Point((int)Math.Round(groove.Center.X), (int)Math.Round(groove.Center.Y));

                    Cv2.DrawMarker(image, center, new Scalar(0, 165, 255), MarkerTypes.Cross, 30, 2);

                    if (showGuides)
                    {
                        Cv2.Line(image, groove.PositiveEdgeLine.StartPoint, groove.PositiveEdgeLine.EndPoint, new Scalar(0, 255, 0), 1);
                        Cv2.Line(image, groove.NegativeEdgeLine.StartPoint, groove.NegativeEdgeLine.EndPoint, new Scalar(255, 0, 0), 1);
                    }

                    string widthText = groove.WidthReal >= 0 ? $"w:{groove.WidthReal:F4}" : "w:?";
                    string depthText = groove.DepthReal >= 0 ? $"d:{groove.DepthReal:F4}" : "d:?";
                    Cv2.PutText(image, widthText, new Point(center.X + 10, center.Y - 10), HersheyFonts.HersheySimplex, 0.6, new Scalar(0, 255, 0), 2);
                    Cv2.PutText(image, depthText, new Point(center.X + 10, center.Y + 15), HersheyFonts.HersheySimplex, 0.6, new Scalar(0, 0, 255), 2);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return image;
        }

        private void FlattenHeightImage(out HObject hoValidMaskImage)
        {
            HObject? hoRectangle = null;
            HObject? hoTmp = null;
            HObject? hoFitSurface = null;

            HOperatorSet.GenEmptyObj(out hoValidMaskImage);

            try
            {
                HOperatorSet.GetImageSize(_hoGrayImage, out HTuple hvWidth, out HTuple hvHeight);
                HOperatorSet.GenRectangle1(out hoRectangle, 0, 0, hvHeight - 1, hvWidth - 1);

                HOperatorSet.Threshold(_hoHeightImage, out hoTmp, InvalidDepthValue - 1, InvalidDepthValue + 1);
                ReplaceHobject(ref _hoIrregularRegion, ref hoTmp);
                HOperatorSet.Difference(hoRectangle, _hoIrregularRegion, out hoTmp);
                ReplaceHobject(ref _hoValidRegion, ref hoTmp);

                HOperatorSet.ReduceDomain(_hoHeightImage, _hoValidRegion, out hoTmp);
                ReplaceHobject(ref _hoHeightImage, ref hoTmp);

                HOperatorSet.MinMaxGray(hoRectangle, _hoHeightImage, 0, out _hvHeightImageMinValue, out _hvHeightImageMaxValue, out HTuple hvRange);
                hvRange.Dispose();

                HOperatorSet.GenImageConst(out hoValidMaskImage, "byte", hvWidth, hvHeight);
                HOperatorSet.PaintRegion(_hoValidRegion, hoValidMaskImage, out hoTmp, 255, "fill");
                ReplaceHobject(ref hoValidMaskImage, ref hoTmp);

                HOperatorSet.AreaCenter(_hoValidRegion, out HTuple hvValidRegionArea, out HTuple hvValidCenterRow, out HTuple hvValidCenterCol);
                HOperatorSet.FitSurfaceFirstOrder(_hoValidRegion, _hoHeightImage, "tukey", 5, 1,
                                                  out HTuple hvAlpha, out HTuple hvBeta, out HTuple hvGamma);
                HOperatorSet.GenImageSurfaceFirstOrder(out hoFitSurface, "real", hvAlpha, hvBeta, hvGamma,
                                                       hvValidCenterRow, hvValidCenterCol, hvWidth, hvHeight);
                HOperatorSet.SubImage(_hoHeightImage, hoFitSurface, out hoTmp, 1, 0);
                ReplaceHobject(ref _hoHeightImage, ref hoTmp);

                _measureParam.IntervalX = Math.Sqrt(_measureParam.IntervalX * _measureParam.IntervalX + (hvBeta.D * _measureParam.IntervalZ) * (hvBeta.D * _measureParam.IntervalZ));
                _measureParam.IntervalY = Math.Sqrt(_measureParam.IntervalY * _measureParam.IntervalY + (hvAlpha.D * _measureParam.IntervalZ) * (hvAlpha.D * _measureParam.IntervalZ));

                hvValidRegionArea.Dispose();
                hvAlpha.Dispose();
                hvBeta.Dispose();
                hvGamma.Dispose();
                hvValidCenterRow.Dispose();
                hvValidCenterCol.Dispose();
                hvWidth.Dispose();
                hvHeight.Dispose();
            }
            finally
            {
                hoRectangle?.Dispose();
                hoTmp?.Dispose();
                hoFitSurface?.Dispose();
            }
        }

        private void DetectGrooveCenters(out HTuple grooveCenterRows, out HTuple grooveCenterCols, out HTuple intraDistance)
        {
            HTuple measureHandle = new HTuple();
            HTuple amplitudeFirst = new HTuple();
            HTuple amplitudeSecond = new HTuple();
            HTuple interDistance = new HTuple();
            HTuple rowEdgeFirst = new HTuple();
            HTuple rowEdgeSecond = new HTuple();
            HTuple colEdgeFirst = new HTuple();
            HTuple colEdgeSecond = new HTuple();

            grooveCenterRows = new HTuple();
            grooveCenterCols = new HTuple();
            intraDistance = new HTuple();

            try
            {
                HOperatorSet.GetImageSize(_hoGrayImage, out HTuple hvWidth, out HTuple hvHeight);
                HTuple imageCenterR = hvHeight * 0.5;
                HTuple imageCenterC = hvWidth * 0.5;

                HOperatorSet.GenMeasureRectangle2(imageCenterR, imageCenterC, (new HTuple(0)).TupleRad(),
                                                  hvWidth * 0.5 - _measureParam.ImageEdgeMaskSize, 10,
                                                  hvWidth, hvHeight, "bilinear", out measureHandle);
                HOperatorSet.MeasurePairs(_hoHeightImage, measureHandle,
                                          _measureParam.HeightAmplitudeSigma, _measureParam.HeightAmplitudeThreshold,
                                          "all", "all",
                                          out rowEdgeFirst, out colEdgeFirst, out amplitudeFirst,
                                          out rowEdgeSecond, out colEdgeSecond, out amplitudeSecond,
                                          out intraDistance, out interDistance);

                grooveCenterRows = (rowEdgeFirst + rowEdgeSecond) * 0.5;
                grooveCenterCols = (colEdgeFirst + colEdgeSecond) * 0.5;

                hvWidth.Dispose();
                hvHeight.Dispose();
                imageCenterR.Dispose();
                imageCenterC.Dispose();
            }
            finally
            {
                if (measureHandle.Length > 0)
                {
                    HOperatorSet.CloseMeasure(measureHandle);
                }

                measureHandle.Dispose();
                amplitudeFirst.Dispose();
                amplitudeSecond.Dispose();
                interDistance.Dispose();
                rowEdgeFirst.Dispose();
                rowEdgeSecond.Dispose();
                colEdgeFirst.Dispose();
                colEdgeSecond.Dispose();
            }
        }

        private double EstimateGroovePhi(HTuple grooveCenterRows, HTuple grooveCenterCols)
        {
            HObject? fitLine = null;
            HTuple metrologyHandle = new HTuple();
            HTuple index = new HTuple();
            HTuple edgeRows = new HTuple();
            HTuple edgeCols = new HTuple();

            try
            {
                int selectIndex = Math.Clamp(1, 0, grooveCenterRows.Length - 1);
                HTuple grooveCenterRow = grooveCenterRows[selectIndex];
                HTuple grooveCenterCol = grooveCenterCols[selectIndex];
                HTuple measureBeginRow = grooveCenterRow - _measureParam.MeasureLength1;
                HTuple measureBeginCol = grooveCenterCol;
                HTuple measureEndRow = grooveCenterRow + _measureParam.MeasureLength1;
                HTuple measureEndCol = grooveCenterCol;

                HOperatorSet.GetImageSize(_hoGrayImage, out HTuple hvWidth, out HTuple hvHeight);
                HOperatorSet.CreateMetrologyModel(out metrologyHandle);
                HOperatorSet.SetMetrologyModelImageSize(metrologyHandle, hvWidth, hvHeight);
                HOperatorSet.AddMetrologyObjectLineMeasure(metrologyHandle,
                                                           measureBeginRow, measureBeginCol, measureEndRow, measureEndCol,
                                                           _measureParam.MeasureLength1, _measureParam.MeasureLength2,
                                                           _measureParam.HeightAmplitudeSigma, _measureParam.HeightAmplitudeThreshold,
                                                           new HTuple(), new HTuple(), out index);
                HOperatorSet.SetMetrologyObjectParam(metrologyHandle, "all", "measure_transition", "positive");
                HOperatorSet.SetMetrologyObjectParam(metrologyHandle, "all", "num_measures", _measureParam.NumMeasures);
                HOperatorSet.SetMetrologyObjectParam(metrologyHandle, "all", "measure_select", "first");
                HOperatorSet.SetMetrologyObjectParam(metrologyHandle, "all", "num_instances", (int)(_measureParam.NumMeasures * 0.25));
                HOperatorSet.SetMetrologyObjectParam(metrologyHandle, "all", "min_score", 0.5);

                HOperatorSet.ApplyMetrologyModel(_hoHeightImage, metrologyHandle);
                HOperatorSet.GetMetrologyObjectMeasures(out HObject contours, metrologyHandle, index, "all", out edgeRows, out edgeCols);
                contours.Dispose();
                HOperatorSet.GetMetrologyObjectResultContour(out fitLine, metrologyHandle, index, "all", 1.5);

                HOperatorSet.CountObj(fitLine, out HTuple fitLineCount);
                try
                {
                    if (fitLineCount.I > 0)
                    {
                        HOperatorSet.FitLineContourXld(fitLine, "tukey", -1, 0, 5, 2,
                                                       out HTuple rowBegin, out HTuple colBegin, out HTuple rowEnd, out HTuple colEnd,
                                                       out HTuple nr, out HTuple nc, out HTuple dist);
                        try
                        {
                            HOperatorSet.AngleLx(rowBegin, colBegin, rowEnd, colEnd, out HTuple phi);
                            try
                            {
                                return ((new HTuple(90)).TupleRad() + phi).D;
                            }
                            finally
                            {
                                phi.Dispose();
                            }
                        }
                        finally
                        {
                            rowBegin.Dispose();
                            colBegin.Dispose();
                            rowEnd.Dispose();
                            colEnd.Dispose();
                            nr.Dispose();
                            nc.Dispose();
                            dist.Dispose();
                        }
                    }
                }
                finally
                {
                    fitLineCount.Dispose();
                    hvWidth.Dispose();
                    hvHeight.Dispose();
                    grooveCenterRow.Dispose();
                    grooveCenterCol.Dispose();
                    measureBeginRow.Dispose();
                    measureBeginCol.Dispose();
                    measureEndRow.Dispose();
                    measureEndCol.Dispose();
                }
            }
            finally
            {
                fitLine?.Dispose();
                if (metrologyHandle.Length > 0)
                {
                    HOperatorSet.ClearMetrologyModel(metrologyHandle);
                }

                metrologyHandle.Dispose();
                index.Dispose();
                edgeRows.Dispose();
                edgeCols.Dispose();
            }

            return (new HTuple(90)).TupleRad().D;
        }

        private bool MeasureGrooveWidth(double beginRow, double beginCol, double endRow, double endCol, double measureLength3,
                                        out double widthReal, out Line positiveEdgeLine, out Line negativeEdgeLine)
        {
            HObject? fitLine0 = null;
            HObject? fitLine1 = null;
            HObject? contours0 = null;
            HObject? contours1 = null;

            HTuple metrologyHandle = new HTuple();
            HTuple index0 = new HTuple();
            HTuple index1 = new HTuple();
            HTuple parameter0 = new HTuple();
            HTuple parameter1 = new HTuple();
            HTuple posEdgeRows = new HTuple();
            HTuple posEdgeCols = new HTuple();
            HTuple negEdgeRows = new HTuple();
            HTuple negEdgeCols = new HTuple();

            widthReal = -1;
            positiveEdgeLine = new Line(new Point2d(0, 0), new Point2d(0, 0));
            negativeEdgeLine = new Line(new Point2d(0, 0), new Point2d(0, 0));

            try
            {
                HOperatorSet.GetImageSize(_hoGrayImage, out HTuple hvWidth, out HTuple hvHeight);
                HOperatorSet.CreateMetrologyModel(out metrologyHandle);
                HOperatorSet.SetMetrologyModelImageSize(metrologyHandle, hvWidth, hvHeight);

                HOperatorSet.AddMetrologyObjectLineMeasure(metrologyHandle,
                                                           beginRow, beginCol, endRow, endCol,
                                                           measureLength3, _measureParam.MeasureLength2,
                                                           _measureParam.HeightAmplitudeSigma, _measureParam.HeightAmplitudeThreshold,
                                                           new HTuple(), new HTuple(), out index0);
                HOperatorSet.AddMetrologyObjectLineMeasure(metrologyHandle,
                                                           endRow, endCol, beginRow, beginCol,
                                                           measureLength3, _measureParam.MeasureLength2,
                                                           _measureParam.HeightAmplitudeSigma, _measureParam.HeightAmplitudeThreshold,
                                                           new HTuple(), new HTuple(), out index1);
                HOperatorSet.SetMetrologyObjectParam(metrologyHandle, "all", "measure_transition", "positive");
                HOperatorSet.SetMetrologyObjectParam(metrologyHandle, "all", "num_measures", _measureParam.NumMeasures);
                HOperatorSet.SetMetrologyObjectParam(metrologyHandle, "all", "measure_select", "first");
                HOperatorSet.SetMetrologyObjectParam(metrologyHandle, "all", "num_instances", (int)(_measureParam.NumMeasures * 0.25));
                HOperatorSet.SetMetrologyObjectParam(metrologyHandle, "all", "min_score", 0.5);

                HOperatorSet.ApplyMetrologyModel(_hoHeightImage, metrologyHandle);
                HOperatorSet.GetMetrologyObjectMeasures(out contours0, metrologyHandle, index0, "all", out posEdgeRows, out posEdgeCols);
                HOperatorSet.GetMetrologyObjectMeasures(out contours1, metrologyHandle, index1, "all", out negEdgeRows, out negEdgeCols);
                HOperatorSet.GetMetrologyObjectResult(metrologyHandle, index0, "all", "result_type", "all_param", out parameter0);
                HOperatorSet.GetMetrologyObjectResult(metrologyHandle, index1, "all", "result_type", "all_param", out parameter1);
                HOperatorSet.GetMetrologyObjectResultContour(out fitLine0, metrologyHandle, index0, "all", 1.5);
                HOperatorSet.GetMetrologyObjectResultContour(out fitLine1, metrologyHandle, index1, "all", 1.5);

                if (parameter0.Length < 4 || parameter1.Length < 4)
                {
                    return false;
                }

                HOperatorSet.DistancePl(parameter0[0], parameter0[1], parameter1[0], parameter1[1], parameter1[2], parameter1[3], out HTuple distance0);
                HOperatorSet.DistancePl(parameter0[2], parameter0[3], parameter1[0], parameter1[1], parameter1[2], parameter1[3], out HTuple distance1);
                HOperatorSet.DistancePl(parameter1[0], parameter1[1], parameter0[0], parameter0[1], parameter0[2], parameter0[3], out HTuple distance2);
                HOperatorSet.DistancePl(parameter1[2], parameter1[3], parameter0[0], parameter0[1], parameter0[2], parameter0[3], out HTuple distance3);

                try
                {
                    double widthMean = new[] { distance0.D, distance1.D, distance2.D, distance3.D }.Average();
                    widthReal = widthMean * _measureParam.IntervalX;
                }
                finally
                {
                    distance0.Dispose();
                    distance1.Dispose();
                    distance2.Dispose();
                    distance3.Dispose();
                }

                positiveEdgeLine = CreateLine(parameter0[0], parameter0[1], parameter0[2], parameter0[3]);
                negativeEdgeLine = CreateLine(parameter1[0], parameter1[1], parameter1[2], parameter1[3]);
                return true;
            }
            finally
            {
                fitLine0?.Dispose();
                fitLine1?.Dispose();
                contours0?.Dispose();
                contours1?.Dispose();

                if (metrologyHandle.Length > 0)
                {
                    HOperatorSet.ClearMetrologyModel(metrologyHandle);
                }

                metrologyHandle.Dispose();
                index0.Dispose();
                index1.Dispose();
                parameter0.Dispose();
                parameter1.Dispose();
                posEdgeRows.Dispose();
                posEdgeCols.Dispose();
                negEdgeRows.Dispose();
                negEdgeCols.Dispose();
            }
        }

        private double MeasureGrooveDepth(HObject hoValidMaskImage, double beginRow, double beginCol, double endRow, double endCol, double phi, double measureLength3,
                                          out double widthRealV2, out double depthRealV2)
        {
            List<double> depthListRealSingle = new List<double>();
            List<double> widthListRealSingleV2 = new List<double>();
            List<double> depthListRealSingleV2 = new List<double>();

            HOperatorSet.GetImageSize(_hoGrayImage, out HTuple hvWidth, out HTuple hvHeight);
            int imageWidth = hvWidth.I;
            int imageHeight = hvHeight.I;
            hvWidth.Dispose();
            hvHeight.Dispose();

            double deltaRow = endRow - beginRow;
            double deltaCol = endCol - beginCol;

            for (int i = 0; i < _measureParam.NumMeasures; i++)
            {
                double t = (i + 0.5) / _measureParam.NumMeasures;
                double rowCenter = beginRow + t * deltaRow;
                double colCenter = beginCol + t * deltaCol;

                GrooveProjectionProfile grooveProjectionProfile = BuildGrooveProjectionProfile(hoValidMaskImage, rowCenter, colCenter, phi, measureLength3, imageWidth, imageHeight);
                if (grooveProjectionProfile.ProfileXValid.Length == 0 || grooveProjectionProfile.ProfileYValid.Length == 0)
                {
                    continue;
                }

                double grooveDepthReal = AnalyzeGrooveDepthProfile(grooveProjectionProfile.ProfileXValid, grooveProjectionProfile.ProfileYValid);
                if (grooveDepthReal >= 0)
                {
                    depthListRealSingle.Add(grooveDepthReal);
                }

                GrooveProfileV2Analysis grooveProfileV2Analysis = AnalyzeGrooveProfileV2(grooveProjectionProfile.ProfileXValid, grooveProjectionProfile.ProfileYValid);
                if (grooveProfileV2Analysis.WidthRealV2 >= 0)
                {
                    widthListRealSingleV2.Add(grooveProfileV2Analysis.WidthRealV2);
                }

                if (grooveProfileV2Analysis.DepthRealV2 >= 0)
                {
                    depthListRealSingleV2.Add(grooveProfileV2Analysis.DepthRealV2);
                }

                
                        // 仅保留图像范围内且在 ValidRegion 内的有效 Projection 值
            }

            widthRealV2 = AggregateGrooveMeasurements(widthListRealSingleV2);
            depthRealV2 = AggregateGrooveMeasurements(depthListRealSingleV2);
            return AggregateGrooveMeasurements(depthListRealSingle);
        }

        private GrooveProjectionProfile BuildGrooveProjectionProfile(HObject hoValidMaskImage, double rowCenter, double colCenter, double phi, double measureLength3,
                                                                     int imageWidth, int imageHeight)
        {
            HTuple hvMeasureHandle = new HTuple();
            List<double> profileXValid = new List<double>();
            List<double> profileYValid = new List<double>();

            try
            {
                HOperatorSet.GenMeasureRectangle2(rowCenter, colCenter, phi, measureLength3, _measureParam.MeasureLength2,
                                                  imageWidth, imageHeight, "bilinear", out hvMeasureHandle);
                HOperatorSet.MeasureProjection(_hoHeightImage, hvMeasureHandle, out HTuple hvProjection);
                try
                {
                    int projectionLength = hvProjection.Length;
                    for (int j = 0; j < projectionLength; j++)
                    {
                        double offset = j - (projectionLength - 1) * 0.5;
                        double sampleRow = rowCenter + offset * Math.Sin(phi);
                        double sampleCol = colCenter + offset * Math.Cos(phi);
                        if (sampleRow < 0 || sampleRow > imageHeight - 1 || sampleCol < 0 || sampleCol > imageWidth - 1)
                        {
                            continue;
                        }

                        HOperatorSet.GetGrayval(hoValidMaskImage, sampleRow, sampleCol, out HTuple hvMaskValue);
                        try
                        {
                            if (hvMaskValue.Length > 0 && hvMaskValue.D > 0)
                            {
                                profileXValid.Add(j * _measureParam.IntervalX);
                                profileYValid.Add(hvProjection[j].D * _measureParam.IntervalZ * -1.0);
                            }
                        }
                        finally
                        {
                            hvMaskValue.Dispose();
                        }
                    }
                }
                finally
                {
                    hvProjection.Dispose();
                }
            }
            finally
            {
                if (hvMeasureHandle.Length > 0)
                {
                    HOperatorSet.CloseMeasure(hvMeasureHandle);
                }

                hvMeasureHandle.Dispose();
            }

            return new GrooveProjectionProfile
            {
                ProfileXValid = profileXValid.ToArray(),
                ProfileYValid = profileYValid.ToArray()
            };
        }

        private static double AnalyzeGrooveDepthProfile(double[] profileXValid, double[] profileYValid)
        {
            if (profileXValid.Length < 30 || profileYValid.Length < 30 || profileXValid.Length != profileYValid.Length)
            {
                return -1;
            }

            double[] profileYSorted = profileYValid.OrderBy(x => x).ToArray();
            int numValidProfile = profileYSorted.Length;
            int surfaceCount = (int)(numValidProfile * 0.20);
            if (surfaceCount < 3)
            {
                surfaceCount = 3;
            }

            int grooveCount = (int)(numValidProfile * 0.05);
            if (grooveCount < 3)
            {
                grooveCount = 3;
            }

            if (surfaceCount >= numValidProfile)
            {
                surfaceCount = numValidProfile - 1;
            }

            if (grooveCount >= numValidProfile)
            {
                grooveCount = numValidProfile - 1;
            }

            if (surfaceCount <= 0 || grooveCount <= 0)
            {
                return -1;
            }

            int endCountDepth = (int)(numValidProfile * 0.10);
            if (endCountDepth < 3)
            {
                endCountDepth = 3;
            }

            if (2 * endCountDepth >= numValidProfile)
            {
                endCountDepth = (int)(numValidProfile / 4.0);
            }

            if (endCountDepth < 1)
            {
                endCountDepth = 1;
            }

            double[] leftDepthEndY = SelectRange(profileYValid, 0, endCountDepth - 1);
            double[] rightDepthEndY = SelectRange(profileYValid, numValidProfile - endCountDepth, numValidProfile - 1);
            double surfaceHintMean = Mean(leftDepthEndY.Concat(rightDepthEndY));

            int midStartDepth = (int)(numValidProfile * 0.45);
            int midEndDepth = (int)(numValidProfile * 0.55);
            if (midEndDepth <= midStartDepth)
            {
                midStartDepth = (int)(numValidProfile / 3.0);
                midEndDepth = (int)(numValidProfile * 2.0 / 3.0);
            }

            double[] centerDepthSampleY = SelectRange(profileYValid, midStartDepth, midEndDepth);
            double grooveHintMean = Mean(centerDepthSampleY);

            List<int> grooveIndices = new List<int>();
            if (grooveHintMean >= surfaceHintMean)
            {
                double[] surfaceCandidateValues = SelectRange(profileYSorted, 0, surfaceCount - 1);
                double[] grooveCandidateValues = SelectRange(profileYSorted, numValidProfile - grooveCount, numValidProfile - 1);
                double grooveThreshold = (Mean(surfaceCandidateValues) + Mean(grooveCandidateValues)) * 0.5;
                for (int k = 0; k < profileYValid.Length; k++)
                {
                    if (profileYValid[k] > grooveThreshold)
                    {
                        grooveIndices.Add(k);
                    }
                }
            }
            else
            {
                double[] surfaceCandidateValues = SelectRange(profileYSorted, numValidProfile - surfaceCount, numValidProfile - 1);
                double[] grooveCandidateValues = SelectRange(profileYSorted, 0, grooveCount - 1);
                double grooveThreshold = (Mean(surfaceCandidateValues) + Mean(grooveCandidateValues)) * 0.5;
                for (int k = 0; k < profileYValid.Length; k++)
                {
                    if (profileYValid[k] < grooveThreshold)
                    {
                        grooveIndices.Add(k);
                    }
                }
            }

            if (grooveIndices.Count < 5)
            {
                return -1;
            }

            int grooveLeft = grooveIndices.First();
            int grooveRight = grooveIndices.Last();
            const int margin = 5;

            List<double> surfaceX = new List<double>();
            List<double> surfaceY = new List<double>();
            if (grooveLeft - margin >= 0)
            {
                for (int k = 0; k <= grooveLeft - margin; k++)
                {
                    surfaceX.Add(profileXValid[k]);
                    surfaceY.Add(profileYValid[k]);
                }
            }

            if (grooveRight + margin <= profileYValid.Length - 1)
            {
                for (int k = grooveRight + margin; k <= profileYValid.Length - 1; k++)
                {
                    surfaceX.Add(profileXValid[k]);
                    surfaceY.Add(profileYValid[k]);
                }
            }

            if (surfaceX.Count < 2)
            {
                return -1;
            }

            double[] grooveX = SelectRange(profileXValid, grooveLeft, grooveRight);
            double[] grooveY = SelectRange(profileYValid, grooveLeft, grooveRight);
            if (!TryFitLine(surfaceX.ToArray(), surfaceY.ToArray(), out FitLineResult fitLineResult))
            {
                return -1;
            }

            if (!TryDistancePointsToLine(grooveX, grooveY, fitLineResult, out double grooveDepthPhysical))
            {
                return -1;
            }

            return grooveDepthPhysical;
        }

        private static GrooveProfileV2Analysis AnalyzeGrooveProfileV2(double[] profileXValid, double[] profileYValid)
        {
            if (profileXValid.Length < 30 || profileYValid.Length < 30 || profileXValid.Length != profileYValid.Length)
            {
                return GrooveProfileV2Analysis.Invalid;
            }

            int numProfile = profileXValid.Length;
            const int smoothRadius = 2;
            double[] profileYSeg = new double[numProfile];
            for (int i = 0; i < numProfile; i++)
            {
                int winStart = Math.Max(i - smoothRadius, 0);
                int winEnd = Math.Min(i + smoothRadius, numProfile - 1);
                profileYSeg[i] = ComputeMedian(SelectRange(profileYValid, winStart, winEnd));
            }

            int endCount = (int)(numProfile * 0.10);
            if (endCount < 5)
            {
                endCount = 5;
            }

            if (2 * endCount >= numProfile)
            {
                endCount = (int)(numProfile / 4.0);
            }

            if (endCount < 1)
            {
                return GrooveProfileV2Analysis.Invalid;
            }

            double[] leftEndY = SelectRange(profileYSeg, 0, endCount - 1);
            double[] rightEndY = SelectRange(profileYSeg, numProfile - endCount, numProfile - 1);
            double surfaceMean = Mean(leftEndY.Concat(rightEndY));

            int midStart = (int)(numProfile * 0.45);
            int midEnd = (int)(numProfile * 0.55);
            if (midEnd <= midStart)
            {
                midStart = (int)(numProfile / 3.0);
                midEnd = (int)(numProfile * 2.0 / 3.0);
            }

            double[] centerSampleY = SelectRange(profileYSeg, midStart, midEnd);
            double grooveMean = Mean(centerSampleY);
            double levelUpper = surfaceMean + (grooveMean - surfaceMean) * 0.10;
            double levelLower = surfaceMean + (grooveMean - surfaceMean) * 0.90;

            int leftUpperIdx = -1;
            int leftLowerIdx = -1;
            int rightLowerIdx = -1;
            int rightUpperIdx = -1;
            bool grooveIsPositive = grooveMean > surfaceMean;

            for (int i = 0; i < numProfile; i++)
            {
                if (leftUpperIdx < 0 && CompareLevel(profileYSeg[i], levelUpper, grooveIsPositive))
                {
                    leftUpperIdx = i;
                }

                if (leftLowerIdx < 0 && CompareLevel(profileYSeg[i], levelLower, grooveIsPositive))
                {
                    leftLowerIdx = i;
                }
            }

            for (int i = numProfile - 1; i >= 0; i--)
            {
                if (rightLowerIdx < 0 && CompareLevel(profileYSeg[i], levelLower, grooveIsPositive))
                {
                    rightLowerIdx = i;
                }

                if (rightUpperIdx < 0 && CompareLevel(profileYSeg[i], levelUpper, grooveIsPositive))
                {
                    rightUpperIdx = i;
                }
            }

            if (!(leftUpperIdx > 0 && leftLowerIdx > leftUpperIdx &&
                  rightLowerIdx > leftLowerIdx && rightUpperIdx > rightLowerIdx &&
                  rightUpperIdx < numProfile - 1))
            {
                return GrooveProfileV2Analysis.Invalid;
            }

            const int fitMargin = 0;
            int lt0 = 0;
            int lt1 = leftUpperIdx - fitMargin;
            int ls0 = leftUpperIdx - fitMargin;
            int ls1 = leftLowerIdx + fitMargin;
            int b0 = leftLowerIdx + fitMargin;
            int b1 = rightLowerIdx - fitMargin;
            int rs0 = rightLowerIdx - fitMargin;
            int rs1 = rightUpperIdx + fitMargin;
            int rt0 = rightUpperIdx + fitMargin;
            int rt1 = numProfile - 1;

            const int minFitPoints = 3;
            if (!HasMinFitPoints(lt0, lt1, minFitPoints) ||
                !HasMinFitPoints(ls0, ls1, minFitPoints) ||
                !HasMinFitPoints(b0, b1, minFitPoints) ||
                !HasMinFitPoints(rs0, rs1, minFitPoints) ||
                !HasMinFitPoints(rt0, rt1, minFitPoints))
            {
                return GrooveProfileV2Analysis.Invalid;
            }

            double[] xLt = SelectRange(profileXValid, lt0, lt1);
            double[] yLt = SelectRange(profileYValid, lt0, lt1);
            double[] xLs = SelectRange(profileXValid, ls0, ls1);
            double[] yLs = SelectRange(profileYValid, ls0, ls1);
            double[] xB = SelectRange(profileXValid, b0, b1);
            double[] yB = SelectRange(profileYValid, b0, b1);
            double[] xRs = SelectRange(profileXValid, rs0, rs1);
            double[] yRs = SelectRange(profileYValid, rs0, rs1);
            double[] xRt = SelectRange(profileXValid, rt0, rt1);
            double[] yRt = SelectRange(profileYValid, rt0, rt1);

            if (!TryFitLine(xLt, yLt, out FitLineResult ltLine) ||
                !TryFitLine(xLs, yLs, out FitLineResult lsLine) ||
                !TryFitLine(xB, yB, out FitLineResult bLine) ||
                !TryFitLine(xRs, yRs, out FitLineResult rsLine) ||
                !TryFitLine(xRt, yRt, out FitLineResult rtLine))
            {
                return GrooveProfileV2Analysis.Invalid;
            }

            if (!TryIntersectLines(ltLine, lsLine, out Point2d corner1) ||
                !TryIntersectLines(lsLine, bLine, out Point2d corner2) ||
                !TryIntersectLines(bLine, rsLine, out Point2d corner3) ||
                !TryIntersectLines(rsLine, rtLine, out Point2d corner4))
            {
                return GrooveProfileV2Analysis.Invalid;
            }

            double upWidthDistance = Math.Sqrt(Math.Pow(corner4.X - corner1.X, 2) + Math.Pow(corner4.Y - corner1.Y, 2));
            double downWidthDistance = Math.Sqrt(Math.Pow(corner3.X - corner2.X, 2) + Math.Pow(corner3.Y - corner2.Y, 2));
            double widthRealV2 = (upWidthDistance + downWidthDistance) * 0.5;

            double[] topFlatX = xLt.Concat(xRt).ToArray();
            double[] topFlatY = yLt.Concat(yRt).ToArray();
            double depthRealV2 = -1;
            if (TryFitLine(topFlatX, topFlatY, out FitLineResult topFlatLine) &&
                TryDistancePointToLine((bLine.RowBegin + bLine.RowEnd) * 0.5, (bLine.ColBegin + bLine.ColEnd) * 0.5, topFlatLine, out double tmpH))
            {
                depthRealV2 = tmpH;
            }

            return new GrooveProfileV2Analysis
            {
                WidthRealV2 = widthRealV2,
                DepthRealV2 = depthRealV2
            };
        }

        private static bool CompareLevel(double value, double level, bool grooveIsPositive)
        {
            return grooveIsPositive ? value > level : value < level;
        }

        private static bool HasMinFitPoints(int startIndex, int endIndex, int minFitPoints)
        {
            return endIndex - startIndex + 1 >= minFitPoints;
        }

        private static double[] SelectRange(double[] values, int startIndex, int endIndex)
        {
            if (values.Length == 0)
            {
                return Array.Empty<double>();
            }

            int clampedStart = Math.Max(startIndex, 0);
            int clampedEnd = Math.Min(endIndex, values.Length - 1);
            if (clampedEnd < clampedStart)
            {
                return Array.Empty<double>();
            }

            int length = clampedEnd - clampedStart + 1;
            double[] result = new double[length];
            Array.Copy(values, clampedStart, result, 0, length);
            return result;
        }

        private static double ComputeMedian(double[] values)
        {
            if (values.Length == 0)
            {
                return -1;
            }

            double[] sorted = values.OrderBy(x => x).ToArray();
            int middleIndex = sorted.Length / 2;
            if (sorted.Length % 2 == 1)
            {
                return sorted[middleIndex];
            }

            return (sorted[middleIndex - 1] + sorted[middleIndex]) * 0.5;
        }

        private static bool TryFitLine(double[] xValues, double[] yValues, out FitLineResult fitLineResult)
        {
            fitLineResult = FitLineResult.Invalid;
            if (xValues.Length < 2 || yValues.Length < 2 || xValues.Length != yValues.Length)
            {
                return false;
            }

            HObject? hoContour = null;
            HTuple hvRows = new HTuple(yValues);
            HTuple hvCols = new HTuple(xValues);
            try
            {
                HOperatorSet.GenContourPolygonXld(out hoContour, hvRows, hvCols);
                HOperatorSet.FitLineContourXld(hoContour, "tukey", -1, 0, 5, 2,
                                               out HTuple hvRowBegin, out HTuple hvColBegin, out HTuple hvRowEnd, out HTuple hvColEnd,
                                               out HTuple hvNr, out HTuple hvNc, out HTuple hvDist);
                try
                {
                    fitLineResult = new FitLineResult(hvRowBegin.D, hvColBegin.D, hvRowEnd.D, hvColEnd.D);
                    return true;
                }
                finally
                {
                    hvRowBegin.Dispose();
                    hvColBegin.Dispose();
                    hvRowEnd.Dispose();
                    hvColEnd.Dispose();
                    hvNr.Dispose();
                    hvNc.Dispose();
                    hvDist.Dispose();
                }
            }
            catch (HOperatorException)
            {
                return false;
            }
            finally
            {
                hvRows.Dispose();
                hvCols.Dispose();
                hoContour?.Dispose();
            }
        }

        private static bool TryIntersectLines(FitLineResult firstLine, FitLineResult secondLine, out Point2d intersectionPoint)
        {
            intersectionPoint = new Point2d(double.NaN, double.NaN);
            try
            {
                HOperatorSet.IntersectionLines(firstLine.RowBegin, firstLine.ColBegin, firstLine.RowEnd, firstLine.ColEnd,
                                               secondLine.RowBegin, secondLine.ColBegin, secondLine.RowEnd, secondLine.ColEnd,
                                               out HTuple hvRow, out HTuple hvCol, out HTuple hvIsOverlapping);
                try
                {
                    if (hvRow.Length == 0 || hvCol.Length == 0)
                    {
                        return false;
                    }

                    intersectionPoint = new Point2d(hvCol.D, hvRow.D);
                    return true;
                }
                finally
                {
                    hvRow.Dispose();
                    hvCol.Dispose();
                    hvIsOverlapping.Dispose();
                }
            }
            catch (HOperatorException)
            {
                return false;
            }
        }

        private static bool TryDistancePointToLine(double row, double col, FitLineResult fitLineResult, out double distance)
        {
            distance = -1;
            try
            {
                HOperatorSet.DistancePl(row, col, fitLineResult.RowBegin, fitLineResult.ColBegin, fitLineResult.RowEnd, fitLineResult.ColEnd, out HTuple hvDistance);
                try
                {
                    distance = hvDistance.D;
                    return distance >= 0;
                }
                finally
                {
                    hvDistance.Dispose();
                }
            }
            catch (HOperatorException)
            {
                return false;
            }
        }

        private static bool TryDistancePointsToLine(double[] grooveX, double[] grooveY, FitLineResult fitLineResult, out double maxDistance)
        {
            maxDistance = -1;
            if (grooveX.Length == 0 || grooveY.Length == 0 || grooveX.Length != grooveY.Length)
            {
                return false;
            }

            HTuple hvRows = new HTuple(grooveY);
            HTuple hvCols = new HTuple(grooveX);
            try
            {
                HOperatorSet.DistancePl(hvRows, hvCols, fitLineResult.RowBegin, fitLineResult.ColBegin, fitLineResult.RowEnd, fitLineResult.ColEnd, out HTuple hvDistances);
                try
                {
                    maxDistance = hvDistances.TupleMax().D;
                    return maxDistance >= 0;
                }
                finally
                {
                    hvDistances.Dispose();
                }
            }
            catch (HOperatorException)
            {
                return false;
            }
            finally
            {
                hvRows.Dispose();
                hvCols.Dispose();
            }
        }

        private static double AggregateGrooveMeasurements(List<double> measurements)
        {
            if (measurements.Count == 0)
            {
                return -1;
            }

            double[] sorted = measurements.OrderBy(x => x).ToArray();
            if (sorted.Length >= 5)
            {
                int trimCount = (int)(sorted.Length * 0.20);
                if (trimCount < 1)
                {
                    trimCount = 1;
                }

                int startIndex = trimCount;
                int endIndex = sorted.Length - 1 - trimCount;
                if (endIndex >= startIndex)
                {
                    return sorted.Skip(startIndex).Take(endIndex - startIndex + 1).Average();
                }
            }

            return ComputeScriptMedian(sorted);
        }

        private sealed class GrooveProjectionProfile
        {
            public double[] ProfileXValid { get; init; } = Array.Empty<double>();

            public double[] ProfileYValid { get; init; } = Array.Empty<double>();
        }

        private sealed class GrooveProfileV2Analysis
        {
            public static GrooveProfileV2Analysis Invalid { get; } = new GrooveProfileV2Analysis();

            public double WidthRealV2 { get; init; } = -1;

            public double DepthRealV2 { get; init; } = -1;
        }

        private readonly struct FitLineResult
        {
            public static FitLineResult Invalid { get; } = new FitLineResult(double.NaN, double.NaN, double.NaN, double.NaN);

            public FitLineResult(double rowBegin, double colBegin, double rowEnd, double colEnd)
            {
                RowBegin = rowBegin;
                ColBegin = colBegin;
                RowEnd = rowEnd;
                ColEnd = colEnd;
            }

            public double RowBegin { get; }

            public double ColBegin { get; }

            public double RowEnd { get; }

            public double ColEnd { get; }
        }
    }
}
