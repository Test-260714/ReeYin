using Custom.ElectroStaticChuckMeasure.ALGO.Frames;
using Custom.ElectroStaticChuckMeasure.ALGO.Parameters;
using HalconDotNet;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;
using OpenCvSharp;
using ReeYin_V.Core.Extension;

namespace Custom.ElectroStaticChuckMeasure.ALGO.Measurement;

public sealed class ConvexMeasurementEngine
{
    public MeasurementResult Measure(ConvexMeasurementInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        using var state = ConvexMeasurementState.FromInput(input);
        Preprocess(state);

        state.Result.IntervalX = state.CurrentIntervalX;
        state.Result.IntervalY = state.CurrentIntervalY;
        state.Result.IntervalZ = state.CurrentIntervalZ;

        GetConvexRegions(state);
        ExtractConvexFeatures(state);
        state.Result.IntervalZ = state.PhysicalIntervalZ;
        state.Result.DisplayGrayImage = state.GrayImage.Clone();

        return state.DetachResult();
    }

    internal static double GetFlatnessV2(double[] x, double[] y, double[] z, out double[] residualZ)
    {
        residualZ = Array.Empty<double>();
        List<Point3d> surfacePoints = ToCvPoint3d(x, y, z);

        double flatness = -1;
        if (surfacePoints.Count > 3)
        {
            Plane fitSurfacePlane = FitPlaneIrlsPCA(surfacePoints);
            residualZ = surfacePoints.Select(p => fitSurfacePlane.DistanceTo(p)).ToArray();
            double minResidualZ = residualZ.Min();
            flatness = residualZ.Max() - minResidualZ;
            residualZ = residualZ.Select(v => v - minResidualZ).ToArray();
        }

        return flatness;
    }

    private static void Preprocess(ConvexMeasurementState state)
    {
        using (var dh = new HDevDisposeHelper())
        {
            HObject? hoTmp = null;
            HObject? hoIrregularRegion = null;

            HTuple hvScaleX;
            HTuple hvScaleY;
            bool fastModel = false;
            if (fastModel)
            {
                if (state.CurrentIntervalX > state.CurrentIntervalY)
                {
                    hvScaleX = 1;
                    hvScaleY = state.CurrentIntervalY / state.CurrentIntervalX;
                }
                else if (state.CurrentIntervalX < state.CurrentIntervalY)
                {
                    hvScaleX = state.CurrentIntervalX / state.CurrentIntervalY;
                    hvScaleY = 1;
                }
                else
                {
                    hvScaleX = 1;
                    hvScaleY = 1;
                }
            }
            else
            {
                if (state.CurrentIntervalX < state.CurrentIntervalY)
                {
                    hvScaleX = 1;
                    hvScaleY = state.CurrentIntervalY / state.CurrentIntervalX;
                }
                else if (state.CurrentIntervalX > state.CurrentIntervalY)
                {
                    hvScaleX = state.CurrentIntervalX / state.CurrentIntervalY;
                    hvScaleY = 1;
                }
                else
                {
                    hvScaleX = 1;
                    hvScaleY = 1;
                }
            }

            try
            {
                double originalIntervalZ = state.CurrentIntervalZ;
                state.CurrentIntervalX = state.CurrentIntervalX / hvScaleX;
                state.CurrentIntervalY = state.CurrentIntervalY / hvScaleY;
                state.CurrentIntervalZ = state.CurrentIntervalZ / state.CurrentIntervalZ;

                state.ConvexStandardDiameterPixel = state.Measurement.ConvexStandardDiameter / state.CurrentIntervalX;
                state.ConvexStandardHeightPixel = state.Measurement.ConvexStandardHeight / state.CurrentIntervalZ;

                HOperatorSet.ZoomImageFactor(state.GrayImage, out hoTmp, hvScaleX, hvScaleY, "bilinear");
                state.GrayImage = ReplaceHobject(state.GrayImage, ref hoTmp);
                HOperatorSet.ZoomImageFactor(state.HeightImage, out hoTmp, hvScaleX, hvScaleY, "nearest_neighbor");
                state.HeightImage = ReplaceHobject(state.HeightImage, ref hoTmp);
                HOperatorSet.ScaleImage(state.HeightImage, out hoTmp, originalIntervalZ, 0);
                state.HeightImage = ReplaceHobject(state.HeightImage, ref hoTmp);

                HOperatorSet.GetImageSize(state.HeightImage, out HTuple hvTmpTileW, out HTuple hvTmpTileH);
                HOperatorSet.GenRectangle1(out hoTmp, 0, 0, hvTmpTileH - 1, hvTmpTileW - 1);
                state.ValidMask = ReplaceHobject(state.ValidMask, ref hoTmp);
                HOperatorSet.Threshold(state.HeightImage, out hoIrregularRegion, state.Sensor.InvalidValue - 1, state.Sensor.InvalidValue + 1);
                HOperatorSet.Difference(state.ValidMask, hoIrregularRegion, out hoTmp);
                state.ValidMask = ReplaceHobject(state.ValidMask, ref hoTmp);

            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Measurement preprocessing failed.", ex);
            }
            finally
            {
                hoTmp?.Dispose();
                hoIrregularRegion?.Dispose();
            }
        }
    }

