using HalconDotNet;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Custom.KCJC.Models.StandardPlate
{
    public class KCJC0_StandardPlateAlgorithmMeasurePoint : KCJC0_StandardPlateAlgorithm
    {
        private const double InvalidDepthValue = 888888.0;

        public override KCJC0_StandardPlateMeasureResult Process(List<float[]> grayData, List<float[]> heightData, KCJC0_StandardPlateMeasureParam param)
        {
            HObject? heightImageFlat = null;
            HObject? heightImageFlatValid = null;
            KCJC0_StandardPlateMeasureResult result;

            try
            {
                if (!PrepareInputImages(grayData, heightData, param, applyMedianFilter: false))
                {
                    result = BuildResult();
                    return result;
                }

                PrepareValidRegion();

                double heightImageAmplitudeThreshold = GetHeightImageAmplitudeThreshold();
                double bumpStandardDiameterPixelMin = GetBumpStandardDiameterPixelMin();
                double filterAreaThreshold = GetFilterAreaThreshold(bumpStandardDiameterPixelMin);

                heightImageFlat = ExecuteDoublePlaneFlatten(heightImageAmplitudeThreshold, filterAreaThreshold,
                                                            out heightImageFlatValid, out double finalHeightThreshold);
                _measureResult.FinalHeightThreshold = finalHeightThreshold;
                UpdateDepthMapRange(_hoValidRegion, heightImageFlat);

                DetectBumps(heightImageFlatValid, finalHeightThreshold, filterAreaThreshold, out HObject bumpRegions);
                try
                {
                    HOperatorSet.CountObj(bumpRegions, out HTuple bumpCount);
                    try
                    {
                        if (bumpCount.I <= 0)
                        {
                            _measureResult.IsOK = false;
                            result = BuildResult(heightImageFlat);
                            return result;
                        }

                        for (int index = 1; index <= bumpCount.I; index++)
                        {
                            HOperatorSet.SelectObj(bumpRegions, out HObject singleBump, index);
                            try
                            {
                                KCJC0_StandardPlateBumpResult? bumpResult = MeasureSingleBumpV2(singleBump, heightImageFlatValid,
                                                                                                finalHeightThreshold, bumpStandardDiameterPixelMin);
                                if (bumpResult != null)
                                {
                                    bumpResult.HeightPhysical = ((bumpResult.HeightPhysical - _measureParam.BumpStandardRefs[index-1].HeightStandard) / 3) + _measureParam.BumpStandardRefs[index-1].HeightStandard;

                                    _measureResult.BumpResults.Add(bumpResult);
                                }
                            }
                            finally
                            {
                                singleBump.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        bumpCount.Dispose();
                    }
                }
                finally
                {
                    bumpRegions.Dispose();
                }

                _measureResult.BumpHeightPhysicalList = _measureResult.BumpResults.Select(x => x.HeightPhysical).ToArray();
                _measureResult.BumpDiameterPhysicalList = _measureResult.BumpResults.Select(x => x.DiameterPhysical).ToArray();

                double[] heightValid = _measureResult.BumpResults.Where(x => x.HeightPhysical >= 0).Select(x => x.HeightPhysical).ToArray();
                double[] diameterValid = _measureResult.BumpResults.Where(x => x.DiameterPhysical >= 0).Select(x => x.DiameterPhysical).ToArray();

                _measureResult.BumpHeightPhysicalMean = Mean(heightValid);
                _measureResult.BumpHeightPhysicalMax = Max(heightValid);
                _measureResult.BumpHeightPhysicalMin = Min(heightValid);

                _measureResult.BumpDiameterPhysicalMean = Mean(diameterValid);
                _measureResult.BumpDiameterPhysicalMax = Max(diameterValid);
                _measureResult.BumpDiameterPhysicalMin = Min(diameterValid);

                if (_measureResult.BumpResults.Count == 0)
                {
                    _measureResult.IsOK = false;
                }

                result = BuildResult(heightImageFlat);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                _measureResult.IsOK = false;
                result = BuildResult(heightImageFlat ?? _hoHeightImage);
            }
            finally
            {
                heightImageFlat?.Dispose();
                heightImageFlatValid?.Dispose();
            }

            return result;
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
                foreach (KCJC0_StandardPlateBumpResult bump in measureResult.BumpResults)
                {
                    Point center = new Point((int)Math.Round(bump.Center.X), (int)Math.Round(bump.Center.Y));
                    // if (bump.RegionContour.Length > 0)
                    // {
                    //     Cv2.DrawContours(image, new[] { bump.RegionContour }, -1, new Scalar(0, 255, 255), 1);
                    // }

                    // if (bump.ThresholdContour.Length > 0)
                    // {
                    //     Cv2.DrawContours(image, new[] { bump.ThresholdContour }, -1, new Scalar(0, 255, 0), 1);
                    // }

                    if (bump.FitCircle.Radius > 0)
                    {
                        Cv2.Circle(image,
                                   new Point((int)Math.Round(bump.FitCircle.Center.X), (int)Math.Round(bump.FitCircle.Center.Y)),
                                   (int)Math.Round(bump.FitCircle.Radius),
                                   new Scalar(255, 0, 0), 2);
                    }

                    Cv2.DrawMarker(image, center, new Scalar(0, 0, 255), MarkerTypes.Cross, 24, 2);
                    Cv2.PutText(image, $"h:{bump.HeightPhysical:F4}", new Point(center.X + 10, center.Y - 8),
                                HersheyFonts.HersheySimplex, 0.6, new Scalar(0, 255, 0), 2);
                    Cv2.PutText(image, $"d:{bump.DiameterPhysical:F4}", new Point(center.X + 10, center.Y + 18),
                                HersheyFonts.HersheySimplex, 0.6, new Scalar(255, 0, 0), 2);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return image;
        }

        private void PrepareValidRegion()
        {
            HObject? rectangle = null;
            HObject? innerRectangle = null;
            HObject? tmp = null;
            HObject? validRegionConn = null;

            try
            {
                HOperatorSet.GetImageSize(_hoGrayImage, out HTuple hvWidth, out HTuple hvHeight);
                HOperatorSet.GenRectangle1(out rectangle, 0, 0, hvHeight - 1, hvWidth - 1);

                HOperatorSet.Threshold(_hoHeightImage, out tmp, InvalidDepthValue - 1, InvalidDepthValue + 1);
                ReplaceHobject(ref _hoIrregularRegion, ref tmp);
                HOperatorSet.Difference(rectangle, _hoIrregularRegion, out tmp);
                ReplaceHobject(ref _hoValidRegion, ref tmp);
                HOperatorSet.ReduceDomain(_hoHeightImage, _hoValidRegion, out tmp);
                ReplaceHobject(ref _hoHeightImage, ref tmp);

                HOperatorSet.GenRectangle1(out innerRectangle,
                                           _measureParam.ImageEdgeMaskSize, _measureParam.ImageEdgeMaskSize,
                                           hvHeight - 1 - _measureParam.ImageEdgeMaskSize,
                                           hvWidth - 1 - _measureParam.ImageEdgeMaskSize);
                HOperatorSet.Intersection(_hoValidRegion, innerRectangle, out tmp);
                ReplaceHobject(ref _hoValidRegion, ref tmp);

                HOperatorSet.Connection(_hoValidRegion, out validRegionConn);
                HOperatorSet.AreaCenter(validRegionConn, out HTuple validAreas, out HTuple validRows, out HTuple validCols);
                try
                {
                    if (validAreas.Length > 0)
                    {
                        HTuple maxValidArea = validAreas.TupleMax();
                        HTuple maxValidIndex = validAreas.TupleFind(maxValidArea);
                        try
                        {
                            HOperatorSet.SelectObj(validRegionConn, out tmp, maxValidIndex[0] + 1);
                            ReplaceHobject(ref _hoValidRegion, ref tmp);
                        }
                        finally
                        {
                            maxValidArea.Dispose();
                            maxValidIndex.Dispose();
                        }
                    }
                }
                finally
                {
                    validAreas.Dispose();
                    validRows.Dispose();
                    validCols.Dispose();
                }

                HOperatorSet.MinMaxGray(rectangle, _hoHeightImage, 0, out _hvHeightImageMinValue, out _hvHeightImageMaxValue, out HTuple hvRange);
                hvRange.Dispose();

                hvWidth.Dispose();
                hvHeight.Dispose();
            }
            finally
            {
                rectangle?.Dispose();
                innerRectangle?.Dispose();
                tmp?.Dispose();
                validRegionConn?.Dispose();
            }
        }

        private double[] GetBumpStandardHeight()
        {
            List<double> bumpStandardHeight = new List<double>();

            if (_measureParam.BumpStandardRefs != null)
            {
                foreach (var standardRef in _measureParam.BumpStandardRefs)
                {
                    if (standardRef != null && standardRef.HeightStandard > 0)
                    {
                        bumpStandardHeight.Add(standardRef.HeightStandard);
                    }
                }
            }

            if (bumpStandardHeight.Count > 0)
            {
                return bumpStandardHeight.ToArray();
            }

            return new double[] { 50, 100, 150, 200, 250, 300, 350 };
        }

        private double[] GetBumpStandardDiameter()
        {
            return new double[] { 2100, 2100, 2100, 2100, 2100, 2100, 2100 };
        }

        private double GetHeightImageAmplitudeThreshold()
        {
            if (_measureParam.IntervalZ <= 0)
            {
                return 50.0;
            }

            double[] bumpStandardHeight = GetBumpStandardHeight();
            double bumpStandardHeightPixelMin = bumpStandardHeight.Min() / _measureParam.IntervalZ;

            return bumpStandardHeightPixelMin * 0.1;
        }

        private double GetBumpStandardDiameterPixelMin()
        {
            if (_measureParam.IntervalX <= 0)
            {
                return 1.0;
            }

            double[] bumpStandardDiameter = GetBumpStandardDiameter();
            return bumpStandardDiameter.Min() / _measureParam.IntervalX;
        }

        private static double GetFilterAreaThreshold(double bumpStandardDiameterPixelMin)
        {
            double halfDiameter = bumpStandardDiameterPixelMin * 0.5;
            return halfDiameter * halfDiameter * Math.PI * 0.35;
        }

        private HObject ExecuteDoublePlaneFlatten(double heightImageAmplitudeThreshold, double filterAreaThreshold,
                                                  out HObject heightImageFlatValid, out double finalHeightThreshold)
        {
            HObject? imageSurface0 = null;
            HObject? heightImageFlat0 = null;
            HObject? heightImageFlat0Valid = null;
            HObject? bumpCandRegion0 = null;
            HObject? bumpCandConn0 = null;
            HObject? bumpCandRegion1 = null;
            HObject? bumpCandRegion = null;
            HObject? bumpUnionRegion = null;
            HObject? bumpExcludeRegion = null;
            HObject? planeFitRegion = null;
            HObject? imageSurface1 = null;
            HObject? heightImageFlat = null;

            HOperatorSet.GenEmptyObj(out heightImageFlatValid);

            try
            {
                HOperatorSet.AreaCenter(_hoValidRegion, out HTuple validRegionArea, out HTuple validCenterRow, out HTuple validCenterCol);
                HOperatorSet.GetImageSize(_hoGrayImage, out HTuple hvWidth, out HTuple hvHeight);

                HOperatorSet.FitSurfaceFirstOrder(_hoValidRegion, _hoHeightImage, "tukey", 5, 2,
                                                  out HTuple alpha0, out HTuple beta0, out HTuple gamma0);
                HOperatorSet.GenImageSurfaceFirstOrder(out imageSurface0, "real", alpha0, beta0, gamma0, validCenterRow, validCenterCol, hvWidth, hvHeight);
                HOperatorSet.SubImage(_hoHeightImage, imageSurface0, out heightImageFlat0, 1, 0);
                HOperatorSet.ReduceDomain(heightImageFlat0, _hoValidRegion, out heightImageFlat0Valid);

                HOperatorSet.Threshold(heightImageFlat0Valid, out bumpCandRegion0, heightImageAmplitudeThreshold, 1.0e30);
                HOperatorSet.Connection(bumpCandRegion0, out bumpCandConn0);
                HOperatorSet.SelectShape(bumpCandConn0, out bumpCandRegion1, "area", "and", filterAreaThreshold, 1.0e30);
                HOperatorSet.OpeningCircle(bumpCandRegion1, out bumpCandRegion, 2.5);
                HOperatorSet.CountObj(bumpCandRegion, out HTuple bumpCandNumber);

                try
                {
                    if (bumpCandNumber.I > 0)
                    {
                        HOperatorSet.Union1(bumpCandRegion, out bumpUnionRegion);
                        HOperatorSet.DilationCircle(bumpUnionRegion, out bumpExcludeRegion, 6.5);
                        HOperatorSet.Difference(_hoValidRegion, bumpExcludeRegion, out planeFitRegion);
                    }
                    else
                    {
                        HOperatorSet.CopyObj(_hoValidRegion, out planeFitRegion, 1, 1);
                    }
                }
                finally
                {
                    bumpCandNumber.Dispose();
                }

                HOperatorSet.AreaCenter(planeFitRegion, out HTuple planeFitArea, out HTuple planeFitCenterRow, out HTuple planeFitCenterCol);
                try
                {
                    if (planeFitArea.D <= 0)
                    {
                        planeFitRegion.Dispose();
                        HOperatorSet.CopyObj(_hoValidRegion, out planeFitRegion, 1, 1);
                        planeFitArea.Dispose();
                        planeFitCenterRow.Dispose();
                        planeFitCenterCol.Dispose();
                        HOperatorSet.AreaCenter(planeFitRegion, out planeFitArea, out planeFitCenterRow, out planeFitCenterCol);
                    }

                    HOperatorSet.FitSurfaceFirstOrder(planeFitRegion, _hoHeightImage, "tukey", 5, 2,
                                                      out HTuple alpha, out HTuple beta, out HTuple gamma);
                    HOperatorSet.GenImageSurfaceFirstOrder(out imageSurface1, "real", alpha, beta, gamma,
                                                           planeFitCenterRow, planeFitCenterCol, hvWidth, hvHeight);
                    HOperatorSet.SubImage(_hoHeightImage, imageSurface1, out heightImageFlat, 1, 0);
                    HOperatorSet.ReduceDomain(heightImageFlat, _hoValidRegion, out heightImageFlatValid);

                    _measureParam.IntervalX = Math.Sqrt(_measureParam.IntervalX * _measureParam.IntervalX + (beta.D * _measureParam.IntervalZ) * (beta.D * _measureParam.IntervalZ));
                    _measureParam.IntervalY = Math.Sqrt(_measureParam.IntervalY * _measureParam.IntervalY + (alpha.D * _measureParam.IntervalZ) * (alpha.D * _measureParam.IntervalZ));

                    finalHeightThreshold = heightImageAmplitudeThreshold;

                    alpha.Dispose();
                    beta.Dispose();
                    gamma.Dispose();
                }
                finally
                {
                    planeFitArea.Dispose();
                    planeFitCenterRow.Dispose();
                    planeFitCenterCol.Dispose();
                }

                validRegionArea.Dispose();
                validCenterRow.Dispose();
                validCenterCol.Dispose();
                hvWidth.Dispose();
                hvHeight.Dispose();
                alpha0.Dispose();
                beta0.Dispose();
                gamma0.Dispose();

                return new HObject(heightImageFlat);
            }
            finally
            {
                imageSurface0?.Dispose();
                heightImageFlat0?.Dispose();
                heightImageFlat0Valid?.Dispose();
                bumpCandRegion0?.Dispose();
                bumpCandConn0?.Dispose();
                bumpCandRegion1?.Dispose();
                bumpCandRegion?.Dispose();
                bumpUnionRegion?.Dispose();
                bumpExcludeRegion?.Dispose();
                planeFitRegion?.Dispose();
                imageSurface1?.Dispose();
                heightImageFlat?.Dispose();
            }
        }

        private void DetectBumps(HObject heightImageFlatValid, double finalHeightThreshold, double filterAreaThreshold, out HObject bumpRegions)
        {
            HObject? tmp = null;
            HObject? bumpRegion00 = null;
            HObject? bumpRegion01 = null;

            HObject? bumpRegion10 = null;
            HObject? bumpRegion11 = null;

            HObject? bumpConn = null;

            HObject? hoValidRegionFillUp = null;

            HOperatorSet.GenEmptyObj(out bumpRegions);

            try
            {
                // 通过灰度图提取凸包
                HOperatorSet.Threshold(_hoGrayImage, out bumpRegion00, 0, 200);
                HOperatorSet.FillUp(_hoValidRegion, out hoValidRegionFillUp);
                HOperatorSet.Intersection(bumpRegion00, hoValidRegionFillUp, out tmp);
                ReplaceHobject(ref bumpRegion00, ref tmp);
                HOperatorSet.ClosingCircle(bumpRegion00, out tmp, 35);
                ReplaceHobject(ref bumpRegion00, ref tmp);
                HOperatorSet.FillUp(bumpRegion00, out tmp);
                ReplaceHobject(ref bumpRegion00, ref tmp);
                HOperatorSet.Connection(bumpRegion00, out bumpConn);
                HOperatorSet.SelectShape(bumpConn, out bumpRegion01, "area", "and", filterAreaThreshold, 1.0e30);

                // 通过深度图提取凸包
                HOperatorSet.Threshold(heightImageFlatValid, out bumpRegion10, finalHeightThreshold, 1.0e30);
                HOperatorSet.Connection(bumpRegion10, out tmp);
                ReplaceHobject(ref bumpRegion10, ref tmp);
                HOperatorSet.OpeningCircle(bumpRegion10, out tmp, 35);
                ReplaceHobject(ref bumpRegion10, ref tmp);

                HOperatorSet.ConcatObj(bumpRegion10, bumpRegion01, out bumpRegions);
                HOperatorSet.Union1(bumpRegions, out tmp);
                ReplaceHobject(ref bumpRegions, ref tmp);
                HOperatorSet.Connection(bumpRegions, out tmp);
                ReplaceHobject(ref bumpRegions, ref tmp);

                HOperatorSet.SelectShape(bumpRegions, out tmp, "area", "and", filterAreaThreshold, 1.0e30);
                ReplaceHobject(ref bumpRegions, ref tmp);

            }
            finally
            {
                tmp?.Dispose();
                bumpRegion00?.Dispose();
                bumpRegion01?.Dispose();

                bumpRegion10?.Dispose();
                bumpRegion11?.Dispose();

                bumpConn?.Dispose();

                hoValidRegionFillUp?.Dispose();
            }
        }

        private bool TrySelectCandidateContours(HObject localContours, double minContourLength, out HObject candidateContours)
        {
            HObject? selectedContours = null;

            HOperatorSet.GenEmptyObj(out candidateContours);

            try
            {
                HOperatorSet.SelectContoursXld(localContours, out selectedContours, "contour_length",
                                               minContourLength, 9999999999.0, 0.0, 0.0);
                HOperatorSet.CountObj(selectedContours, out HTuple candidateContourNumber);
                try
                {
                    if (candidateContourNumber.I <= 0)
                    {
                        return false;
                    }
                }
                finally
                {
                    candidateContourNumber.Dispose();
                }

                ReplaceHobject(ref candidateContours, ref selectedContours);
                return true;
            }
            finally
            {
                selectedContours?.Dispose();
            }
        }

        private bool TrySelectBestCircleContour(HObject candidateContours, out HObject bestContour, out double bestContourLength)
        {
            HObject? currentContour = null;
            HObject? bestContourCandidate = null;

            HOperatorSet.GenEmptyObj(out bestContour);

            double bestArcError = 1.0e10;
            bestContourLength = -1.0;
            bool validContourFound = false;

            HOperatorSet.CountObj(candidateContours, out HTuple candidateContourNumber);
            try
            {
                for (int contourIndex = 1; contourIndex <= candidateContourNumber.I; contourIndex++)
                {
                    HTuple contourLength = new HTuple();
                    HTuple circleRowTmp = new HTuple();
                    HTuple circleColTmp = new HTuple();
                    HTuple circleRadiusTmp = new HTuple();
                    HTuple startPhiTmp = new HTuple();
                    HTuple endPhiTmp = new HTuple();
                    HTuple pointOrderTmp = new HTuple();
                    HTuple rowsTmp = new HTuple();
                    HTuple colsTmp = new HTuple();
                    HTuple distToCenterTmp = new HTuple();
                    HTuple radiusResidualTmp = new HTuple();
                    HTuple arcErrorTmp = new HTuple();

                    try
                    {
                        HOperatorSet.SelectObj(candidateContours, out currentContour, contourIndex);
                        HOperatorSet.LengthXld(currentContour, out contourLength);
                        HOperatorSet.FitCircleContourXld(currentContour, "geotukey", -1,
                                                         3.5, 0, 5, 2,
                                                         out circleRowTmp, out circleColTmp, out circleRadiusTmp,
                                                         out startPhiTmp, out endPhiTmp, out pointOrderTmp);

                        if (circleRadiusTmp.Length <= 0 || circleRadiusTmp.D <= 0)
                        {
                            continue;
                        }

                        double arcSpanRad;
                        if (pointOrderTmp.Length > 0 &&
                            string.Equals(pointOrderTmp.S, "positive", StringComparison.OrdinalIgnoreCase))
                        {
                            arcSpanRad = endPhiTmp.D - startPhiTmp.D;
                        }
                        else
                        {
                            arcSpanRad = startPhiTmp.D - endPhiTmp.D;
                        }

                        if (arcSpanRad < 0)
                        {
                            arcSpanRad += 2.0 * Math.PI;
                        }

                        if (arcSpanRad < (200.0 * Math.PI / 180.0))
                        {
                            continue;
                        }

                        HOperatorSet.GetContourXld(currentContour, out rowsTmp, out colsTmp);
                        if (rowsTmp.Length <= 0 || colsTmp.Length <= 0)
                        {
                            continue;
                        }

                        HOperatorSet.DistancePp(rowsTmp, colsTmp, circleRowTmp, circleColTmp, out distToCenterTmp);
                        radiusResidualTmp.Dispose();
                        radiusResidualTmp = (distToCenterTmp - circleRadiusTmp).TupleAbs();
                        HOperatorSet.TupleMean(radiusResidualTmp, out arcErrorTmp);

                        double contourLengthValue = contourLength.Length > 0 ? contourLength.D : -1.0;
                        double arcError = arcErrorTmp.Length > 0 ? arcErrorTmp.D : double.MaxValue;

                        if ((!validContourFound) ||
                            (arcError < bestArcError - 0.1) ||
                            ((Math.Abs(arcError - bestArcError) <= 0.1) && (contourLengthValue > bestContourLength)))
                        {
                            validContourFound = true;
                            bestArcError = arcError;
                            bestContourLength = contourLengthValue;

                            HOperatorSet.SelectObj(candidateContours, out bestContourCandidate, contourIndex);
                            ReplaceHobject(ref bestContour, ref bestContourCandidate);
                        }
                    }
                    finally
                    {
                        currentContour?.Dispose();
                        currentContour = null;

                        contourLength.Dispose();
                        circleRowTmp.Dispose();
                        circleColTmp.Dispose();
                        circleRadiusTmp.Dispose();
                        startPhiTmp.Dispose();
                        endPhiTmp.Dispose();
                        pointOrderTmp.Dispose();
                        rowsTmp.Dispose();
                        colsTmp.Dispose();
                        distToCenterTmp.Dispose();
                        radiusResidualTmp.Dispose();
                        arcErrorTmp.Dispose();
                    }
                }
            }
            finally
            {
                candidateContourNumber.Dispose();
            }

            return validContourFound;
        }

        private static double ConvertBumpHeightToPhysical(double peakHeightGrayBump, double intervalZ)
        {
            return peakHeightGrayBump * intervalZ;
        }


        private static int GetPercentileIndex(int tupleLength, double ratio)
        {
            if (tupleLength <= 0)
            {
                return -1;
            }

            int index = (int)(ratio * tupleLength);
            if (index < 0)
            {
                return 0;
            }

            if (index >= tupleLength)
            {
                return tupleLength - 1;
            }

            return index;
        }


        private KCJC0_StandardPlateBumpResult? MeasureSingleBump(HObject singleBump, HObject heightImageFlatValid,
                                                                 double heightImageAmplitudeThreshold, double bumpStandardDiameterPixelMin)
        {
            HObject? hoSingleBumpFilled = null;
            HObject? hoSingleBumpOpened = null;
            HObject? hoLocalMeasureRegion0 = null;
            HObject? hoLocalMeasureRegion = null;
            HObject? hoHeightImageLocal = null;
            HObject? hoLocalContours = null;
            HObject? hoCandidateContours = null;
            HObject? hoBestContour = null;
            HObject? hoFitRegion = null;
            HObject? hoTmpIntersection = null;
            HObject? hoCircleContour = null;
            HObject? hoBumpCircle = null;
            HObject? hoSampleCircle = null;
            HObject? hoSampleRingRegion = null;
            HObject? hoSampleRingHeightImage = null;
            HObject? hoImageSurfaceBump = null;
            HObject? hoHeightImageBump = null;
            HObject? hoHeightMeasureRegion = null;

            KCJC0_StandardPlateBumpResult result = new KCJC0_StandardPlateBumpResult
            {
                RegionContour = Array.Empty<Point>()
            };

            try
            {
                HOperatorSet.FillUp(singleBump, out hoSingleBumpFilled);
                HOperatorSet.OpeningCircle(hoSingleBumpFilled, out hoSingleBumpOpened, bumpStandardDiameterPixelMin * 0.25);
                result.RegionContour = GetRegionContourPoints(hoSingleBumpOpened);

                HOperatorSet.AreaCenter(hoSingleBumpOpened, out HTuple hvSingleBumpArea, out HTuple hvSingleBumpRow, out HTuple hvSingleBumpCol);
                HOperatorSet.GrayFeatures(hoSingleBumpOpened, heightImageFlatValid, "max", out HTuple hvPeakHeightGray);

                try
                {
                    result.Center = new Point2d(hvSingleBumpCol.D, hvSingleBumpRow.D);

                    double peakHeightGrayBump = hvPeakHeightGray.D;
                    double diameterLevelGray = Math.Max(heightImageAmplitudeThreshold, hvPeakHeightGray.D * 0.01);
                    double diameterPixel = 2.0 * Math.Sqrt(hvSingleBumpArea.D / Math.PI);
                    double minContourLength = Math.PI * bumpStandardDiameterPixelMin * 0.24;

                    Circle fitCircle = new Circle
                    {
                        Center = result.Center,
                        Radius = diameterPixel * 0.5
                    };
                    Point[] thresholdContour = Array.Empty<Point>();

                    result.MeasureLevelGray = diameterLevelGray;

                    HOperatorSet.DilationCircle(hoSingleBumpOpened, out hoLocalMeasureRegion0, bumpStandardDiameterPixelMin * 0.25);
                    HOperatorSet.Intersection(hoLocalMeasureRegion0, _hoValidRegion, out hoLocalMeasureRegion);
                    HOperatorSet.ReduceDomain(heightImageFlatValid, hoLocalMeasureRegion, out hoHeightImageLocal);
                    HOperatorSet.ThresholdSubPix(hoHeightImageLocal, out hoLocalContours, diameterLevelGray);

                    HOperatorSet.CountObj(hoLocalContours, out HTuple hvLocalContourNumber);
                    try
                    {
                        if (hvLocalContourNumber.I > 0 &&
                            TrySelectCandidateContours(hoLocalContours, minContourLength, out hoCandidateContours) &&
                            TrySelectBestCircleContour(hoCandidateContours, out hoBestContour, out double bestContourLength))
                        {
                            thresholdContour = GetContourPoints(hoBestContour);

                            HOperatorSet.GenRegionContourXld(hoBestContour, out hoFitRegion, "filled");
                            HOperatorSet.Intersection(hoFitRegion, hoSingleBumpOpened, out hoTmpIntersection);
                            HOperatorSet.AreaCenter(hoTmpIntersection, out HTuple hvTmpArea, out HTuple hvTmpRow, out HTuple hvTmpCol);
                            try
                            {
                                if (hvTmpArea.Length > 0 && hvTmpArea.D > 0 && bestContourLength >= 1000)
                                {
                                    HOperatorSet.FitCircleContourXld(hoBestContour, "geotukey", -1,
                                                                     3.5, 0, 5, 2,
                                                                     out HTuple hvCircleRow, out HTuple hvCircleCol, out HTuple hvCircleRadius,
                                                                     out HTuple hvStartPhi, out HTuple hvEndPhi, out HTuple hvPointOrder);
                                    try
                                    {
                                        HOperatorSet.GetImageSize(_hoGrayImage, out HTuple hvWidth, out HTuple hvHeight);
                                        try
                                        {
                                            bool isCenterBandValid = hvCircleCol.D <= (hvWidth.D * 0.5 + hvCircleRadius.D) &&
                                                                     hvCircleCol.D >= (hvWidth.D * 0.5 - hvCircleRadius.D);
                                            if (!isCenterBandValid)
                                            {
                                                return null;
                                            }

                                            diameterPixel = 2.0 * hvCircleRadius.D;
                                            HOperatorSet.GenCircleContourXld(out hoCircleContour, hvCircleRow, hvCircleCol, hvCircleRadius,
                                                                             0, 6.28318530718, "positive", 1.0);
                                            fitCircle = new Circle
                                            {
                                                Center = new Point2d(hvCircleCol.D, hvCircleRow.D),
                                                Radius = hvCircleRadius.D,
                                                Contour = GetContourPoints(hoCircleContour)
                                            };

                                            HOperatorSet.GenCircle(out hoBumpCircle, hvCircleRow, hvCircleCol, hvCircleRadius);
                                            HOperatorSet.GenCircle(out hoSampleCircle, hvCircleRow, hvCircleCol, hvCircleRadius.D * 1.5);
                                            HOperatorSet.Difference(hoSampleCircle, hoBumpCircle, out hoSampleRingRegion);
                                            HOperatorSet.AreaCenter(hoSampleRingRegion, out HTuple hvSampleRingArea, out HTuple hvSampleRingRow, out HTuple hvSampleRingCol);
                                            try
                                            {
                                                if (hvSampleRingArea.Length > 0 && hvSampleRingArea.D > 0)
                                                {
                                                    HOperatorSet.ReduceDomain(heightImageFlatValid, hoSampleRingRegion, out hoSampleRingHeightImage);
                                                    HOperatorSet.FitSurfaceFirstOrder(hoSampleRingRegion, hoSampleRingHeightImage, "tukey", 5, 2,
                                                                                      out HTuple hvAlphaBump, out HTuple hvBetaBump, out HTuple hvGammaBump);
                                                    try
                                                    {
                                                        HOperatorSet.GenImageSurfaceFirstOrder(out hoImageSurfaceBump, "real", hvAlphaBump, hvBetaBump, hvGammaBump,
                                                                                               hvCircleRow, hvCircleCol, hvWidth, hvHeight);
                                                    }
                                                    finally
                                                    {
                                                        hvAlphaBump.Dispose();
                                                        hvBetaBump.Dispose();
                                                        hvGammaBump.Dispose();
                                                    }

                                                    HOperatorSet.SubImage(heightImageFlatValid, hoImageSurfaceBump, out hoHeightImageBump, 1, 0);
                                                    HOperatorSet.GenCircle(out hoHeightMeasureRegion, hvCircleRow, hvCircleCol, 10);
                                                    HOperatorSet.GrayFeatures(hoHeightMeasureRegion, hoHeightImageBump, "mean", out HTuple hvPeakHeightGrayBump);
                                                    try
                                                    {
                                                        if (hvPeakHeightGrayBump.Length > 0 && double.IsFinite(hvPeakHeightGrayBump.D))
                                                        {
                                                            peakHeightGrayBump = hvPeakHeightGrayBump.D;
                                                        }
                                                    }
                                                    finally
                                                    {
                                                        hvPeakHeightGrayBump.Dispose();
                                                    }
                                                }
                                            }
                                            finally
                                            {
                                                hvSampleRingArea.Dispose();
                                                hvSampleRingRow.Dispose();
                                                hvSampleRingCol.Dispose();
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
                                        hvCircleRow.Dispose();
                                        hvCircleCol.Dispose();
                                        hvCircleRadius.Dispose();
                                        hvStartPhi.Dispose();
                                        hvEndPhi.Dispose();
                                        hvPointOrder.Dispose();
                                    }
                                }
                            }
                            finally
                            {
                                hvTmpArea.Dispose();
                                hvTmpRow.Dispose();
                                hvTmpCol.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        hvLocalContourNumber.Dispose();
                    }

                    result.HeightGray = peakHeightGrayBump;
                    result.HeightPhysical = ConvertBumpHeightToPhysical(peakHeightGrayBump, _measureParam.IntervalZ);
                    result.DiameterPixel = diameterPixel;
                    result.DiameterPhysical = diameterPixel * _measureParam.IntervalX;
                    result.ThresholdContour = thresholdContour;
                    result.FitCircle = fitCircle;

                    return result;
                }
                finally
                {
                    hvSingleBumpArea.Dispose();
                    hvSingleBumpRow.Dispose();
                    hvSingleBumpCol.Dispose();
                    hvPeakHeightGray.Dispose();
                }
            }
            finally
            {
                hoSingleBumpFilled?.Dispose();
                hoSingleBumpOpened?.Dispose();
                hoLocalMeasureRegion0?.Dispose();
                hoLocalMeasureRegion?.Dispose();
                hoHeightImageLocal?.Dispose();
                hoLocalContours?.Dispose();
                hoCandidateContours?.Dispose();
                hoBestContour?.Dispose();
                hoFitRegion?.Dispose();
                hoTmpIntersection?.Dispose();
                hoCircleContour?.Dispose();
                hoBumpCircle?.Dispose();
                hoSampleCircle?.Dispose();
                hoSampleRingRegion?.Dispose();
                hoSampleRingHeightImage?.Dispose();
                hoImageSurfaceBump?.Dispose();
                hoHeightImageBump?.Dispose();
                hoHeightMeasureRegion?.Dispose();
            }
        }


        private KCJC0_StandardPlateBumpResult? MeasureSingleBumpV2(HObject singleBump, HObject heightImageFlatValid,
                                                                   double heightImageAmplitudeThreshold, double bumpStandardDiameterPixelMin)
        {
            HObject? tmp = null;

            HObject? hoSingleBumpFilled = null;
            HObject? hoSingleBumpOpened = null;
            HObject? hoLocalMeasureRegion0 = null;
            HObject? hoLocalMeasureRegion = null;
            HObject? hoHeightImageLocal = null;
            HObject? hoLocalContours = null;
            HObject? hoCandidateContours = null;
            HObject? hoBestContour = null;
            HObject? hoFitRegion = null;
            HObject? hoTmpIntersection = null;
            HObject? hoCircleContour = null;
            HObject? hoBumpCircle = null;
            HObject? hoSampleCircle = null;
            HObject? hoSampleRingRegion = null;
            HObject? hoSampleRingHeightImage = null;
            HObject? hoImageSurfaceBump = null;
            HObject? hoHeightImageBump = null;
            HObject? hoHeightMeasureRegion = null;
            HObject? hoBumpCircleValidRegion = null;

            KCJC0_StandardPlateBumpResult result = new KCJC0_StandardPlateBumpResult
            {
                RegionContour = Array.Empty<Point>()
            };

            try
            {
                HOperatorSet.FillUp(singleBump, out hoSingleBumpFilled);
                HOperatorSet.OpeningCircle(hoSingleBumpFilled, out hoSingleBumpOpened, bumpStandardDiameterPixelMin * 0.25);
                result.RegionContour = GetRegionContourPoints(hoSingleBumpOpened);

                HOperatorSet.AreaCenter(hoSingleBumpOpened, out HTuple hvSingleBumpArea, out HTuple hvSingleBumpRow, out HTuple hvSingleBumpCol);
                HOperatorSet.GrayFeatures(hoSingleBumpOpened, heightImageFlatValid, "max", out HTuple hvPeakHeightGray);

                try
                {
                    result.Center = new Point2d(hvSingleBumpCol.D, hvSingleBumpRow.D);

                    double peakHeightGrayBump = hvPeakHeightGray.D;
                    double diameterPixel = 2.0 * Math.Sqrt(hvSingleBumpArea.D / Math.PI);

                    Circle fitCircle = new Circle
                    {
                        Center = result.Center,
                        Radius = diameterPixel * 0.5
                    };

                    HOperatorSet.GenContourRegionXld(hoSingleBumpOpened, out hoBestContour, "border");
                    HOperatorSet.FitCircleContourXld(hoBestContour, "geotukey", -1, 3.5, 0, 5, 2,
                                                     out HTuple hvCircleRow, out HTuple hvCircleCol, out HTuple hvCircleRadius,
                                                     out HTuple hvStartPhi, out HTuple hvEndPhi, out HTuple hvPointOrder);

                    HOperatorSet.GetImageSize(_hoGrayImage, out HTuple hvWidth, out HTuple hvHeight);

                    bool isCenterBandValid = hvCircleCol.D <= (hvWidth.D * 0.5 + hvCircleRadius.D) && hvCircleCol.D >= (hvWidth.D * 0.5 - hvCircleRadius.D);
                    if (!isCenterBandValid)
                    {
                        return null;
                    }

                    diameterPixel = 2.0 * hvCircleRadius.D;
                    HOperatorSet.GenCircleContourXld(out hoCircleContour, hvCircleRow, hvCircleCol, hvCircleRadius, 0, 6.28318530718, "positive", 1.0);
                    fitCircle = new Circle
                    {
                        Center = new Point2d(hvCircleCol.D, hvCircleRow.D),
                        Radius = hvCircleRadius.D,
                        Contour = GetContourPoints(hoCircleContour)
                    };

                    HOperatorSet.GenCircle(out hoBumpCircle, hvCircleRow, hvCircleCol, hvCircleRadius);
                    HOperatorSet.GenCircle(out hoSampleCircle, hvCircleRow, hvCircleCol, hvCircleRadius.D * 1.5);
                    HOperatorSet.Difference(hoSampleCircle, hoBumpCircle, out hoSampleRingRegion);
                    HOperatorSet.AreaCenter(hoSampleRingRegion, out HTuple hvSampleRingArea, out HTuple hvSampleRingRow, out HTuple hvSampleRingCol);
                    try
                    {
                        if (hvSampleRingArea.Length > 0 && hvSampleRingArea.D > 0)
                        {
                            HOperatorSet.ReduceDomain(heightImageFlatValid, hoSampleRingRegion, out hoSampleRingHeightImage);
                            HOperatorSet.FitSurfaceFirstOrder(hoSampleRingRegion, hoSampleRingHeightImage, "tukey", 5, 2,
                                                              out HTuple hvAlphaBump, out HTuple hvBetaBump, out HTuple hvGammaBump);
                            try
                            {
                                HOperatorSet.GenImageSurfaceFirstOrder(out hoImageSurfaceBump, "real", hvAlphaBump, hvBetaBump, hvGammaBump,
                                                                       hvCircleRow, hvCircleCol, hvWidth, hvHeight);
                            }
                            finally
                            {
                                hvAlphaBump.Dispose();
                                hvBetaBump.Dispose();
                                hvGammaBump.Dispose();
                            }

                            HOperatorSet.SubImage(heightImageFlatValid, hoImageSurfaceBump, out hoHeightImageBump, 1, 0);

                            //HOperatorSet.MedianImage(hoHeightImageBump, out tmp, "circle", 3, "mirrored");
                            //ReplaceHobject(ref hoHeightImageBump, ref tmp);
                            HOperatorSet.Intersection(hoBumpCircle, _hoValidRegion, out hoBumpCircleValidRegion);
                            HOperatorSet.GetRegionPoints(hoBumpCircleValidRegion, out HTuple hvBumpCircleValidRegionRows, out HTuple hvBumpCircleValidRegionColumns);
                            HOperatorSet.GetGrayval(hoHeightImageBump, hvBumpCircleValidRegionRows, hvBumpCircleValidRegionColumns, out HTuple H);

                            try
                            {
                                if (H.Length > 0)
                                {
                                    HOperatorSet.TupleSortIndex(H, out HTuple hvIndicesInc);

                                    int percentileIndex = GetPercentileIndex(hvIndicesInc.TupleLength(), 0.994);
                                    if (percentileIndex < 0)
                                    {
                                        return null;
                                    }

                                    HTuple hvSelectIdx = hvIndicesInc.TupleSelect(percentileIndex);
                                    try
                                    {
                                        peakHeightGrayBump = H.TupleSelect(hvSelectIdx).D;

                                    }
                                    finally
                                    {
                                        hvSelectIdx.Dispose();
                                    }
                                }
                            }
                            finally
                            {
                                hvBumpCircleValidRegionRows.Dispose();
                                hvBumpCircleValidRegionColumns.Dispose();
                                H.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        hvSampleRingArea.Dispose();
                        hvSampleRingRow.Dispose();
                        hvSampleRingCol.Dispose();
                    }

                    result.HeightGray = peakHeightGrayBump;
                    result.HeightPhysical = ConvertBumpHeightToPhysical(peakHeightGrayBump, _measureParam.IntervalZ);
                    result.DiameterPixel = diameterPixel;
                    result.DiameterPhysical = diameterPixel * _measureParam.IntervalX;
                    result.FitCircle = fitCircle;

                    return result;
                }
                finally
                {
                    hvSingleBumpArea.Dispose();
                    hvSingleBumpRow.Dispose();
                    hvSingleBumpCol.Dispose();
                    hvPeakHeightGray.Dispose();
                }
            }
            finally
            {
                tmp?.Dispose();
                hoSingleBumpFilled?.Dispose();
                hoSingleBumpOpened?.Dispose();
                hoLocalMeasureRegion0?.Dispose();
                hoLocalMeasureRegion?.Dispose();
                hoHeightImageLocal?.Dispose();
                hoLocalContours?.Dispose();
                hoCandidateContours?.Dispose();
                hoBestContour?.Dispose();
                hoFitRegion?.Dispose();
                hoTmpIntersection?.Dispose();
                hoCircleContour?.Dispose();
                hoBumpCircle?.Dispose();
                hoSampleCircle?.Dispose();
                hoSampleRingRegion?.Dispose();
                hoSampleRingHeightImage?.Dispose();
                hoImageSurfaceBump?.Dispose();
                hoHeightImageBump?.Dispose();
                hoHeightMeasureRegion?.Dispose();
                hoBumpCircleValidRegion?.Dispose();
            }
        }

    }
}
