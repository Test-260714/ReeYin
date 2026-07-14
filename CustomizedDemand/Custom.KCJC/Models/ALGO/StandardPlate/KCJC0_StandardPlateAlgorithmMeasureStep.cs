using HalconDotNet;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Custom.KCJC.Models.StandardPlate
{
    public class KCJC0_StandardPlateAlgorithmMeasureStep : KCJC0_StandardPlateAlgorithm
    {
        // 阶梯标准块的扫描方向必须由地阶梯向高阶梯方向扫描
        
        private const double InvalidDepthValue = 888888.0;
        private const double StepRegionOffset = 100.0;
        private const double StepSurfaceClippingFactor = 2.0;

        private static readonly Scalar StepTopRegionDrawColor = new Scalar(0, 255, 0);
        private static readonly Scalar StepDownRegionDrawColor = new Scalar(255, 0, 0);
        private static readonly Scalar StepReferencePointDrawColor = new Scalar(0, 0, 255);

        private sealed class StepEdgeData
        {
            public double StartRow { get; init; }

            public double StartCol { get; init; }

            public double EndRow { get; init; }

            public double EndCol { get; init; }

            public Line EdgeLine { get; init; } = new Line(new Point2d(0, 0), new Point2d(0, 0));
        }

        public override KCJC0_StandardPlateMeasureResult Process(List<float[]> grayData, List<float[]> heightData, KCJC0_StandardPlateMeasureParam param)
        {
            try
            {
                if (!PrepareInputImages(grayData, heightData, param, applyMedianFilter: false))
                {
                    return BuildResult();
                }

                // RotateImagesForStepMeasurement();

                if (!PrepareStepValidRegion(out double measureRegionTop, out double measureRegionLeft,
                                            out double measureRegionDown, out double measureRegionRight,
                                            out double measureLength1, out double measureLength2))
                {
                    _measureResult.IsOK = false;
                    return BuildResult();
                }

                UpdateDepthMapRange(_hoValidRegion, _hoHeightImage);

                List<StepEdgeData> stepEdges = DetectStepEdges(measureRegionTop, measureRegionLeft, measureRegionDown, measureRegionRight,
                                                               measureLength1, measureLength2);
                if (stepEdges.Count == 0)
                {
                    _measureResult.IsOK = false;
                    return BuildResult();
                }

                MeasureStepHeights(stepEdges, measureRegionTop, measureRegionLeft, measureRegionDown, measureRegionRight);

                _measureResult.StepHeightPhysicalList = _measureResult.StepResults.Select(x => x.HeightPhysical).ToArray();
                double[] validHeights = _measureResult.StepResults
                    .Where(x => x.HeightPhysical >= 0 && double.IsFinite(x.HeightPhysical)).Select(x => x.HeightPhysical).ToArray();

                _measureResult.StepHeightPhysicalMean = Mean(validHeights);
                _measureResult.StepHeightPhysicalMax = Max(validHeights);
                _measureResult.StepHeightPhysicalMin = Min(validHeights);

                if (validHeights.Length == 0)
                {
                    _measureResult.IsOK = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                _measureResult.IsOK = false;
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
                foreach (KCJC0_StandardPlateStepResult step in measureResult.StepResults)
                {
                    if (showGuides)
                    {
                        if (step.StepTopRegionContour.Length > 0)
                        {
                            Cv2.DrawContours(image, new[] { step.StepTopRegionContour }, -1, StepTopRegionDrawColor, 1);
                        }

                        if (step.StepDownRegionContour.Length > 0)
                        {
                            Cv2.DrawContours(image, new[] { step.StepDownRegionContour }, -1, StepDownRegionDrawColor, 1);
                        }

                        Cv2.Line(image, step.StepEdgeLine.StartPoint, step.StepEdgeLine.EndPoint, new Scalar(0, 165, 255), 2);

                        Point referencePoint = new Point(
                            (int)Math.Round(step.ReferencePoint.X),
                            (int)Math.Round(step.ReferencePoint.Y));
                        Cv2.Circle(image, referencePoint, 4, StepReferencePointDrawColor, -1);
                    }

                    Point textPoint = new Point(
                        (int)Math.Round(step.ReferencePoint.X),
                        (int)Math.Round(step.ReferencePoint.Y));
                    textPoint.X = Math.Clamp(textPoint.X + 10, 0, Math.Max(image.Width - 1, 0));
                    textPoint.Y = Math.Clamp(textPoint.Y - 8, 0, Math.Max(image.Height - 1, 0));

                    string heightText = step.HeightPhysical >= 0 ? $"h:{step.HeightPhysical:F4}" : "h:?";
                    Cv2.PutText(image, heightText, textPoint, HersheyFonts.HersheySimplex, 0.65, new Scalar(0, 255, 0), 2);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return image;
        }

        private void RotateImagesForStepMeasurement()
        {
            HObject? tmp = null;

            try
            {
                HOperatorSet.RotateImage(_hoGrayImage, out tmp, -90, "nearest_neighbor");
                ReplaceHobject(ref _hoGrayImage, ref tmp);
                HOperatorSet.RotateImage(_hoHeightImage, out tmp, -90, "nearest_neighbor");
                ReplaceHobject(ref _hoHeightImage, ref tmp);

                HOperatorSet.GetImageSize(_hoGrayImage, out HTuple hvWidth, out HTuple hvHeight);
                hvWidth.Dispose();
                hvHeight.Dispose();
            }
            finally
            {
                tmp?.Dispose();
            }
        }

        private bool PrepareStepValidRegion(out double measureRegionTop, out double measureRegionLeft,
                                            out double measureRegionDown, out double measureRegionRight,
                                            out double measureLength1, out double measureLength2)
        {
            HObject? hoRectangle = null;
            HObject? hoInnerRectangle = null;
            HObject? hoTmp = null;
            HObject? hoValidRegionConn = null;
            HObject? hoSelectedValidRegion = null;
            HObject? hoUnionValidRegion = null;

            HTuple hvRange = new HTuple();

            measureRegionTop = 0;
            measureRegionLeft = 0;
            measureRegionDown = 0;
            measureRegionRight = 0;
            measureLength1 = 250.0;
            measureLength2 = 0;

            try
            {
                HOperatorSet.GetImageSize(_hoGrayImage, out HTuple hvWidth, out HTuple hvHeight);
                try
                {
                    HOperatorSet.GenRectangle1(out hoRectangle, 0, 0, hvHeight - 1, hvWidth - 1);

                    HOperatorSet.Threshold(_hoHeightImage, out hoTmp, InvalidDepthValue - 1, InvalidDepthValue + 1);
                    ReplaceHobject(ref _hoIrregularRegion, ref hoTmp);
                    HOperatorSet.Difference(hoRectangle, _hoIrregularRegion, out hoTmp);
                    ReplaceHobject(ref _hoValidRegion, ref hoTmp);
                    HOperatorSet.ReduceDomain(_hoHeightImage, _hoValidRegion, out hoTmp);
                    ReplaceHobject(ref _hoHeightImage, ref hoTmp);

                    HOperatorSet.MinMaxGray(hoRectangle, _hoHeightImage, 0, out _hvHeightImageMinValue, out _hvHeightImageMaxValue, out hvRange);
                    HOperatorSet.PaintRegion(_hoIrregularRegion, _hoHeightImage, out hoTmp, _hvHeightImageMinValue, "fill");
                    ReplaceHobject(ref _hoHeightImage, ref hoTmp);

                    HOperatorSet.GenRectangle1(out hoInnerRectangle,
                                               _measureParam.ImageEdgeMaskSize, _measureParam.ImageEdgeMaskSize,
                                               hvHeight - 1 - _measureParam.ImageEdgeMaskSize,
                                               hvWidth - 1 - _measureParam.ImageEdgeMaskSize);
                    HOperatorSet.Intersection(_hoValidRegion, hoInnerRectangle, out hoTmp);
                    ReplaceHobject(ref _hoValidRegion, ref hoTmp);

                    HOperatorSet.Connection(_hoValidRegion, out hoValidRegionConn);
                    HOperatorSet.SelectShape(hoValidRegionConn, out hoSelectedValidRegion, "area", "and", 50000.0, 1.0e12);
                    HOperatorSet.CountObj(hoSelectedValidRegion, out HTuple hvSelectedValidRegionCount);
                    try
                    {
                        if (hvSelectedValidRegionCount.I <= 0)
                        {
                            return false;
                        }
                    }
                    finally
                    {
                        hvSelectedValidRegionCount.Dispose();
                    }

                    HOperatorSet.Union1(hoSelectedValidRegion, out hoUnionValidRegion);
                    ReplaceHobject(ref _hoValidRegion, ref hoUnionValidRegion);

                    measureLength2 = Math.Truncate(hvHeight.D * 0.5) - _measureParam.ImageEdgeMaskSize - 1.0;
                    if (measureLength2 <= 0)
                    {
                        return false;
                    }

                    double imageCenterRow = hvHeight.D * 0.5;
                    double imageCenterCol = hvWidth.D * 0.5;
                    measureRegionTop = imageCenterRow - measureLength2;
                    measureRegionLeft = imageCenterCol - measureLength1;
                    measureRegionDown = imageCenterRow + measureLength2;
                    measureRegionRight = imageCenterCol + measureLength1;

                    return true;
                }
                finally
                {
                    hvWidth.Dispose();
                    hvHeight.Dispose();
                }
            }
            finally
            {
                hoRectangle?.Dispose();
                hoInnerRectangle?.Dispose();
                hoTmp?.Dispose();
                hoValidRegionConn?.Dispose();
                hoSelectedValidRegion?.Dispose();
                hoUnionValidRegion?.Dispose();
                hvRange.Dispose();
            }
        }

        private List<StepEdgeData> DetectStepEdges(double measureRegionTop, double measureRegionLeft,
                                                   double measureRegionDown, double measureRegionRight,
                                                   double measureLength1, double measureLength2)
        {
            List<StepEdgeData> stepEdges = new List<StepEdgeData>();

            HTuple measureHandle = new HTuple();
            HTuple rowEdgesTuple = new HTuple();
            HTuple colEdgesTuple = new HTuple();
            HTuple amplitude = new HTuple();
            HTuple distance = new HTuple();

            try
            {
                HOperatorSet.GetImageSize(_hoGrayImage, out HTuple hvWidth, out HTuple hvHeight);
                double imageCenterRow = hvHeight.D * 0.5;
                double imageCenterCol = hvWidth.D * 0.5;

                HOperatorSet.GenMeasureRectangle2(imageCenterRow, imageCenterCol, (new HTuple(-90)).TupleRad(),
                                                  measureLength2, 10,
                                                  hvWidth, hvHeight, "bilinear", out measureHandle);
                HOperatorSet.MeasurePos(_hoHeightImage, measureHandle,
                                        _measureParam.HeightAmplitudeSigma, _measureParam.HeightAmplitudeThreshold,
                                        "positive", "all",
                                        out rowEdgesTuple, out colEdgesTuple, out amplitude, out distance);

                int edgeCount = Math.Min(rowEdgesTuple.Length, colEdgesTuple.Length);
                List<(double Row, double Col)> sortedEdges = Enumerable.Range(0, edgeCount)
                    .Select(index => (Row: rowEdgesTuple[index].D, Col: colEdgesTuple[index].D))
                    .OrderBy(item => item.Row)
                    .ToList();

                for (int index = 0; index < sortedEdges.Count; index++)
                {
                    double rowEdge = sortedEdges[index].Row;
                    double colEdge = sortedEdges[index].Col;

                    double heightTop = index == 0
                        ? (rowEdge - measureRegionTop) * 0.5
                        : (rowEdge - sortedEdges[index - 1].Row) * 0.5;
                    double heightDown = index == sortedEdges.Count - 1
                        ? (measureRegionDown - rowEdge) * 0.5
                        : (sortedEdges[index + 1].Row - rowEdge) * 0.5;
                    double length1 = Math.Min(heightTop, heightDown);

                    if (length1 <= 0)
                    {
                        continue;
                    }

                    if (TryFitSingleStepEdge(rowEdge, colEdge, measureRegionTop, measureRegionLeft,
                                             measureRegionDown, measureRegionRight, measureLength1, length1,
                                             out StepEdgeData? stepEdge))
                    {
                        stepEdges.Add(stepEdge!);
                    }
                }

                hvWidth.Dispose();
                hvHeight.Dispose();
            }
            finally
            {
                if (measureHandle.Length > 0)
                {
                    HOperatorSet.CloseMeasure(measureHandle);
                }

                measureHandle.Dispose();
                rowEdgesTuple.Dispose();
                colEdgesTuple.Dispose();
                amplitude.Dispose();
                distance.Dispose();
            }

            return stepEdges;
        }

        private bool TryFitSingleStepEdge(double rowEdge, double colEdge,
                                          double measureRegionTop, double measureRegionLeft,
                                          double measureRegionDown, double measureRegionRight,
                                          double measureLength1, double length1,
                                          out StepEdgeData? stepEdge)
        {
            HObject? stepEdgeContour = null;
            HObject? fitEdgeContour = null;
            HObject? contours = null;

            HTuple metrologyHandle = new HTuple();
            HTuple index0 = new HTuple();
            HTuple parameter = new HTuple();
            HTuple fitEdgeRows = new HTuple();
            HTuple fitEdgeCols = new HTuple();
            HTuple contourCount = new HTuple();

            stepEdge = null;

            try
            {
                double measureLineStartRow = rowEdge;
                double measureLineStartCol = colEdge - measureLength1;
                double measureLineEndRow = rowEdge;
                double measureLineEndCol = colEdge + measureLength1;

                HOperatorSet.GetImageSize(_hoGrayImage, out HTuple hvWidth, out HTuple hvHeight);
                HOperatorSet.CreateMetrologyModel(out metrologyHandle);
                HOperatorSet.SetMetrologyModelImageSize(metrologyHandle, hvWidth, hvHeight);
                HOperatorSet.AddMetrologyObjectLineMeasure(metrologyHandle,
                                                           measureLineStartRow, measureLineStartCol,
                                                           measureLineEndRow, measureLineEndCol,
                                                           length1, 10,
                                                           _measureParam.HeightAmplitudeSigma, _measureParam.HeightAmplitudeThreshold,
                                                           new HTuple(), new HTuple(), out index0);
                HOperatorSet.SetMetrologyObjectParam(metrologyHandle, "all", "measure_transition", "all");
                HOperatorSet.SetMetrologyObjectParam(metrologyHandle, "all", "num_measures", _measureParam.NumMeasures);
                HOperatorSet.SetMetrologyObjectParam(metrologyHandle, "all", "measure_select", "first");
                HOperatorSet.SetMetrologyObjectParam(metrologyHandle, "all", "num_instances", 1);
                HOperatorSet.SetMetrologyObjectParam(metrologyHandle, "all", "min_score", 0.5);

                HOperatorSet.ApplyMetrologyModel(_hoHeightImage, metrologyHandle);
                HOperatorSet.GetMetrologyObjectMeasures(out contours, metrologyHandle, index0, "all", out fitEdgeRows, out fitEdgeCols);
                HOperatorSet.GetMetrologyObjectResult(metrologyHandle, index0, "all", "result_type", "all_param", out parameter);
                HOperatorSet.GetMetrologyObjectResultContour(out stepEdgeContour, metrologyHandle, index0, "all", 1.5);
                HOperatorSet.CountObj(stepEdgeContour, out contourCount);

                try
                {
                    if (contourCount.I == 1 && parameter.Length >= 4)
                    {
                        if (!TryProjectStepLineToMeasureRegion(measureRegionTop, measureRegionLeft, measureRegionDown, measureRegionRight,
                                                               parameter[0].D, parameter[1].D, parameter[2].D, parameter[3].D,
                                                               out double startRow, out double startCol,
                                                               out double endRow, out double endCol))
                        {
                            return false;
                        }

                        stepEdge = new StepEdgeData
                        {
                            StartRow = startRow,
                            StartCol = startCol,
                            EndRow = endRow,
                            EndCol = endCol,
                            EdgeLine = CreateLine(startRow, startCol, endRow, endCol)
                        };
                        return true;
                    }

                    if (contourCount.I == 0 && fitEdgeRows.Length > 3 && fitEdgeCols.Length > 3)
                    {
                        HOperatorSet.GenContourPolygonXld(out fitEdgeContour, fitEdgeRows, fitEdgeCols);
                        HOperatorSet.FitLineContourXld(fitEdgeContour, "tukey", -1, 0, 5, 2,
                                                       out HTuple rowBegin, out HTuple colBegin,
                                                       out HTuple rowEnd, out HTuple colEnd,
                                                       out HTuple nr, out HTuple nc, out HTuple dist);
                        try
                        {
                            if (!TryProjectStepLineToMeasureRegion(measureRegionTop, measureRegionLeft, measureRegionDown, measureRegionRight,
                                                                   rowBegin.D, colBegin.D, rowEnd.D, colEnd.D,
                                                                   out double startRow, out double startCol,
                                                                   out double endRow, out double endCol))
                            {
                                return false;
                            }

                            stepEdge = new StepEdgeData
                            {
                                StartRow = startRow,
                                StartCol = startCol,
                                EndRow = endRow,
                                EndCol = endCol,
                                EdgeLine = CreateLine(startRow, startCol, endRow, endCol)
                            };
                            return true;
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
                    hvWidth.Dispose();
                    hvHeight.Dispose();
                }
            }
            finally
            {
                stepEdgeContour?.Dispose();
                fitEdgeContour?.Dispose();
                contours?.Dispose();

                if (metrologyHandle.Length > 0)
                {
                    HOperatorSet.ClearMetrologyModel(metrologyHandle);
                }

                metrologyHandle.Dispose();
                index0.Dispose();
                parameter.Dispose();
                fitEdgeRows.Dispose();
                fitEdgeCols.Dispose();
                contourCount.Dispose();
            }

            return false;
        }

        private static bool TryProjectStepLineToMeasureRegion(double measureRegionTop, double measureRegionLeft,
                                                              double measureRegionDown, double measureRegionRight,
                                                              double rowBegin, double colBegin,
                                                              double rowEnd, double colEnd,
                                                              out double startRow, out double startCol,
                                                              out double endRow, out double endCol)
        {
            HTuple startRowTuple = new HTuple();
            HTuple startColTuple = new HTuple();
            HTuple endRowTuple = new HTuple();
            HTuple endColTuple = new HTuple();
            HTuple overlapLeft = new HTuple();
            HTuple overlapRight = new HTuple();

            startRow = 0;
            startCol = 0;
            endRow = 0;
            endCol = 0;

            try
            {
                HOperatorSet.IntersectionLines(measureRegionTop, measureRegionLeft, measureRegionDown, measureRegionLeft,
                                               rowBegin, colBegin, rowEnd, colEnd,
                                               out startRowTuple, out startColTuple, out overlapLeft);
                HOperatorSet.IntersectionLines(measureRegionTop, measureRegionRight, measureRegionDown, measureRegionRight,
                                               rowBegin, colBegin, rowEnd, colEnd,
                                               out endRowTuple, out endColTuple, out overlapRight);

                if (startRowTuple.Length <= 0 || startColTuple.Length <= 0 ||
                    endRowTuple.Length <= 0 || endColTuple.Length <= 0)
                {
                    return false;
                }

                startRow = startRowTuple.D;
                startCol = startColTuple.D;
                endRow = endRowTuple.D;
                endCol = endColTuple.D;

                return double.IsFinite(startRow) && double.IsFinite(startCol) &&
                       double.IsFinite(endRow) && double.IsFinite(endCol);
            }
            finally
            {
                startRowTuple.Dispose();
                startColTuple.Dispose();
                endRowTuple.Dispose();
                endColTuple.Dispose();
                overlapLeft.Dispose();
                overlapRight.Dispose();
            }
        }

        private void MeasureStepHeights(IReadOnlyList<StepEdgeData> stepEdges,
                                        double measureRegionTop, double measureRegionLeft,
                                        double measureRegionDown, double measureRegionRight)
        {
            HOperatorSet.GetImageSize(_hoGrayImage, out HTuple hvWidth, out HTuple hvHeight);
            int imageWidth = hvWidth.I;
            int imageHeight = hvHeight.I;
            hvWidth.Dispose();
            hvHeight.Dispose();

            for (int edgeIndex = 0; edgeIndex < stepEdges.Count; edgeIndex++)
            {
                StepEdgeData edge = stepEdges[edgeIndex];
                HObject? stepTopRegion = null;
                HObject? stepDownRegion = null;
                HObject? tmp = null;
                HObject? topPlane = null;
                HObject? downPlane = null;

                double referenceRow = Math.Round((edge.StartRow + edge.EndRow) * 0.5);
                double referenceCol = Math.Round((edge.StartCol + edge.EndCol) * 0.5);
                double stepHeight = -1;
                Point[] stepTopRegionContour = Array.Empty<Point>();
                Point[] stepDownRegionContour = Array.Empty<Point>();

                try
                {
                    double topStepEdgeStartRow1 = edgeIndex == 0
                        ? measureRegionTop + StepRegionOffset
                        : (stepEdges[edgeIndex - 1].StartRow + edge.StartRow) * 0.5;
                    double topStepEdgeStartRow2 = edgeIndex == 0
                        ? measureRegionTop + StepRegionOffset
                        : (stepEdges[edgeIndex - 1].EndRow + edge.EndRow) * 0.5;
                    double topStepEdgeEndRow1 = edge.StartRow - StepRegionOffset;
                    double topStepEdgeEndRow2 = edge.EndRow - StepRegionOffset;

                    double downStepEdgeStartRow1 = edge.StartRow + StepRegionOffset;
                    double downStepEdgeStartRow2 = edge.EndRow + StepRegionOffset;
                    double downStepEdgeEndRow1 = edgeIndex == stepEdges.Count - 1
                        ? measureRegionDown - StepRegionOffset
                        : (edge.StartRow + stepEdges[edgeIndex + 1].StartRow) * 0.5;
                    double downStepEdgeEndRow2 = edgeIndex == stepEdges.Count - 1
                        ? measureRegionDown - StepRegionOffset
                        : (edge.EndRow + stepEdges[edgeIndex + 1].EndRow) * 0.5;

                    HOperatorSet.GenRegionPolygonFilled(out stepTopRegion,
                                                        new HTuple(new[] { topStepEdgeStartRow1, topStepEdgeStartRow2, topStepEdgeEndRow2, topStepEdgeEndRow1 }),
                                                        new HTuple(new[] { measureRegionLeft, measureRegionRight, measureRegionRight, measureRegionLeft }));
                    HOperatorSet.GenRegionPolygonFilled(out stepDownRegion,
                                                        new HTuple(new[] { downStepEdgeStartRow1, downStepEdgeStartRow2, downStepEdgeEndRow2, downStepEdgeEndRow1 }),
                                                        new HTuple(new[] { measureRegionLeft, measureRegionRight, measureRegionRight, measureRegionLeft }));

                    HOperatorSet.Intersection(stepTopRegion, _hoValidRegion, out tmp);
                    stepTopRegion.Dispose();
                    stepTopRegion = tmp;
                    tmp = null;

                    HOperatorSet.Intersection(stepDownRegion, _hoValidRegion, out tmp);
                    stepDownRegion.Dispose();
                    stepDownRegion = tmp;
                    tmp = null;

                    HOperatorSet.AreaCenter(stepTopRegion, out HTuple topArea, out HTuple topCenterRow, out HTuple topCenterCol);
                    HOperatorSet.AreaCenter(stepDownRegion, out HTuple downArea, out HTuple downCenterRow, out HTuple downCenterCol);
                    try
                    {
                        if (topArea.D > 20 && downArea.D > 20)
                        {
                            HOperatorSet.FitSurfaceFirstOrder(stepTopRegion, _hoHeightImage, "tukey", 5, StepSurfaceClippingFactor,
                                                              out HTuple alphaTop, out HTuple betaTop, out HTuple gammaTop);
                            HOperatorSet.FitSurfaceFirstOrder(stepDownRegion, _hoHeightImage, "tukey", 5, StepSurfaceClippingFactor,
                                                              out HTuple alphaDown, out HTuple betaDown, out HTuple gammaDown);
                            try
                            {
                                HOperatorSet.GenImageSurfaceFirstOrder(out topPlane, "real", alphaTop, betaTop, gammaTop,
                                                                       topCenterRow, topCenterCol, imageWidth, imageHeight);
                                HOperatorSet.GenImageSurfaceFirstOrder(out downPlane, "real", alphaDown, betaDown, gammaDown,
                                                                       downCenterRow, downCenterCol, imageWidth, imageHeight);
                            }
                            finally
                            {
                                alphaTop.Dispose();
                                betaTop.Dispose();
                                gammaTop.Dispose();
                                alphaDown.Dispose();
                                betaDown.Dispose();
                                gammaDown.Dispose();
                            }

                            HOperatorSet.GetGrayval(topPlane, referenceRow, referenceCol, out HTuple zTop);
                            HOperatorSet.GetGrayval(downPlane, referenceRow, referenceCol, out HTuple zDown);
                            try
                            {
                                if (zTop.Length > 0 && zDown.Length > 0)
                                {
                                    stepHeight = Math.Abs(zTop.D - zDown.D) * _measureParam.IntervalZ;
                                }
                            }
                            finally
                            {
                                zTop.Dispose();
                                zDown.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        topArea.Dispose();
                        topCenterRow.Dispose();
                        topCenterCol.Dispose();
                        downArea.Dispose();
                        downCenterRow.Dispose();
                        downCenterCol.Dispose();
                    }

                    stepTopRegionContour = GetRegionContourPoints(stepTopRegion);
                    stepDownRegionContour = GetRegionContourPoints(stepDownRegion);
                }
                finally
                {
                    stepTopRegion?.Dispose();
                    stepDownRegion?.Dispose();
                    tmp?.Dispose();
                    topPlane?.Dispose();
                    downPlane?.Dispose();
                }

                _measureResult.StepResults.Add(new KCJC0_StandardPlateStepResult
                {
                    StepEdgeLine = edge.EdgeLine,
                    StepTopRegionContour = stepTopRegionContour,
                    StepDownRegionContour = stepDownRegionContour,
                    ReferencePoint = new Point2d(referenceCol, referenceRow),
                    HeightPhysical = stepHeight
                });

                if (!double.IsFinite(stepHeight) || stepHeight < 0)
                {
                    _measureResult.IsOK = false;
                }
            }
        }
    }
}