    private static void GetConvexRegions(ConvexMeasurementState state)
    {
        using (var dh = new HDevDisposeHelper())
        {
            HObject? hoTmp = null;
            HObject? hoSampleValidMaskImage = null;
            HObject? hoHeightImageWeighted = null;
            HObject? hoHeightWeightedMeanSmall = null;
            HObject? hoValidMaskMeanSmall = null;
            HObject? hoHeightImageSampleSmall = null;
            HObject? hoHeightWeightedMeanBig = null;
            HObject? hoValidMaskMeanBig = null;
            HObject? hoHeightImageSampleBig = null;

            try
            {
                HOperatorSet.GetImageSize(state.HeightImage, out HTuple hvImageWidth, out HTuple hvImageHeight);
                HOperatorSet.GenImageConst(out hoSampleValidMaskImage, "real", hvImageWidth, hvImageHeight);
                HOperatorSet.PaintRegion(state.ValidMask, hoSampleValidMaskImage, out hoTmp, 1.0, "fill");
                ReplaceHobject(ref hoSampleValidMaskImage, ref hoTmp);

                HOperatorSet.MultImage(state.HeightImage, hoSampleValidMaskImage, out hoHeightImageWeighted, 1.0, 0.0);

                HTuple hvSmallKernelSize = Math.Max(1.0, (state.ConvexStandardDiameterPixel * 0.1 * 0.5).D);
                HTuple hvBigKernelSize = Math.Max(1.0, (state.ConvexStandardDiameterPixel * 2).D);

                HOperatorSet.MeanImage(hoHeightImageWeighted, out hoHeightWeightedMeanSmall, hvSmallKernelSize, hvSmallKernelSize);
                HOperatorSet.MeanImage(hoSampleValidMaskImage, out hoValidMaskMeanSmall, hvSmallKernelSize, hvSmallKernelSize);
                HOperatorSet.ReduceDomain(hoHeightWeightedMeanSmall, state.ValidMask, out hoTmp);
                ReplaceHobject(ref hoHeightWeightedMeanSmall, ref hoTmp);
                HOperatorSet.ReduceDomain(hoValidMaskMeanSmall, state.ValidMask, out hoTmp);
                ReplaceHobject(ref hoValidMaskMeanSmall, ref hoTmp);
                HOperatorSet.DivImage(hoHeightWeightedMeanSmall, hoValidMaskMeanSmall, out hoHeightImageSampleSmall, 1.0, 0.0);

                HOperatorSet.MeanImage(hoHeightImageWeighted, out hoHeightWeightedMeanBig, hvBigKernelSize, hvBigKernelSize);
                HOperatorSet.MeanImage(hoSampleValidMaskImage, out hoValidMaskMeanBig, hvBigKernelSize, hvBigKernelSize);
                HOperatorSet.ReduceDomain(hoHeightWeightedMeanBig, state.ValidMask, out hoTmp);
                ReplaceHobject(ref hoHeightWeightedMeanBig, ref hoTmp);
                HOperatorSet.ReduceDomain(hoValidMaskMeanBig, state.ValidMask, out hoTmp);
                ReplaceHobject(ref hoValidMaskMeanBig, ref hoTmp);
                HOperatorSet.DivImage(hoHeightWeightedMeanBig, hoValidMaskMeanBig, out hoHeightImageSampleBig, 1.0, 0.0);

                state.FilterAreaThreshold = (state.ConvexStandardDiameterPixel / 2) * (state.ConvexStandardDiameterPixel / 2) * 3.14159265359;
                HObject? hoConvexRegions = null;
                HOperatorSet.DynThreshold(hoHeightImageSampleSmall, hoHeightImageSampleBig, out hoConvexRegions, state.ConvexStandardHeightPixel * 0.1, "light");
                state.ConvexRegions = ReplaceHobject(state.ConvexRegions, ref hoConvexRegions);
                HOperatorSet.Connection(state.ConvexRegions, out hoTmp);
                state.ConvexRegions = ReplaceHobject(state.ConvexRegions, ref hoTmp);
                HOperatorSet.SelectShape(
                    state.ConvexRegions,
                    out hoTmp,
                    (new HTuple("circularity")).TupleConcat("area"),
                    "and",
                    (new HTuple(0.45)).TupleConcat(state.FilterAreaThreshold * 0.5),
                    (new HTuple(1)).TupleConcat(state.FilterAreaThreshold * 2));
                state.ConvexRegions = ReplaceHobject(state.ConvexRegions, ref hoTmp);
                HOperatorSet.Intersection(state.ConvexRegions, state.ValidMask, out hoTmp);
                state.ConvexRegions = ReplaceHobject(state.ConvexRegions, ref hoTmp);

            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Convex region extraction failed.", ex);
            }
            finally
            {
                hoTmp?.Dispose();
                hoSampleValidMaskImage?.Dispose();
                hoHeightImageWeighted?.Dispose();
                hoHeightWeightedMeanSmall?.Dispose();
                hoValidMaskMeanSmall?.Dispose();
                hoHeightImageSampleSmall?.Dispose();
                hoHeightWeightedMeanBig?.Dispose();
                hoValidMaskMeanBig?.Dispose();
                hoHeightImageSampleBig?.Dispose();
            }
        }
    }

