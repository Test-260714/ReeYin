using HalconDotNet;
using MathNet.Numerics.Distributions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using OpenCvSharp;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Services.CustomProject;
using ReeYin_V.Logger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;


namespace Custom.EVEMFDJC.Models
{
    public partial class EVEMFDJC0_Algorithm
    {
        public static class FeatureAlgorithms
        {
            private static void ReplaceHobject(ref HObject target, ref HObject source)
            {
                target.Dispose();
                target = source;
                HOperatorSet.GenEmptyObj(out source);
            }

            private static bool HasTupleValue(HTuple tuple, int minLength = 1)
            {
                return tuple != null && tuple.TupleLength() >= minLength;
            }

            private static bool HasValidSegmentation(Box bbox)
            {
                return bbox != null
                    && bbox.Seg != null
                    && bbox.Seg.Valid == 1
                    && bbox.Seg.Width > 0
                    && bbox.Seg.Height > 0
                    && bbox.Seg.Data != null
                    && bbox.Seg.Data.Length > 0
                    && bbox.Seg.AffineMatrix != null
                    && bbox.Seg.AffineMatrix.Length >= 6;
            }

            private static void FillDefectBaseResult(DefectResult result, Box bbox, HTuple offsetX, HTuple offsetY, HTuple scaleX, HTuple scaleY)
            {
                result.Left = (bbox.Left + offsetX) * scaleX;
                result.Top = (bbox.Top + offsetY) * scaleY;
                result.Right = (bbox.Right + offsetX) * scaleX;
                result.Bottom = (bbox.Bottom + offsetY) * scaleY;
                result.ClassId = bbox.ClassId;
                result.Confidence = bbox.Confidence;
                result.InstanceId = bbox.InstanceId;
            }
            public static int Sigmoid(HObject hoInImage, out HObject hoOutImage)
            {
                HObject hoOnes;

                HOperatorSet.GetImageSize(hoInImage, out HTuple hvCracksWidth, out HTuple hvCracksHeight);
                HOperatorSet.ScaleImage(hoInImage, out hoInImage, -1, 0);
                HOperatorSet.ExpImage(hoInImage, out hoInImage, "e");
                HOperatorSet.ScaleImage(hoInImage, out hoInImage, 1, 1);
                HOperatorSet.GenImageConst(out hoOnes, "real", hvCracksWidth, hvCracksHeight);
                HOperatorSet.ScaleImage(hoOnes, out hoOnes, 1, 1);
                HOperatorSet.DivImage(hoOnes, hoInImage, out hoOutImage, 1, 0);

                hoOnes.Dispose();

                return 0;
            }


            public static WarpResult GetWarpFeatureOld(HObject hoTileGrayImage, HObject hoTileHeightImage, HTuple hvCx, HTuple hvCy,
                                                    HTuple hvOrbitParam, HObject hoValidMask, MFDJC0_MeasureParam measureParam, double height_select = 0)
            {
                WarpResult result = new WarpResult();

                HObject hoDeepX, hoDeepY, hoDeepZ;
                HObject hoDeepXYZ, hoPlaneXYZ;
                HObject hoBaseRegion, hoNailRegion;
                HObject hoNailXYZ;
                HObject hoPeek;

                // 设置分辨率（毫米)
                HTuple scaleX, scaleY;
                if (measureParam.IntervalX > measureParam.IntervalY)
                {
                    scaleX = measureParam.IntervalX / measureParam.IntervalY;
                    scaleY = 1;
                }
                else
                {
                    scaleX = 1;
                    scaleY = measureParam.IntervalY / measureParam.IntervalX;
                }
                HTuple hvXp = new HTuple(((measureParam.IntervalX * 1.0) / scaleX) / 1000);
                HTuple hvYp = new HTuple(((measureParam.IntervalY * 1.0) / scaleY) / 1000);
                HTuple hvZp = new HTuple((measureParam.IntervalZ * 1.0) / 1000);

                //HOperatorSet.ZoomRegion(hoValidMask, out hoValidMask, scaleX, scaleY);

                HOperatorSet.GetImageSize(hoTileHeightImage, out HTuple hvImageW, out HTuple hvImageH);
                // 生成三通道TIFF
                HTuple hvRowD, hvColD;
                HOperatorSet.GetRegionPoints(hoTileHeightImage, out hvRowD, out hvColD);
                HTuple hvValueX = hvColD * hvXp;
                HTuple hvValueY = hvRowD * hvYp;
                HOperatorSet.GenImageConst(out hoDeepX, "real", hvImageW, hvImageH);
                HOperatorSet.GenImageConst(out hoDeepY, "real", hvImageW, hvImageH);
                HOperatorSet.SetGrayval(hoDeepX, hvRowD, hvColD, hvValueX);
                HOperatorSet.SetGrayval(hoDeepY, hvRowD, hvColD, hvValueY);
                HOperatorSet.ConvertImageType(hoTileHeightImage, out hoDeepZ, "real");
                HOperatorSet.ScaleImage(hoDeepZ, out hoDeepZ, hvZp, 0);
                HOperatorSet.Compose3(hoDeepX, hoDeepY, hoDeepZ, out hoDeepXYZ);

                HTuple hvCirRow = hvOrbitParam[0];
                HTuple hvCirCol = hvOrbitParam[1];
                HTuple hvCirRad = hvOrbitParam[2];
                // 定位密封钉区域
                HOperatorSet.GenCircle(out hoNailRegion, hvCirRow, hvCirCol, hvCirRad * 0.656);

                // 定位基准面
                HOperatorSet.GenCircle(out hoBaseRegion, hvCirRow, hvCirCol, hvCirRad * 1.55);
                HOperatorSet.Difference(hoValidMask, hoBaseRegion, out hoBaseRegion);

                HOperatorSet.AreaCenter(hoBaseRegion, out HTuple hvTmpArea, out HTuple hvTmpRow, out HTuple hvTmpCol);
                if (hvTmpArea.D == 0)
                {
                    Console.WriteLine("ERROR:failed to extract the region of base plane");

                    result.IsOk = false;

                    return result;
                }

                HTuple hvPland3d, hvSampP3d;
                HTuple hvPose, hvNormal;
                HTuple hvNail3d, hvNailRpT;
                HTuple hvMatRpT;
                HTuple hvZValue;
                HOperatorSet.ReduceDomain(hoDeepXYZ, hoBaseRegion, out hoPlaneXYZ);
                HOperatorSet.Decompose3(hoPlaneXYZ, out hoDeepX, out hoDeepY, out hoDeepZ);
                HOperatorSet.XyzToObjectModel3d(hoDeepX, hoDeepY, hoDeepZ, out hvPland3d);
                HOperatorSet.SampleObjectModel3d(hvPland3d, "fast", 0.05, new HTuple(), new HTuple(), out hvSampP3d);
                HOperatorSet.FitPrimitivesObjectModel3d(hvSampP3d, (new HTuple("primitive_type")).TupleConcat("fitting_algorithm"),
                                                                   (new HTuple("plane")).TupleConcat("least_squares"), out hvPland3d);
                HOperatorSet.GetObjectModel3dParams(hvPland3d, "primitive_parameter_pose", out hvPose);
                HOperatorSet.PoseInvert(hvPose, out hvPose);
                HOperatorSet.GetObjectModel3dParams(hvPland3d, "primitive_parameter", out hvNormal);
                if ((int)(new HTuple(((hvNormal.TupleSelect(2))).TupleLess(0))) != 0)
                {
                    HTuple hvFlip;
                    HOperatorSet.CreatePose(0, 0, 0, 180, 0, 0, "Rp+T", "gba", "point", out hvFlip);
                    HOperatorSet.PoseCompose(hvFlip, hvPose, out hvPose);
                }

                // 翘钉高度测量
                HOperatorSet.Intersection(hoValidMask, hoNailRegion, out hoNailRegion);
                HOperatorSet.AreaCenter(hoNailRegion, out hvTmpArea, out hvTmpRow, out hvTmpCol);
                if (hvTmpArea.D == 0)
                {
                    Console.WriteLine("ERROR:failed to extract the region of anchor!");

                    result.IsOk = false;

                    return result;
                }
                HOperatorSet.ReduceDomain(hoDeepXYZ, hoNailRegion, out hoNailXYZ);
                HOperatorSet.Decompose3(hoNailXYZ, out hoDeepX, out hoDeepY, out hoDeepZ);
                HOperatorSet.XyzToObjectModel3d(hoDeepX, hoDeepY, hoDeepZ, out hvNail3d);

                HOperatorSet.PoseToHomMat3d(hvPose, out hvMatRpT);
                HOperatorSet.AffineTransObjectModel3d(hvNail3d, hvMatRpT, out hvNailRpT);

                HOperatorSet.GetObjectModel3dParams(hvNailRpT, "point_coord_z", out hvZValue);

                HTuple hvHeight = hvZValue.TupleMax();
                HOperatorSet.TupleFind(hvZValue, hvHeight, out HTuple hvIndex);
                HOperatorSet.GetObjectModel3dParams(hvNail3d, "point_coord_z", out HTuple hvRValue);
                HTuple hvDeepVal = hvRValue.TupleSelect(hvIndex);

                HOperatorSet.Threshold(hoDeepZ, out hoPeek, hvDeepVal - 0.0001, hvDeepVal + 0.0001);
                HOperatorSet.GetRegionPoints(hoPeek, out HTuple hvRows, out HTuple hvColumns);

                HTuple hvRow = hvRows.TupleSelect(0);
                HTuple hvColumn = hvColumns.TupleSelect(0);

                // 结果输出, 单位mm
                if (hvHeight.D < height_select)
                    result.IsOk = true;
                else
                    result.IsOk = false;
                result.Height = hvHeight.D;
                result.HighestPointRow = hvRow.D;
                result.HighestPointCol = hvColumn.D;


                hoDeepX.Dispose(); hoDeepY.Dispose(); hoDeepZ.Dispose();
                hoDeepXYZ.Dispose(); hoPlaneXYZ.Dispose();
                hoBaseRegion.Dispose(); hoNailRegion.Dispose();
                hoNailXYZ.Dispose();
                hoPeek.Dispose();


                return result;
            }


