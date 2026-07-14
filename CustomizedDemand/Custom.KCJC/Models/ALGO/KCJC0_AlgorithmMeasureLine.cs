using HalconDotNet;
using OpenCvSharp;
using OpenCvSharp.Flann;
using ReeYin_V.Core.Extension;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace Custom.KCJC.Models.ALGO
{
    public class KCJC0_AlgorithmMeasureLine : KCJC0_Algorithm
    {
        private bool _disposed = false;

        private HObject _hoFitEtchingRegionStartEdge = new HObject();   // 拟合出的刻蚀区扫描起始边缘
        private HObject _hoFitEtchingRegionEndEdge = new HObject();     // 拟合出的刻蚀区扫描结束边缘
        private HObject _hoFitEtchingRegionTopEdge = new HObject();     // 拟合出的刻蚀区上边缘
        private HObject _hoFitEtchingRegionBottomEdge = new HObject();  // 拟合出的刻蚀区下边缘

        private HObject _hoRefLines = new HObject();  // 刻蚀线的方向参考线

        private HTuple _hvStandardEtchingLineWidthPixel = new HTuple();            // 刻蚀线标准宽(像素)
        private HTuple _hvStandardEtchingPointDistPixel = new HTuple();            // 刻蚀线标准间距(像素)
        private HTuple _hvStandardEtchingLineDepthPixel = new HTuple();            // 刻蚀线标准深度(像素)

        private HTuple _hvMeasureCenterRow = new HTuple();      // 测量(刻蚀)区域中心行坐标
        private HTuple _hvMeasureCenterCol = new HTuple();      // 测量(刻蚀)区域中心列坐标

        private HTuple _hvPhi = new HTuple();                   // 测量(刻蚀)区域旋转角

        private HTuple _hvEtchingLineStartPointRows = new HTuple(), _hvEtchingLineStartPointCols = new HTuple();    // 极片扫描的刻蚀线起始点
        private HTuple _hvEtchingLineEndPointRows = new HTuple(), _hvEtchingLineEndPointCols = new HTuple();        // 极片扫描的刻蚀线结束点

        private HTuple _hvEtchingRegionTopEdgeRows = new HTuple(), _hvEtchingRegionTopEdgeCols = new HTuple();        // 刻蚀区域上边缘点集
        private HTuple _hvEtchingRegionBottomEdgeRows = new HTuple(), _hvEtchingRegionBottomEdgeCols = new HTuple();  // 刻蚀区域下边缘点集

        private HTuple _hvEtchingRegionLeftGapList = new HTuple();                           // 刻蚀线左侧顶点与极片左侧边缘间距的集合
        private HTuple _hvEtchingRegionRightGapList = new HTuple();                          // 刻蚀线右侧顶点与极片右侧边缘间距的集合
        private HTuple _hvEtchingRegionLeftGap = new HTuple();                               // 刻蚀区左侧与极片左边缘间距
        private HTuple _hvEtchingRegionRightGap = new HTuple();                              // 刻蚀区右侧与极片右边缘间距

        private HTuple _hvEtchingRegionTopGapList = new HTuple();                            // 刻蚀线顶部与极片顶部缘间距的集合
        private HTuple _hvEtchingRegionBottomGapList = new HTuple();                         // 刻蚀线底部与极片底部缘间距的集合
        private HTuple _hvEtchingRegionTopGap = new HTuple();                                // 刻蚀区顶部与极片顶部缘间距
        private HTuple _hvEtchingRegionBottomGap = new HTuple();                             // 刻蚀区底部与极片底部缘间距

        private HTuple _hvRefLineTopRows = new HTuple();     // 刻蚀线顶部行坐标
        private HTuple _hvRefLineTopCols = new HTuple();     // 刻蚀线顶部列坐标
        private HTuple _hvRefLineDownRows = new HTuple();    // 刻蚀线底部行坐标
        private HTuple _hvRefLineDownCols = new HTuple();    // 刻蚀线底部列坐标


        public KCJC0_AlgorithmMeasureLine()
        {
            InitVariable();
        }


        public override void Dispose()
        {
            base.Dispose();

            if (!_disposed)
            {
                _hoFitEtchingRegionStartEdge.Dispose();
                _hoFitEtchingRegionEndEdge.Dispose();
                _hoFitEtchingRegionTopEdge.Dispose();
                _hoFitEtchingRegionBottomEdge.Dispose();

                _hoRefLines.Dispose();

                _hvMeasureCenterRow.Dispose();
                _hvMeasureCenterCol.Dispose();

                _hvPhi.Dispose();

                _hvEtchingLineEndPointRows.Dispose();
                _hvEtchingLineEndPointCols.Dispose();
                _hvEtchingLineStartPointRows.Dispose();
                _hvEtchingLineStartPointCols.Dispose();

                _hvEtchingRegionTopEdgeRows.Dispose();
                _hvEtchingRegionTopEdgeCols.Dispose();
                _hvEtchingRegionBottomEdgeRows.Dispose();
                _hvEtchingRegionBottomEdgeCols.Dispose();

                _hvEtchingRegionLeftGapList.Dispose();
                _hvEtchingRegionRightGapList.Dispose();
                _hvEtchingRegionLeftGap.Dispose();
                _hvEtchingRegionRightGap.Dispose();

                _hvEtchingRegionTopGapList.Dispose();
                _hvEtchingRegionBottomGapList.Dispose();
                _hvEtchingRegionTopGap.Dispose();
                _hvEtchingRegionBottomGap.Dispose();

                _hvRefLineTopRows.Dispose();
                _hvRefLineTopCols.Dispose();
                _hvRefLineDownRows.Dispose();
                _hvRefLineDownCols.Dispose();

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

            HOperatorSet.GenEmptyObj(out _hoFitEtchingRegionStartEdge);
            HOperatorSet.GenEmptyObj(out _hoFitEtchingRegionEndEdge);
            HOperatorSet.GenEmptyObj(out _hoFitEtchingRegionTopEdge);
            HOperatorSet.GenEmptyObj(out _hoFitEtchingRegionBottomEdge);

            HOperatorSet.GenEmptyObj(out _hoRefLines);

            _hvMeasureCenterRow = new HTuple();
            _hvMeasureCenterCol = new HTuple();

            _hvPhi = new HTuple();

            _hvEtchingLineEndPointRows = new HTuple();
            _hvEtchingLineEndPointCols = new HTuple();
            _hvEtchingLineStartPointRows = new HTuple();
            _hvEtchingLineStartPointCols = new HTuple();

            _hvEtchingRegionTopEdgeRows = new HTuple();
            _hvEtchingRegionTopEdgeCols = new HTuple();
            _hvEtchingRegionBottomEdgeRows = new HTuple();
            _hvEtchingRegionBottomEdgeCols = new HTuple();

            _hvEtchingRegionLeftGapList = new HTuple();
            _hvEtchingRegionRightGapList = new HTuple();
            _hvEtchingRegionLeftGap = new HTuple(-1);
            _hvEtchingRegionRightGap = new HTuple(-1);
            _hvEtchingRegionTopGapList = new HTuple();
            _hvEtchingRegionBottomGapList = new HTuple();
            _hvEtchingRegionTopGap = new HTuple(-1);
            _hvEtchingRegionBottomGap = new HTuple(-1);

            _hvRefLineTopRows = new HTuple();
            _hvRefLineTopCols = new HTuple();
            _hvRefLineDownRows = new HTuple();
            _hvRefLineDownCols = new HTuple();

            return 0;
        }


        /// <summary>
        /// 计算样品的倾斜角度
        /// </summary>
        private int CalculateSampleAngle()
        {
            using (var dh = new HDevDisposeHelper())
            {
                HObject? hoTmp = null;

                HObject? hoCalibRegion = null;
                HObject? hoCalibRegionImage = null;
                HObject? hoFitLine = null;

                HTuple? hvPlateCenterR = null;
                HTuple? hvPlateCenterC = null;
                HTuple? hvPlateRegionWidth = null;
                HTuple? hvPlateRegionHeight = null;

                try
                {
                    HOperatorSet.GenEmptyObj(out hoCalibRegion);
                    HOperatorSet.GenEmptyObj(out hoCalibRegionImage);
                    HOperatorSet.GenEmptyObj(out hoFitLine);

                    hvPlateCenterR = new HTuple();
                    hvPlateCenterC = new HTuple();
                    hvPlateRegionWidth = new HTuple();
                    hvPlateRegionHeight = new HTuple();

                    // 计算测量窗口状态
                    HOperatorSet.RegionFeatures(_hoPlateRegion, "row", out hvPlateCenterR);
                    HOperatorSet.RegionFeatures(_hoPlateRegion, "column", out hvPlateCenterC);
                    HOperatorSet.RegionFeatures(_hoPlateRegion, "width", out hvPlateRegionWidth);
                    HOperatorSet.RegionFeatures(_hoPlateRegion, "height", out hvPlateRegionHeight);

                    _hvMeasureCenterRow = new HTuple(hvPlateCenterR);
                    _hvMeasureCenterCol = new HTuple(hvPlateCenterC);

                    HTuple hvCalibRegionW, hvCalibRegionH;
                    hvCalibRegionW = (hvPlateRegionWidth * 0.5) * 1.3;
                    hvCalibRegionH = hvPlateRegionHeight * 0.25;
                    HOperatorSet.GenRectangle2(out hoTmp, hvPlateCenterR, hvPlateCenterC,
                                               (_hvPlateStartEdgePhi + _hvPlateEndEdgePhi) * 0.5, hvCalibRegionW * 0.5, hvCalibRegionH * 0.5);
                    ReplaceHobject(ref hoCalibRegion, ref hoTmp);
                    HOperatorSet.Intersection(hoCalibRegion, _hoValidRegion, out hoTmp);
                    ReplaceHobject(ref hoCalibRegion, ref hoTmp);

                    // 计算倾斜角度
                    HTuple hvTmpNum, hvTmpFitLineNum;
                    HTuple hvTmpCenterR, hvTmpCenterC, hvTmpW, hvTmpH, hvTmpPhi, hvTmpDist;
                    HTuple hvMeasureNum, hvMeasureW, hvMeasureH;
                    HTuple hvOffsetR, hvOffsetC;
                    HTuple hvBeginRow, hvBeginCol, hvEndRow, hvEndCol;
                    HTuple hvEndEdgeRows, hvEndEdgeCols;
                    HOperatorSet.ReduceDomain(_hoGrayImage, hoCalibRegion, out hoTmp);
                    ReplaceHobject(ref hoCalibRegionImage, ref hoTmp);
                    HOperatorSet.CountObj(hoCalibRegion, out hvTmpNum);
                    if (hvTmpNum.D > 0)
                    {
                        HOperatorSet.RegionFeatures(hoCalibRegion, "row", out hvTmpCenterR);
                        HOperatorSet.RegionFeatures(hoCalibRegion, "column", out hvTmpCenterC);
                        HOperatorSet.RegionFeatures(hoCalibRegion, "rect2_len2", out hvTmpW);
                        HOperatorSet.RegionFeatures(hoCalibRegion, "rect2_len1", out hvTmpH);
                        HOperatorSet.RegionFeatures(hoCalibRegion, "rect2_phi", out hvTmpPhi);

                        hvTmpDist = hvTmpW.TupleMin2(hvTmpH);
                        hvMeasureNum = new HTuple(15);
                        hvMeasureW = 2 * (hvCalibRegionH * 0.5) / hvMeasureNum;
                        if (hvMeasureW.D > 40)
                            hvMeasureW = new HTuple(40);

                        hvMeasureH = hvCalibRegionW * 0.5;

                        hvOffsetR = (0.5 * hvCalibRegionH) * ((new HTuple(90)).TupleRad() + (_hvPlateStartEdgePhi + _hvPlateEndEdgePhi) * 0.5).TupleSin();
                        hvOffsetC = (0.5 * hvCalibRegionH) * ((new HTuple(90)).TupleRad() + (_hvPlateStartEdgePhi + _hvPlateEndEdgePhi) * 0.5).TupleCos();

                        hvBeginRow = hvTmpCenterR - hvOffsetR;
                        hvBeginCol = hvTmpCenterC + hvOffsetC;
                        hvEndRow = hvTmpCenterR + hvOffsetR;
                        hvEndCol = hvTmpCenterC - hvOffsetC;

                        LineCalipers(hoCalibRegionImage, hvBeginRow, hvBeginCol, hvEndRow, hvEndCol, hvMeasureW, hvMeasureH,
                                           _measureParam.GrayAmplitudeSigma, _measureParam.GrayAmplitudeThr, "positive", hvMeasureNum, "all", 0.5,
                                           out hoTmp, out hvEndEdgeRows, out hvEndEdgeCols);
                        ReplaceHobject(ref hoFitLine, ref hoTmp);

                        HOperatorSet.CountObj(hoFitLine, out hvTmpFitLineNum);
                        if (hvTmpFitLineNum.D > 0)
                        {
                            HTuple hvRowBegin, hvColBegin, hvRowEnd, hvColEnd;
                            HTuple hvNr, hvNc, hvDist;
                            HTuple hvPhi;
                            HOperatorSet.FitLineContourXld(hoFitLine, "tukey", -1, 0, 5, 2, out hvRowBegin, out hvColBegin, out hvRowEnd, out hvColEnd,
                                                           out hvNr, out hvNc, out hvDist);

                            if(hvRowBegin > hvRowEnd)
                            {
                                (hvRowBegin, hvRowEnd) = (hvRowEnd, hvRowBegin);
                                (hvColBegin, hvColEnd) = (hvColEnd, hvColBegin);
                            }

                            HOperatorSet.AngleLx(hvRowBegin, hvColBegin, hvRowEnd, hvColEnd, out hvPhi);

                            //_hvPlatePhi = new HTuple(((new HTuple(90)).TupleRad()) + hvPhi);
                            _hvPhi = new HTuple(((new HTuple(90)).TupleRad()) + hvPhi);
                        }
                        else
                        {
                            //_hvPlatePhi = (_hvPlateStartEdgePhi + _hvPlateEndEdgePhi) * 0.5;
                            _hvPhi = (_hvPlateStartEdgePhi + _hvPlateEndEdgePhi) * 0.5;
                        }
                    }
                    else
                    {
                        //_hvPlatePhi = (_hvPlateStartEdgePhi + _hvPlateEndEdgePhi) * 0.5;
                        _hvPhi = (_hvPlateStartEdgePhi + _hvPlateEndEdgePhi) * 0.5;
                    }
                }
                finally
                {
                    hoTmp?.Dispose();

                    hoCalibRegion?.Dispose(); 
                    hoCalibRegionImage?.Dispose(); 
                    hoFitLine?.Dispose();

                    hvPlateCenterR?.Dispose(); 
                    hvPlateCenterC?.Dispose(); 
                    hvPlateRegionWidth?.Dispose(); 
                    hvPlateRegionHeight?.Dispose();
                }
            }

            return 0;
        }


        /// <summary>
        /// 计算对刻蚀线进行筛选的参考点(极片中心各条刻蚀线的终点)
        /// </summary>
        private int GetReferenceLine()
        {
            using (var dh = new HDevDisposeHelper())
            {
                HObject? hoTmp = null;
                HObject? hoFilterEtchingLineRegion = null;
                HObject? hoRefLine = null;

                HTuple hvEtchingPointRefRows = new HTuple();
                HTuple hvEtchingPointRefCols = new HTuple();

                HTuple hvFilterEtchingLineHandle = new HTuple();

                HTuple hvTmpEdgeRowNeg = new HTuple();
                HTuple hvTmpEdgeColNeg = new HTuple();
                HTuple hvTmpEdgeRowPos = new HTuple();
                HTuple hvTmpEdgeColPos = new HTuple();

                HTuple hvAmplitude = new HTuple();
                HTuple hvDistance = new HTuple();


                try 
                {
                    HOperatorSet.GenEmptyObj(out hoTmp);
                    HOperatorSet.GenEmptyObj(out hoFilterEtchingLineRegion);

                    HTuple hvDetRegW = (_measureParam.ScanWidth * 0.05) * 0.5;

                    // 计算测量窗口状态
                    HOperatorSet.RegionFeatures(_hoPlateRegion, "row", out HTuple hvPlateCenterR);
                    HOperatorSet.RegionFeatures(_hoPlateRegion, "column", out HTuple hvPlateCenterC);
                    HOperatorSet.RegionFeatures(_hoPlateRegion, "width", out HTuple hvPlateRegionWidth);
                    HOperatorSet.RegionFeatures(_hoPlateRegion, "height", out HTuple hvPlateRegionHeight);

                    HOperatorSet.GenRectangle2(out hoFilterEtchingLineRegion, hvPlateCenterR, hvPlateCenterC, _hvPhi, 
                                               _measureParam.ScanWidth * 0.5 - _measureParam.ImageEdgeMaskSize, hvDetRegW);
                    HOperatorSet.Intersection(hoFilterEtchingLineRegion, _hoValidRegion, out hoTmp);
                    ReplaceHobject(ref hoFilterEtchingLineRegion, ref hoTmp);
                    HOperatorSet.CountObj(hoFilterEtchingLineRegion, out HTuple hvTmpNum);
                    if (hvTmpNum.D > 0)
                    {
                        HOperatorSet.GenMeasureRectangle2(hvPlateCenterR, hvPlateCenterC, _hvPhi,
                                                      _measureParam.ScanWidth * 0.5 - _measureParam.ImageEdgeMaskSize, hvDetRegW,
                                                      _measureParam.ScanWidth, _measureParam.ScanHeight, "bilinear", out hvFilterEtchingLineHandle);

                        HOperatorSet.MeasurePos(_hoGrayImage, hvFilterEtchingLineHandle, _measureParam.GrayAmplitudeSigma, _measureParam.GrayAmplitudeThr, 
                                               "negative", "all", out hvTmpEdgeRowNeg, out hvTmpEdgeColNeg, out hvAmplitude, out hvDistance);
                        HOperatorSet.MeasurePos(_hoGrayImage, hvFilterEtchingLineHandle, _measureParam.GrayAmplitudeSigma, _measureParam.GrayAmplitudeThr,
                                               "positive", "all", out hvTmpEdgeRowPos, out hvTmpEdgeColPos, out hvAmplitude, out hvDistance);

                        HOperatorSet.CloseMeasure(hvFilterEtchingLineHandle);


                        HTuple hvNegIdx = 0;
                        HTuple hvPosIdx = 0;
                        HTuple hvItemNumMax = (new HTuple(hvTmpEdgeRowNeg.TupleLength())).TupleMax2(new HTuple(hvTmpEdgeRowPos.TupleLength()));


                        if (hvTmpEdgeRowNeg.Length > 0 && hvTmpEdgeRowPos.Length > 0)
                        {
                            for (int edgeIdx = 0; edgeIdx < hvItemNumMax; edgeIdx++)
                            {
                                HTuple hvSelectRowPos = hvTmpEdgeRowPos.TupleSelect(hvPosIdx);
                                HTuple hvSelectColPos = hvTmpEdgeColPos.TupleSelect(hvPosIdx);
                                HTuple hvSelectRowNeg = hvTmpEdgeRowNeg.TupleSelect(hvNegIdx);
                                HTuple hvSelectColNeg = hvTmpEdgeColNeg.TupleSelect(hvNegIdx);

                                if ((int)(new HTuple(hvSelectColNeg.TupleLess(hvSelectColPos))) != 0)
                                {
                                    HTuple hvCenterRow = (hvSelectRowPos + hvSelectRowNeg) * 0.5;
                                    HTuple hvCenterCol = (hvSelectColPos + hvSelectColNeg) * 0.5;

                                    HTuple ExpTmpLocalVar_EtchingPointRefRows = hvEtchingPointRefRows.TupleConcat(hvCenterRow);
                                    hvEtchingPointRefRows.Dispose();
                                    hvEtchingPointRefRows = ExpTmpLocalVar_EtchingPointRefRows;

                                    HTuple ExpTmpLocalVar_EtchingPointRefCols = hvEtchingPointRefCols.TupleConcat(hvCenterCol);
                                    hvEtchingPointRefCols.Dispose();
                                    hvEtchingPointRefCols = ExpTmpLocalVar_EtchingPointRefCols;

                                    HTuple ExpTmpLocalVar_NegIdx = hvNegIdx + 1;
                                    hvNegIdx.Dispose();
                                    hvNegIdx = ExpTmpLocalVar_NegIdx;

                                    HTuple ExpTmpLocalVar_PosIdx = hvPosIdx + 1;
                                    hvPosIdx.Dispose();
                                    hvPosIdx = ExpTmpLocalVar_PosIdx;

                                }
                                else
                                {
                                    HTuple ExpTmpLocalVar_PosIdx = hvPosIdx + 1;
                                    hvPosIdx.Dispose();
                                    hvPosIdx = ExpTmpLocalVar_PosIdx;
                                }

                                if ((int)((new HTuple(hvNegIdx.TupleGreater((new HTuple(hvTmpEdgeRowNeg.TupleLength()
                                    )) - 1))).TupleOr(new HTuple(hvPosIdx.TupleGreater((new HTuple(hvTmpEdgeRowPos.TupleLength()
                                    )) - 1)))) != 0)
                                {
                                    break;
                                }
                            }
                        }

                    }

                    if(hvEtchingPointRefRows.Length > 0)
                    {
                        for(int tmpIdx = 0; tmpIdx < hvEtchingPointRefRows.Length; tmpIdx++)
                        {
                            HTuple hvRefLineStartRow = (hvEtchingPointRefRows.TupleSelect(tmpIdx)) - ((hvPlateRegionHeight * 0.5) * (_hvPhi.TupleCos()));
                            HTuple hvRefLineStartCol = (hvEtchingPointRefCols.TupleSelect(tmpIdx)) - ((hvPlateRegionHeight * 0.5) * (_hvPhi.TupleSin()));
                            HTuple hvRefLineEndRow = (hvEtchingPointRefRows.TupleSelect(tmpIdx)) + ((hvPlateRegionHeight * 0.5) * (_hvPhi.TupleCos()));
                            HTuple hvRefLineEndCol = (hvEtchingPointRefCols.TupleSelect(tmpIdx)) + ((hvPlateRegionHeight * 0.5) * (_hvPhi.TupleSin()));

                            HOperatorSet.GenContourPolygonXld(out hoRefLine, hvRefLineStartRow.TupleConcat(hvRefLineEndRow), 
                                                                             hvRefLineStartCol.TupleConcat(hvRefLineEndCol));

                            HOperatorSet.IntersectionLines(hvRefLineStartRow, hvRefLineStartCol, hvRefLineEndRow, hvRefLineEndCol, 
                                                           _hvLeftTopRow, _hvLeftTopColumn, _hvRightTopRow, _hvRightTopColumn,
                                                           out HTuple hvTopInterRow, out HTuple hvTopInterCol, out HTuple hv__);
                            HOperatorSet.IntersectionLines(hvRefLineStartRow, hvRefLineStartCol, hvRefLineEndRow, hvRefLineEndCol, 
                                                           _hvLeftDownRow, _hvLeftDownColumn, _hvRightDownRow, _hvRightDownColumn,
                                                           out HTuple hvDownInterRow, out HTuple hvDownInterCol, out hv__);

                            if(_measureParam.EtchingLineMeasureMaskRate > 0 && _measureParam.EtchingLineMeasureMaskRate < 1)
                            {
                                if ((int)((new HTuple((new HTuple(hvTopInterCol.TupleLess((1 - 0.5 * _measureParam.EtchingLineMeasureMaskRate) * _measureParam.ScanWidth))).TupleAnd(
                                       new HTuple(hvTopInterCol.TupleGreater(0.5 * _measureParam.EtchingLineMeasureMaskRate * _measureParam.ScanWidth))))).TupleAnd(
                                      (new HTuple(hvDownInterCol.TupleLess((1 - 0.5 * _measureParam.EtchingLineMeasureMaskRate) * _measureParam.ScanWidth))).TupleAnd(
                                       new HTuple(hvDownInterCol.TupleGreater(0.5 * _measureParam.EtchingLineMeasureMaskRate * _measureParam.ScanWidth))))) != 0)
                                {
                                    HOperatorSet.ConcatObj(_hoRefLines, hoRefLine, out hoTmp);
                                    ReplaceHobject(ref _hoRefLines, ref hoTmp);

                                    HTuple ExpTmpLocalVar_RefLineTopRows = _hvRefLineTopRows.TupleConcat(hvTopInterRow);
                                    _hvRefLineTopRows.Dispose();
                                    _hvRefLineTopRows = ExpTmpLocalVar_RefLineTopRows;

                                    HTuple ExpTmpLocalVar_RefLineTopCols = _hvRefLineTopCols.TupleConcat(hvTopInterCol);
                                    _hvRefLineTopCols.Dispose();
                                    _hvRefLineTopCols = ExpTmpLocalVar_RefLineTopCols;

                                    HTuple ExpTmpLocalVar_RefLineDownRows = _hvRefLineDownRows.TupleConcat(hvDownInterRow);
                                    _hvRefLineDownRows.Dispose();
                                    _hvRefLineDownRows = ExpTmpLocalVar_RefLineDownRows;

                                    HTuple ExpTmpLocalVar_RefLineDownCols = _hvRefLineDownCols.TupleConcat(hvDownInterCol);
                                    _hvRefLineDownCols.Dispose();
                                    _hvRefLineDownCols = ExpTmpLocalVar_RefLineDownCols;
                                }
                            }
                            else
                            {
                                HOperatorSet.GenEmptyObj(out hoTmp);
                                ReplaceHobject(ref _hoRefLines, ref hoTmp);

                                _hvRefLineTopRows.Dispose();
                                _hvRefLineTopCols.Dispose();
                                _hvRefLineDownRows.Dispose();
                                _hvRefLineDownCols.Dispose();
                                _hvRefLineTopRows = new HTuple();
                                _hvRefLineTopCols = new HTuple();
                                _hvRefLineDownRows = new HTuple();
                                _hvRefLineDownCols = new HTuple();
                            }
                        }
                    } 
                }
                finally
                {
                    hvEtchingPointRefRows.Dispose();
                    hvEtchingPointRefCols.Dispose();
                    hvFilterEtchingLineHandle.Dispose();

                    hoTmp?.Dispose();
                    hoFilterEtchingLineRegion?.Dispose();
                    hoRefLine?.Dispose();
                }
            }

            return 0;
        }


        /// <summary>
        /// 定位刻蚀线起点与终点
        /// </summary>
        private int GetEtchingStartEndLine(HObject hoGrayImage, HTuple hvEdgeType, HTuple hvRowBegin, HTuple hvColBegin,
                                           HTuple hvRowEnd, HTuple hvColEnd, HTuple hvPhi,
                                           out HTuple hvUpperPointRows, out HTuple hvUpperPointCols,
                                           out HTuple hvLowerPointRows, out HTuple hvLowerPointCols)
        {
            using (var dh = new HDevDisposeHelper())
            {
                HObject? hoTmp = null;
                HObject? hoGapRegion = null;
                HObject? hoGapImage = null;
                HObject? hoGapImageMeanS = null;
                HObject? hoGapImageMeanB = null;
                HObject? hoGapRegionTmp = null;
                HObject? hoConnectedRegions = null;
                HObject? hoSelectedRegions = null;
                HObject? hoGapImagePart = null;
                HObject? hoGapImagePartRegionTmp = null;
                HObject? hoGapImagePartRegionS = null;
                HObject? hoEtchingLineTmp = null;

                try
                {
                    hvUpperPointRows = new HTuple();
                    hvUpperPointCols = new HTuple();
                    hvLowerPointRows = new HTuple();
                    hvLowerPointCols = new HTuple();

                    HTuple hvImgWidth, hvImgHeight;
                    HTuple hvEdgeLineCenterR, hvEdgeLineCenterC;
                    HTuple hvMeasureWidth;

                    hvEdgeLineCenterR = (hvRowBegin + hvRowEnd) * 0.5;
                    hvEdgeLineCenterC = (hvColBegin + hvColEnd) * 0.5;

                    HOperatorSet.GetImageSize(hoGrayImage, out hvImgWidth, out hvImgHeight);
                    HOperatorSet.DistancePp(hvRowBegin, hvColBegin, hvRowEnd, hvColEnd, out hvMeasureWidth);

                    HTuple hvCalibRegionW = (hvMeasureWidth * 0.5) * 1.3;
                    HTuple hvCalibRegionH = (hvMeasureWidth * 0.5) * 1.3;
                    HTuple hvOffsetR = (0.5 * hvCalibRegionH) * (((new HTuple(90)).TupleRad() + hvPhi).TupleSin());
                    HTuple hvOffsetC = (0.5 * hvCalibRegionH) * (((new HTuple(90)).TupleRad() + hvPhi).TupleCos());

                    HTuple hvGapRegionCenterR, hvGapRegionCenterC;
                    if ((int)(hvEdgeType.TupleEqual("strat_edge")) == 1)
                    {
                        hvGapRegionCenterR = hvEdgeLineCenterR + hvOffsetR;
                        hvGapRegionCenterC = hvEdgeLineCenterC - hvOffsetC;
                    }
                    else
                    {
                        hvGapRegionCenterR = hvEdgeLineCenterR - hvOffsetR;
                        hvGapRegionCenterC = hvEdgeLineCenterC + hvOffsetC;
                    }
                    
                    HOperatorSet.GenRectangle2(out hoGapRegion, hvGapRegionCenterR, hvGapRegionCenterC, hvPhi, hvCalibRegionW * 0.5, hvCalibRegionH * 0.5);
                    HOperatorSet.ReduceDomain(hoGrayImage, hoGapRegion, out hoGapImage);
        
                    HOperatorSet.MeanImage(hoGapImage, out hoGapImageMeanS, 70, 70);
                    HOperatorSet.MeanImage(hoGapImage, out hoGapImageMeanB, 700, 70);
                    HOperatorSet.DynThreshold(hoGapImageMeanS, hoGapImageMeanB, out hoGapRegionTmp, 16, "dark");
        
                    HOperatorSet.Connection(hoGapRegionTmp, out hoConnectedRegions);
                    HOperatorSet.SelectShape(hoConnectedRegions, out hoSelectedRegions, "area", "and", 20000, 99999999);
                    HOperatorSet.Union1(hoSelectedRegions, out hoGapRegionTmp);

                    HOperatorSet.CropDomain(hoGapImage, out hoGapImagePart);
                    HOperatorSet.RotateImage(hoGapImagePart, out hoTmp, -(hvPhi.TupleDeg()), "constant");
                    ReplaceHobject(ref hoGapImagePart, ref hoTmp);

                    HTuple hvTmpW, hvTmpH;
                    HTuple hvHomMat2DIdentity, hvHomMat2D, hvHomMat2DInvert;
                    HOperatorSet.GetImageSize(hoGapImagePart, out hvTmpW, out hvTmpH);
                    HOperatorSet.HomMat2dIdentity(out hvHomMat2DIdentity);
                    HOperatorSet.HomMat2dTranslate(hvHomMat2DIdentity, (-hvGapRegionCenterR) + (hvTmpH * 0.5),
                                                                       (-hvGapRegionCenterC) + (hvTmpW * 0.5), out hvHomMat2D);
                    HOperatorSet.HomMat2dRotate(hvHomMat2D, -hvPhi, hvTmpH * 0.5, hvTmpW * 0.5, out hvHomMat2D);
                    HOperatorSet.HomMat2dInvert(hvHomMat2D, out hvHomMat2DInvert);

                    
                    HOperatorSet.AffineTransRegion(hoGapRegionTmp, out hoGapImagePartRegionTmp, hvHomMat2D, "nearest_neighbor");
                    HOperatorSet.FillUp(hoGapImagePartRegionTmp, out hoTmp);
                    ReplaceHobject(ref hoGapImagePartRegionTmp, ref hoTmp);
                    HOperatorSet.ClosingCircle(hoGapImagePartRegionTmp, out hoTmp, 5);
                    ReplaceHobject(ref hoGapImagePartRegionTmp, ref hoTmp);
                    HOperatorSet.OpeningCircle(hoGapImagePartRegionTmp, out hoTmp, 5);
                    ReplaceHobject(ref hoGapImagePartRegionTmp, ref hoTmp);
                    HOperatorSet.OpeningRectangle1(hoGapImagePartRegionTmp, out hoTmp, 1, hvTmpH * 0.25);
                    ReplaceHobject(ref hoGapImagePartRegionTmp, ref hoTmp);
                    HOperatorSet.ClosingRectangle1(hoGapImagePartRegionTmp, out hoTmp, 20, 20);
                    ReplaceHobject(ref hoGapImagePartRegionTmp, ref hoTmp);
                    HOperatorSet.Connection(hoGapImagePartRegionTmp, out hoGapImagePartRegionS);
                    HOperatorSet.SelectShape(hoGapImagePartRegionS, out hoTmp, (new HTuple("rect2_phi")).TupleConcat("rect2_phi"), "or",
                                             (new HTuple((new HTuple(90 - 10)).TupleRad())).TupleConcat(-((new HTuple(90 + 10)).TupleRad())),
                                             (new HTuple((new HTuple(90 + 10)).TupleRad())).TupleConcat(-((new HTuple(90 - 10)).TupleRad())));
                    ReplaceHobject(ref hoGapImagePartRegionS, ref hoTmp);

                    // 计算每个槽分割region的起始端点
                    HTuple hvEtchingLineNum;
                    HOperatorSet.CountObj(hoGapImagePartRegionS, out hvEtchingLineNum);
                    HOperatorSet.SortRegion(hoGapImagePartRegionS, out hoTmp, "first_point", "true", "column");
                    ReplaceHobject(ref hoGapImagePartRegionS, ref hoTmp);

                    for (int idx = 0; idx < hvEtchingLineNum.I; idx++)
                    {
                        HTuple hvEtchingLineWidthTmp;
                        HTuple hvUpperLeftRow, hvUpperLeftCol, hvUpperPointRow, hvUpperPointCol;
                        HOperatorSet.SelectObj(hoGapImagePartRegionS, out hoEtchingLineTmp, idx + 1);
                        HOperatorSet.RegionFeatures(hoEtchingLineTmp, "width", out hvEtchingLineWidthTmp);
                        HOperatorSet.RegionFeatures(hoEtchingLineTmp, "row1", out hvUpperLeftRow);
                        HOperatorSet.RegionFeatures(hoEtchingLineTmp, "column1", out hvUpperLeftCol);
                        hvUpperPointRow = hvUpperLeftRow;
                        hvUpperPointCol = hvUpperLeftCol + 0.5 * hvEtchingLineWidthTmp;
                        HOperatorSet.AffineTransPixel(hvHomMat2DInvert, hvUpperPointRow, hvUpperPointCol, out hvUpperPointRow, out hvUpperPointCol);
                        hvUpperPointRows = hvUpperPointRows.TupleConcat(hvUpperPointRow);
                        hvUpperPointCols = hvUpperPointCols.TupleConcat(hvUpperPointCol);

                        HTuple hvLowerRightRow, hvLowerRightCol, hvLowerPointRow, hvLowerPointCol;
                        HOperatorSet.RegionFeatures(hoEtchingLineTmp, "row2", out hvLowerRightRow);
                        HOperatorSet.RegionFeatures(hoEtchingLineTmp, "column2", out hvLowerRightCol);
                        hvLowerPointRow = hvLowerRightRow;
                        hvLowerPointCol = hvLowerRightCol - 0.5 * hvEtchingLineWidthTmp;
                        HOperatorSet.AffineTransPixel(hvHomMat2DInvert, hvLowerPointRow, hvLowerPointCol, out hvLowerPointRow, out hvLowerPointCol);
                        hvLowerPointRows = hvLowerPointRows.TupleConcat(hvLowerPointRow);
                        hvLowerPointCols = hvLowerPointCols.TupleConcat(hvLowerPointCol);   
                    }
                }
                finally
                {
                    hoTmp?.Dispose();

                    hoGapRegion?.Dispose(); hoGapImage?.Dispose();
                    hoGapImageMeanS?.Dispose(); hoGapImageMeanB?.Dispose(); hoGapRegionTmp?.Dispose();
                    hoConnectedRegions?.Dispose(); hoSelectedRegions?.Dispose();
                    hoGapImagePart?.Dispose();
                    hoGapImagePartRegionTmp?.Dispose(); hoGapImagePartRegionS?.Dispose();

                    hoEtchingLineTmp?.Dispose();
                }
            }

            return 0;
        }


        /// <summary>
        /// 测量刻蚀线起点终点到极片边缘的距离
        /// </summary>
        private int MeasureEtchingLineStartEndGap()
        {
            using (var dh = new HDevDisposeHelper())
            {
                HObject? hoTmp = null;

                try
                {
                    HTuple hvTmpRowBegin = new HTuple();
                    HTuple hvTmpColBegin = new HTuple();
                    HTuple hvTmpRowEnd = new HTuple();
                    HTuple hvTmpColEnd = new HTuple();

                    // 生成测量区域
                    if (_hvStartEdgeRowBegin.D != 0 && _hvStartEdgeRowEnd.D != 0)
                    {
                        if (_measureParam.PlatePart == -1)
                        {
                            hvTmpRowBegin = _hvLeftTopRow;
                            hvTmpColBegin = _hvLeftTopColumn;
                            hvTmpRowEnd = (_hvLeftTopRow + _hvRightTopRow) * 0.5;
                            hvTmpColEnd = (_hvLeftTopColumn + _hvRightTopColumn) * 0.5;
                        }
                        else if (_measureParam.PlatePart == 1)
                        {
                            hvTmpRowBegin = (_hvLeftTopRow + _hvRightTopRow) * 0.5;
                            hvTmpColBegin = (_hvLeftTopColumn + _hvRightTopColumn) * 0.5;
                            hvTmpRowEnd = _hvRightTopRow;
                            hvTmpColEnd = _hvRightTopColumn;
                        }
                        else
                        {
                            hvTmpRowBegin = _hvLeftTopRow;
                            hvTmpColBegin = _hvLeftTopColumn;
                            hvTmpRowEnd = _hvRightTopRow;
                            hvTmpColEnd = _hvRightTopColumn;
                        }

                        HTuple hvUpperCenterRows, hvUpperCenterCols, hvLowerCenterRows, hvLowerCenterCols;
                        GetEtchingStartEndLine(_hoGrayImage, "strat_edge", hvTmpRowBegin, hvTmpColBegin, hvTmpRowEnd, hvTmpColEnd, _hvPhi,
                                               out hvUpperCenterRows, out hvUpperCenterCols, out hvLowerCenterRows, out hvLowerCenterCols);

                        _hvEtchingLineStartPointRows = new HTuple(hvUpperCenterRows);
                        _hvEtchingLineStartPointCols = new HTuple(hvUpperCenterCols);
                    }
                    else
                    {
                        _hvEtchingLineStartPointRows = new HTuple();
                        _hvEtchingLineStartPointCols = new HTuple();
                    }

                    if (_hvEndEdgeRowBegin.D != _measureParam.ScanHeight && _hvEndEdgeRowEnd.D != _measureParam.ScanHeight)
                    {
                        if (_measureParam.PlatePart == -1)
                        {
                            hvTmpRowBegin = _hvLeftDownRow;
                            hvTmpColBegin = _hvLeftDownColumn;
                            hvTmpRowEnd = (_hvLeftDownRow + _hvRightDownRow) * 0.5;
                            hvTmpColEnd = (_hvLeftDownColumn + _hvRightDownColumn) * 0.5;
                        }
                        else if (_measureParam.PlatePart == 1)
                        {
                            hvTmpRowBegin = (_hvLeftDownRow + _hvRightDownRow) * 0.5;
                            hvTmpColBegin = (_hvLeftDownColumn + _hvRightDownColumn) * 0.5;
                            hvTmpRowEnd = _hvRightDownRow;
                            hvTmpColEnd = _hvRightDownColumn;
                        }
                        else
                        {
                            hvTmpRowBegin = _hvLeftDownRow;
                            hvTmpColBegin = _hvLeftDownColumn;
                            hvTmpRowEnd = _hvRightDownRow;
                            hvTmpColEnd = _hvRightDownColumn;
                        }

                        HTuple hvUpperCenterRows, hvUpperCenterCols, hvLowerCenterRows, hvLowerCenterCols;
                        GetEtchingStartEndLine(_hoGrayImage, "end_edge", hvTmpRowBegin, hvTmpColBegin, hvTmpRowEnd, hvTmpColEnd, _hvPhi,
                                               out hvUpperCenterRows, out hvUpperCenterCols, out hvLowerCenterRows, out hvLowerCenterCols);

                        _hvEtchingLineEndPointRows = new HTuple(hvLowerCenterRows);
                        _hvEtchingLineEndPointCols = new HTuple(hvLowerCenterCols);
                    }
                    else
                    {
                        _hvEtchingLineEndPointRows = new HTuple();
                        _hvEtchingLineEndPointCols = new HTuple();
                    }

                    // 计算刻蚀线起点到极片边缘平均间距
                    _hvEtchingRegionLeftGapList = new HTuple();
                    if (_hvEtchingLineStartPointRows.TupleLength() > 0 &&
                       _hvEtchingLineStartPointCols.TupleLength() > 0)
                    {

                        HTuple hvTmpPointNum = _hvEtchingLineStartPointRows.TupleLength();

                        for (int idx = 0; idx < hvTmpPointNum; idx++)
                        {
                            HTuple hvTmpPointRow = _hvEtchingLineStartPointRows.TupleSelect(idx);
                            HTuple hvTmpPointCol = _hvEtchingLineStartPointCols.TupleSelect(idx);

                            HTuple hvTmpPointRowProj, hvTmpPointColProj;
                            HOperatorSet.ProjectionPl(hvTmpPointRow, hvTmpPointCol, _hvLeftTopRow, _hvLeftTopColumn, _hvRightTopRow, _hvRightTopColumn,
                                                      out hvTmpPointRowProj, out hvTmpPointColProj);

                            if (hvTmpPointRow.D > hvTmpPointRowProj.D)
                            {
                                HTuple hvTmpDist;
                                HOperatorSet.DistancePp(hvTmpPointRow, hvTmpPointCol, hvTmpPointRowProj, hvTmpPointColProj, out hvTmpDist);
                                _hvEtchingRegionLeftGapList = _hvEtchingRegionLeftGapList.TupleConcat(hvTmpDist);
                            }
                            else
                            {
                                _hvEtchingRegionLeftGapList = _hvEtchingRegionLeftGapList.TupleConcat(0);
                            }
                        }

                        _hvEtchingRegionLeftGap = _hvEtchingRegionLeftGapList.TupleMean();

                        HTuple hvOffsetRow = _hvEtchingRegionLeftGap / _hvPhi.TupleCos();

                        HOperatorSet.GenContourPolygonXld(out _hoFitEtchingRegionStartEdge, new HTuple(_hvLeftTopRow + hvOffsetRow, _hvRightTopRow + hvOffsetRow),
                                                          new HTuple(_hvLeftTopColumn, _hvRightTopColumn));

                    }
                    else
                    {
                        _hvEtchingRegionLeftGap = -1;

                        HOperatorSet.GenContourPolygonXld(out _hoFitEtchingRegionStartEdge, new HTuple(_hvLeftTopRow, _hvRightTopRow),
                                                          new HTuple(_hvLeftTopColumn, _hvRightTopColumn));
                    }

                    // 计算刻蚀线终点到极片边缘平均间距
                    _hvEtchingRegionRightGapList = new HTuple();
                    if (_hvEtchingLineEndPointRows.TupleLength() > 0 &&
                        _hvEtchingLineEndPointCols.TupleLength() > 0)
                    {
                        HTuple hvTmpPointNum = _hvEtchingLineEndPointRows.TupleLength();

                        for (int idx = 0; idx < hvTmpPointNum; idx++)
                        {
                            HTuple hvTmpPointRow = _hvEtchingLineEndPointRows.TupleSelect(idx);
                            HTuple hvTmpPointCol = _hvEtchingLineEndPointCols.TupleSelect(idx);

                            HTuple hvTmpPointRowProj, hvTmpPointColProj;
                            HOperatorSet.ProjectionPl(hvTmpPointRow, hvTmpPointCol, _hvLeftDownRow, _hvLeftDownColumn, _hvRightDownRow, _hvRightDownColumn,
                                                      out hvTmpPointRowProj, out hvTmpPointColProj);

                            if (hvTmpPointRow.D < hvTmpPointRowProj.D)
                            {
                                HTuple hvTmpDist;
                                HOperatorSet.DistancePp(hvTmpPointRow, hvTmpPointCol, hvTmpPointRowProj, hvTmpPointColProj, out hvTmpDist);
                                _hvEtchingRegionRightGapList = _hvEtchingRegionRightGapList.TupleConcat(hvTmpDist);
                            }
                            else
                            {
                                _hvEtchingRegionRightGapList = _hvEtchingRegionRightGapList.TupleConcat(0);
                            }
                        }

                        _hvEtchingRegionRightGap = _hvEtchingRegionRightGapList.TupleMean();

                        HTuple hvOffsetRow = _hvEtchingRegionRightGap / _hvPhi.TupleCos();

                        HOperatorSet.GenContourPolygonXld(out _hoFitEtchingRegionEndEdge, new HTuple(_hvLeftDownRow - hvOffsetRow, _hvRightDownRow - hvOffsetRow),
                                                          new HTuple(_hvLeftDownColumn, _hvRightDownColumn));
                    }
                    else
                    {
                        _hvEtchingRegionRightGap = -1;

                        HOperatorSet.GenContourPolygonXld(out _hoFitEtchingRegionEndEdge, new HTuple(_hvLeftDownRow, _hvRightDownRow),
                                                          new HTuple(_hvLeftDownColumn, _hvRightDownColumn));
                    }

                    hvTmpRowBegin.Dispose();
                    hvTmpColBegin.Dispose();
                    hvTmpRowEnd.Dispose();
                    hvTmpColEnd.Dispose();
                }
                finally
                {
                    hoTmp?.Dispose();

                }
            }

            return 0;
        }


        /// <summary>
        /// 测量刻蚀线顶部底部到极片边缘的距离
        /// </summary>
        private int MeasureEtchingLineTopBottomGap()
        {
            using (var dh = new HDevDisposeHelper())
            {
                HObject? hoTmp = null;

                try
                {
                    if (_measureParam.PlatePart != 0)
                    {
                        HTuple hvSamplePointNum = _hvPlateTopBottomEdgeSamplePointRows.TupleLength();

                        _hvEtchingRegionTopEdgeRows = new HTuple();
                        _hvEtchingRegionTopEdgeCols = new HTuple();
                        _hvEtchingRegionBottomEdgeRows = new HTuple();
                        _hvEtchingRegionBottomEdgeCols = new HTuple();

                        HTuple hvEtchingRegionMeasureHandle = new HTuple();
                        for (int idx = 0; idx < hvSamplePointNum; idx++)
                        {
                            if (_measureParam.PlatePart == -1)
                            {
                                // 生成拟合刻蚀区顶部边缘句柄
                                HTuple hvSamplingPointRow = _hvPlateTopBottomEdgeSamplePointRows.TupleSelect(idx);
                                HTuple hvSamplingPointCol = (_hvPlateTopBottomEdgeSamplePointCols.TupleSelect(idx)) * 0.5;
                                HTuple hvTmpLength1 = (_hvPlateTopBottomEdgeSamplePointCols.TupleSelect(idx)) * 0.5;
                                HTuple hvTmpLength2 = 125;

                                HOperatorSet.GenMeasureRectangle2(hvSamplingPointRow, hvSamplingPointCol, _hvPhi,
                                                                  hvTmpLength1 - _measureParam.PlateEdgeMaskSize, hvTmpLength2,
                                                                  _measureParam.ScanWidth, _measureParam.ScanHeight, "bilinear",
                                                                  out hvEtchingRegionMeasureHandle);

                            }
                            else if (_measureParam.PlatePart == 1)
                            {
                                // 生成拟合刻蚀区底部边缘句柄
                                HTuple hvSamplingPointRow = _hvPlateTopBottomEdgeSamplePointRows.TupleSelect(idx);
                                HTuple hvSamplingPointCol = ((_measureParam.ScanWidth - _hvPlateTopBottomEdgeSamplePointCols.TupleSelect(idx)) * 0.5) +
                                                              _hvPlateTopBottomEdgeSamplePointCols.TupleSelect(idx);
                                HTuple hvTmpLength1 = (_measureParam.ScanWidth - (_hvPlateTopBottomEdgeSamplePointCols.TupleSelect(idx))) * 0.5;
                                HTuple hvTmpLength2 = 125;

                                HOperatorSet.GenMeasureRectangle2(hvSamplingPointRow, hvSamplingPointCol, _hvPhi + ((new HTuple(180)).TupleRad()),
                                                                  hvTmpLength1 - _measureParam.PlateEdgeMaskSize, hvTmpLength2,
                                                                  _measureParam.ScanWidth, _measureParam.ScanHeight, "bilinear",
                                                                  out hvEtchingRegionMeasureHandle);

                            }

                            HTuple hvTmpEdgeRowNeg, hvTmpEdgeColNeg, hvAmplitudeNeg, hvDistanceNeg;
                            HTuple hvTmpEdgeRowPos, hvTmpEdgeColPos, hvAmplitudePos, hvDistancePos;
                            HOperatorSet.MeasurePos(_hoGrayImage, hvEtchingRegionMeasureHandle, _measureParam.GrayAmplitudeSigma,
                                                    _measureParam.GrayAmplitudeThr, "positive", "last",
                                                    out hvTmpEdgeRowNeg, out hvTmpEdgeColNeg, out hvAmplitudeNeg, out hvDistanceNeg);

                            HOperatorSet.MeasurePos(_hoGrayImage, hvEtchingRegionMeasureHandle, _measureParam.GrayAmplitudeSigma,
                                                    _measureParam.GrayAmplitudeThr, "negative", "last",
                                                    out hvTmpEdgeRowPos, out hvTmpEdgeColPos, out hvAmplitudePos, out hvDistancePos);

                            // 对测量点坐标结果进行筛选
                            if (hvTmpEdgeColNeg.TupleLength() > 0 && hvTmpEdgeColPos.TupleLength() > 0)
                            {
                                HTuple hvNeedFix = new HTuple(0);
                                if (_measureParam.PlatePart == -1)
                                {
                                    if (hvTmpEdgeColNeg.D < hvTmpEdgeColPos.D)
                                    {
                                        hvNeedFix = 1;

                                        _hvEtchingRegionTopEdgeRows = _hvEtchingRegionTopEdgeRows.TupleConcat(_hvPlateTopBottomEdgeSamplePointRows.TupleSelect(idx));
                                        _hvEtchingRegionTopEdgeCols = _hvEtchingRegionTopEdgeCols.TupleConcat(_hvPlateTopBottomEdgeSamplePointCols.TupleSelect(idx));
                                    }
                                    else
                                    {
                                        _hvEtchingRegionTopEdgeRows = _hvEtchingRegionTopEdgeRows.TupleConcat(hvTmpEdgeRowNeg);
                                        _hvEtchingRegionTopEdgeCols = _hvEtchingRegionTopEdgeCols.TupleConcat(hvTmpEdgeColNeg);
                                    }
                                }
                                else if (_measureParam.PlatePart == 1)
                                {
                                    if (hvTmpEdgeColNeg.D > hvTmpEdgeColPos.D)
                                    {
                                        hvNeedFix = 1;

                                        _hvEtchingRegionBottomEdgeRows = _hvEtchingRegionBottomEdgeRows.TupleConcat(_hvPlateTopBottomEdgeSamplePointRows.TupleSelect(idx));
                                        _hvEtchingRegionBottomEdgeCols = _hvEtchingRegionBottomEdgeCols.TupleConcat(_hvPlateTopBottomEdgeSamplePointCols.TupleSelect(idx));
                                    }
                                    else
                                    {
                                        _hvEtchingRegionBottomEdgeRows = _hvEtchingRegionBottomEdgeRows.TupleConcat(hvTmpEdgeRowNeg);
                                        _hvEtchingRegionBottomEdgeCols = _hvEtchingRegionBottomEdgeCols.TupleConcat(hvTmpEdgeColNeg);
                                    }
                                }
                            }

                        }

                        HOperatorSet.CloseMeasure(hvEtchingRegionMeasureHandle);

                        // 计算刻蚀区顶部、底部距离极片顶部、底部的间距，拟合刻蚀线边缘
                        if (_measureParam.PlatePart == -1)
                        {
                            // 顶部
                            HTuple hvTmpPointNum = _hvEtchingRegionTopEdgeRows.TupleLength();

                            _hvEtchingRegionTopGapList = new HTuple();

                            HTuple hvFilterEtchingRegionSampleLineRows = new HTuple();
                            HTuple hvFilterEtchingRegionSampleLineCols = new HTuple();

                            for (int idx = 0; idx < hvTmpPointNum; idx++)
                            {
                                HTuple hvTmpPointRow = _hvEtchingRegionTopEdgeRows.TupleSelect(idx);
                                HTuple hvTmpPointCol = _hvEtchingRegionTopEdgeCols.TupleSelect(idx);

                                HTuple hvTmpPointRowProj, hvTmpPointColProj;
                                HOperatorSet.ProjectionPl(hvTmpPointRow, hvTmpPointCol, _hvRightTopRow, _hvRightTopColumn,
                                                          _hvRightDownRow, _hvRightDownColumn, out hvTmpPointRowProj, out hvTmpPointColProj);
                                if (hvTmpPointCol.D < hvTmpPointColProj.D)
                                {
                                    HTuple hvTmpDist;
                                    HOperatorSet.DistancePp(hvTmpPointRow, hvTmpPointCol, hvTmpPointRowProj, hvTmpPointColProj, out hvTmpDist);

                                    _hvEtchingRegionTopGapList = _hvEtchingRegionTopGapList.TupleConcat(hvTmpDist);
                                    hvFilterEtchingRegionSampleLineRows = hvFilterEtchingRegionSampleLineRows.TupleConcat(hvTmpPointRow);
                                    hvFilterEtchingRegionSampleLineCols = hvFilterEtchingRegionSampleLineCols.TupleConcat(hvTmpPointCol);
                                }
                            }

                            if (_hvEtchingRegionTopGapList.TupleLength() > 0)
                            {
                                _hvEtchingRegionTopGap = _hvEtchingRegionTopGapList.TupleMean();

                                HObject hoEtchingRegionTopEdgeContour;
                                HTuple hvEtchingEdgeRowBegin, hvEtchingEdgeColBegin, hvEtchingEdgeRowEnd, hvEtchingEdgeColEnd;
                                HTuple hvNr1, hvNc1, hvDist1;
                                HOperatorSet.GenContourPolygonXld(out hoEtchingRegionTopEdgeContour, hvFilterEtchingRegionSampleLineRows,
                                                                  hvFilterEtchingRegionSampleLineCols);
                                HOperatorSet.FitLineContourXld(hoEtchingRegionTopEdgeContour, "tukey", -1, 0, 5, 2,
                                                               out hvEtchingEdgeRowBegin, out hvEtchingEdgeColBegin, out hvEtchingEdgeRowEnd,
                                                               out hvEtchingEdgeColEnd, out hvNr1, out hvNc1, out hvDist1);
                                HOperatorSet.GenContourPolygonXld(out _hoFitEtchingRegionTopEdge, new HTuple(hvEtchingEdgeRowBegin, hvEtchingEdgeRowEnd),
                                                                  new HTuple(hvEtchingEdgeColBegin, hvEtchingEdgeColEnd));

                                hoEtchingRegionTopEdgeContour.Dispose();
                            }
                            else
                            {
                                _hvEtchingRegionTopGap = -1;
                                HOperatorSet.GenContourPolygonXld(out _hoFitEtchingRegionTopEdge, new HTuple(_hvRightTopRow, _hvRightDownRow),
                                                                  new HTuple(_hvRightTopColumn, _hvRightDownColumn));
                            }

                            hvFilterEtchingRegionSampleLineRows.Dispose();
                            hvFilterEtchingRegionSampleLineCols.Dispose();
                        }
                        else if (_measureParam.PlatePart == 1)
                        {
                            // 底部
                            HTuple hvTmpPointNum = _hvEtchingRegionBottomEdgeRows.TupleLength();

                            _hvEtchingRegionBottomGapList = new HTuple();

                            HTuple hvFilterEtchingRegionSampleLineRows = new HTuple();
                            HTuple hvFilterEtchingRegionSampleLineCols = new HTuple();

                            for (int idx = 0; idx < hvTmpPointNum; idx++)
                            {
                                HTuple hvTmpPointRow = _hvEtchingRegionBottomEdgeRows.TupleSelect(idx);
                                HTuple hvTmpPointCol = _hvEtchingRegionBottomEdgeCols.TupleSelect(idx);

                                HTuple hvTmpPointRowProj, hvTmpPointColProj;
                                HOperatorSet.ProjectionPl(hvTmpPointRow, hvTmpPointCol, _hvLeftTopRow, _hvLeftTopColumn,
                                                          _hvLeftDownRow, _hvLeftDownColumn, out hvTmpPointRowProj, out hvTmpPointColProj);
                                if (hvTmpPointCol.D > hvTmpPointColProj.D)
                                {
                                    HTuple hvTmpDist;
                                    HOperatorSet.DistancePp(hvTmpPointRow, hvTmpPointCol, hvTmpPointRowProj, hvTmpPointColProj, out hvTmpDist);

                                    _hvEtchingRegionBottomGapList = _hvEtchingRegionBottomGapList.TupleConcat(hvTmpDist);
                                    hvFilterEtchingRegionSampleLineRows = hvFilterEtchingRegionSampleLineRows.TupleConcat(hvTmpPointRow);
                                    hvFilterEtchingRegionSampleLineCols = hvFilterEtchingRegionSampleLineCols.TupleConcat(hvTmpPointCol);
                                }
                            }

                            if (_hvEtchingRegionBottomGapList.TupleLength() > 0)
                            {
                                _hvEtchingRegionBottomGap = _hvEtchingRegionBottomGapList.TupleMean();

                                HObject hoEtchingRegionBottomEdgeContour;
                                HTuple hvEtchingEdgeRowBegin, hvEtchingEdgeColBegin, hvEtchingEdgeRowEnd, hvEtchingEdgeColEnd;
                                HTuple hvNr1, hvNc1, hvDist1;
                                HOperatorSet.GenContourPolygonXld(out hoEtchingRegionBottomEdgeContour, hvFilterEtchingRegionSampleLineRows,
                                                                  hvFilterEtchingRegionSampleLineCols);
                                HOperatorSet.FitLineContourXld(hoEtchingRegionBottomEdgeContour, "tukey", -1, 0, 5, 2,
                                                               out hvEtchingEdgeRowBegin, out hvEtchingEdgeColBegin, out hvEtchingEdgeRowEnd,
                                                               out hvEtchingEdgeColEnd, out hvNr1, out hvNc1, out hvDist1);
                                HOperatorSet.GenContourPolygonXld(out _hoFitEtchingRegionBottomEdge, new HTuple(hvEtchingEdgeRowBegin, hvEtchingEdgeRowEnd),
                                                                  new HTuple(hvEtchingEdgeColBegin, hvEtchingEdgeColEnd));

                                hoEtchingRegionBottomEdgeContour.Dispose();
                            }
                            else
                            {
                                _hvEtchingRegionBottomGap = -1;
                                HOperatorSet.GenContourPolygonXld(out _hoFitEtchingRegionBottomEdge, new HTuple(_hvLeftTopRow, _hvLeftDownRow),
                                                                  new HTuple(_hvLeftTopColumn, _hvLeftDownColumn));
                            }

                            hvFilterEtchingRegionSampleLineRows.Dispose();
                            hvFilterEtchingRegionSampleLineCols.Dispose();
                        }

                        hvEtchingRegionMeasureHandle.Dispose();
                    }
                }
                finally
                {
                    hoTmp?.Dispose();
                }
            }

            return 0;
        }


        /// <summary>
        /// 计算测量区域中心
        /// </summary>
        private int CalculateMeasureRegionCenter(HObject hoEdgeLine1, HObject hoEdgeLine2, HTuple hvBasePointRow, HTuple hvBasePointCol,
                                                 HTuple hvPhi, HTuple hvOffsetPointRow, HTuple hvOffsetPointCol, out HTuple hvRegionCenterRow,
                                                 out HTuple hvRegionCenterCol, out HTuple hvRegionWidth, out HTuple hvRegionHeight)
        {
            using (var dh = new HDevDisposeHelper())
            {
                HObject? hoTmp = null;

                HTuple? hvCornerRowList = null;
                HTuple? hvCornerColList = null;

                HTuple? hvEdgeLine1Rows = null; 
                HTuple? hvEdgeLine1Cols = null; 
                HTuple? hvEdgeLine2Rows = null; 
                HTuple? hvEdgeLine2Cols = null;

                try
                {
                    hvRegionCenterRow = new HTuple();
                    hvRegionCenterCol = new HTuple();
                    hvRegionWidth = new HTuple();
                    hvRegionHeight = new HTuple();

                    HTuple hvOffsetRow = hvOffsetPointRow - hvBasePointRow;
                    HTuple hvOffsetCol = hvOffsetPointCol - hvBasePointCol;

                    hvCornerRowList = new HTuple();
                    hvCornerColList = new HTuple();

                    HOperatorSet.GetContourXld(hoEdgeLine1, out hvEdgeLine1Rows, out hvEdgeLine1Cols);
                    HOperatorSet.GetContourXld(hoEdgeLine2, out hvEdgeLine2Rows, out hvEdgeLine2Cols);

                    HTuple hvLeftTopRow, hvLeftTopCol, hvRightTopRow, hvRightTopCol;
                    HTuple hvRightDownRow, hvRightDownCol, hvLeftDownRow, hvLeftDownCol;
                    HTuple hvIsOverlapping;
                    HOperatorSet.IntersectionLines(hvEdgeLine1Rows.TupleSelect(0), hvEdgeLine1Cols.TupleSelect(0),
                                                   hvEdgeLine1Rows.TupleSelect(1), hvEdgeLine1Cols.TupleSelect(1),
                                                   hvBasePointRow, hvBasePointCol, hvBasePointRow - (2 * (hvPhi.TupleSin())), hvBasePointCol + (2 * (hvPhi.TupleCos())),
                                                   out hvLeftTopRow, out hvLeftTopCol, out hvIsOverlapping);
                    HOperatorSet.IntersectionLines(hvEdgeLine2Rows.TupleSelect(0), hvEdgeLine2Cols.TupleSelect(0),
                                                   hvEdgeLine2Rows.TupleSelect(1), hvEdgeLine2Cols.TupleSelect(1),
                                                   hvBasePointRow, hvBasePointCol, hvBasePointRow - (2 * (hvPhi.TupleSin())), hvBasePointCol + (2 * (hvPhi.TupleCos())),
                                                   out hvRightTopRow, out hvRightTopCol, out hvIsOverlapping);
                    HOperatorSet.IntersectionLines(hvEdgeLine2Rows.TupleSelect(0), hvEdgeLine2Cols.TupleSelect(0),
                                                   hvEdgeLine2Rows.TupleSelect(1), hvEdgeLine2Cols.TupleSelect(1),
                                                   hvOffsetPointRow, hvOffsetPointCol, hvOffsetPointRow - (2 * (hvPhi.TupleSin())), hvOffsetPointCol + (2 * (hvPhi.TupleCos())),
                                                   out hvRightDownRow, out hvRightDownCol, out hvIsOverlapping);
                    HOperatorSet.IntersectionLines(hvEdgeLine1Rows.TupleSelect(0), hvEdgeLine1Cols.TupleSelect(0),
                                                   hvEdgeLine1Rows.TupleSelect(1), hvEdgeLine1Cols.TupleSelect(1),
                                                   hvOffsetPointRow, hvOffsetPointCol, hvOffsetPointRow - (2 * (hvPhi.TupleSin())), hvOffsetPointCol + (2 * (hvPhi.TupleCos())),
                                                   out hvLeftDownRow, out hvLeftDownCol, out hvIsOverlapping);

                    hvCornerRowList = (hvCornerRowList.TupleConcat(hvLeftTopRow)).TupleConcat(hvRightTopRow);
                    hvCornerColList = (hvCornerColList.TupleConcat(hvLeftTopCol)).TupleConcat(hvRightTopCol);

                    HTuple hvRowProj1, hvColProj1, hvRowProj2, hvColProj2;
                    HOperatorSet.ProjectionPl(hvRightDownRow, hvRightDownCol, hvLeftTopRow, hvLeftTopCol, hvRightTopRow, hvRightTopCol, out hvRowProj1, out hvColProj1);
                    HOperatorSet.ProjectionPl(hvLeftDownRow, hvLeftDownCol, hvLeftTopRow, hvLeftTopCol, hvRightTopRow, hvRightTopCol, out hvRowProj2, out hvColProj2);

                    hvCornerRowList = (hvCornerRowList.TupleConcat(hvRowProj1)).TupleConcat(hvRowProj2);
                    hvCornerColList = (hvCornerColList.TupleConcat(hvColProj1)).TupleConcat(hvColProj2);

                    HTuple hvIndices;
                    HOperatorSet.TupleSortIndex(hvCornerColList, out hvIndices);

                    HOperatorSet.DistancePp(hvCornerRowList.TupleSelect(hvIndices.TupleSelect(1)), hvCornerColList.TupleSelect(hvIndices.TupleSelect(1)),
                                            hvCornerRowList.TupleSelect(hvIndices.TupleSelect(2)), hvCornerColList.TupleSelect(hvIndices.TupleSelect(2)),
                                            out hvRegionWidth);
                    hvRegionHeight = hvOffsetRow * (hvPhi.TupleCos());

                    HTuple hvTmpCenterPointRow = (hvCornerRowList.TupleSelect(hvIndices.TupleSelect(1)) + hvCornerRowList.TupleSelect(hvIndices.TupleSelect(2))) * 0.5;
                    HTuple hvTmpCenterPointCol = (hvCornerColList.TupleSelect(hvIndices.TupleSelect(1)) + hvCornerColList.TupleSelect(hvIndices.TupleSelect(2))) * 0.5;


                    HTuple hvCenterOffsetR = 0.5 * hvRegionHeight * hvPhi.TupleCos();
                    HTuple hvCenterOffsetC = 0.5 * hvRegionHeight * hvPhi.TupleSin();

                    hvRegionCenterRow = hvTmpCenterPointRow + hvCenterOffsetR;
                    hvRegionCenterCol = hvTmpCenterPointCol + hvCenterOffsetC;
 
                }
                finally
                {
                    hoTmp?.Dispose();

                    hvCornerRowList?.Dispose();
                    hvCornerColList?.Dispose();
                    hvEdgeLine1Rows?.Dispose();
                    hvEdgeLine1Cols?.Dispose();
                    hvEdgeLine2Rows?.Dispose();
                    hvEdgeLine2Cols?.Dispose();
                }
            }

            return 0;
        }


        private int MeasureLineIntersectionDistance(HObject hoLine1, HObject hoLine2, double centerX, double centerY, string axis, out HTuple hvDist)
        {
            using (var dh = new HDevDisposeHelper())
            {
                HObject? hoTmp = null;

                HTuple? hvLine1Rows = null; 
                HTuple? hvLine1Cols = null; 
                HTuple? hvLine2Rows = null; 
                HTuple? hvLine2Cols = null;

                try
                {
                    
                    HOperatorSet.GetContourXld(hoLine1, out hvLine1Rows, out hvLine1Cols);
                    HOperatorSet.GetContourXld(hoLine2, out hvLine2Rows, out hvLine2Cols);

                    HTuple hvLine1IntersectionRow, hvLine1IntersectionCol, hvLine2IntersectionRow, hvLine2IntersectionCol;
                    HTuple hvIsOverlapping;

                    if (axis == "x")
                    {
                        HOperatorSet.IntersectionLines(hvLine1Rows.TupleSelect(0), hvLine1Cols.TupleSelect(0),
                                                       hvLine1Rows.TupleSelect(1), hvLine1Cols.TupleSelect(1),
                                                       centerY, centerX, centerY - (2 * (_hvPhi.TupleSin())), centerX + (2 * (_hvPhi.TupleCos())),
                                                       out hvLine1IntersectionRow, out hvLine1IntersectionCol, out hvIsOverlapping);

                        HOperatorSet.IntersectionLines(hvLine2Rows.TupleSelect(0), hvLine2Cols.TupleSelect(0),
                                                       hvLine2Rows.TupleSelect(1), hvLine2Cols.TupleSelect(1),
                                                       centerY, centerX, centerY - (2 * (_hvPhi.TupleSin())), centerX + (2 * (_hvPhi.TupleCos())),
                                                       out hvLine2IntersectionRow, out hvLine2IntersectionCol, out hvIsOverlapping);
                    }
                    else
                    {
                        HOperatorSet.IntersectionLines(hvLine1Rows.TupleSelect(0), hvLine1Cols.TupleSelect(0),
                                                       hvLine1Rows.TupleSelect(1), hvLine1Cols.TupleSelect(1),
                                                       centerY, centerX, centerY - (2 * (_hvPhi.TupleCos())), centerX + (2 * (_hvPhi.TupleSin())),
                                                       out hvLine1IntersectionRow, out hvLine1IntersectionCol, out hvIsOverlapping);

                        HOperatorSet.IntersectionLines(hvLine2Rows.TupleSelect(0), hvLine2Cols.TupleSelect(0),
                                                       hvLine2Rows.TupleSelect(1), hvLine2Cols.TupleSelect(1),
                                                       centerY, centerX, centerY - (2 * (_hvPhi.TupleCos())), centerX + (2 * (_hvPhi.TupleSin())),
                                                       out hvLine2IntersectionRow, out hvLine2IntersectionCol, out hvIsOverlapping);
                    }

                    // 沿X轴方向，hoLine1在hoLine2左侧
                    if (axis == "x")
                    {
                        if (hvLine1IntersectionCol < hvLine2IntersectionCol)
                        {
                            HOperatorSet.DistancePp(hvLine1IntersectionRow, hvLine1IntersectionCol, hvLine2IntersectionRow, hvLine2IntersectionCol,
                                                    out hvDist);
                        }
                        else
                        {
                            hvDist = 0;
                        }
                    }
                    // 沿y轴方向，hoLine1在hoLine2上边
                    else
                    {
                        if (hvLine1IntersectionRow < hvLine2IntersectionRow)
                        {
                            HOperatorSet.DistancePp(hvLine1IntersectionRow, hvLine1IntersectionCol, hvLine2IntersectionRow, hvLine2IntersectionCol,
                                                    out hvDist);
                        }
                        else
                        {
                            hvDist = 0;
                        }
                    }
                }
                finally
                {
                    hoTmp?.Dispose();

                    hvLine1Rows?.Dispose();
                    hvLine1Cols?.Dispose();
                    hvLine2Rows?.Dispose();
                    hvLine2Cols?.Dispose();
                }
            }

            return 0;
        }


        private double[] CreateDoubleArrayWithStep(double start, double step, int count)
        {
            double[] array = new double[count];
            for (int i = 0; i < count; i++)
            {
                array[i] = start + i * (step / 100);
            }
            return array;
        }


        /// <summary>
        /// 测量刻蚀线宽度
        /// </summary>
        private int GetEtchingWidth(HObject hoGrayImage, HTuple hvCenterRow, HTuple hvCenterCol, HTuple hvPhi, HTuple hvDetRegW, HTuple hvDetRegH,
                                    HTuple hvWidthRoughly, out HTuple hvEtchingLineWidth)
        {
            using (var dh = new HDevDisposeHelper())
            {
                HObject? hoTmp = null;
                HObject? hoContours = null;
                HObject? hoFitLinePos = null;
                HObject? hoFitLineNeg = null;
                HObject? hoFitLinePosWithDist = null;

                HTuple hvMetrologyHandle = new HTuple();
                HTuple hvIndexPos = new HTuple();
                HTuple hvIndexNeg = new HTuple();
                HTuple hvEdgePosRows = new HTuple();
                HTuple hvEdgePosCols = new HTuple();
                HTuple hvEdgeNegRows = new HTuple();
                HTuple hvEdgeNegCols = new HTuple();
                HTuple hvParameter = new HTuple();

                HTuple hvEdgePosRowBegin = new HTuple();
                HTuple hvEdgePosColBegin = new HTuple();
                HTuple hvEdgePosRowEnd = new HTuple();
                HTuple hvEdgePosColEnd = new HTuple();
                HTuple hvEdgeNegRowBegin = new HTuple();
                HTuple hvEdgeNegColBegin = new HTuple();
                HTuple hvEdgeNegRowEnd = new HTuple();
                HTuple hvEdgeNegColEnd = new HTuple();

                HTuple hvNr = new HTuple();
                HTuple hvNc = new HTuple();
                HTuple hvDist = new HTuple();

                HTuple hvTmpWidth = new HTuple();
                HTuple hvEtchingLineWidthList = new HTuple();

                hvEtchingLineWidth = new HTuple();

                try
                {
                    
                    HTuple hvSampleNum = ((hvDetRegH / 256)).TupleInt();
                    //HTuple hvSampleNum = ((hvDetRegH / 64)).TupleInt();
                    if (hvSampleNum == 0)
                    {
                        hvSampleNum = new HTuple(1);
                    }

                    HTuple hvStep = hvDetRegH / hvSampleNum;

                    HTuple hvOffsetR = 0.5 * hvDetRegH * (hvPhi.TupleCos());
                    HTuple hvOffsetC = 0.5 * hvDetRegH * (hvPhi.TupleSin());
                    HTuple hvTopPointRow = hvCenterRow - hvDetRegH * (hvPhi.TupleCos());
                    HTuple hvTopPointCol = hvCenterCol - hvDetRegH * (hvPhi.TupleSin());
                    HTuple hvBottomPointRow = hvCenterRow + hvDetRegH * (hvPhi.TupleCos());
                    HTuple hvBottomPointCol = hvCenterCol + hvDetRegH * (hvPhi.TupleSin());

                    HTuple hvRowSequence, hvColSequence;
                    if (hvPhi > 0)
                    {
                        HTuple hvRowStep = (hvBottomPointRow - hvTopPointRow) / hvSampleNum;
                        HTuple hvColStep = (hvBottomPointCol - hvTopPointCol) / hvSampleNum;
                        HOperatorSet.TupleGenSequence(hvTopPointRow, hvBottomPointRow, hvRowStep, out hvRowSequence);
                        HOperatorSet.TupleGenSequence(hvTopPointCol, hvBottomPointCol, hvColStep, out hvColSequence);
                    }
                    else
                    {
                        HTuple hvRowStep = (hvBottomPointRow - hvTopPointRow) / hvSampleNum;
                        HTuple hvColStep = (hvTopPointCol - hvBottomPointCol) / hvSampleNum;
                        HOperatorSet.TupleGenSequence(hvTopPointRow, hvBottomPointRow, hvRowStep, out hvRowSequence);
                        HOperatorSet.TupleInverse(hvRowSequence, out hvRowSequence);
                        HOperatorSet.TupleGenSequence(hvBottomPointCol, hvTopPointCol, hvColStep, out hvColSequence);
                    }

                    HTuple hvItemNum = (new HTuple(hvRowSequence.TupleLength())).TupleMin2(new HTuple(hvColSequence.TupleLength()));
                    for (int idx = 0; idx < hvItemNum - 1; idx++)
                    {
                        HTuple hvTmpRow = (hvRowSequence.TupleSelect(idx) + hvRowSequence.TupleSelect(idx + 1)) * 0.5;
                        HTuple hvTmpCol = (hvColSequence.TupleSelect(idx) + hvColSequence.TupleSelect(idx + 1)) * 0.5;

                        HTuple hvStepTopPointRow = hvTmpRow - hvStep * (hvPhi.TupleCos());
                        HTuple hvStepTopPointCol = hvTmpCol - hvStep * (hvPhi.TupleSin());
                        HTuple hvStepBottomPointRow = hvTmpRow + hvStep * (hvPhi.TupleCos());
                        HTuple hvStepBottomPointCol = hvTmpCol + hvStep * (hvPhi.TupleSin());

                        HOperatorSet.CreateMetrologyModel(out hvMetrologyHandle);
                        HOperatorSet.SetMetrologyModelImageSize(hvMetrologyHandle, hvDetRegW, hvStep);

                        //HOperatorSet.AddMetrologyObjectLineMeasure(hvMetrologyHandle, hvTopPointRow, hvTopPointCol, hvBottomPointRow, hvBottomPointCol,
                        //                                           hvDetRegW, 20, _measureParam.GrayAmplitudeSigma, _measureParam.GrayAmplitudeThr,
                        //                                           new HTuple(), new HTuple(), out hvIndexPos);
                        //HOperatorSet.AddMetrologyObjectLineMeasure(hvMetrologyHandle, hvTopPointRow, hvTopPointCol, hvBottomPointRow, hvBottomPointCol,
                        //                                           hvDetRegW, 20, _measureParam.GrayAmplitudeSigma, _measureParam.GrayAmplitudeThr,
                        //                                           new HTuple(), new HTuple(), out hvIndexNeg);
                        HOperatorSet.AddMetrologyObjectLineMeasure(hvMetrologyHandle, hvStepTopPointRow, hvStepTopPointCol, hvStepBottomPointRow, hvStepBottomPointCol,
                                                                   hvDetRegW, 20, 4, _measureParam.GrayAmplitudeThr, new HTuple(), new HTuple(), out hvIndexPos);
                        HOperatorSet.AddMetrologyObjectLineMeasure(hvMetrologyHandle, hvStepTopPointRow, hvStepTopPointCol, hvStepBottomPointRow, hvStepBottomPointCol,
                                                                   hvDetRegW, 20, 4, _measureParam.GrayAmplitudeThr, new HTuple(), new HTuple(), out hvIndexNeg);

                        HOperatorSet.SetMetrologyObjectParam(hvMetrologyHandle, hvIndexPos, "measure_transition", "positive");
                        HOperatorSet.SetMetrologyObjectParam(hvMetrologyHandle, hvIndexNeg, "measure_transition", "negative");

                        int numMeasures = (int)(hvStep.D / 10.0);
                        HOperatorSet.SetMetrologyObjectParam(hvMetrologyHandle, "all", "num_measures", numMeasures);
                        HOperatorSet.SetMetrologyObjectParam(hvMetrologyHandle, "all", "measure_select", "first");
                        HOperatorSet.SetMetrologyObjectParam(hvMetrologyHandle, "all", "num_instances", 1);
                        HOperatorSet.SetMetrologyObjectParam(hvMetrologyHandle, "all", "min_score", 0.5);

                        HOperatorSet.ApplyMetrologyModel(hoGrayImage, hvMetrologyHandle);
                        HOperatorSet.GetMetrologyObjectMeasures(out hoContours, hvMetrologyHandle, hvIndexPos, "positive", out hvEdgePosRows, out hvEdgePosCols);
                        HOperatorSet.GetMetrologyObjectMeasures(out hoContours, hvMetrologyHandle, hvIndexNeg, "negative", out hvEdgeNegRows, out hvEdgeNegCols);
                        HOperatorSet.GetMetrologyObjectResultContour(out hoFitLinePos, hvMetrologyHandle, hvIndexPos, "all", 1.5);
                        HOperatorSet.GetMetrologyObjectResultContour(out hoFitLineNeg, hvMetrologyHandle, hvIndexNeg, "all", 1.5);

                        HOperatorSet.CountObj(hoFitLinePos, out HTuple hvFitLinePosNum);
                        HOperatorSet.CountObj(hoFitLineNeg, out HTuple hvFitLineNegNum);
                        if (hvFitLinePosNum.I == 0)
                        {
                            if (hvEdgePosRows.TupleLength() > 1)
                            {
                                HObject hoPlatEdgeRightContour;
                                HOperatorSet.GenEmptyObj(out hoPlatEdgeRightContour);

                                HOperatorSet.GenContourPolygonXld(out hoPlatEdgeRightContour, hvEdgePosRows, hvEdgePosCols);
                                HOperatorSet.FitLineContourXld(hoPlatEdgeRightContour, "tukey", -1, 0, 5, 2, out hvEdgePosRowBegin, out hvEdgePosColBegin, out hvEdgePosRowEnd, out hvEdgePosColEnd,
                                                               out hvNr, out hvNc, out hvDist);
                                HOperatorSet.GenContourPolygonXld(out hoTmp, hvEdgePosRowBegin.TupleConcat(hvEdgePosRowEnd), hvEdgePosColBegin.TupleConcat(hvEdgePosColEnd));
                                ReplaceHobject(ref hoFitLinePos, ref hoTmp);

                                hoPlatEdgeRightContour.Dispose();
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else
                        {
                            HOperatorSet.FitLineContourXld(hoFitLinePos, "tukey", -1, 0, 5, 2, out hvEdgePosRowBegin, out hvEdgePosColBegin, out hvEdgePosRowEnd, out hvEdgePosColEnd,
                                                           out hvNr, out hvNc, out hvDist);
                        }
                        if (hvFitLineNegNum.I == 0)
                        {
                            if (hvEdgeNegRows.TupleLength() > 1)
                            {
                                HObject hoPlatEdgeRightContour;
                                HOperatorSet.GenEmptyObj(out hoPlatEdgeRightContour);

                                HOperatorSet.GenContourPolygonXld(out hoPlatEdgeRightContour, hvEdgePosRows, hvEdgePosCols);
                                HOperatorSet.FitLineContourXld(hoPlatEdgeRightContour, "tukey", -1, 0, 5, 2, out hvEdgeNegRowBegin, out hvEdgeNegColBegin, out hvEdgeNegRowEnd, out hvEdgeNegColEnd,
                                                               out hvNr, out hvNc, out hvDist);
                                HOperatorSet.GenContourPolygonXld(out hoTmp, hvEdgeNegRowBegin.TupleConcat(hvEdgeNegRowEnd), hvEdgeNegColBegin.TupleConcat(hvEdgeNegColEnd));
                                ReplaceHobject(ref hoFitLineNeg, ref hoTmp);

                                hoPlatEdgeRightContour.Dispose();
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else
                        {
                            HOperatorSet.FitLineContourXld(hoFitLineNeg, "tukey", -1, 0, 5, 2, out hvEdgeNegRowBegin, out hvEdgeNegColBegin, out hvEdgeNegRowEnd, out hvEdgeNegColEnd,
                                                           out hvNr, out hvNc, out hvDist);
                        }

                        HTuple hvEdgePosCenterRow = (hvEdgePosRowBegin + hvEdgePosRowEnd) * 0.5;
                        HTuple hvEdgePosCenterCol = (hvEdgePosColBegin + hvEdgePosColEnd) * 0.5;
                        HTuple hvEdgeNegCenterRow = (hvEdgeNegRowBegin + hvEdgeNegRowEnd) * 0.5;
                        HTuple hvEdgeNegCenterCol = (hvEdgeNegColBegin + hvEdgeNegColEnd) * 0.5;
                        HOperatorSet.DistancePl(hvEdgePosCenterRow, hvEdgePosCenterCol, hvEdgeNegRowBegin, hvEdgeNegColBegin, hvEdgeNegRowEnd, hvEdgeNegColEnd, out HTuple hvDistancePosNeg);
                        HOperatorSet.DistancePl(hvEdgeNegCenterRow, hvEdgeNegCenterCol, hvEdgePosRowBegin, hvEdgePosColBegin, hvEdgePosRowEnd, hvEdgePosColEnd, out HTuple hvDistanceNegPos);
                        hvTmpWidth = (hvDistancePosNeg + hvDistanceNegPos) * 0.5;

                        hvEtchingLineWidthList = hvEtchingLineWidthList.TupleConcat(hvTmpWidth);
                    }

                    if (hvEtchingLineWidthList.Length > 0)
                    {
                        HOperatorSet.TupleSort(hvEtchingLineWidthList, out HTuple hvEtchingLineWidthSorted);
                        HTuple hvWidthCount = hvEtchingLineWidthSorted.TupleLength();
                        HTuple hvTrimCount = (hvWidthCount / 4).TupleInt();
                        HTuple hvMiddleStart = hvTrimCount;
                        HTuple hvMiddleEnd = hvWidthCount - hvTrimCount - 1;
                        HOperatorSet.TupleSelectRange(hvEtchingLineWidthSorted, hvMiddleStart, hvMiddleEnd, out HTuple hvEtchingLineWidthMiddle);
                        HOperatorSet.TupleMean(hvEtchingLineWidthMiddle, out hvEtchingLineWidth);
                    }

                }
                catch
                {
                    if(hvEtchingLineWidthList.Length > 0)
                    {
                        HOperatorSet.TupleSort(hvEtchingLineWidthList, out HTuple hvEtchingLineWidthSorted);
                        HTuple hvWidthCount = hvEtchingLineWidthSorted.TupleLength();
                        HTuple hvTrimCount = (hvWidthCount / 4).TupleInt();
                        HTuple hvMiddleStart = hvTrimCount;
                        HTuple hvMiddleEnd = hvWidthCount - hvTrimCount - 1;
                        HOperatorSet.TupleSelectRange(hvEtchingLineWidthSorted, hvMiddleStart, hvMiddleEnd, out HTuple hvEtchingLineWidthMiddle);
                        HOperatorSet.TupleMean(hvEtchingLineWidthMiddle, out hvEtchingLineWidth);
                    }
                }
                finally
                {
                    hoTmp?.Dispose();
                    hoContours?.Dispose();
                    hoFitLinePos?.Dispose();
                    hoFitLineNeg?.Dispose();
                    hoFitLinePosWithDist?.Dispose();

                    hvMetrologyHandle.Dispose();
                    hvIndexPos.Dispose();
                    hvIndexNeg.Dispose();
                    hvEdgePosRows.Dispose();
                    hvEdgePosCols.Dispose();
                    hvEdgeNegRows.Dispose();
                    hvEdgeNegCols.Dispose();
                    hvParameter.Dispose();

                }

            }

            return 0;
        }


        /// <summary>
        /// 测量刻蚀线深度
        /// </summary>
        private int GetEtchingDepth(HObject hoHeightImage, HTuple hvCenterRow, HTuple hvCenterCol, HTuple hvPhi, HTuple hvDetRegW, HTuple hvDetRegH,
                                    HTuple hvEtchingLineWidth, out HTuple hvEtchingLineDepth, out HTuple hvPoleXOffset)
        {
            using (var dh = new HDevDisposeHelper())
            {
                HObject? hoTmp = null;

                HObject? hoMeasureDepthRegion = null; 
                HObject? hoHeightImageReduced = null;
                HObject? hoHeightImagePart = null;
                HObject? hoMeasureDepthRegionEro = null;
                HObject? hoTmpMask = null;
                HObject? hoFitSurface = null;
                HObject? hoImageSub = null;
                HObject? hoFlatContour = null;
                HObject? hoFitLineContour = null;
                HObject? ZeroRegion = null;

                HTuple? hvHorProjection = null; 
                HTuple? hvVertProjection = null;

                try
                {
                    hvPoleXOffset = new HTuple();
                    hvEtchingLineDepth = new HTuple();

                    //临时代码
                    //HOperatorSet.GenRectangle2(out HObject R000, hvCenterRow, hvCenterCol, hvPhi, hvDetRegW, hvDetRegH);
                    //HOperatorSet.ReduceDomain(_hoHeightImage, R000, out HObject HeightImageR000);
                    //HOperatorSet.CropDomain(HeightImageR000, out HObject HeightImageR000Part);
                    //HOperatorSet.WriteImage(HeightImageR000Part, "tiff", 8888880, "C:/Users/REECHI05/Desktop/22/CSharp_1.tiff");

                    // 计算刻蚀线深度
                    HTuple hvSampleNum = ((hvDetRegH / 256)).TupleInt();
                    //HTuple hvSampleNum = ((hvDetRegH / 64)).TupleInt();
                    if (hvSampleNum == 0)
                    {
                        hvSampleNum = new HTuple(1);
                    }

                    HTuple hvStep = hvDetRegH / hvSampleNum;

                    HTuple hvOffsetR = 0.5 * hvDetRegH * (hvPhi.TupleCos());
                    HTuple hvOffsetC = 0.5 * hvDetRegH * (hvPhi.TupleSin());
                    HTuple hvTopPointRow = hvCenterRow - hvDetRegH * (hvPhi.TupleCos());
                    HTuple hvTopPointCol = hvCenterCol - hvDetRegH * (hvPhi.TupleSin());
                    HTuple hvBottomPointRow = hvCenterRow + hvDetRegH * (hvPhi.TupleCos());
                    HTuple hvBottomPointCol = hvCenterCol + hvDetRegH * (hvPhi.TupleSin());

                    HTuple hvRowSequence, hvColSequence;
                    if (hvPhi > 0)
                    {
                        HTuple hvRowStep = (hvBottomPointRow - hvTopPointRow) / hvSampleNum;
                        HTuple hvColStep = (hvBottomPointCol - hvTopPointCol) / hvSampleNum;
                        HOperatorSet.TupleGenSequence(hvTopPointRow, hvBottomPointRow, hvRowStep, out hvRowSequence);
                        HOperatorSet.TupleGenSequence(hvTopPointCol, hvBottomPointCol, hvColStep, out hvColSequence);
                    }
                    else
                    {
                        HTuple hvRowStep = (hvBottomPointRow - hvTopPointRow) / hvSampleNum;
                        HTuple hvColStep = (hvTopPointCol - hvBottomPointCol) / hvSampleNum;
                        HOperatorSet.TupleGenSequence(hvTopPointRow, hvBottomPointRow, hvRowStep, out hvRowSequence);
                        HOperatorSet.TupleInverse(hvRowSequence, out hvRowSequence);
                        HOperatorSet.TupleGenSequence(hvBottomPointCol, hvTopPointCol, hvColStep, out hvColSequence);
                    }

                    HTuple hvPoleXList = new HTuple();
                    HTuple hvEtchingLineDepthList = new HTuple();

                    HTuple hvItemNum = (new HTuple(hvRowSequence.TupleLength())).TupleMin2(new HTuple(hvColSequence.TupleLength()));
                    //for (int idx = 1; idx < hvItemNum - 1; idx++)
                    for (int idx = 0; idx < hvItemNum - 1; idx++)
                    {
                        HTuple hvTmpRow = (hvRowSequence.TupleSelect(idx) + hvRowSequence.TupleSelect(idx + 1)) * 0.5;
                        HTuple hvTmpCol = (hvColSequence.TupleSelect(idx) + hvColSequence.TupleSelect(idx + 1)) * 0.5;

                        HOperatorSet.GenRectangle2(out hoMeasureDepthRegion, hvTmpRow, hvTmpCol, hvPhi, hvDetRegW, hvStep);
                        HOperatorSet.Intersection(hoMeasureDepthRegion, _hoValidRegion, out hoTmp);
                        ReplaceHobject(ref hoMeasureDepthRegion, ref hoTmp);
                        HOperatorSet.ReduceDomain(hoHeightImage, hoMeasureDepthRegion, out hoHeightImageReduced);

                        // crop进小图来处理
                        HOperatorSet.CropDomain(hoHeightImageReduced, out hoHeightImagePart);
                        HOperatorSet.GetDomain(hoHeightImagePart, out hoMeasureDepthRegion);
                        double phi = Math.Atan2(Math.Sin(hvPhi.D), Math.Cos(hvPhi.D)); // -> [-pi, pi]
                        if (phi > Math.PI / 2.0)
                            phi -= Math.PI;
                        if (phi < -Math.PI / 2.0)
                            phi += Math.PI;
                        HTuple hvPhiNorm = phi;
                        HOperatorSet.AreaCenter(hoMeasureDepthRegion, out HTuple _, out HTuple hvCenterRowPart, out HTuple hvCenterColPart);
                        HOperatorSet.HomMat2dIdentity(out HTuple hvHom);
                        HOperatorSet.HomMat2dRotate(hvHom, -hvPhiNorm, hvCenterRowPart, hvCenterColPart, out hvHom);
                        HOperatorSet.HomMat2dTranslate(hvHom, 0.5, 0.5, out HTuple hvHomTmp);
                        HOperatorSet.HomMat2dTranslateLocal(hvHomTmp, -0.5, -0.5, out HTuple hvHomAdapted);
                        HOperatorSet.AffineTransImage(hoHeightImagePart, out hoTmp, hvHomAdapted, "nearest_neighbor", "true");
                        ReplaceHobject(ref hoHeightImagePart, ref hoTmp);
                        HOperatorSet.AffineTransRegion(hoMeasureDepthRegion, out hoTmp, hvHomAdapted, "nearest_neighbor");
                        ReplaceHobject(ref hoMeasureDepthRegion, ref hoTmp);
                        HOperatorSet.Threshold(hoHeightImagePart, out ZeroRegion, 0, 0);
                        HOperatorSet.Difference(hoMeasureDepthRegion, ZeroRegion, out hoTmp);
                        ReplaceHobject(ref hoMeasureDepthRegion, ref hoTmp);
                        HOperatorSet.ReduceDomain(hoHeightImagePart, hoMeasureDepthRegion, out hoTmp);
                        ReplaceHobject(ref hoHeightImageReduced, ref hoTmp);

                        HOperatorSet.ErosionCircle(hoMeasureDepthRegion, out hoMeasureDepthRegionEro, 3);
                        HOperatorSet.RegionFeatures(hoMeasureDepthRegionEro, "area", out HTuple hvTmpArea);
                        HOperatorSet.GetImageSize(hoHeightImageReduced, out HTuple hvSampleWidth, out HTuple hvSampleHeight);

                        if (hvTmpArea.D > 100)
                        {
                            HOperatorSet.FitSurfaceFirstOrder(hoMeasureDepthRegionEro, hoHeightImageReduced, "tukey", 5, 1, out HTuple hv_Alpha, out HTuple hv_Beta, out HTuple hv_Gamma);
                            HOperatorSet.AreaCenter(hoMeasureDepthRegionEro, out HTuple hvTmpArea0, out HTuple hvTmpCenterR, out HTuple hvTmpCenterC);
                            HOperatorSet.GenImageSurfaceFirstOrder(out hoFitSurface, "real", hv_Alpha, hv_Beta, hv_Gamma, hvTmpCenterR, hvTmpCenterC, hvSampleWidth, hvSampleHeight);
                            HOperatorSet.SubImage(hoHeightImageReduced, hoFitSurface, out hoImageSub, 1, 0);
                            HOperatorSet.Threshold(hoImageSub, out hoTmpMask, -700, 700);

                            hoFitSurface.Dispose(); hoImageSub.Dispose();
                        }
                        else
                        {
                            HOperatorSet.GenRectangle1(out hoTmpMask, 0, 0, hvSampleHeight - 1, hvSampleWidth - 1);
                        }
                        HOperatorSet.Intersection(hoMeasureDepthRegion, hoTmpMask, out hoTmp);
                        ReplaceHobject(ref hoMeasureDepthRegion, ref hoTmp);
                        HOperatorSet.ReduceDomain(hoHeightImageReduced, hoMeasureDepthRegion, out hoTmp);
                        ReplaceHobject(ref hoHeightImageReduced, ref hoTmp);

                        HOperatorSet.GrayProjections(hoMeasureDepthRegion, hoHeightImageReduced, "simple", out hvHorProjection, out hvVertProjection);

                        //截取有效测量区域
                        HTuple hvGrooveStart = (hvDetRegW - hvEtchingLineWidth * 0.75).TupleInt();
                        HTuple hvGrooveEnd = (hvDetRegW + hvEtchingLineWidth * 0.75).TupleInt();

                        HTuple hvDepthProjection;
                        //if (hvDetRegW > hvDetRegH)
                        //{
                        //    hvDepthProjection = new HTuple(hvVertProjection);
                        //}
                        //else
                        //{
                        //    hvDepthProjection = new HTuple(hvHorProjection);
                        //    if (hvPhi > 0)
                        //    {
                        //        HOperatorSet.TupleInverse(hvDepthProjection, out hvDepthProjection);
                        //    }
                        //}
                        hvDepthProjection = new HTuple(hvVertProjection);

                        if (hvGrooveStart < 0)
                        {
                            hvGrooveStart = 0;
                        }

                        if (hvGrooveEnd > hvDepthProjection.Length)
                        {
                            hvGrooveEnd = hvDepthProjection.Length - 1;
                        }

                        HTuple hvFlatLeftStart;
                        if ((hvGrooveStart - 10) >= 0)
                        {
                            hvFlatLeftStart = hvGrooveStart - 10;
                        }
                        else
                        {
                            hvFlatLeftStart = 0;
                        }

                        HTuple hvFlatRightEnd;
                        if ((hvGrooveEnd + 10) < hvDepthProjection.Length)
                        {
                            hvFlatRightEnd = hvGrooveEnd + 10;
                        }
                        else
                        {
                            hvFlatRightEnd = hvDepthProjection.Length - 1;
                        }

                        HTuple hvTmpFlatPointLeftX, hvTmpFlatPointLeftY;
                        HTuple hvTmpGroovePointX, hvTmpGroovePointY;
                        HTuple hvTmpFlatPointRightX, hvTmpFlatPointRightY;
                        HOperatorSet.TupleGenSequence(hvFlatLeftStart, hvGrooveStart, 1, out hvTmpFlatPointLeftX);
                        HOperatorSet.TupleSelectRange(hvDepthProjection, hvFlatLeftStart, hvGrooveStart, out hvTmpFlatPointLeftY);
                        HOperatorSet.TupleGenSequence(hvGrooveStart, hvGrooveEnd, 1, out hvTmpGroovePointX);
                        HOperatorSet.TupleSelectRange(hvDepthProjection, hvGrooveStart, hvGrooveEnd, out hvTmpGroovePointY);
                        HOperatorSet.TupleGenSequence(hvGrooveEnd, hvFlatRightEnd, 1, out hvTmpFlatPointRightX);
                        HOperatorSet.TupleSelectRange(hvDepthProjection, hvGrooveEnd, hvFlatRightEnd, out hvTmpFlatPointRightY);

                        HTuple hvTmpFlatPointRows = new HTuple();
                        HTuple hvTmpFlatPointCols = new HTuple();
                        hvTmpFlatPointRows = hvTmpFlatPointRows.TupleConcat(hvTmpFlatPointLeftY, hvTmpFlatPointRightY);
                        hvTmpFlatPointCols = hvTmpFlatPointCols.TupleConcat(hvTmpFlatPointLeftX, hvTmpFlatPointRightX);

                        //HObject hoGrooveContour;
                        HTuple hvFitLineRowBegin, hvFitLineColBegin, hvFitLineRowEnd, hvFitLineColEnd;
                        HTuple hvNr1, hvNc1, hvDist1;
                        HTuple hvTmpGrooveDepthList, TmpPoleX;
                        HTuple hvTmpGrooveDepth;
                        //HTuple hvDistanceMin;
                        HOperatorSet.GenContourPolygonXld(out hoFlatContour, hvTmpFlatPointRows, hvTmpFlatPointCols);
                        HOperatorSet.FitLineContourXld(hoFlatContour, "tukey", -1, 0, 5, 2, out hvFitLineRowBegin, out hvFitLineColBegin,
                                                       out hvFitLineRowEnd, out hvFitLineColEnd, out hvNr1, out hvNc1, out hvDist1);
                        HOperatorSet.GenContourPolygonXld(out hoFitLineContour, hvFitLineRowBegin.TupleConcat(hvFitLineRowEnd),
                                                          hvFitLineColBegin.TupleConcat(hvFitLineColEnd));

                        //HOperatorSet.GenContourPolygonXld(out hoGrooveContour, hvTmpGroovePointY, hvTmpGroovePointX);
                        //HOperatorSet.DistanceLc(hoGrooveContour, hvFitLineRowBegin, hvFitLineColBegin, hvFitLineRowEnd, hvFitLineColEnd,
                        //                        out hvDistanceMin, out hvTmpGrooveDepth);
                        HOperatorSet.DistancePl(hvTmpGroovePointY, hvTmpGroovePointX, hvFitLineRowBegin, hvFitLineColBegin,
                                                hvFitLineRowEnd, hvFitLineColEnd, out hvTmpGrooveDepthList);
                        HOperatorSet.TupleMax(hvTmpGrooveDepthList, out hvTmpGrooveDepth);
                        HOperatorSet.TupleFindFirst(hvTmpGrooveDepthList, hvTmpGrooveDepth, out TmpPoleX);


                        hvPoleXList = hvPoleXList.TupleConcat(TmpPoleX + hvGrooveStart - hvDetRegW);
                        hvEtchingLineDepthList = hvEtchingLineDepthList.TupleConcat(hvTmpGrooveDepth * _measureParam.IntervalZ);

                        //hoGrooveContour.Dispose();

                    }

                    if (hvPoleXList.Length > 0)
                    {
                        hvPoleXOffset = hvPoleXList.TupleMean();
                    }
                    if (hvEtchingLineDepthList.Length > 0)
                    {
                        //hvEtchingLineDepth = hvEtchingLineDepthList.TupleMean();
                        //hvEtchingLineDepth = hvEtchingLineDepthList.TupleMedian();

                        // 对深度值排序后取中间区间均值，降低两端异常值影响
                        HOperatorSet.TupleSort(hvEtchingLineDepthList, out HTuple hvEtchingLineDepthSorted);
                        HTuple hvDepthCount = hvEtchingLineDepthSorted.TupleLength();
                        HTuple hvTrimCount = (hvDepthCount / 4).TupleInt();
                        HTuple hvMiddleStart = hvTrimCount;
                        HTuple hvMiddleEnd = hvDepthCount - hvTrimCount - 1;
                        HOperatorSet.TupleSelectRange(hvEtchingLineDepthSorted, hvMiddleStart, hvMiddleEnd, out HTuple hvEtchingLineDepthMiddle);
                        HOperatorSet.TupleMean(hvEtchingLineDepthMiddle, out hvEtchingLineDepth);
                    }
                }
                finally
                {
                    hoTmp?.Dispose();

                    hoMeasureDepthRegion?.Dispose();
                    hoHeightImageReduced?.Dispose();
                    hoHeightImagePart?.Dispose();
                    hoMeasureDepthRegionEro?.Dispose();
                    hoTmpMask?.Dispose();
                    hoFitSurface?.Dispose();
                    hoImageSub?.Dispose();
                    hoFlatContour?.Dispose();
                    hoFitLineContour?.Dispose();
                    ZeroRegion?.Dispose();

                    hvHorProjection?.Dispose();
                    hvVertProjection?.Dispose();
                }
            }

            return 0;
        }


        /// <summary>
        /// 由点和角度生成线段
        /// </summary>
        private int GenLine(OpenCvSharp.Point2d center, double phi, double length, out Line line)
        {
            line = new Line(new OpenCvSharp.Point2d(0, 0), new OpenCvSharp.Point2d(0, 0));

            OpenCvSharp.Point2d start, end;

            double offsetX = 0.5 * length * Math.Sin(phi);
            double offsetY = 0.5 * length * Math.Cos(phi);

            start.X = center.X - offsetX;
            start.Y = center.Y - offsetY;

            end.X = center.X + offsetX;
            end.Y = center.Y + offsetY;

            line = new Line(start, end);

            return 0;
        }


        private static double[] GaussianSmooth1D(double[] src, double sigmaSamples)
        {
            if (sigmaSamples <= 0.01) return (double[])src.Clone();

            int radius = Math.Max(1, (int)Math.Ceiling(3.0 * sigmaSamples));
            int size = radius * 2 + 1;
            double[] k = new double[size];
            double sum = 0.0;

            for (int i = -radius; i <= radius; i++)
            {
                double v = Math.Exp(-(i * i) / (2.0 * sigmaSamples * sigmaSamples));
                k[i + radius] = v;
                sum += v;
            }
            for (int i = 0; i < size; i++) k[i] /= sum;

            double[] dst = new double[src.Length];
            for (int i = 0; i < src.Length; i++)
            {
                double acc = 0.0;
                for (int j = -radius; j <= radius; j++)
                {
                    int idx = i + j;
                    if (idx < 0) idx = 0;
                    if (idx >= src.Length) idx = src.Length - 1;
                    acc += src[idx] * k[j + radius];
                }
                dst[i] = acc;
            }
            return dst;
        }

        private static void ComputeD1D2(double[] x, double[] y, out double[] d1, out double[] d2)
        {
            int n = x.Length;
            d1 = new double[n];
            d2 = new double[n];

            d1[0] = (y[1] - y[0]) / (x[1] - x[0]);
            d1[n - 1] = (y[n - 1] - y[n - 2]) / (x[n - 1] - x[n - 2]);

            for (int i = 1; i < n - 1; i++)
            {
                double dxL = x[i] - x[i - 1];
                double dxR = x[i + 1] - x[i];

                double sL = (y[i] - y[i - 1]) / dxL;
                double sR = (y[i + 1] - y[i]) / dxR;

                d1[i] = (y[i + 1] - y[i - 1]) / (x[i + 1] - x[i - 1]);
                d2[i] = 2.0 * (sR - sL) / (x[i + 1] - x[i - 1]);
            }

            d2[0] = d2[1];
            d2[n - 1] = d2[n - 2];
        }


        public class D2Extremum
        {
            public int Index;      // 样本索引
            public double X;       // 曲线X
            public double Y;       // 平滑后曲线Y
            public double D1;      // 一阶导
            public double D2;      // 二阶导
            public bool IsMax;     // true=二阶导局部极大, false=局部极小
        }


        public class MeasurePosLikeEdge
        {
            public double X;
            public double Y;
            public double Amplitude;      // |dY/dX|
            public string Transition;     // "positive" / "negative"
        }


        public static List<D2Extremum> FindSecondDerivativeExtrema(double[] x, double[] y, double sigmaSamples, double d2Threshold, 
                                                                   int minDistanceSamples = 1)
        {
            if (x == null || y == null || x.Length != y.Length || x.Length < 3)
                throw new ArgumentException("x/y长度不合法");

            double[] ys = GaussianSmooth1D(y, sigmaSamples);
            ComputeD1D2(x, ys, out var d1, out var d2);

            var candidates = new List<D2Extremum>();
            for (int i = 1; i < x.Length - 1; i++)
            {
                bool isMax = d2[i] > d2[i - 1] && d2[i] >= d2[i + 1];
                bool isMin = d2[i] < d2[i - 1] && d2[i] <= d2[i + 1];
                if (!isMax && !isMin) continue;
                if (Math.Abs(d2[i]) < d2Threshold) continue;

                candidates.Add(new D2Extremum
                {
                    Index = i,
                    X = x[i],
                    Y = ys[i],
                    D1 = d1[i],
                    D2 = d2[i],
                    IsMax = isMax
                });
            }

            // 非极大值抑制（按|D2|保留强峰）
            candidates.Sort((a, b) => Math.Abs(b.D2).CompareTo(Math.Abs(a.D2)));
            var keep = new List<D2Extremum>();
            foreach (var c in candidates)
            {
                bool tooClose = false;
                for (int i = 0; i < keep.Count; i++)
                {
                    if (Math.Abs(c.Index - keep[i].Index) < minDistanceSamples)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (!tooClose) keep.Add(c);
            }

            keep.Sort((a, b) => a.Index.CompareTo(b.Index));
            return keep;
        }


        public static List<MeasurePosLikeEdge> FindEdgesLikeMeasurePosByD2(double[] x, double[] y, double sigmaSamples, double d1Threshold, 
                                                                           double d2Threshold, int minDistanceSamples = 1)
        {
            var ex = FindSecondDerivativeExtrema(x, y, sigmaSamples, d2Threshold, minDistanceSamples);

            var edges = new List<MeasurePosLikeEdge>();
            for (int i = 0; i < ex.Count; i++)
            {
                // 额外使用一阶导幅值进行门限约束，抑制噪声极值点
                if (Math.Abs(ex[i].D1) < d1Threshold)
                {
                    continue;
                }

                edges.Add(new MeasurePosLikeEdge
                {
                    X = ex[i].X,
                    Y = ex[i].Y,
                    Amplitude = Math.Abs(ex[i].D1),
                    Transition = ex[i].D1 >= 0 ? "positive" : "negative"
                });
            }

            return edges;
        }


        /// <summary>
        /// 分区测量过程
        /// </summary>
        private int PartitionProcess(HTuple hvRegionCenterRow, HTuple hvRegionCenterCol, HTuple hvPhi, HTuple hvRegionWidth, HTuple hvRegionHeight,
                                     out KCJC0_PartitionResult partitionResult)
        {
            using (var dh = new HDevDisposeHelper())
            {
                HObject? hoTmp = null;

                HObject? hoMRegion = null;
                HObject? hoHeightImageReduce = null;
                HObject? hoHeightImagePart = null;
                HObject? hoMRegionEro = null;
                HObject? hoTmpMask = null;
                HObject? hoFitSurface = null;
                HObject? hoImageSub = null;
                HObject? ZeroRegion = null;

                HObject? hoRefLine = null;

                try
                {
                    partitionResult = new KCJC0_PartitionResult();

                    HTuple hvTmpEtchingLineEdgeNegRows = new HTuple();
                    HTuple hvTmpEtchingLineEdgeNegCols = new HTuple();
                    HTuple hvTmpEtchingLineEdgePosRows = new HTuple();
                    HTuple hvTmpEtchingLineEdgePosCols = new HTuple();

                    HTuple hvTmpEtchingPointRows = new HTuple();
                    HTuple hvTmpEtchingPointCols = new HTuple();

                    HTuple hvTmpEtchingLineWidthList = new HTuple();
                    HTuple hvTmpEtchingPointDistList = new HTuple();
                    HTuple hvTmpEtchingLineWidthMean = new HTuple();
                    HTuple hvTmpEtchingPointDistMean = new HTuple();

                    HTuple hvTmpEtchingLineWidthRealList = new HTuple();
                    HTuple hvTmpEtchingPointDistRealList = new HTuple();
                    HTuple hvTmpEtchingLineWidthRealMean = new HTuple();
                    HTuple hvTmpEtchingPointDistRealMean = new HTuple();

                    HTuple hvTmpEtchingLineDepths = new HTuple();
                    HTuple hvTmpEtchingLineDepthMean = new HTuple(-1);

                    HTuple hvEtchingLinePosProj = new HTuple();
                    HTuple hvEtchingLineNegProj = new HTuple();
                    HTuple hvEtchingPointProj = new HTuple();


                    // 计算分区内刻蚀线到极片顶部、底部的间距
                    // 顶部
                    if (_measureParam.PlatePart == -1)
                    {
                        MeasureLineIntersectionDistance(_hoFitEtchingRegionTopEdge, _hoFitTopEdge, hvRegionCenterCol.D, hvRegionCenterRow.D,
                                                        "x", out HTuple hvTmpDist);

                        partitionResult.EtchingRegionTopGap = hvTmpDist.D;
                        partitionResult.EtchingRegionTopGapReal = partitionResult.EtchingRegionTopGap * _measureParam.IntervalX;

                    }
                    else if (_measureParam.PlatePart == 1)
                    {
                        MeasureLineIntersectionDistance(_hoFitBottomEdge, _hoFitEtchingRegionBottomEdge, hvRegionCenterCol.D, hvRegionCenterRow.D,
                                                        "x", out HTuple hvTmpDist);

                        partitionResult.EtchingRegionBottomGap = hvTmpDist.D;
                        partitionResult.EtchingRegionBottomGapReal = partitionResult.EtchingRegionBottomGap * _measureParam.IntervalX;
                    }

                    // 测量区域的轮廓
                    partitionResult.MeasureRegion = new RotatedRect(new OpenCvSharp.Point2d(hvRegionCenterCol.D, hvRegionCenterRow.D), hvPhi.D,
                                                                    hvRegionWidth.D, hvRegionHeight.D);

                    HTuple hvOriRegionHeight = hvRegionHeight;
                    if (hvRegionHeight > 250)
                    {
                        hvRegionHeight = 250;
                    }

                    //临时代码
                    //HOperatorSet.GenRectangle2(out HObject R000, hvRegionCenterRow, hvRegionCenterCol, hvPhi, hvRegionWidth, hvOriRegionHeight);
                    //HOperatorSet.ReduceDomain(_hoHeightImage, R000, out HObject HeightImageR000);
                    //HOperatorSet.CropDomain(HeightImageR000, out HObject HeightImageR000Part);
                    //HOperatorSet.WriteImage(HeightImageR000Part, "tiff", 8888880, "C:/Users/REECHI05/Desktop/22/CSharp.tiff");


                    HTuple hvHorProjection, hvVertProjection, hvHeightCurve;
                    HOperatorSet.GenRectangle2(out hoMRegion, hvRegionCenterRow, hvRegionCenterCol, hvPhi, hvRegionWidth, hvRegionHeight);
                    HOperatorSet.Intersection(hoMRegion, _hoValidRegion, out hoTmp);
                    ReplaceHobject(ref hoMRegion, ref hoTmp);
                    HOperatorSet.ReduceDomain(_hoHeightImage, hoMRegion, out hoHeightImageReduce);

                    // crop进小图来处理
                    HOperatorSet.CropDomain(hoHeightImageReduce, out hoHeightImagePart);
                    HOperatorSet.GetDomain(hoHeightImagePart, out hoTmp);
                    ReplaceHobject(ref hoMRegion, ref hoTmp);
                    double phi = Math.Atan2(Math.Sin(hvPhi.D), Math.Cos(hvPhi.D)); // -> [-pi, pi]
                    if (phi > Math.PI / 2.0)
                        phi -= Math.PI;
                    if (phi < -Math.PI / 2.0)
                        phi += Math.PI;
                    HTuple hvPhiNorm = phi;
                    HOperatorSet.AreaCenter(hoMRegion, out HTuple _, out HTuple hvCenterRowPart, out HTuple hvCenterColPart);
                    HOperatorSet.HomMat2dIdentity(out HTuple hvHom);
                    HOperatorSet.HomMat2dRotate(hvHom, -hvPhiNorm, hvCenterRowPart, hvCenterColPart, out hvHom);
                    HOperatorSet.HomMat2dTranslate(hvHom, 0.5, 0.5, out HTuple hvHomTmp);
                    HOperatorSet.HomMat2dTranslateLocal(hvHomTmp, -0.5, -0.5, out HTuple hvHomAdapted);
                    HOperatorSet.AffineTransImage(hoHeightImagePart, out hoTmp, hvHomAdapted, "nearest_neighbor", "true");
                    ReplaceHobject(ref hoHeightImagePart, ref hoTmp);
                    HOperatorSet.AffineTransRegion(hoMRegion, out hoTmp, hvHomAdapted, "nearest_neighbor");
                    ReplaceHobject(ref hoMRegion, ref hoTmp);
                    HOperatorSet.Threshold(hoHeightImagePart, out ZeroRegion, 0, 0);
                    HOperatorSet.Difference(hoMRegion, ZeroRegion, out hoTmp);
                    ReplaceHobject(ref hoMRegion, ref hoTmp);
                    HOperatorSet.ReduceDomain(hoHeightImagePart, hoMRegion, out hoTmp);
                    ReplaceHobject(ref hoHeightImageReduce, ref hoTmp);

                    HOperatorSet.ErosionCircle(hoMRegion, out hoMRegionEro, 3);
                    HOperatorSet.RegionFeatures(hoMRegionEro, "area", out HTuple hvTmpArea);
                    HOperatorSet.GetImageSize(hoHeightImageReduce, out HTuple hvSampleWidth, out HTuple hvSampleHeight);

                    if (hvTmpArea.D > 100)
                    {
                        HOperatorSet.FitSurfaceFirstOrder(hoMRegionEro, hoHeightImageReduce, "tukey", 5, 1, out HTuple hv_Alpha, out HTuple hv_Beta, out HTuple hv_Gamma);
                        HOperatorSet.AreaCenter(hoMRegionEro, out HTuple hvTmpArea0, out HTuple hvTmpCenterR, out HTuple hvTmpCenterC);
                        HOperatorSet.GenImageSurfaceFirstOrder(out hoFitSurface, "real", hv_Alpha, hv_Beta, hv_Gamma, hvTmpCenterR, hvTmpCenterC, hvSampleWidth, hvSampleHeight);
                        HOperatorSet.SubImage(hoHeightImageReduce, hoFitSurface, out hoImageSub, 1, 0);
                        HOperatorSet.Threshold(hoImageSub, out hoTmpMask, -700, 700);

                        hoFitSurface.Dispose(); hoImageSub.Dispose(); ZeroRegion.Dispose();
                    }
                    else
                    {
                        HOperatorSet.GenRectangle1(out hoTmpMask, 0, 0, hvSampleHeight - 1, hvSampleWidth - 1);
                    }
                    HOperatorSet.Intersection(hoMRegion, hoTmpMask, out hoTmp);
                    ReplaceHobject(ref hoMRegion, ref hoTmp);
                    HOperatorSet.ReduceDomain(hoHeightImageReduce, hoMRegion, out hoTmp);
                    ReplaceHobject(ref hoHeightImageReduce, ref hoTmp);
                    HOperatorSet.GrayProjections(hoMRegion, hoHeightImageReduce, "simple", out hvHorProjection, out hvVertProjection);

                    hoMRegionEro.Dispose(); hoTmpMask.Dispose();

                    //if (hvRegionWidth < hvRegionHeight)
                    //{
                    //    hvHeightCurve = hvHorProjection;
                    //    if (hvPhi > 0)
                    //    {
                    //        HOperatorSet.TupleInverse(hvHeightCurve, out hvHeightCurve);
                    //    }
                    //}
                    //else
                    //{
                    //    hvHeightCurve = hvVertProjection;
                    //}
                    hvHeightCurve = new HTuple(hvVertProjection);

                    // 测量区域的表面高度曲线
                    // 测量区域的表面高度曲线的X坐标
                    partitionResult.HeightCurveX = CreateDoubleArrayWithStep(0, _measureParam.IntervalX, hvHeightCurve.Length);
                    // 测量区域的表面高度曲线的Y坐标(加补偿映射到正值)
                    HTuple hvTmpMax = hvHeightCurve.TupleMax();
                    HTuple hvTmpMin = hvHeightCurve.TupleMin();
                    if (hvTmpMin.D < 0)
                        hvHeightCurve = hvHeightCurve - hvTmpMin;
                    partitionResult.HeightCurveY = hvHeightCurve.DArr;

                    /********************/
                    //double[] ys = GaussianSmooth1D(partitionResult.HeightCurveY, 5);
                    //ComputeD1D2(partitionResult.HeightCurveX, ys, out var d1, out var d2);
                    //var edges = FindEdgesLikeMeasurePosByD2(
                    //                                        partitionResult.HeightCurveX,
                    //                                        partitionResult.HeightCurveY,
                    //                                        sigmaSamples: 5,
                    //                                        d1Threshold: 30,
                    //                                        d2Threshold: 30,
                    //                                        minDistanceSamples: 3);
                    //partitionResult.HeightCurveY = d2;
                    /********************/

                    HTuple hvHomMat2DIdentity, hvTmpHomMat2D, hvTmpHomMat2DInvert;
                    HOperatorSet.HomMat2dIdentity(out hvHomMat2DIdentity);
                    HOperatorSet.HomMat2dRotate(hvHomMat2DIdentity, -hvPhi, hvRegionCenterRow, hvRegionCenterCol, out hvTmpHomMat2D);
                    HOperatorSet.HomMat2dTranslate(hvTmpHomMat2D, -hvRegionCenterRow + hvRegionHeight, -hvRegionCenterCol + hvRegionWidth, out hvTmpHomMat2D);
                    HOperatorSet.HomMat2dInvert(hvTmpHomMat2D, out hvTmpHomMat2DInvert);

                    // 原图到分区的变换矩阵
                    partitionResult.HomMat2D = new double[] { hvTmpHomMat2D[0], hvTmpHomMat2D[1], hvTmpHomMat2D[2],
                                                          hvTmpHomMat2D[3], hvTmpHomMat2D[4], hvTmpHomMat2D[5] };
                    // 分区到原图的变换矩阵
                    partitionResult.HomMat2DInvert = new double[] { hvTmpHomMat2DInvert[0], hvTmpHomMat2DInvert[1], hvTmpHomMat2DInvert[2],
                                                                hvTmpHomMat2DInvert[3], hvTmpHomMat2DInvert[4], hvTmpHomMat2DInvert[5] };

                    HTuple hvTmpMeasureHandle;
                    HOperatorSet.GenMeasureRectangle2(hvRegionCenterRow, hvRegionCenterCol, hvPhi, hvRegionWidth, hvRegionHeight,
                                                      _measureParam.ScanWidth, _measureParam.ScanHeight, "bilinear", out hvTmpMeasureHandle);

                    HTuple hvTmpEdgeRowNeg, hvTmpEdgeColNeg, hvAmplitudeNeg, hvDistanceNeg;
                    HTuple hvTmpEdgeRowPos, hvTmpEdgeColPos, hvAmplitudePos, hvDistancePos;

                    HOperatorSet.MeasurePos(_hoGrayImage, hvTmpMeasureHandle, _measureParam.GrayAmplitudeSigma, _measureParam.GrayAmplitudeThr,
                                            "negative", "all", out hvTmpEdgeRowNeg, out hvTmpEdgeColNeg, out hvAmplitudeNeg, out hvDistanceNeg);
                    HOperatorSet.MeasurePos(_hoGrayImage, hvTmpMeasureHandle, _measureParam.GrayAmplitudeSigma, _measureParam.GrayAmplitudeThr,
                                            "positive", "all", out hvTmpEdgeRowPos, out hvTmpEdgeColPos, out hvAmplitudePos, out hvDistancePos);

                    HOperatorSet.CloseMeasure(hvTmpMeasureHandle);

                    HOperatorSet.CountObj(_hoRefLines, out HTuple hvRefLinesNum);

                    HTuple hvItemNumMax = (new HTuple(hvTmpEdgeRowNeg.TupleLength())).TupleMax2(new HTuple(hvTmpEdgeRowPos.TupleLength()));
                    int negIdx = 0;
                    int posIdx = 0;
                    if (hvTmpEdgeRowNeg.Length > 0 && hvTmpEdgeRowPos.Length > 0)
                    {
                        for (int edgeIdx = 0; edgeIdx < hvItemNumMax; edgeIdx++)
                        {
                            HTuple hvSelectRowPos = hvTmpEdgeRowPos.TupleSelect(posIdx);
                            HTuple hvSelectColPos = hvTmpEdgeColPos.TupleSelect(posIdx);
                            HTuple hvSelectRowNeg = hvTmpEdgeRowNeg.TupleSelect(negIdx);
                            HTuple hvSelectColNeg = hvTmpEdgeColNeg.TupleSelect(negIdx);

                            if (hvSelectColNeg < hvSelectColPos)
                            {
                                HTuple hvTmpWidthRoughly, hvTmpWidth;
                                HOperatorSet.DistancePp(hvSelectRowPos, hvSelectColPos, hvSelectRowNeg, hvSelectColNeg, out hvTmpWidthRoughly);

                                HTuple hvCenterRow = (hvSelectRowPos + hvSelectRowNeg) * 0.5;
                                HTuple hvCenterCol = (hvSelectColPos + hvSelectColNeg) * 0.5;

                                bool IsValidEtchingLine = false;
                                if (_measureParam.EtchingLineMeasureMaskRate > 0 && _measureParam.EtchingLineMeasureMaskRate < 1)
                                {
                                    for (int refLineIdx = 1; refLineIdx <= hvRefLinesNum.D; refLineIdx++)
                                    {
                                        HOperatorSet.SelectObj(_hoRefLines, out hoRefLine, refLineIdx);

                                        HTuple hv__;
                                        HOperatorSet.FitLineContourXld(hoRefLine, "tukey", -1, 0, 5, 2, out HTuple hvRefLineRowBegin, out HTuple hvRefLineColBegin,
                                                                       out HTuple hvRefLineRowEnd, out HTuple hvRefLineColEnd, out hv__, out hv__, out hv__);
                                        HOperatorSet.DistancePl(hvCenterRow, hvCenterCol, hvRefLineRowBegin, hvRefLineColBegin, hvRefLineRowEnd, hvRefLineColEnd, out HTuple hvDistance00);

                                        if ((int)(new HTuple(hvDistance00.TupleLess(hvTmpWidthRoughly * 2))) != 0)
                                        {
                                            IsValidEtchingLine = true;
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    IsValidEtchingLine = true;
                                }

                                if (IsValidEtchingLine)
                                {
                                    hvTmpEtchingLineEdgeNegRows = hvTmpEtchingLineEdgeNegRows.TupleConcat(hvSelectRowPos);
                                    hvTmpEtchingLineEdgeNegCols = hvTmpEtchingLineEdgeNegCols.TupleConcat(hvSelectColPos);
                                    hvTmpEtchingLineEdgePosRows = hvTmpEtchingLineEdgePosRows.TupleConcat(hvSelectRowNeg);
                                    hvTmpEtchingLineEdgePosCols = hvTmpEtchingLineEdgePosCols.TupleConcat(hvSelectColNeg);

                                    hvTmpEtchingPointRows = hvTmpEtchingPointRows.TupleConcat(hvCenterRow);
                                    hvTmpEtchingPointCols = hvTmpEtchingPointCols.TupleConcat(hvCenterCol);

                                    HTuple hvDepthDetRegW = (hvTmpWidthRoughly * 0.75) + (hvTmpWidthRoughly * 0.75);
                                    //HTuple hvDepthDetRegH = new HTuple(hvRegionHeight);
                                    HTuple hvDepthDetRegH = new HTuple(hvOriRegionHeight);

                                    HTuple hvPoleXOffset;
                                    HTuple hvEtchingLineDepth;

                                    //GetEtchingWidth(_hoGrayImage, hvCenterRow, hvCenterCol, hvPhi, hvDepthDetRegW, hvDepthDetRegH, hvTmpWidthRoughly, out hvTmpWidth);
                                    //if (hvTmpWidth.Length == 0)
                                    //    continue;
                                    hvTmpEtchingLineWidthList = hvTmpEtchingLineWidthList.TupleConcat(hvTmpWidthRoughly);
                                    hvTmpEtchingLineWidthRealList = hvTmpEtchingLineWidthRealList.TupleConcat(hvTmpWidthRoughly * _measureParam.IntervalY);

                                    GetEtchingDepth(_hoHeightImage, hvCenterRow, hvCenterCol, hvPhi, hvDepthDetRegW, hvDepthDetRegH, hvTmpWidthRoughly, out hvEtchingLineDepth, out hvPoleXOffset);

                                    hvTmpEtchingLineDepths = hvTmpEtchingLineDepths.TupleConcat(hvEtchingLineDepth);

                                    Line etchingLinePos, etchingLineNeg;
                                    GenLine(new OpenCvSharp.Point2d(hvSelectColPos.D, hvSelectRowPos.D), hvPhi, hvOriRegionHeight * 0.5, out etchingLinePos);
                                    GenLine(new OpenCvSharp.Point2d(hvSelectColNeg.D, hvSelectRowNeg.D), hvPhi, hvOriRegionHeight * 0.5, out etchingLineNeg);

                                    HTuple hvTmpRow, hvTmpCol;

                                    // 刻蚀线正边缘线
                                    partitionResult.EtchingLinePos.Add(etchingLinePos);

                                    // 刻蚀线负边缘线
                                    partitionResult.EtchingLineNeg.Add(etchingLineNeg);

                                    // 刻蚀线正边缘点坐标
                                    partitionResult.EtchingLinePosPoint.Add(new OpenCvSharp.Point2d(hvSelectColPos.D, hvSelectRowPos.D));
                                    HOperatorSet.AffineTransPixel(hvTmpHomMat2D, hvSelectRowPos, hvSelectColPos, out hvTmpRow, out hvTmpCol);
                                    hvEtchingLinePosProj = hvEtchingLinePosProj.TupleConcat(hvTmpCol);

                                    // 刻蚀线负边缘点坐标
                                    partitionResult.EtchingLineNegPoint.Add(new OpenCvSharp.Point2d(hvSelectColNeg.D, hvSelectRowNeg.D));
                                    HOperatorSet.AffineTransPixel(hvTmpHomMat2D, hvSelectRowNeg, hvSelectColNeg, out hvTmpRow, out hvTmpCol);
                                    hvEtchingLineNegProj = hvEtchingLineNegProj.TupleConcat(hvTmpCol);

                                    // 刻蚀线中心(刻蚀点)
                                    partitionResult.EtchingPoint.Add(new OpenCvSharp.Point2d(hvCenterCol.D, hvCenterRow.D));
                                    HOperatorSet.AffineTransPixel(hvTmpHomMat2D, hvCenterRow, hvCenterCol, out hvTmpRow, out hvTmpCol);
                                    hvEtchingPointProj = hvEtchingPointProj.TupleConcat(hvTmpCol + hvPoleXOffset);
                                    //hvEtchingPointProj = hvEtchingPointProj.TupleConcat(hvTmpCol);
                                }

                                negIdx++;
                                posIdx++;
                            }
                            else
                            {
                                posIdx++;
                            }

                            if (negIdx > (hvTmpEdgeRowNeg.Length - 1) || posIdx > (hvTmpEdgeRowPos.Length - 1))
                            {
                                break;
                            }
                        }

                        if(hvTmpEtchingLineWidthList.Length > 0)
                            hvTmpEtchingLineWidthMean = hvTmpEtchingLineWidthList.TupleMean();
                        if(hvTmpEtchingLineWidthRealList.Length > 0)
                            hvTmpEtchingLineWidthRealMean = hvTmpEtchingLineWidthRealList.TupleMean();
                        if(hvTmpEtchingLineDepths.Length > 0)
                            hvTmpEtchingLineDepthMean = hvTmpEtchingLineDepths.TupleMean();
                    }

                    HTuple hvTmpEtchingPointNum = new HTuple(hvTmpEtchingPointRows.TupleLength());

                    if (hvTmpEtchingPointNum > 1)
                    {
                        for (int pointIdx = 0; pointIdx < hvTmpEtchingPointNum - 1; pointIdx++)
                        {
                            // 计算刻蚀点间距
                            HTuple hvTmpWidth;
                            HOperatorSet.DistancePp(hvTmpEtchingPointRows.TupleSelect(pointIdx), hvTmpEtchingPointCols.TupleSelect(pointIdx),
                                                    hvTmpEtchingPointRows.TupleSelect(pointIdx + 1), hvTmpEtchingPointCols.TupleSelect(pointIdx + 1), out hvTmpWidth);

                            hvTmpEtchingPointDistList = hvTmpEtchingPointDistList.TupleConcat(hvTmpWidth);
                            hvTmpEtchingPointDistRealList = hvTmpEtchingPointDistRealList.TupleConcat(hvTmpWidth * _measureParam.IntervalY);

                        }
                    }
                    else
                    {
                        if (hvDistanceNeg.Length > 0)
                        {
                            hvTmpEtchingPointDistList = hvTmpEtchingPointDistList.TupleConcat(hvDistanceNeg);
                            hvTmpEtchingPointDistRealList = hvTmpEtchingPointDistRealList.TupleConcat(hvDistanceNeg * _measureParam.IntervalY);
                        }
                        else if (hvDistancePos.Length > 0)
                        {
                            hvTmpEtchingPointDistList = hvTmpEtchingPointDistList.TupleConcat(hvDistancePos);
                            hvTmpEtchingPointDistRealList = hvTmpEtchingPointDistRealList.TupleConcat(hvDistancePos * _measureParam.IntervalY);
                        }
                    }

                    if (hvTmpEtchingPointDistList.Length > 0)
                    {
                        hvTmpEtchingPointDistMean = hvTmpEtchingPointDistList.TupleMean();
                        hvTmpEtchingPointDistRealMean = hvTmpEtchingPointDistRealList.TupleMean();

                    }
                    else
                    {
                        hvTmpEtchingPointDistMean = -1;
                        hvTmpEtchingPointDistRealMean = -1;

                    }



                    // 刻蚀线宽度
                    if (hvTmpEtchingLineWidthList.Length > 0)
                    {
                        partitionResult.EtchingLineWidthList = hvTmpEtchingLineWidthList.DArr;
                        partitionResult.EtchingLineWidthMean = hvTmpEtchingLineWidthMean;

                        partitionResult.EtchingLineNum = hvTmpEtchingLineWidthList.Length;
                    }
                    else
                    {
                        partitionResult.EtchingLineWidthList = new double[] { };
                        partitionResult.EtchingLineWidthMean = -1;

                        partitionResult.EtchingLineNum = 0;
                    }

                    // 刻蚀点间距
                    if (hvTmpEtchingPointDistList.Length > 0)
                    {
                        partitionResult.EtchingPointDistList = hvTmpEtchingPointDistList.DArr;
                        partitionResult.EtchingPointDistMean = hvTmpEtchingPointDistMean;
                    }
                    else
                    {
                        partitionResult.EtchingPointDistList = new double[] { };
                        partitionResult.EtchingPointDistMean = -1;
                    }


                    /*物理单位的值*/
                    // 刻蚀线实际宽度
                    if (hvTmpEtchingLineWidthRealList.Length > 0)
                    {
                        partitionResult.EtchingLineWidthRealList = hvTmpEtchingLineWidthRealList.DArr;
                        partitionResult.EtchingLineWidthRealMean = hvTmpEtchingLineWidthRealMean;
                    }
                    else
                    {
                        partitionResult.EtchingLineWidthRealList = new double[] { };
                        partitionResult.EtchingLineWidthRealMean = -1;
                    }

                    // 刻蚀点实际间距
                    if (hvTmpEtchingPointDistRealList.Length > 0)
                    {
                        partitionResult.EtchingPointDistRealList = hvTmpEtchingPointDistRealList.DArr;
                        partitionResult.EtchingPointDistRealMean = hvTmpEtchingPointDistRealMean;
                    }
                    else
                    {
                        partitionResult.EtchingPointDistRealList = new double[] { };
                        partitionResult.EtchingPointDistRealMean = -1;
                    }

                    // 刻蚀线深度
                    if (hvTmpEtchingLineDepths.Length > 0)
                    {
                        partitionResult.EtchingLineDepthList = hvTmpEtchingLineDepths.DArr;
                        partitionResult.EtchingLineDepthMean = hvTmpEtchingLineDepthMean;
                    }
                    else
                    {
                        partitionResult.EtchingLineDepthList = new double[] { };
                        partitionResult.EtchingLineDepthMean = -1;
                    }

                    // 刻蚀线负边缘点在表面高度曲线投影
                    if (hvEtchingLinePosProj.Length > 0)
                    {
                        partitionResult.EtchingLinePosProj = hvEtchingLinePosProj.DArr;
                    }
                    else
                    {
                        partitionResult.EtchingLinePosProj = new double[] { };
                    }

                    // 刻蚀线负边缘点在表面高度曲线投影
                    if (hvEtchingLineNegProj.Length > 0)
                    {
                        partitionResult.EtchingLineNegProj = hvEtchingLineNegProj.DArr;
                    }
                    else
                    {
                        partitionResult.EtchingLineNegProj = new double[] { };
                    }

                    // 刻蚀线中心(刻蚀点)在表面高度曲线投影
                    if (hvEtchingPointProj.Length > 0)
                    {
                        partitionResult.EtchingPointProj = hvEtchingPointProj.DArr;
                    }
                    else
                    {
                        partitionResult.EtchingPointProj = new double[] { };
                    }


                }
                finally
                {
                    hoTmp?.Dispose();

                    hoMRegion?.Dispose();
                    hoHeightImageReduce?.Dispose();
                    hoHeightImagePart?.Dispose();
                    hoMRegionEro?.Dispose();
                    hoTmpMask?.Dispose();
                    hoFitSurface?.Dispose();
                    hoImageSub?.Dispose();
                    ZeroRegion?.Dispose();
                }
            }

            return 0;
        }


        /// <summary>
        /// 对极片各分区测量
        /// </summary>
        private int MeasureEtchingRegions()
        {
            using (var dh = new HDevDisposeHelper())
            {
                HObject? hoTmp = null;

                HObject? hoSampleLeftEdge = null;
                HObject? hoSampleRightEdge = null;

                try
                {
                    // 计算极片左右侧边缘到去除区的偏移距离
                    HTuple hvLeftInvalidRegionEdgeRowOffset = _hvPlateLeftEdgeMaskSize / (_hvPhi.TupleCos());
                    HTuple hvRightInvalidRegionEdgeRowOffset = _hvPlateRightEdgeMaskSize / (_hvPhi.TupleCos());

                    // 计算中间分区，生成测量句柄
                    HTuple hvValidRegionStartEdgeRow;
                    if (_hvEtchingRegionLeftGap > 0)
                    {
                        hvValidRegionStartEdgeRow = (_hvLeftTopRow + _hvRightTopRow) * 0.5 + hvLeftInvalidRegionEdgeRowOffset + _hvEtchingRegionLeftGap;
                    }
                    else
                    {
                        hvValidRegionStartEdgeRow = (_hvLeftTopRow + _hvRightTopRow) * 0.5 + hvLeftInvalidRegionEdgeRowOffset;
                    }
                    HTuple hvValidRegionEndEdgeRow;
                    if (_hvEtchingRegionRightGap > 0)
                    {
                        hvValidRegionEndEdgeRow = (_hvLeftDownRow + _hvRightDownRow) * 0.5 - hvRightInvalidRegionEdgeRowOffset - _hvEtchingRegionRightGap;
                    }
                    else
                    {
                        hvValidRegionEndEdgeRow = (_hvLeftDownRow + _hvRightDownRow) * 0.5 - hvRightInvalidRegionEdgeRowOffset;
                    }


                    // 计算实际采样区域边缘
                    HOperatorSet.CountObj(_hoRefLines, out HTuple hvRefLinesNum);
                    HTuple hvSampleLeftEdgeOffsetCol = _hvStandardEtchingPointDistPixel * 0.5;
                    HTuple hvSampleRightEdgeOffsetCol = _hvStandardEtchingPointDistPixel * 0.5;

                    if ((int)(new HTuple(hvRefLinesNum.TupleGreater(0))) != 0)
                    {
                        HOperatorSet.TupleMin(_hvRefLineTopCols, out HTuple hvRefLineTopColMin);
                        HOperatorSet.TupleMax(_hvRefLineTopCols, out HTuple hvRefLineTopColMax);
                        HOperatorSet.TupleMin(_hvRefLineDownCols, out HTuple hvRefLineDownColMin);
                        HOperatorSet.TupleMax(_hvRefLineDownCols, out HTuple hvRefLineDownColMax);

                        HOperatorSet.TupleMin2(hvRefLineTopColMin, hvRefLineDownColMin, out HTuple hvRefLineColMin);
                        HOperatorSet.TupleMax2(hvRefLineTopColMax, hvRefLineDownColMax, out HTuple hvRefLineColMax);

                        if ((int)(new HTuple(((hvRefLineColMin - hvSampleLeftEdgeOffsetCol)).TupleLess(0))) != 0)
                        {
                            hvSampleLeftEdgeOffsetCol = new HTuple(hvRefLineColMin);
                        }
                        if ((int)(new HTuple(((hvRefLineColMax + hvSampleRightEdgeOffsetCol)).TupleGreater(_measureParam.ScanWidth))) != 0)
                        {
                            hvSampleRightEdgeOffsetCol = _measureParam.ScanWidth - hvRefLineColMax;
                        }

                        HOperatorSet.GenContourPolygonXld(out hoSampleLeftEdge, ((_hvRefLineTopRows.TupleSelect(0))).TupleConcat(_hvRefLineDownRows.TupleSelect(0)), 
                            (((_hvRefLineTopCols.TupleSelect(0)) - hvSampleLeftEdgeOffsetCol)).TupleConcat((_hvRefLineDownCols.TupleSelect(0)) - hvSampleLeftEdgeOffsetCol));

                        HOperatorSet.GenContourPolygonXld(out hoSampleRightEdge, ((_hvRefLineTopRows.TupleSelect(hvRefLinesNum - 1))).TupleConcat(_hvRefLineDownRows.TupleSelect(hvRefLinesNum - 1)),
                                (((_hvRefLineTopCols.TupleSelect(hvRefLinesNum - 1)) + hvSampleRightEdgeOffsetCol)).TupleConcat((_hvRefLineDownCols.TupleSelect(hvRefLinesNum - 1)) + hvSampleRightEdgeOffsetCol));
                    }
                    else
                    {
                        hoSampleLeftEdge = new HObject(_hoFitBottomEdge);
                        hoSampleRightEdge = new HObject(_hoFitTopEdge);
                    }


                    HTuple hvSamplingPointRows;
                    HOperatorSet.TupleGenSequence(hvValidRegionStartEdgeRow, hvValidRegionEndEdgeRow,
                                                  (hvValidRegionEndEdgeRow - hvValidRegionStartEdgeRow) / _measureParam.SamplePointNum,
                                                  out hvSamplingPointRows);

                    for (int idx = 0; idx < hvSamplingPointRows.TupleLength() - 1; idx++)
                    {

                        HTuple hvTmpCol = (_hvLeftTopColumn + _hvRightTopColumn) * 0.5;

                        HTuple hvRegionCenterRow, hvRegionCenterCol, hvRegionWidth, hvRegionHeight;

                        //CalculateMeasureRegionCenter(_hoFitBottomEdge, _hoFitTopEdge, hvSamplingPointRows.TupleSelect(idx),
                        //                             hvTmpCol, _hvPhi, hvSamplingPointRows.TupleSelect(idx + 1),
                        //                             hvTmpCol, out hvRegionCenterRow, out hvRegionCenterCol, out hvRegionWidth, out hvRegionHeight);
                        CalculateMeasureRegionCenter(hoSampleLeftEdge, hoSampleRightEdge, hvSamplingPointRows.TupleSelect(idx),
                                                     hvTmpCol, _hvPhi, hvSamplingPointRows.TupleSelect(idx + 1),
                                                     hvTmpCol, out hvRegionCenterRow, out hvRegionCenterCol, out hvRegionWidth, out hvRegionHeight);


                        hvRegionWidth = (hvRegionWidth * 0.5) - _measureParam.PlateEdgeMaskSize;
                        hvRegionHeight = hvRegionHeight * 0.5;

                        KCJC0_PartitionResult partitionResult;
                        PartitionProcess(hvRegionCenterRow, hvRegionCenterCol, _hvPhi, hvRegionWidth, hvRegionHeight, out partitionResult);

                        // _partitionResults.Add(partitionResult);
                        _measureResult.PartitionResults.Add(partitionResult);

                    }

                    // 计算第一个分区和最后一个分区刻蚀线起点、终点到极片扫描起始边缘的距离
                    KCJC0_PartitionResult partitionResultFirst = _measureResult.PartitionResults.First();
                    double[] etchingRegionLeftGapList = new double[partitionResultFirst.EtchingLineNum];
                    double[] etchingRegionLeftGapRealList = new double[partitionResultFirst.EtchingLineNum];
                    for (int i = 0; i < partitionResultFirst.EtchingLineNum; i++)
                    {
                        MeasureLineIntersectionDistance(_hoFitStartEdge, _hoFitEtchingRegionStartEdge,
                                                        partitionResultFirst.EtchingPoint[i].X, partitionResultFirst.EtchingPoint[i].Y,
                                                        "y", out HTuple hvTmpDist);
                        etchingRegionLeftGapList[i] = hvTmpDist;
                        etchingRegionLeftGapRealList[i] = hvTmpDist * _measureParam.IntervalY;
                    }
                    _measureResult.EtchingRegionLeftGapList = etchingRegionLeftGapList;
                    _measureResult.EtchingRegionLeftGapRealList = etchingRegionLeftGapRealList;

                    KCJC0_PartitionResult partitionResultLast = _measureResult.PartitionResults.Last();
                    double[] etchingRegionRightGapList = new double[partitionResultLast.EtchingLineNum];
                    double[] etchingRegionRightGapRealList = new double[partitionResultLast.EtchingLineNum];
                    for (int i = 0; i < partitionResultLast.EtchingLineNum; i++)
                    {
                        MeasureLineIntersectionDistance(_hoFitEtchingRegionEndEdge, _hoFitEndEdge,
                                                        partitionResultLast.EtchingPoint[i].X, partitionResultLast.EtchingPoint[i].Y,
                                                        "y", out HTuple hvTmpDist);
                        etchingRegionRightGapList[i] = hvTmpDist;
                        etchingRegionRightGapRealList[i] = hvTmpDist * _measureParam.IntervalY;
                    }
                    _measureResult.EtchingRegionRightGapList = etchingRegionRightGapList;
                    _measureResult.EtchingRegionRightGapRealList = etchingRegionRightGapRealList;
                }
                finally
                {
                    hoTmp?.Dispose();

                    hoSampleLeftEdge?.Dispose();
                    hoSampleRightEdge?.Dispose();
                }
            }

            return 0;
        }


        private double[] PaddingArray(double[] originalArray, int targetLength, double paddingValue)
        {
            double[] newArray = new double[targetLength];
            if (originalArray.Length >= targetLength)
            {
                Array.Copy(originalArray, newArray, targetLength);
                return newArray;
            }
            else
            {
                Array.Copy(originalArray, newArray, originalArray.Length);

                for (int i = originalArray.Length; i < targetLength; i++)
                {
                    newArray[i] = paddingValue;
                }

                return newArray;
            }
        }


        /// <summary>
        /// 组装测量结果
        /// </summary>
        private int PourResult(out KCJC0_MeasureResult result)
        {
            HTuple? TmpRows = null; 
            HTuple? TmpCols = null;

            using (var dh = new HDevDisposeHelper())
            {
                try
                {
                    result = new KCJC0_MeasureResult();

                    if (_measureParam.PlatePart == -1)
                    {
                        // 拟合出的极片上边缘线
                        _measureResult.FitTopEdgeLine = new Line(new OpenCvSharp.Point2d(_hvRightTopColumn, _hvRightTopRow),
                                                                 new OpenCvSharp.Point2d(_hvRightDownColumn, _hvRightDownRow));

                        HOperatorSet.GetContourXld(_hoFitEtchingRegionTopEdge, out TmpRows, out TmpCols);
                        // 拟合出的刻蚀区上边缘线
                        if (TmpRows.Length == 0)
                        {
                            _measureResult.FitEtchingRegionTopEdgeLine = new Line(new OpenCvSharp.Point2d(0, 0),
                                                                              new OpenCvSharp.Point2d(0, 0));
                        }
                        else
                        {
                            _measureResult.FitEtchingRegionTopEdgeLine = new Line(new OpenCvSharp.Point2d(TmpCols.TupleSelect(0), TmpRows.TupleSelect(0)),
                                                                              new OpenCvSharp.Point2d(TmpCols.TupleSelect(1), TmpRows.TupleSelect(1)));
                        }

                        // 刻蚀区顶部与极片顶部缘间距
                        _measureResult.GlobalEtchingRegionTopGap = _hvEtchingRegionTopGap;
                        // 刻蚀区顶部与极片顶部缘实际间距
                        if (_hvEtchingRegionTopGap.D == -1)
                        {
                            _measureResult.EtchingRegionTopGapReal = -1;
                        }
                        else
                        {
                            _measureResult.EtchingRegionTopGapReal = _hvEtchingRegionTopGap * _measureParam.IntervalY;
                        }
                    }
                    if (_measureParam.PlatePart == 1)
                    {
                        // 拟合出的极片下边缘线
                        _measureResult.FitBottomEdgeLine = new Line(new OpenCvSharp.Point2d(_hvLeftTopColumn, _hvLeftTopRow),
                                                                    new OpenCvSharp.Point2d(_hvLeftDownColumn, _hvLeftDownRow));

                        HOperatorSet.GetContourXld(_hoFitEtchingRegionBottomEdge, out TmpRows, out TmpCols);
                        // 拟合出的刻蚀区下边缘线
                        if (TmpRows.Length == 0)
                        {
                            _measureResult.FitEtchingRegionBottomEdgeLine = new Line(new OpenCvSharp.Point2d(0, 0),
                                                                                 new OpenCvSharp.Point2d(0, 0));
                        }
                        else
                        {
                            _measureResult.FitEtchingRegionBottomEdgeLine = new Line(new OpenCvSharp.Point2d(TmpCols.TupleSelect(0), TmpRows.TupleSelect(0)),
                                                                                 new OpenCvSharp.Point2d(TmpCols.TupleSelect(1), TmpRows.TupleSelect(1)));
                        }

                        // 刻蚀区底部与极片底部缘间距
                        _measureResult.GlobalEtchingRegionBottomGap = _hvEtchingRegionBottomGap;
                        // 刻蚀区底部与极片底部缘实际间距
                        if (_hvEtchingRegionBottomGap.D == -1)
                        {
                            _measureResult.EtchingRegionBottomGapReal = -1;
                        }
                        else
                        {
                            _measureResult.EtchingRegionBottomGapReal = _hvEtchingRegionBottomGap * _measureParam.IntervalY;
                        }
                    }

                    // 拟合出的极片起始扫描的边缘线
                    _measureResult.FitStartEdgeLine = new Line(new OpenCvSharp.Point2d(_hvLeftTopColumn, _hvLeftTopRow),
                                                               new OpenCvSharp.Point2d(_hvRightTopColumn, _hvRightTopRow));
                    // 拟合出的极片结束扫描的边缘线
                    _measureResult.FitEndEdgeLine = new Line(new OpenCvSharp.Point2d(_hvLeftDownColumn, _hvLeftDownRow),
                                                             new OpenCvSharp.Point2d(_hvRightDownColumn, _hvRightDownRow));

                    // 刻蚀区与极片起始扫描边缘间距
                    _measureResult.GlobalEtchingRegionLeftGap = _hvEtchingRegionLeftGap;
                    // 刻蚀区与极片起始扫描边缘实际间距
                    if (_hvEtchingRegionLeftGap.D == -1)
                    {
                        _measureResult.EtchingRegionLeftGapReal = -1;
                    }
                    else
                    {
                        _measureResult.EtchingRegionLeftGapReal = _hvEtchingRegionLeftGap * _measureParam.IntervalY;
                    }

                    // 刻蚀区与极片结束扫描边缘间距
                    _measureResult.GlobalEtchingRegionRightGap = _hvEtchingRegionRightGap;
                    // 刻蚀区与极片结束扫描边缘实际间距
                    if (_hvEtchingRegionRightGap.D == -1)
                    {
                        _measureResult.EtchingRegionRightGapReal = -1;
                    }
                    else
                    {
                        _measureResult.EtchingRegionRightGapReal = _hvEtchingRegionRightGap * _measureParam.IntervalY;
                    }

                    HOperatorSet.GetContourXld(_hoFitEtchingRegionStartEdge, out TmpRows, out TmpCols);
                    // 拟合出的刻蚀区起始扫描的边缘线
                    if (TmpRows.Length == 0)
                    {
                        _measureResult.FitEtchingRegionStartEdgeLine = new Line(new OpenCvSharp.Point2d(0, 0),
                                                                            new OpenCvSharp.Point2d(0, 0));
                    }
                    else
                    {
                        _measureResult.FitEtchingRegionStartEdgeLine = new Line(new OpenCvSharp.Point2d(TmpCols.TupleSelect(0), TmpRows.TupleSelect(0)),
                                                                            new OpenCvSharp.Point2d(TmpCols.TupleSelect(1), TmpRows.TupleSelect(1)));
                    }


                    HOperatorSet.GetContourXld(_hoFitEtchingRegionEndEdge, out TmpRows, out TmpCols);
                    // 拟合出的刻蚀区结束扫描的边缘线
                    if (TmpRows.Length == 0)
                    {
                        _measureResult.FitEtchingRegionEndEdgeLine = new Line(new OpenCvSharp.Point2d(0, 0),
                                                                          new OpenCvSharp.Point2d(0, 0));
                    }
                    else
                    {
                        _measureResult.FitEtchingRegionEndEdgeLine = new Line(new OpenCvSharp.Point2d(TmpCols.TupleSelect(0), TmpRows.TupleSelect(0)),
                                                                          new OpenCvSharp.Point2d(TmpCols.TupleSelect(1), TmpRows.TupleSelect(1)));
                    }

                    // 分区结果预处理
                    List<double> widthReal = new List<double>();
                    List<double> Depth = new List<double>();

                    // 对各分区槽宽、槽深、槽间距的数组进行padding到相同长度
                    int measurePartNum = _measureResult.PartitionResults.Count;
                    if (measurePartNum > 0)
                    {
                        //int lineNumMax = _measureResult.PartitionResults.Max(x => x.EtchingLineNum);
                        int lineNumMax = _measureParam.EtchingLineNumMax;

                        // 槽宽
                        double[] EtchingLineWidthRealMeanList = new double[lineNumMax];
                        double[] EtchingLineWidthRealMaxList = new double[lineNumMax];
                        double[] EtchingLineWidthRealMinList = new double[lineNumMax];
                        // 槽深
                        double[] EtchingLineDepthMeanList = new double[lineNumMax];
                        double[] EtchingLineDepthMaxList = new double[lineNumMax];
                        double[] EtchingLineDepthMinList = new double[lineNumMax];

                        // 槽间距
                        double[] EtchingPointDistRealMeanList = new double[lineNumMax - 1];
                        double[] EtchingPointDistRealMaxList = new double[lineNumMax - 1];
                        double[] EtchingPointDistRealMinList = new double[lineNumMax - 1];

                        for (int idx = 0; idx < measurePartNum; idx++)
                        {
                            _measureResult.PartitionResults[idx].EtchingLineWidthRealList = PaddingArray(_measureResult.PartitionResults[idx].EtchingLineWidthRealList, lineNumMax, -1);
                            _measureResult.PartitionResults[idx].EtchingPointDistRealList = PaddingArray(_measureResult.PartitionResults[idx].EtchingPointDistRealList, lineNumMax - 1, -1);
                            _measureResult.PartitionResults[idx].EtchingLineDepthList = PaddingArray(_measureResult.PartitionResults[idx].EtchingLineDepthList, lineNumMax, -1);
                            widthReal.Add(_measureResult.PartitionResults[idx].EtchingLineWidthRealMean);
                            Depth.Add(_measureResult.PartitionResults[idx].EtchingLineDepthMean);
                        }

                        _measureResult.EtchingRegionLeftGapRealList = PaddingArray(_measureResult.EtchingRegionLeftGapRealList, lineNumMax, -1);
                        _measureResult.EtchingRegionRightGapRealList = PaddingArray(_measureResult.EtchingRegionRightGapRealList, lineNumMax, -1);

                        for (int lineIdx = 0; lineIdx < lineNumMax; lineIdx++)
                        {
                            if (_measureResult.PartitionResults.Any(x => x.EtchingLineWidthRealList[lineIdx] == -1))
                            {
                                EtchingLineWidthRealMeanList[lineIdx] = -1;
                                EtchingLineWidthRealMaxList[lineIdx] = -1;
                                EtchingLineWidthRealMinList[lineIdx] = -1;

                                EtchingLineDepthMeanList[lineIdx] = -1;
                                EtchingLineDepthMaxList[lineIdx] = -1;
                                EtchingLineDepthMinList[lineIdx] = -1;

                                _measureResult.IsOK = false;
                            }
                            else
                            {
                                EtchingLineWidthRealMeanList[lineIdx] = _measureResult.PartitionResults.Average(x => x.EtchingLineWidthRealList[lineIdx]);
                                EtchingLineWidthRealMaxList[lineIdx] = _measureResult.PartitionResults.Max(x => x.EtchingLineWidthRealList[lineIdx]);
                                EtchingLineWidthRealMinList[lineIdx] = _measureResult.PartitionResults.Min(x => x.EtchingLineWidthRealList[lineIdx]);

                                EtchingLineDepthMeanList[lineIdx] = _measureResult.PartitionResults.Average(x => x.EtchingLineDepthList[lineIdx]);
                                EtchingLineDepthMaxList[lineIdx] = _measureResult.PartitionResults.Max(x => x.EtchingLineDepthList[lineIdx]);
                                EtchingLineDepthMinList[lineIdx] = _measureResult.PartitionResults.Min(x => x.EtchingLineDepthList[lineIdx]);
                            }
                        }

                        for (int lineIdx = 0; lineIdx < lineNumMax - 1; lineIdx++)
                        {
                            if (_measureResult.PartitionResults.Any(x => x.EtchingPointDistRealList[lineIdx] == -1))
                            {
                                EtchingPointDistRealMeanList[lineIdx] = -1;
                                EtchingPointDistRealMaxList[lineIdx] = -1;
                                EtchingPointDistRealMinList[lineIdx] = -1;

                                _measureResult.IsOK = false;
                            }
                            else
                            {
                                EtchingPointDistRealMeanList[lineIdx] = _measureResult.PartitionResults.Average(x => x.EtchingPointDistRealList[lineIdx]);
                                EtchingPointDistRealMaxList[lineIdx] = _measureResult.PartitionResults.Max(x => x.EtchingPointDistRealList[lineIdx]);
                                EtchingPointDistRealMinList[lineIdx] = _measureResult.PartitionResults.Min(x => x.EtchingPointDistRealList[lineIdx]);
                            }
                        }


                        _measureResult.EtchingLineWidthRealMeanList = EtchingLineWidthRealMeanList;
                        _measureResult.EtchingLineWidthRealMaxList = EtchingLineWidthRealMaxList;
                        _measureResult.EtchingLineWidthRealMinList = EtchingLineWidthRealMinList;

                        _measureResult.EtchingLineDepthMeanList = EtchingLineDepthMeanList;
                        _measureResult.EtchingLineDepthMaxList = EtchingLineDepthMaxList;
                        _measureResult.EtchingLineDepthMinList = EtchingLineDepthMinList;

                        _measureResult.EtchingPointDistRealMeanList = EtchingPointDistRealMeanList;
                        _measureResult.EtchingPointDistRealMaxList = EtchingPointDistRealMaxList;
                        _measureResult.EtchingPointDistRealMinList = EtchingPointDistRealMinList;


                        if (_measureResult.PartitionResults.Any(x => x.EtchingRegionTopGapReal != -1))
                        {
                            _measureResult.EtchingRegionTopGapRealMean = _measureResult.PartitionResults.Average(x => x.EtchingRegionTopGapReal);
                            _measureResult.EtchingRegionTopGapRealMax = _measureResult.PartitionResults.Max(x => x.EtchingRegionTopGapReal);
                            _measureResult.EtchingRegionTopGapRealMin = _measureResult.PartitionResults.Min(x => x.EtchingRegionTopGapReal);
                        }

                        if (_measureResult.PartitionResults.Any(x => x.EtchingRegionBottomGapReal != -1))
                        {
                            _measureResult.EtchingRegionBottomGapRealMean = _measureResult.PartitionResults.Average(x => x.EtchingRegionBottomGapReal);
                            _measureResult.EtchingRegionBottomGapRealMax = _measureResult.PartitionResults.Max(x => x.EtchingRegionBottomGapReal);
                            _measureResult.EtchingRegionBottomGapRealMin = _measureResult.PartitionResults.Min(x => x.EtchingRegionBottomGapReal);
                        }

                    }
                    else
                    {
                        _measureResult.EtchingLineWidthRealMeanList = new double[] { };
                        _measureResult.EtchingLineWidthRealMaxList = new double[] { };
                        _measureResult.EtchingLineWidthRealMinList = new double[] { };

                        _measureResult.EtchingLineDepthMeanList = new double[] { };
                        _measureResult.EtchingLineDepthMaxList = new double[] { };
                        _measureResult.EtchingLineDepthMinList = new double[] { };

                        _measureResult.EtchingPointDistRealMeanList = new double[] { };
                        _measureResult.EtchingPointDistRealMaxList = new double[] { };
                        _measureResult.EtchingPointDistRealMinList = new double[] { };
                    }


                    if (widthReal.Count > 0)
                    {
                        // 槽宽分区平均值
                        _measureResult.GlobalEtchingLineWidthRealMean = widthReal.Average();
                        // 槽宽分区最大值
                        _measureResult.GlobalEtchingLineWidthRealMax = widthReal.Max();
                        // 槽宽分区最小值
                        _measureResult.GlobalEtchingLineWidthRealMin = widthReal.Min();
                        // 槽宽分区极差
                        _measureResult.GlobalEtchingLineWidthRealRange = _measureResult.GlobalEtchingLineWidthRealMax -
                                                                         _measureResult.GlobalEtchingLineWidthRealMin;
                    }
                    else
                    {
                        _measureResult.GlobalEtchingLineWidthRealMean = -1;
                        _measureResult.GlobalEtchingLineWidthRealMax = -1;
                        _measureResult.GlobalEtchingLineWidthRealMin = -1;
                        _measureResult.GlobalEtchingLineWidthRealRange = -1;
                    }

                    if (Depth.Count > 0)
                    {
                        // 槽深分区平均值
                        _measureResult.GlobalEtchingLineDepthMean = Depth.Average();
                        // 槽深分区最大值
                        _measureResult.GlobalEtchingLineDepthMax = Depth.Max();
                        // 槽深分区最小值
                        _measureResult.GlobalEtchingLineDepthMin = Depth.Min();
                        // 槽深分区极差
                        _measureResult.GlobalEtchingLineDepthRange = _measureResult.GlobalEtchingLineDepthMax -
                                                                     _measureResult.GlobalEtchingLineDepthMin;
                    }
                    else
                    {
                        _measureResult.GlobalEtchingLineDepthMean = -1;
                        _measureResult.GlobalEtchingLineDepthMax = -1;
                        _measureResult.GlobalEtchingLineDepthMin = -1;
                        _measureResult.GlobalEtchingLineDepthRange = -1;
                    }

                    // 显示的偏转角度
                    if (_measureParam.PlatePart != 0)
                    {
                        if (_hvPlatePhi.Length > 0)
                        {
                            _measureResult.EtchingLineAngle = -_hvPlatePhi.D;
                        }
                    }
                    else
                    {
                        if (_hvPhi.Length > 0)
                        {
                            _measureResult.EtchingLineAngle = -_hvPhi.D;
                        }
                    }

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

                    //_measureResult.DepthMap = HobjectToFloatArray(_hoHeightImage);
                    //_measureResult.DepthMap = ConvertHObjectToBitmap(_hoHeightImage);

                    result = _measureResult;

                    _measureResult = new KCJC0_MeasureResult();
                }
                finally
                {
                    TmpRows?.Dispose();
                    TmpCols?.Dispose();
                }
            }

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

                //HObject? hoLightingChartDepthMap = null;

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

                        _hvStandardEtchingLineWidthPixel = _measureParam.StandardEtchingLineWidthReal / _measureParam.IntervalX;
                        _hvStandardEtchingPointDistPixel = _measureParam.StandardEtchingPointDistReal / _measureParam.IntervalX;
                        _hvStandardEtchingLineDepthPixel = _measureParam.StandardEtchingLineDepthReal / _measureParam.IntervalZ;

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

                        // 计算样品的倾斜角度
                        CalculateSampleAngle();

                        // 计算对刻蚀线进行筛选的参考点(极片中心区域各条刻蚀线的终点)
                        GetReferenceLine();

                        // 开始测量
                        // 测量刻蚀线起点、终点到极片边缘的距离
                        MeasureEtchingLineStartEndGap();

                        // 拟合刻蚀区顶部或底部边缘，计算刻蚀区顶部或底部到极片边缘的间距
                        MeasureEtchingLineTopBottomGap();

                        // 对极片各分区测量
                        MeasureEtchingRegions();

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

                    //hoLightingChartDepthMap?.Dispose();
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

                image = CvDrawLine(image, measureResult.FitTopEdgeLine.StartPoint, measureResult.FitTopEdgeLine.EndPoint, new Scalar(0, 0, 255), 8);
                image = CvDrawLine(image, measureResult.FitBottomEdgeLine.StartPoint, measureResult.FitBottomEdgeLine.EndPoint, new Scalar(0, 0, 255), 8);
                image = CvDrawLine(image, measureResult.FitStartEdgeLine.StartPoint, measureResult.FitStartEdgeLine.EndPoint, new Scalar(0, 0, 255), 8);
                image = CvDrawLine(image, measureResult.FitEndEdgeLine.StartPoint, measureResult.FitEndEdgeLine.EndPoint, new Scalar(0, 0, 255), 8);

                image = CvDrawLine(image, measureResult.FitEtchingRegionTopEdgeLine.StartPoint, measureResult.FitEtchingRegionTopEdgeLine.EndPoint, new Scalar(0, 255, 0), 8);
                image = CvDrawLine(image, measureResult.FitEtchingRegionBottomEdgeLine.StartPoint, measureResult.FitEtchingRegionBottomEdgeLine.EndPoint, new Scalar(0, 255, 0), 8);
                image = CvDrawLine(image, measureResult.FitEtchingRegionStartEdgeLine.StartPoint, measureResult.FitEtchingRegionStartEdgeLine.EndPoint, new Scalar(0, 255, 0), 8);
                image = CvDrawLine(image, measureResult.FitEtchingRegionEndEdgeLine.StartPoint, measureResult.FitEtchingRegionEndEdgeLine.EndPoint, new Scalar(0, 255, 0), 8);


                int partNum = measureResult.PartitionResults.Count;
                for (int ii = 0; ii < partNum; ii++)
                {
                    int negLineNum = measureResult.PartitionResults[ii].EtchingLineNeg.Count;
                    for (int j = 0; j < negLineNum; j++)
                    {
                        image = CvDrawLine(image, measureResult.PartitionResults[ii].EtchingLineNeg[j].StartPoint, measureResult.PartitionResults[ii].EtchingLineNeg[j].EndPoint,
                                           new Scalar(255, 255, 0), 8);
                    }

                    int posLineNum = measureResult.PartitionResults[ii].EtchingLinePos.Count;
                    for (int j = 0; j < posLineNum; j++)
                    {
                        image = CvDrawLine(image, measureResult.PartitionResults[ii].EtchingLinePos[j].StartPoint, measureResult.PartitionResults[ii].EtchingLinePos[j].EndPoint,
                                           new Scalar(255, 255, 0), 8);
                    }


                    if (showGuides)
                    {
                        image = CvDrawRotatedRect(image, measureResult.PartitionResults[ii].MeasureRegion);
                    }

                }

                if (measureResult.EtchingLineAngle != Single.NegativeInfinity)
                {
                    image = CvDrawPointer(image, measureResult.EtchingLineAngle);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }


            return image;
        }


        /// <summary>
        /// opencv绘直线
        /// </summary>
        public Mat CvDrawLine(Mat image, OpenCvSharp.Point start, OpenCvSharp.Point end, Scalar scalar, int thickness)
        {
            if (start.X != 0 || start.Y != 0 || end.X != 0 || end.Y != 0)
            {
                Cv2.Line(image, start, end, scalar, thickness, LineTypes.AntiAlias);
            }
            return image;
        }


        /// <summary>
        /// opencv绘旋转矩形
        /// </summary>
        public Mat CvDrawRotatedRect(Mat image, RotatedRect rotatedRect)
        {
            List<List<OpenCvSharp.Point>> pointCollection = new List<List<OpenCvSharp.Point>>() { rotatedRect.Corners };

            Cv2.Polylines(image, pointCollection, true, new Scalar(0, 255, 255), 8, LineTypes.AntiAlias);

            return image;
        }


        // 绘制刻度线
        static void DrawScaleLines(Mat image, OpenCvSharp.Point center, int radius, int minAngle, int maxAngle, int step, Scalar color)
        {
            for (int angle = minAngle; angle <= maxAngle; angle += step)
            {
                double radian = angle * Math.PI / 180;
                OpenCvSharp.Point outer = new OpenCvSharp.Point(center.X + (int)(radius * Math.Sin(radian)),
                                                                center.Y - (int)(radius * Math.Cos(radian)));
                OpenCvSharp.Point inner = new OpenCvSharp.Point(center.X + (int)((radius - 20) * Math.Sin(radian)),
                                                                center.Y - (int)((radius - 20) * Math.Cos(radian)));
                Cv2.Line(image, inner, outer, color, angle % 30 == 0 ? 3 : 1);

                // 添加刻度数字
                OpenCvSharp.Point textPos;
                if (angle % 30 == 0)
                {
                    if (angle <= 0)
                    {
                        textPos = new OpenCvSharp.Point(center.X + (int)((radius - 40) * Math.Sin(radian)),
                                                        center.Y - (int)((radius - 40) * Math.Cos(radian)));
                    }
                    else
                    {
                        textPos = new OpenCvSharp.Point(center.X + (int)((radius - 50) * Math.Sin(radian)),
                                                        center.Y - (int)((radius - 50) * Math.Cos(radian)));
                    }

                    Cv2.PutText(image, $"{angle}", textPos, HersheyFonts.HersheySimplex, 0.6, color, 1);
                }
            }
        }


        // 绘制指针
        static void DrawPointer(Mat image, OpenCvSharp.Point center, int length, double angle, Scalar color, int thickness)
        {
            double radian = angle * Math.PI / 180;
            OpenCvSharp.Point tip = new OpenCvSharp.Point(center.X + (int)(length * Math.Sin(radian)),
                                                          center.Y - (int)(length * Math.Cos(radian)));

            Cv2.Line(image, center, tip, color, thickness);

            double tailRadian = (angle + 180) * Math.PI / 180;
            OpenCvSharp.Point tail = new OpenCvSharp.Point(center.X + (int)(20 * Math.Sin(tailRadian)),
                                                           center.Y - (int)(20 * Math.Cos(tailRadian)));
            Cv2.Line(image, center, tail, color, thickness / 2);
        }


        // 根据角度获取指针颜色
        static Scalar GetPointerColor(double angle, int minAngle, int maxAngle)
        {
            double ratio = (double)(Math.Abs(angle) - minAngle) / (maxAngle - minAngle);
            int green = (int)(255 * (1 - ratio));
            int red = (int)(255 * ratio);
            return new Scalar(0, green, red);
        }


        /// <summary>
        /// 绘制极片偏转角度
        /// </summary>
        public Mat CvDrawPointer(Mat image, double radians)
        {
            double angle = radians * (180 / Math.PI);

            int radius = 250;
            OpenCvSharp.Point center = new OpenCvSharp.Point(radius, radius);
            OpenCvSharp.Size axes = new OpenCvSharp.Size(radius, radius);
            Cv2.Ellipse(image, center, axes, 0, -180, 0, new Scalar(0, 0, 255), 4);

            DrawScaleLines(image, center, radius, -90, 90, 5, new Scalar(0, 0, 255));

            Scalar pointerColor = GetPointerColor(angle, 0, 10);
            DrawPointer(image, center, radius - 30, angle, pointerColor, 6);

            Cv2.Circle(image, center, 10, new Scalar(0, 0, 0), -1);
            Cv2.Circle(image, center, 5, new Scalar(255, 255, 255), -1);

            Cv2.PutText(image, $"Etching line deflection {angle:F3} degrees", new OpenCvSharp.Point(center.X - 240, center.Y + 50),
                        HersheyFonts.HersheySimplex, 1, new Scalar(0, 0, 255), 2);

            return image;
        }


    }
}