    private static void ExtractConvexFeatures(ConvexMeasurementState state)
    {
        using (var dh = new HDevDisposeHelper())
        {
            HObject? hoTmp = null;
            HTuple? hvMetrologyHandle = null;
            HObject? hoHeightImageSampleExpanded = null;
            HObject? hoConvexRegion = null;
            HObject? hoFitConvexRegions = null;
            HObject? hoTmpConvexContours = null;
            HObject? hoTmpConvexContour = null;
            HObject? hoConvexContour = null;
            HObject? hoSampleRegion = null;
            HObject? hoSampleReduce = null;
            HObject? hoSamplePart = null;
            HObject? hoPlaneMeasureRegion = null;
            HObject? hoConvexMeasureRegion = null;
            HObject? hoPlaneMeasureRegionRing = null;
            HObject? hoPlaneMeasureReduced = null;
            HObject? hoPartSurface = null;
            HObject? hoSamplePartSub = null;
            HObject? hoSamplePartSmooth = null;
            HObject? hoSamplePartSubSmooth = null;
            HObject? hoConvexPartRegion = null;
            HObject? hoConvexFlatnessRegion = null;
            HObject? hoSamplePartMask = null;

            try
            {
                HOperatorSet.GetImageSize(state.HeightImage, out HTuple hvImageWidth, out HTuple hvImageHeight);

                HOperatorSet.ReduceDomain(state.HeightImage, state.ValidMask, out hoTmp);
                state.HeightImage = ReplaceHobject(state.HeightImage, ref hoTmp);
                HOperatorSet.ExpandDomainGray(state.HeightImage, out hoHeightImageSampleExpanded, state.ConvexStandardDiameterPixel);
                HOperatorSet.ReduceDomain(hoHeightImageSampleExpanded, state.ValidMask, out hoTmp);
                ReplaceHobject(ref hoHeightImageSampleExpanded, ref hoTmp);

                HOperatorSet.CountObj(state.ConvexRegions, out HTuple hvConvexNum);
                HOperatorSet.GenEmptyObj(out hoFitConvexRegions);
                for (int idx = 0; idx < hvConvexNum; idx++)
                {
                    ConvexFeature convexResult = new ConvexFeature();

                    HOperatorSet.SelectObj(state.ConvexRegions, out hoConvexRegion, idx + 1);

                    HOperatorSet.RegionFeatures(hoConvexRegion, "roundness", out HTuple hvConvexRoundness);
                    HOperatorSet.RegionFeatures(hoConvexRegion, "row", out HTuple hvConvexCenterRow);
                    HOperatorSet.RegionFeatures(hoConvexRegion, "column", out HTuple hvConvexCenterCol);
                    HOperatorSet.RegionFeatures(hoConvexRegion, "inner_radius", out HTuple hvConvexInnerRadius);
                    HOperatorSet.RegionFeatures(hoConvexRegion, "outer_radius", out HTuple hvConvexOuterRadius);

                    HTuple hvConvexRadius = (hvConvexInnerRadius + hvConvexOuterRadius) * 0.5;
                    HTuple hvConvexParam;

                    hvMetrologyHandle = null;
                    try
                    {
                        HOperatorSet.CreateMetrologyModel(out hvMetrologyHandle);
                        HOperatorSet.AddMetrologyObjectCircleMeasure(
                            hvMetrologyHandle,
                            hvConvexCenterRow,
                            hvConvexCenterCol,
                            hvConvexRadius,
                            hvConvexRadius * 0.5,
                            2,
                            1.5,
                            state.ConvexStandardHeightPixel * 0.1,
                            new HTuple(),
                            new HTuple(),
                            out HTuple hvIndex);
                        HOperatorSet.SetMetrologyObjectParam(hvMetrologyHandle, 0, "measure_transition", "negative");
                        HOperatorSet.SetMetrologyObjectParam(hvMetrologyHandle, "all", "min_score", 0.01);
                        HOperatorSet.SetMetrologyObjectParam(hvMetrologyHandle, "all", "measure_select", "last");
                        HOperatorSet.ApplyMetrologyModel(hoHeightImageSampleExpanded, hvMetrologyHandle);
                        HOperatorSet.GetMetrologyObjectResult(hvMetrologyHandle, "all", "all", "result_type", "all_param", out hvConvexParam);

                        hoConvexContour?.Dispose();
                        HOperatorSet.GetMetrologyObjectResultContour(out hoConvexContour, hvMetrologyHandle, "all", "all", 1.5);
                    }
                    finally
                    {
                        if (hvMetrologyHandle != null)
                        {
                            HOperatorSet.ClearMetrologyModel(hvMetrologyHandle);
                            hvMetrologyHandle.Dispose();
                            hvMetrologyHandle = null;
                        }
                    }

                    if (hvConvexParam.Length > 0)
                    {
                        hvConvexCenterRow = hvConvexParam[0];
                        hvConvexCenterCol = hvConvexParam[1];
                        hvConvexRadius = hvConvexParam[2];
                    }

                    //HOperatorSet.ConcatObj(hoFitConvexRegions, hoConvexContour, out hoTmp);
                    //ReplaceHobject(ref hoFitConvexRegions, ref hoTmp);

                    HTuple hvSampleRadius = hvConvexRadius * 3;
                    HTuple hvMeasureRadius = hvConvexRadius * 1.5;

                    hoSampleRegion?.Dispose();
                    hoSampleReduce?.Dispose();
                    hoSamplePart?.Dispose();
                    hoPlaneMeasureRegion?.Dispose();
                    hoConvexMeasureRegion?.Dispose();
                    hoPlaneMeasureRegionRing?.Dispose();
                    hoPlaneMeasureReduced?.Dispose();
                    hoPartSurface?.Dispose();
                    hoSamplePartSub?.Dispose();
                    hoSamplePartSmooth?.Dispose();
                    hoSamplePartSubSmooth?.Dispose();
                    hoConvexPartRegion?.Dispose();
                    hoConvexFlatnessRegion?.Dispose();
                    hoSamplePartMask?.Dispose();

                    HOperatorSet.GenCircle(out hoSampleRegion, hvConvexCenterRow, hvConvexCenterCol, hvSampleRadius);
                    HOperatorSet.ReduceDomain(hoSampleRegion, state.ValidMask, out hoTmp);
                    ReplaceHobject(ref hoSampleRegion, ref hoTmp);
                    HOperatorSet.RegionFeatures(hoSampleRegion, "width", out HTuple hvSampleWidth);
                    HOperatorSet.RegionFeatures(hoSampleRegion, "height", out HTuple hvSampleHeight);
                    HOperatorSet.ReduceDomain(state.HeightImage, hoSampleRegion, out hoSampleReduce);

                    HOperatorSet.CropDomain(hoSampleReduce, out hoSamplePart);
                    HOperatorSet.GetImageSize(hoSamplePart, out HTuple hvPartWidth, out HTuple hvPartHeight);

                    HTuple hvPartCenterCol;
                    if (hvConvexCenterCol.D < hvSampleRadius.D)
                    {
                        hvPartCenterCol = hvConvexCenterCol - (hvSampleWidth - hvPartWidth);
                    }
                    else
                    {
                        HOperatorSet.DistancePl(hvConvexCenterRow, hvConvexCenterCol, 0, 0, hvImageHeight, 0, out HTuple hvCenter2EdgeDistance);
                        hvPartCenterCol = hvCenter2EdgeDistance > hvSampleRadius
                            ? new HTuple(hvSampleRadius)
                            : hvSampleRadius - (hvSampleWidth - hvPartWidth);
                    }

                    HTuple hvPartCenterRow;
                    if (hvConvexCenterRow < hvSampleRadius)
                    {
                        hvPartCenterRow = hvConvexCenterRow - (hvSampleHeight - hvPartHeight);
                    }
                    else
                    {
                        HOperatorSet.DistancePl(hvConvexCenterRow, hvConvexCenterCol, 0, 0, 0, hvImageWidth, out HTuple hvCenter2EdgeDistance);
                        hvPartCenterRow = hvCenter2EdgeDistance > hvSampleRadius
                            ? new HTuple(hvSampleRadius)
                            : hvSampleRadius - (hvSampleHeight - hvPartHeight);
                    }

                    HOperatorSet.GenCircle(out hoPlaneMeasureRegion, hvPartCenterRow, hvPartCenterCol, hvSampleRadius);
                    HOperatorSet.GenCircle(out hoConvexMeasureRegion, hvPartCenterRow, hvPartCenterCol, hvMeasureRadius);
                    HOperatorSet.Difference(hoPlaneMeasureRegion, hoConvexMeasureRegion, out hoPlaneMeasureRegionRing);
                    HOperatorSet.ReduceDomain(hoSamplePart, hoPlaneMeasureRegionRing, out hoPlaneMeasureReduced);

                    HOperatorSet.FitSurfaceFirstOrder(hoPlaneMeasureRegionRing, hoPlaneMeasureReduced, "tukey", 5, 1, out HTuple hvAlpha, out HTuple hvBeta, out HTuple hvGamma);
                    HOperatorSet.GenImageSurfaceFirstOrder(out hoPartSurface, "real", hvAlpha, hvBeta, hvGamma, hvPartCenterRow, hvPartCenterCol, hvPartWidth, hvPartHeight);
                    HOperatorSet.SubImage(hoSamplePart, hoPartSurface, out hoSamplePartSub, 1, 0);
                    HOperatorSet.GrayFeatures(hoPlaneMeasureRegionRing, hoSamplePartSub, "mean", out HTuple hvPlaneHeightMean);

                    HOperatorSet.MedianImage(hoSamplePart, out hoSamplePartSmooth, "circle", 3, "mirrored");
                    HOperatorSet.MedianImage(hoSamplePartSub, out hoSamplePartSubSmooth, "circle", 3, "mirrored");

                    hoTmpConvexContours?.Dispose();
                    HOperatorSet.ThresholdSubPix(hoSamplePartSub, out hoTmpConvexContours, 5);
                    HOperatorSet.CountObj(hoTmpConvexContours, out HTuple hvTmpConvexContoursNum);
                    if(hvTmpConvexContoursNum.D > 0)
                    {
                        HOperatorSet.LengthXld(hoTmpConvexContours, out HTuple hvContourLengths);
                        HOperatorSet.TupleMax(hvContourLengths, out HTuple hvMaxContourLength);
                        HOperatorSet.TupleFind(hvContourLengths, hvMaxContourLength, out HTuple hvMaxContourIndex);
                        HOperatorSet.SelectObj(hoTmpConvexContours, out hoTmpConvexContour, hvMaxContourIndex + 1);
                        if(hvMaxContourLength.D > hvConvexRadius * 0.5)
                        {
                            HOperatorSet.FitCircleContourXld(hoTmpConvexContour, "geotukey", -1, 3.5, 0, 5, 2, out HTuple hvTmpConvexCenterRow, out HTuple hvTmpConvexCenterCol, 
                                                             out hvConvexRadius, out _, out _, out _);
                            HOperatorSet.GenCircleContourXld(out hoTmp, hvConvexCenterRow, hvConvexCenterCol, hvConvexRadius, 0, new HTuple(360).TupleRad(), "positive", 1.0);
                            ReplaceHobject(ref hoConvexContour, ref hoTmp);
                        }
                    }
                    HOperatorSet.ConcatObj(hoFitConvexRegions, hoConvexContour, out hoTmp);
                    ReplaceHobject(ref hoFitConvexRegions, ref hoTmp);

                    HOperatorSet.MoveRegion(hoConvexRegion, out hoConvexPartRegion, (-hvConvexCenterRow) + hvPartCenterRow, (-hvConvexCenterCol) + hvPartCenterCol);

                    HOperatorSet.GenRectangle1(out hoSamplePartMask, 0, 0, hvPartHeight - 1, hvPartWidth - 1);
                    HOperatorSet.Intersection(hoConvexPartRegion, hoSamplePartMask, out hoTmp);
                    ReplaceHobject(ref hoConvexPartRegion, ref hoTmp);

                    HOperatorSet.GetRegionPoints(hoConvexPartRegion, out HTuple hvConvexPointRows, out HTuple hvConvexPointColumns);
                    HOperatorSet.GetGrayval(hoSamplePartSubSmooth, hvConvexPointRows, hvConvexPointColumns, out HTuple hvH);

                    HOperatorSet.TupleSortIndex(hvH, out HTuple hvIndicesInc);
                    HOperatorSet.TupleLength(hvIndicesInc, out HTuple hvN);
                    double pLow = 0.01;
                    double pHigh = 0.05;
                    int start = (int)Math.Floor((1.0 - pHigh) * (hvN.D - 1));
                    int end = (int)Math.Floor((1.0 - pLow) * (hvN.D - 1));
                    start = Math.Max(0, start);
                    end = Math.Max(0, end);
                    if (end < start)
                        end = start;

                    HOperatorSet.TupleSelectRange(hvIndicesInc, start, end, out HTuple hvTopIdx);
                    HOperatorSet.TupleSelect(hvH, hvTopIdx, out HTuple hvTop);
                    HOperatorSet.TupleMean(hvTop, out HTuple hvPeak);
                    HTuple hvConvexHeight = (hvPeak - hvPlaneHeightMean).TupleAbs();

                    HTuple hvMeasureFlatnessRadius = hvConvexRadius * 0.5;
                    HOperatorSet.GenCircle(out hoConvexFlatnessRegion, hvPartCenterRow, hvPartCenterCol, hvMeasureFlatnessRadius);

                    HOperatorSet.GetRegionPoints(hoConvexFlatnessRegion, out HTuple hvConvexFlatnessPointRows, out HTuple hvConvexFlatnessPointColumns);
                    HOperatorSet.GetGrayval(hoSamplePartSubSmooth, hvConvexFlatnessPointRows, hvConvexFlatnessPointColumns, out HTuple hvFlatnessH);
                    HOperatorSet.GetGrayval(hoSamplePartSmooth, hvConvexFlatnessPointRows, hvConvexFlatnessPointColumns, out HTuple hvFlatnessHReal);

                    double[] x = Array.ConvertAll(hvConvexFlatnessPointColumns.LArr, v => v * state.Result.IntervalX);
                    double[] y = Array.ConvertAll(hvConvexFlatnessPointRows.LArr, v => v * state.Result.IntervalY);
                    double[] z = Array.ConvertAll(hvFlatnessH.TupleReal().DArr, v => v * state.Result.IntervalZ);

                    double convexFlatnessSingle = GetFlatnessRobust(x, y, z);

                    HOperatorSet.TupleSortIndex(hvFlatnessH, out HTuple hvFlatnessIndicesInc);
                    HOperatorSet.TupleLength(hvFlatnessIndicesInc, out HTuple hvFlatnessN);
                    int flatnessStart = (int)Math.Floor((1.0 - pHigh) * (hvFlatnessN.D - 1));
                    int flatnessEnd = (int)Math.Floor((1.0 - pLow) * (hvFlatnessN.D - 1));
                    flatnessStart = Math.Max(0, flatnessStart);
                    flatnessEnd = Math.Max(0, flatnessEnd);
                    if (flatnessEnd < flatnessStart)
                        flatnessEnd = flatnessStart;

                    HOperatorSet.TupleSelectRange(hvFlatnessIndicesInc, flatnessStart, flatnessEnd, out HTuple hvFlatnessTopIdx);
                    HOperatorSet.TupleSelect(hvFlatnessHReal, hvFlatnessTopIdx, out HTuple hvFlatnessTopReal);
                    HOperatorSet.TupleMean(hvFlatnessTopReal, out HTuple hvSurfaceValue);

                    convexResult.PixelX = hvConvexCenterCol.D;
                    convexResult.PixelY = hvConvexCenterRow.D;
                    convexResult.X = hvConvexCenterCol.D * state.Result.IntervalX;
                    convexResult.Y = hvConvexCenterRow.D * state.Result.IntervalY;
                    convexResult.Z = hvSurfaceValue.D * state.Result.IntervalZ;
                    convexResult.Diameter = hvConvexRadius.D * 2 * state.Result.IntervalX;
                    convexResult.Roundness = hvConvexRoundness.D;
                    convexResult.Height = hvConvexHeight.D * state.Result.IntervalZ;
                    convexResult.Flatness = convexFlatnessSingle;

                    state.Result.ConvexResults.Add(convexResult);
                }

                state.Result.FitConvexRegion?.Dispose();
                state.Result.FitConvexRegion = hoFitConvexRegions.Clone();

                double[] convexX = state.Result.ConvexResults.Select(c => c.X).ToArray();
                double[] convexY = state.Result.ConvexResults.Select(c => c.Y).ToArray();
                double[] convexZ = state.Result.ConvexResults.Select(c => c.Z).ToArray();
                state.Result.ConvexsFlatness = GetFlatnessV2(convexX, convexY, convexZ, out double[] residualConvexZ);
                for (int i = 0; i < state.Result.ConvexResults.Count && i < residualConvexZ.Length; i++)
                    state.Result.ConvexResults[i].ResidualZ = residualConvexZ[i];

            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Convex feature extraction failed.", ex);
            }
            finally
            {
                hoTmp?.Dispose();
                hoHeightImageSampleExpanded?.Dispose();
                hoConvexRegion?.Dispose();
                hoFitConvexRegions?.Dispose();
                hoTmpConvexContours?.Dispose();
                hoTmpConvexContour?.Dispose();
                hoConvexContour?.Dispose();
                hoSampleRegion?.Dispose();
                hoSampleReduce?.Dispose();
                hoSamplePart?.Dispose();
                hoPlaneMeasureRegion?.Dispose();
                hoConvexMeasureRegion?.Dispose();
                hoPlaneMeasureRegionRing?.Dispose();
                hoPlaneMeasureReduced?.Dispose();
                hoPartSurface?.Dispose();
                hoSamplePartSub?.Dispose();
                hoSamplePartSmooth?.Dispose();
                hoSamplePartSubSmooth?.Dispose();
                hoConvexPartRegion?.Dispose();
                hoConvexFlatnessRegion?.Dispose();
                hoSamplePartMask?.Dispose();
            }
        }
    }

