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
        public int GetNailCenterAndOrbitMaskWrapper(
            HObject hoImage,
            HObject hoHeightImage,
            out HTuple hvNailCx,
            out HTuple hvNailCy,
            out HTuple hvOrbitParam)
        {
            hvNailCx = new HTuple();
            hvNailCy = new HTuple();
            hvOrbitParam = new HTuple();

            try
            {
                if (_locator == null || string.IsNullOrWhiteSpace(_locator.AlgName))
                {
                    Logs.LogError($"{DateTime.Now}:GetNailCenterAndOrbitMaskWrapper() locator alg_name is empty.");
                    return -1;
                }

                if (_locator.AlgName == nameof(GetNailCenterAndOrbitMaskWrapper))
                {
                    Logs.LogError($"{DateTime.Now}:GetNailCenterAndOrbitMaskWrapper() recursive locator alg_name is invalid.");
                    return -1;
                }

                MethodInfo? method = GetType().GetMethod(
                    _locator.AlgName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (method == null)
                {
                    Logs.LogError($"{DateTime.Now}:GetNailCenterAndOrbitMaskWrapper() method not found: {_locator.AlgName}.");
                    return -1;
                }

                ParameterInfo[] parameters = method.GetParameters();
                object?[] args = new object?[parameters.Length];
                int cxIndex = -1;
                int cyIndex = -1;
                int orbitIndex = -1;

                Dictionary<string, object?> paramDict = _locator.AlgParam
                    .ToDictionary(p => p.Name, p => (object?)BsonTypeMapper.MapToDotNetValue(p.Value));

                for (int i = 0; i < parameters.Length; i++)
                {
                    ParameterInfo param = parameters[i];
                    if (param.IsOut)
                    {
                        args[i] = new HTuple();

                        if (param.Name == "hvNailCx" || param.Name == "hvCx")
                            cxIndex = i;
                        else if (param.Name == "hvNailCy" || param.Name == "hvCy")
                            cyIndex = i;
                        else if (param.Name == "hvOrbitParam" || param.Name == "hvOrbitOuterParam")
                            orbitIndex = i;

                        continue;
                    }

                    if (param.Name == "hoImage" || param.Name == "hoTileGrayImage")
                    {
                        args[i] = hoImage;
                    }
                    else if (param.Name == "hoHeightImage" || param.Name == "hoTileHeightImage")
                    {
                        args[i] = hoHeightImage;
                    }
                    else if (param.Name != null && paramDict.TryGetValue(param.Name, out object? value))
                    {
                        Type targetType = Nullable.GetUnderlyingType(param.ParameterType) ?? param.ParameterType;
                        args[i] = value == null ? null : Convert.ChangeType(value, targetType);
                    }
                    else if (param.HasDefaultValue)
                    {
                        args[i] = param.DefaultValue;
                    }
                    else
                    {
                        Logs.LogError($"{DateTime.Now}:GetNailCenterAndOrbitMaskWrapper() missing required parameter: {param.Name}.");
                        return -1;
                    }
                }

                if (cxIndex < 0 || cyIndex < 0 || orbitIndex < 0)
                {
                    Logs.LogError($"{DateTime.Now}:GetNailCenterAndOrbitMaskWrapper() output parameters are incomplete: {_locator.AlgName}.");
                    return -1;
                }

                object? invokeResult = method.Invoke(this, args);
                if (invokeResult is not int state)
                {
                    Logs.LogError($"{DateTime.Now}:GetNailCenterAndOrbitMaskWrapper() return type is not int: {_locator.AlgName}.");
                    return -1;
                }

                hvNailCx = args[cxIndex] as HTuple ?? new HTuple();
                hvNailCy = args[cyIndex] as HTuple ?? new HTuple();
                hvOrbitParam = args[orbitIndex] as HTuple ?? new HTuple();

                return state;
            }
            catch (TargetInvocationException ex)
            {
                Exception innerEx = ex.InnerException ?? ex;
                Logs.LogError($"{DateTime.Now}:GetNailCenterAndOrbitMaskWrapper() error:{innerEx.Message}, stack:{innerEx.StackTrace}");
                Console.WriteLine(innerEx.StackTrace);
                return -1;
            }
            catch (Exception ex)
            {
                Logs.LogError($"{DateTime.Now}:GetNailCenterAndOrbitMaskWrapper() error:{ex.Message}, stack:{ex.StackTrace}");
                Console.WriteLine(ex.StackTrace);
                return -1;
            }
        }

        /// <summary>
        /// 定位密封钉中心(old)
        /// </summary>
        public int GetNailCenterAndOrbitMaskOld(HObject hoImage, HObject hoHeightImage, out HTuple hvNailCx, out HTuple hvNailCy, out HTuple hvOrbitParam)
        {
            HObject hoFindRegion;
            HObject hoImageReduced;

            HTuple hvAngle, hvScale, hvScore;

            HOperatorSet.GetImageSize(hoImage, out HTuple hvTmpW, out HTuple hvTmpH);
            HOperatorSet.GenCircle(out hoFindRegion, 0.5 * hvTmpH, 0.5 * hvTmpW, 560.07);
            HOperatorSet.ReduceDomain(hoImage, hoFindRegion, out hoImageReduced);

            HOperatorSet.FindScaledShapeModel(hoImageReduced, _hvNailCenterModelID, -0.39, 0.78, 0.3, 3, 0.9, 1, 0.5,
                                              "least_squares", 0, 0.9, out hvNailCy, out hvNailCx, out hvAngle, out hvScale, out hvScore);

            HObject hoModel;
            HObject hoModelRegon;
            HOperatorSet.GetShapeModelContours(out hoModel, _hvNailCenterModelID, 1);
            HOperatorSet.GenRegionContourXld(hoModel, out hoModelRegon, "filled");
            HOperatorSet.RegionFeatures(hoModelRegon, "outer_radius", out HTuple radius);

            if (hvScore.Length > 0)
            {
                HOperatorSet.GenCircle(out _hoNailCenterModel, hvNailCy, hvNailCx, hvScale * radius);

                _nailCenterIsTrue = true;
            }
            else
            {
                hvNailCx = 0.5 * hvTmpW;
                hvNailCy = 0.5 * hvTmpH;
                HOperatorSet.GenCircle(out _hoNailCenterModel, hvNailCy, hvNailCx, radius);

                _nailCenterIsTrue = false;
            }

            // 计算焊接轨迹的外轮廓
            HObject hoImageMean;
            HObject hoContours;

            HTuple hvMetrologyHandle;
            HTuple hvIndex;
            HTuple hvContourRow, hvContourCol;

            HOperatorSet.CreateMetrologyModel(out hvMetrologyHandle);
            HOperatorSet.AddMetrologyObjectCircleMeasure(hvMetrologyHandle, hvNailCy, hvNailCx, 1500, 638, 5, 28, 30,
                                                         new HTuple(), new HTuple(), out hvIndex);
            HOperatorSet.SetMetrologyObjectParam(hvMetrologyHandle, 0, "measure_transition", "negative");
            HOperatorSet.SetMetrologyObjectParam(hvMetrologyHandle, "all", "min_score", 0.01);
            HOperatorSet.SetMetrologyObjectParam(hvMetrologyHandle, "all", "measure_select", "last");

            HOperatorSet.MeanImage(hoImage, out hoImageMean, 33, 33);

            HOperatorSet.ApplyMetrologyModel(hoImageMean, hvMetrologyHandle);
            HOperatorSet.GetMetrologyObjectMeasures(out hoContours, hvMetrologyHandle, "all", "all", out hvContourRow, out hvContourCol);
            HOperatorSet.GetMetrologyObjectResult(hvMetrologyHandle, "all", "all", "result_type", "all_param", out hvOrbitParam);

            // 焊迹掩码
            if (hvOrbitParam.Length > 0)
            {
                HTuple hvCirRow, hvCirCol, hvCirRad;

                hvCirRow = hvOrbitParam[0];
                hvCirCol = hvOrbitParam[1];
                hvCirRad = hvOrbitParam[2];

                HObject hoCircleInter, hoCircleOuter;
                HObject hoRect, hoRing;
                HOperatorSet.GenCircle(out hoCircleInter, hvCirRow, hvCirCol, hvCirRad * 0.656);
                HOperatorSet.GenCircle(out hoCircleOuter, hvCirRow, hvCirCol, hvCirRad);

                HOperatorSet.GenRectangle1(out hoRect, hvCirRow + (hvCirRad * 0.656), hvCirCol - (hvCirRad * 0.846),
                                                       hvCirRow + hvCirRad, hvCirCol + (hvCirRad * 0.846));

                HOperatorSet.Difference(hoCircleOuter, hoCircleInter, out hoRing);
                HOperatorSet.Union2(hoRing, hoRect, out _hoOrbitMask);

                hoCircleInter.Dispose();
                hoCircleOuter.Dispose();
                hoRect.Dispose();
                hoRing.Dispose();
            }
            else
            {
                HOperatorSet.GenRectangle1(out _hoOrbitMask, 0, 0, hvTmpH, hvTmpW);
            }

            HTuple scaleX, scaleY;
            int imageNum = _imageData.Count;
            if (imageNum > 0)
            {
                if (_imageData[0].hvIntervalX > _imageData[0].hvIntervalY)
                {
                    scaleX = _imageData[0].hvIntervalX / _imageData[0].hvIntervalY;
                    scaleY = 1;
                }
                else
                {
                    scaleX = 1;
                    scaleY = _imageData[0].hvIntervalY / _imageData[0].hvIntervalX;
                }
            }
            else
            {
                scaleX = 1;
                scaleY = 1;
            }

            HOperatorSet.Intersection(_hoValidMask, _hoOrbitMask, out _hoOrbitMask);

            HOperatorSet.ZoomRegion(_hoOrbitMask, out _hoOrbitMask, 1 / scaleX, 1 / scaleY);



            hoFindRegion.Dispose();
            hoImageReduced.Dispose();
            hoModel.Dispose();
            hoModelRegon.Dispose();
            hoImageMean.Dispose();
            hoContours.Dispose();

            return 0;
        }


        /// <summary>
        /// 定位密封钉中心(亿纬新工艺)
        /// </summary>
        public int GetNailCenterAndOrbitMask(HObject hoImage, HObject hoHeightImage, out HTuple hvNailCx, out HTuple hvNailCy, out HTuple hvOrbitOuterParam)
        {
            using (var dh = new HDevDisposeHelper())
            {
                int state = -1;

                hvNailCx = new HTuple();
                hvNailCy = new HTuple();
                hvOrbitOuterParam = new HTuple();

                HTuple hvIndex;
                HTuple hvModelAngle, hvModelScore, hvModelRadius;
                HTuple hvOrbitInterHandle = new HTuple();
                HTuple hvCoarseHandle = new HTuple();
                HTuple hvOrbitOuterHandle = new HTuple();

                HObject hoCoarseNailCircleRegion = new HObject();
                HObject hoTemplateRegion = new HObject();
                HObject hoImageResize = new HObject();
                HObject hoImageResizeMean = new HObject();
                HObject hoROI_0 = new HObject();
                HObject hoROIValid_0 = new HObject();
                HObject hoImageReduced = new HObject();
                HObject hoHeightImageReducedExpanded = new HObject();
                HObject hoImageModelCircle = new HObject();
                HObject hoImageReducedMean = new HObject();
                HObject hoInvalidROI_0 = new HObject();
                HObject hoOrbitInterMeasures = new HObject();
                HObject hoCoverCircleMeasures = new HObject();
                HObject hoOrbitInterContour = new HObject();
                HObject hoCoverContour = new HObject();
                HObject hoOrbitOuterMeasures = new HObject();
                HObject hoOrbitOuterFitContour = new HObject();
                HObject hoOrbitOuterContour = new HObject();
                HObject hoOrbitOuterContourRegion = new HObject();
                HObject hoOrbitOuterContourImage = new HObject();
                HObject hoNailCenterModel = new HObject();
                HObject hoOrbitMask = new HObject();
                HObject hoNailWarpBaseMask = new HObject();
                HObject hoCircleInter = new HObject();
                HObject hoCircleOuter = new HObject();
                HObject hoRect0 = new HObject();
                HObject hoRect1 = new HObject();
                HObject hoRing = new HObject();
                HObject hoNailCircle = new HObject();
                HObject hoBaseCircleOuter = new HObject();
                HObject hoTmp = new HObject();
                HObject hoTmp1 = new HObject();

                HOperatorSet.GenEmptyObj(out hoCoarseNailCircleRegion);
                HOperatorSet.GenEmptyObj(out hoTemplateRegion);
                HOperatorSet.GenEmptyObj(out hoImageResize);
                HOperatorSet.GenEmptyObj(out hoImageResizeMean);
                HOperatorSet.GenEmptyObj(out hoROI_0);
                HOperatorSet.GenEmptyObj(out hoROIValid_0);
                HOperatorSet.GenEmptyObj(out hoImageReduced);
                HOperatorSet.GenEmptyObj(out hoHeightImageReducedExpanded);
                HOperatorSet.GenEmptyObj(out hoImageModelCircle);
                HOperatorSet.GenEmptyObj(out hoImageReducedMean);
                HOperatorSet.GenEmptyObj(out hoInvalidROI_0);
                HOperatorSet.GenEmptyObj(out hoOrbitInterMeasures);
                HOperatorSet.GenEmptyObj(out hoCoverCircleMeasures);
                HOperatorSet.GenEmptyObj(out hoOrbitInterContour);
                HOperatorSet.GenEmptyObj(out hoCoverContour);
                HOperatorSet.GenEmptyObj(out hoOrbitOuterMeasures);
                HOperatorSet.GenEmptyObj(out hoOrbitOuterFitContour);
                HOperatorSet.GenEmptyObj(out hoOrbitOuterContour);
                HOperatorSet.GenEmptyObj(out hoOrbitOuterContourRegion);
                HOperatorSet.GenEmptyObj(out hoOrbitOuterContourImage);
                HOperatorSet.GenEmptyObj(out hoNailCenterModel);
                HOperatorSet.GenEmptyObj(out hoOrbitMask);
                HOperatorSet.GenEmptyObj(out hoNailWarpBaseMask);
                HOperatorSet.GenEmptyObj(out hoCircleInter);
                HOperatorSet.GenEmptyObj(out hoCircleOuter);
                HOperatorSet.GenEmptyObj(out hoRect0);
                HOperatorSet.GenEmptyObj(out hoRect1);
                HOperatorSet.GenEmptyObj(out hoRing);
                HOperatorSet.GenEmptyObj(out hoNailCircle);
                HOperatorSet.GenEmptyObj(out hoBaseCircleOuter);
                HOperatorSet.GenEmptyObj(out hoTmp);
                HOperatorSet.GenEmptyObj(out hoTmp1);

                bool nailCenterIsTrue = false;

                try
                {
                    HOperatorSet.GetImageSize(hoImage, out HTuple hvImageOriW, out HTuple hvImageOriH);
                    HOperatorSet.GetNccModelRegion(out hoTemplateRegion, _hvNailCenterModelID);
                    HOperatorSet.RegionFeatures(hoTemplateRegion, "outer_radius", out hvModelRadius);

                    // 密封钉中心区域粗定位
                    HTuple hvAccelerationFactor = 4.0f;
                    HTuple hvScaleFactorW = 1.0f / hvAccelerationFactor;
                    HTuple hvScaleFactorH = 1.0f / hvAccelerationFactor;
                    HOperatorSet.ZoomImageFactor(hoImage, out hoImageResize, hvScaleFactorW, hvScaleFactorH, "constant");
                    HOperatorSet.GetImageSize(hoImageResize, out HTuple hvResizeWidth, out HTuple hvResizeHeight);

                    HTuple hvResizeImageCenterRow = hvResizeHeight / 2.0f;
                    HTuple hvResizeImageCenterCol = hvResizeWidth / 2.0f;
                    HTuple hvTmpRadius = (hvResizeImageCenterRow + hvResizeImageCenterCol) / 2.0f;

                    HOperatorSet.CreateMetrologyModel(out hvCoarseHandle);
                    HOperatorSet.AddMetrologyObjectCircleMeasure(hvCoarseHandle, hvResizeImageCenterRow, hvResizeImageCenterCol,
                                                                 hvTmpRadius * 1.2, hvTmpRadius * 0.35, 5, 25, 30, new HTuple(), new HTuple(), out hvIndex);
                    HOperatorSet.SetMetrologyObjectParam(hvCoarseHandle, 0, "measure_transition", "positive");
                    HOperatorSet.SetMetrologyObjectParam(hvCoarseHandle, "all", "min_score", 0.01);
                    HOperatorSet.SetMetrologyObjectParam(hvCoarseHandle, "all", "measure_select", "last");
                    HOperatorSet.MeanImage(hoImageResize, out hoImageResizeMean, 11, 11);
                    HOperatorSet.ApplyMetrologyModel(hoImageResizeMean, hvCoarseHandle);
                    HOperatorSet.GetMetrologyObjectResult(hvCoarseHandle, "all", "all", "result_type", "all_param", out HTuple hvNailCircleParam);

                    HTuple hvCoarseCenterRow, hvCoarseCenterCol, hvCoarseRadius;
                    if (hvNailCircleParam.Length > 0)
                    {
                        hvCoarseCenterRow = (hvNailCircleParam.TupleSelect(0)) * hvAccelerationFactor;
                        hvCoarseCenterCol = (hvNailCircleParam.TupleSelect(1)) * hvAccelerationFactor;
                        hvCoarseRadius = (hvNailCircleParam.TupleSelect(2)) * hvAccelerationFactor;

                        HOperatorSet.GenCircle(out hoCoarseNailCircleRegion, hvCoarseCenterRow, hvCoarseCenterCol, hvCoarseRadius);
                    }
                    else
                    {
                        hvCoarseCenterRow = hvImageOriH / 2.0f;
                        hvCoarseCenterCol = hvImageOriW / 2.0f;
                        hvCoarseRadius = Math.Min(hvImageOriH, hvImageOriW) * 0.5;
                    }

                    // 精定位密封钉中心
                    HOperatorSet.GenCircle(out hoROI_0, hvCoarseCenterRow, hvCoarseCenterCol, hvCoarseRadius * 0.25);
                    HOperatorSet.ReduceDomain(hoHeightImage, hoROI_0, out hoImageReduced);
                    HOperatorSet.Intersection(hoROI_0, _hoIrregularMask, out hoInvalidROI_0);
                    HOperatorSet.PaintRegion(hoInvalidROI_0, hoImageReduced, out hoTmp, _hvHeightImageGlobalMinValue, "fill");
                    ReplaceHobject(ref hoImageReduced, ref hoTmp);
                    HOperatorSet.ScaleImageMax(hoImageReduced, out hoTmp);
                    ReplaceHobject(ref hoImageReduced, ref hoTmp);
                    HOperatorSet.MeanImage(hoImageReduced, out hoImageReducedMean, 5, 5);
                    HOperatorSet.FindNccModel(hoImageReducedMean, _hvNailCenterModelID, -0.39, 0.79, 0.65, 1, 0.5, "true", 0,
                                                out hvNailCy, out hvNailCx, out hvModelAngle, out hvModelScore);

                    if (hvModelScore.Length > 0)
                    {
                        HOperatorSet.GenCircle(out hoNailCenterModel, hvNailCy, hvNailCx, hvModelRadius);
                        nailCenterIsTrue = true;
                    }
                    else
                    {
                        hvNailCx.Dispose();
                        hvNailCy.Dispose();
                        hvNailCx = hvCoarseCenterCol.Clone();
                        hvNailCy = hvCoarseCenterRow.Clone();
                        HOperatorSet.GenCircle(out hoNailCenterModel, hvNailCy, hvNailCx, hvModelRadius);
                        nailCenterIsTrue = true;
                    }

                    // 定位焊缝外圈圆
                    HTuple hvPointRow, hvPointCol;
                    HOperatorSet.CreateMetrologyModel(out hvOrbitOuterHandle);
                    HOperatorSet.AddMetrologyObjectCircleMeasure(hvOrbitOuterHandle, hvNailCy / hvAccelerationFactor, hvNailCx / hvAccelerationFactor,
                                                                 (hvCoarseRadius / hvAccelerationFactor) * 0.55, (hvCoarseRadius / hvAccelerationFactor) * 0.35,
                                                                 5, 28, 30, new HTuple(), new HTuple(), out hvIndex);
                    HOperatorSet.SetMetrologyObjectParam(hvOrbitOuterHandle, 0, "measure_transition", "negative");
                    HOperatorSet.SetMetrologyObjectParam(hvOrbitOuterHandle, "all", "min_score", 0.01);
                    HOperatorSet.SetMetrologyObjectParam(hvOrbitOuterHandle, "all", "measure_select", "last");
                    HOperatorSet.ApplyMetrologyModel(hoImageResizeMean, hvOrbitOuterHandle);
                    HOperatorSet.GetMetrologyObjectMeasures(out hoOrbitOuterMeasures, hvOrbitOuterHandle, "all", "all", out hvPointRow, out hvPointCol);

                    HTuple TmpCircleRow = new HTuple();
                    HTuple TmpCircleColumn = new HTuple();
                    HTuple TmpCircleRadius = new HTuple();
                    if (hvPointRow.Length > 2)
                    {
                        HOperatorSet.GenContourPolygonXld(out hoOrbitOuterFitContour, hvPointRow, hvPointCol);
                        try
                        {
                            HOperatorSet.FitCircleContourXld(hoOrbitOuterFitContour, "geotukey", -1, 0, 0, 3, 2,
                                                             out TmpCircleRow, out TmpCircleColumn, out TmpCircleRadius,
                                                             out _, out _, out _);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"WARN:failed to fit orbit outer-circle contour: {ex.Message}");
                        }
                    }

                    bool hasOrbitOuterFit = TmpCircleRow.Length > 0;
                    HTuple hvFallbackOrbitOuterRow = hvImageOriH / 2.0f;
                    HTuple hvFallbackOrbitOuterCol = hvImageOriW / 2.0f;
                    HOperatorSet.TupleMin2(hvFallbackOrbitOuterRow, hvFallbackOrbitOuterCol, out HTuple hvFallbackOrbitOuterRad);

                    if (hasOrbitOuterFit)
                    {
                        HOperatorSet.TupleConcat(hvOrbitOuterParam, TmpCircleRow * hvAccelerationFactor, out hvOrbitOuterParam);
                        HOperatorSet.TupleConcat(hvOrbitOuterParam, TmpCircleColumn * hvAccelerationFactor, out hvOrbitOuterParam);
                        HOperatorSet.TupleConcat(hvOrbitOuterParam, TmpCircleRadius * hvAccelerationFactor, out hvOrbitOuterParam);

                        HOperatorSet.GenCircleContourXld(out hoOrbitOuterContour, TmpCircleRow, TmpCircleColumn, TmpCircleRadius,
                                                         (new HTuple(0)).TupleRad(), (new HTuple(360)).TupleRad(), "positive", 1);
                    }
                    else
                    {
                        HOperatorSet.TupleConcat(hvOrbitOuterParam, hvFallbackOrbitOuterRow, out hvOrbitOuterParam);
                        HOperatorSet.TupleConcat(hvOrbitOuterParam, hvFallbackOrbitOuterCol, out hvOrbitOuterParam);
                        HOperatorSet.TupleConcat(hvOrbitOuterParam, hvFallbackOrbitOuterRad, out hvOrbitOuterParam);

                        Console.WriteLine("WARN:fallback to the full-image outer-circle parameters because orbit outer-circle fitting failed.");
                    }

                    // 焊迹掩码
                    HTuple hvOrbitOuterCircleRow, hvOrbitOuterCircleCol, hvOrbitOuterCircleRad;
                    HTuple hvOrbitInterCircleRow, hvOrbitInterCircleCol, hvOrbitInterCircleRad;
                    if (hasOrbitOuterFit)
                    {
                        HTuple hvOrbitInterParam;
                        HTuple hvCoverCircleParam;

                        hvOrbitOuterCircleRow = TmpCircleRow;
                        hvOrbitOuterCircleCol = TmpCircleColumn;
                        hvOrbitOuterCircleRad = TmpCircleRadius;

                        double orbitOuterCircleRowValue = hvOrbitOuterCircleRow.D;
                        double orbitOuterCircleColValue = hvOrbitOuterCircleCol.D;
                        double orbitOuterCircleRadValue = hvOrbitOuterCircleRad.D;
                        bool canLocateOrbitInterCircle = orbitOuterCircleRadValue > 0
                                                        && orbitOuterCircleRowValue >= 0
                                                        && orbitOuterCircleRowValue < hvResizeHeight.D
                                                        && orbitOuterCircleColValue >= 0
                                                        && orbitOuterCircleColValue < hvResizeWidth.D;

                        if (canLocateOrbitInterCircle)
                        {
                            HOperatorSet.GenRegionContourXld(hoOrbitOuterContour, out hoOrbitOuterContourRegion, "filled");
                            HOperatorSet.ReduceDomain(hoImageResizeMean, hoOrbitOuterContourRegion, out hoOrbitOuterContourImage);
                            HOperatorSet.CreateMetrologyModel(out hvOrbitInterHandle);
                            HOperatorSet.AddMetrologyObjectCircleMeasure(hvOrbitInterHandle, hvOrbitOuterCircleRow, hvOrbitOuterCircleCol,
                                                                         hvOrbitOuterCircleRad * 0.5, hvOrbitOuterCircleRad * 0.45,
                                                                         5, 28, 30, new HTuple(), new HTuple(), out HTuple hvIndex0);
                            HOperatorSet.AddMetrologyObjectCircleMeasure(hvOrbitInterHandle, hvOrbitOuterCircleRow, hvOrbitOuterCircleCol,
                                                                         hvOrbitOuterCircleRad * 0.5, hvOrbitOuterCircleRad * 0.45,
                                                                         5, 28, 30, new HTuple(), new HTuple(), out HTuple hvIndex1);
                            HOperatorSet.SetMetrologyObjectParam(hvOrbitInterHandle, hvIndex0, "measure_transition", "negative");
                            HOperatorSet.SetMetrologyObjectParam(hvOrbitInterHandle, hvIndex1, "measure_transition", "positive");
                            HOperatorSet.SetMetrologyObjectParam(hvOrbitInterHandle, "all", "min_score", 0.01);
                            HOperatorSet.SetMetrologyObjectParam(hvOrbitInterHandle, hvIndex0, "measure_select", "last");
                            HOperatorSet.SetMetrologyObjectParam(hvOrbitInterHandle, hvIndex1, "measure_select", "first");

                            HOperatorSet.ApplyMetrologyModel(hoOrbitOuterContourImage, hvOrbitInterHandle);
                            HOperatorSet.GetMetrologyObjectResult(hvOrbitInterHandle, hvIndex0, "all", "result_type", "all_param", out hvOrbitInterParam);
                            HOperatorSet.GetMetrologyObjectResult(hvOrbitInterHandle, hvIndex1, "all", "result_type", "all_param", out hvCoverCircleParam);

                            if (hvOrbitInterParam.TupleLength() >= 3
                                && hvOrbitInterParam[2].D > 0
                                && hvOrbitInterParam[2].D < orbitOuterCircleRadValue)
                            {
                                hvOrbitInterCircleRow = hvOrbitInterParam[0];
                                hvOrbitInterCircleCol = hvOrbitInterParam[1];
                                hvOrbitInterCircleRad = hvOrbitInterParam[2];
                            }
                            else
                            {
                                hvOrbitInterCircleRow = hvOrbitOuterCircleRow;
                                hvOrbitInterCircleCol = hvOrbitOuterCircleCol;
                                hvOrbitInterCircleRad = hvOrbitOuterCircleRad * 0.65;

                                Console.WriteLine("WARN:fallback to the default inner-circle radius because inner-circle metrology failed.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("WARN:skip orbit inner-circle metrology because the fitted outer-circle center is outside the resized image.");
                            hvOrbitInterCircleRow = hvOrbitOuterCircleRow;
                            hvOrbitInterCircleCol = hvOrbitOuterCircleCol;
                            hvOrbitInterCircleRad = hvOrbitOuterCircleRad * 0.656;
                        }

                        if (hvOrbitInterCircleRad.D <= 0 || hvOrbitInterCircleRad.D >= hvOrbitOuterCircleRad.D)
                        {
                            Console.WriteLine("WARN:adjust invalid inner-circle radius by fallback ratio.");
                            hvOrbitInterCircleRow = hvOrbitOuterCircleRow;
                            hvOrbitInterCircleCol = hvOrbitOuterCircleCol;
                            hvOrbitInterCircleRad = hvOrbitOuterCircleRad * 0.656;
                        }

                        HTuple TmpOrbitW = hvOrbitOuterCircleRad - hvOrbitInterCircleRad;

                        HOperatorSet.GenCircle(out hoCircleInter, hvOrbitOuterCircleRow, hvOrbitOuterCircleCol, hvOrbitOuterCircleRad - TmpOrbitW);
                        HOperatorSet.GenCircle(out hoCircleOuter, hvOrbitOuterCircleRow, hvOrbitOuterCircleCol, hvOrbitOuterCircleRad);

                        HOperatorSet.GenRectangle1(out hoRect0,
                                                   hvOrbitOuterCircleRow + (hvOrbitOuterCircleRad - TmpOrbitW), hvOrbitOuterCircleCol - hvOrbitOuterCircleRad,
                                                   hvOrbitOuterCircleRow + hvOrbitOuterCircleRad, hvOrbitOuterCircleCol + hvOrbitOuterCircleRad);

                        HOperatorSet.GenRectangle1(out hoRect1,
                                                   hvOrbitOuterCircleRow - hvOrbitOuterCircleRad, hvOrbitOuterCircleCol - hvOrbitOuterCircleRad,
                                                   hvOrbitOuterCircleRow - (hvOrbitOuterCircleRad - TmpOrbitW), hvOrbitOuterCircleCol + hvOrbitOuterCircleRad);

                        HOperatorSet.Difference(hoCircleOuter, hoCircleInter, out hoRing);
                        HOperatorSet.Union2(hoRing, hoRect0, out hoOrbitMask);
                        HOperatorSet.Union2(hoOrbitMask, hoRect1, out hoTmp);
                        ReplaceHobject(ref hoOrbitMask, ref hoTmp);
                        HOperatorSet.ZoomRegion(hoOrbitMask, out hoTmp, hvAccelerationFactor, hvAccelerationFactor);
                        ReplaceHobject(ref hoOrbitMask, ref hoTmp);
                    }
                    else
                    {
                        HOperatorSet.GenRectangle1(out hoOrbitMask, 0, 0, hvImageOriH, hvImageOriW);

                        hvOrbitOuterCircleRow = hvFallbackOrbitOuterRow / hvAccelerationFactor;
                        hvOrbitOuterCircleCol = hvFallbackOrbitOuterCol / hvAccelerationFactor;
                        hvOrbitOuterCircleRad = hvFallbackOrbitOuterRad / hvAccelerationFactor;
                    }

                    // 翘钉基准面掩码
                    if (hvNailCircleParam.Length > 0 && hasOrbitOuterFit)
                    {
                        HOperatorSet.GenCircle(out hoNailCircle, hvCoarseCenterRow, hvCoarseCenterCol, hvCoarseRadius);
                        HOperatorSet.GenCircle(out hoBaseCircleOuter,
                                               hvOrbitOuterCircleRow * hvAccelerationFactor, hvOrbitOuterCircleCol * hvAccelerationFactor,
                                               hvOrbitOuterCircleRad * hvAccelerationFactor);

                        HOperatorSet.Difference(hoNailCircle, hoBaseCircleOuter, out hoNailWarpBaseMask);
                        HOperatorSet.Intersection(_hoValidMask, hoNailWarpBaseMask, out hoTmp);
                        ReplaceHobject(ref hoNailWarpBaseMask, ref hoTmp);
                    }
                    else
                    {
                        HOperatorSet.GenCircle(out hoTmp1,
                                               hvOrbitOuterCircleRow * hvAccelerationFactor, hvOrbitOuterCircleCol * hvAccelerationFactor,
                                               hvOrbitOuterCircleRad * hvAccelerationFactor * 1.55);
                        HOperatorSet.Difference(_hoValidMask, hoTmp1, out hoNailWarpBaseMask);
                    }
                    HOperatorSet.Difference(hoNailWarpBaseMask, hoOrbitMask, out hoTmp);
                    ReplaceHobject(ref hoNailWarpBaseMask, ref hoTmp);

                    HTuple scaleX, scaleY;
                    int imageNum = _imageData.Count;
                    if (imageNum > 0)
                    {
                        if (_imageData[0].hvIntervalX > _imageData[0].hvIntervalY)
                        {
                            scaleX = _imageData[0].hvIntervalX / _imageData[0].hvIntervalY;
                            scaleY = 1;
                        }
                        else
                        {
                            scaleX = 1;
                            scaleY = _imageData[0].hvIntervalY / _imageData[0].hvIntervalX;
                        }
                    }
                    else
                    {
                        scaleX = 1;
                        scaleY = 1;
                    }

                    HOperatorSet.Intersection(_hoValidMask, hoOrbitMask, out hoTmp);
                    ReplaceHobject(ref hoOrbitMask, ref hoTmp);
                    HOperatorSet.ZoomRegion(hoOrbitMask, out hoTmp, 1 / scaleX, 1 / scaleY);
                    ReplaceHobject(ref hoOrbitMask, ref hoTmp);

                    _hoNailCenterModel.Dispose();
                    _hoOrbitMask.Dispose();
                    _hoNailWarpBaseMask.Dispose();
                    _hoNailCenterModel = hoNailCenterModel.Clone();
                    _hoOrbitMask = hoOrbitMask.Clone();
                    _hoNailWarpBaseMask = hoNailWarpBaseMask.Clone();
                    _nailCenterIsTrue = nailCenterIsTrue;

                    state = 0;
                }
                catch (Exception ex)
                {
                    Logs.LogError($"{DateTime.Now}:GetNailCenterAndOrbitMask()报错信息:{ex.Message},调用堆栈:{ex.StackTrace}");
                    Console.WriteLine(ex.StackTrace);
                }
                finally
                {
                    try
                    {
                        if (hvOrbitInterHandle.Length > 0)
                            HOperatorSet.ClearMetrologyModel(hvOrbitInterHandle);
                    }
                    catch { }

                    try
                    {
                        if (hvOrbitOuterHandle.Length > 0)
                            HOperatorSet.ClearMetrologyModel(hvOrbitOuterHandle);
                    }
                    catch { }

                    try
                    {
                        if (hvCoarseHandle.Length > 0)
                            HOperatorSet.ClearMetrologyModel(hvCoarseHandle);
                    }
                    catch { }

                    hoCoarseNailCircleRegion.Dispose();
                    hoTemplateRegion.Dispose();
                    hoImageResize.Dispose();
                    hoImageResizeMean.Dispose();
                    hoROI_0.Dispose();
                    hoImageReduced.Dispose();
                    hoImageReducedMean.Dispose();
                    hoInvalidROI_0.Dispose();
                    hoOrbitOuterMeasures.Dispose();
                    hoOrbitOuterFitContour.Dispose();
                    hoOrbitOuterContour.Dispose();
                    hoOrbitOuterContourRegion.Dispose();
                    hoOrbitOuterContourImage.Dispose();
                    hoNailCenterModel.Dispose();
                    hoOrbitMask.Dispose();
                    hoNailWarpBaseMask.Dispose();
                    hoCircleInter.Dispose();
                    hoCircleOuter.Dispose();
                    hoRect0.Dispose();
                    hoRect1.Dispose();
                    hoRing.Dispose();
                    hoNailCircle.Dispose();
                    hoBaseCircleOuter.Dispose();
                    hoTmp.Dispose();
                    hoTmp1.Dispose();
                }

                return state;
            }
        }

        /// <summary>
        /// 定位密封钉中心(亿纬老工艺)
        /// </summary>
        public int GetNailCenterAndOrbitMaskV2(HObject hoImage, HObject hoHeightImage, out HTuple hvNailCx, out HTuple hvNailCy, out HTuple hvOrbitOuterParam)
        {
            using (var dh = new HDevDisposeHelper())
            {
                int state = -1;

                hvNailCx = new HTuple();
                hvNailCy = new HTuple();
                hvOrbitOuterParam = new HTuple();

                HTuple hvIndex;
                HTuple hvModelAngle, hvModelScore, hvModelRadius;
                HTuple hvOrbitInterHandle = new HTuple();
                HTuple hvOrbitOuterHandle = new HTuple();

                HObject hoOrbitOuterCircleRegion = new HObject();
                HObject hoTemplateRegion = new HObject();
                HObject hoImageResize = new HObject();
                HObject hoImageResizeMean = new HObject();
                HObject hoOrbitOuterMeasures = new HObject();
                HObject hoOrbitOuterFitContour = new HObject();
                HObject hoOrbitOuterContour = new HObject();
                HObject hoROI_0 = new HObject();
                HObject hoImageReduced = new HObject();
                HObject hoImageReducedMean = new HObject();
                HObject hoInvalidROI_0 = new HObject();
                HObject hoOrbitOuterContourRegion = new HObject();
                HObject hoOrbitOuterContourImage = new HObject();
                HObject hoNailCenterModel = new HObject();
                HObject hoOrbitMask = new HObject();
                HObject hoNailWarpBaseMask = new HObject();
                HObject hoCircleInter = new HObject();
                HObject hoCircleOuter = new HObject();
                HObject hoRect0 = new HObject();
                HObject hoRect1 = new HObject();
                HObject hoRing = new HObject();
                HObject hoTmp = new HObject();
                HObject hoTmp1 = new HObject();

                HOperatorSet.GenEmptyObj(out hoOrbitOuterCircleRegion);
                HOperatorSet.GenEmptyObj(out hoTemplateRegion);
                HOperatorSet.GenEmptyObj(out hoImageResize);
                HOperatorSet.GenEmptyObj(out hoImageResizeMean);
                HOperatorSet.GenEmptyObj(out hoOrbitOuterMeasures);
                HOperatorSet.GenEmptyObj(out hoOrbitOuterFitContour);
                HOperatorSet.GenEmptyObj(out hoOrbitOuterContour);
                HOperatorSet.GenEmptyObj(out hoROI_0);
                HOperatorSet.GenEmptyObj(out hoImageReduced);
                HOperatorSet.GenEmptyObj(out hoImageReducedMean);
                HOperatorSet.GenEmptyObj(out hoInvalidROI_0);
                HOperatorSet.GenEmptyObj(out hoOrbitOuterContourRegion);
                HOperatorSet.GenEmptyObj(out hoOrbitOuterContourImage);
                HOperatorSet.GenEmptyObj(out hoNailCenterModel);
                HOperatorSet.GenEmptyObj(out hoOrbitMask);
                HOperatorSet.GenEmptyObj(out hoNailWarpBaseMask);
                HOperatorSet.GenEmptyObj(out hoCircleInter);
                HOperatorSet.GenEmptyObj(out hoCircleOuter);
                HOperatorSet.GenEmptyObj(out hoRect0);
                HOperatorSet.GenEmptyObj(out hoRect1);
                HOperatorSet.GenEmptyObj(out hoRing);
                HOperatorSet.GenEmptyObj(out hoTmp);
                HOperatorSet.GenEmptyObj(out hoTmp1);

                bool nailCenterIsTrue = false;

                try
                {
                    HOperatorSet.GetImageSize(hoImage, out HTuple hvImageOriW, out HTuple hvImageOriH);
                    HOperatorSet.GetNccModelRegion(out hoTemplateRegion, _hvNailCenterModelID);
                    HOperatorSet.RegionFeatures(hoTemplateRegion, "outer_radius", out hvModelRadius);

                    // 定位焊缝外圈圆，密封钉中心粗定位
                    HTuple hvAccelerationFactor = 4.0f;
                    HTuple hvScaleFactorW = 1.0f / hvAccelerationFactor;
                    HTuple hvScaleFactorH = 1.0f / hvAccelerationFactor;
                    HOperatorSet.ZoomImageFactor(hoImage, out hoImageResize, hvScaleFactorW, hvScaleFactorH, "constant");
                    HOperatorSet.GetImageSize(hoImageResize, out HTuple hvResizeWidth, out HTuple hvResizeHeight);

                    HTuple hvResizeImageCenterRow = hvResizeHeight / 2.0f;
                    HTuple hvResizeImageCenterCol = hvResizeWidth / 2.0f;
                    HTuple hvTmpRadius = (hvResizeImageCenterRow + hvResizeImageCenterCol) / 2.0f;

                    HOperatorSet.CreateMetrologyModel(out hvOrbitOuterHandle);
                    HOperatorSet.AddMetrologyObjectCircleMeasure(hvOrbitOuterHandle, hvResizeImageCenterRow, hvResizeImageCenterCol,
                                                                 hvTmpRadius * 0.6, hvTmpRadius * 0.55, 5, 5, 10, new HTuple(), new HTuple(), out hvIndex);
                    HOperatorSet.SetMetrologyObjectParam(hvOrbitOuterHandle, 0, "measure_transition", "positive");
                    HOperatorSet.SetMetrologyObjectParam(hvOrbitOuterHandle, "all", "min_score", 0.01);
                    HOperatorSet.SetMetrologyObjectParam(hvOrbitOuterHandle, "all", "measure_select", "last");
                    HOperatorSet.MeanImage(hoImageResize, out hoImageResizeMean, 11, 11);
                    HOperatorSet.ApplyMetrologyModel(hoImageResizeMean, hvOrbitOuterHandle);
                    HOperatorSet.GetMetrologyObjectMeasures(out hoOrbitOuterMeasures, hvOrbitOuterHandle, "all", "all", out HTuple hvPointRow, out HTuple hvPointCol);

                    HTuple TmpCircleRow = new HTuple();
                    HTuple TmpCircleColumn = new HTuple();
                    HTuple TmpCircleRadius = new HTuple();
                    if (hvPointRow.Length > 2)
                    {
                        HOperatorSet.GenContourPolygonXld(out hoOrbitOuterFitContour, hvPointRow, hvPointCol);
                        try
                        {
                            HOperatorSet.FitCircleContourXld(hoOrbitOuterFitContour, "geotukey", -1, 0, 0, 3, 2,
                                                             out TmpCircleRow, out TmpCircleColumn, out TmpCircleRadius,
                                                             out _, out _, out _);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"WARN:failed to fit orbit outer-circle contour: {ex.Message}");
                        }
                    }
                    bool hasOrbitOuterFit = TmpCircleRow.Length > 0;
                    HTuple hvFallbackOrbitOuterRow = hvImageOriH / 2.0f;
                    HTuple hvFallbackOrbitOuterCol = hvImageOriW / 2.0f;
                    HOperatorSet.TupleMin2(hvFallbackOrbitOuterRow, hvFallbackOrbitOuterCol, out HTuple hvFallbackOrbitOuterRad);

                    if (hasOrbitOuterFit)
                    {
                        HOperatorSet.TupleConcat(hvOrbitOuterParam, TmpCircleRow * hvAccelerationFactor, out hvOrbitOuterParam);
                        HOperatorSet.TupleConcat(hvOrbitOuterParam, TmpCircleColumn * hvAccelerationFactor, out hvOrbitOuterParam);
                        HOperatorSet.TupleConcat(hvOrbitOuterParam, TmpCircleRadius * hvAccelerationFactor, out hvOrbitOuterParam);

                        HOperatorSet.GenCircleContourXld(out hoOrbitOuterContour, TmpCircleRow, TmpCircleColumn, TmpCircleRadius,
                                                         (new HTuple(0)).TupleRad(), (new HTuple(360)).TupleRad(), "positive", 1);
                    }
                    else
                    {
                        HOperatorSet.TupleConcat(hvOrbitOuterParam, hvFallbackOrbitOuterRow, out hvOrbitOuterParam);
                        HOperatorSet.TupleConcat(hvOrbitOuterParam, hvFallbackOrbitOuterCol, out hvOrbitOuterParam);
                        HOperatorSet.TupleConcat(hvOrbitOuterParam, hvFallbackOrbitOuterRad, out hvOrbitOuterParam);

                        Console.WriteLine("WARN:fallback to the full-image outer-circle parameters because orbit outer-circle fitting failed.");
                    }

                    // 焊迹掩码
                    HTuple hvOrbitOuterRow, hvOrbitOuterCol, hvOrbitOuterRadius;
                    if (hvOrbitOuterParam.Length > 0)
                    {
                        hvOrbitOuterRow = hvOrbitOuterParam.TupleSelect(0);
                        hvOrbitOuterCol = hvOrbitOuterParam.TupleSelect(1);
                        hvOrbitOuterRadius = hvOrbitOuterParam.TupleSelect(2);

                        HOperatorSet.GenCircle(out hoOrbitOuterCircleRegion, hvOrbitOuterRow, hvOrbitOuterCol, hvOrbitOuterRadius);
                    }
                    else
                    {
                        hvOrbitOuterRow = hvImageOriH / 2.0f;
                        hvOrbitOuterCol = hvImageOriW / 2.0f;
                        hvOrbitOuterRadius = Math.Min(hvImageOriH, hvImageOriW) * 0.5;
                    }

                    // 精定位密封钉中心
                    HOperatorSet.GenCircle(out hoROI_0, hvOrbitOuterRow, hvOrbitOuterCol, hvOrbitOuterRadius * 0.25);
                    HOperatorSet.ReduceDomain(hoHeightImage, hoROI_0, out hoImageReduced);
                    HOperatorSet.Intersection(hoROI_0, _hoIrregularMask, out hoInvalidROI_0);
                    HOperatorSet.PaintRegion(hoInvalidROI_0, hoImageReduced, out hoTmp, _hvHeightImageGlobalMinValue, "fill");
                    ReplaceHobject(ref hoImageReduced, ref hoTmp);
                    HOperatorSet.ScaleImageMax(hoImageReduced, out hoTmp);
                    ReplaceHobject(ref hoImageReduced, ref hoTmp);
                    HOperatorSet.MeanImage(hoImageReduced, out hoImageReducedMean, 5, 5);
                    HOperatorSet.FindNccModel(hoImageReducedMean, _hvNailCenterModelID, -0.39, 0.79, 0.65, 1, 0.5, "true", 0,
                                                out hvNailCy, out hvNailCx, out hvModelAngle, out hvModelScore);

                    if (hvModelScore.Length > 0)
                    {
                        HOperatorSet.GenCircle(out hoNailCenterModel, hvNailCy, hvNailCx, hvModelRadius);
                        nailCenterIsTrue = true;
                    }
                    else
                    {
                        hvNailCx.Dispose();
                        hvNailCy.Dispose();
                        hvNailCx = hvOrbitOuterCol.Clone();
                        hvNailCy = hvOrbitOuterRow.Clone();
                        HOperatorSet.GenCircle(out hoNailCenterModel, hvNailCy, hvNailCx, hvModelRadius);
                        nailCenterIsTrue = true;
                    }

                    HTuple hvOrbitOuterCircleRow, hvOrbitOuterCircleCol, hvOrbitOuterCircleRad;
                    HTuple hvOrbitInterCircleRow, hvOrbitInterCircleCol, hvOrbitInterCircleRad;
                    if (hasOrbitOuterFit)
                    {
                        HTuple hvOrbitInterParam;
                        HTuple hvCoverCircleParam;

                        hvOrbitOuterCircleRow = TmpCircleRow;
                        hvOrbitOuterCircleCol = TmpCircleColumn;
                        hvOrbitOuterCircleRad = TmpCircleRadius;

                        double orbitOuterCircleRowValue = hvOrbitOuterCircleRow.D;
                        double orbitOuterCircleColValue = hvOrbitOuterCircleCol.D;
                        double orbitOuterCircleRadValue = hvOrbitOuterCircleRad.D;
                        bool canLocateOrbitInterCircle = orbitOuterCircleRadValue > 0
                                                        && orbitOuterCircleRowValue >= 0
                                                        && orbitOuterCircleRowValue < hvResizeHeight.D
                                                        && orbitOuterCircleColValue >= 0
                                                        && orbitOuterCircleColValue < hvResizeWidth.D;

                        if (canLocateOrbitInterCircle)
                        {
                            HOperatorSet.GenRegionContourXld(hoOrbitOuterContour, out hoOrbitOuterContourRegion, "filled");
                            HOperatorSet.ReduceDomain(hoImageResizeMean, hoOrbitOuterContourRegion, out hoOrbitOuterContourImage);
                            HOperatorSet.CreateMetrologyModel(out hvOrbitInterHandle);
                            HOperatorSet.AddMetrologyObjectCircleMeasure(hvOrbitInterHandle, hvOrbitOuterCircleRow, hvOrbitOuterCircleCol,
                                                                         hvOrbitOuterCircleRad * 0.5, hvOrbitOuterCircleRad * 0.45,
                                                                         5, 28, 30, new HTuple(), new HTuple(), out HTuple hvIndex0);
                            HOperatorSet.AddMetrologyObjectCircleMeasure(hvOrbitInterHandle, hvOrbitOuterCircleRow, hvOrbitOuterCircleCol,
                                                                         hvOrbitOuterCircleRad * 0.5, hvOrbitOuterCircleRad * 0.45,
                                                                         5, 28, 30, new HTuple(), new HTuple(), out HTuple hvIndex1);
                            HOperatorSet.SetMetrologyObjectParam(hvOrbitInterHandle, hvIndex0, "measure_transition", "negative");
                            HOperatorSet.SetMetrologyObjectParam(hvOrbitInterHandle, hvIndex1, "measure_transition", "positive");
                            HOperatorSet.SetMetrologyObjectParam(hvOrbitInterHandle, "all", "min_score", 0.01);
                            HOperatorSet.SetMetrologyObjectParam(hvOrbitInterHandle, hvIndex0, "measure_select", "last");
                            HOperatorSet.SetMetrologyObjectParam(hvOrbitInterHandle, hvIndex1, "measure_select", "first");

                            HOperatorSet.ApplyMetrologyModel(hoOrbitOuterContourImage, hvOrbitInterHandle);
                            HOperatorSet.GetMetrologyObjectResult(hvOrbitInterHandle, hvIndex0, "all", "result_type", "all_param", out hvOrbitInterParam);
                            HOperatorSet.GetMetrologyObjectResult(hvOrbitInterHandle, hvIndex1, "all", "result_type", "all_param", out hvCoverCircleParam);

                            if (hvOrbitInterParam.TupleLength() >= 3
                                && hvOrbitInterParam[2].D > 0
                                && hvOrbitInterParam[2].D < orbitOuterCircleRadValue)
                            {
                                hvOrbitInterCircleRow = hvOrbitInterParam[0];
                                hvOrbitInterCircleCol = hvOrbitInterParam[1];
                                hvOrbitInterCircleRad = hvOrbitInterParam[2];
                            }
                            else
                            {
                                hvOrbitInterCircleRow = hvOrbitOuterCircleRow;
                                hvOrbitInterCircleCol = hvOrbitOuterCircleCol;
                                hvOrbitInterCircleRad = hvOrbitOuterCircleRad * 0.656;

                                Console.WriteLine("WARN:fallback to the default inner-circle radius because inner-circle metrology failed.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("WARN:skip orbit inner-circle metrology because the fitted outer-circle center is outside the resized image.");
                            hvOrbitInterCircleRow = hvOrbitOuterCircleRow;
                            hvOrbitInterCircleCol = hvOrbitOuterCircleCol;
                            hvOrbitInterCircleRad = hvOrbitOuterCircleRad * 0.656;
                        }

                        if (hvOrbitInterCircleRad.D <= 0 || hvOrbitInterCircleRad.D >= hvOrbitOuterCircleRad.D)
                        {
                            Console.WriteLine("WARN:adjust invalid inner-circle radius by fallback ratio.");
                            hvOrbitInterCircleRow = hvOrbitOuterCircleRow;
                            hvOrbitInterCircleCol = hvOrbitOuterCircleCol;
                            hvOrbitInterCircleRad = hvOrbitOuterCircleRad * 0.656;
                        }

                        HTuple TmpOrbitW = hvOrbitOuterCircleRad - hvOrbitInterCircleRad;

                        HOperatorSet.GenCircle(out hoCircleInter, hvOrbitOuterCircleRow, hvOrbitOuterCircleCol, hvOrbitOuterCircleRad - TmpOrbitW);
                        HOperatorSet.GenCircle(out hoCircleOuter, hvOrbitOuterCircleRow, hvOrbitOuterCircleCol, hvOrbitOuterCircleRad);

                        HOperatorSet.GenRectangle1(out hoRect0,
                                                   hvOrbitOuterCircleRow + (hvOrbitOuterCircleRad - TmpOrbitW), hvOrbitOuterCircleCol - hvOrbitOuterCircleRad,
                                                   hvOrbitOuterCircleRow + hvOrbitOuterCircleRad, hvOrbitOuterCircleCol + hvOrbitOuterCircleRad);

                        HOperatorSet.GenRectangle1(out hoRect1,
                                                   hvOrbitOuterCircleRow - hvOrbitOuterCircleRad, hvOrbitOuterCircleCol - hvOrbitOuterCircleRad,
                                                   hvOrbitOuterCircleRow - (hvOrbitOuterCircleRad - TmpOrbitW), hvOrbitOuterCircleCol + hvOrbitOuterCircleRad);

                        HOperatorSet.Difference(hoCircleOuter, hoCircleInter, out hoRing);
                        HOperatorSet.Union2(hoRing, hoRect0, out hoOrbitMask);
                        HOperatorSet.Union2(hoOrbitMask, hoRect1, out hoTmp);
                        ReplaceHobject(ref hoOrbitMask, ref hoTmp);
                        HOperatorSet.ZoomRegion(hoOrbitMask, out hoTmp, hvAccelerationFactor, hvAccelerationFactor);
                        ReplaceHobject(ref hoOrbitMask, ref hoTmp);
                    }
                    else
                    {
                        HOperatorSet.GenRectangle1(out hoOrbitMask, 0, 0, hvImageOriH, hvImageOriW);

                        hvOrbitOuterCircleRow = hvFallbackOrbitOuterRow / hvAccelerationFactor;
                        hvOrbitOuterCircleCol = hvFallbackOrbitOuterCol / hvAccelerationFactor;
                        hvOrbitOuterCircleRad = hvFallbackOrbitOuterRad / hvAccelerationFactor;
                    }

                    // 翘钉基准面掩码
                    if (hasOrbitOuterFit)
                    {
                        HOperatorSet.GenCircle(out hoTmp1,
                                               hvOrbitOuterCircleRow * hvAccelerationFactor, hvOrbitOuterCircleCol * hvAccelerationFactor,
                                               hvOrbitOuterCircleRad * hvAccelerationFactor);
                        HOperatorSet.Difference(_hoValidMask, hoTmp1, out hoNailWarpBaseMask);
                    }
                    else
                    {
                        HOperatorSet.GenCircle(out hoTmp1,
                                               hvOrbitOuterCircleRow * hvAccelerationFactor, hvOrbitOuterCircleCol * hvAccelerationFactor,
                                               hvOrbitOuterCircleRad * hvAccelerationFactor * 1.55);
                        HOperatorSet.Difference(_hoValidMask, hoTmp1, out hoNailWarpBaseMask);
                    }
                    HOperatorSet.Difference(hoNailWarpBaseMask, hoOrbitMask, out hoTmp);
                    ReplaceHobject(ref hoNailWarpBaseMask, ref hoTmp);

                    HTuple scaleX, scaleY;
                    int imageNum = _imageData.Count;
                    if (imageNum > 0)
                    {
                        if (_imageData[0].hvIntervalX > _imageData[0].hvIntervalY)
                        {
                            scaleX = _imageData[0].hvIntervalX / _imageData[0].hvIntervalY;
                            scaleY = 1;
                        }
                        else
                        {
                            scaleX = 1;
                            scaleY = _imageData[0].hvIntervalY / _imageData[0].hvIntervalX;
                        }
                    }
                    else
                    {
                        scaleX = 1;
                        scaleY = 1;
                    }

                    HOperatorSet.Intersection(_hoValidMask, hoOrbitMask, out hoTmp);
                    ReplaceHobject(ref hoOrbitMask, ref hoTmp);
                    HOperatorSet.ZoomRegion(hoOrbitMask, out hoTmp, 1 / scaleX, 1 / scaleY);
                    ReplaceHobject(ref hoOrbitMask, ref hoTmp);

                    _hoNailCenterModel.Dispose();
                    _hoOrbitMask.Dispose();
                    _hoNailWarpBaseMask.Dispose();
                    _hoNailCenterModel = hoNailCenterModel.Clone();
                    _hoOrbitMask = hoOrbitMask.Clone();
                    _hoNailWarpBaseMask = hoNailWarpBaseMask.Clone();
                    _nailCenterIsTrue = nailCenterIsTrue;

                    state = 0;
                }
                catch (Exception ex)
                {
                    Logs.LogError($"{DateTime.Now}:GetNailCenterAndOrbitMaskV2()报错信息:{ex.Message},调用堆栈:{ex.StackTrace}");
                    Console.WriteLine(ex.StackTrace);
                }
                finally
                {
                    try
                    {
                        if (hvOrbitInterHandle.Length > 0)
                            HOperatorSet.ClearMetrologyModel(hvOrbitInterHandle);
                    }
                    catch { }

                    try
                    {
                        if (hvOrbitOuterHandle.Length > 0)
                            HOperatorSet.ClearMetrologyModel(hvOrbitOuterHandle);
                    }
                    catch { }

                    hoOrbitOuterCircleRegion.Dispose();
                    hoTemplateRegion.Dispose();
                    hoImageResize.Dispose();
                    hoImageResizeMean.Dispose();
                    hoOrbitOuterMeasures.Dispose();
                    hoOrbitOuterFitContour.Dispose();
                    hoOrbitOuterContour.Dispose();
                    hoROI_0.Dispose();
                    hoImageReduced.Dispose();
                    hoImageReducedMean.Dispose();
                    hoInvalidROI_0.Dispose();
                    hoOrbitOuterContourRegion.Dispose();
                    hoOrbitOuterContourImage.Dispose();
                    hoNailCenterModel.Dispose();
                    hoOrbitMask.Dispose();
                    hoNailWarpBaseMask.Dispose();
                    hoCircleInter.Dispose();
                    hoCircleOuter.Dispose();
                    hoRect0.Dispose();
                    hoRect1.Dispose();
                    hoRing.Dispose();
                    hoTmp.Dispose();
                    hoTmp1.Dispose();
                }

                return state;
            }
        }


        /// <summary>
        /// 定位密封钉中心(欣旺达)
        /// </summary>
        public int GetNailCenterAndOrbitMask_Sunwoda(HObject hoImage, HObject hoHeightImage, out HTuple hvNailCx, out HTuple hvNailCy, out HTuple hvOrbitOuterParam)
        {
            using (var dh = new HDevDisposeHelper())
            {
                int state = -1;

                hvNailCx = new HTuple();
                hvNailCy = new HTuple();
                hvOrbitOuterParam = new HTuple();

                HTuple hvIndex;
                HTuple hvModelAngle, hvModelScore, hvModelRadius;
                HTuple hvOrbitInterHandle = new HTuple();
                HTuple hvCoarseHandle = new HTuple();
                HTuple hvOrbitOuterHandle = new HTuple();

                HObject hoCoarseNailCircleRegion = new HObject();
                HObject hoTemplateRegion = new HObject();
                HObject hoImageResize = new HObject();
                HObject hoImageResizeMean = new HObject();
                HObject hoROI_0 = new HObject();
                HObject hoROIValid_0 = new HObject();
                HObject hoImageReduced = new HObject();
                HObject hoHeightImageReducedExpanded = new HObject();
                HObject hoImageReducedMean = new HObject();
                HObject hoImageModelCircle = new HObject();
                HObject hoInvalidROI_0 = new HObject();
                HObject hoOrbitOuterMeasures = new HObject();
                HObject hoOrbitOuterFitContour = new HObject();
                HObject hoOrbitOuterContour = new HObject();
                HObject hoOrbitOuterContourRegion = new HObject();
                HObject hoOrbitOuterContourImage = new HObject();
                HObject hoNailCenterModel = new HObject();
                HObject hoOrbitMask = new HObject();
                HObject hoNailWarpBaseMask = new HObject();
                HObject hoCircleInter = new HObject();
                HObject hoCircleOuter = new HObject();
                HObject hoRect0 = new HObject();
                HObject hoRect1 = new HObject();
                HObject hoRing = new HObject();
                HObject hoNailCircle = new HObject();
                HObject hoBaseCircleOuter = new HObject();
                HObject hoTmp = new HObject();
                HObject hoTmp1 = new HObject();

                HOperatorSet.GenEmptyObj(out hoCoarseNailCircleRegion);
                HOperatorSet.GenEmptyObj(out hoTemplateRegion);
                HOperatorSet.GenEmptyObj(out hoImageResize);
                HOperatorSet.GenEmptyObj(out hoImageResizeMean);
                HOperatorSet.GenEmptyObj(out hoROI_0);
                HOperatorSet.GenEmptyObj(out hoROIValid_0);
                HOperatorSet.GenEmptyObj(out hoImageReduced);
                HOperatorSet.GenEmptyObj(out hoHeightImageReducedExpanded);
                HOperatorSet.GenEmptyObj(out hoImageReducedMean);
                HOperatorSet.GenEmptyObj(out hoImageModelCircle);
                HOperatorSet.GenEmptyObj(out hoInvalidROI_0);
                HOperatorSet.GenEmptyObj(out hoOrbitOuterMeasures);
                HOperatorSet.GenEmptyObj(out hoOrbitOuterFitContour);
                HOperatorSet.GenEmptyObj(out hoOrbitOuterContour);
                HOperatorSet.GenEmptyObj(out hoOrbitOuterContourRegion);
                HOperatorSet.GenEmptyObj(out hoOrbitOuterContourImage);
                HOperatorSet.GenEmptyObj(out hoNailCenterModel);
                HOperatorSet.GenEmptyObj(out hoOrbitMask);
                HOperatorSet.GenEmptyObj(out hoNailWarpBaseMask);
                HOperatorSet.GenEmptyObj(out hoCircleInter);
                HOperatorSet.GenEmptyObj(out hoCircleOuter);
                HOperatorSet.GenEmptyObj(out hoRect0);
                HOperatorSet.GenEmptyObj(out hoRect1);
                HOperatorSet.GenEmptyObj(out hoRing);
                HOperatorSet.GenEmptyObj(out hoNailCircle);
                HOperatorSet.GenEmptyObj(out hoBaseCircleOuter);
                HOperatorSet.GenEmptyObj(out hoTmp);
                HOperatorSet.GenEmptyObj(out hoTmp1);

                bool nailCenterIsTrue = false;

                try
                {
                    HOperatorSet.GetImageSize(hoImage, out HTuple hvImageOriW, out HTuple hvImageOriH);
                    HOperatorSet.GetNccModelRegion(out hoTemplateRegion, _hvNailCenterModelID);
                    HOperatorSet.RegionFeatures(hoTemplateRegion, "outer_radius", out hvModelRadius);

                    // 加速系数
                    HTuple hvAccelerationFactor = 4.0f;
                    HTuple hvScaleFactorW = 1.0f / hvAccelerationFactor;
                    HTuple hvScaleFactorH = 1.0f / hvAccelerationFactor;
                    HOperatorSet.ZoomImageFactor(hoImage, out hoImageResize, hvScaleFactorW, hvScaleFactorH, "constant");
                    HOperatorSet.GetImageSize(hoImageResize, out HTuple hvResizeWidth, out HTuple hvResizeHeight);

                    if(_expendPixels > 0)
                    {
                        HOperatorSet.ExpandDomainGray(hoImageResize, out hoTmp, _expendPixels);
                        ReplaceHobject(ref hoImageResize, ref hoTmp);
                    }
                    HOperatorSet.MeanImage(hoImageResize, out hoImageResizeMean, 11, 11);

                    // 定位密封钉清洗面
                    HTuple hvResizeImageCenterRow = hvResizeHeight / 2.0f;
                    HTuple hvResizeImageCenterCol = hvResizeWidth / 2.0f;
                    HTuple hvTmpRadius = (hvResizeImageCenterRow + hvResizeImageCenterCol) / 2.0f;

                    HOperatorSet.CreateMetrologyModel(out hvCoarseHandle);
                    HOperatorSet.AddMetrologyObjectCircleMeasure(hvCoarseHandle, hvResizeImageCenterRow, hvResizeImageCenterCol,
                                                                 hvTmpRadius * 1.2, hvTmpRadius * 0.35, 5, 5, 15, new HTuple(), new HTuple(), out hvIndex);
                    HOperatorSet.SetMetrologyObjectParam(hvCoarseHandle, 0, "measure_transition", "positive");
                    HOperatorSet.SetMetrologyObjectParam(hvCoarseHandle, "all", "min_score", 0.05);
                    HOperatorSet.SetMetrologyObjectParam(hvCoarseHandle, "all", "measure_select", "first");
                    HOperatorSet.ApplyMetrologyModel(hoImageResizeMean, hvCoarseHandle);
                    HOperatorSet.GetMetrologyObjectResult(hvCoarseHandle, "all", "all", "result_type", "all_param", out HTuple hvNailCircleParam);

                    HTuple hvCoarseCenterRow, hvCoarseCenterCol, hvCoarseRadius;
                    if (hvNailCircleParam.Length > 0)
                    {
                        hvCoarseCenterRow = (hvNailCircleParam.TupleSelect(0)) * hvAccelerationFactor;
                        hvCoarseCenterCol = (hvNailCircleParam.TupleSelect(1)) * hvAccelerationFactor;
                        hvCoarseRadius = (hvNailCircleParam.TupleSelect(2)) * hvAccelerationFactor;

                        HOperatorSet.GenCircle(out hoCoarseNailCircleRegion, hvCoarseCenterRow, hvCoarseCenterCol, hvCoarseRadius);
                    }
                    else
                    {
                        hvCoarseCenterRow = hvImageOriH / 2.0f;
                        hvCoarseCenterCol = hvImageOriW / 2.0f;
                        hvCoarseRadius = Math.Min(hvImageOriH, hvImageOriW) * 0.5;
                    }

                    // 精定位密封钉中心
                    // 定位钉盖和焊缝内圈圆
                    HOperatorSet.CreateMetrologyModel(out hvOrbitInterHandle);
                    HOperatorSet.AddMetrologyObjectCircleMeasure(hvOrbitInterHandle, hvCoarseCenterRow / hvAccelerationFactor, hvCoarseCenterCol / hvAccelerationFactor,
                                                                 (hvCoarseRadius / hvAccelerationFactor) * 0.55, (hvCoarseRadius / hvAccelerationFactor) * 0.25,
                                                                 5, 5, 70, new HTuple(), new HTuple(), out HTuple hvIndex0);
                    HOperatorSet.AddMetrologyObjectCircleMeasure(hvOrbitInterHandle, hvCoarseCenterRow / hvAccelerationFactor, hvCoarseCenterCol / hvAccelerationFactor,
                                                                 (hvCoarseRadius / hvAccelerationFactor) * 0.55, (hvCoarseRadius / hvAccelerationFactor) * 0.25,
                                                                 5, 5, 70, new HTuple(), new HTuple(), out HTuple hvIndex1);
                    HOperatorSet.SetMetrologyObjectParam(hvOrbitInterHandle, hvIndex0, "measure_transition", "negative");
                    HOperatorSet.SetMetrologyObjectParam(hvOrbitInterHandle, hvIndex1, "measure_transition", "positive");
                    HOperatorSet.SetMetrologyObjectParam(hvOrbitInterHandle, "all", "min_score", 0.1);
                    HOperatorSet.SetMetrologyObjectParam(hvOrbitInterHandle, hvIndex0, "measure_select", "last");
                    HOperatorSet.SetMetrologyObjectParam(hvOrbitInterHandle, hvIndex1, "measure_select", "first");
                    HOperatorSet.ApplyMetrologyModel(hoImageResizeMean, hvOrbitInterHandle);
                    HOperatorSet.GetMetrologyObjectResult(hvOrbitInterHandle, hvIndex0, "all", "result_type", "all_param", out HTuple hvOrbitInterParam);
                    HOperatorSet.GetMetrologyObjectResult(hvOrbitInterHandle, hvIndex1, "all", "result_type", "all_param", out HTuple hvCoverCircleParam);

                    HTuple hvOrbitInterCircleRow, hvOrbitInterCircleCol, hvOrbitInterCircleRad;
                    if (hvOrbitInterParam.Length >= 3 && hvOrbitInterParam[2].D > 0)
                    {
                        hvOrbitInterCircleRow = hvOrbitInterParam[0] * hvAccelerationFactor;
                        hvOrbitInterCircleCol = hvOrbitInterParam[1] * hvAccelerationFactor;
                        hvOrbitInterCircleRad = hvOrbitInterParam[2] * hvAccelerationFactor;
                    }
                    else
                    {
                        hvOrbitInterCircleRow = hvCoarseCenterRow;
                        hvOrbitInterCircleCol = hvCoarseCenterCol;
                        hvOrbitInterCircleRad = hvCoarseRadius * 0.538;
                    }

                    HTuple hvCoverCircleRow, hvCoverCircleCol, hvCoverCircleRad;
                    if (hvCoverCircleParam.Length >= 3 && hvCoverCircleParam[2].D > 0)
                    {
                        hvCoverCircleRow = hvCoverCircleParam[0] * hvAccelerationFactor;
                        hvCoverCircleCol = hvCoverCircleParam[1] * hvAccelerationFactor;
                        hvCoverCircleRad = hvCoverCircleParam[2] * hvAccelerationFactor;
                    }
                    else if (hvOrbitInterParam.Length >= 3 && hvOrbitInterCircleRad.D > 0)
                    {
                        hvCoverCircleRow = hvOrbitInterCircleRow;
                        hvCoverCircleCol = hvOrbitInterCircleCol;
                        hvCoverCircleRad = hvOrbitInterCircleRad * 0.84;
                    }
                    else
                    {
                        hvCoverCircleRow = hvCoarseCenterRow;
                        hvCoverCircleCol = hvCoarseCenterCol;
                        hvCoverCircleRad = hvCoarseRadius * 0.538 * 0.84;
                    }

                    if (hvCoverCircleRad.D <= 0)
                    {
                        hvCoverCircleRow = hvCoarseCenterRow;
                        hvCoverCircleCol = hvCoarseCenterCol;
                        hvCoverCircleRad = hvCoarseRadius * 0.538 * 0.84;
                    }

                    HOperatorSet.GenCircle(out hoROI_0, hvCoverCircleRow, hvCoverCircleCol, hvCoverCircleRad * 0.25);
                    HOperatorSet.Difference(hoROI_0, _hoIrregularMask, out hoROIValid_0);
                    HOperatorSet.AreaCenter(hoROIValid_0, out HTuple hvROIValidArea, out _, out _);
                    if (hvROIValidArea.Length == 0 || hvROIValidArea.D <= 0)
                    {
                        HOperatorSet.GenCircle(out hoTmp, hvCoverCircleRow, hvCoverCircleCol, hvCoverCircleRad * 0.25);
                        ReplaceHobject(ref hoROIValid_0, ref hoTmp);
                    }
                    HOperatorSet.ReduceDomain(hoHeightImage, hoROIValid_0, out hoImageReduced);
                    HOperatorSet.ExpandDomainGray(hoImageReduced, out hoHeightImageReducedExpanded, hvModelRadius);
                    HOperatorSet.ReduceDomain(hoHeightImageReducedExpanded, hoROI_0, out hoTmp);
                    ReplaceHobject(ref hoHeightImageReducedExpanded, ref hoTmp);
                    HOperatorSet.ScaleImageMax(hoHeightImageReducedExpanded, out hoTmp);
                    ReplaceHobject(ref hoImageReduced, ref hoTmp);
                    HOperatorSet.MeanImage(hoImageReduced, out hoImageModelCircle, 25, 25);
                    HOperatorSet.FindNccModel(hoImageModelCircle, _hvNailCenterModelID, -0.39, 0.79, 0.5, 1, 0.65, "true", 0,
                                                out hvNailCy, out hvNailCx, out hvModelAngle, out hvModelScore);

                    if (hvModelScore.Length > 0)
                    {
                        HOperatorSet.GenCircle(out hoNailCenterModel, hvNailCy, hvNailCx, hvModelRadius);
                        nailCenterIsTrue = true;
                    }
                    else
                    {
                        hvNailCx.Dispose();
                        hvNailCy.Dispose();
                        hvNailCx = hvCoverCircleCol.Clone();
                        hvNailCy = hvCoverCircleRow.Clone();
                        HOperatorSet.GenCircle(out hoNailCenterModel, hvNailCy, hvNailCx, hvModelRadius);
                        nailCenterIsTrue = true;
                    }

                    HTuple hvL1 = (hvCoarseRadius - hvOrbitInterCircleRad) / hvAccelerationFactor;
                    if (hvL1.D <= 0)
                    {
                        hvOrbitInterCircleRow = hvCoarseCenterRow;
                        hvOrbitInterCircleCol = hvCoarseCenterCol;
                        hvOrbitInterCircleRad = hvCoarseRadius * 0.538;
                        hvL1 = (hvCoarseRadius - hvOrbitInterCircleRad) / hvAccelerationFactor;
                    }
                    HTuple hvR1 = hvOrbitInterCircleRad / hvAccelerationFactor + hvL1 * 0.5;

                    HOperatorSet.CreateMetrologyModel(out hvOrbitOuterHandle);
                    HOperatorSet.AddMetrologyObjectCircleMeasure(hvOrbitOuterHandle, hvOrbitInterCircleRow / hvAccelerationFactor, hvOrbitInterCircleCol / hvAccelerationFactor,
                                                                 hvR1, hvL1 * 0.3, 5, 15, 20, new HTuple(), new HTuple(), out hvIndex);
                    HOperatorSet.SetMetrologyObjectParam(hvOrbitOuterHandle, 0, "measure_transition", "negative");
                    HOperatorSet.SetMetrologyObjectParam(hvOrbitOuterHandle, "all", "min_score", 0.1);
                    HOperatorSet.SetMetrologyObjectParam(hvOrbitOuterHandle, "all", "measure_select", "last");
                    HOperatorSet.ApplyMetrologyModel(hoImageResizeMean, hvOrbitOuterHandle);
                    HOperatorSet.GetMetrologyObjectMeasures(out hoOrbitOuterMeasures, hvOrbitOuterHandle, "all", "all", out _, out _);
                    HOperatorSet.GetMetrologyObjectResult(hvOrbitOuterHandle, "all", "all", "result_type", "all_param", out HTuple hvOrbitOuterParamResize);

                    bool hasOrbitOuterParam = hvOrbitOuterParamResize.Length >= 3 && hvOrbitOuterParamResize[2].D > 0;
                    HTuple hvFallbackOrbitOuterRow = hvImageOriH * 0.5;
                    HTuple hvFallbackOrbitOuterCol = hvImageOriW * 0.5;
                    HOperatorSet.TupleMin2(hvFallbackOrbitOuterRow, hvFallbackOrbitOuterCol, out HTuple hvFallbackOrbitOuterRad);

                    HTuple hvOrbitOuterCircleRow, hvOrbitOuterCircleCol, hvOrbitOuterCircleRad;
                    if (hasOrbitOuterParam)
                    {
                        hvOrbitOuterCircleRow = hvOrbitOuterParamResize[0] * hvAccelerationFactor;
                        hvOrbitOuterCircleCol = hvOrbitOuterParamResize[1] * hvAccelerationFactor;
                        hvOrbitOuterCircleRad = hvOrbitOuterParamResize[2] * hvAccelerationFactor;

                        HOperatorSet.TupleConcat(hvOrbitOuterParam, hvOrbitOuterCircleRow, out hvOrbitOuterParam);
                        HOperatorSet.TupleConcat(hvOrbitOuterParam, hvOrbitOuterCircleCol, out hvOrbitOuterParam);
                        HOperatorSet.TupleConcat(hvOrbitOuterParam, hvOrbitOuterCircleRad, out hvOrbitOuterParam);
                    }
                    else
                    {
                        hvOrbitOuterCircleRow = hvFallbackOrbitOuterRow.Clone();
                        hvOrbitOuterCircleCol = hvFallbackOrbitOuterCol.Clone();
                        hvOrbitOuterCircleRad = hvFallbackOrbitOuterRad.Clone();

                        HOperatorSet.TupleConcat(hvOrbitOuterParam, hvOrbitOuterCircleRow, out hvOrbitOuterParam);
                        HOperatorSet.TupleConcat(hvOrbitOuterParam, hvOrbitOuterCircleCol, out hvOrbitOuterParam);
                        HOperatorSet.TupleConcat(hvOrbitOuterParam, hvOrbitOuterCircleRad, out hvOrbitOuterParam);
                    }

                    if (hasOrbitOuterParam)
                    {
                        HTuple TmpOrbitW = hvOrbitOuterCircleRad - hvOrbitInterCircleRad;
                        if (TmpOrbitW.D <= 0)
                        {
                            hvOrbitInterCircleRad = hvOrbitOuterCircleRad * 0.656;
                            TmpOrbitW = hvOrbitOuterCircleRad - hvOrbitInterCircleRad;
                        }

                        HOperatorSet.GenCircle(out hoCircleInter, hvOrbitOuterCircleRow, hvOrbitOuterCircleCol, hvOrbitOuterCircleRad - TmpOrbitW);
                        HOperatorSet.GenCircle(out hoCircleOuter, hvOrbitOuterCircleRow, hvOrbitOuterCircleCol, hvOrbitOuterCircleRad);

                        HOperatorSet.GenRectangle1(out hoRect0,
                                                   hvOrbitOuterCircleRow + (hvOrbitOuterCircleRad - TmpOrbitW), hvOrbitOuterCircleCol - hvOrbitOuterCircleRad,
                                                   hvOrbitOuterCircleRow + hvOrbitOuterCircleRad, hvOrbitOuterCircleCol + hvOrbitOuterCircleRad);

                        HOperatorSet.GenRectangle1(out hoRect1,
                                                   hvOrbitOuterCircleRow - hvOrbitOuterCircleRad, hvOrbitOuterCircleCol - hvOrbitOuterCircleRad,
                                                   hvOrbitOuterCircleRow - (hvOrbitOuterCircleRad - TmpOrbitW), hvOrbitOuterCircleCol + hvOrbitOuterCircleRad);

                        HOperatorSet.Difference(hoCircleOuter, hoCircleInter, out hoRing);
                        HOperatorSet.Union2(hoRing, hoRect0, out hoOrbitMask);
                        HOperatorSet.Union2(hoOrbitMask, hoRect1, out hoTmp);
                        ReplaceHobject(ref hoOrbitMask, ref hoTmp);
                    }
                    else
                    {
                        HOperatorSet.GenRectangle1(out hoOrbitMask, 0, 0, hvImageOriH, hvImageOriW);
                    }

                    if (hvNailCircleParam.Length > 0 && hasOrbitOuterParam)
                    {
                        HOperatorSet.GenCircle(out hoNailCircle, hvCoarseCenterRow, hvCoarseCenterCol, hvCoarseRadius);
                        HOperatorSet.GenCircle(out hoBaseCircleOuter, hvOrbitOuterCircleRow, hvOrbitOuterCircleCol, hvOrbitOuterCircleRad);
                        HOperatorSet.Difference(hoNailCircle, hoBaseCircleOuter, out hoNailWarpBaseMask);
                        HOperatorSet.Intersection(_hoValidMask, hoNailWarpBaseMask, out hoTmp);
                        ReplaceHobject(ref hoNailWarpBaseMask, ref hoTmp);
                    }
                    else
                    {
                        HOperatorSet.GenCircle(out hoTmp1, hvOrbitOuterCircleRow, hvOrbitOuterCircleCol, hvOrbitOuterCircleRad * 1.55);
                        HOperatorSet.Difference(_hoValidMask, hoTmp1, out hoNailWarpBaseMask);
                    }
                    HOperatorSet.Difference(hoNailWarpBaseMask, hoOrbitMask, out hoTmp);
                    ReplaceHobject(ref hoNailWarpBaseMask, ref hoTmp);
                    HTuple scaleX, scaleY;
                    int imageNum = _imageData.Count;
                    if (imageNum > 0)
                    {
                        if (_imageData[0].hvIntervalX > _imageData[0].hvIntervalY)
                        {
                            scaleX = _imageData[0].hvIntervalX / _imageData[0].hvIntervalY;
                            scaleY = 1;
                        }
                        else
                        {
                            scaleX = 1;
                            scaleY = _imageData[0].hvIntervalY / _imageData[0].hvIntervalX;
                        }
                    }
                    else
                    {
                        scaleX = 1;
                        scaleY = 1;
                    }

                    HOperatorSet.Intersection(_hoValidMask, hoOrbitMask, out hoTmp);
                    ReplaceHobject(ref hoOrbitMask, ref hoTmp);
                    HOperatorSet.ZoomRegion(hoOrbitMask, out hoTmp, 1 / scaleX, 1 / scaleY);
                    ReplaceHobject(ref hoOrbitMask, ref hoTmp);

                    _hoNailCenterModel.Dispose();
                    _hoOrbitMask.Dispose();
                    _hoNailWarpBaseMask.Dispose();
                    _hoNailCenterModel = hoNailCenterModel.Clone();
                    _hoOrbitMask = hoOrbitMask.Clone();
                    _hoNailWarpBaseMask = hoNailWarpBaseMask.Clone();
                    _nailCenterIsTrue = nailCenterIsTrue;

                    state = 0;
                }
                catch (Exception ex)
                {
                    Logs.LogError($"{DateTime.Now}:GetNailCenterAndOrbitMask()报错信息:{ex.Message},调用堆栈:{ex.StackTrace}");
                    Console.WriteLine(ex.StackTrace);
                }
                finally
                {
                    try
                    {
                        if (hvOrbitInterHandle.Length > 0)
                            HOperatorSet.ClearMetrologyModel(hvOrbitInterHandle);
                    }
                    catch { }

                    try
                    {
                        if (hvOrbitOuterHandle.Length > 0)
                            HOperatorSet.ClearMetrologyModel(hvOrbitOuterHandle);
                    }
                    catch { }

                    try
                    {
                        if (hvCoarseHandle.Length > 0)
                            HOperatorSet.ClearMetrologyModel(hvCoarseHandle);
                    }
                    catch { }

                    hoCoarseNailCircleRegion.Dispose();
                    hoTemplateRegion.Dispose();
                    hoImageResize.Dispose();
                    hoImageResizeMean.Dispose();
                    hoROI_0.Dispose();
                    hoROIValid_0.Dispose();
                    hoImageReduced.Dispose();
                    hoHeightImageReducedExpanded.Dispose();
                    hoImageReducedMean.Dispose();
                    hoImageModelCircle.Dispose();
                    hoInvalidROI_0.Dispose();
                    hoOrbitOuterMeasures.Dispose();
                    hoOrbitOuterFitContour.Dispose();
                    hoOrbitOuterContour.Dispose();
                    hoOrbitOuterContourRegion.Dispose();
                    hoOrbitOuterContourImage.Dispose();
                    hoNailCenterModel.Dispose();
                    hoOrbitMask.Dispose();
                    hoNailWarpBaseMask.Dispose();
                    hoCircleInter.Dispose();
                    hoCircleOuter.Dispose();
                    hoRect0.Dispose();
                    hoRect1.Dispose();
                    hoRing.Dispose();
                    hoNailCircle.Dispose();
                    hoBaseCircleOuter.Dispose();
                    hoTmp.Dispose();
                    hoTmp1.Dispose();
                }

                return state;
            }
        }



    }
}
