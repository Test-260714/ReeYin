using HalconDotNet;
using OpenCvSharp;
using ReeYin_V.Core.Extension;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Custom.KCJC.Models.ALGO
{
    public class KCJC0_AlgorithmMeasurePoint : KCJC0_Algorithm
    {
        private const string RegionBinaryOutputDir = @"D:\workspace\2_ReechiImageAlgorithm\CPlusPlus\01_Development\01_Products\10_Custom.KCJC\result";

        private bool _disposed = false;

        private HObject _hoConvexRegions = new HObject();
        private HObject _hoConcaveRegions = new HObject();

        private HObject _hoFitConvexRegions = new HObject();
        private HObject _hoFitConcaveRegions = new HObject();

        private HObject _hoHeightImageReduced = new HObject();

        private double _standardRadiusPixel = 1.0;
        private double _standardHeightPixel = 1.0;
        private int _autoSmallKernelSize = 3;
        private int _autoBigKernelSize = 7;
        private double _autoDynThresh = 1.0;


        public KCJC0_AlgorithmMeasurePoint()
        {
            InitVariable();
        }

        public override void Dispose()
        {
            base.Dispose();

            if (!_disposed)
            {
                _hoConvexRegions.Dispose();
                _hoConcaveRegions.Dispose();

                _hoFitConvexRegions.Dispose();
                _hoFitConcaveRegions.Dispose();

                _hoHeightImageReduced.Dispose();

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

            HOperatorSet.GenEmptyObj(out _hoConvexRegions);
            HOperatorSet.GenEmptyObj(out _hoConcaveRegions);

            HOperatorSet.GenEmptyObj(out _hoFitConvexRegions);
            HOperatorSet.GenEmptyObj(out _hoFitConcaveRegions);

            HOperatorSet.GenEmptyObj(out _hoHeightImageReduced);

            _standardRadiusPixel = 1.0;
            _standardHeightPixel = 1.0;
            _autoSmallKernelSize = 3;
            _autoBigKernelSize = 7;
            _autoDynThresh = 1.0;

            return 0;
        }

        private void UpdatePointDerivedParameters()
        {
            double standardRadiusPixel = _measureParam?.StandardRadiusPixel ?? 0.0;
            double standardHeightPixel = _measureParam?.StandardHeightPixel ?? 0.0;

            _standardRadiusPixel = double.IsFinite(standardRadiusPixel) && standardRadiusPixel > 0 ? standardRadiusPixel : 1.0;
            _standardHeightPixel = double.IsFinite(standardHeightPixel) && standardHeightPixel > 0 ? standardHeightPixel : 1.0;
            _autoSmallKernelSize = _measureParam?.AutoSmallKernelSize ?? 3;
            _autoBigKernelSize = _measureParam?.AutoBigKernelSize ?? Math.Max(_autoSmallKernelSize + 1, 7);
            _autoDynThresh = _measureParam?.AutoDynThresh ?? 1.0;

            if (!double.IsFinite(_autoDynThresh) || _autoDynThresh <= 0)
            {
                _autoDynThresh = Math.Max(1.0, _standardHeightPixel * 0.1);
            }
        }

        protected override int ScaleGrayHeightImageV2()
        {
            int status = base.ScaleGrayHeightImageV2();
            if (status == 0)
            {
                UpdatePointDerivedParameters();
            }
            else
            {
                _standardRadiusPixel = 1.0;
                _standardHeightPixel = 1.0;
                _autoSmallKernelSize = 3;
                _autoBigKernelSize = 7;
                _autoDynThresh = 1.0;
            }

            return status;
        }


        /// <summary>
        /// 凸包与凹坑区域定位
        /// </summary>
        private int DetectPlateConvexAndConcave()
        {
            using (var dh = new HDevDisposeHelper())
            {
                HObject? hoTmp = null;

                HObject? hoHeightImageMeanS = null;
                HObject? hoHeightImageMeanB = null;
                HObject? hoTmpDynThreshRegion = null;
                HTuple hvShapeFeatures = new HTuple();
                HTuple hvMinValues = new HTuple();
                HTuple hvMaxValues = new HTuple();

                try
                {
                    ResetObject(ref _hoConvexRegions);
                    ResetObject(ref _hoConcaveRegions);
                    ResetObject(ref _hoHeightImageReduced);

                    if (!IsValidHalconObject(_hoHeightImage) || !IsValidHalconObject(_hoPlateRegion))
                    {
                        return -1;
                    }

                    int smallKernelSize = Math.Max(3, _autoSmallKernelSize);
                    int bigKernelSize = Math.Max(smallKernelSize + 1, _autoBigKernelSize);
                    double standardRadiusPixel = _standardRadiusPixel > 0 ? _standardRadiusPixel : 1.0;
                    double filterAreaThresh = Math.PI * standardRadiusPixel * standardRadiusPixel;

                    hvShapeFeatures = new HTuple("circularity").TupleConcat("area");
                    hvMinValues = new HTuple(0.5).TupleConcat(Math.Max(1.0, filterAreaThresh * 0.2));
                    hvMaxValues = new HTuple(1.0).TupleConcat(Math.Max(1.0, filterAreaThresh * 2.0));

                    HOperatorSet.ReduceDomain(_hoHeightImage, _hoPlateRegion, out hoTmp);
                    ReplaceHobject(ref _hoHeightImageReduced, ref hoTmp);

                    if (!IsValidHalconObject(_hoHeightImageReduced))
                    {
                        return -1;
                    }

                    HOperatorSet.MeanImage(_hoHeightImageReduced, out hoHeightImageMeanS,
                                           smallKernelSize, smallKernelSize);
                    HOperatorSet.MeanImage(_hoHeightImageReduced, out hoHeightImageMeanB,
                                           bigKernelSize, bigKernelSize);

                    // 凸包定位
                    HOperatorSet.DynThreshold(hoHeightImageMeanS, hoHeightImageMeanB, out hoTmpDynThreshRegion, _autoDynThresh, "light");
                    HOperatorSet.OpeningCircle(hoTmpDynThreshRegion, out hoTmp, 20);
                    ReplaceHobject(ref hoTmpDynThreshRegion, ref hoTmp);
                    HOperatorSet.ClosingCircle(hoTmpDynThreshRegion, out hoTmp, 20);
                    ReplaceHobject(ref _hoConvexRegions, ref hoTmp);
                    // 凸包区域筛选
                    HOperatorSet.Connection(_hoConvexRegions, out hoTmp);
                    ReplaceHobject(ref _hoConvexRegions, ref hoTmp);
                    HOperatorSet.SelectShape(_hoConvexRegions, out hoTmp, hvShapeFeatures, "and", hvMinValues, hvMaxValues);
                    ReplaceHobject(ref _hoConvexRegions, ref hoTmp);

                    // 凹坑定位
                    HOperatorSet.DynThreshold(hoHeightImageMeanS, hoHeightImageMeanB, out hoTmp, _autoDynThresh, "dark");
                    ReplaceHobject(ref hoTmpDynThreshRegion, ref hoTmp);
                    HOperatorSet.OpeningCircle(hoTmpDynThreshRegion, out hoTmp, 20);
                    ReplaceHobject(ref hoTmpDynThreshRegion, ref hoTmp);
                    HOperatorSet.ClosingCircle(hoTmpDynThreshRegion, out hoTmp, 20);
                    ReplaceHobject(ref _hoConcaveRegions, ref hoTmp);
                    // 凹坑区域筛选
                    HOperatorSet.Connection(_hoConcaveRegions, out hoTmp);
                    ReplaceHobject(ref _hoConcaveRegions, ref hoTmp);
                    HOperatorSet.SelectShape(_hoConcaveRegions, out hoTmp, hvShapeFeatures, "and", hvMinValues, hvMaxValues);
                    ReplaceHobject(ref _hoConcaveRegions, ref hoTmp);

                    //SaveRegionAsBinaryImage(_hoConvexRegions, "ConvexRegionsBin.png");
                    //SaveRegionAsBinaryImage(_hoConcaveRegions, "ConcaveRegionsBin.png");

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    ResetObject(ref _hoConvexRegions);
                    ResetObject(ref _hoConcaveRegions);
                    ResetObject(ref _hoHeightImageReduced);
                    return -1;
                }
                finally
                {
                    hoTmp?.Dispose();

                    hoHeightImageMeanS?.Dispose();
                    hoHeightImageMeanB?.Dispose();
                    hoTmpDynThreshRegion?.Dispose();
                    hvShapeFeatures.Dispose();
                    hvMinValues.Dispose();
                    hvMaxValues.Dispose();
                }
            }

            return 0;
        }

        private void SaveRegionAsBinaryImage(HObject? hoRegion, string fileName)
        {
            using (var dh = new HDevDisposeHelper())
            {
                HObject? hoRegionBinImage = null;
                HObject? hoSourceRegion = null;
                HTuple hvWidth = new HTuple();
                HTuple hvHeight = new HTuple();

                try
                {
                    if (!IsValidHalconObject(_hoGrayImage))
                    {
                        return;
                    }

                    HOperatorSet.GetImageSize(_hoGrayImage, out hvWidth, out hvHeight);
                    if (hvWidth.Length <= 0 || hvHeight.Length <= 0 || hvWidth.I <= 0 || hvHeight.I <= 0)
                    {
                        return;
                    }

                    Directory.CreateDirectory(RegionBinaryOutputDir);

                    if (IsValidHalconObject(hoRegion))
                    {
                        hoSourceRegion = hoRegion!;
                    }
                    else
                    {
                        HOperatorSet.GenEmptyRegion(out hoSourceRegion);
                    }

                    HOperatorSet.RegionToBin(hoSourceRegion, out hoRegionBinImage, 255, 0, hvWidth, hvHeight);
                    if (!IsValidHalconObject(hoRegionBinImage))
                    {
                        return;
                    }

                    string filePath = Path.Combine(RegionBinaryOutputDir, fileName);
                    HOperatorSet.WriteImage(hoRegionBinImage, "png", 0, filePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                finally
                {
                    hoRegionBinImage?.Dispose();
                    if (hoSourceRegion != null && !ReferenceEquals(hoSourceRegion, hoRegion))
                    {
                        hoSourceRegion.Dispose();
                    }

                    hvWidth.Dispose();
                    hvHeight.Dispose();
                }
            }
        }

        private static bool IsValidHalconObject(HObject? obj)
        {
            return obj != null && obj.IsInitialized();
        }

        private static void ResetObject(ref HObject target)
        {
            HOperatorSet.GenEmptyObj(out HObject hoEmpty);
            ReplaceHobject(ref target, ref hoEmpty);
        }

        private static int GetObjectCount(HObject? hoObject)
        {
            if (!IsValidHalconObject(hoObject))
            {
                return 0;
            }

            HOperatorSet.CountObj(hoObject!, out HTuple hvCount);
            try
            {
                return hvCount.I;
            }
            finally
            {
                hvCount.Dispose();
            }
        }

        private bool TryGetRegionSmallestCircle(HObject region, out double row, out double col, out double radius)
        {
            using (var dh = new HDevDisposeHelper())
            {
                HObject? hoContour = null;

                HTuple hvRow = new HTuple();
                HTuple hvCol = new HTuple();
                HTuple hvRadius = new HTuple();

                row = -1;
                col = -1;
                radius = -1;

                try
                {
                    if (GetObjectCount(region) <= 0)
                    {
                        return false;
                    }

                    HOperatorSet.GenContourRegionXld(region, out hoContour, "border");
                    if (GetObjectCount(hoContour) <= 0)
                    {
                        return false;
                    }

                    HOperatorSet.SmallestCircleXld(hoContour, out hvRow, out hvCol, out hvRadius);
                    if (hvRadius.Length <= 0)
                    {
                        return false;
                    }

                    row = hvRow.D;
                    col = hvCol.D;
                    radius = hvRadius.D;

                    return row >= 0 && col >= 0 && radius > 0;
                }
                finally
                {
                    hoContour?.Dispose();

                    hvRow.Dispose();
                    hvCol.Dispose();
                    hvRadius.Dispose();
                }
            }
        }

        private bool TrySelectBestCircleContour(HObject contours, HObject referenceRegion, double rowOffset, double colOffset,
                                                out HObject bestContour, out double bestContourLength)
        {
            using (var dh = new HDevDisposeHelper())
            {
                HObject? hoCurrentContour = null;
                HObject? hoBestContourCandidate = null;
                HObject? hoReferenceRegionErosion = null;

                try
                {
                    HOperatorSet.GenEmptyObj(out bestContour);

                    bestContourLength = -1;
                    double bestArcError = double.MaxValue;
                    double arcErrorTieTol = 0.1;
                    double minArcAngleRad = 240.0 * Math.PI / 180.0;
                    bool validContourFound = false;
                    bool isValidConvex = true;
                    double erosionRadius = 5.0;

                    int contourCount = GetObjectCount(contours);
                    if (contourCount <= 0 || GetObjectCount(referenceRegion) <= 0)
                    {
                        return false;
                    }

                    HOperatorSet.ErosionCircle(referenceRegion, out hoReferenceRegionErosion, erosionRadius);
                    if (GetObjectCount(hoReferenceRegionErosion) <= 0)
                    {
                        return false;
                    }

                    for (int contourIndex = 1; contourIndex <= contourCount; contourIndex++)
                    {
                        HObject? hoCurrentFitRegion = null;
                        HObject? hoCurrentFitRegionMoved = null;
                        HObject? hoCurrentFitRegionErosion = null;
                        HObject? hoTmpIntersection = null;

                        HTuple hvContourLength = new HTuple();
                        HTuple hvCircleRowTmp = new HTuple();
                        HTuple hvCircleColTmp = new HTuple();
                        HTuple hvCircleRadiusTmp = new HTuple();
                        HTuple hvStartPhiTmp = new HTuple();
                        HTuple hvEndPhiTmp = new HTuple();
                        HTuple hvPointOrderTmp = new HTuple();
                        HTuple hvRowsTmp = new HTuple();
                        HTuple hvColsTmp = new HTuple();
                        HTuple hvDistToCenterTmp = new HTuple();
                        HTuple hvRadiusResidualTmp = new HTuple();
                        HTuple hvArcErrorTmp = new HTuple();
                        HTuple hvContourIntersectionAreaTmp = new HTuple();
                        HTuple hvTmpRow = new HTuple();
                        HTuple hvTmpCol = new HTuple();

                        try
                        {
                            HOperatorSet.SelectObj(contours, out hoCurrentContour, contourIndex);
                            if (GetObjectCount(hoCurrentContour) <= 0)
                            {
                                continue;
                            }

                            HOperatorSet.LengthXld(hoCurrentContour, out hvContourLength);
                            double contourLength = hvContourLength.Length > 0 ? hvContourLength.D : -1;

                            HOperatorSet.FitCircleContourXld(hoCurrentContour, "geotukey", -1, 3.5, 0, 5, 2,
                                                             out hvCircleRowTmp, out hvCircleColTmp, out hvCircleRadiusTmp,
                                                             out hvStartPhiTmp, out hvEndPhiTmp, out hvPointOrderTmp);
                            if (hvCircleRadiusTmp.Length <= 0 || hvCircleRadiusTmp.D <= 0)
                            {
                                continue;
                            }

                            double arcSpanRad;
                            if (hvPointOrderTmp.Length > 0 &&
                                string.Equals(hvPointOrderTmp.S, "positive", StringComparison.OrdinalIgnoreCase))
                            {
                                arcSpanRad = hvEndPhiTmp.D - hvStartPhiTmp.D;
                            }
                            else
                            {
                                arcSpanRad = hvStartPhiTmp.D - hvEndPhiTmp.D;
                            }

                            if (arcSpanRad < 0)
                            {
                                arcSpanRad += 2.0 * Math.PI;
                            }

                            if (arcSpanRad < minArcAngleRad)
                            {
                                continue;
                            }

                            // 轮廓转区域后映射回全局坐标，与侵蚀后的原始区域求交，
                            // 若交集面积不大于 0，则认为中心区域存在明显丢数据，整段轮廓拟合无效。
                            HOperatorSet.GenRegionContourXld(hoCurrentContour, out hoCurrentFitRegion, "filled");
                            if (GetObjectCount(hoCurrentFitRegion) <= 0)
                            {
                                continue;
                            }

                            HOperatorSet.MoveRegion(hoCurrentFitRegion, out hoCurrentFitRegionMoved, rowOffset, colOffset);
                            if (GetObjectCount(hoCurrentFitRegionMoved) <= 0)
                            {
                                continue;
                            }

                            HOperatorSet.ErosionCircle(hoCurrentFitRegionMoved, out hoCurrentFitRegionErosion, erosionRadius);
                            if (GetObjectCount(hoCurrentFitRegionErosion) <= 0)
                            {
                                continue;
                            }

                            HOperatorSet.Intersection(hoCurrentFitRegionErosion, hoReferenceRegionErosion, out hoTmpIntersection);
                            HOperatorSet.AreaCenter(hoTmpIntersection, out hvContourIntersectionAreaTmp, out hvTmpRow, out hvTmpCol);
                            double contourIntersectionArea = hvContourIntersectionAreaTmp.Length > 0 ? hvContourIntersectionAreaTmp.D : 0.0;
                            if (contourIntersectionArea <= 0)
                            {
                                isValidConvex = false;
                            }

                            HOperatorSet.GetContourXld(hoCurrentContour, out hvRowsTmp, out hvColsTmp);
                            if (hvRowsTmp.Length <= 0 || hvColsTmp.Length <= 0)
                            {
                                continue;
                            }

                            HOperatorSet.DistancePp(hvRowsTmp, hvColsTmp, hvCircleRowTmp, hvCircleColTmp, out hvDistToCenterTmp);
                            hvRadiusResidualTmp = (hvDistToCenterTmp - hvCircleRadiusTmp).TupleAbs();
                            HOperatorSet.TupleMean(hvRadiusResidualTmp, out hvArcErrorTmp);
                            double arcError = hvArcErrorTmp.Length > 0 ? hvArcErrorTmp.D : double.MaxValue;

                            if (contourIntersectionArea > 0 &&
                                (!validContourFound ||
                                 arcError < bestArcError - arcErrorTieTol ||
                                 (Math.Abs(arcError - bestArcError) <= arcErrorTieTol && contourLength > bestContourLength)))
                            {
                                validContourFound = true;
                                bestArcError = arcError;
                                bestContourLength = contourLength;

                                HOperatorSet.SelectObj(contours, out hoBestContourCandidate, contourIndex);
                                ReplaceHobject(ref bestContour, ref hoBestContourCandidate);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                        finally
                        {
                            hoCurrentContour?.Dispose();
                            hoCurrentContour = null;

                            hoCurrentFitRegion?.Dispose();
                            hoCurrentFitRegionMoved?.Dispose();
                            hoCurrentFitRegionErosion?.Dispose();
                            hoTmpIntersection?.Dispose();

                            hvContourLength.Dispose();
                            hvCircleRowTmp.Dispose();
                            hvCircleColTmp.Dispose();
                            hvCircleRadiusTmp.Dispose();
                            hvStartPhiTmp.Dispose();
                            hvEndPhiTmp.Dispose();
                            hvPointOrderTmp.Dispose();
                            hvRowsTmp.Dispose();
                            hvColsTmp.Dispose();
                            hvDistToCenterTmp.Dispose();
                            hvRadiusResidualTmp.Dispose();
                            hvArcErrorTmp.Dispose();
                            hvContourIntersectionAreaTmp.Dispose();
                            hvTmpRow.Dispose();
                            hvTmpCol.Dispose();
                        }

                    }
                    return validContourFound && isValidConvex && GetObjectCount(bestContour) > 0;
                }
                finally
                {
                    hoReferenceRegionErosion?.Dispose();
                }
            }
        }

        private bool TrySelectCandidateContoursByLength(HObject localContours, double minContourLength, out HObject candidateContours)
        {
            using (var dh = new HDevDisposeHelper())
            {
                HOperatorSet.GenEmptyObj(out candidateContours);
                HObject? hoSelectedContours = null;

                try
                {
                    if (GetObjectCount(localContours) <= 0)
                    {
                        return false;
                    }

                    HOperatorSet.SelectContoursXld(localContours, out hoSelectedContours, "contour_length",
                                                   minContourLength, 9999999999.0, 0.0, 0.0);
                    if (GetObjectCount(hoSelectedContours) <= 0)
                    {
                        return false;
                    }

                    ReplaceHobject(ref candidateContours, ref hoSelectedContours);
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    return false;
                }
                finally
                {
                    hoSelectedContours?.Dispose();
                }
            }
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

        private bool TryGetHeightAndMeasurePoint(HObject hoImageSub, HObject hoMeasureRing, HObject hoFitRegion, bool isConvex,
                                                 double rowOffset, double colOffset,
                                                 out double heightPixel, out double measurePointX, out double measurePointY)
        {
            using (var dh = new HDevDisposeHelper())
            {
                HObject? hoImageSubRing = null;
                HObject? hoImageSubSmooth = null;

                HTuple hvMeanValue = new HTuple();
                HTuple hvFitRegionRows = new HTuple();
                HTuple hvFitRegionColumns = new HTuple();
                HTuple hvGrayValues = new HTuple();
                HTuple hvIndicesInc = new HTuple();

                heightPixel = double.NaN;
                measurePointX = -1;
                measurePointY = -1;

                try
                {
                    if (GetObjectCount(hoMeasureRing) <= 0 || GetObjectCount(hoFitRegion) <= 0)
                    {
                        return false;
                    }

                    HOperatorSet.ReduceDomain(hoImageSub, hoMeasureRing, out hoImageSubRing);
                    HOperatorSet.GrayFeatures(hoMeasureRing, hoImageSubRing, "mean", out hvMeanValue);
                    if (hvMeanValue.Length <= 0)
                    {
                        return false;
                    }

                    HOperatorSet.MedianImage(hoImageSub, out hoImageSubSmooth, "circle", 7, "mirrored");
                    HOperatorSet.GetRegionPoints(hoFitRegion, out hvFitRegionRows, out hvFitRegionColumns);
                    if (hvFitRegionRows.Length <= 0 || hvFitRegionColumns.Length <= 0)
                    {
                        return false;
                    }

                    HOperatorSet.GetGrayval(hoImageSubSmooth, hvFitRegionRows, hvFitRegionColumns, out hvGrayValues);
                    if (hvGrayValues.Length <= 0)
                    {
                        return false;
                    }

                    HOperatorSet.TupleSortIndex(hvGrayValues, out hvIndicesInc);
                    int percentileIndex = GetPercentileIndex(hvIndicesInc.TupleLength(), isConvex ? 0.99 : 0.01);
                    if (percentileIndex < 0)
                    {
                        return false;
                    }

                    HTuple hvSelectIdx = hvIndicesInc.TupleSelect(percentileIndex);
                    try
                    {
                        double selectedValue = hvGrayValues.TupleSelect(hvSelectIdx).D;
                        heightPixel = Math.Abs(selectedValue - hvMeanValue.D);
                        measurePointX = hvFitRegionColumns.TupleSelect(hvSelectIdx).D + colOffset;
                        measurePointY = hvFitRegionRows.TupleSelect(hvSelectIdx).D + rowOffset;
                    }
                    finally
                    {
                        hvSelectIdx.Dispose();
                    }

                    return double.IsFinite(heightPixel) &&
                           double.IsFinite(measurePointX) &&
                           double.IsFinite(measurePointY);
                }
                finally
                {
                    hoImageSubRing?.Dispose();
                    hoImageSubSmooth?.Dispose();

                    hvMeanValue.Dispose();
                    hvFitRegionRows.Dispose();
                    hvFitRegionColumns.Dispose();
                    hvGrayValues.Dispose();
                    hvIndicesInc.Dispose();
                }
            }
        }


        /// <summary>
        /// 计算凸包间或凹坑间最小间距
        /// 为了减少计算量，默认所有压印近似等间距
        /// </summary>
        private double GetRegionMInDistance(HObject hoRegions)
        {
            double minDistance = double.MaxValue;

            using (var dh = new HDevDisposeHelper())
            {
                HObject? hoRegion1 = null;
                HObject? hoRegion2 = null;

                HTuple hvMinDistanceList = new HTuple();

                try
                {
                    int regionsNum = GetObjectCount(hoRegions);
                    if (regionsNum > 1)
                    {
                        for (int i = 1; i <= regionsNum; i++)
                        {
                            HOperatorSet.SelectObj(hoRegions, out hoRegion1, i);
                            if (!TryGetRegionSmallestCircle(hoRegion1, out double row1, out double col1, out _))
                            {
                                hoRegion1?.Dispose();
                                hoRegion1 = null;
                                continue;
                            }

                            HTuple hvMinDistanceTmp = new HTuple(999999999999999);

                            for (int j = 1; j <= regionsNum; j++)
                            {
                                if(i == j)
                                {
                                    continue;
                                }
                                HOperatorSet.SelectObj(hoRegions, out hoRegion2, j);
                                if (!TryGetRegionSmallestCircle(hoRegion2, out double row2, out double col2, out _))
                                {
                                    hoRegion2?.Dispose();
                                    hoRegion2 = null;
                                    continue;
                                }

                                double distance = Math.Sqrt(Math.Pow(row1 - row2, 2) + Math.Pow(col1 - col2, 2));
                                if (distance < hvMinDistanceTmp.D)
                                {
                                    hvMinDistanceTmp.Dispose();
                                    hvMinDistanceTmp = new HTuple(distance);
                                }

                                hoRegion2?.Dispose();
                                hoRegion2 = null;
                            }

                            hvMinDistanceList = hvMinDistanceList.TupleConcat(hvMinDistanceTmp);

                            hoRegion1?.Dispose();
                            hoRegion1 = null;
                        }

                        minDistance = hvMinDistanceList.TupleMean();
                    }
                    else if (regionsNum == 1)
                    {
                        HOperatorSet.SelectObj(hoRegions, out hoRegion1, 1);
                        if (TryGetRegionSmallestCircle(hoRegion1, out _, out _, out double radius1))
                        {
                            minDistance = radius1 * 2.5;
                        }
                        else
                        {
                            minDistance = 1;
                        }
                    }
                    else
                    {
                        minDistance = 1;
                    }

                    if (!double.IsFinite(minDistance) || minDistance <= 0 || minDistance == double.MaxValue)
                    {
                        minDistance = 1;
                    }
                }
                finally
                {
                    hoRegion1?.Dispose();
                    hoRegion2?.Dispose();
                }
            }

            return minDistance;
        }



        /// <summary>
        /// 提取凸包凹坑轮廓
        /// </summary>
        private int DetectConvexAndConcaveEdge(string type, ref HObject hoFitRegions)
        {
            using (var dh = new HDevDisposeHelper())
            {
                HObject? hoRegion = null;
                List<KCJC0_ConvexConcaveResult>? targetResults = null;

                try
                {
                    bool isConvex;
                    if (string.Equals(type, "Convex", StringComparison.OrdinalIgnoreCase))
                    {
                        isConvex = true;
                        targetResults = _measureResult.ConvexResultsList;
                    }
                    else if (string.Equals(type, "Concave", StringComparison.OrdinalIgnoreCase))
                    {
                        isConvex = false;
                        targetResults = _measureResult.ConcaveResultsList;
                    }
                    else
                    {
                        ResetObject(ref hoFitRegions);
                        return -1;
                    }

                    ResetObject(ref hoFitRegions);
                    targetResults.Clear();

                    HObject sourceRegions = isConvex ? _hoConvexRegions : _hoConcaveRegions;
                    HObject sourceHeightImage = !isConvex && IsValidHalconObject(_hoHeightImageReduced)
                        ? _hoHeightImageReduced
                        : _hoHeightImage;

                    int regionCount = GetObjectCount(sourceRegions);
                    if (regionCount <= 0 || !IsValidHalconObject(sourceHeightImage) || !IsValidHalconObject(_hoGrayImage))
                    {
                        return 0;
                    }

                    HOperatorSet.GetImageSize(_hoGrayImage, out HTuple hvImageW, out HTuple hvImageH);
                    double imageWidth = hvImageW.D;
                    double imageHeight = hvImageH.D;
                    hvImageW.Dispose();
                    hvImageH.Dispose();

                    double minDistance = GetRegionMInDistance(sourceRegions);
                    for (int idx = 1; idx <= regionCount; idx++)
                    {
                        HOperatorSet.SelectObj(sourceRegions, out hoRegion, idx);

                        KCJC0_ConvexConcaveResult? result = TryMeasureSingleConvexConcaveRegion(
                            hoRegion, sourceHeightImage, isConvex, minDistance, imageWidth, imageHeight, ref hoFitRegions);

                        if (result != null)
                        {
                            targetResults.Add(result);
                        }

                        hoRegion?.Dispose();
                        hoRegion = null;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    ResetObject(ref hoFitRegions);
                    targetResults?.Clear();
                    return -1;
                }
                finally
                {
                    hoRegion?.Dispose();
                }
            }

            return 0;
        }

        private KCJC0_ConvexConcaveResult? TryMeasureSingleConvexConcaveRegion(HObject hoRegion, HObject sourceHeightImage, bool isConvex,
                                                                                double minDistance, double imageWidth, double imageHeight,
                                                                                ref HObject hoFitRegions)
        {
            using (var dh = new HDevDisposeHelper())
            {
                HObject? hoCircle = null;
                HObject? hoCircleReduced = null;
                HObject? hoCircleValidRegion = null;
                HObject? hoCirclePart = null;
                HObject? hoMeasurePartCircle = null;
                HObject? hoMeasurePartBuffer = null;
                HObject? hoMeasurePartRing = null;
                HObject? hoMeasurePartRingReduced = null;
                HObject? hoFitSurface = null;
                HObject? hoImageSub = null;
                HObject? hoLocalContours = null;
                HObject? hoCandidateContours = null;
                HObject? hoBestContour = null;
                HObject? hoFitRegionLocal = null;
                HObject? hoFitRegionGlobal = null;
                HObject? hoTmpIntersection = null;
                HObject? hoRegionOpening = null;
                HObject? hoTmp = null;

                HTuple hvCircleWidth = new HTuple();
                HTuple hvCircleHeight = new HTuple();
                HTuple hvPointer = new HTuple();
                HTuple hvType = new HTuple();
                HTuple hvPartWidth = new HTuple();
                HTuple hvPartHeight = new HTuple();
                HTuple hvCenter2EdgeDistance = new HTuple();
                HTuple hvAlpha = new HTuple();
                HTuple hvBeta = new HTuple();
                HTuple hvGamma = new HTuple();
                HTuple hvPeakHeightGray = new HTuple();
                HTuple hvCircleRow = new HTuple();
                HTuple hvCircleCol = new HTuple();
                HTuple hvCircleRadius = new HTuple();
                HTuple hvStartPhi = new HTuple();
                HTuple hvEndPhi = new HTuple();
                HTuple hvPointOrder = new HTuple();
                HTuple hvCircleOuterRadius = new HTuple();
                HTuple hvTmpArea = new HTuple();
                HTuple hvTmpRow = new HTuple();
                HTuple hvTmpCol = new HTuple();

                try
                {
                    if (!TryGetRegionSmallestCircle(hoRegion, out double coreRow, out double coreCol, out double coreRadius))
                    {
                        return null;
                    }

                    double sampleRadius;
                    double measureRadius;
                    if (minDistance < imageWidth * 0.85)
                    {
                        sampleRadius = minDistance - coreRadius;
                        measureRadius = minDistance * 0.45;
                    }
                    else
                    {
                        sampleRadius = coreRadius * 3.0;
                        measureRadius = coreRadius * 2.0;
                    }

                    if (!double.IsFinite(sampleRadius) || !double.IsFinite(measureRadius) ||
                        sampleRadius <= 1 || measureRadius <= 1)
                    {
                        return null;
                    }

                    HOperatorSet.GenCircle(out hoCircle, coreRow, coreCol, sampleRadius);
                    HOperatorSet.ReduceDomain(sourceHeightImage, hoCircle, out hoCircleReduced);
                    HOperatorSet.Intersection(hoCircle, _hoValidRegion, out hoCircleValidRegion);

                    HOperatorSet.CropDomain(hoCircleReduced, out hoCirclePart);
                    HOperatorSet.GetImagePointer1(hoCirclePart, out hvPointer, out hvType, out hvPartWidth, out hvPartHeight);
                    if (hvPartWidth.Length <= 0 || hvPartHeight.Length <= 0 ||
                        hvPartWidth.D <= 1 || hvPartHeight.D <= 1)
                    {
                        return null;
                    }

                    HOperatorSet.RegionFeatures(hoCircle, "width", out hvCircleWidth);
                    HOperatorSet.RegionFeatures(hoCircle, "height", out hvCircleHeight);
                    if (hvCircleWidth.Length <= 0 || hvCircleHeight.Length <= 0)
                    {
                        return null;
                    }

                    double circleCenterCol;
                    if (coreCol < sampleRadius)
                    {
                        circleCenterCol = coreCol - (hvCircleWidth.D - hvPartWidth.D);
                    }
                    else
                    {
                        HOperatorSet.DistancePl(coreRow, coreCol, _hvLeftTopRow, _hvLeftTopColumn,
                                                _hvLeftDownRow, _hvLeftDownColumn, out hvCenter2EdgeDistance);
                        circleCenterCol = hvCenter2EdgeDistance.Length > 0 && hvCenter2EdgeDistance.D > sampleRadius
                            ? sampleRadius
                            : sampleRadius - (hvCircleWidth.D - hvPartWidth.D);
                    }

                    double circleCenterRow;
                    if (coreRow < sampleRadius)
                    {
                        circleCenterRow = coreRow - (hvCircleHeight.D - hvPartHeight.D);
                    }
                    else
                    {
                        hvCenter2EdgeDistance.Dispose();
                        hvCenter2EdgeDistance = new HTuple();
                        HOperatorSet.DistancePl(coreRow, coreCol, _hvLeftTopRow, _hvLeftTopColumn,
                                                _hvRightTopRow, _hvRightTopColumn, out hvCenter2EdgeDistance);
                        circleCenterRow = hvCenter2EdgeDistance.Length > 0 && hvCenter2EdgeDistance.D > sampleRadius
                            ? sampleRadius
                            : sampleRadius - (hvCircleHeight.D - hvPartHeight.D);
                    }

                    HOperatorSet.GenCircle(out hoMeasurePartCircle, circleCenterRow, circleCenterCol, measureRadius);
                    double ringWidth = Math.Max(0.0, Math.Truncate(minDistance * 0.1));
                    HOperatorSet.DilationCircle(hoMeasurePartCircle, out hoMeasurePartBuffer, ringWidth);
                    HOperatorSet.Difference(hoMeasurePartBuffer, hoMeasurePartCircle, out hoMeasurePartRing);

                    if (GetObjectCount(hoMeasurePartRing) <= 0)
                    {
                        return null;
                    }

                    HOperatorSet.ReduceDomain(hoCirclePart, hoMeasurePartRing, out hoMeasurePartRingReduced);
                    HOperatorSet.FitSurfaceFirstOrder(hoMeasurePartRing, hoMeasurePartRingReduced, "tukey", 5, 1,
                                                      out hvAlpha, out hvBeta, out hvGamma);
                    HOperatorSet.GenImageSurfaceFirstOrder(out hoFitSurface, "real", hvAlpha, hvBeta, hvGamma,
                                                           circleCenterRow, circleCenterCol, hvPartWidth, hvPartHeight);
                    HOperatorSet.SubImage(hoCirclePart, hoFitSurface, out hoImageSub, 1, 0);

                    HOperatorSet.GrayFeatures(hoMeasurePartCircle, hoImageSub, isConvex ? "max" : "min", out hvPeakHeightGray);
                    if (hvPeakHeightGray.Length <= 0 || !double.IsFinite(hvPeakHeightGray.D))
                    {
                        return null;
                    }

                    double standardRadiusPixel = _standardRadiusPixel > 0 ? _standardRadiusPixel : 1.0;
                    double minContourLength = 2.0 * 3.1415926 * standardRadiusPixel * 0.24;
                    double thresholdRatio = isConvex ? 0.3 : 0.2;
                    HOperatorSet.ThresholdSubPix(hoImageSub, out hoLocalContours, hvPeakHeightGray.D * thresholdRatio);
                    bool hasFitRegionGlobal = false;
                    double radiusPixel = -1;
                    double heightPixel = double.NaN;
                    double measurePointX = coreCol;
                    double measurePointY = coreRow;
                    double rowOffset = coreRow - circleCenterRow;
                    double colOffset = coreCol - circleCenterCol;
                    int localContourCount = GetObjectCount(hoLocalContours);
                    if (localContourCount > 0)
                    {
                        bool hasCandidateContours = TrySelectCandidateContoursByLength(hoLocalContours, minContourLength, out hoCandidateContours);
                        bool hasBestContour = false;
                        double bestContourLength = -1;
                        if (hasCandidateContours)
                        {
                            hasBestContour = TrySelectBestCircleContour(hoCandidateContours, hoRegion, rowOffset, colOffset,
                                                                       out hoBestContour, out bestContourLength);
                        }

                        double maxContourLength = -1;
                        if (hasBestContour)
                        {
                            HOperatorSet.GenRegionContourXld(hoBestContour, out hoFitRegionLocal, "filled");
                            if (GetObjectCount(hoFitRegionLocal) > 0)
                            {
                                HOperatorSet.MoveRegion(hoFitRegionLocal, out hoFitRegionGlobal, rowOffset, colOffset);
                                if (GetObjectCount(hoFitRegionGlobal) > 0)
                                {
                                    HOperatorSet.Intersection(hoFitRegionGlobal, hoRegion, out hoTmpIntersection);
                                    HOperatorSet.AreaCenter(hoTmpIntersection, out hvTmpArea, out hvTmpRow, out hvTmpCol);

                                    if (hvTmpArea.Length > 0 && hvTmpArea.D > 0)
                                    {
                                        HOperatorSet.ConcatObj(hoFitRegions, hoFitRegionGlobal, out hoTmp);
                                        ReplaceHobject(ref hoFitRegions, ref hoTmp);
                                        hasFitRegionGlobal = true;
                                        maxContourLength = bestContourLength;
                                    }
                                    else
                                    {
                                        maxContourLength = -1;
                                    }
                                }
                                else
                                {
                                    maxContourLength = -1;
                                }
                            }
                        }

                        if (maxContourLength >= 10.0 && GetObjectCount(hoBestContour) > 0 && GetObjectCount(hoFitRegionLocal) > 0)
                        {
                            HOperatorSet.FitCircleContourXld(hoBestContour, "geotukey", -1, 3.5, 0, 5, 2,
                                                             out hvCircleRow, out hvCircleCol, out hvCircleRadius,
                                                             out hvStartPhi, out hvEndPhi, out hvPointOrder);
                            if (hvCircleRadius.Length > 0 && hvCircleRadius.D > 0)
                            {
                                if (hvCircleRow.Length > 0 && hvCircleCol.Length > 0 &&
                                    double.IsFinite(hvCircleRow.D) && double.IsFinite(hvCircleCol.D))
                                {
                                    double fittedCircleRow = hvCircleRow.D + rowOffset;
                                    double fittedCircleCol = hvCircleCol.D + colOffset;
                                    
                                    //double minEdgeDistance = Math.Min(
                                    //    Math.Min(fittedCircleRow, imageHeight - 1.0 - fittedCircleRow),
                                    //    Math.Min(fittedCircleCol, imageWidth - 1.0 - fittedCircleCol));

                                    HOperatorSet.DistancePl(new HTuple(fittedCircleRow), new HTuple(fittedCircleCol), _hvLeftTopRow, _hvLeftTopColumn, _hvRightTopRow, _hvRightTopColumn, out HTuple minEdgeDistance1);
                                    HOperatorSet.DistancePl(new HTuple(fittedCircleRow), new HTuple(fittedCircleCol), _hvRightTopRow, _hvRightTopColumn, _hvRightDownRow, _hvRightDownColumn, out HTuple minEdgeDistance2);
                                    HOperatorSet.DistancePl(new HTuple(fittedCircleRow), new HTuple(fittedCircleCol), _hvRightDownRow, _hvRightDownColumn, _hvLeftDownRow, _hvLeftDownColumn, out HTuple minEdgeDistance3);
                                    HOperatorSet.DistancePl(new HTuple(fittedCircleRow), new HTuple(fittedCircleCol), _hvLeftDownRow, _hvLeftDownColumn, _hvLeftTopRow, _hvLeftTopColumn, out HTuple minEdgeDistance4);

                                    double minEdgeDistance = Math.Abs(Math.Min(Math.Min(minEdgeDistance1.D, minEdgeDistance2.D),
                                                                               Math.Min(minEdgeDistance3.D, minEdgeDistance4.D)));

                                    // 过滤距离极片边缘小于0.95倍半径的凸点
                                    // if (minEdgeDistance < hvCircleRadius.D * 0.95)
                                    if (minEdgeDistance < hvCircleRadius.D * 0.75)
                                    {
                                        return null;
                                    }

                                    // 只保留图片中间凸点
                                    HOperatorSet.DistancePl(new HTuple(fittedCircleRow), new HTuple(fittedCircleCol), 
                                                            0, new HTuple(0.5 * imageWidth), imageHeight, new HTuple(0.5 * imageWidth), out HTuple medianWidthDistance);
                                    //if(medianWidthDistance.D > hvCircleRadius * 0.5)
                                    if (medianWidthDistance.D > hvCircleRadius)
                                    {
                                        return null;
                                    }

                                }


                                HOperatorSet.OpeningCircle(hoFitRegionLocal, out hoRegionOpening, Math.Max(0.0, hvCircleRadius.D * 0.2));
                                HOperatorSet.RegionFeatures(hoRegionOpening, "outer_radius", out hvCircleOuterRadius);

                                radiusPixel = hvCircleRadius.D;
                                if (hvCircleOuterRadius.Length > 0 && hvCircleOuterRadius.D > 0)
                                {
                                    radiusPixel = isConvex ? Math.Max(radiusPixel, hvCircleOuterRadius.D) : hvCircleOuterRadius.D;
                                }
                            }

                            if (!TryGetHeightAndMeasurePoint(hoImageSub, hoMeasurePartRing, hoFitRegionLocal, isConvex,
                                                             rowOffset, colOffset,
                                                             out heightPixel, out measurePointX, out measurePointY))
                            {
                                heightPixel = double.NaN;
                                measurePointX = coreCol;
                                measurePointY = coreRow;
                            }
                        }
                        else
                        {
                            heightPixel = double.NaN;
                            radiusPixel = -1;
                        }
                    }

                    if (!double.IsFinite(heightPixel) || !double.IsFinite(measurePointX) || !double.IsFinite(measurePointY))
                    {
                        return null;
                    }

                    KCJC0_ConvexConcaveResult result = new KCJC0_ConvexConcaveResult
                    {
                        RegionType = isConvex ? "Convex" : "Concave",
                        CorePolygon = new Polygon(hoRegion),
                        FitPolygon = hasFitRegionGlobal && hoFitRegionGlobal != null ? new Polygon(hoFitRegionGlobal) : new Polygon(),
                        HeightDiff = heightPixel * _measureParam.IntervalZ,
                        MeasurePointX = measurePointX,
                        MeasurePointY = measurePointY,
                        Radius = radiusPixel > 0 ? radiusPixel * _measureParam.IntervalX : -1
                    };

                    return result;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    return null;
                }
                finally
                {
                    hoCircle?.Dispose();
                    hoCircleReduced?.Dispose();
                    hoCircleValidRegion?.Dispose();
                    hoCirclePart?.Dispose();
                    hoMeasurePartCircle?.Dispose();
                    hoMeasurePartBuffer?.Dispose();
                    hoMeasurePartRing?.Dispose();
                    hoMeasurePartRingReduced?.Dispose();
                    hoFitSurface?.Dispose();
                    hoImageSub?.Dispose();
                    hoLocalContours?.Dispose();
                    hoCandidateContours?.Dispose();
                    hoBestContour?.Dispose();
                    hoFitRegionLocal?.Dispose();
                    hoFitRegionGlobal?.Dispose();
                    hoTmpIntersection?.Dispose();
                    hoRegionOpening?.Dispose();
                    hoTmp?.Dispose();

                    hvCircleWidth.Dispose();
                    hvCircleHeight.Dispose();
                    hvPointer.Dispose();
                    hvType.Dispose();
                    hvPartWidth.Dispose();
                    hvPartHeight.Dispose();
                    hvCenter2EdgeDistance.Dispose();
                    hvAlpha.Dispose();
                    hvBeta.Dispose();
                    hvGamma.Dispose();
                    hvPeakHeightGray.Dispose();
                    hvCircleRow.Dispose();
                    hvCircleCol.Dispose();
                    hvCircleRadius.Dispose();
                    hvStartPhi.Dispose();
                    hvEndPhi.Dispose();
                    hvPointOrder.Dispose();
                    hvCircleOuterRadius.Dispose();
                    hvTmpArea.Dispose();
                    hvTmpRow.Dispose();
                    hvTmpCol.Dispose();
                }
            }
        }


        /// <summary>
        /// 组装测量结果
        /// </summary>
        private int PourResult(out KCJC0_MeasureResult result)
        {
            result = new KCJC0_MeasureResult();


            // 深度图显示的上下限
            if (_hvDepthMapMinValue.Length > 0)
            {
                _measureResult.DepthMapMinValue = _hvDepthMapMinValue.D;
            }
            else
            {
                if (_hvHeightImageMinValue.Length > 0)
                {
                    _measureResult.DepthMapMinValue = _hvHeightImageMinValue.D;
                }
                else
                {
                    _measureResult.DepthMapMinValue = 0;
                }
            }
            if (_hvDepthMapMaxValue.Length > 0)
            {
                _measureResult.DepthMapMaxValue = _hvDepthMapMaxValue.D;
            }
            else
            {
                if (_hvHeightImageMaxValue.Length > 0)
                {
                    _measureResult.DepthMapMaxValue = _hvHeightImageMaxValue.D;
                }
                else
                {
                    _measureResult.DepthMapMaxValue = 0;
                }
            }

            //图片缩放因子
            if (_hvImageScaleX.Length > 0)
            {
                _measureResult.ImageScaleW = _hvImageScaleX.D * _measureParam.DepthMapSampleDownSizeW;
            }
            if (_hvImageScaleY.Length > 0)
            {
                _measureResult.ImageScaleH = _hvImageScaleY.D * _measureParam.DepthMapSampleDownSizeH;
            }


            result = _measureResult;

            _measureResult = new KCJC0_MeasureResult();

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

            using (var dh = new HDevDisposeHelper())
            {
                HObject? hoTmp = null;

                HObject? hoLightingChartDepthMap = null;

                try
                {
                    Dispose();
                    InitVariable();

                    _measureParam = param.DeepCopy();
                    //图片在X轴与Y轴方向的缩放比例
                    //_hvImageScaleX = 1;
                    //_hvImageScaleY = _measureParam.IntervalY / _measureParam.IntervalX;

                    HTuple hvIntervalX = _measureParam.IntervalX;
                    HTuple hvIntervalY = _measureParam.IntervalY;
                    HTuple hvIntervalZ = _measureParam.IntervalZ;
                    bool fastModel = false;
                    if (fastModel)
                    {
                        if (hvIntervalX > hvIntervalY)
                        {
                            _hvImageScaleX = 1;
                            _hvImageScaleY = hvIntervalY / hvIntervalX;
                        }
                        else
                        {
                            _hvImageScaleX = hvIntervalX / hvIntervalY;
                            _hvImageScaleY = 1;
                        }
                    }
                    else
                    {
                        if (hvIntervalX < hvIntervalY)
                        {
                            _hvImageScaleX = 1;
                            _hvImageScaleY = hvIntervalY / hvIntervalX;
                        }
                        else
                        {
                            _hvImageScaleX = hvIntervalX / hvIntervalY;
                            _hvImageScaleY = 1;
                        }
                    }

                    _hvPlateLeftEdgeMaskSize = _measureParam.PlateLeftEdgeMaskSizeReal / _measureParam.IntervalY;
                    _hvPlateRightEdgeMaskSize = _measureParam.PlateRightEdgeMaskSizeReal / _measureParam.IntervalY;

                    // 将C#数组格式的图片数据转为HObject对象
                    int statusGrayDate, statusHeightData;
                    statusGrayDate = ConvertListToHObject(grayDate, ImageType.Gray, out hoTmp);
                    ReplaceHobject(ref _hoGrayImage, ref hoTmp);
                    statusHeightData = ConvertListToHObject(heightData, ImageType.Depth, out hoTmp);
                    ReplaceHobject(ref _hoHeightImage, ref hoTmp);

                    if (statusGrayDate == 0 && statusHeightData == 0)
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

                        // 根据X、Y方向的像素当量比例缩放图片
                        ScaleGrayHeightImageV2();

                        // 保持lightingChart点云与灰度图方向一致
                        //HOperatorSet.GenEmptyObj(out hoLightingChartDepthMap);
                        //HOperatorSet.MirrorImage(_hoHeightImage, out hoTmp, "column");
                        //ReplaceHobject(ref hoLightingChartDepthMap, ref hoTmp);
                        _measureResult.DepthMap = HobjectToFloatArray(_hoHeightImage.Clone());

                        // 去除深度图异常点
                        ModifyHeightImageOutlierV2();

                        // 判断极片的扫描部位
                        DetectPlateRegion();

                        // 定位图片中极片区域的四个边缘与四个顶点
                        LocationPlatRegionAndKeypoint();

                        // 凸包与凹坑区域定位
                        DetectPlateConvexAndConcave();

                        // 提取凸包轮廓
                        DetectConvexAndConcaveEdge("Convex", ref _hoFitConvexRegions);

                        // 提取凹坑轮廓
                        //DetectConvexAndConcaveEdge("Concave", ref _hoFitConcaveRegions);

                        // 输出测量结果
                        PourResult(out result);
                    }
                    else
                    {
                        // 输出测量结果
                        PourResult(out result);
                    }


                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);

                    // 输出测量结果
                    PourResult(out result);

                }
                finally
                {
                    hoTmp?.Dispose();

                    hoLightingChartDepthMap?.Dispose();
                }
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
                HobjectToMat(_hoGrayImage, out image);
                Cv2.CvtColor(image, image, ColorConversionCodes.GRAY2BGR);

                bool IsDrawablePoint(double x, double y)
                {
                    return double.IsFinite(x) &&
                           double.IsFinite(y) &&
                           x >= 0 && x < image.Width &&
                           y >= 0 && y < image.Height;
                }

                bool IsDrawablePolygon(Polygon? candidate)
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

                bool IsDrawableResult(KCJC0_ConvexConcaveResult? candidate)
                {
                    return candidate != null &&
                           IsDrawablePoint(candidate.MeasurePointX, candidate.MeasurePointY) &&
                           double.IsFinite(candidate.HeightDiff) &&
                           candidate.HeightDiff > 0 &&
                           double.IsFinite(candidate.Radius) &&
                           candidate.Radius > 0 &&
                           IsDrawablePolygon(candidate.FitPolygon);
                }

                void DrawResults(List<KCJC0_ConvexConcaveResult> results, Scalar markerColor, Scalar fitColor)
                {
                    for (int i = 0; i < results.Count; i++)
                    {
                        KCJC0_ConvexConcaveResult result = results[i];
                        if (!IsDrawableResult(result))
                        {
                            continue;
                        }

                        Polygon corePolygon = result.CorePolygon;
                        Polygon polygon = result.FitPolygon;

                        string heightDiff = result.HeightDiff.ToString("F3");
                        string radius = result.Radius.ToString("F3");
                        int centerX = -1;
                        int centerY = -1;
                        if (corePolygon != null)
                        {
                            Cv2.DrawMarker(image, new OpenCvSharp.Point((int)result.MeasurePointX, (int)result.MeasurePointY),
                                           markerColor, MarkerTypes.Cross, markerSize: 30, thickness: 4);

                            centerX = (int)result.MeasurePointX;
                            centerY = (int)result.MeasurePointY;
                        }

                        Cv2.DrawContours(image, polygon.Contours, -1, fitColor, 1);
                        Cv2.Circle(image, new OpenCvSharp.Point((int)polygon.Center.X, (int)polygon.Center.Y),
                                   (int)polygon.Radius, fitColor, 4);

                        if (centerX != -1 && centerY != -1)
                        {
                            string id = "id:" + (i+1).ToString();

                            string textH = "h:" + heightDiff + " um";
                            string textR = "r:" + radius + " um";

                            OpenCvSharp.Point hp = new OpenCvSharp.Point(20 + centerX, 20 + centerY);
                            Cv2.PutText(image, textH, hp, HersheyFonts.HersheySimplex, 1, new Scalar(0, 0, 255), 2);

                            OpenCvSharp.Point rp = new OpenCvSharp.Point(20 + centerX, 50 + centerY);
                            Cv2.PutText(image, textR, rp, HersheyFonts.HersheySimplex, 1, new Scalar(0, 0, 255), 2);

                            OpenCvSharp.Point idp = new OpenCvSharp.Point(20 + centerX, 80 + centerY);
                            Cv2.PutText(image, id, idp, HersheyFonts.HersheySimplex, 1, new Scalar(0, 0, 255), 2);
                        }
                    }
                }

                DrawResults(measureResult.ConvexResultsList, new Scalar(255, 128, 0), new Scalar(0, 255, 0));
                DrawResults(measureResult.ConcaveResultsList, new Scalar(255, 0, 255), new Scalar(0, 255, 255));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }

            return image;
        }




    }
}