    private static double GetFlatnessRobust(double[] x, double[] y, double[] z)
    {
        List<Point3d> surfacePoints = ToCvPoint3d(x, y, z);

        double flatness = -1;
        if (surfacePoints.Count > 3)
        {
            List<Point3d> pointsIn = TrimByMad(surfacePoints, 3.0);
            Plane fitSurfacePlane = FitPlaneIrlsPCA(pointsIn);
            flatness = CalculateFlatness(pointsIn, fitSurfacePlane);
        }

        return flatness;
    }

    private static double GetFlatness(double[] x, double[] y, double[] z)
    {
        List<Point3d> surfacePoints = ToCvPoint3d(x, y, z);

        double flatness = -1;
        if (surfacePoints.Count > 3)
        {
            Plane fitSurfacePlane = FitPlaneIrlsPCA(surfacePoints);
            flatness = CalculateFlatness(surfacePoints, fitSurfacePlane);
        }

        return flatness;
    }

    private static List<Point3d> ToCvPoint3d(double[] x, double[] y, double[] z)
    {
        if (x == null || y == null || z == null)
            throw new ArgumentNullException("X/Y/Z cannot be null.");

        int n = Math.Min(x.Length, Math.Min(y.Length, z.Length));
        var pts = new List<Point3d>(n);
        for (int i = 0; i < n; i++)
            pts.Add(new Point3d(x[i], y[i], z[i]));
        return pts;
    }