            public static WarpResult GetWarpFeatureOld2(HObject hoTileGrayImage, HObject hoTileHeightImage, HTuple hvCx, HTuple hvCy,
                                                    HTuple hvOrbitParam, HObject hoValidMask, HObject hoNailWarpBaseMask,
                                                    MFDJC0_MeasureParam measureParam, double height_select = 0)
            {
                using (var dh = new HDevDisposeHelper())
                {
                    WarpResult result = new WarpResult();
                    List<Polygon> polygons = result.Polygons;

                    HObject hoTileGrayImageZoom = new HObject();
                    HObject hoTileHeightImageZoom = new HObject();
                    HObject hoNailWarpBaseMaskZoom = new HObject();
                    HObject hoValidMaskThreshold = new HObject();
                    HObject hoIrregularRegion = new HObject();
                    HObject hoIrregularRegion0 = new HObject();
                    HObject hoIrregularRegion1 = new HObject();
                    HObject hoIrregularRegion2 = new HObject();
                    HObject hoIrregularMask = new HObject();
                    HObject hoValidMaskZoom = new HObject();
                    HObject hoNailWarpBaseMaskValid = new HObject();
                    HObject hoDeepX = new HObject();
                    HObject hoDeepY = new HObject();
                    HObject hoDeepZ = new HObject();
                    HObject hoDeepXYZ = new HObject();
                    HObject hoPlaneXYZ = new HObject();
                    HObject hoPlaneX = new HObject();
                    HObject hoPlaneY = new HObject();
                    HObject hoPlaneZ = new HObject();
                    HObject hoNailRegion = new HObject();
                    HObject hoNailRegionValid = new HObject();
                    HObject hoNailXYZ = new HObject();
                    HObject hoNailX = new HObject();
                    HObject hoNailY = new HObject();
                    HObject hoNailZ = new HObject();
                    HObject hoPeek = new HObject();
                    HObject hoHeightImageAffineTrans = new HObject();
                    HObject hoMaxHeightRegion = new HObject();
                    HObject hoWarpRegion = new HObject();
                    HObject hoWarpRegions = new HObject();
                    HObject hoTmp = new HObject();

                    HOperatorSet.GenEmptyObj(out hoTileGrayImageZoom);
                    HOperatorSet.GenEmptyObj(out hoTileHeightImageZoom);
                    HOperatorSet.GenEmptyObj(out hoNailWarpBaseMaskZoom);
                    HOperatorSet.GenEmptyObj(out hoValidMaskThreshold);
                    HOperatorSet.GenEmptyObj(out hoIrregularRegion);
                    HOperatorSet.GenEmptyObj(out hoIrregularRegion0);
                    HOperatorSet.GenEmptyObj(out hoIrregularRegion1);
                    HOperatorSet.GenEmptyObj(out hoIrregularRegion2);
                    HOperatorSet.GenEmptyObj(out hoIrregularMask);
                    HOperatorSet.GenEmptyObj(out hoValidMaskZoom);
                    HOperatorSet.GenEmptyObj(out hoNailWarpBaseMaskValid);
                    HOperatorSet.GenEmptyObj(out hoDeepX);
                    HOperatorSet.GenEmptyObj(out hoDeepY);
                    HOperatorSet.GenEmptyObj(out hoDeepZ);
                    HOperatorSet.GenEmptyObj(out hoDeepXYZ);
                    HOperatorSet.GenEmptyObj(out hoPlaneXYZ);
                    HOperatorSet.GenEmptyObj(out hoPlaneX);
                    HOperatorSet.GenEmptyObj(out hoPlaneY);
                    HOperatorSet.GenEmptyObj(out hoPlaneZ);
                    HOperatorSet.GenEmptyObj(out hoNailRegion);
                    HOperatorSet.GenEmptyObj(out hoNailRegionValid);
                    HOperatorSet.GenEmptyObj(out hoNailXYZ);
                    HOperatorSet.GenEmptyObj(out hoNailX);
                    HOperatorSet.GenEmptyObj(out hoNailY);
                    HOperatorSet.GenEmptyObj(out hoNailZ);
                    HOperatorSet.GenEmptyObj(out hoPeek);
                    HOperatorSet.GenEmptyObj(out hoHeightImageAffineTrans);
                    HOperatorSet.GenEmptyObj(out hoMaxHeightRegion);
                    HOperatorSet.GenEmptyObj(out hoWarpRegion);
                    HOperatorSet.GenEmptyObj(out hoWarpRegions);
                    HOperatorSet.GenEmptyObj(out hoTmp);

                    HTuple hvPland3d = new HTuple();
                    HTuple hvSampP3d = new HTuple();
                    HTuple hvPose = new HTuple();
                    HTuple hvNormal = new HTuple();
                    HTuple hvNail3d = new HTuple();
                    HTuple hvNailRpT = new HTuple();
                    HTuple hvMatRpT = new HTuple();
                    HTuple hvRowD = new HTuple();
                    HTuple hvColD = new HTuple();
                    HTuple hvRows = new HTuple();
                    HTuple hvCols = new HTuple();
                    HTuple hvPointXNoGlueT = new HTuple();
                    HTuple hvPointYNoGlueT = new HTuple();
                    HTuple hvPointZNoGlueT = new HTuple();
                    HTuple hvPointZNoGlueValidT = new HTuple();
                    HTuple hvMaxHeightRows = new HTuple();
                    HTuple hvMaxHeightColumns = new HTuple();

                    try
                    {
                        if (hvOrbitParam.TupleLength() < 3)
                        {
                            Console.WriteLine("ERROR:invalid hvOrbitParam in GetWarpFeature()");
                            result.IsOk = false;
                            return result;
                        }

                        HTuple hvAccelerationFactor = 4.0f;
                        HTuple hvScaleFactorW = 1.0f / hvAccelerationFactor;
                        HTuple hvScaleFactorH = 1.0f / hvAccelerationFactor;
                        HOperatorSet.ZoomImageFactor(hoTileGrayImage, out hoTileGrayImageZoom, hvScaleFactorW, hvScaleFactorH, "bilinear");
                        HOperatorSet.ZoomImageFactor(hoTileHeightImage, out hoTileHeightImageZoom, hvScaleFactorW, hvScaleFactorH, "nearest_neighbor");
                        HOperatorSet.ZoomRegion(hoNailWarpBaseMask, out hoNailWarpBaseMaskZoom, hvScaleFactorW, hvScaleFactorH);

                        HOperatorSet.Threshold(hoTileHeightImageZoom, out hoValidMaskThreshold, measureParam.MinDepth, measureParam.MaxDepth);

                        HOperatorSet.Threshold(hoTileHeightImageZoom, out hoIrregularRegion0, 8888880, 8888880);
                        HOperatorSet.ConcatObj(hoIrregularRegion, hoIrregularRegion0, out hoTmp);
                        ReplaceHobject(ref hoIrregularRegion, ref hoTmp);
                        HOperatorSet.Threshold(hoTileHeightImageZoom, out hoIrregularRegion1, -2147483648, -2147483648);
                        HOperatorSet.ConcatObj(hoIrregularRegion, hoIrregularRegion1, out hoTmp);
                        ReplaceHobject(ref hoIrregularRegion, ref hoTmp);
                        HOperatorSet.Threshold(hoTileHeightImageZoom, out hoIrregularRegion2, 0, 0);
                        HOperatorSet.ConcatObj(hoIrregularRegion, hoIrregularRegion2, out hoTmp);
                        ReplaceHobject(ref hoIrregularRegion, ref hoTmp);
                        HOperatorSet.Union1(hoIrregularRegion, out hoIrregularMask);
                        HOperatorSet.Difference(hoValidMaskThreshold, hoIrregularMask, out hoValidMaskZoom);
                        HOperatorSet.Intersection(hoNailWarpBaseMaskZoom, hoValidMaskZoom, out hoNailWarpBaseMaskValid);

                        HTuple scaleX, scaleY;
                        if (measureParam.IntervalX > measureParam.IntervalY)
                        {
                            scaleX = measureParam.IntervalX / measureParam.IntervalY;
                            scaleY = 1;
                        }
                        else
                        {
                            scaleX = 1;
                            scaleY = measureParam.IntervalY / measureParam.IntervalX;
                        }
                        HTuple hvXp = new HTuple(((measureParam.IntervalX * hvAccelerationFactor * 1.0) / scaleX) / 1000);
                        HTuple hvYp = new HTuple(((measureParam.IntervalY * hvAccelerationFactor * 1.0) / scaleY) / 1000);
                        HTuple hvZp = new HTuple((measureParam.IntervalZ * 1.0) / 1000);

                        HOperatorSet.GetImageSize(hoTileHeightImageZoom, out HTuple hvImageW, out HTuple hvImageH);
                        HOperatorSet.GetRegionPoints(hoTileHeightImageZoom, out hvRowD, out hvColD);
                        HTuple hvValueX = hvColD * hvXp;
                        HTuple hvValueY = hvRowD * hvYp;
                        HOperatorSet.GenImageConst(out hoDeepX, "real", hvImageW, hvImageH);
                        HOperatorSet.GenImageConst(out hoDeepY, "real", hvImageW, hvImageH);
                        HOperatorSet.SetGrayval(hoDeepX, hvRowD, hvColD, hvValueX);
                        HOperatorSet.SetGrayval(hoDeepY, hvRowD, hvColD, hvValueY);
                        HOperatorSet.ConvertImageType(hoTileHeightImageZoom, out hoDeepZ, "real");
                        HOperatorSet.ScaleImage(hoDeepZ, out hoTmp, hvZp, 0);
                        ReplaceHobject(ref hoDeepZ, ref hoTmp);
                        HOperatorSet.Compose3(hoDeepX, hoDeepY, hoDeepZ, out hoDeepXYZ);

                        HTuple hvCirRow = hvOrbitParam[0] * hvScaleFactorW;
                        HTuple hvCirCol = hvOrbitParam[1] * hvScaleFactorH;
                        HTuple hvCirRad = hvOrbitParam[2] / hvAccelerationFactor;
                        HOperatorSet.GenCircle(out hoNailRegion, hvCirRow, hvCirCol, hvCirRad);

                        HOperatorSet.AreaCenter(hoNailWarpBaseMaskValid, out HTuple hvTmpArea, out HTuple hvTmpRow, out HTuple hvTmpCol);
                        if (hvTmpArea.D == 0)
                        {
                            Console.WriteLine("ERROR:failed to extract the region of base plane");
                            result.IsOk = false;
                            return result;
                        }

                        HOperatorSet.ReduceDomain(hoDeepXYZ, hoNailWarpBaseMaskValid, out hoPlaneXYZ);
                        HOperatorSet.Decompose3(hoPlaneXYZ, out hoPlaneX, out hoPlaneY, out hoPlaneZ);
                        HOperatorSet.XyzToObjectModel3d(hoPlaneX, hoPlaneY, hoPlaneZ, out hvPland3d);
                        HOperatorSet.SampleObjectModel3d(hvPland3d, "fast", 0.05, new HTuple(), new HTuple(), out hvSampP3d);
                        HOperatorSet.FitPrimitivesObjectModel3d(hvSampP3d, (new HTuple("primitive_type")).TupleConcat("fitting_algorithm"),
                                                                           (new HTuple("plane")).TupleConcat("least_squares"), out hvPland3d);
                        HOperatorSet.GetObjectModel3dParams(hvPland3d, "primitive_parameter_pose", out hvPose);
                        HOperatorSet.PoseInvert(hvPose, out hvPose);
                        HOperatorSet.GetObjectModel3dParams(hvPland3d, "primitive_parameter", out hvNormal);
                        if (hvNormal.TupleLength() >= 3 && hvNormal.TupleSelect(2).D < 0)
                        {
                            HTuple hvFlip;
                            HOperatorSet.CreatePose(0, 0, 0, 180, 0, 0, "Rp+T", "gba", "point", out hvFlip);
                            HOperatorSet.PoseCompose(hvFlip, hvPose, out hvPose);
                        }

                        HOperatorSet.Intersection(hoValidMaskZoom, hoNailRegion, out hoNailRegionValid);
                        HOperatorSet.AreaCenter(hoNailRegionValid, out hvTmpArea, out hvTmpRow, out hvTmpCol);
                        if (hvTmpArea.D == 0)
                        {
                            Console.WriteLine("ERROR:failed to extract the region of anchor!");
                            result.IsOk = false;
                            return result;
                        }

                        HOperatorSet.ReduceDomain(hoDeepXYZ, hoNailRegionValid, out hoNailXYZ);
                        HOperatorSet.Decompose3(hoNailXYZ, out hoNailX, out hoNailY, out hoNailZ);
                        HOperatorSet.XyzToObjectModel3d(hoNailX, hoNailY, hoNailZ, out hvNail3d);

                        HOperatorSet.PoseToHomMat3d(hvPose, out hvMatRpT);
                        HOperatorSet.AffineTransObjectModel3d(hvNail3d, hvMatRpT, out hvNailRpT);

                        HOperatorSet.RegionFeatures(hoNailRegionValid, "row1", out HTuple hvUpperLeftValueRow);
                        HOperatorSet.RegionFeatures(hoNailRegionValid, "column1", out HTuple hvUpperLeftValueCol);
                        HOperatorSet.GetObjectModel3dParams(hvNailRpT, "point_coord_x", out hvPointXNoGlueT);
                        HOperatorSet.GetObjectModel3dParams(hvNailRpT, "point_coord_y", out hvPointYNoGlueT);
                        HOperatorSet.GetObjectModel3dParams(hvNailRpT, "point_coord_z", out hvPointZNoGlueT);

                        hvCols = ((hvPointXNoGlueT / hvXp)).TupleInt();
                        hvRows = ((hvPointYNoGlueT / hvYp)).TupleInt();

                        HOperatorSet.TupleMin(hvRows, out HTuple hvRowsMin);
                        HOperatorSet.TupleMin(hvCols, out HTuple hvColsMin);

                        HTuple hvOffsetRow = hvUpperLeftValueRow - hvRowsMin;
                        HTuple hvOffsetCol = hvUpperLeftValueCol - hvColsMin;

                        hvRows = (hvRows + hvOffsetRow).TupleInt();
                        hvCols = (hvCols + hvOffsetCol).TupleInt();

                        int hvPointCount = Math.Min(hvRows.TupleLength(), Math.Min(hvCols.TupleLength(), hvPointZNoGlueT.TupleLength()));
                        int hvImageWInt = hvImageW.I;
                        int hvImageHInt = hvImageH.I;
                        List<int> hvValidRows = new List<int>(hvPointCount);
                        List<int> hvValidCols = new List<int>(hvPointCount);
                        List<double> hvValidPointZ = new List<double>(hvPointCount);
                        for (int i = 0; i < hvPointCount; i++)
                        {
                            int row = hvRows[i].I;
                            int col = hvCols[i].I;
                            double pointZ = hvPointZNoGlueT[i].D;
                            if (double.IsNaN(pointZ) || double.IsInfinity(pointZ))
                            {
                                continue;
                            }
                            if (row < 0 || row >= hvImageHInt || col < 0 || col >= hvImageWInt)
                            {
                                continue;
                            }

                            hvValidRows.Add(row);
                            hvValidCols.Add(col);
                            hvValidPointZ.Add(pointZ);
                        }
                        if (hvValidRows.Count <= 0)
                        {
                            Console.WriteLine("ERROR:no valid transformed points for set_grayval");
                            result.IsOk = false;
                            return result;
                        }
                        hvRows.Dispose();
                        hvCols.Dispose();
                        hvRows = new HTuple(hvValidRows.ToArray());
                        hvCols = new HTuple(hvValidCols.ToArray());
                        hvPointZNoGlueValidT = new HTuple(hvValidPointZ.ToArray());

                        HTuple hvWarpMinHeightValue = hvPointZNoGlueValidT.TupleMin();
                        HTuple hvGlobalMaxWarpHeightValue = hvPointZNoGlueValidT.TupleMax();
                        result.IsOk = hvGlobalMaxWarpHeightValue.D < height_select;
                        result.Height = hvGlobalMaxWarpHeightValue.D;

                        HOperatorSet.GenImageConst(out hoHeightImageAffineTrans, "real", hvImageW, hvImageH);
                        HOperatorSet.ScaleImage(hoHeightImageAffineTrans, out hoTmp, 0, hvWarpMinHeightValue);
                        ReplaceHobject(ref hoHeightImageAffineTrans, ref hoTmp);
                        HOperatorSet.SetGrayval(hoHeightImageAffineTrans, hvRows, hvCols, hvPointZNoGlueValidT);

                        HOperatorSet.Threshold(hoHeightImageAffineTrans, out hoMaxHeightRegion, hvGlobalMaxWarpHeightValue - 0.0001, hvGlobalMaxWarpHeightValue + 0.0001);
                        HOperatorSet.AreaCenter(hoMaxHeightRegion, out HTuple hvArea, out HTuple hvMaxHeightRow, out HTuple hvMaxHeightColumn);
                        if (hvArea.D <= 0)
                        {
                            HOperatorSet.Threshold(hoHeightImageAffineTrans, out hoTmp, hvGlobalMaxWarpHeightValue - 0.01, hvGlobalMaxWarpHeightValue + 0.01);
                            ReplaceHobject(ref hoMaxHeightRegion, ref hoTmp);
                            HOperatorSet.AreaCenter(hoMaxHeightRegion, out hvArea, out hvMaxHeightRow, out hvMaxHeightColumn);
                        }
                        if (hvArea.D > 0)
                        {
                            HOperatorSet.GetRegionPoints(hoMaxHeightRegion, out hvMaxHeightRows, out hvMaxHeightColumns);
                            if (hvMaxHeightRows.TupleLength() > 0 && hvMaxHeightColumns.TupleLength() > 0)
                            {
                                result.HighestPointRow = hvMaxHeightRows[(int)Math.Floor(hvMaxHeightRows.TupleLength() * 0.5)].D * hvAccelerationFactor.D;
                                result.HighestPointCol = hvMaxHeightColumns[(int)Math.Floor(hvMaxHeightColumns.TupleLength() * 0.5)].D * hvAccelerationFactor.D;
                            }
                            else
                            {
                                result.HighestPointRow = hvMaxHeightRow.D * hvAccelerationFactor.D;
                                result.HighestPointCol = hvMaxHeightColumn.D * hvAccelerationFactor.D;
                            }
                        }
                        else
                        {
                            Console.WriteLine("WARN:failed to extract max-height region of warp feature.");
                        }

                        if (height_select < hvGlobalMaxWarpHeightValue.D)
                        {
                            HOperatorSet.Threshold(hoHeightImageAffineTrans, out hoWarpRegion, height_select, hvGlobalMaxWarpHeightValue + 0.01);
                            HOperatorSet.ClosingCircle(hoWarpRegion, out hoTmp, 5.5);
                            ReplaceHobject(ref hoWarpRegion, ref hoTmp);
                            HOperatorSet.Connection(hoWarpRegion, out hoWarpRegions);
                            HOperatorSet.SelectShape(hoWarpRegions, out hoTmp, "area", "and", 0, 9999999999999999999);
                            ReplaceHobject(ref hoWarpRegions, ref hoTmp);
                            HOperatorSet.CountObj(hoWarpRegions, out HTuple hvRegionsNum);

                            if (hvRegionsNum > 0)
                            {
                                HTuple hvLocalMaxWarpHeightValue = 0;
                                for (int i = 0; i < hvRegionsNum; i++)
                                {
                                    HObject hoWarpRegionSelected = new HObject();
                                    HObject hoWarpRegionHeightImage = new HObject();
                                    HObject hoWarpMaxHeightRegion = new HObject();
                                    HObject hoWarpRegionZoom = new HObject();
                                    HOperatorSet.GenEmptyObj(out hoWarpRegionSelected);
                                    HOperatorSet.GenEmptyObj(out hoWarpRegionHeightImage);
                                    HOperatorSet.GenEmptyObj(out hoWarpMaxHeightRegion);
                                    HOperatorSet.GenEmptyObj(out hoWarpRegionZoom);

                                    try
                                    {
                                        HOperatorSet.SelectObj(hoWarpRegions, out hoWarpRegionSelected, i + 1);
                                        HOperatorSet.AreaCenter(hoWarpRegionSelected, out hvArea, out HTuple hvTmpValue0, out HTuple hvTmpValue1);
                                        if (hvArea > 0)
                                        {
                                            HOperatorSet.ReduceDomain(hoHeightImageAffineTrans, hoWarpRegionSelected, out hoWarpRegionHeightImage);
                                            HOperatorSet.GrayFeatures(hoWarpRegionSelected, hoWarpRegionHeightImage, "max", out HTuple hvTmpValue);
                                            HOperatorSet.Threshold(hoWarpRegionHeightImage, out hoWarpMaxHeightRegion, hvTmpValue - 0.01, hvTmpValue + 0.01);
                                            HOperatorSet.AreaCenter(hoWarpMaxHeightRegion, out hvArea, out HTuple hvTmpMaxHeightRow, out HTuple hvTmpMaxHeightColumn);
                                            if (hvArea > 0 && hvLocalMaxWarpHeightValue < hvTmpValue)
                                            {
                                                hvLocalMaxWarpHeightValue = hvTmpValue;
                                                result.HighestPointRow = hvTmpMaxHeightRow.D * hvAccelerationFactor.D;
                                                result.HighestPointCol = hvTmpMaxHeightColumn.D * hvAccelerationFactor.D;
                                            }

                                            HOperatorSet.ZoomRegion(hoWarpRegionSelected, out hoWarpRegionZoom, hvAccelerationFactor, hvAccelerationFactor);
                                            polygons.Add(new Polygon(hoWarpRegionZoom));
                                        }
                                    }
                                    finally
                                    {
                                        hoWarpRegionSelected.Dispose();
                                        hoWarpRegionHeightImage.Dispose();
                                        hoWarpMaxHeightRegion.Dispose();
                                        hoWarpRegionZoom.Dispose();
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logs.LogError($"{DateTime.Now}:GetWarpFeature()报错信息:{ex.Message},调用堆栈:{ex.StackTrace}");
                        Console.WriteLine(ex.StackTrace);
                        result.IsOk = false;
                    }
                    finally
                    {
                        hvPland3d.Dispose();
                        hvSampP3d.Dispose();
                        hvPose.Dispose();
                        hvNormal.Dispose();
                        hvNail3d.Dispose();
                        hvNailRpT.Dispose();
                        hvMatRpT.Dispose();
                        hvRowD.Dispose();
                        hvColD.Dispose();
                        hvRows.Dispose();
                        hvCols.Dispose();
                        hvPointXNoGlueT.Dispose();
                        hvPointYNoGlueT.Dispose();
                        hvPointZNoGlueT.Dispose();
                        hvPointZNoGlueValidT.Dispose();
                        hvMaxHeightRows.Dispose();
                        hvMaxHeightColumns.Dispose();

                        hoTileGrayImageZoom.Dispose();
                        hoTileHeightImageZoom.Dispose();
                        hoNailWarpBaseMaskZoom.Dispose();
                        hoValidMaskThreshold.Dispose();
                        hoIrregularRegion.Dispose();
                        hoIrregularRegion0.Dispose();
                        hoIrregularRegion1.Dispose();
                        hoIrregularRegion2.Dispose();
                        hoIrregularMask.Dispose();
                        hoValidMaskZoom.Dispose();
                        hoNailWarpBaseMaskValid.Dispose();
                        hoDeepX.Dispose();
                        hoDeepY.Dispose();
                        hoDeepZ.Dispose();
                        hoDeepXYZ.Dispose();
                        hoPlaneXYZ.Dispose();
                        hoPlaneX.Dispose();
                        hoPlaneY.Dispose();
                        hoPlaneZ.Dispose();
                        hoNailRegion.Dispose();
                        hoNailRegionValid.Dispose();
                        hoNailXYZ.Dispose();
                        hoNailX.Dispose();
                        hoNailY.Dispose();
                        hoNailZ.Dispose();
                        hoPeek.Dispose();
                        hoHeightImageAffineTrans.Dispose();
                        hoMaxHeightRegion.Dispose();
                        hoWarpRegion.Dispose();
                        hoWarpRegions.Dispose();
                        hoTmp.Dispose();
                    }

                    return result;
                }
            }


            public static WarpResult GetWarpFeature(HObject hoTileGrayImage, HObject hoTileHeightImage, HTuple hvCx, HTuple hvCy,
                                                    HTuple hvOrbitParam, HObject hoValidMask, HObject hoNailWarpBaseMask,
                                                    MFDJC0_MeasureParam measureParam, double height_select = 0)
            {
                using (var dh = new HDevDisposeHelper())
                {
                    WarpResult result = new WarpResult();
                    List<Polygon> polygons = result.Polygons;

                    HObject hoTileGrayImageZoom = new HObject();
                    HObject hoTileHeightImageZoom = new HObject();
                    HObject hoNailWarpBaseMaskZoom = new HObject();
                    HObject hoValidMaskThreshold = new HObject();
                    HObject hoIrregularRegion = new HObject();
                    HObject hoIrregularRegion0 = new HObject();
                    HObject hoIrregularRegion1 = new HObject();
                    HObject hoIrregularRegion2 = new HObject();
                    HObject hoIrregularMask = new HObject();
                    HObject hoValidMaskZoom = new HObject();
                    HObject hoNailWarpBaseMaskValid = new HObject();
                    HObject hoNailRegion = new HObject();
                    HObject hoNailRegionValid = new HObject();
                    HObject hoImageSurface = new HObject();
                    HObject hoImageSub = new HObject();

                    HObject hoMaxHeightRegion = new HObject();
                    HObject hoMaxHeightRegionZoom = new HObject();
                    HObject hoWarpRegion = new HObject();
                    HObject hoWarpRegions = new HObject();
                    HObject hoTmp = new HObject();

                    HOperatorSet.GenEmptyObj(out hoTileGrayImageZoom);
                    HOperatorSet.GenEmptyObj(out hoTileHeightImageZoom);
                    HOperatorSet.GenEmptyObj(out hoNailWarpBaseMaskZoom);
                    HOperatorSet.GenEmptyObj(out hoValidMaskThreshold);
                    HOperatorSet.GenEmptyObj(out hoIrregularRegion);
                    HOperatorSet.GenEmptyObj(out hoIrregularRegion0);
                    HOperatorSet.GenEmptyObj(out hoIrregularRegion1);
                    HOperatorSet.GenEmptyObj(out hoIrregularRegion2);
                    HOperatorSet.GenEmptyObj(out hoIrregularMask);
                    HOperatorSet.GenEmptyObj(out hoValidMaskZoom);
                    HOperatorSet.GenEmptyObj(out hoNailWarpBaseMaskValid);
                    HOperatorSet.GenEmptyObj(out hoNailRegion);
                    HOperatorSet.GenEmptyObj(out hoNailRegionValid);
                    HOperatorSet.GenEmptyObj(out hoImageSurface);
                    HOperatorSet.GenEmptyObj(out hoImageSub);

                    HOperatorSet.GenEmptyObj(out hoMaxHeightRegion);
                    HOperatorSet.GenEmptyObj(out hoMaxHeightRegionZoom);
                    HOperatorSet.GenEmptyObj(out hoWarpRegion);
                    HOperatorSet.GenEmptyObj(out hoWarpRegions);
                    HOperatorSet.GenEmptyObj(out hoTmp);

                    HTuple hvGlobalMaxHeightArea = new HTuple();
                    HTuple hvLocalMaxHeightArea = new HTuple();

                    HTuple hvMaxHeightRows = new HTuple();
                    HTuple hvMaxHeightColumns = new HTuple();

                    try
                    {
                        if (hvOrbitParam.TupleLength() < 3)
                        {
                            Console.WriteLine("ERROR:invalid hvOrbitParam in GetWarpFeature()");
                            result.IsOk = false;
                            return result;
                        }

                        HTuple hvAccelerationFactor = 4.0f;
                        HTuple hvScaleFactorW = 1.0f / hvAccelerationFactor;
                        HTuple hvScaleFactorH = 1.0f / hvAccelerationFactor;
                        HOperatorSet.ZoomImageFactor(hoTileGrayImage, out hoTileGrayImageZoom, hvScaleFactorW, hvScaleFactorH, "bilinear");
                        HOperatorSet.ZoomImageFactor(hoTileHeightImage, out hoTileHeightImageZoom, hvScaleFactorW, hvScaleFactorH, "nearest_neighbor");
                        HOperatorSet.ZoomRegion(hoNailWarpBaseMask, out hoNailWarpBaseMaskZoom, hvScaleFactorW, hvScaleFactorH);

                        HOperatorSet.Threshold(hoTileHeightImageZoom, out hoValidMaskThreshold, measureParam.MinDepth, measureParam.MaxDepth);

                        HOperatorSet.Threshold(hoTileHeightImageZoom, out hoIrregularRegion0, 8888880, 8888880);
                        HOperatorSet.ConcatObj(hoIrregularRegion, hoIrregularRegion0, out hoTmp);
                        ReplaceHobject(ref hoIrregularRegion, ref hoTmp);
                        HOperatorSet.Threshold(hoTileHeightImageZoom, out hoIrregularRegion1, -2147483648, -2147483648);
                        HOperatorSet.ConcatObj(hoIrregularRegion, hoIrregularRegion1, out hoTmp);
                        ReplaceHobject(ref hoIrregularRegion, ref hoTmp);
                        HOperatorSet.Threshold(hoTileHeightImageZoom, out hoIrregularRegion2, 0, 0);
                        HOperatorSet.ConcatObj(hoIrregularRegion, hoIrregularRegion2, out hoTmp);
                        ReplaceHobject(ref hoIrregularRegion, ref hoTmp);
                        HOperatorSet.Union1(hoIrregularRegion, out hoIrregularMask);
                        HOperatorSet.Difference(hoValidMaskThreshold, hoIrregularMask, out hoValidMaskZoom);
                        HOperatorSet.Intersection(hoNailWarpBaseMaskZoom, hoValidMaskZoom, out hoNailWarpBaseMaskValid);

                        HTuple scaleX, scaleY;
                        if (measureParam.IntervalX > measureParam.IntervalY)
                        {
                            scaleX = measureParam.IntervalX / measureParam.IntervalY;
                            scaleY = 1;
                        }
                        else
                        {
                            scaleX = 1;
                            scaleY = measureParam.IntervalY / measureParam.IntervalX;
                        }
                        HTuple hvXp = new HTuple(((measureParam.IntervalX * hvAccelerationFactor * 1.0) / scaleX) / 1000);
                        HTuple hvYp = new HTuple(((measureParam.IntervalY * hvAccelerationFactor * 1.0) / scaleY) / 1000);
                        HTuple hvZp = new HTuple((measureParam.IntervalZ * 1.0) / 1000);

                        HOperatorSet.GetImageSize(hoTileHeightImageZoom, out HTuple hvImageW, out HTuple hvImageH);

                        HTuple hvCirRow = hvOrbitParam[0] * hvScaleFactorW;
                        HTuple hvCirCol = hvOrbitParam[1] * hvScaleFactorH;
                        HTuple hvCirRad = hvOrbitParam[2] / hvAccelerationFactor;
                        HOperatorSet.GenCircle(out hoNailRegion, hvCirRow, hvCirCol, hvCirRad);
                        HOperatorSet.AreaCenter(hoNailWarpBaseMaskValid, out HTuple hvTmpArea, out HTuple hvTmpRow, out HTuple hvTmpCol);
                        if (hvTmpArea.D == 0)
                        {
                            Console.WriteLine("ERROR:failed to extract the region of base plane");
                            result.IsOk = false;
                            return result;
                        }

                        HOperatorSet.Intersection(hoValidMaskZoom, hoNailRegion, out hoNailRegionValid);
                        HOperatorSet.AreaCenter(hoNailRegionValid, out hvTmpArea, out hvTmpRow, out hvTmpCol);
                        if (hvTmpArea.D == 0)
                        {
                            Console.WriteLine("ERROR:failed to extract the region of anchor!");
                            result.IsOk = false;
                            return result;
                        }

                        HOperatorSet.FitSurfaceFirstOrder(hoNailWarpBaseMaskValid, hoTileHeightImageZoom, "tukey", 5, 1, out HTuple hvAlpha, out HTuple hvBeta, out HTuple hvGamma);
                        HOperatorSet.GenImageSurfaceFirstOrder(out hoImageSurface, "real", hvAlpha, hvBeta, hvGamma, hvTmpRow, hvTmpCol, hvImageW, hvImageH);
                        HOperatorSet.ReduceDomain(hoTileHeightImageZoom, hoNailRegionValid, out hoTmp);
                        ReplaceHobject(ref hoTileHeightImageZoom, ref hoTmp);
                        HOperatorSet.ReduceDomain(hoImageSurface, hoNailRegionValid, out hoTmp);
                        ReplaceHobject(ref hoImageSurface, ref hoTmp);
                        HOperatorSet.SubImage(hoTileHeightImageZoom, hoImageSurface, out hoImageSub, 1, 0);

                        //缩放到毫米单位
                        HOperatorSet.ScaleImage(hoImageSub, out hoTmp, hvZp, 0);
                        ReplaceHobject(ref hoImageSub, ref hoTmp);

                        HOperatorSet.MedianImage(hoImageSub, out hoTmp, "circle", 1, "mirrored");
                        ReplaceHobject(ref hoImageSub, ref hoTmp);

                        HOperatorSet.GrayFeatures(hoNailRegionValid, hoImageSub, "min", out HTuple hvWarpMinHeightValue);
                        HOperatorSet.GrayFeatures(hoNailRegionValid, hoImageSub, "max", out HTuple hvGlobalMaxWarpHeightValue);

                        result.IsOk = hvGlobalMaxWarpHeightValue.D < height_select;
                        result.Height = hvGlobalMaxWarpHeightValue.D;

                        HOperatorSet.Threshold(hoImageSub, out hoMaxHeightRegion, hvGlobalMaxWarpHeightValue - 0.0001, hvGlobalMaxWarpHeightValue + 0.0001);
                        HOperatorSet.AreaCenter(hoMaxHeightRegion, out hvGlobalMaxHeightArea, out HTuple hvMaxHeightRow, out HTuple hvMaxHeightColumn);
                        if (hvGlobalMaxHeightArea.D <= 0)
                        {
                            HOperatorSet.Threshold(hoImageSub, out hoTmp, hvGlobalMaxWarpHeightValue - 0.01, hvGlobalMaxWarpHeightValue + 0.01);
                            ReplaceHobject(ref hoMaxHeightRegion, ref hoTmp);
                            HOperatorSet.AreaCenter(hoMaxHeightRegion, out hvGlobalMaxHeightArea, out hvMaxHeightRow, out hvMaxHeightColumn);
                        }
                        if (hvGlobalMaxHeightArea.D > 0)
                        {
                            HOperatorSet.GetRegionPoints(hoMaxHeightRegion, out hvMaxHeightRows, out hvMaxHeightColumns);
                            if (hvMaxHeightRows.TupleLength() > 0 && hvMaxHeightColumns.TupleLength() > 0)
                            {
                                result.HighestPointRow = hvMaxHeightRows[(int)Math.Floor(hvMaxHeightRows.TupleLength() * 0.5)].D * hvAccelerationFactor.D;
                                result.HighestPointCol = hvMaxHeightColumns[(int)Math.Floor(hvMaxHeightColumns.TupleLength() * 0.5)].D * hvAccelerationFactor.D;
                            }
                            else
                            {
                                result.HighestPointRow = hvMaxHeightRow.D * hvAccelerationFactor.D;
                                result.HighestPointCol = hvMaxHeightColumn.D * hvAccelerationFactor.D;
                            }
                        }
                        else
                        {
                            Console.WriteLine("WARN:failed to extract max-height region of warp feature.");
                        }

                        if (height_select < hvGlobalMaxWarpHeightValue.D)
                        {
                            HOperatorSet.Threshold(hoImageSub, out hoWarpRegion, height_select, hvGlobalMaxWarpHeightValue + 0.01);
                            HOperatorSet.ClosingCircle(hoWarpRegion, out hoTmp, 5.5);
                            ReplaceHobject(ref hoWarpRegion, ref hoTmp);
                            HOperatorSet.Connection(hoWarpRegion, out hoWarpRegions);
                            HOperatorSet.SelectShape(hoWarpRegions, out hoTmp, "area", "and", 0, 9999999999999999999);
                            ReplaceHobject(ref hoWarpRegions, ref hoTmp);
                            HOperatorSet.CountObj(hoWarpRegions, out HTuple hvRegionsNum);

                            if (hvRegionsNum > 0)
                            {
                                HTuple hvLocalMaxWarpHeightValue = 0;
                                for (int i = 0; i < hvRegionsNum; i++)
                                {
                                    HObject hoWarpRegionSelected = new HObject();
                                    HObject hoWarpRegionHeightImage = new HObject();
                                    HObject hoWarpMaxHeightRegion = new HObject();
                                    HObject hoWarpRegionZoom = new HObject();
                                    HOperatorSet.GenEmptyObj(out hoWarpRegionSelected);
                                    HOperatorSet.GenEmptyObj(out hoWarpRegionHeightImage);
                                    HOperatorSet.GenEmptyObj(out hoWarpMaxHeightRegion);
                                    HOperatorSet.GenEmptyObj(out hoWarpRegionZoom);

                                    try
                                    {
                                        HOperatorSet.SelectObj(hoWarpRegions, out hoWarpRegionSelected, i + 1);
                                        HOperatorSet.AreaCenter(hoWarpRegionSelected, out hvLocalMaxHeightArea, out HTuple hvTmpValue0, out HTuple hvTmpValue1);
                                        if (hvLocalMaxHeightArea > 0)
                                        {
                                            HOperatorSet.ReduceDomain(hoImageSub, hoWarpRegionSelected, out hoWarpRegionHeightImage);
                                            HOperatorSet.GrayFeatures(hoWarpRegionSelected, hoWarpRegionHeightImage, "max", out HTuple hvTmpValue);
                                            HOperatorSet.Threshold(hoWarpRegionHeightImage, out hoWarpMaxHeightRegion, hvTmpValue - 0.01, hvTmpValue + 0.01);
                                            HOperatorSet.AreaCenter(hoWarpMaxHeightRegion, out hvLocalMaxHeightArea, out HTuple hvTmpMaxHeightRow, out HTuple hvTmpMaxHeightColumn);
                                            if (hvLocalMaxHeightArea > 0 && hvLocalMaxWarpHeightValue < hvTmpValue)
                                            {
                                                hvLocalMaxWarpHeightValue = hvTmpValue;
                                                result.HighestPointRow = hvTmpMaxHeightRow.D * hvAccelerationFactor.D;
                                                result.HighestPointCol = hvTmpMaxHeightColumn.D * hvAccelerationFactor.D;
                                            }

                                            HOperatorSet.ZoomRegion(hoWarpRegionSelected, out hoWarpRegionZoom, hvAccelerationFactor, hvAccelerationFactor);
                                            polygons.Add(new Polygon(hoWarpRegionZoom));
                                        }
                                    }
                                    finally
                                    {
                                        hoWarpRegionSelected.Dispose();
                                        hoWarpRegionHeightImage.Dispose();
                                        hoWarpMaxHeightRegion.Dispose();
                                        hoWarpRegionZoom.Dispose();
                                    }
                                }
                            }
                            else if (hvGlobalMaxHeightArea.D > 0)
                            {
                                HOperatorSet.ZoomRegion(hoMaxHeightRegion, out hoMaxHeightRegionZoom, hvAccelerationFactor, hvAccelerationFactor);
                                polygons.Add(new Polygon(hoMaxHeightRegionZoom));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logs.LogError($"{DateTime.Now}:GetWarpFeature()报错信息:{ex.Message},调用堆栈:{ex.StackTrace}");
                        Console.WriteLine(ex.StackTrace);
                        result.IsOk = false;
                    }
                    finally
                    {
                        hvGlobalMaxHeightArea.Dispose();
                        hvLocalMaxHeightArea.Dispose();

                        hvMaxHeightRows.Dispose();
                        hvMaxHeightColumns.Dispose();

                        hoTileGrayImageZoom.Dispose();
                        hoTileHeightImageZoom.Dispose();
                        hoNailWarpBaseMaskZoom.Dispose();
                        hoValidMaskThreshold.Dispose();
                        hoIrregularRegion.Dispose();
                        hoIrregularRegion0.Dispose();
                        hoIrregularRegion1.Dispose();
                        hoIrregularRegion2.Dispose();
                        hoIrregularMask.Dispose();
                        hoValidMaskZoom.Dispose();
                        hoNailWarpBaseMaskValid.Dispose();
                        hoNailRegion.Dispose();
                        hoNailRegionValid.Dispose();
                        hoImageSurface.Dispose();
                        hoImageSub.Dispose();

                        hoMaxHeightRegion.Dispose();
                        hoMaxHeightRegionZoom.Dispose();
                        hoWarpRegion.Dispose();
                        hoWarpRegions.Dispose();
                        hoTmp.Dispose();
                    }

                    return result;
                }
            }


            public static OrbitResult GetOrbitFeature(HTuple hvCx, HTuple hvCy, HTuple hvOrbitParam,
                                                      MFDJC0_MeasureParam measureParam, bool nailCenterIsTrue,
                                                      double offset_select = 0)
            {
                using (var dh = new HDevDisposeHelper())
                {
                    OrbitResult result = new OrbitResult();

                    HTuple scaleX = new HTuple();
                    HTuple scaleY = new HTuple();
                    HTuple hvXp = new HTuple();
                    HTuple hvYp = new HTuple();
                    HTuple hvDistanceRow = new HTuple();
                    HTuple hvDistanceCol = new HTuple();
                    HTuple hvDistance = new HTuple();

                    try
                    {
                        if (HasTupleValue(hvCx))
                            result.NailCenterCol = hvCx.TupleSelect(0).D;
                        if (HasTupleValue(hvCy))
                            result.NailCenterRow = hvCy.TupleSelect(0).D;
                        if (HasTupleValue(hvOrbitParam))
                            result.OrbitCenterRow = hvOrbitParam.TupleSelect(0).D;
                        if (HasTupleValue(hvOrbitParam, 2))
                            result.OrbitCenterCol = hvOrbitParam.TupleSelect(1).D;
                        if (HasTupleValue(hvOrbitParam, 3))
                            result.OrbitRadius = hvOrbitParam.TupleSelect(2).D;

                        if (measureParam == null)
                        {
                            Console.WriteLine("ERROR:GetOrbitFeature: measureParam is null");
                            result.IsOk = false;
                            return result;
                        }

                        if (!HasTupleValue(hvCx) || !HasTupleValue(hvCy) || !HasTupleValue(hvOrbitParam, 3))
                        {
                            Console.WriteLine("ERROR:GetOrbitFeature: invalid input tuple");
                            result.IsOk = false;
                            return result;
                        }

                        if (measureParam.IntervalX > measureParam.IntervalY)
                        {
                            scaleX = measureParam.IntervalX / measureParam.IntervalY;
                            scaleY = 1;
                        }
                        else
                        {
                            scaleX = 1;
                            scaleY = measureParam.IntervalY / measureParam.IntervalX;
                        }

                        hvXp = (measureParam.IntervalX * 1.0) / scaleX;
                        hvYp = (measureParam.IntervalY * 1.0) / scaleY;

                        hvDistanceRow = (hvOrbitParam[0] - hvCy) * hvXp;
                        hvDistanceCol = (hvOrbitParam[1] - hvCx) * hvYp;
                        hvDistance = (hvDistanceRow * hvDistanceRow + hvDistanceCol * hvDistanceCol).TupleSqrt();

                        // 结果输出, 单位mm
                        result.Offset = hvDistance.D * 0.001;
                        result.IsOk = (result.Offset < offset_select) && nailCenterIsTrue;
                    }
                    catch (Exception ex)
                    {
                        Logs.LogError($"{DateTime.Now}:GetOrbitFeature()报错信息:{ex.Message},调用堆栈:{ex.StackTrace}");
                        Console.WriteLine(ex.StackTrace);
                        result.IsOk = false;
                    }
                    finally
                    {
                        scaleX.Dispose();
                        scaleY.Dispose();
                        hvXp.Dispose();
                        hvYp.Dispose();
                        hvDistanceRow.Dispose();
                        hvDistanceCol.Dispose();
                        hvDistance.Dispose();
                    }

                    return result;
                }
            }

            public static DefectResult GetCrackFeature(int bboxId, ImageData imageData, HObject hoOrbitMask,
                                                       MFDJC0_MeasureParam measureParam,
                                                       double area_select = 0, double length_select = 0,
                                                       double width_select = 0, double depth_select = 0)
            {
                using (var dh = new HDevDisposeHelper())
                {
                    DefectResult result = new DefectResult();
                    List<Polygon> polygons = result.DefectPolygons;
                    bool hasProcessException = false;

                    HObject hoHeightImage = new HObject();
                    HObject hoValidMask = new HObject();
                    HObject hoValidMaskZoom = new HObject();
                    HObject hoOrbitMaskMoved = new HObject();
                    HObject hoCracks = new HObject();
                    HObject hoBoxRegionMask = new HObject();
                    HObject hoTmp = new HObject();

                    HOperatorSet.GenEmptyObj(out hoHeightImage);
                    HOperatorSet.GenEmptyObj(out hoValidMask);
                    HOperatorSet.GenEmptyObj(out hoValidMaskZoom);
                    HOperatorSet.GenEmptyObj(out hoOrbitMaskMoved);
                    HOperatorSet.GenEmptyObj(out hoCracks);
                    HOperatorSet.GenEmptyObj(out hoBoxRegionMask);
                    HOperatorSet.GenEmptyObj(out hoTmp);

                    HTuple scaleX = new HTuple();
                    HTuple scaleY = new HTuple();
                    HTuple hvXp = new HTuple();
                    HTuple hvZp = new HTuple();
                    HTuple offsetX = new HTuple();
                    HTuple offsetY = new HTuple();
                    HTuple hvAccelerationFactor = new HTuple();
                    HTuple hvScaleFactorW = new HTuple();
                    HTuple hvScaleFactorH = new HTuple();
                    HTuple hvNum = new HTuple();
                    HTuple hvDepthFeature = new HTuple();
                    HTuple hvWidthFeature = new HTuple();
                    HTuple hvLengthFeature = new HTuple();
                    HTuple hvAreaFeature = new HTuple();
                    HTuple hvaffineMatrix = new HTuple();
                    HTuple hvthreshold = new HTuple();
                    GCHandle handle = default(GCHandle);

                    try
                    {
                        if (imageData == null || measureParam == null)
                        {
                            Console.WriteLine("ERROR:GetCrackFeature: imageData or measureParam is null");
                            result.IsOk = false;
                            return result;
                        }

                        if (imageData.Boxes == null || bboxId < 0 || bboxId >= imageData.Boxes.Count)
                        {
                            Console.WriteLine("ERROR:GetCrackFeature: bboxId out of range");
                            result.IsOk = false;
                            return result;
                        }

                        Box bbox = imageData.Boxes[bboxId];
                        if (bbox == null)
                        {
                            Console.WriteLine("ERROR:GetCrackFeature: bbox is null");
                            result.IsOk = false;
                            return result;
                        }

                        if (imageData.hvIntervalX > imageData.hvIntervalY)
                        {
                            scaleX = imageData.hvIntervalX / imageData.hvIntervalY;
                            scaleY = 1;
                        }
                        else
                        {
                            scaleX = 1;
                            scaleY = imageData.hvIntervalY / imageData.hvIntervalX;
                        }
                        hvXp = (imageData.hvIntervalX * 1.0) / scaleX;
                        hvZp = (imageData.hvIntervalZ * 1.0);

                        offsetX = imageData.OffsetX;
                        offsetY = imageData.OffsetY;

                        FillDefectBaseResult(result, bbox, offsetX, offsetY, scaleX, scaleY);
                        result.CenterColFeature = ((((bbox.Left + bbox.Right) / 2.0) + offsetX) * scaleX).D;
                        result.CenterRowFeature = ((((bbox.Top + bbox.Bottom) / 2.0) + offsetY) * scaleY).D;

                        if (hoOrbitMask == null || !hoOrbitMask.IsInitialized())
                        {
                            Console.WriteLine("ERROR:GetCrackFeature: hoOrbitMask is invalid");
                            result.IsOk = false;
                            return result;
                        }

                        if (imageData.hoHeightImage == null || !imageData.hoHeightImage.IsInitialized()
                            || imageData.hoValidMask == null || !imageData.hoValidMask.IsInitialized())
                        {
                            Console.WriteLine("ERROR:GetCrackFeature: input image or valid mask is invalid");
                            result.IsOk = false;
                            return result;
                        }

                        hoHeightImage.Dispose();
                        hoHeightImage = imageData.hoHeightImage.Clone();
                        hoValidMask.Dispose();
                        hoValidMask = imageData.hoValidMask.Clone();

                        HOperatorSet.MoveRegion(hoOrbitMask, out hoOrbitMaskMoved, -offsetY, -offsetX);

                        hvAccelerationFactor = 1.0f;
                        hvScaleFactorW = 1.0f / hvAccelerationFactor;
                        hvScaleFactorH = 1.0f / hvAccelerationFactor;
                        HOperatorSet.ZoomImageFactor(hoHeightImage, out hoTmp, hvScaleFactorW, hvScaleFactorH, "nearest_neighbor");
                        ReplaceHobject(ref hoHeightImage, ref hoTmp);
                        HOperatorSet.ZoomRegion(hoOrbitMaskMoved, out hoTmp, hvScaleFactorW, hvScaleFactorH);
                        ReplaceHobject(ref hoOrbitMaskMoved, ref hoTmp);
                        HOperatorSet.ZoomRegion(hoValidMask, out hoTmp, hvScaleFactorW, hvScaleFactorH);
                        ReplaceHobject(ref hoValidMask, ref hoTmp);
                        HOperatorSet.Threshold(hoHeightImage, out hoValidMaskZoom, measureParam.MinDepth, measureParam.MaxDepth);
                        HOperatorSet.Intersection(hoValidMask, hoValidMaskZoom, out hoTmp);
                        ReplaceHobject(ref hoValidMask, ref hoTmp);

                        if (!HasValidSegmentation(bbox))
                        {
                            result.IsOk = false;
                            return result;
                        }

                        hvaffineMatrix = new HTuple(bbox.Seg.AffineMatrix);
                        hvaffineMatrix[0] = bbox.Seg.AffineMatrix[4];
                        hvaffineMatrix[1] = bbox.Seg.AffineMatrix[3];
                        hvaffineMatrix[2] = bbox.Seg.AffineMatrix[5];
                        hvaffineMatrix[3] = bbox.Seg.AffineMatrix[1];
                        hvaffineMatrix[4] = bbox.Seg.AffineMatrix[0];
                        hvaffineMatrix[5] = bbox.Seg.AffineMatrix[2];

                        HOperatorSet.GetImageSize(hoHeightImage, out HTuple hvRestoreWidth, out HTuple hvRestoreHeight);

                        // 深度图像素值转换为微米单位
                        HOperatorSet.ScaleImage(hoHeightImage, out hoTmp, hvZp, 0);
                        ReplaceHobject(ref hoHeightImage, ref hoTmp);

                        hvthreshold = new HTuple(bbox.Seg.Thresh);
                        handle = GCHandle.Alloc(bbox.Seg.Data, GCHandleType.Pinned);
                        IntPtr pointer = handle.AddrOfPinnedObject();
                        HOperatorSet.GenImage1(out hoCracks, "real", bbox.Seg.Width, bbox.Seg.Height, pointer);
                        //HOperatorSet.AffineTransImage(hoCracks, out hoTmp, hvaffineMatrix, "bilinear", "true");
                        HOperatorSet.AffineTransImageSize(hoCracks, out hoTmp, hvaffineMatrix, "bilinear", hvRestoreWidth, hvRestoreHeight);
                        ReplaceHobject(ref hoCracks, ref hoTmp);

                        //Sigmoid(hoCracks, out hoTmp);
                        //ReplaceHobject(ref hoCracks, ref hoTmp);

                        HOperatorSet.Threshold(hoCracks, out hoTmp, hvthreshold, 255);
                        ReplaceHobject(ref hoCracks, ref hoTmp);

                        // 只保留框内的分割掩码
                        HOperatorSet.GenRectangle1(out hoBoxRegionMask, bbox.Top, bbox.Left, bbox.Bottom, bbox.Right);
                        HOperatorSet.Intersection(hoCracks, hoBoxRegionMask, out hoTmp);
                        ReplaceHobject(ref hoCracks, ref hoTmp);

                        HOperatorSet.ZoomRegion(hoCracks, out hoTmp, hvScaleFactorW, hvScaleFactorH);
                        ReplaceHobject(ref hoCracks, ref hoTmp);
                        //HOperatorSet.Intersection(hoCracks, hoOrbitMaskMoved, out hoTmp);
                        //ReplaceHobject(ref hoCracks, ref hoTmp);
                        HOperatorSet.Intersection(hoCracks, hoValidMask, out hoTmp);
                        ReplaceHobject(ref hoCracks, ref hoTmp);
                        HOperatorSet.Connection(hoCracks, out hoTmp);
                        ReplaceHobject(ref hoCracks, ref hoTmp);
                        HOperatorSet.SelectShape(hoCracks, out hoTmp, "area", "and", 99, 9999999999999999999);
                        ReplaceHobject(ref hoCracks, ref hoTmp);

                        HOperatorSet.CountObj(hoCracks, out hvNum);

                        int polygonOffsetCol = (int)(offsetX.D * scaleX.D);
                        int polygonOffsetRow = (int)(offsetY.D * scaleY.D);

                        for (int i = 0; i < hvNum; i++)
                        {
                            HObject hoCrackCurrent = new HObject();
                            HObject hoScaledCurrent = new HObject();
                            HObject hoCrackZCurrent = new HObject();
                            HObject hoPolygonRegion = new HObject();
                            HObject hoLengthRegion = new HObject();
                            HObject hoRoughlySkeleton = new HObject();
                            HObject hoDistanceImage = new HObject();
                            HObject hoSkeletonCurrent = new HObject();
                            HObject hoContoursCurrent = new HObject();
                            HObject hoLinesCurrent = new HObject();
                            HObject hoLineCurrent = new HObject();
                            HObject hoCrackNext = new HObject();
                            HObject hoScaledNext = new HObject();
                            HObject hoTmpCurrent = new HObject();

                            HOperatorSet.GenEmptyObj(out hoCrackCurrent);
                            HOperatorSet.GenEmptyObj(out hoScaledCurrent);
                            HOperatorSet.GenEmptyObj(out hoCrackZCurrent);
                            HOperatorSet.GenEmptyObj(out hoPolygonRegion);
                            HOperatorSet.GenEmptyObj(out hoLengthRegion);
                            HOperatorSet.GenEmptyObj(out hoRoughlySkeleton);
                            HOperatorSet.GenEmptyObj(out hoDistanceImage);
                            HOperatorSet.GenEmptyObj(out hoSkeletonCurrent);
                            HOperatorSet.GenEmptyObj(out hoContoursCurrent);
                            HOperatorSet.GenEmptyObj(out hoLinesCurrent);
                            HOperatorSet.GenEmptyObj(out hoLineCurrent);
                            HOperatorSet.GenEmptyObj(out hoCrackNext);
                            HOperatorSet.GenEmptyObj(out hoScaledNext);
                            HOperatorSet.GenEmptyObj(out hoTmpCurrent);

                            HTuple hvRows = new HTuple();
                            HTuple hvCols = new HTuple();
                            HTuple hvX = new HTuple();
                            HTuple hvY = new HTuple();
                            HTuple hvZ = new HTuple();
                            HTuple hvCrackCloud = new HTuple();
                            HTuple hvPlane = new HTuple();
                            HTuple hvPose = new HTuple();
                            HTuple hvNormal = new HTuple();
                            HTuple hvPoseMat = new HTuple();
                            HTuple hvConnCloud = new HTuple();
                            HTuple hvSeleCloud = new HTuple();
                            HTuple hvUnionCloud = new HTuple();
                            HTuple hvPointNum = new HTuple();
                            HTuple hvSmthCloud = new HTuple();
                            HTuple hvAffdCloud = new HTuple();
                            HTuple hvValueZ = new HTuple();
                            HTuple hvSortedZ = new HTuple();
                            HTuple hvAvgZ = new HTuple();
                            HTuple hvMinZ = new HTuple();
                            HTuple hvMark0 = new HTuple();
                            HTuple hvB0 = new HTuple();
                            HTuple hvTop0 = new HTuple();
                            HTuple hvDiv0 = new HTuple();
                            HTuple hvMark1 = new HTuple();
                            HTuple hvB1 = new HTuple();
                            HTuple hvTop2 = new HTuple();
                            HTuple hvDiv1 = new HTuple();
                            HTuple hvTemp = new HTuple(0);
                            HTuple hvRadius = new HTuple();
                            HTuple hvLengthes = new HTuple();
                            HTuple hvIndex = new HTuple();
                            HTuple hvSize = new HTuple();
                            HTuple hvLength = new HTuple(0);
                            HTuple hvGapDistance = new HTuple(0);
                            HTuple hvTmpArea = new HTuple();
                            HTuple hvFlip = new HTuple();
                            HTuple hvMaxLengthIndex = new HTuple();

                            HTuple hvRowsSkel = new HTuple();
                            HTuple hvColsSkel = new HTuple();
                            HTuple hvDistVals = new HTuple();
                            HTuple hvWidthVals = new HTuple();
                            HTuple hvWidthValsSorted = new HTuple();
                            HTuple hvWidthValsValid = new HTuple();
                            HTuple hvWidthTypical = new HTuple();
                            HTuple hvRadiusClose = new HTuple();
                            HTuple hvRadiusOpen = new HTuple();

                            try
                            {
                                HOperatorSet.SelectObj(hoCracks, out hoCrackCurrent, i + 1);
                                HOperatorSet.ZoomRegion(hoCrackCurrent, out hoScaledCurrent, scaleX, scaleY);
                                HOperatorSet.FillUp(hoScaledCurrent, out hoTmpCurrent);
                                ReplaceHobject(ref hoScaledCurrent, ref hoTmpCurrent);

                                HOperatorSet.ZoomRegion(hoScaledCurrent, out hoPolygonRegion, hvAccelerationFactor, hvAccelerationFactor);
                                polygons.Add(new Polygon(hoPolygonRegion, polygonOffsetCol, polygonOffsetRow));

                                HOperatorSet.RegionFeatures(hoScaledCurrent, "inner_radius", out hvRadius);
                                if (HasTupleValue(hvRadius))
                                    hvWidthFeature = hvWidthFeature.TupleConcat((2 * hvRadius) * hvXp);

                                //填孔，避免区域内部小黑洞影响骨架
                                HOperatorSet.FillUp(hoScaledCurrent, out hoLengthRegion);
                                // B.粗骨架
                                HOperatorSet.Skeleton(hoLengthRegion, out hoRoughlySkeleton);
                                // C.距离变换
                                HOperatorSet.DistanceTransform(hoLengthRegion, out hoDistanceImage, "euclidean", "true", hvRestoreWidth * scaleX, hvRestoreHeight * scaleY);
                                // D. 骨架点采样
                                HOperatorSet.GetRegionPoints(hoRoughlySkeleton, out hvRowsSkel, out hvColsSkel);
                                HOperatorSet.GetGrayval(hoDistanceImage, hvRowsSkel.TupleInt(), hvColsSkel.TupleInt(), out hvDistVals);
                                hvWidthVals = 2.0 * hvDistVals;
                                // E. 去掉太小的异常值
                                HOperatorSet.TupleSort(hvWidthVals, out hvWidthValsSorted);
                                HOperatorSet.TupleMedian(hvWidthVals, out HTuple hvWidthMed);
                                HTuple hvThresh = 0.5 * hvWidthMed;

                                for (int idx = 0; idx < hvWidthVals.Length; idx++)
                                {
                                    if (hvWidthVals[idx] >= hvThresh.D)
                                    {
                                        hvWidthValsValid = hvWidthValsValid.TupleConcat(hvWidthVals[idx]);
                                    }
                                }
                                // F.计算典型宽度
                                if (hvWidthValsValid.Length > 0)
                                {
                                    HOperatorSet.TupleMedian(hvWidthValsValid, out hvWidthTypical);
                                }
                                else
                                {
                                    hvWidthTypical = hvWidthMed;
                                }
                                // G. 自动生成形态学参数（0.2与0.12为主要调参项）
                                HOperatorSet.TupleMax2(1, 0.20 * hvWidthTypical, out hvRadiusClose);
                                HOperatorSet.TupleMax2(0.5, 0.12 * hvWidthTypical, out hvRadiusOpen);
                                // H. 形态学平滑
                                HOperatorSet.ClosingCircle(hoLengthRegion, out hoTmp, hvRadiusClose);
                                ReplaceHobject(ref hoLengthRegion, ref hoTmp);
                                HOperatorSet.OpeningCircle(hoLengthRegion, out hoTmp, hvRadiusOpen);
                                ReplaceHobject(ref hoLengthRegion, ref hoTmp);

                                
                                //HOperatorSet.ClosingCircle(hoScaledCurrent, out hoLengthRegion, 50);
                                HOperatorSet.Skeleton(hoLengthRegion, out hoSkeletonCurrent);
                                HOperatorSet.GenContoursSkeletonXld(hoSkeletonCurrent, out hoContoursCurrent, 1, "filter");
                                HOperatorSet.UnionAdjacentContoursXld(hoContoursCurrent, out hoLinesCurrent, 10, 1, "attr_keep");
                                HOperatorSet.LengthXld(hoLinesCurrent, out hvLengthes);
                                if (HasTupleValue(hvLengthes))
                                {
                                    HOperatorSet.TupleSortIndex(hvLengthes, out hvIndex);
                                    HOperatorSet.TupleLength(hvIndex, out hvSize);
                                    if (HasTupleValue(hvSize) && hvSize.I > 0)
                                    {
                                        hvMaxLengthIndex = hvIndex.TupleSelect(hvSize.I - 1);
                                        HOperatorSet.SelectObj(hoLinesCurrent, out hoLineCurrent, hvMaxLengthIndex + 1);
                                        hvLength = hvLengthes.TupleSelect(hvMaxLengthIndex);
                                    }
                                }
                                if (i + 2 <= hvNum.I)
                                {
                                    HOperatorSet.SelectObj(hoCracks, out hoCrackNext, i + 2);
                                    HOperatorSet.ZoomRegion(hoCrackNext, out hoScaledNext, scaleX, scaleY);
                                    HOperatorSet.DistanceRrMinDil(hoScaledCurrent, hoScaledNext, out hvGapDistance);
                                }
                                hvLengthFeature = hvLengthFeature.TupleConcat((hvLength + hvGapDistance) * hvXp);

                                HOperatorSet.RegionFeatures(hoScaledCurrent, "area", out hvTmpArea);
                                if (HasTupleValue(hvTmpArea))
                                {
                                    hvTmpArea = hvTmpArea * hvXp * hvXp;
                                    hvAreaFeature = hvAreaFeature.TupleConcat(hvTmpArea);
                                }

                                HOperatorSet.ReduceDomain(hoHeightImage, hoCrackCurrent, out hoCrackZCurrent);
                                HOperatorSet.GetRegionPoints(hoCrackZCurrent, out hvRows, out hvCols);
                                if (!HasTupleValue(hvRows) || !HasTupleValue(hvCols))
                                    continue;

                                hvX = hvCols * imageData.hvIntervalX;
                                hvY = hvRows * imageData.hvIntervalY;
                                HOperatorSet.GetGrayval(hoCrackZCurrent, hvRows, hvCols, out hvZ);
                                if (!HasTupleValue(hvZ))
                                    continue;

                                HOperatorSet.GenObjectModel3dFromPoints(hvX, hvY, hvZ, out hvCrackCloud);
                                HOperatorSet.FitPrimitivesObjectModel3d(hvCrackCloud, (new HTuple("primitive_type")).TupleConcat("fitting_algorithm"),
                                                                         (new HTuple("plane")).TupleConcat("least_squares"), out hvPlane);
                                HOperatorSet.GetObjectModel3dParams(hvPlane, "primitive_parameter_pose", out hvPose);
                                HOperatorSet.PoseInvert(hvPose, out hvPose);
                                HOperatorSet.GetObjectModel3dParams(hvPlane, "primitive_parameter", out hvNormal);
                                if (HasTupleValue(hvNormal, 3) && hvNormal.TupleSelect(2).D < 0)
                                {
                                    HOperatorSet.CreatePose(0, 0, 0, 180, 0, 0, "Rp+T", "gba", "point", out hvFlip);
                                    HOperatorSet.PoseCompose(hvFlip, hvPose, out hvPose);
                                }
                                HOperatorSet.PoseToHomMat3d(hvPose, out hvPoseMat);
                                HOperatorSet.ConnectionObjectModel3d(hvCrackCloud, "distance_3d", 2 * hvXp, out hvConnCloud);
                                HOperatorSet.SelectObjectModel3d(hvConnCloud, "num_points", "and", 9, 9999999999999999999, out hvSeleCloud);
                                HOperatorSet.UnionObjectModel3d(hvSeleCloud, "points_surface", out hvUnionCloud);
                                HOperatorSet.GetObjectModel3dParams(hvUnionCloud, "num_points", out hvPointNum);
                                if (!HasTupleValue(hvPointNum) || hvPointNum.I == 0)
                                    continue;

                                HOperatorSet.SmoothObjectModel3d(hvUnionCloud, "mls", "mls_kNN", 199, out hvSmthCloud);
                                HOperatorSet.AffineTransObjectModel3d(hvSmthCloud, hvPoseMat, out hvAffdCloud);
                                HOperatorSet.GetObjectModel3dParams(hvAffdCloud, "point_coord_z", out hvValueZ);
                                if (!HasTupleValue(hvValueZ))
                                    continue;

                                HOperatorSet.TupleSort(hvValueZ, out hvSortedZ);
                                HOperatorSet.TupleInverse(hvSortedZ, out hvSortedZ);
                                HOperatorSet.TupleMean(hvSortedZ, out hvAvgZ);
                                HOperatorSet.TupleMin(hvSortedZ, out hvMinZ);
                                HOperatorSet.TupleLessElem(hvSortedZ, hvAvgZ, out hvMark0);
                                HOperatorSet.TupleFindFirst(hvMark0, 1, out hvB0);
                                if (HasTupleValue(hvB0) && hvB0.I != -1)
                                {
                                    HOperatorSet.TupleSelectRange(hvSortedZ, 0, hvB0, out hvTop0);
                                    if (HasTupleValue(hvTop0))
                                    {
                                        HOperatorSet.TupleMean(hvTop0, out hvDiv0);
                                        HOperatorSet.TupleLessElem(hvTop0, hvDiv0, out hvMark1);
                                        HOperatorSet.TupleFindFirst(hvMark1, 1, out hvB1);
                                        if (HasTupleValue(hvB1) && hvB1.I != -1)
                                        {
                                            HOperatorSet.TupleSelectRange(hvTop0, 0, hvB1, out hvTop2);
                                            if (HasTupleValue(hvTop2))
                                                HOperatorSet.TupleMean(hvTop2, out hvDiv1);
                                            else
                                                hvDiv1 = hvDiv0.Clone();
                                        }
                                        else
                                        {
                                            hvDiv1 = hvDiv0.Clone();
                                        }
                                        hvTemp = hvDiv1 - hvMinZ;
                                    }
                                }
                                hvDepthFeature = hvDepthFeature.TupleConcat(hvTemp);
                            }
                            catch (Exception ex)
                            {
                                hasProcessException = true;
                                Console.WriteLine($"WARN:GetCrackFeature()第{i + 1}个裂纹测量失败:{ex.Message}");
                            }
                            finally
                            {
                                hoCrackCurrent.Dispose();
                                hoScaledCurrent.Dispose();
                                hoCrackZCurrent.Dispose();
                                hoPolygonRegion.Dispose();
                                hoLengthRegion.Dispose();
                                hoRoughlySkeleton.Dispose();
                                hoDistanceImage.Dispose();
                                hoSkeletonCurrent.Dispose();
                                hoContoursCurrent.Dispose();
                                hoLinesCurrent.Dispose();
                                hoLineCurrent.Dispose();
                                hoCrackNext.Dispose();
                                hoScaledNext.Dispose();
                                hoTmpCurrent.Dispose();

                                hvRows.Dispose();
                                hvCols.Dispose();
                                hvX.Dispose();
                                hvY.Dispose();
                                hvZ.Dispose();
                                hvCrackCloud.Dispose();
                                hvPlane.Dispose();
                                hvPose.Dispose();
                                hvNormal.Dispose();
                                hvPoseMat.Dispose();
                                hvConnCloud.Dispose();
                                hvSeleCloud.Dispose();
                                hvUnionCloud.Dispose();
                                hvPointNum.Dispose();
                                hvSmthCloud.Dispose();
                                hvAffdCloud.Dispose();
                                hvValueZ.Dispose();
                                hvSortedZ.Dispose();
                                hvAvgZ.Dispose();
                                hvMinZ.Dispose();
                                hvMark0.Dispose();
                                hvB0.Dispose();
                                hvTop0.Dispose();
                                hvDiv0.Dispose();
                                hvMark1.Dispose();
                                hvB1.Dispose();
                                hvTop2.Dispose();
                                hvDiv1.Dispose();
                                hvTemp.Dispose();
                                hvRadius.Dispose();
                                hvLengthes.Dispose();
                                hvIndex.Dispose();
                                hvSize.Dispose();
                                hvLength.Dispose();
                                hvGapDistance.Dispose();
                                hvTmpArea.Dispose();
                                hvFlip.Dispose();
                                hvMaxLengthIndex.Dispose();
                            }
                        }

                        if (HasTupleValue(hvDepthFeature))
                            result.DepthFeature = hvDepthFeature.TupleMax().D * 0.001;
                        if (HasTupleValue(hvWidthFeature))
                            result.WidthFeature = (hvWidthFeature.TupleMax() * hvAccelerationFactor).D * 0.001;
                        if (HasTupleValue(hvLengthFeature))
                            result.LengthFeature = (hvLengthFeature.TupleSum() * hvAccelerationFactor).D * 0.001;
                        if (HasTupleValue(hvAreaFeature))
                            result.AreaFeature = (hvAreaFeature.TupleSum() * hvAccelerationFactor * hvAccelerationFactor).D * 0.001 * 0.001;

                        if (HasTupleValue(hvDepthFeature) && HasTupleValue(hvWidthFeature) && HasTupleValue(hvLengthFeature) && HasTupleValue(hvAreaFeature))
                        {
                            if (result.DepthFeature > depth_select && result.AreaFeature > area_select &&
                                result.LengthFeature > length_select && result.WidthFeature > width_select)
                            {
                                result.IsOk = false;
                            }
                            else
                            {
                                result.IsOk = true;
                            }
                        }
                        else if (HasTupleValue(hvNum) && hvNum.I > 0 && hasProcessException)
                        {
                            result.IsOk = false;
                        }
                        else
                        {
                            result.IsOk = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logs.LogError($"{DateTime.Now}:GetCrackFeature()报错信息:{ex.Message},调用堆栈:{ex.StackTrace}");
                        Console.WriteLine(ex.StackTrace);
                        result.IsOk = false;
                    }
                    finally
                    {
                        if (handle.IsAllocated)
                            handle.Free();

                        hoHeightImage.Dispose();
                        hoValidMask.Dispose();
                        hoValidMaskZoom.Dispose();
                        hoOrbitMaskMoved.Dispose();
                        hoCracks.Dispose();
                        hoBoxRegionMask.Dispose();
                        hoTmp.Dispose();

                        scaleX.Dispose();
                        scaleY.Dispose();
                        hvXp.Dispose();
                        hvZp.Dispose();
                        offsetX.Dispose();
                        offsetY.Dispose();
                        hvAccelerationFactor.Dispose();
                        hvScaleFactorW.Dispose();
                        hvScaleFactorH.Dispose();
                        hvNum.Dispose();
                        hvDepthFeature.Dispose();
                        hvWidthFeature.Dispose();
                        hvLengthFeature.Dispose();
                        hvAreaFeature.Dispose();
                        hvaffineMatrix.Dispose();
                        hvthreshold.Dispose();
                    }

                    return result;
                }
            }


            public static DefectResult GetDepthFeature(int bboxId, ImageData imageData, MFDJC0_MeasureParam measureParam,
                                                       double area_select = 0, double depth_select = 0)
            {
                using (var dh = new HDevDisposeHelper())
                {
                    DefectResult result = new DefectResult();
                    List<Polygon> polygons = result.DefectPolygons;
                    bool hasProcessException = false;

                    HObject hoHeightImage = new HObject();
                    HObject hoValidMask = new HObject();
                    HObject hoValidMaskZoom = new HObject();
                    HObject hoDefect = new HObject();
                    HObject hoBoxRegionMask = new HObject();
                    HObject hoTmp = new HObject();

                    HOperatorSet.GenEmptyObj(out hoHeightImage);
                    HOperatorSet.GenEmptyObj(out hoValidMask);
                    HOperatorSet.GenEmptyObj(out hoValidMaskZoom);
                    HOperatorSet.GenEmptyObj(out hoDefect);
                    HOperatorSet.GenEmptyObj(out hoBoxRegionMask);
                    HOperatorSet.GenEmptyObj(out hoTmp);

                    HTuple scaleX = new HTuple();
                    HTuple scaleY = new HTuple();
                    HTuple hvXp = new HTuple();
                    HTuple hvZp = new HTuple();
                    HTuple offsetX = new HTuple();
                    HTuple offsetY = new HTuple();
                    HTuple hvAccelerationFactor = new HTuple();
                    HTuple hvScaleFactorW = new HTuple();
                    HTuple hvScaleFactorH = new HTuple();
                    HTuple hvNum = new HTuple();
                    HTuple hvDepthFeature = new HTuple();
                    HTuple hvAreaFeature = new HTuple();
                    HTuple hvaffineMatrix = new HTuple();
                    HTuple hvthreshold = new HTuple();
                    GCHandle handle = default(GCHandle);

                    try
                    {
                        if (imageData == null || measureParam == null)
                        {
                            Console.WriteLine("ERROR:GetDepthFeature: imageData or measureParam is null");
                            result.IsOk = false;
                            return result;
                        }

                        if (imageData.Boxes == null || bboxId < 0 || bboxId >= imageData.Boxes.Count)
                        {
                            Console.WriteLine("ERROR:GetDepthFeature: bboxId out of range");
                            result.IsOk = false;
                            return result;
                        }

                        Box bbox = imageData.Boxes[bboxId];
                        if (bbox == null)
                        {
                            Console.WriteLine("ERROR:GetDepthFeature: bbox is null");
                            result.IsOk = false;
                            return result;
                        }

                        if (imageData.hvIntervalX > imageData.hvIntervalY)
                        {
                            scaleX = imageData.hvIntervalX / imageData.hvIntervalY;
                            scaleY = 1;
                        }
                        else
                        {
                            scaleX = 1;
                            scaleY = imageData.hvIntervalY / imageData.hvIntervalX;
                        }
                        hvXp = (imageData.hvIntervalX * 1.0) / scaleX;
                        hvZp = (imageData.hvIntervalZ * 1.0);

                        offsetX = imageData.OffsetX;
                        offsetY = imageData.OffsetY;

                        FillDefectBaseResult(result, bbox, offsetX, offsetY, scaleX, scaleY);
                        result.CenterColFeature = ((((bbox.Left + bbox.Right) / 2.0) + offsetX) * scaleX).D;
                        result.CenterRowFeature = ((((bbox.Top + bbox.Bottom) / 2.0) + offsetY) * scaleY).D;


                        if (imageData.hoHeightImage == null || !imageData.hoHeightImage.IsInitialized()
                            || imageData.hoValidMask == null || !imageData.hoValidMask.IsInitialized())
                        {
                            Console.WriteLine("ERROR:GetDepthFeature: input image or valid mask is invalid");
                            result.IsOk = false;
                            return result;
                        }

                        hoHeightImage.Dispose();
                        hoHeightImage = imageData.hoHeightImage.Clone();
                        hoValidMask.Dispose();
                        hoValidMask = imageData.hoValidMask.Clone();

                        hvAccelerationFactor = 1.0f;
                        hvScaleFactorW = 1.0f / hvAccelerationFactor;
                        hvScaleFactorH = 1.0f / hvAccelerationFactor;
                        HOperatorSet.ZoomImageFactor(hoHeightImage, out hoTmp, hvScaleFactorW, hvScaleFactorH, "nearest_neighbor");
                        ReplaceHobject(ref hoHeightImage, ref hoTmp);
                        HOperatorSet.ZoomRegion(hoValidMask, out hoTmp, hvScaleFactorW, hvScaleFactorH);
                        ReplaceHobject(ref hoValidMask, ref hoTmp);
                        HOperatorSet.Threshold(hoHeightImage, out hoValidMaskZoom, measureParam.MinDepth, measureParam.MaxDepth);
                        HOperatorSet.Intersection(hoValidMask, hoValidMaskZoom, out hoTmp);
                        ReplaceHobject(ref hoValidMask, ref hoTmp);

                        if (!HasValidSegmentation(bbox))
                        {
                            result.IsOk = false;
                            return result;
                        }

                        hvaffineMatrix = new HTuple(bbox.Seg.AffineMatrix);
                        hvaffineMatrix[0] = bbox.Seg.AffineMatrix[4];
                        hvaffineMatrix[1] = bbox.Seg.AffineMatrix[3];
                        hvaffineMatrix[2] = bbox.Seg.AffineMatrix[5];
                        hvaffineMatrix[3] = bbox.Seg.AffineMatrix[1];
                        hvaffineMatrix[4] = bbox.Seg.AffineMatrix[0];
                        hvaffineMatrix[5] = bbox.Seg.AffineMatrix[2];

                        HOperatorSet.GetImageSize(hoHeightImage, out HTuple hvRestoreWidth, out HTuple hvRestoreHeight);

                        // 深度图像素值转换为微米单位
                        HOperatorSet.ScaleImage(hoHeightImage, out hoTmp, hvZp, 0);
                        ReplaceHobject(ref hoHeightImage, ref hoTmp);


                        hvthreshold = new HTuple(bbox.Seg.Thresh);
                        handle = GCHandle.Alloc(bbox.Seg.Data, GCHandleType.Pinned);
                        IntPtr pointer = handle.AddrOfPinnedObject();
                        HOperatorSet.GenImage1(out hoDefect, "real", bbox.Seg.Width, bbox.Seg.Height, pointer);
                        //HOperatorSet.AffineTransImage(hoDefect, out hoTmp, hvaffineMatrix, "bilinear", "true");
                        HOperatorSet.AffineTransImageSize(hoDefect, out hoTmp, hvaffineMatrix, "bilinear", hvRestoreWidth, hvRestoreHeight);
                        ReplaceHobject(ref hoDefect, ref hoTmp);

                        //Sigmoid(hoDefect, out hoTmp);
                        //ReplaceHobject(ref hoDefect, ref hoTmp);

                        HOperatorSet.Threshold(hoDefect, out hoTmp, hvthreshold, 255);
                        ReplaceHobject(ref hoDefect, ref hoTmp);

                        // 只保留框内的分割掩码
                        HOperatorSet.GenRectangle1(out hoBoxRegionMask, bbox.Top, bbox.Left, bbox.Bottom, bbox.Right);
                        HOperatorSet.Intersection(hoDefect, hoBoxRegionMask, out hoTmp);
                        ReplaceHobject(ref hoDefect, ref hoTmp);

                        HOperatorSet.ZoomRegion(hoDefect, out hoTmp, hvScaleFactorW, hvScaleFactorH);
                        ReplaceHobject(ref hoDefect, ref hoTmp);
                        HOperatorSet.Intersection(hoDefect, hoValidMask, out hoTmp);
                        ReplaceHobject(ref hoDefect, ref hoTmp);
                        HOperatorSet.Connection(hoDefect, out hoTmp);
                        ReplaceHobject(ref hoDefect, ref hoTmp);
                        HOperatorSet.SelectShape(hoDefect, out hoTmp, "area", "and", 99, 9999999999999999999);
                        ReplaceHobject(ref hoDefect, ref hoTmp);

                        HOperatorSet.CountObj(hoDefect, out hvNum);

                        int polygonOffsetCol = (int)(offsetX.D * scaleX.D);
                        int polygonOffsetRow = (int)(offsetY.D * scaleY.D);

                        for (int i = 0; i < hvNum; i++)
                        {
                            HObject hoDefectCurrent = new HObject();
                            HObject hoScaledCurrent = new HObject();
                            HObject hoDefectZCurrent = new HObject();
                            HObject hoPolygonRegion = new HObject();
                            HObject hoTmpCurrent = new HObject();

                            HOperatorSet.GenEmptyObj(out hoDefectCurrent);
                            HOperatorSet.GenEmptyObj(out hoScaledCurrent);
                            HOperatorSet.GenEmptyObj(out hoDefectZCurrent);
                            HOperatorSet.GenEmptyObj(out hoPolygonRegion);
                            HOperatorSet.GenEmptyObj(out hoTmpCurrent);

                            HTuple hvRows = new HTuple();
                            HTuple hvCols = new HTuple();
                            HTuple hvX = new HTuple();
                            HTuple hvY = new HTuple();
                            HTuple hvZ = new HTuple();
                            HTuple hvDefectloud = new HTuple();
                            HTuple hvPlane = new HTuple();
                            HTuple hvPose = new HTuple();
                            HTuple hvNormal = new HTuple();
                            HTuple hvPoseMat = new HTuple();
                            HTuple hvConnCloud = new HTuple();
                            HTuple hvSeleCloud = new HTuple();
                            HTuple hvUnionCloud = new HTuple();
                            HTuple hvPointNum = new HTuple();
                            HTuple hvSmthCloud = new HTuple();
                            HTuple hvAffdCloud = new HTuple();
                            HTuple hvValueZ = new HTuple();
                            HTuple hvSortedZ = new HTuple();
                            HTuple hvAvgZ = new HTuple();
                            HTuple hvMinZ = new HTuple();
                            HTuple hvMark0 = new HTuple();
                            HTuple hvB0 = new HTuple();
                            HTuple hvTop0 = new HTuple();
                            HTuple hvDiv0 = new HTuple();
                            HTuple hvMark1 = new HTuple();
                            HTuple hvB1 = new HTuple();
                            HTuple hvTop2 = new HTuple();
                            HTuple hvDiv1 = new HTuple();
                            HTuple hvTemp = new HTuple(0);
                            HTuple hvLengthes = new HTuple();
                            HTuple hvIndex = new HTuple();
                            HTuple hvSize = new HTuple();
                            HTuple hvLength = new HTuple(0);
                            HTuple hvGapDistance = new HTuple(0);
                            HTuple hvTmpArea = new HTuple();
                            HTuple hvFlip = new HTuple();
                            HTuple hvMaxLengthIndex = new HTuple();

                            try
                            {
                                HOperatorSet.SelectObj(hoDefect, out hoDefectCurrent, i + 1);
                                HOperatorSet.ZoomRegion(hoDefectCurrent, out hoScaledCurrent, scaleX, scaleY);
                                HOperatorSet.FillUp(hoScaledCurrent, out hoTmpCurrent);
                                ReplaceHobject(ref hoScaledCurrent, ref hoTmpCurrent);

                                HOperatorSet.ZoomRegion(hoScaledCurrent, out hoPolygonRegion, hvAccelerationFactor, hvAccelerationFactor);
                                polygons.Add(new Polygon(hoPolygonRegion, polygonOffsetCol, polygonOffsetRow));

                                HOperatorSet.RegionFeatures(hoScaledCurrent, "area", out hvTmpArea);
                                if (HasTupleValue(hvTmpArea))
                                {
                                    hvTmpArea = hvTmpArea * hvXp * hvXp;
                                    hvAreaFeature = hvAreaFeature.TupleConcat(hvTmpArea);
                                }

                                HOperatorSet.ReduceDomain(hoHeightImage, hoDefectCurrent, out hoDefectZCurrent);
                                HOperatorSet.GetRegionPoints(hoDefectZCurrent, out hvRows, out hvCols);
                                if (!HasTupleValue(hvRows) || !HasTupleValue(hvCols))
                                    continue;

                                hvX = hvCols * imageData.hvIntervalX;
                                hvY = hvRows * imageData.hvIntervalY;
                                HOperatorSet.GetGrayval(hoDefectZCurrent, hvRows, hvCols, out hvZ);
                                if (!HasTupleValue(hvZ))
                                    continue;

                                HOperatorSet.GenObjectModel3dFromPoints(hvX, hvY, hvZ, out hvDefectloud);
                                HOperatorSet.FitPrimitivesObjectModel3d(hvDefectloud, (new HTuple("primitive_type")).TupleConcat("fitting_algorithm"),
                                                                         (new HTuple("plane")).TupleConcat("least_squares"), out hvPlane);
                                HOperatorSet.GetObjectModel3dParams(hvPlane, "primitive_parameter_pose", out hvPose);
                                HOperatorSet.PoseInvert(hvPose, out hvPose);
                                HOperatorSet.GetObjectModel3dParams(hvPlane, "primitive_parameter", out hvNormal);
                                if (HasTupleValue(hvNormal, 3) && hvNormal.TupleSelect(2).D < 0)
                                {
                                    HOperatorSet.CreatePose(0, 0, 0, 180, 0, 0, "Rp+T", "gba", "point", out hvFlip);
                                    HOperatorSet.PoseCompose(hvFlip, hvPose, out hvPose);
                                }
                                HOperatorSet.PoseToHomMat3d(hvPose, out hvPoseMat);
                                HOperatorSet.ConnectionObjectModel3d(hvDefectloud, "distance_3d", 2 * hvXp, out hvConnCloud);
                                HOperatorSet.SelectObjectModel3d(hvConnCloud, "num_points", "and", 9, 9999999999999999999, out hvSeleCloud);
                                HOperatorSet.UnionObjectModel3d(hvSeleCloud, "points_surface", out hvUnionCloud);
                                HOperatorSet.GetObjectModel3dParams(hvUnionCloud, "num_points", out hvPointNum);
                                if (!HasTupleValue(hvPointNum) || hvPointNum.I == 0)
                                    continue;

                                HOperatorSet.SmoothObjectModel3d(hvUnionCloud, "mls", "mls_kNN", 199, out hvSmthCloud);
                                HOperatorSet.AffineTransObjectModel3d(hvSmthCloud, hvPoseMat, out hvAffdCloud);
                                HOperatorSet.GetObjectModel3dParams(hvAffdCloud, "point_coord_z", out hvValueZ);
                                if (!HasTupleValue(hvValueZ))
                                    continue;

                                HOperatorSet.TupleSort(hvValueZ, out hvSortedZ);
                                HOperatorSet.TupleInverse(hvSortedZ, out hvSortedZ);
                                HOperatorSet.TupleMean(hvSortedZ, out hvAvgZ);
                                HOperatorSet.TupleMin(hvSortedZ, out hvMinZ);
                                HOperatorSet.TupleLessElem(hvSortedZ, hvAvgZ, out hvMark0);
                                HOperatorSet.TupleFindFirst(hvMark0, 1, out hvB0);
                                if (HasTupleValue(hvB0) && hvB0.I != -1)
                                {
                                    HOperatorSet.TupleSelectRange(hvSortedZ, 0, hvB0, out hvTop0);
                                    if (HasTupleValue(hvTop0))
                                    {
                                        HOperatorSet.TupleMean(hvTop0, out hvDiv0);
                                        HOperatorSet.TupleLessElem(hvTop0, hvDiv0, out hvMark1);
                                        HOperatorSet.TupleFindFirst(hvMark1, 1, out hvB1);
                                        if (HasTupleValue(hvB1) && hvB1.I != -1)
                                        {
                                            HOperatorSet.TupleSelectRange(hvTop0, 0, hvB1, out hvTop2);
                                            if (HasTupleValue(hvTop2))
                                                HOperatorSet.TupleMean(hvTop2, out hvDiv1);
                                            else
                                                hvDiv1 = hvDiv0.Clone();
                                        }
                                        else
                                        {
                                            hvDiv1 = hvDiv0.Clone();
                                        }
                                        hvTemp = hvDiv1 - hvMinZ;
                                    }
                                }
                                hvDepthFeature = hvDepthFeature.TupleConcat(hvTemp);
                            }
                            catch (Exception ex)
                            {
                                hasProcessException = true;
                                Console.WriteLine($"WARN:GetDepthFeature()第{i + 1}个缺陷测量失败:{ex.Message}");
                            }
                            finally
                            {
                                hoDefectCurrent.Dispose();
                                hoScaledCurrent.Dispose();
                                hoDefectZCurrent.Dispose();
                                hoPolygonRegion.Dispose();
                                hoTmpCurrent.Dispose();

                                hvRows.Dispose();
                                hvCols.Dispose();
                                hvX.Dispose();
                                hvY.Dispose();
                                hvZ.Dispose();
                                hvDefectloud.Dispose();
                                hvPlane.Dispose();
                                hvPose.Dispose();
                                hvNormal.Dispose();
                                hvPoseMat.Dispose();
                                hvConnCloud.Dispose();
                                hvSeleCloud.Dispose();
                                hvUnionCloud.Dispose();
                                hvPointNum.Dispose();
                                hvSmthCloud.Dispose();
                                hvAffdCloud.Dispose();
                                hvValueZ.Dispose();
                                hvSortedZ.Dispose();
                                hvAvgZ.Dispose();
                                hvMinZ.Dispose();
                                hvMark0.Dispose();
                                hvB0.Dispose();
                                hvTop0.Dispose();
                                hvDiv0.Dispose();
                                hvMark1.Dispose();
                                hvB1.Dispose();
                                hvTop2.Dispose();
                                hvDiv1.Dispose();
                                hvTemp.Dispose();
                                hvLengthes.Dispose();
                                hvIndex.Dispose();
                                hvSize.Dispose();
                                hvLength.Dispose();
                                hvGapDistance.Dispose();
                                hvTmpArea.Dispose();
                                hvFlip.Dispose();
                                hvMaxLengthIndex.Dispose();
                            }
                        }

                        if (HasTupleValue(hvDepthFeature))
                            result.DepthFeature = hvDepthFeature.TupleMax().D * 0.001;

                        if (HasTupleValue(hvAreaFeature))
                            result.AreaFeature = (hvAreaFeature.TupleSum() * hvAccelerationFactor * hvAccelerationFactor).D * 0.001 * 0.001;

                        if (HasTupleValue(hvDepthFeature) && HasTupleValue(hvAreaFeature))
                        {
                            if (result.DepthFeature > depth_select && result.AreaFeature > area_select)
                            {
                                result.IsOk = false;
                            }
                            else
                            {
                                result.IsOk = true;
                            }
                        }
                        else if (HasTupleValue(hvNum) && hvNum.I > 0 && hasProcessException)
                        {
                            result.IsOk = false;
                        }
                        else
                        {
                            result.IsOk = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logs.LogError($"{DateTime.Now}:GetDepthFeature()报错信息:{ex.Message},调用堆栈:{ex.StackTrace}");
                        Console.WriteLine(ex.StackTrace);
                        result.IsOk = false;
                    }
                    finally
                    {
                        if (handle.IsAllocated)
                            handle.Free();

                        hoHeightImage.Dispose();
                        hoValidMask.Dispose();
                        hoValidMaskZoom.Dispose();
                        hoDefect.Dispose();
                        hoBoxRegionMask.Dispose();
                        hoTmp.Dispose();

                        scaleX.Dispose();
                        scaleY.Dispose();
                        hvXp.Dispose();
                        hvZp.Dispose();
                        offsetX.Dispose();
                        offsetY.Dispose();
                        hvAccelerationFactor.Dispose();
                        hvScaleFactorW.Dispose();
                        hvScaleFactorH.Dispose();
                        hvNum.Dispose();
                        hvDepthFeature.Dispose();
                        hvAreaFeature.Dispose();
                        hvaffineMatrix.Dispose();
                        hvthreshold.Dispose();
                    }

                    return result;
                }
            }


            public static DefectResult GetHeightFeature(int bboxId, ImageData imageData, MFDJC0_MeasureParam measureParam,
                                                       double area_select = 0, double height_select = 0)
            {
                using (var dh = new HDevDisposeHelper())
                {
                    DefectResult result = new DefectResult();
                    List<Polygon> polygons = result.DefectPolygons;
                    bool hasProcessException = false;

                    HObject hoHeightImage = new HObject();
                    HObject hoValidMask = new HObject();
                    HObject hoValidMaskZoom = new HObject();
                    HObject hoDefect = new HObject();
                    HObject hoBoxRegionMask = new HObject();
                    HObject hoTmp = new HObject();

                    HOperatorSet.GenEmptyObj(out hoHeightImage);
                    HOperatorSet.GenEmptyObj(out hoValidMask);
                    HOperatorSet.GenEmptyObj(out hoValidMaskZoom);
                    HOperatorSet.GenEmptyObj(out hoDefect);
                    HOperatorSet.GenEmptyObj(out hoBoxRegionMask);
                    HOperatorSet.GenEmptyObj(out hoTmp);

                    HTuple scaleX = new HTuple();
                    HTuple scaleY = new HTuple();
                    HTuple hvXp = new HTuple();
                    HTuple hvZp = new HTuple();
                    HTuple offsetX = new HTuple();
                    HTuple offsetY = new HTuple();
                    HTuple hvAccelerationFactor = new HTuple();
                    HTuple hvScaleFactorW = new HTuple();
                    HTuple hvScaleFactorH = new HTuple();
                    HTuple hvNum = new HTuple();
                    HTuple hvDepthFeature = new HTuple();
                    HTuple hvAreaFeature = new HTuple();
                    HTuple hvaffineMatrix = new HTuple();
                    HTuple hvthreshold = new HTuple();
                    GCHandle handle = default(GCHandle);

                    try
                    {
                        if (imageData == null || measureParam == null)
                        {
                            Console.WriteLine("ERROR:GetHeightFeature: imageData or measureParam is null");
                            result.IsOk = false;
                            return result;
                        }

                        if (imageData.Boxes == null || bboxId < 0 || bboxId >= imageData.Boxes.Count)
                        {
                            Console.WriteLine("ERROR:GetHeightFeature: bboxId out of range");
                            result.IsOk = false;
                            return result;
                        }

                        Box bbox = imageData.Boxes[bboxId];
                        if (bbox == null)
                        {
                            Console.WriteLine("ERROR:GetHeightFeature: bbox is null");
                            result.IsOk = false;
                            return result;
                        }

                        if (imageData.hvIntervalX > imageData.hvIntervalY)
                        {
                            scaleX = imageData.hvIntervalX / imageData.hvIntervalY;
                            scaleY = 1;
                        }
                        else
                        {
                            scaleX = 1;
                            scaleY = imageData.hvIntervalY / imageData.hvIntervalX;
                        }
                        hvXp = (imageData.hvIntervalX * 1.0) / scaleX;
                        hvZp = (imageData.hvIntervalZ * 1.0);

                        offsetX = imageData.OffsetX;
                        offsetY = imageData.OffsetY;

                        FillDefectBaseResult(result, bbox, offsetX, offsetY, scaleX, scaleY);
                        result.CenterColFeature = ((((bbox.Left + bbox.Right) / 2.0) + offsetX) * scaleX).D;
                        result.CenterRowFeature = ((((bbox.Top + bbox.Bottom) / 2.0) + offsetY) * scaleY).D;


                        if (imageData.hoHeightImage == null || !imageData.hoHeightImage.IsInitialized()
                            || imageData.hoValidMask == null || !imageData.hoValidMask.IsInitialized())
                        {
                            Console.WriteLine("ERROR:GetHeightFeature: input image or valid mask is invalid");
                            result.IsOk = false;
                            return result;
                        }

                        hoHeightImage.Dispose();
                        hoHeightImage = imageData.hoHeightImage.Clone();
                        hoValidMask.Dispose();
                        hoValidMask = imageData.hoValidMask.Clone();

                        hvAccelerationFactor = 1.0f;
                        hvScaleFactorW = 1.0f / hvAccelerationFactor;
                        hvScaleFactorH = 1.0f / hvAccelerationFactor;
                        HOperatorSet.ZoomImageFactor(hoHeightImage, out hoTmp, hvScaleFactorW, hvScaleFactorH, "nearest_neighbor");
                        ReplaceHobject(ref hoHeightImage, ref hoTmp);
                        HOperatorSet.ZoomRegion(hoValidMask, out hoTmp, hvScaleFactorW, hvScaleFactorH);
                        ReplaceHobject(ref hoValidMask, ref hoTmp);
                        HOperatorSet.Threshold(hoHeightImage, out hoValidMaskZoom, measureParam.MinDepth, measureParam.MaxDepth);
                        HOperatorSet.Intersection(hoValidMask, hoValidMaskZoom, out hoTmp);
                        ReplaceHobject(ref hoValidMask, ref hoTmp);

                        if (!HasValidSegmentation(bbox))
                        {
                            result.IsOk = false;
                            return result;
                        }

                        hvaffineMatrix = new HTuple(bbox.Seg.AffineMatrix);
                        hvaffineMatrix[0] = bbox.Seg.AffineMatrix[4];
                        hvaffineMatrix[1] = bbox.Seg.AffineMatrix[3];
                        hvaffineMatrix[2] = bbox.Seg.AffineMatrix[5];
                        hvaffineMatrix[3] = bbox.Seg.AffineMatrix[1];
                        hvaffineMatrix[4] = bbox.Seg.AffineMatrix[0];
                        hvaffineMatrix[5] = bbox.Seg.AffineMatrix[2];

                        HOperatorSet.GetImageSize(hoHeightImage, out HTuple hvRestoreWidth, out HTuple hvRestoreHeight);

                        // 深度图像素值转换为微米单位
                        HOperatorSet.ScaleImage(hoHeightImage, out hoTmp, hvZp, 0);
                        ReplaceHobject(ref hoHeightImage, ref hoTmp);

                        hvthreshold = new HTuple(bbox.Seg.Thresh);
                        handle = GCHandle.Alloc(bbox.Seg.Data, GCHandleType.Pinned);
                        IntPtr pointer = handle.AddrOfPinnedObject();
                        HOperatorSet.GenImage1(out hoDefect, "real", bbox.Seg.Width, bbox.Seg.Height, pointer);
                        //HOperatorSet.AffineTransImage(hoDefect, out hoTmp, hvaffineMatrix, "bilinear", "true");
                        HOperatorSet.AffineTransImageSize(hoDefect, out hoTmp, hvaffineMatrix, "bilinear", hvRestoreWidth, hvRestoreHeight);
                        ReplaceHobject(ref hoDefect, ref hoTmp);

                        //Sigmoid(hoDefect, out hoTmp);
                        //ReplaceHobject(ref hoDefect, ref hoTmp);

                        HOperatorSet.Threshold(hoDefect, out hoTmp, hvthreshold, 255);
                        ReplaceHobject(ref hoDefect, ref hoTmp);

                        // 只保留框内的分割掩码
                        HOperatorSet.GenRectangle1(out hoBoxRegionMask, bbox.Top, bbox.Left, bbox.Bottom, bbox.Right);
                        HOperatorSet.Intersection(hoDefect, hoBoxRegionMask, out hoTmp);
                        ReplaceHobject(ref hoDefect, ref hoTmp);

                        HOperatorSet.ZoomRegion(hoDefect, out hoTmp, hvScaleFactorW, hvScaleFactorH);
                        ReplaceHobject(ref hoDefect, ref hoTmp);
                        HOperatorSet.Intersection(hoDefect, hoValidMask, out hoTmp);
                        ReplaceHobject(ref hoDefect, ref hoTmp);
                        HOperatorSet.Connection(hoDefect, out hoTmp);
                        ReplaceHobject(ref hoDefect, ref hoTmp);
                        HOperatorSet.SelectShape(hoDefect, out hoTmp, "area", "and", 99, 9999999999999999999);
                        ReplaceHobject(ref hoDefect, ref hoTmp);

                        HOperatorSet.CountObj(hoDefect, out hvNum);

                        int polygonOffsetCol = (int)(offsetX.D * scaleX.D);
                        int polygonOffsetRow = (int)(offsetY.D * scaleY.D);

                        for (int i = 0; i < hvNum; i++)
                        {
                            HObject hoDefectCurrent = new HObject();
                            HObject hoScaledCurrent = new HObject();
                            HObject hoDefectZCurrent = new HObject();
                            HObject hoPolygonRegion = new HObject();
                            HObject hoTmpCurrent = new HObject();

                            HOperatorSet.GenEmptyObj(out hoDefectCurrent);
                            HOperatorSet.GenEmptyObj(out hoScaledCurrent);
                            HOperatorSet.GenEmptyObj(out hoDefectZCurrent);
                            HOperatorSet.GenEmptyObj(out hoPolygonRegion);
                            HOperatorSet.GenEmptyObj(out hoTmpCurrent);

                            HTuple hvRows = new HTuple();
                            HTuple hvCols = new HTuple();
                            HTuple hvX = new HTuple();
                            HTuple hvY = new HTuple();
                            HTuple hvZ = new HTuple();
                            HTuple hvDefectloud = new HTuple();
                            HTuple hvPlane = new HTuple();
                            HTuple hvPose = new HTuple();
                            HTuple hvNormal = new HTuple();
                            HTuple hvPoseMat = new HTuple();
                            HTuple hvConnCloud = new HTuple();
                            HTuple hvSeleCloud = new HTuple();
                            HTuple hvUnionCloud = new HTuple();
                            HTuple hvPointNum = new HTuple();
                            HTuple hvSmthCloud = new HTuple();
                            HTuple hvAffdCloud = new HTuple();
                            HTuple hvValueZ = new HTuple();
                            HTuple hvSortedZ = new HTuple();
                            HTuple hvAvgZ = new HTuple();
                            HTuple hvMaxZ = new HTuple();
                            HTuple hvMark0 = new HTuple();
                            HTuple hvB0 = new HTuple();
                            HTuple hvTop0 = new HTuple();
                            HTuple hvDiv0 = new HTuple();
                            HTuple hvMark1 = new HTuple();
                            HTuple hvB1 = new HTuple();
                            HTuple hvTop2 = new HTuple();
                            HTuple hvDiv1 = new HTuple();
                            HTuple hvTemp = new HTuple(0);
                            HTuple hvLengthes = new HTuple();
                            HTuple hvIndex = new HTuple();
                            HTuple hvSize = new HTuple();
                            HTuple hvLength = new HTuple(0);
                            HTuple hvGapDistance = new HTuple(0);
                            HTuple hvTmpArea = new HTuple();
                            HTuple hvFlip = new HTuple();
                            HTuple hvMaxLengthIndex = new HTuple();

                            try
                            {
                                HOperatorSet.SelectObj(hoDefect, out hoDefectCurrent, i + 1);
                                HOperatorSet.ZoomRegion(hoDefectCurrent, out hoScaledCurrent, scaleX, scaleY);
                                HOperatorSet.FillUp(hoScaledCurrent, out hoTmpCurrent);
                                ReplaceHobject(ref hoScaledCurrent, ref hoTmpCurrent);

                                HOperatorSet.ZoomRegion(hoScaledCurrent, out hoPolygonRegion, hvAccelerationFactor, hvAccelerationFactor);
                                polygons.Add(new Polygon(hoPolygonRegion, polygonOffsetCol, polygonOffsetRow));

                                HOperatorSet.RegionFeatures(hoScaledCurrent, "area", out hvTmpArea);
                                if (HasTupleValue(hvTmpArea))
                                {
                                    hvTmpArea = hvTmpArea * hvXp * hvXp;
                                    hvAreaFeature = hvAreaFeature.TupleConcat(hvTmpArea);
                                }

                                HOperatorSet.ReduceDomain(hoHeightImage, hoDefectCurrent, out hoDefectZCurrent);
                                HOperatorSet.GetRegionPoints(hoDefectZCurrent, out hvRows, out hvCols);
                                if (!HasTupleValue(hvRows) || !HasTupleValue(hvCols))
                                    continue;

                                hvX = hvCols * imageData.hvIntervalX;
                                hvY = hvRows * imageData.hvIntervalY;
                                HOperatorSet.GetGrayval(hoDefectZCurrent, hvRows, hvCols, out hvZ);
                                if (!HasTupleValue(hvZ))
                                    continue;

                                HOperatorSet.GenObjectModel3dFromPoints(hvX, hvY, hvZ, out hvDefectloud);
                                HOperatorSet.FitPrimitivesObjectModel3d(hvDefectloud, (new HTuple("primitive_type")).TupleConcat("fitting_algorithm"),
                                                                         (new HTuple("plane")).TupleConcat("least_squares"), out hvPlane);
                                HOperatorSet.GetObjectModel3dParams(hvPlane, "primitive_parameter_pose", out hvPose);
                                HOperatorSet.PoseInvert(hvPose, out hvPose);
                                HOperatorSet.GetObjectModel3dParams(hvPlane, "primitive_parameter", out hvNormal);
                                if (HasTupleValue(hvNormal, 3) && hvNormal.TupleSelect(2).D < 0)
                                {
                                    HOperatorSet.CreatePose(0, 0, 0, 180, 0, 0, "Rp+T", "gba", "point", out hvFlip);
                                    HOperatorSet.PoseCompose(hvFlip, hvPose, out hvPose);
                                }
                                HOperatorSet.PoseToHomMat3d(hvPose, out hvPoseMat);
                                HOperatorSet.ConnectionObjectModel3d(hvDefectloud, "distance_3d", 2 * hvXp, out hvConnCloud);
                                HOperatorSet.SelectObjectModel3d(hvConnCloud, "num_points", "and", 9, 9999999999999999999, out hvSeleCloud);
                                HOperatorSet.UnionObjectModel3d(hvSeleCloud, "points_surface", out hvUnionCloud);
                                HOperatorSet.GetObjectModel3dParams(hvUnionCloud, "num_points", out hvPointNum);
                                if (!HasTupleValue(hvPointNum) || hvPointNum.I == 0)
                                    continue;

                                HOperatorSet.SmoothObjectModel3d(hvUnionCloud, "mls", "mls_kNN", 199, out hvSmthCloud);
                                HOperatorSet.AffineTransObjectModel3d(hvSmthCloud, hvPoseMat, out hvAffdCloud);
                                HOperatorSet.GetObjectModel3dParams(hvAffdCloud, "point_coord_z", out hvValueZ);
                                if (!HasTupleValue(hvValueZ))
                                    continue;

                                HOperatorSet.TupleSort(hvValueZ, out hvSortedZ);
                                HOperatorSet.TupleMean(hvSortedZ, out hvAvgZ);
                                HOperatorSet.TupleMax(hvSortedZ, out hvMaxZ);
                                HOperatorSet.TupleGreaterElem(hvSortedZ, hvAvgZ, out hvMark0);
                                HOperatorSet.TupleFindFirst(hvMark0, 1, out hvB0);
                                if (HasTupleValue(hvB0) && hvB0.I != -1)
                                {
                                    HOperatorSet.TupleSelectRange(hvSortedZ, 0, hvB0, out hvTop0);
                                    if (HasTupleValue(hvTop0))
                                    {
                                        HOperatorSet.TupleMean(hvTop0, out hvDiv0);
                                        HOperatorSet.TupleGreaterElem(hvTop0, hvDiv0, out hvMark1);
                                        HOperatorSet.TupleFindFirst(hvMark1, 1, out hvB1);
                                        if (HasTupleValue(hvB1) && hvB1.I != -1)
                                        {
                                            HOperatorSet.TupleSelectRange(hvTop0, 0, hvB1, out hvTop2);
                                            if (HasTupleValue(hvTop2))
                                                HOperatorSet.TupleMean(hvTop2, out hvDiv1);
                                            else
                                                hvDiv1 = hvDiv0.Clone();
                                        }
                                        else
                                        {
                                            hvDiv1 = hvDiv0.Clone();
                                        }
                                        hvTemp = hvMaxZ - hvDiv1;
                                    }
                                }
                                hvDepthFeature = hvDepthFeature.TupleConcat(hvTemp);
                            }
                            catch (Exception ex)
                            {
                                hasProcessException = true;
                                Console.WriteLine($"WARN:GetHeightFeature()第{i + 1}个缺陷测量失败:{ex.Message}");
                            }
                            finally
                            {
                                hoDefectCurrent.Dispose();
                                hoScaledCurrent.Dispose();
                                hoDefectZCurrent.Dispose();
                                hoPolygonRegion.Dispose();
                                hoTmpCurrent.Dispose();

                                hvRows.Dispose();
                                hvCols.Dispose();
                                hvX.Dispose();
                                hvY.Dispose();
                                hvZ.Dispose();
                                hvDefectloud.Dispose();
                                hvPlane.Dispose();
                                hvPose.Dispose();
                                hvNormal.Dispose();
                                hvPoseMat.Dispose();
                                hvConnCloud.Dispose();
                                hvSeleCloud.Dispose();
                                hvUnionCloud.Dispose();
                                hvPointNum.Dispose();
                                hvSmthCloud.Dispose();
                                hvAffdCloud.Dispose();
                                hvValueZ.Dispose();
                                hvSortedZ.Dispose();
                                hvAvgZ.Dispose();
                                hvMaxZ.Dispose();
                                hvMark0.Dispose();
                                hvB0.Dispose();
                                hvTop0.Dispose();
                                hvDiv0.Dispose();
                                hvMark1.Dispose();
                                hvB1.Dispose();
                                hvTop2.Dispose();
                                hvDiv1.Dispose();
                                hvTemp.Dispose();
                                hvLengthes.Dispose();
                                hvIndex.Dispose();
                                hvSize.Dispose();
                                hvLength.Dispose();
                                hvGapDistance.Dispose();
                                hvTmpArea.Dispose();
                                hvFlip.Dispose();
                                hvMaxLengthIndex.Dispose();
                            }
                        }

                        if (HasTupleValue(hvDepthFeature))
                            result.DepthFeature = hvDepthFeature.TupleMax().D * 0.001;

                        if (HasTupleValue(hvAreaFeature))
                            result.AreaFeature = (hvAreaFeature.TupleSum() * hvAccelerationFactor * hvAccelerationFactor).D * 0.001 * 0.001;

                        if (HasTupleValue(hvDepthFeature) && HasTupleValue(hvAreaFeature))
                        {
                            if (result.DepthFeature > height_select && result.AreaFeature > area_select)
                            {
                                result.IsOk = false;
                            }
                            else
                            {
                                result.IsOk = true;
                            }
                        }
                        else if (HasTupleValue(hvNum) && hvNum.I > 0 && hasProcessException)
                        {
                            result.IsOk = false;
                        }
                        else
                        {
                            result.IsOk = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logs.LogError($"{DateTime.Now}:GetHeightFeature()报错信息:{ex.Message},调用堆栈:{ex.StackTrace}");
                        Console.WriteLine(ex.StackTrace);
                        result.IsOk = false;
                    }
                    finally
                    {
                        if (handle.IsAllocated)
                            handle.Free();

                        hoHeightImage.Dispose();
                        hoValidMask.Dispose();
                        hoValidMaskZoom.Dispose();
                        hoDefect.Dispose();
                        hoBoxRegionMask.Dispose();
                        hoTmp.Dispose();

                        scaleX.Dispose();
                        scaleY.Dispose();
                        hvXp.Dispose();
                        hvZp.Dispose();
                        offsetX.Dispose();
                        offsetY.Dispose();
                        hvAccelerationFactor.Dispose();
                        hvScaleFactorW.Dispose();
                        hvScaleFactorH.Dispose();
                        hvNum.Dispose();
                        hvDepthFeature.Dispose();
                        hvAreaFeature.Dispose();
                        hvaffineMatrix.Dispose();
                        hvthreshold.Dispose();
                    }

                    return result;
                }
            }


            public static DefectResult GetDiameterFeature(int bboxId, ImageData imageData, int image_type = 0, double select = 0,
                                                          double min_gray_value = 0, double max_gray_value = 0,
                                                          double area_lower_limit = 0, double area_upper_limit = 0)
            {
                using (var dh = new HDevDisposeHelper())
                {
                    DefectResult result = new DefectResult();
                    List<Polygon> polygons = result.DefectPolygons;

                    HObject hoImage = new HObject();
                    HObject hoDefectMask = new HObject();
                    HObject hoBoxRegionMask = new HObject();
                    HObject hoRect = new HObject();
                    HObject hoReduceImage = new HObject();
                    HObject hoRegion = new HObject();
                    HObject hoRegions = new HObject();
                    HObject hoTmpRegions = new HObject();
                    HObject hoTmpRegion = new HObject();
                    HObject hoTmp = new HObject();

                    HOperatorSet.GenEmptyObj(out hoImage);
                    HOperatorSet.GenEmptyObj(out hoDefectMask);
                    HOperatorSet.GenEmptyObj(out hoBoxRegionMask);
                    HOperatorSet.GenEmptyObj(out hoRect);
                    HOperatorSet.GenEmptyObj(out hoReduceImage);
                    HOperatorSet.GenEmptyObj(out hoRegion);
                    HOperatorSet.GenEmptyObj(out hoRegions);
                    HOperatorSet.GenEmptyObj(out hoTmpRegions);
                    HOperatorSet.GenEmptyObj(out hoTmpRegion);
                    HOperatorSet.GenEmptyObj(out hoTmp);

                    HTuple scaleX = new HTuple();
                    HTuple scaleY = new HTuple();
                    HTuple hvXp = new HTuple();
                    HTuple hvYp = new HTuple();
                    HTuple offsetX = new HTuple();
                    HTuple offsetY = new HTuple();
                    HTuple hvMaskArea = new HTuple();
                    HTuple hvInnerRadius = new HTuple();
                    HTuple hvOuterRadius = new HTuple();
                    HTuple hvArea = new HTuple();
                    HTuple hvOuterRadiusReal = new HTuple(0);
                    HTuple hvInnerRadiusReal = new HTuple(0);
                    HTuple hvAreaReal = new HTuple(0);
                    HTuple hvNum = new HTuple();
                    HTuple hvaffineMatrix = new HTuple();
                    HTuple hvthreshold = new HTuple();
                    GCHandle handle = default(GCHandle);

                    try
                    {
                        if (imageData == null)
                        {
                            Console.WriteLine("ERROR:GetDiameterFeature: imageData is null");
                            result.IsOk = false;
                            return result;
                        }

                        if (imageData.Boxes == null || bboxId < 0 || bboxId >= imageData.Boxes.Count)
                        {
                            Console.WriteLine("ERROR:GetDiameterFeature: bboxId out of range");
                            result.IsOk = false;
                            return result;
                        }

                        Box bbox = imageData.Boxes[bboxId];
                        if (bbox == null)
                        {
                            Console.WriteLine("ERROR:GetDiameterFeature: bbox is null");
                            result.IsOk = false;
                            return result;
                        }

                        if (imageData.hvIntervalX > imageData.hvIntervalY)
                        {
                            scaleX = imageData.hvIntervalX / imageData.hvIntervalY;
                            scaleY = 1;
                        }
                        else
                        {
                            scaleX = 1;
                            scaleY = imageData.hvIntervalY / imageData.hvIntervalX;
                        }
                        hvXp = (imageData.hvIntervalX * 1.0) / scaleX;
                        hvYp = (imageData.hvIntervalY * 1.0) / scaleY;
                        offsetX = imageData.OffsetX;
                        offsetY = imageData.OffsetY;

                        FillDefectBaseResult(result, bbox, offsetX, offsetY, scaleX, scaleY);

                        if (image_type == 0)
                        {
                            if (imageData.hoGrayImage == null || !imageData.hoGrayImage.IsInitialized())
                            {
                                Console.WriteLine("ERROR:GetDiameterFeature: gray image is invalid");
                                result.IsOk = false;
                                return result;
                            }
                            hoImage.Dispose();
                            hoImage = imageData.hoGrayImage.Clone();
                        }
                        else if (image_type == 1)
                        {
                            if (imageData.hoHeightImage == null || !imageData.hoHeightImage.IsInitialized())
                            {
                                Console.WriteLine("ERROR:GetDiameterFeature: height image is invalid");
                                result.IsOk = false;
                                return result;
                            }
                            hoImage.Dispose();
                            hoImage = imageData.hoHeightImage.Clone();
                        }
                        else
                        {
                            Console.WriteLine("ERROR:GetDiameterFeature: image_type error");
                            result.IsOk = false;
                            return result;
                        }

                        bool useFallbackMask = true;
                        if (HasValidSegmentation(bbox))
                        {
                            try
                            {
                                hvaffineMatrix = new HTuple(bbox.Seg.AffineMatrix);
                                hvaffineMatrix[0] = bbox.Seg.AffineMatrix[4];
                                hvaffineMatrix[1] = bbox.Seg.AffineMatrix[3];
                                hvaffineMatrix[2] = bbox.Seg.AffineMatrix[5];
                                hvaffineMatrix[3] = bbox.Seg.AffineMatrix[1];
                                hvaffineMatrix[4] = bbox.Seg.AffineMatrix[0];
                                hvaffineMatrix[5] = bbox.Seg.AffineMatrix[2];

                                HOperatorSet.GetImageSize(hoImage, out HTuple hvRestoreWidth, out HTuple hvRestoreHeight);

                                hvthreshold = new HTuple(bbox.Seg.Thresh);
                                handle = GCHandle.Alloc(bbox.Seg.Data, GCHandleType.Pinned);
                                IntPtr pointer = handle.AddrOfPinnedObject();
                                HOperatorSet.GenImage1(out hoDefectMask, "real", bbox.Seg.Width, bbox.Seg.Height, pointer);
                                //HOperatorSet.AffineTransImage(hoDefectMask, out hoTmp, hvaffineMatrix, "bilinear", "true");
                                HOperatorSet.AffineTransImageSize(hoDefectMask, out hoTmp, hvaffineMatrix, "bilinear", hvRestoreWidth, hvRestoreHeight);
                                ReplaceHobject(ref hoDefectMask, ref hoTmp);

                                //Sigmoid(hoDefectMask, out hoTmp);
                                //ReplaceHobject(ref hoDefectMask, ref hoTmp);

                                HOperatorSet.Threshold(hoDefectMask, out hoTmp, hvthreshold, 255);
                                ReplaceHobject(ref hoDefectMask, ref hoTmp);

                                // 只保留框内的分割掩码
                                HOperatorSet.GenRectangle1(out hoBoxRegionMask, bbox.Top, bbox.Left, bbox.Bottom, bbox.Right);
                                HOperatorSet.Intersection(hoDefectMask, hoBoxRegionMask, out hoTmp);
                                ReplaceHobject(ref hoDefectMask, ref hoTmp);

                                HOperatorSet.RegionFeatures(hoDefectMask, "area", out hvMaskArea);
                                useFallbackMask = !HasTupleValue(hvMaskArea) || hvMaskArea.TupleSum().D <= 0;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"WARN:GetDiameterFeature()分割掩码构建失败:{ex.Message}");
                                useFallbackMask = true;
                                if (handle.IsAllocated)
                                {
                                    handle.Free();
                                    handle = default(GCHandle);
                                }
                            }
                        }

                        if (useFallbackMask)
                        {
                            HOperatorSet.GenRectangle1(out hoRect, bbox.Top, bbox.Left, bbox.Bottom, bbox.Right);
                            HOperatorSet.ReduceDomain(hoImage, hoRect, out hoReduceImage);
                            HOperatorSet.Threshold(hoReduceImage, out hoRegion, min_gray_value, max_gray_value);
                            if (image_type == 1)
                            {
                                HOperatorSet.Threshold(hoReduceImage, out hoTmpRegion, 8888880, 8888880);
                                HOperatorSet.Union2(hoRegion, hoTmpRegion, out hoTmp);
                                ReplaceHobject(ref hoRegion, ref hoTmp);
                            }
                            HOperatorSet.Connection(hoRegion, out hoTmp);
                            ReplaceHobject(ref hoRegions, ref hoTmp);
                            HOperatorSet.SelectShape(hoRegions, out hoTmp, "area", "and", area_lower_limit, area_upper_limit);
                            ReplaceHobject(ref hoRegions, ref hoTmp);
                            HOperatorSet.Union1(hoRegions, out hoTmp);
                            ReplaceHobject(ref hoRegions, ref hoTmp);
                            HOperatorSet.ClosingCircle(hoRegions, out hoTmp, 5.5);
                            ReplaceHobject(ref hoDefectMask, ref hoTmp);
                        }

                        HOperatorSet.ZoomRegion(hoDefectMask, out hoTmp, scaleX, scaleY);
                        ReplaceHobject(ref hoDefectMask, ref hoTmp);
                        HOperatorSet.RegionFeatures(hoDefectMask, "inner_radius", out hvInnerRadius);
                        HOperatorSet.RegionFeatures(hoDefectMask, "outer_radius", out hvOuterRadius);
                        HOperatorSet.RegionFeatures(hoDefectMask, "area", out hvArea);

                        if (HasTupleValue(hvOuterRadius))
                            hvOuterRadiusReal = (hvOuterRadius * 2) * hvXp;
                        if (HasTupleValue(hvInnerRadius))
                            hvInnerRadiusReal = (hvInnerRadius * 2) * hvXp;
                        if (HasTupleValue(hvArea))
                            hvAreaReal = hvArea * hvXp * hvYp;

                        HOperatorSet.Connection(hoDefectMask, out hoTmpRegions);
                        HOperatorSet.CountObj(hoTmpRegions, out hvNum);
                        for (int i = 0; i < hvNum; i++)
                        {
                            HOperatorSet.SelectObj(hoTmpRegions, out hoTmpRegion, i + 1);
                            polygons.Add(new Polygon(hoTmpRegion, (int)(offsetX.D * scaleX.D), (int)(offsetY.D * scaleY.D)));
                        }

                        if (HasTupleValue(hvOuterRadiusReal) && hvOuterRadiusReal.D > 0)
                        {
                            result.LengthFeature = hvOuterRadiusReal.D * 0.001;
                            result.Diameter = hvOuterRadiusReal.D * 0.001;
                        }
                        if (HasTupleValue(hvInnerRadiusReal) && hvInnerRadiusReal.D > 0)
                        {
                            result.WidthFeature = hvInnerRadiusReal.D * 0.001;
                        }
                        if (HasTupleValue(hvAreaReal) && hvAreaReal.D > 0)
                            result.AreaFeature = hvAreaReal.D * 0.001 * 0.001;

                        result.IsOk = !(result.Diameter > select);
                    }
                    catch (Exception ex)
                    {
                        Logs.LogError($"{DateTime.Now}:GetDiameterFeature()报错信息:{ex.Message},调用堆栈:{ex.StackTrace}");
                        Console.WriteLine(ex.StackTrace);
                        result.IsOk = false;
                    }
                    finally
                    {
                        if (handle.IsAllocated)
                            handle.Free();

                        hoImage.Dispose();
                        hoDefectMask.Dispose();
                        hoBoxRegionMask.Dispose();
                        hoRect.Dispose();
                        hoReduceImage.Dispose();
                        hoRegion.Dispose();
                        hoRegions.Dispose();
                        hoTmpRegions.Dispose();
                        hoTmpRegion.Dispose();
                        hoTmp.Dispose();

                        scaleX.Dispose();
                        scaleY.Dispose();
                        hvXp.Dispose();
                        hvYp.Dispose();
                        offsetX.Dispose();
                        offsetY.Dispose();
                        hvMaskArea.Dispose();
                        hvInnerRadius.Dispose();
                        hvOuterRadius.Dispose();
                        hvArea.Dispose();
                        hvOuterRadiusReal.Dispose();
                        hvInnerRadiusReal.Dispose();
                        hvAreaReal.Dispose();
                        hvNum.Dispose();
                        hvaffineMatrix.Dispose();
                        hvthreshold.Dispose();
                    }

                    return result;
                }
            }
            public static DefectResult GetLengthFeature(int bboxId, ImageData imageData, int image_type = 0, double select = 0,
                                                        double min_gray_value = 0, double max_gray_value = 0)
            {
                using (var dh = new HDevDisposeHelper())
                {
                    DefectResult result = new DefectResult();
                    List<Polygon> polygons = result.DefectPolygons;

                    HObject hoImage = new HObject();
                    HObject hoDefectMask = new HObject();
                    HObject hoBoxRegionMask = new HObject();
                    HObject hoRect = new HObject();
                    HObject hoReduceImage = new HObject();
                    HObject hoRegion = new HObject();
                    HObject hoRegions = new HObject();
                    HObject hoTmpRegion = new HObject();
                    HObject hoTmp = new HObject();
                    HObject hoObject1 = new HObject();

                    HOperatorSet.GenEmptyObj(out hoImage);
                    HOperatorSet.GenEmptyObj(out hoDefectMask);
                    HOperatorSet.GenEmptyObj(out hoBoxRegionMask);
                    HOperatorSet.GenEmptyObj(out hoRect);
                    HOperatorSet.GenEmptyObj(out hoReduceImage);
                    HOperatorSet.GenEmptyObj(out hoRegion);
                    HOperatorSet.GenEmptyObj(out hoRegions);
                    HOperatorSet.GenEmptyObj(out hoTmpRegion);
                    HOperatorSet.GenEmptyObj(out hoTmp);
                    HOperatorSet.GenEmptyObj(out hoObject1);

                    HTuple scaleX = new HTuple();
                    HTuple scaleY = new HTuple();
                    HTuple hvXp = new HTuple();
                    HTuple hvYp = new HTuple();
                    HTuple offsetX = new HTuple();
                    HTuple offsetY = new HTuple();
                    HTuple hvMaskArea = new HTuple();
                    HTuple hvAreas = new HTuple();
                    HTuple hvAreaIndices = new HTuple();
                    HTuple hvAN = new HTuple();
                    HTuple hvLength = new HTuple(0);
                    HTuple hvLengthReal = new HTuple(0);
                    HTuple hvAreaReal = new HTuple(0);
                    HTuple hvNum = new HTuple();
                    HTuple hvaffineMatrix = new HTuple();
                    HTuple hvthreshold = new HTuple();
                    GCHandle handle = default(GCHandle);

                    try
                    {
                        if (imageData == null)
                        {
                            Console.WriteLine("ERROR:GetLengthFeature: imageData is null");
                            result.IsOk = false;
                            return result;
                        }

                        if (imageData.Boxes == null || bboxId < 0 || bboxId >= imageData.Boxes.Count)
                        {
                            Console.WriteLine("ERROR:GetLengthFeature: bboxId out of range");
                            result.IsOk = false;
                            return result;
                        }

                        Box bbox = imageData.Boxes[bboxId];
                        if (bbox == null)
                        {
                            Console.WriteLine("ERROR:GetLengthFeature: bbox is null");
                            result.IsOk = false;
                            return result;
                        }

                        if (imageData.hvIntervalX > imageData.hvIntervalY)
                        {
                            scaleX = imageData.hvIntervalX / imageData.hvIntervalY;
                            scaleY = 1;
                        }
                        else
                        {
                            scaleX = 1;
                            scaleY = imageData.hvIntervalY / imageData.hvIntervalX;
                        }
                        hvXp = (imageData.hvIntervalX * 1.0) / scaleX;
                        hvYp = (imageData.hvIntervalY * 1.0) / scaleY;
                        offsetX = imageData.OffsetX;
                        offsetY = imageData.OffsetY;

                        FillDefectBaseResult(result, bbox, offsetX, offsetY, scaleX, scaleY);

                        if (image_type == 0)
                        {
                            if (imageData.hoGrayImage == null || !imageData.hoGrayImage.IsInitialized())
                            {
                                Console.WriteLine("ERROR:GetLengthFeature: gray image is invalid");
                                result.IsOk = false;
                                return result;
                            }
                            hoImage.Dispose();
                            hoImage = imageData.hoGrayImage.Clone();
                        }
                        else if (image_type == 1)
                        {
                            if (imageData.hoHeightImage == null || !imageData.hoHeightImage.IsInitialized())
                            {
                                Console.WriteLine("ERROR:GetLengthFeature: height image is invalid");
                                result.IsOk = false;
                                return result;
                            }
                            hoImage.Dispose();
                            hoImage = imageData.hoHeightImage.Clone();
                        }
                        else
                        {
                            Console.WriteLine("ERROR:GetLengthFeature: image_type error");
                            result.IsOk = false;
                            return result;
                        }

                        HOperatorSet.GetImageSize(hoImage, out HTuple hvRestoreWidth, out HTuple hvRestoreHeight);

                        bool useFallbackMask = true;
                        if (HasValidSegmentation(bbox))
                        {
                            try
                            {
                                hvaffineMatrix = new HTuple(bbox.Seg.AffineMatrix);
                                hvaffineMatrix[0] = bbox.Seg.AffineMatrix[4];
                                hvaffineMatrix[1] = bbox.Seg.AffineMatrix[3];
                                hvaffineMatrix[2] = bbox.Seg.AffineMatrix[5];
                                hvaffineMatrix[3] = bbox.Seg.AffineMatrix[1];
                                hvaffineMatrix[4] = bbox.Seg.AffineMatrix[0];
                                hvaffineMatrix[5] = bbox.Seg.AffineMatrix[2];

                                hvthreshold = new HTuple(bbox.Seg.Thresh);
                                handle = GCHandle.Alloc(bbox.Seg.Data, GCHandleType.Pinned);
                                IntPtr pointer = handle.AddrOfPinnedObject();
                                HOperatorSet.GenImage1(out hoDefectMask, "real", bbox.Seg.Width, bbox.Seg.Height, pointer);
                                //HOperatorSet.AffineTransImage(hoDefectMask, out hoTmp, hvaffineMatrix, "bilinear", "true");
                                HOperatorSet.AffineTransImageSize(hoDefectMask, out hoTmp, hvaffineMatrix, "bilinear", hvRestoreWidth, hvRestoreHeight);
                                ReplaceHobject(ref hoDefectMask, ref hoTmp);

                                //Sigmoid(hoDefectMask, out hoTmp);
                                //ReplaceHobject(ref hoDefectMask, ref hoTmp);

                                HOperatorSet.Threshold(hoDefectMask, out hoTmp, hvthreshold, 255);
                                ReplaceHobject(ref hoDefectMask, ref hoTmp);

                                // 只保留框内的分割掩码
                                HOperatorSet.GenRectangle1(out hoBoxRegionMask, bbox.Top, bbox.Left, bbox.Bottom, bbox.Right);
                                HOperatorSet.Intersection(hoDefectMask, hoBoxRegionMask, out hoTmp);
                                ReplaceHobject(ref hoDefectMask, ref hoTmp);

                                HOperatorSet.RegionFeatures(hoDefectMask, "area", out hvMaskArea);
                                useFallbackMask = !HasTupleValue(hvMaskArea) || hvMaskArea.TupleSum().D <= 0;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"WARN:GetLengthFeature()分割掩码构建失败:{ex.Message}");
                                useFallbackMask = true;
                                if (handle.IsAllocated)
                                {
                                    handle.Free();
                                    handle = default(GCHandle);
                                }
                            }
                        }

                        if (useFallbackMask)
                        {
                            HOperatorSet.GenRectangle1(out hoRect, bbox.Top, bbox.Left, bbox.Bottom, bbox.Right);
                            HOperatorSet.ReduceDomain(hoImage, hoRect, out hoReduceImage);
                            HOperatorSet.Threshold(hoReduceImage, out hoRegion, min_gray_value, max_gray_value);
                            HOperatorSet.ClosingCircle(hoRegion, out hoTmp, 7.5);
                            ReplaceHobject(ref hoRegion, ref hoTmp);
                            HOperatorSet.OpeningCircle(hoRegion, out hoTmp, 7.5);
                            ReplaceHobject(ref hoDefectMask, ref hoTmp);
                        }

                        HOperatorSet.Connection(hoDefectMask, out hoTmp);
                        ReplaceHobject(ref hoDefectMask, ref hoTmp);
                        HOperatorSet.SelectShape(hoDefectMask, out hoTmp, "area", "and", 50, 1e20);
                        ReplaceHobject(ref hoDefectMask, ref hoTmp);

                        HOperatorSet.RegionFeatures(hoDefectMask, "area", out hvAreas);
                        HOperatorSet.ZoomRegion(hoDefectMask, out hoTmp, scaleX, scaleY);
                        ReplaceHobject(ref hoDefectMask, ref hoTmp);

                        HOperatorSet.TupleSortIndex(hvAreas, out hvAreaIndices);
                        HOperatorSet.TupleLength(hvAreaIndices, out hvAN);

                        bool hasFirstTrack = false;
                        if (HasTupleValue(hvAN) && hvAN.I >= 1)
                        {
                            HObject hoSkeleton1 = new HObject();
                            HObject hoRoughlySkeleton1 = new HObject();
                            HObject hoDistanceImage1 = new HObject();
                            HObject hoContours1 = new HObject();
                            HObject hoLines1 = new HObject();
                            HObject hoLine1 = new HObject();
                            HOperatorSet.GenEmptyObj(out hoSkeleton1);
                            HOperatorSet.GenEmptyObj(out hoRoughlySkeleton1);
                            HOperatorSet.GenEmptyObj(out hoDistanceImage1);
                            HOperatorSet.GenEmptyObj(out hoContours1);
                            HOperatorSet.GenEmptyObj(out hoLines1);
                            HOperatorSet.GenEmptyObj(out hoLine1);

                            HTuple hvRowsSkel1 = new HTuple();
                            HTuple hvColsSkel1 = new HTuple();
                            HTuple hvDistVals1 = new HTuple();
                            HTuple hvWidthVals1 = new HTuple();
                            HTuple hvWidthValsSorted1 = new HTuple();
                            HTuple hvWidthValsValid1 = new HTuple();
                            HTuple hvWidthTypical1 = new HTuple();
                            HTuple hvRadiusClose1 = new HTuple();
                            HTuple hvRadiusOpen1 = new HTuple();

                            HTuple hvLengthes1 = new HTuple();
                            HTuple hvLenIndices1 = new HTuple();
                            HTuple hvLN1 = new HTuple();
                            HTuple hvMaxLengthIndex1 = new HTuple();

                            try
                            {
                                HOperatorSet.SelectObj(hoDefectMask, out hoObject1, hvAreaIndices.TupleSelect(hvAN.I - 1) + 1);
                                hasFirstTrack = true;

                                //填孔，避免区域内部小黑洞影响骨架
                                HOperatorSet.FillUp(hoObject1, out hoTmp);
                                ReplaceHobject(ref hoObject1, ref hoTmp);
                                // A. 轻度预处理
                                //HOperatorSet.ClosingCircle(hoObject1, out hoTmp, 1.5);
                                //ReplaceHobject(ref hoObject1, ref hoTmp);
                                //HOperatorSet.OpeningCircle(hoObject1, out hoTmp, 1.0);
                                //ReplaceHobject(ref hoObject1, ref hoTmp);
                                // B.粗骨架
                                HOperatorSet.Skeleton(hoObject1, out hoRoughlySkeleton1);
                                // C.距离变换
                                HOperatorSet.DistanceTransform(hoObject1, out hoDistanceImage1, "euclidean", "true", hvRestoreWidth * scaleX, hvRestoreHeight * scaleY);
                                // D. 骨架点采样
                                HOperatorSet.GetRegionPoints(hoRoughlySkeleton1, out hvRowsSkel1, out hvColsSkel1);
                                HOperatorSet.GetGrayval(hoDistanceImage1, hvRowsSkel1.TupleInt(), hvColsSkel1.TupleInt(), out hvDistVals1);
                                hvWidthVals1 = 2.0 * hvDistVals1;
                                // E. 去掉太小的异常值
                                HOperatorSet.TupleSort(hvWidthVals1, out hvWidthValsSorted1);
                                HOperatorSet.TupleMedian(hvWidthVals1, out HTuple hvWidthMed1);
                                HTuple hvThresh1 = 0.5 * hvWidthMed1;
                                
                                for(int i=0; i < hvWidthVals1.Length; i++)
                                {
                                    if (hvWidthVals1[i] >= hvThresh1.D)
                                    {
                                        hvWidthValsValid1 = hvWidthValsValid1.TupleConcat(hvWidthVals1[i]);
                                    }
                                }
                                // F.计算典型宽度
                                if(hvWidthValsValid1.Length > 0)
                                {
                                    HOperatorSet.TupleMedian(hvWidthValsValid1, out hvWidthTypical1);
                                }
                                else
                                {
                                    hvWidthTypical1 = hvWidthMed1;
                                }
                                // G. 自动生成形态学参数（0.2与0.12为主要调参项）
                                HOperatorSet.TupleMax2(1, 0.20 * hvWidthTypical1, out hvRadiusClose1);
                                HOperatorSet.TupleMax2(0.5, 0.12 * hvWidthTypical1, out hvRadiusOpen1);
                                // H. 形态学平滑
                                HOperatorSet.ClosingCircle(hoObject1, out hoTmp, hvRadiusClose1);
                                ReplaceHobject(ref hoObject1, ref hoTmp);
                                HOperatorSet.OpeningCircle(hoObject1, out hoTmp, hvRadiusOpen1);
                                ReplaceHobject(ref hoObject1, ref hoTmp);


                                HOperatorSet.Skeleton(hoObject1, out hoSkeleton1);
                                HOperatorSet.GenContoursSkeletonXld(hoSkeleton1, out hoContours1, 1, "filter");
                                HOperatorSet.UnionAdjacentContoursXld(hoContours1, out hoLines1, 10, 1, "attr_keep");
                                HOperatorSet.LengthXld(hoLines1, out hvLengthes1);
                                if (HasTupleValue(hvLengthes1))
                                {
                                    HOperatorSet.TupleSortIndex(hvLengthes1, out hvLenIndices1);
                                    HOperatorSet.TupleLength(hvLenIndices1, out hvLN1);
                                    if (HasTupleValue(hvLN1) && hvLN1.I > 0)
                                    {
                                        hvMaxLengthIndex1 = hvLenIndices1.TupleSelect(hvLN1.I - 1);
                                        HOperatorSet.SelectObj(hoLines1, out hoLine1, hvMaxLengthIndex1 + 1);
                                        hvLength = hvLengthes1.TupleSelect(hvMaxLengthIndex1);
                                    }
                                }
                            }
                            catch(Exception ex)
                            {
                                Logs.LogError($"{DateTime.Now}:GetLengthFeature()报错信息:{ex.Message},调用堆栈:{ex.StackTrace}");
                                Console.WriteLine(ex.StackTrace);
                            }
                            finally
                            {
                                hoSkeleton1.Dispose();
                                hoRoughlySkeleton1.Dispose();
                                hoDistanceImage1.Dispose();
                                hoContours1.Dispose();
                                hoLines1.Dispose();
                                hoLine1.Dispose();

                                hvRowsSkel1.Dispose();
                                hvColsSkel1.Dispose();
                                hvDistVals1.Dispose();
                                hvWidthVals1.Dispose();
                                hvWidthValsSorted1.Dispose();
                                hvWidthTypical1.Dispose();
                                hvRadiusClose1.Dispose();
                                hvRadiusOpen1.Dispose();

                                hvLengthes1.Dispose();
                                hvLenIndices1.Dispose();
                                hvLN1.Dispose();
                                hvMaxLengthIndex1.Dispose();
                            }
                        }

                        if (HasTupleValue(hvAN) && hvAN.I >= 2 && hasFirstTrack)
                        {
                            HObject hoObject2 = new HObject();
                            HObject hoSkeleton2 = new HObject();
                            HObject hoRoughlySkeleton2 = new HObject();
                            HObject hoDistanceImage2 = new HObject();
                            HObject hoContours2 = new HObject();
                            HObject hoLines2 = new HObject();
                            HObject hoLine2 = new HObject();
                            HOperatorSet.GenEmptyObj(out hoObject2);
                            HOperatorSet.GenEmptyObj(out hoSkeleton2);
                            HOperatorSet.GenEmptyObj(out hoRoughlySkeleton2);
                            HOperatorSet.GenEmptyObj(out hoDistanceImage2);
                            HOperatorSet.GenEmptyObj(out hoContours2);
                            HOperatorSet.GenEmptyObj(out hoLines2);
                            HOperatorSet.GenEmptyObj(out hoLine2);

                            HTuple hvRowsSkel2 = new HTuple();
                            HTuple hvColsSkel2 = new HTuple();
                            HTuple hvDistVals2 = new HTuple();
                            HTuple hvWidthVals2 = new HTuple();
                            HTuple hvWidthValsSorted2 = new HTuple();
                            HTuple hvWidthValsValid2 = new HTuple();
                            HTuple hvWidthTypical2 = new HTuple();
                            HTuple hvRadiusClose2 = new HTuple();
                            HTuple hvRadiusOpen2 = new HTuple();

                            HTuple hvLengthes2 = new HTuple();
                            HTuple hvLenIndices2 = new HTuple();
                            HTuple hvLN2 = new HTuple();
                            HTuple hvMaxLengthIndex2 = new HTuple();
                            HTuple hvDistance = new HTuple(0);

                            try
                            {
                                HOperatorSet.SelectObj(hoDefectMask, out hoObject2, hvAreaIndices.TupleSelect(hvAN.I - 2) + 1);

                                //填孔，避免区域内部小黑洞影响骨架
                                HOperatorSet.FillUp(hoObject2, out hoTmp);
                                ReplaceHobject(ref hoObject2, ref hoTmp);
                                // A. 轻度预处理
                                //HOperatorSet.ClosingCircle(hoObject2, out hoTmp, 1.5);
                                //ReplaceHobject(ref hoObject2, ref hoTmp);
                                //HOperatorSet.OpeningCircle(hoObject2, out hoTmp, 1.0);
                                //ReplaceHobject(ref hoObject2, ref hoTmp);
                                // B.粗骨架
                                HOperatorSet.Skeleton(hoObject2, out hoRoughlySkeleton2);
                                // C.距离变换
                                HOperatorSet.DistanceTransform(hoObject2, out hoDistanceImage2, "euclidean", "true", hvRestoreWidth * scaleX, hvRestoreHeight * scaleY);
                                // D. 骨架点采样
                                HOperatorSet.GetRegionPoints(hoRoughlySkeleton2, out hvRowsSkel2, out hvColsSkel2);
                                HOperatorSet.GetGrayval(hoDistanceImage2, hvRowsSkel2.TupleInt(), hvColsSkel2.TupleInt(), out hvDistVals2);
                                hvWidthVals2 = 2.0 * hvDistVals2;
                                // E. 去掉太小的异常值
                                HOperatorSet.TupleSort(hvWidthVals2, out hvWidthValsSorted2);
                                HOperatorSet.TupleMedian(hvWidthVals2, out HTuple hvWidthMed2);
                                HTuple hvThresh2 = 0.5 * hvWidthMed2;

                                for (int i = 0; i < hvWidthVals2.Length; i++)
                                {
                                    if (hvWidthVals2[i] >= hvThresh2.D)
                                    {
                                        hvWidthValsValid2 = hvWidthValsValid2.TupleConcat(hvWidthVals2[i]);
                                    }
                                }
                                // F.计算典型宽度
                                if (hvWidthValsValid2.Length > 0)
                                {
                                    HOperatorSet.TupleMedian(hvWidthValsValid2, out hvWidthTypical2);
                                }
                                else
                                {
                                    hvWidthTypical2 = hvWidthMed2;
                                }
                                // G. 自动生成形态学参数（0.2与0.12为主要调参项）
                                HOperatorSet.TupleMax2(1, 0.20 * hvWidthTypical2, out hvRadiusClose2);
                                HOperatorSet.TupleMax2(0.5, 0.12 * hvWidthTypical2, out hvRadiusOpen2);
                                // H. 形态学平滑
                                HOperatorSet.ClosingCircle(hoObject2, out hoTmp, hvRadiusClose2);
                                ReplaceHobject(ref hoObject2, ref hoTmp);
                                HOperatorSet.OpeningCircle(hoObject2, out hoTmp, hvRadiusOpen2);
                                ReplaceHobject(ref hoObject2, ref hoTmp);


                                HOperatorSet.Skeleton(hoObject2, out hoSkeleton2);
                                HOperatorSet.GenContoursSkeletonXld(hoSkeleton2, out hoContours2, 1, "filter");
                                HOperatorSet.UnionAdjacentContoursXld(hoContours2, out hoLines2, 10, 1, "attr_keep");
                                HOperatorSet.LengthXld(hoLines2, out hvLengthes2);
                                if (HasTupleValue(hvLengthes2))
                                {
                                    HOperatorSet.TupleSortIndex(hvLengthes2, out hvLenIndices2);
                                    HOperatorSet.TupleLength(hvLenIndices2, out hvLN2);
                                    if (HasTupleValue(hvLN2) && hvLN2.I > 0)
                                    {
                                        hvMaxLengthIndex2 = hvLenIndices2.TupleSelect(hvLN2.I - 1);
                                        HOperatorSet.SelectObj(hoLines2, out hoLine2, hvMaxLengthIndex2 + 1);
                                        HOperatorSet.DistanceRrMinDil(hoObject1, hoObject2, out hvDistance);
                                        hvLength = hvLength + hvLengthes2.TupleSelect(hvMaxLengthIndex2) + hvDistance;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Logs.LogError($"{DateTime.Now}:GetLengthFeature()报错信息:{ex.Message},调用堆栈:{ex.StackTrace}");
                                Console.WriteLine(ex.StackTrace);
                            }
                            finally
                            {
                                hoObject2.Dispose();
                                hoSkeleton2.Dispose();
                                hoRoughlySkeleton2.Dispose();
                                hoDistanceImage2.Dispose();
                                hoContours2.Dispose();
                                hoLines2.Dispose();
                                hoLine2.Dispose();

                                hvRowsSkel2.Dispose();
                                hvColsSkel2.Dispose();
                                hvDistVals2.Dispose();
                                hvWidthVals2.Dispose();
                                hvWidthValsSorted2.Dispose();
                                hvWidthTypical2.Dispose();
                                hvRadiusClose2.Dispose();
                                hvRadiusOpen2.Dispose();

                                hvLengthes2.Dispose();
                                hvLenIndices2.Dispose();
                                hvLN2.Dispose();
                                hvMaxLengthIndex2.Dispose();
                                hvDistance.Dispose();
                            }
                        }

                        hvLengthReal = hvLength * hvXp;
                        if (HasTupleValue(hvAreas))
                            hvAreaReal = hvAreas.TupleSum() * hvXp * hvYp;

                        HOperatorSet.CountObj(hoDefectMask, out hvNum);
                        for (int i = 0; i < hvNum; i++)
                        {
                            HOperatorSet.SelectObj(hoDefectMask, out hoTmpRegion, i + 1);
                            polygons.Add(new Polygon(hoTmpRegion, (int)(offsetX.D * scaleX.D), (int)(offsetY.D * scaleY.D)));
                        }

                        if (HasTupleValue(hvLengthReal) && hvLengthReal.D > 0)
                            result.LengthFeature = hvLengthReal.D * 0.001;
                        if (HasTupleValue(hvAreaReal) && hvAreaReal.D > 0)
                            result.AreaFeature = hvAreaReal.D * 0.001 * 0.001;

                        result.IsOk = !(result.LengthFeature > select);
                    }
                    catch (Exception ex)
                    {
                        Logs.LogError($"{DateTime.Now}:GetLengthFeature()报错信息:{ex.Message},调用堆栈:{ex.StackTrace}");
                        Console.WriteLine(ex.StackTrace);
                        result.IsOk = false;
                    }
                    finally
                    {
                        if (handle.IsAllocated)
                            handle.Free();

                        hoImage.Dispose();
                        hoDefectMask.Dispose();
                        hoBoxRegionMask.Dispose();
                        hoRect.Dispose();
                        hoReduceImage.Dispose();
                        hoRegion.Dispose();
                        hoRegions.Dispose();
                        hoTmpRegion.Dispose();
                        hoTmp.Dispose();
                        hoObject1.Dispose();

                        scaleX.Dispose();
                        scaleY.Dispose();
                        hvXp.Dispose();
                        hvYp.Dispose();
                        offsetX.Dispose();
                        offsetY.Dispose();
                        hvMaskArea.Dispose();
                        hvAreas.Dispose();
                        hvAreaIndices.Dispose();
                        hvAN.Dispose();
                        hvLength.Dispose();
                        hvLengthReal.Dispose();
                        hvAreaReal.Dispose();
                        hvNum.Dispose();
                        hvaffineMatrix.Dispose();
                        hvthreshold.Dispose();
                    }

                    return result;
                }
            }

            public static DefectResult GetMoltenBeadFeature_Sunwoda(int bboxId, ImageData imageData, MFDJC0_MeasureParam measureParam, HObject hoNailBaseMask,
                                                                    HObject hoOrbitMask, double area_select = 0, double height_select = 0)
            {
                using (var dh = new HDevDisposeHelper())
                {
                    DefectResult result = new DefectResult();
                    List<Polygon> polygons = result.DefectPolygons;
                    bool hasProcessException = false;

                    HObject hoHeightImage = new HObject();
                    HObject hoValidMask = new HObject();
                    HObject hoValidMaskZoom = new HObject();
                    HObject hoNailBaseMaskMoved = new HObject();
                    HObject hoOrbitMaskMoved = new HObject();
                    HObject hoDefect = new HObject();
                    HObject hoBoxRegionMask = new HObject();
                    HObject hoDefectOnOrbit = new HObject();
                    HObject hoNailBaseSurface = new HObject();
                    HObject hoNailBaseResidual = new HObject();
                    HObject hoTmp = new HObject();

                    HOperatorSet.GenEmptyObj(out hoHeightImage);
                    HOperatorSet.GenEmptyObj(out hoValidMask);
                    HOperatorSet.GenEmptyObj(out hoValidMaskZoom);
                    HOperatorSet.GenEmptyObj(out hoNailBaseMaskMoved);
                    HOperatorSet.GenEmptyObj(out hoOrbitMaskMoved);
                    HOperatorSet.GenEmptyObj(out hoDefect);
                    HOperatorSet.GenEmptyObj(out hoBoxRegionMask);
                    HOperatorSet.GenEmptyObj(out hoDefectOnOrbit);
                    HOperatorSet.GenEmptyObj(out hoNailBaseSurface);
                    HOperatorSet.GenEmptyObj(out hoNailBaseResidual);
                    HOperatorSet.GenEmptyObj(out hoTmp);

                    HTuple scaleX = new HTuple();
                    HTuple scaleY = new HTuple();
                    HTuple hvXp = new HTuple();
                    HTuple hvZp = new HTuple();
                    HTuple offsetX = new HTuple();
                    HTuple offsetY = new HTuple();
                    HTuple hvAccelerationFactor = new HTuple();
                    HTuple hvScaleFactorW = new HTuple();
                    HTuple hvScaleFactorH = new HTuple();
                    HTuple hvNum = new HTuple();
                    HTuple hvDepthFeature = new HTuple();
                    HTuple hvAreaFeature = new HTuple();
                    HTuple hvaffineMatrix = new HTuple();
                    HTuple hvthreshold = new HTuple();
                    GCHandle handle = default(GCHandle);

                    try
                    {
                        if (imageData == null || measureParam == null)
                        {
                            Console.WriteLine("ERROR:GetMoltenBeadFeature_Sunwoda: imageData or measureParam is null");
                            result.IsOk = false;
                            return result;
                        }

                        if (imageData.Boxes == null || bboxId < 0 || bboxId >= imageData.Boxes.Count)
                        {
                            Console.WriteLine("ERROR:GetMoltenBeadFeature_Sunwoda: bboxId out of range");
                            result.IsOk = false;
                            return result;
                        }

                        Box bbox = imageData.Boxes[bboxId];
                        if (bbox == null)
                        {
                            Console.WriteLine("ERROR:GetMoltenBeadFeature_Sunwoda: bbox is null");
                            result.IsOk = false;
                            return result;
                        }

                        if (imageData.hvIntervalX > imageData.hvIntervalY)
                        {
                            scaleX = imageData.hvIntervalX / imageData.hvIntervalY;
                            scaleY = 1;
                        }
                        else
                        {
                            scaleX = 1;
                            scaleY = imageData.hvIntervalY / imageData.hvIntervalX;
                        }
                        hvXp = (imageData.hvIntervalX * 1.0) / scaleX;
                        hvZp = (imageData.hvIntervalZ * 1.0);

                        offsetX = imageData.OffsetX;
                        offsetY = imageData.OffsetY;

                        FillDefectBaseResult(result, bbox, offsetX, offsetY, scaleX, scaleY);
                        result.CenterColFeature = ((((bbox.Left + bbox.Right) / 2.0) + offsetX) * scaleX).D;
                        result.CenterRowFeature = ((((bbox.Top + bbox.Bottom) / 2.0) + offsetY) * scaleY).D;



                        if (hoNailBaseMask == null || !hoNailBaseMask.IsInitialized())
                        {
                            Console.WriteLine("ERROR:GetMoltenBeadFeature_Sunwoda: hoNailBaseMask is invalid");
                            result.IsOk = false;
                            return result;
                        }
                        if (hoOrbitMask == null || !hoOrbitMask.IsInitialized())
                        {
                            Console.WriteLine("ERROR:GetMoltenBeadFeature_Sunwoda: hoOrbitMask is invalid");
                            result.IsOk = false;
                            return result;
                        }

                        if (imageData.hoHeightImage == null || !imageData.hoHeightImage.IsInitialized()
                            || imageData.hoValidMask == null || !imageData.hoValidMask.IsInitialized())
                        {
                            Console.WriteLine("ERROR:GetMoltenBeadFeature_Sunwoda: input image or valid mask is invalid");
                            result.IsOk = false;
                            return result;
                        }

                        hoHeightImage.Dispose();
                        hoHeightImage = imageData.hoHeightImage.Clone();
                        hoValidMask.Dispose();
                        hoValidMask = imageData.hoValidMask.Clone();

                        HOperatorSet.ZoomRegion(hoNailBaseMask, out hoNailBaseMaskMoved, 1 / scaleX, 1 / scaleY);
                        HOperatorSet.MoveRegion(hoNailBaseMaskMoved, out hoTmp, -offsetY, -offsetX);
                        ReplaceHobject(ref hoNailBaseMaskMoved, ref hoTmp);

                        HOperatorSet.MoveRegion(hoOrbitMask, out hoOrbitMaskMoved, -offsetY, -offsetX);

                        hvAccelerationFactor = 1.0f;
                        hvScaleFactorW = 1.0f / hvAccelerationFactor;
                        hvScaleFactorH = 1.0f / hvAccelerationFactor;
                        HOperatorSet.ZoomImageFactor(hoHeightImage, out hoTmp, hvScaleFactorW, hvScaleFactorH, "nearest_neighbor");
                        ReplaceHobject(ref hoHeightImage, ref hoTmp);
                        HOperatorSet.ZoomRegion(hoValidMask, out hoTmp, hvScaleFactorW, hvScaleFactorH);
                        ReplaceHobject(ref hoValidMask, ref hoTmp);
                        HOperatorSet.ZoomRegion(hoNailBaseMaskMoved, out hoTmp, hvScaleFactorW, hvScaleFactorH);
                        ReplaceHobject(ref hoNailBaseMaskMoved, ref hoTmp);
                        HOperatorSet.ZoomRegion(hoOrbitMaskMoved, out hoTmp, hvScaleFactorW, hvScaleFactorH);
                        ReplaceHobject(ref hoOrbitMaskMoved, ref hoTmp);

                        HOperatorSet.Threshold(hoHeightImage, out hoValidMaskZoom, measureParam.MinDepth, measureParam.MaxDepth);
                        HOperatorSet.Intersection(hoValidMask, hoValidMaskZoom, out hoTmp);
                        ReplaceHobject(ref hoValidMask, ref hoTmp);
                        HOperatorSet.Intersection(hoNailBaseMaskMoved, hoValidMaskZoom, out hoTmp);
                        ReplaceHobject(ref hoNailBaseMaskMoved, ref hoTmp);
                        HOperatorSet.Intersection(hoOrbitMaskMoved, hoValidMaskZoom, out hoTmp);
                        ReplaceHobject(ref hoOrbitMaskMoved, ref hoTmp);

                        HOperatorSet.GetImageSize(hoHeightImage, out HTuple hvRestoreWidth, out HTuple hvRestoreHeight);

                        
                        // 深度图像素值转换为微米单位
                        HOperatorSet.ScaleImage(hoHeightImage, out hoTmp, imageData.hvIntervalZ, 0);
                        ReplaceHobject(ref hoHeightImage, ref hoTmp);

                        // 以hoNailWarpBaseMask为基准面校正hoHeightImage
                        HOperatorSet.AreaCenter(hoNailBaseMaskMoved, out HTuple hvBaseMaskArea, out HTuple hvBaseMaskRow, out HTuple hvBaseMaskCol);
                        HOperatorSet.FitSurfaceFirstOrder(hoNailBaseMaskMoved, hoHeightImage, "tukey", 5, 2, 
                                                          out HTuple hvAlpha, out HTuple hvBeta, out HTuple hvGamma);
                        HOperatorSet.GenImageSurfaceFirstOrder(out hoNailBaseSurface, "real", hvAlpha, hvBeta, hvGamma, 
                                                               hvBaseMaskRow, hvBaseMaskCol, hvRestoreWidth, hvRestoreHeight);
                        HOperatorSet.SubImage(hoHeightImage, hoNailBaseSurface, out hoNailBaseResidual, 1, 0);
                        HOperatorSet.ReduceDomain(hoNailBaseResidual, hoValidMask, out hoTmp);
                        ReplaceHobject(ref hoNailBaseResidual, ref hoTmp);

                        if (!HasValidSegmentation(bbox))
                        {
                            result.IsOk = false;
                            return result;
                        }

                        hvaffineMatrix = new HTuple(bbox.Seg.AffineMatrix);
                        hvaffineMatrix[0] = bbox.Seg.AffineMatrix[4];
                        hvaffineMatrix[1] = bbox.Seg.AffineMatrix[3];
                        hvaffineMatrix[2] = bbox.Seg.AffineMatrix[5];
                        hvaffineMatrix[3] = bbox.Seg.AffineMatrix[1];
                        hvaffineMatrix[4] = bbox.Seg.AffineMatrix[0];
                        hvaffineMatrix[5] = bbox.Seg.AffineMatrix[2];

                        hvthreshold = new HTuple(bbox.Seg.Thresh);
                        handle = GCHandle.Alloc(bbox.Seg.Data, GCHandleType.Pinned);
                        IntPtr pointer = handle.AddrOfPinnedObject();
                        HOperatorSet.GenImage1(out hoDefect, "real", bbox.Seg.Width, bbox.Seg.Height, pointer);
                        //HOperatorSet.AffineTransImage(hoDefect, out hoTmp, hvaffineMatrix, "bilinear", "true");
                        HOperatorSet.AffineTransImageSize(hoDefect, out hoTmp, hvaffineMatrix, "bilinear", hvRestoreWidth, hvRestoreHeight);
                        ReplaceHobject(ref hoDefect, ref hoTmp);

                        //Sigmoid(hoDefect, out hoTmp);
                        //ReplaceHobject(ref hoDefect, ref hoTmp);

                        HOperatorSet.Threshold(hoDefect, out hoTmp, hvthreshold, 255);
                        ReplaceHobject(ref hoDefect, ref hoTmp);

                        // 只保留框内的分割掩码
                        HOperatorSet.GenRectangle1(out hoBoxRegionMask, bbox.Top, bbox.Left, bbox.Bottom, bbox.Right);
                        HOperatorSet.Intersection(hoDefect, hoBoxRegionMask, out hoTmp);
                        ReplaceHobject(ref hoDefect, ref hoTmp);

                        HTuple hvWidth = Math.Abs(bbox.Bottom - bbox.Top) * imageData.hvIntervalX;
                        HTuple hvHeight = Math.Abs(bbox.Right - bbox.Left) * imageData.hvIntervalY;
                        HOperatorSet.TupleMax2(hvWidth, hvHeight, out HTuple hvDiameter);

                        HOperatorSet.ZoomRegion(hoDefect, out hoTmp, hvScaleFactorW, hvScaleFactorH);
                        ReplaceHobject(ref hoDefect, ref hoTmp);
                        HOperatorSet.Intersection(hoDefect, hoValidMask, out hoTmp);
                        ReplaceHobject(ref hoDefect, ref hoTmp);
                        HOperatorSet.Connection(hoDefect, out hoTmp);
                        ReplaceHobject(ref hoDefect, ref hoTmp);
                        HOperatorSet.SelectShape(hoDefect, out hoTmp, "area", "and", 99, 9999999999999999999);
                        ReplaceHobject(ref hoDefect, ref hoTmp);

                        HOperatorSet.Intersection(hoDefect, hoOrbitMask, out hoDefectOnOrbit);
                        HOperatorSet.AreaCenter(hoDefectOnOrbit, out HTuple hvOnOrbitArea, out _, out _);

                        HOperatorSet.CountObj(hoDefect, out hvNum);

                        int polygonOffsetCol = (int)(offsetX.D * scaleX.D);
                        int polygonOffsetRow = (int)(offsetY.D * scaleY.D);

                        for (int i = 0; i < hvNum; i++)
                        {
                            HObject hoDefectCurrent = new HObject();
                            HObject hoScaledCurrent = new HObject();
                            HObject hoDefectZCurrent = new HObject();
                            HObject hoPolygonRegion = new HObject();
                            HObject hoTmpCurrent = new HObject();

                            HOperatorSet.GenEmptyObj(out hoDefectCurrent);
                            HOperatorSet.GenEmptyObj(out hoScaledCurrent);
                            HOperatorSet.GenEmptyObj(out hoDefectZCurrent);
                            HOperatorSet.GenEmptyObj(out hoPolygonRegion);
                            HOperatorSet.GenEmptyObj(out hoTmpCurrent);

                            HTuple hvRows = new HTuple();
                            HTuple hvCols = new HTuple();
                            HTuple hvX = new HTuple();
                            HTuple hvY = new HTuple();
                            HTuple hvZ = new HTuple();
                            HTuple hvDefectloud = new HTuple();
                            HTuple hvPlane = new HTuple();
                            HTuple hvPose = new HTuple();
                            HTuple hvNormal = new HTuple();
                            HTuple hvPoseMat = new HTuple();
                            HTuple hvConnCloud = new HTuple();
                            HTuple hvSeleCloud = new HTuple();
                            HTuple hvUnionCloud = new HTuple();
                            HTuple hvPointNum = new HTuple();
                            HTuple hvSmthCloud = new HTuple();
                            HTuple hvAffdCloud = new HTuple();
                            HTuple hvValueZ = new HTuple();
                            HTuple hvSortedZ = new HTuple();
                            HTuple hvAvgZ = new HTuple();
                            HTuple hvMaxZ = new HTuple();
                            HTuple hvMark0 = new HTuple();
                            HTuple hvB0 = new HTuple();
                            HTuple hvTop0 = new HTuple();
                            HTuple hvDiv0 = new HTuple();
                            HTuple hvMark1 = new HTuple();
                            HTuple hvB1 = new HTuple();
                            HTuple hvTop2 = new HTuple();
                            HTuple hvDiv1 = new HTuple();
                            HTuple hvTemp = new HTuple(0);
                            HTuple hvLengthes = new HTuple();
                            HTuple hvIndex = new HTuple();
                            HTuple hvSize = new HTuple();
                            HTuple hvLength = new HTuple(0);
                            HTuple hvGapDistance = new HTuple(0);



                            HTuple hvTmpArea = new HTuple();
                            HTuple hvFlip = new HTuple();
                            HTuple hvMaxLengthIndex = new HTuple();

                            try
                            {
                                HOperatorSet.SelectObj(hoDefect, out hoDefectCurrent, i + 1);
                                HOperatorSet.ZoomRegion(hoDefectCurrent, out hoScaledCurrent, scaleX, scaleY);
                                HOperatorSet.FillUp(hoScaledCurrent, out hoTmpCurrent);
                                ReplaceHobject(ref hoScaledCurrent, ref hoTmpCurrent);

                                HOperatorSet.ZoomRegion(hoScaledCurrent, out hoPolygonRegion, hvAccelerationFactor, hvAccelerationFactor);
                                polygons.Add(new Polygon(hoPolygonRegion, polygonOffsetCol, polygonOffsetRow));

                                HOperatorSet.RegionFeatures(hoScaledCurrent, "area", out hvTmpArea);
                                if (HasTupleValue(hvTmpArea))
                                {
                                    hvTmpArea = hvTmpArea * hvXp * hvXp;
                                    hvAreaFeature = hvAreaFeature.TupleConcat(hvTmpArea);
                                }

                                HOperatorSet.DilationCircle(hoDefectCurrent, out hoTmpCurrent, 3);
                                ReplaceHobject(ref hoDefectCurrent, ref hoTmpCurrent);

                                HOperatorSet.ReduceDomain(hoNailBaseResidual, hoDefectCurrent, out hoDefectZCurrent);
                                HOperatorSet.GetRegionPoints(hoDefectZCurrent, out hvRows, out hvCols);
                                if (!HasTupleValue(hvRows) || !HasTupleValue(hvCols))
                                    continue;

                                hvX = hvCols * imageData.hvIntervalX;
                                hvY = hvRows * imageData.hvIntervalY;
                                HOperatorSet.GetGrayval(hoDefectZCurrent, hvRows, hvCols, out hvZ);
                                if (!HasTupleValue(hvZ))
                                    continue;

                                //HOperatorSet.GenObjectModel3dFromPoints(hvX, hvY, hvZ, out hvDefectloud);
                                //HOperatorSet.FitPrimitivesObjectModel3d(hvDefectloud, (new HTuple("primitive_type")).TupleConcat("fitting_algorithm"),
                                //                                         (new HTuple("plane")).TupleConcat("least_squares"), out hvPlane);
                                //HOperatorSet.GetObjectModel3dParams(hvPlane, "primitive_parameter_pose", out hvPose);
                                //HOperatorSet.PoseInvert(hvPose, out hvPose);
                                //HOperatorSet.GetObjectModel3dParams(hvPlane, "primitive_parameter", out hvNormal);
                                //if (HasTupleValue(hvNormal, 3) && hvNormal.TupleSelect(2).D < 0)
                                //{
                                //    HOperatorSet.CreatePose(0, 0, 0, 180, 0, 0, "Rp+T", "gba", "point", out hvFlip);
                                //    HOperatorSet.PoseCompose(hvFlip, hvPose, out hvPose);
                                //}
                                //HOperatorSet.PoseToHomMat3d(hvPose, out hvPoseMat);
                                //HOperatorSet.ConnectionObjectModel3d(hvDefectloud, "distance_3d", 2 * hvXp, out hvConnCloud);
                                //HOperatorSet.SelectObjectModel3d(hvConnCloud, "num_points", "and", 9, 9999999999999999999, out hvSeleCloud);
                                //HOperatorSet.UnionObjectModel3d(hvSeleCloud, "points_surface", out hvUnionCloud);
                                //HOperatorSet.GetObjectModel3dParams(hvUnionCloud, "num_points", out hvPointNum);
                                //if (!HasTupleValue(hvPointNum) || hvPointNum.I == 0)
                                //    continue;

                                //HOperatorSet.SmoothObjectModel3d(hvUnionCloud, "mls", "mls_kNN", 199, out hvSmthCloud);
                                //HOperatorSet.AffineTransObjectModel3d(hvSmthCloud, hvPoseMat, out hvAffdCloud);
                                //HOperatorSet.GetObjectModel3dParams(hvAffdCloud, "point_coord_z", out hvValueZ);
                                //if (!HasTupleValue(hvValueZ))
                                //    continue;

                                hvValueZ = hvZ;
                                HOperatorSet.TupleSort(hvValueZ, out hvSortedZ);
                                HOperatorSet.TupleMean(hvSortedZ, out hvAvgZ);
                                HOperatorSet.TupleMax(hvSortedZ, out hvMaxZ);
                                HOperatorSet.TupleGreaterElem(hvSortedZ, hvAvgZ, out hvMark0);
                                HOperatorSet.TupleFindFirst(hvMark0, 1, out hvB0);

                                if (HasTupleValue(hvB0) && hvB0.I != -1)
                                {
                                    //焊缝表面熔珠高度
                                    if (hvOnOrbitArea.D > 0)
                                    {
                                        HOperatorSet.TupleSelectRange(hvSortedZ, 0, hvB0, out hvTop0);
                                        if (HasTupleValue(hvTop0))
                                        {
                                            HOperatorSet.TupleMean(hvTop0, out hvDiv0);
                                            HOperatorSet.TupleGreaterElem(hvTop0, hvDiv0, out hvMark1);
                                            HOperatorSet.TupleFindFirst(hvMark1, 1, out hvB1);
                                            if (HasTupleValue(hvB1) && hvB1.I != -1)
                                            {
                                                HOperatorSet.TupleSelectRange(hvTop0, 0, hvB1, out hvTop2);
                                                if (HasTupleValue(hvTop2))
                                                    HOperatorSet.TupleMean(hvTop2, out hvDiv1);
                                                else
                                                    hvDiv1 = hvDiv0.Clone();
                                            }
                                            else
                                            {
                                                hvDiv1 = hvDiv0.Clone();
                                            }
                                            hvTemp = hvMaxZ - hvDiv1;
                                        }
                                    }
                                    //焊缝外表面熔珠高度
                                    else
                                    {
                                        hvTemp = hvMaxZ;
                                    }
                                }

                                hvDepthFeature = hvDepthFeature.TupleConcat(hvTemp);
                            }
                            catch (Exception ex)
                            {
                                hasProcessException = true;
                                Console.WriteLine($"WARN:GetMoltenBeadFeature_Sunwoda()第{i + 1}个熔珠测量失败:{ex.Message}");
                            }
                            finally
                            {
                                hoDefectCurrent.Dispose();
                                hoScaledCurrent.Dispose();
                                hoDefectZCurrent.Dispose();
                                hoPolygonRegion.Dispose();
                                hoTmpCurrent.Dispose();

                                hvRows.Dispose();
                                hvCols.Dispose();
                                hvX.Dispose();
                                hvY.Dispose();
                                hvZ.Dispose();
                                hvDefectloud.Dispose();
                                hvPlane.Dispose();
                                hvPose.Dispose();
                                hvNormal.Dispose();
                                hvPoseMat.Dispose();
                                hvConnCloud.Dispose();
                                hvSeleCloud.Dispose();
                                hvUnionCloud.Dispose();
                                hvPointNum.Dispose();
                                hvSmthCloud.Dispose();
                                hvAffdCloud.Dispose();
                                hvValueZ.Dispose();
                                hvSortedZ.Dispose();
                                hvAvgZ.Dispose();
                                hvMaxZ.Dispose();
                                hvMark0.Dispose();
                                hvB0.Dispose();
                                hvTop0.Dispose();
                                hvDiv0.Dispose();
                                hvMark1.Dispose();
                                hvB1.Dispose();
                                hvTop2.Dispose();
                                hvDiv1.Dispose();
                                hvTemp.Dispose();
                                hvLengthes.Dispose();
                                hvIndex.Dispose();
                                hvSize.Dispose();
                                hvLength.Dispose();
                                hvGapDistance.Dispose();
                                hvTmpArea.Dispose();
                                hvFlip.Dispose();
                                hvMaxLengthIndex.Dispose();
                            }
                        }

                        if(HasTupleValue(hvDiameter))
                            result.Diameter = hvDiameter * 0.001;

                        if (HasTupleValue(hvDepthFeature))
                            result.DepthFeature = hvDepthFeature.TupleMax().D * 0.001;

                        if (HasTupleValue(hvAreaFeature))
                            result.AreaFeature = (hvAreaFeature.TupleSum() * hvAccelerationFactor * hvAccelerationFactor).D * 0.001 * 0.001;

                        if (HasTupleValue(hvDepthFeature) && HasTupleValue(hvAreaFeature))
                        {
                            if (result.DepthFeature > height_select && result.AreaFeature > area_select)
                            {
                                result.IsOk = false;
                            }
                            else
                            {
                                result.IsOk = true;
                            }
                        }
                        else if (HasTupleValue(hvNum) && hvNum.I > 0 && hasProcessException)
                        {
                            result.IsOk = false;
                        }
                        else
                        {
                            result.IsOk = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logs.LogError($"{DateTime.Now}:GetMoltenBeadFeature_Sunwoda()报错信息:{ex.Message},调用堆栈:{ex.StackTrace}");
                        Console.WriteLine(ex.StackTrace);
                        result.IsOk = false;
                    }
                    finally
                    {
                        if (handle.IsAllocated)
                            handle.Free();

                        hoHeightImage.Dispose();
                        hoValidMask.Dispose();
                        hoValidMaskZoom.Dispose();
                        hoNailBaseMaskMoved.Dispose();
                        hoOrbitMaskMoved.Dispose();
                        hoDefect.Dispose();
                        hoBoxRegionMask.Dispose();
                        hoDefectOnOrbit.Dispose();
                        hoNailBaseSurface.Dispose();
                        hoNailBaseResidual.Dispose();
                        hoTmp.Dispose();

                        scaleX.Dispose();
                        scaleY.Dispose();
                        hvXp.Dispose();
                        hvZp.Dispose();
                        offsetX.Dispose();
                        offsetY.Dispose();
                        hvAccelerationFactor.Dispose();
                        hvScaleFactorW.Dispose();
                        hvScaleFactorH.Dispose();
                        hvNum.Dispose();
                        hvDepthFeature.Dispose();
                        hvAreaFeature.Dispose();
                        hvaffineMatrix.Dispose();
                        hvthreshold.Dispose();
                    }

                    return result;
                }
            }

        }
    }
}