    private static Plane FitPlaneFast(List<Point3d> points)
    {
        long n = points.Count;
        if (n < 3)
            throw new ArgumentException("Need >= 3 points.");

        double sx = 0;
        double sy = 0;
        double sz = 0;
        double sxx = 0;
        double syy = 0;
        double szz = 0;
        double sxy = 0;
        double sxz = 0;
        double syz = 0;
        foreach (Point3d p in points)
        {
            sx += p.X;
            sy += p.Y;
            sz += p.Z;
            sxx += p.X * p.X;
            syy += p.Y * p.Y;
            szz += p.Z * p.Z;
            sxy += p.X * p.Y;
            sxz += p.X * p.Z;
            syz += p.Y * p.Z;
        }

        double cx = sx / n;
        double cy = sy / n;
        double cz = sz / n;

        double cxx = sxx / n - cx * cx;
        double cyy = syy / n - cy * cy;
        double czz = szz / n - cz * cz;
        double cxy = sxy / n - cx * cy;
        double cxz = sxz / n - cx * cz;
        double cyz = syz / n - cy * cz;

        Matrix<double> matrix = Matrix<double>.Build.DenseOfArray(new[,]
        {
            { cxx, cxy, cxz },
            { cxy, cyy, cyz },
            { cxz, cyz, czz }
        });

        var evd = matrix.Evd(Symmetricity.Symmetric);
        int minIdx = 0;
        double minVal = double.MaxValue;
        for (int i = 0; i < 3; i++)
        {
            double value = evd.EigenValues[i].Real;
            if (value < minVal)
            {
                minVal = value;
                minIdx = i;
            }
        }

        Vector<double> normal = evd.EigenVectors.Column(minIdx).Normalize(2);
        double d = -(normal[0] * cx + normal[1] * cy + normal[2] * cz);

        return new Plane(normal[0], normal[1], normal[2], d);
    }

    private static Plane FitPlane(List<Point3d> points)
    {
        var matrix = Matrix<double>.Build;
        var vector = Vector<double>.Build;

        Matrix<double> data = matrix.Dense(points.Count, 3, (i, j) =>
        {
            return j switch
            {
                0 => points[i].X,
                1 => points[i].Y,
                2 => points[i].Z,
                _ => 0
            };
        });

        Vector<double> centroid = vector.DenseOfEnumerable(new[] { points.Average(p => p.X), points.Average(p => p.Y), points.Average(p => p.Z) });
        Matrix<double> centered = data - matrix.Dense(data.RowCount, 3, (i, j) => centroid[j]);

        var svd = centered.Svd();
        Vector<double> normal = svd.VT.Row(2).Normalize(2);
        double d = -normal.DotProduct(centroid);

        return new Plane(normal[0], normal[1], normal[2], d);
    }

    private static int MadStats(List<double> values, out double median, out double mad, out double sigmaHat)
    {
        double tmpMed = values.Median();
        List<double> abs = values.Select(v => Math.Abs(v - tmpMed)).ToList();

        median = tmpMed;
        mad = abs.Median();
        sigmaHat = 1.4826 * (mad + 1e-12);

        return 0;
    }

    private static List<Point3d> TrimByMad(List<Point3d> pts, double k = 3.0)
    {
        var keep = new List<Point3d>();

        if (pts.Count < 3)
            return pts;

        Plane plane = FitPlaneFast(pts);
        List<double> r = pts.Select(p => plane.DistanceTo(p)).ToList();
        MadStats(r, out double med, out double mad, out double sigmaHat);

        for (int i = 0; i < pts.Count; i++)
        {
            if (Math.Abs(r[i] - med) <= k * sigmaHat)
                keep.Add(pts[i]);
        }

        return keep;
    }

    private static Plane FitPlaneIrlsPCA(List<Point3d> pts, int maxIter = 30, double tol = 1e-6)
    {
        Plane plane = FitPlaneFast(pts);

        for (int it = 0; it < maxIter; it++)
        {
            List<double> r = pts.Select(p => plane.DistanceTo(p)).ToList();
            MadStats(r, out double med, out double mad, out double sigmaHat);
            double delta = 1.345 * sigmaHat + 1e-12;

            double[] w = r.Select(a =>
            {
                double t = Math.Abs(a);
                return t <= delta ? 1.0 : delta / t;
            }).ToArray();

            double sw = w.Sum();
            double mx = 0;
            double my = 0;
            double mz = 0;
            for (int i = 0; i < pts.Count; i++)
            {
                mx += w[i] * pts[i].X;
                my += w[i] * pts[i].Y;
                mz += w[i] * pts[i].Z;
            }

            mx /= sw;
            my /= sw;
            mz /= sw;

            double sxx = 0;
            double sxy = 0;
            double sxz = 0;
            double syy = 0;
            double syz = 0;
            double szz = 0;
            for (int i = 0; i < pts.Count; i++)
            {
                double dx = pts[i].X - mx;
                double dy = pts[i].Y - my;
                double dz = pts[i].Z - mz;
                double wi = w[i];
                sxx += wi * dx * dx;
                sxy += wi * dx * dy;
                sxz += wi * dx * dz;
                syy += wi * dy * dy;
                syz += wi * dy * dz;
                szz += wi * dz * dz;
            }

            Matrix<double> matrix = Matrix<double>.Build.DenseOfArray(new double[,]
            {
                { sxx, sxy, sxz },
                { sxy, syy, syz },
                { sxz, syz, szz }
            });

            var evd = matrix.Evd(Symmetricity.Symmetric);
            double[] evals = evd.EigenValues.Select(z => z.Real).ToArray();
            int k = Array.IndexOf(evals, evals.Min());
            Vector<double> n = evd.EigenVectors.Column(k).Normalize(2);

            var newPlane = new Plane(n[0], n[1], n[2], -(n[0] * mx + n[1] * my + n[2] * mz));

            if (Math.Abs(newPlane.A - plane.A) + Math.Abs(newPlane.B - plane.B) +
                Math.Abs(newPlane.C - plane.C) + Math.Abs(newPlane.D - plane.D) < tol)
            {
                return newPlane;
            }

            plane = newPlane;
        }

        return plane;
    }

    private static double CalculateFlatness(List<Point3d> points, Plane plane)
    {
        List<double> distances = points.Select(p => plane.DistanceTo(p)).ToList();
        return distances.Max() - distances.Min();
    }

    private static void ReplaceHobject(ref HObject target, ref HObject? source)
    {
        HObject current = target;
        if (!ReferenceEquals(current, source))
            current?.Dispose();

        target = source ?? new HObject();
        source = null;
    }

    private static HObject ReplaceHobject(HObject target, ref HObject? source)
    {
        HObject current = target;
        if (!ReferenceEquals(current, source))
            current?.Dispose();

        HObject replacement = source ?? new HObject();
        source = null;
        return replacement;
    }
}

internal sealed class ConvexMeasurementState : IDisposable
{
    private bool _disposed;
    private bool _resultDetached;

    public SensorParameters Sensor { get; init; } = new();

    public MeasurementParameters Measurement { get; init; } = new();

    public MeasurementResult Result { get; init; } = new();

    public double PhysicalIntervalZ { get; init; }

    public double CurrentIntervalX { get; set; }

    public double CurrentIntervalY { get; set; }

    public double CurrentIntervalZ { get; set; }

    public HObject GrayImage { get; set; } = new();

    public HObject HeightImage { get; set; } = new();

    public HObject ValidMask { get; set; } = new();

    public HObject ConvexRegions { get; set; } = new();

    public HTuple ConvexStandardDiameterPixel { get; set; } = new();

    public HTuple ConvexStandardHeightPixel { get; set; } = new();

    public HTuple FilterAreaThreshold { get; set; } = new();

    public static ConvexMeasurementState FromInput(ConvexMeasurementInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        HOperatorSet.GenEmptyObj(out HObject convexRegions);
        return new ConvexMeasurementState
        {
            Sensor = input.Sensor,
            Measurement = input.Measurement,
            Result = new MeasurementResult(),
            PhysicalIntervalZ = input.Sensor.IntervalZ,
            CurrentIntervalX = input.Sensor.IntervalX,
            CurrentIntervalY = input.Sensor.IntervalY,
            CurrentIntervalZ = input.Sensor.IntervalZ,
            GrayImage = input.Frame.GrayImage.Clone(),
            HeightImage = input.Frame.HeightImage.Clone(),
            ValidMask = input.Frame.ValidMask.Clone(),
            ConvexRegions = convexRegions
        };
    }

    public MeasurementResult DetachResult()
    {
        _resultDetached = true;
        return Result;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        GrayImage.Dispose();
        HeightImage.Dispose();
        ValidMask.Dispose();
        ConvexRegions.Dispose();
        ConvexStandardDiameterPixel.Dispose();
        ConvexStandardHeightPixel.Dispose();
        FilterAreaThreshold.Dispose();
        if (!_resultDetached)
            Result.Dispose();
        _disposed = true;
    }
}
