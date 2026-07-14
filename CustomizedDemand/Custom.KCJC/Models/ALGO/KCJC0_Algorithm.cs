using HalconDotNet;
using OpenCvSharp;
using ReeYin_V.Core.Services.CustomProject;
using ReeYin_V.Share;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Custom.KCJC.Models
{
    public abstract class KCJC0_Algorithm : ICustomAlgo
    {
        private bool _disposedBase = false;

        protected KCJC0_MeasureParam _measureParam;

        protected HObject _hoGrayImage = new HObject();
        protected HObject _hoHeightImage = new HObject();
        protected HObject _hoIrregularRegion = new HObject();
        protected HObject _hoValidRegion = new HObject();
        protected HObject _hoPlateRegion = new HObject();

        protected HObject _hoFitStartEdge = new HObject();                // 拟合出的极片扫描起始边缘
        protected HObject _hoFitEndEdge = new HObject();                  // 拟合出的极片扫描结束边缘
        protected HObject _hoFitTopEdge = new HObject();                  // 拟合出的极片上边缘
        protected HObject _hoFitBottomEdge = new HObject();               // 拟合出的极片下边缘

        protected HTuple _hvHeightImageMinValue = new HTuple();
        protected HTuple _hvHeightImageMaxValue = new HTuple();
        protected HTuple _hvDepthMapMinValue = new HTuple();
        protected HTuple _hvDepthMapMaxValue = new HTuple();
        protected HTuple _hvPlatePhi = new HTuple();                // 测量极片旋转角

        protected HTuple _hvPlateLeftEdgeMaskSize = new HTuple();           // 头部去除区像素尺寸
        protected HTuple _hvPlateRightEdgeMaskSize = new HTuple();          // 尾部去除区像素尺寸

        protected HTuple _hvImageScaleX = new HTuple();                     // 图片在X轴方向的缩放比例
        protected HTuple _hvImageScaleY = new HTuple();                     // 图片在Y轴方向的缩放比例

        protected HTuple _hvStartEdgeRows = new HTuple(), _hvStartEdgeCols = new HTuple();            // 极片扫描的起始边缘点集
        protected HTuple _hvEndEdgeRows = new HTuple(), _hvEndEdgeCols = new HTuple();                // 极片扫描的结束边缘点集
        protected HTuple _hvStartEdgeRowBegin = new HTuple(), _hvStartEdgeColBegin = new HTuple();    // 极片扫描的起始边缘端点1
        protected HTuple _hvStartEdgeRowEnd = new HTuple(), _hvStartEdgeColEnd = new HTuple();        // 极片扫描的起始边缘端点2
        protected HTuple _hvEndEdgeRowBegin = new HTuple(), _hvEndEdgeColBegin = new HTuple();        // 极片扫描的结束边缘端点1
        protected HTuple _hvEndEdgeRowEnd = new HTuple(), _hvEndEdgeColEnd = new HTuple();            // 极片扫描的结束边缘端点2
        protected HTuple _hvPlateStartEdgePhi = new HTuple(), _hvPlateEndEdgePhi = new HTuple();      // 极片扫描起始结束边缘倾斜角度
        protected HTuple _hvEndRowEdge = new HTuple(), _hvEndColEdge = new HTuple();                  // 极片扫描的结束边缘中点
        protected HTuple _hvStartRowEdge = new HTuple(), _hvStartColEdge = new HTuple();              // 极片扫描的起始边缘中点

        protected HTuple _hvLeftTopRow = new HTuple(), _hvLeftTopColumn = new HTuple();                 // 定位出当前图片视野中极片区域左上角
        protected HTuple _hvRightTopRow = new HTuple(), _hvRightTopColumn = new HTuple();               // 定位出当前图片视野中极片区域右上角
        protected HTuple _hvRightDownRow = new HTuple(), _hvRightDownColumn = new HTuple();             // 定位出当前图片视野中极片区域右下角
        protected HTuple _hvLeftDownRow = new HTuple(), _hvLeftDownColumn = new HTuple();               // 定位出当前图片视野中极片区域左下角

        protected HTuple _hvPlateTopBottomEdgeSamplePointRows = new HTuple();
        protected HTuple _hvPlateTopBottomEdgeSamplePointCols = new HTuple();                            // 极片上下边缘点集

        protected KCJC0_MeasureResult _measureResult = new KCJC0_MeasureResult();                        // 整体测量结果

        static KCJC0_Algorithm()
        {
            HOperatorSet.SetSystem("global_mem_cache", "idle");
            HOperatorSet.SetSystem("temporary_mem_cache", "idle");
            HOperatorSet.SetSystem("image_cache_capacity", 0);
        }

        public KCJC0_Algorithm()
        {
            _measureParam = new KCJC0_MeasureParam();
        }


        public virtual void Dispose()
        {
            if (!_disposedBase)
            {
                _hoGrayImage.Dispose();
                _hoHeightImage.Dispose();
                _hoPlateRegion.Dispose();
                _hoIrregularRegion.Dispose();
                _hoValidRegion.Dispose();
                _hoFitStartEdge.Dispose();
                _hoFitEndEdge.Dispose();
                _hoFitTopEdge.Dispose();
                _hoFitBottomEdge.Dispose();

                _hvHeightImageMinValue.Dispose();
                _hvHeightImageMaxValue.Dispose();
                _hvDepthMapMinValue.Dispose();
                _hvDepthMapMaxValue.Dispose();

                _hvPlatePhi.Dispose();

                _hvPlateLeftEdgeMaskSize.Dispose();
                _hvPlateRightEdgeMaskSize.Dispose();

                _hvImageScaleX.Dispose();
                _hvImageScaleY.Dispose();

                _hvStartEdgeRows.Dispose();
                _hvStartEdgeCols.Dispose();
                _hvEndEdgeRows.Dispose();
                _hvEndEdgeCols.Dispose();
                _hvStartEdgeRowBegin.Dispose();
                _hvStartEdgeColBegin.Dispose();
                _hvStartEdgeRowEnd.Dispose();
                _hvStartEdgeColEnd.Dispose();
                _hvEndEdgeRowBegin.Dispose();
                _hvEndEdgeColBegin.Dispose();
                _hvEndEdgeRowEnd.Dispose();
                _hvEndEdgeColEnd.Dispose();
                _hvPlateStartEdgePhi.Dispose();
                _hvPlateEndEdgePhi.Dispose();
                _hvEndRowEdge.Dispose();
                _hvEndColEdge.Dispose();
                _hvStartRowEdge.Dispose();
                _hvStartColEdge.Dispose();

                _hvLeftTopRow.Dispose(); _hvLeftTopColumn.Dispose();
                _hvRightTopRow.Dispose(); _hvRightTopColumn.Dispose();
                _hvRightDownRow.Dispose(); _hvRightDownColumn.Dispose();
                _hvLeftDownRow.Dispose(); _hvLeftDownColumn.Dispose();

                _hvPlateTopBottomEdgeSamplePointRows.Dispose();
                _hvPlateTopBottomEdgeSamplePointCols.Dispose();

                _measureResult.ClearData();

                _disposedBase = true;
            }

        }


        /// <summary>
        /// 变量初始化
        /// </summary>
        public virtual int InitVariable()
        {
            _disposedBase = false;

            HOperatorSet.GenEmptyObj(out _hoGrayImage);
            HOperatorSet.GenEmptyObj(out _hoHeightImage);
            HOperatorSet.GenEmptyObj(out _hoPlateRegion);
            HOperatorSet.GenEmptyObj(out _hoIrregularRegion);
            HOperatorSet.GenEmptyObj(out _hoValidRegion);
            HOperatorSet.GenEmptyObj(out _hoFitStartEdge);
            HOperatorSet.GenEmptyObj(out _hoFitEndEdge);
            HOperatorSet.GenEmptyObj(out _hoFitTopEdge);
            HOperatorSet.GenEmptyObj(out _hoFitBottomEdge);


            _hvHeightImageMinValue = new HTuple();
            _hvHeightImageMaxValue = new HTuple();
            _hvDepthMapMinValue = new HTuple();
            _hvDepthMapMaxValue = new HTuple();

            _hvPlatePhi = new HTuple();


            _hvPlateLeftEdgeMaskSize = new HTuple();
            _hvPlateRightEdgeMaskSize = new HTuple();

            _hvImageScaleX = new HTuple();
            _hvImageScaleY = new HTuple();

            _hvStartEdgeRows = new HTuple();
            _hvStartEdgeCols = new HTuple();
            _hvEndEdgeRows = new HTuple();
            _hvEndEdgeCols = new HTuple();
            _hvStartEdgeRowBegin = new HTuple();
            _hvStartEdgeColBegin = new HTuple();
            _hvStartEdgeRowEnd = new HTuple();
            _hvStartEdgeColEnd = new HTuple();
            _hvEndEdgeRowBegin = new HTuple();
            _hvEndEdgeColBegin = new HTuple();
            _hvEndEdgeRowEnd = new HTuple();
            _hvEndEdgeColEnd = new HTuple();
            _hvPlateStartEdgePhi = new HTuple();
            _hvPlateEndEdgePhi = new HTuple();
            _hvEndRowEdge = new HTuple();
            _hvEndColEdge = new HTuple();
            _hvStartRowEdge = new HTuple();
            _hvStartColEdge = new HTuple();



            _hvLeftTopRow = new HTuple(0); _hvLeftTopColumn = new HTuple(0);
            _hvRightTopRow = new HTuple(0); _hvRightTopColumn = new HTuple(0);
            _hvRightDownRow = new HTuple(0); _hvRightDownColumn = new HTuple(0);
            _hvLeftDownRow = new HTuple(0); _hvLeftDownColumn = new HTuple(0);

            _hvPlateTopBottomEdgeSamplePointRows = new HTuple();
            _hvPlateTopBottomEdgeSamplePointCols = new HTuple();

            _measureResult = new KCJC0_MeasureResult();

            return 0;
        }


        /// <summary>
        /// OpenCVSharp Mat转List<float[]>
        /// </summary>
        public List<float[]> ConvertMatToList(Mat mat)
        {
            List<float[]> data = new List<float[]>();

            if (mat == null || mat.Empty())
                return data;

            Mat work = mat;
            bool needDispose = false;

            if (!mat.IsContinuous())
            {
                work = mat.Clone();
                needDispose = true;
            }

            int channels = mat.Channels();
            if (channels != 1)
                throw new InvalidOperationException("Only single-channel matrices are supported");

            try
            {
                int rows = work.Rows;
                int cols = work.Cols;
                MatType type = work.Type();

                if (type == MatType.CV_8UC1)
                {
                    ProcessByteMat(mat, rows, cols, data);
                }
                else if (type == MatType.CV_32FC1)
                {
                    ProcessFloatMat(mat, rows, cols, data);
                }
                else if (type == MatType.CV_32SC1)
                {
                    ProcessIntMat(mat, rows, cols, data);
                }
                else if (type == MatType.CV_16SC1)
                {
                    ProcessShortMat(mat, rows, cols, data);
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported matrix type: {type}");
                }
            }
            finally
            {
                if (needDispose) 
                    work.Dispose();
            }

            return data;
        }


        private static void ProcessByteMat(Mat mat, int rows, int cols, List<float[]> data)
        {
            byte[] buffer = new byte[rows * cols];
            Marshal.Copy(mat.Data, buffer, 0, buffer.Length);

            for (int i = 0; i < rows; i++)
            {
                float[] row = new float[cols];
                int offset = i * cols;
                for (int j = 0; j < cols; j++)
                {
                    row[j] = buffer[offset + j];
                }
                data.Add(row);
            }
        }

        private static void ProcessFloatMat(Mat mat, int rows, int cols, List<float[]> data)
        {
            float[] buffer = new float[rows * cols];
            Marshal.Copy(mat.Data, buffer, 0, buffer.Length);

            for (int i = 0; i < rows; i++)
            {
                float[] row = new float[cols];
                Array.Copy(buffer, i * cols, row, 0, cols);
                data.Add(row);
            }
        }

        private static void ProcessIntMat(Mat mat, int rows, int cols, List<float[]> data)
        {
            int[] buffer = new int[rows * cols];
            Marshal.Copy(mat.Data, buffer, 0, buffer.Length);

            for (int i = 0; i < rows; i++)
            {
                float[] row = new float[cols];
                for (int j = 0; j < cols; j++)
                {
                    row[j] = buffer[i * cols + j];  // 转为 float 存入结果
                }
                data.Add(row);
            }
        }

        private static void ProcessShortMat(Mat mat, int rows, int cols, List<float[]> data)
        {
            short[] buffer = new short[rows * cols];
            Marshal.Copy(mat.Data, buffer, 0, buffer.Length);

            for (int i = 0; i < rows; i++)
            {
                float[] row = new float[cols];
                for (int j = 0; j < cols; j++)
                {
                    row[j] = buffer[i * cols + j];  // 转 float 存储
                }
                data.Add(row);
            }
        }



        /// <summary>
        /// 将List<float[]>数组转换为halcon图片对象
        /// </summary>
        /// <param name="data">输入的List<float[]>数组</param>
        /// <param name="imageType">输入图片数据类型</param>
        /// <param name="hoObject">输出的halcon图片对象</param>
        /// <returns>状态标志</returns>
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

        public static void ReplaceHobject(ref HObject target, ref HObject? source)
        {
            var current = target;
            if (!ReferenceEquals(current, source))
            {
                current?.Dispose();
            }

            target = source ?? new HObject();
            source = null;
        }


        /// <summary>
        /// 修正深度图异常点,转换深度图图片类型
        /// </summary>
        protected virtual int ModifyHeightImageOutlier()
        {
            using (var dh = new HDevDisposeHelper())
            {
                
                HTuple hvRange = new HTuple();

                HObject? hoRectangle = null;
                HObject? hoTmp = null;

                try
                {
                    HOperatorSet.GetImageSize(_hoGrayImage, out HTuple hvWidth, out HTuple hvHeight);
                    if (_measureParam.ScanWidth != hvWidth)
                        _measureParam.ScanWidth = hvWidth;
                    if (_measureParam.ScanHeight != hvHeight)
                        _measureParam.ScanHeight = hvHeight;

                    HOperatorSet.GenRectangle1(out hoRectangle, 0, 0, _measureParam.ScanHeight, _measureParam.ScanWidth);
                    HOperatorSet.MinMaxGray(hoRectangle, _hoHeightImage, 0, out _hvHeightImageMinValue, out _hvHeightImageMaxValue, out hvRange);
                    HOperatorSet.Threshold(_hoHeightImage, out hoTmp, 8888880, 8888880);
                    ReplaceHobject(ref _hoIrregularRegion, ref hoTmp);
                    HOperatorSet.Union1(_hoIrregularRegion, out hoTmp);
                    ReplaceHobject(ref _hoIrregularRegion, ref hoTmp);
                    HOperatorSet.PaintRegion(_hoIrregularRegion, _hoHeightImage, out hoTmp, _hvHeightImageMinValue, "fill");
                    ReplaceHobject(ref _hoHeightImage, ref hoTmp);
                    HOperatorSet.ConvertImageType(_hoHeightImage, out hoTmp, "real");
                    ReplaceHobject(ref _hoHeightImage, ref hoTmp);

                    HOperatorSet.Difference(hoRectangle, _hoIrregularRegion, out hoTmp);
                    ReplaceHobject(ref _hoValidRegion, ref hoTmp);
                }
                finally
                {
                    hoTmp?.Dispose();
                    hoRectangle?.Dispose();

                    hvRange?.Dispose();
                }

            }

            return 0;
        }


        /// <summary>
        /// 修正深度图异常点,转换深度图图片类型
        /// </summary>
        protected virtual int ModifyHeightImageOutlierV2()
        {
            using (var dh = new HDevDisposeHelper())
            {
                HTuple hvRange = new HTuple();

                HObject? hoRectangle = null;
                HObject? hoTmp = null;

                try
                {
                    HOperatorSet.GetImageSize(_hoGrayImage, out HTuple hvWidth, out HTuple hvHeight);
                    if (_measureParam.ScanWidth != hvWidth)
                        _measureParam.ScanWidth = hvWidth;
                    if (_measureParam.ScanHeight != hvHeight)
                        _measureParam.ScanHeight = hvHeight;

                    HOperatorSet.GenRectangle1(out hoRectangle, 0, 0, _measureParam.ScanHeight - 1, _measureParam.ScanWidth - 1);

                    HOperatorSet.ConvertImageType(_hoHeightImage, out hoTmp, "real");
                    ReplaceHobject(ref _hoHeightImage, ref hoTmp);

                    HOperatorSet.Threshold(_hoHeightImage, out hoTmp, 888888-1, 888888+1);
                    ReplaceHobject(ref _hoIrregularRegion, ref hoTmp);
                    HOperatorSet.Difference(hoRectangle, _hoIrregularRegion, out hoTmp);
                    ReplaceHobject(ref _hoValidRegion, ref hoTmp);

                    //HOperatorSet.ReduceDomain(_hoHeightImage, _hoValidRegion, out hoTmp);
                    //ReplaceHobject(ref _hoHeightImage, ref hoTmp);

                    HOperatorSet.MinMaxGray(_hoValidRegion, _hoHeightImage, 0, out _hvHeightImageMinValue, out _hvHeightImageMaxValue, out hvRange);
                    HOperatorSet.PaintRegion(_hoIrregularRegion, _hoHeightImage, out hoTmp, _hvHeightImageMinValue, "fill");
                    ReplaceHobject(ref _hoHeightImage, ref hoTmp);
                }
                finally
                {
                    hoTmp?.Dispose();
                    hoRectangle?.Dispose();

                    hvRange.Dispose();
                }
            }

            return 0;
        }


        /// <summary>
        /// halcon HObject类型的深度图片转float[][]类型数据
        /// </summary>
        protected virtual float[][] HobjectToFloatArray(HObject hoImage)
        {
            float[][] dst = new float[][] { };

            using (var dh = new HDevDisposeHelper())
            {
                HObject? hoTmp = null;

                try
                {
                    HOperatorSet.CountChannels(hoImage, out HTuple hvChannels);

                    if (hvChannels.Length == 0)
                    {
                        return dst;
                    }
                    if (hvChannels[0].I == 1)
                    {
                        double tmpScaleW = 1 / (_measureParam.DepthMapSampleDownSizeW * 1.0);
                        double tmpScaleH = 1 / (_measureParam.DepthMapSampleDownSizeH * 1.0);

                        HOperatorSet.ZoomImageFactor(hoImage, out hoTmp, tmpScaleW, tmpScaleH, "nearest_neighbor");
                        ReplaceHobject(ref hoImage, ref hoTmp);

                        //HOperatorSet.MirrorImage(hoImage, out hoTmp, "column");
                        //ReplaceHobject(ref hoImage, ref hoTmp);

                        HOperatorSet.GetImagePointer1(hoImage, out HTuple hvPointer, out HTuple hvType, out HTuple hvWidth, out HTuple hvHeight);

                        int w = hvWidth;
                        int h = hvHeight;
                        int size = w * h;
                        float[] floatBuffer = new float[size];
                        Marshal.Copy(hvPointer, floatBuffer, 0, size);

                        float[][] result = new float[h][];
                        for (int y = 0; y < h; y++)
                        {
                            result[y] = new float[w];
                            for (int x = 0; x < w; x++)
                            {
                                int index = y * w + x;
                                result[y][x] = floatBuffer[index];
                            }
                        }

                        dst = result;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                }
                finally
                {
                    hoTmp?.Dispose();
                }
            }
            return dst;
        }


        /// <summary>
        /// halcon HObject类型图片转OpenCVSharp Mat类型
        /// </summary>
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
                    IntPtr intPtr = IntPtr.Zero;
                    HOperatorSet.GetImagePointer1(hoImage, out HTuple hvPointer, out HTuple hvType, out HTuple hvWidth, out HTuple hvHeight);
                    intPtr = hvPointer;
                    dst = Mat.FromPixelData(hvHeight, hvWidth, MatType.CV_8UC1, intPtr);
                }
                else if (hvChannels[0].I == 3)
                {
                    IntPtr ptrRed = IntPtr.Zero;
                    IntPtr ptrGreen = IntPtr.Zero;
                    IntPtr ptrBlue = IntPtr.Zero;

                    HOperatorSet.GetImagePointer3(hoImage, out HTuple hvPtrRed, out HTuple hvPtrGreen, out HTuple hvPtrBlue,
                                                  out HTuple hvType, out HTuple hvWidth, out HTuple hvHeight);
                    ptrRed = hvPtrRed;
                    ptrGreen = hvPtrGreen;
                    ptrBlue = hvPtrBlue;

                    //分别生成3张图片
                    matRed = Mat.FromPixelData(hvHeight, hvWidth, MatType.CV_8UC1, ptrRed);
                    matGreen = Mat.FromPixelData(hvHeight, hvWidth, MatType.CV_8UC1, ptrGreen);
                    matBlue = Mat.FromPixelData(hvHeight, hvWidth, MatType.CV_8UC1, ptrBlue);

                    //合成
                    Cv2.Merge(new[] { matBlue, matGreen, matRed }, dst);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                //释放
                matBlue?.Dispose();
                matGreen?.Dispose();
                matRed?.Dispose();
            }
        }



        /// <summary>
        /// 根据X、Y方向的像素当量比例缩放图片
        /// </summary>
        protected virtual int ScaleGrayHeightImage()
        {
            using (var dh = new HDevDisposeHelper())
            {
                HObject? hoTmp = null;

                try
                {
                    HOperatorSet.GetImageSize(_hoGrayImage, out HTuple hvWidth0, out HTuple hvHeight0);
                    HOperatorSet.GetImageSize(_hoHeightImage, out HTuple hvWidth1, out HTuple hvHeight1);

                    HOperatorSet.ZoomImageFactor(_hoGrayImage, out hoTmp, _hvImageScaleX, _hvImageScaleY, "bilinear");
                    ReplaceHobject(ref _hoGrayImage, ref hoTmp);
                    HOperatorSet.ZoomImageFactor(_hoHeightImage, out hoTmp, _hvImageScaleX, _hvImageScaleY, "nearest_neighbor");
                    ReplaceHobject(ref _hoHeightImage, ref hoTmp);
                    HTuple hvWidth = new HTuple();
                    HTuple hvHeight = new HTuple();
                    HOperatorSet.GetImageSize(_hoGrayImage, out hvWidth, out hvHeight);

                    HOperatorSet.ZoomRegion(_hoValidRegion, out hoTmp, _hvImageScaleX, _hvImageScaleY);
                    ReplaceHobject(ref _hoValidRegion, ref hoTmp);
                    if (_measureParam.ScanWidth != hvWidth)
                        _measureParam.ScanWidth = hvWidth;
                    if (_measureParam.ScanHeight != hvHeight)
                        _measureParam.ScanHeight = hvHeight;
                    hvWidth.Dispose();
                    hvHeight.Dispose();

                    _measureParam.IntervalY = _measureParam.IntervalX;
                    _measureParam.IntervalZ = _measureParam.IntervalZ / (_measureParam.IntervalZ * 10);

                    _hvPlateLeftEdgeMaskSize = _measureParam.PlateLeftEdgeMaskSizeReal / _measureParam.IntervalY;
                    _hvPlateRightEdgeMaskSize = _measureParam.PlateRightEdgeMaskSizeReal / _measureParam.IntervalY;
                }
                finally
                {
                    hoTmp?.Dispose();
                }
            }

            return 0;
        }


        /// <summary>
        /// 根据X、Y方向的像素当量比例缩放图片
        /// </summary>
        protected virtual int ScaleGrayHeightImageV2()
        {
            using (var dh = new HDevDisposeHelper())
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

                    HTuple hvWidth = new HTuple();
                    HTuple hvHeight = new HTuple();
                    HOperatorSet.GetImageSize(_hoGrayImage, out hvWidth, out hvHeight);

                    if (_measureParam.ScanWidth != hvWidth)
                        _measureParam.ScanWidth = hvWidth;
                    if (_measureParam.ScanHeight != hvHeight)
                        _measureParam.ScanHeight = hvHeight;
                    hvWidth.Dispose();
                    hvHeight.Dispose();

                    _measureParam.IntervalX = (_measureParam.IntervalX / _hvImageScaleX).D;
                    _measureParam.IntervalY = (_measureParam.IntervalY / _hvImageScaleY).D;
                    _measureParam.IntervalZ = _measureParam.IntervalZ / _measureParam.IntervalZ;

                    _hvPlateLeftEdgeMaskSize = _measureParam.PlateLeftEdgeMaskSizeReal / _measureParam.IntervalY;
                    _hvPlateRightEdgeMaskSize = _measureParam.PlateRightEdgeMaskSizeReal / _measureParam.IntervalY;


                }
                finally
                {
                    hoTmp?.Dispose();
                }
            }
            return 0;
        }


        /// <summary>
        /// 判断极片的扫描部位
        /// </summary>
        protected virtual int DetectPlateRegion()
        {
            using (var dh = new HDevDisposeHelper())
            {
                try
                {
                    HTuple hvPlateCenterR = new HTuple();
                    HTuple hvPlateCenterC = new HTuple();

                    HTuple hvTmpTopEdgeMeasureHandle = new HTuple();
                    HTuple hvTmpBottomEdgeMeasureHandle = new HTuple();

                    HTuple hvAmplitudePos = new HTuple();
                    HTuple hvDistancePos = new HTuple();

                    HTuple hvTmpTopRowEdge = new HTuple();
                    HTuple hvTmpTopColEdge = new HTuple();
                    HTuple hvTmpBottomRowEdge = new HTuple();
                    HTuple hvTmpBottomColEdge = new HTuple();

                    HTuple hvDetRegW = (_measureParam.ScanWidth * 0.05) * 0.5;

                    hvPlateCenterR = _measureParam.ScanHeight * 0.5;
                    hvPlateCenterC = _measureParam.ScanWidth * 0.5;

                    // 定位极片上边缘
                    HOperatorSet.GenMeasureRectangle2(hvPlateCenterR, hvPlateCenterC, (new HTuple(180)).TupleRad(),
                                                      _measureParam.ScanWidth * 0.5 - _measureParam.ImageEdgeMaskSize, hvDetRegW,
                                                      _measureParam.ScanWidth, _measureParam.ScanHeight, "bilinear", out hvTmpTopEdgeMeasureHandle);
                    HOperatorSet.MeasurePos(_hoHeightImage, hvTmpTopEdgeMeasureHandle, _measureParam.HeightAmplitudeSigma, _measureParam.HeightAmplitudeThr,
                                            "positive", "first", out hvTmpTopRowEdge, out hvTmpTopColEdge, out hvAmplitudePos, out hvDistancePos);
                    HOperatorSet.CloseMeasure(hvTmpTopEdgeMeasureHandle);

                    // 定位极片下边缘
                    HOperatorSet.GenMeasureRectangle2(hvPlateCenterR, hvPlateCenterC, (new HTuple(0)).TupleRad(),
                                                      _measureParam.ScanWidth * 0.5 - _measureParam.ImageEdgeMaskSize, hvDetRegW,
                                                      _measureParam.ScanWidth, _measureParam.ScanHeight, "bilinear", out hvTmpBottomEdgeMeasureHandle);
                    HOperatorSet.MeasurePos(_hoHeightImage, hvTmpBottomEdgeMeasureHandle, _measureParam.HeightAmplitudeSigma, _measureParam.HeightAmplitudeThr,
                                            "positive", "first", out hvTmpBottomRowEdge, out hvTmpBottomColEdge, out hvAmplitudePos, out hvDistancePos);
                    HOperatorSet.CloseMeasure(hvTmpBottomEdgeMeasureHandle);

                    if (hvTmpTopRowEdge.Length > 0 && hvTmpBottomRowEdge.Length <= 0)
                    {
                        _measureParam.PlatePart = -1;
                    }
                    else if (hvTmpTopRowEdge.Length <= 0 && hvTmpBottomRowEdge.Length > 0)
                    {
                        _measureParam.PlatePart = 1;
                    }
                    else
                    {
                        _measureParam.PlatePart = 0;
                    }
                }
                finally
                {

                }
            }

            return 0;
        }



        /// <summary>
        /// 边缘卡尺精定位边缘
        /// </summary>
        protected virtual int LineCalipers(HObject hoImage, HTuple hvRowBegin, HTuple hvColumnBegin, HTuple hvRowEnd, HTuple hvColumnEnd,
                                       HTuple hvMeasureWidth, HTuple hvMeasureHeight, HTuple hvMeasureSigma, HTuple hvMeasureThr,
                                       HTuple hvMeasureTransition, HTuple hvMeasureNum, HTuple hvMeasureSelect, HTuple hvMinScore,
                                       out HObject hoFitLine, out HTuple hvEdgeRows, out HTuple hvEdgeCols)
        {
            using (var dh = new HDevDisposeHelper())
            {
                HObject? hoTmp = null;

                HObject? hoContours = null;

                HTuple hvImgWidth = new HTuple();
                HTuple hvImgHeight = new HTuple();
                HTuple hvMetrologyHandle = new HTuple();
                HTuple hvIndex = new HTuple();
                HTuple hvParameter = new HTuple();
                HTuple hvFitLineNum = new HTuple();

                try
                {
                    HOperatorSet.GenEmptyObj(out hoFitLine);
                    HOperatorSet.GenEmptyObj(out hoContours);

                    hvEdgeRows = new HTuple();
                    hvEdgeCols = new HTuple();

                    HOperatorSet.GetImageSize(hoImage, out hvImgWidth, out hvImgHeight);
                    HOperatorSet.CreateMetrologyModel(out hvMetrologyHandle);
                    HOperatorSet.SetMetrologyModelImageSize(hvMetrologyHandle, hvImgWidth, hvImgHeight);

                    HOperatorSet.AddMetrologyObjectLineMeasure(hvMetrologyHandle, hvRowBegin, hvColumnBegin, hvRowEnd, hvColumnEnd,
                                                               hvMeasureHeight, hvMeasureWidth, hvMeasureSigma, hvMeasureThr, new HTuple(), new HTuple(), out hvIndex);

                    HOperatorSet.SetMetrologyObjectParam(hvMetrologyHandle, "all", "measure_transition", hvMeasureTransition);
                    HOperatorSet.SetMetrologyObjectParam(hvMetrologyHandle, "all", "num_measures", hvMeasureNum);
                    HOperatorSet.SetMetrologyObjectParam(hvMetrologyHandle, "all", "measure_select", hvMeasureSelect);
                    HOperatorSet.SetMetrologyObjectParam(hvMetrologyHandle, "all", "num_instances", ((hvMeasureNum * 0.25)).TupleInt());
                    HOperatorSet.SetMetrologyObjectParam(hvMetrologyHandle, "all", "min_score", hvMinScore);

                    HOperatorSet.ApplyMetrologyModel(hoImage, hvMetrologyHandle);
                    HOperatorSet.GetMetrologyObjectMeasures(out hoTmp, hvMetrologyHandle, "all", "all", out hvEdgeRows, out hvEdgeCols);
                    ReplaceHobject(ref hoContours, ref hoTmp);
                    HOperatorSet.GetMetrologyObjectResult(hvMetrologyHandle, "all", "all", "result_type", "all_param", out hvParameter);
                    HOperatorSet.GetMetrologyObjectResultContour(out hoTmp, hvMetrologyHandle, 0, "all", 1.5);
                    ReplaceHobject(ref hoFitLine, ref hoTmp);

                    

                    HOperatorSet.CountObj(hoFitLine, out hvFitLineNum);
                    if (hvFitLineNum.I > 1)
                    {
                        HOperatorSet.SelectObj(hoFitLine, out hoTmp, 1);
                        ReplaceHobject(ref hoFitLine, ref hoTmp);
                    }
                    else if (hvFitLineNum.I == 0)
                    {
                        if (hvEdgeRows.TupleLength() > 1)
                        {
                            HObject hoPlatEdgeRightContour;
                            HTuple hvTmpRowBegin = new HTuple();
                            HTuple hvTmpColBegin = new HTuple();
                            HTuple hvTmpRowEnd = new HTuple();
                            HTuple hvTmpColEnd = new HTuple();
                            HTuple hvNr = new HTuple();
                            HTuple hvNc = new HTuple();
                            HTuple hvDist = new HTuple();

                            HOperatorSet.GenEmptyObj(out hoPlatEdgeRightContour);

                            HOperatorSet.GenContourPolygonXld(out hoPlatEdgeRightContour, hvEdgeRows, hvEdgeCols);
                            HOperatorSet.FitLineContourXld(hoPlatEdgeRightContour, "tukey", -1, 0, 5, 2, out hvTmpRowBegin, out hvTmpColBegin, out hvTmpRowEnd, out hvTmpColEnd,
                                                           out hvNr, out hvNc, out hvDist);
                            HOperatorSet.GenContourPolygonXld(out hoTmp, hvTmpRowBegin.TupleConcat(hvTmpRowEnd), hvTmpColBegin.TupleConcat(hvTmpColEnd));
                            ReplaceHobject(ref hoFitLine, ref hoTmp);

                            hoPlatEdgeRightContour.Dispose();
                            hvTmpRowBegin.Dispose();
                            hvTmpColBegin.Dispose();
                            hvTmpRowEnd.Dispose();
                            hvTmpColEnd.Dispose();
                            hvNr.Dispose();
                            hvNc.Dispose();
                            hvDist.Dispose();
                        }
                        else if (hvEdgeRows.TupleLength() == 1)
                        {
                            HOperatorSet.GenContourPolygonXld(out hoTmp, ((hvEdgeRows.TupleSelect(0))).TupleConcat(hvEdgeRows.TupleSelect(0)),
                                                              (new HTuple(0)).TupleConcat(hvImgWidth));
                            ReplaceHobject(ref hoFitLine, ref hoTmp);
                        }
                    }

                    HOperatorSet.ClearMetrologyModel(hvMetrologyHandle);
                }
                finally
                {
                    

                    hoTmp?.Dispose();
                    hoContours?.Dispose();

                    hvImgWidth.Dispose();
                    hvImgHeight.Dispose();
                    hvMetrologyHandle.Dispose();
                    hvIndex.Dispose();
                    hvParameter.Dispose();
                }
            }

            return 0;
        }



        /// <summary>
        /// 拟合顶部或底部边缘,调整对极片边缘的拟合
        /// </summary>
        protected virtual int FitPlateTopBottomEdgeFixSEEdge()
        {
            using (var dh = new HDevDisposeHelper())
            {
                HObject? hoTmp = null;

                HObject? hoPlateTopBottomEdgeContour = null;
                HObject? hoFitPlateTopBottomEdge = null;

                HTuple hvSamplingPointRows = new HTuple();

                HTuple hvSamplingPointCol = new HTuple();
                HTuple hvSamplingPointRow = new HTuple();
                HTuple hvMeasureHandle = new HTuple();

                HTuple hvTmpEdgeRow = new HTuple();
                HTuple hvTmpEdgeCol = new HTuple();
                HTuple hvAmplitudeNeg = new HTuple();
                HTuple hvDistanceNeg = new HTuple();

                HTuple hvPlateEdgeRowBegin = new HTuple();
                HTuple hvPlateEdgeColBegin = new HTuple();
                HTuple hvPlateEdgeRowEnd = new HTuple();
                HTuple hvPlateEdgeRowColEnd = new HTuple();

                HTuple hv_Nr = new HTuple();
                HTuple hv_Nc = new HTuple();
                HTuple hv_Dist = new HTuple();

                HTuple hvSampleNum = _measureParam.PlateTDEdgeSamplingMum;
                HTuple hvTmpBeginRow = (_hvStartEdgeRowBegin + _hvStartEdgeRowEnd) * 0.5;
                HTuple hvTmpEndRow = (_hvEndEdgeRowBegin + _hvEndEdgeRowEnd) * 0.5;

                try
                {
                    HOperatorSet.TupleGenSequence(hvTmpBeginRow, hvTmpEndRow, (((hvTmpEndRow - hvTmpBeginRow) / hvSampleNum)).TupleInt(), out hvSamplingPointRows);

                    for (int idx = 1; idx < hvSamplingPointRows.TupleLength() - 1; idx++)
                    {
                        hvSamplingPointCol = _measureParam.ScanWidth * 0.5;
                        hvSamplingPointRow = hvSamplingPointRows.TupleSelect(idx);

                        if (_measureParam.PlatePart == -1)
                        {
                            // 拟合极片顶部边缘
                            HOperatorSet.GenMeasureRectangle2(hvSamplingPointRow, hvSamplingPointCol, (_hvPlateStartEdgePhi + _hvPlateEndEdgePhi) * 0.5,
                                                              _measureParam.ScanWidth * 0.5 - _measureParam.ImageEdgeMaskSize, 60,
                                                              _measureParam.ScanWidth, _measureParam.ScanHeight, "bilinear", out hvMeasureHandle);
                        }
                        else if (_measureParam.PlatePart == 1)
                        {
                            // 拟合极片底部边缘
                            HOperatorSet.GenMeasureRectangle2(hvSamplingPointRow, hvSamplingPointCol, (_hvPlateStartEdgePhi + _hvPlateEndEdgePhi) * 0.5 + (new HTuple(180)).TupleRad(),
                                                              _measureParam.ScanWidth * 0.5 - _measureParam.ImageEdgeMaskSize, 60,
                                                              _measureParam.ScanWidth, _measureParam.ScanHeight, "bilinear", out hvMeasureHandle);
                        }


                        hvTmpEdgeRow = new HTuple();
                        hvTmpEdgeCol = new HTuple();
                        HOperatorSet.MeasurePos(_hoHeightImage, hvMeasureHandle, _measureParam.HeightAmplitudeSigma, _measureParam.HeightAmplitudeThr, "negative", "first",
                                                out hvTmpEdgeRow, out hvTmpEdgeCol, out hvAmplitudeNeg, out hvDistanceNeg);

                        _hvPlateTopBottomEdgeSamplePointRows = _hvPlateTopBottomEdgeSamplePointRows.TupleConcat(hvTmpEdgeRow);
                        _hvPlateTopBottomEdgeSamplePointCols = _hvPlateTopBottomEdgeSamplePointCols.TupleConcat(hvTmpEdgeCol);

                    }
                    HOperatorSet.CloseMeasure(hvMeasureHandle);

                    HOperatorSet.GenContourPolygonXld(out hoPlateTopBottomEdgeContour, _hvPlateTopBottomEdgeSamplePointRows, _hvPlateTopBottomEdgeSamplePointCols);
                    HOperatorSet.FitLineContourXld(hoPlateTopBottomEdgeContour, "tukey", -1, 0, 5, 2, out hvPlateEdgeRowBegin, out hvPlateEdgeColBegin, out hvPlateEdgeRowEnd,
                                                   out hvPlateEdgeRowColEnd, out hv_Nr, out hv_Nc, out hv_Dist);
                    HOperatorSet.GenContourPolygonXld(out hoFitPlateTopBottomEdge, hvPlateEdgeRowBegin.TupleConcat(hvPlateEdgeRowEnd), hvPlateEdgeColBegin.TupleConcat(hvPlateEdgeRowColEnd));


                    HTuple hvStartEdgePointHalfNum = new HTuple(_hvStartEdgeRows.TupleLength() * 0.5).TupleFloor();
                    if (hvStartEdgePointHalfNum.D < 1)
                    {
                        hvStartEdgePointHalfNum = new HTuple(1);
                    }
                    HTuple hvEndEdgePointHalfNum = new HTuple(_hvEndEdgeRows.TupleLength() * 0.5).TupleFloor();
                    if (hvEndEdgePointHalfNum.D < 1)
                    {
                        hvEndEdgePointHalfNum = new HTuple(1);
                    }

                    // 修正极片扫描的始末边缘
                    HTuple hvStartEdgePointBeginIdx = new HTuple();
                    HTuple hvStartEdgePointEndIdx = new HTuple();
                    HTuple hvEndEdgePointBeginIdx = new HTuple();
                    HTuple hvEndEdgePointEndIdx = new HTuple();
                    if (_measureParam.PlatePart == -1)
                    {
                        hvStartEdgePointBeginIdx = 0;
                        hvStartEdgePointEndIdx = hvStartEdgePointHalfNum - 1;
                        hvEndEdgePointBeginIdx = 0;
                        hvEndEdgePointEndIdx = hvEndEdgePointHalfNum - 1;

                        _hoFitTopEdge = new HObject(hoFitPlateTopBottomEdge);
                        HOperatorSet.GenContourPolygonXld(out hoTmp, new HTuple(_measureParam.ScanHeight * 0.5 - 10, _measureParam.ScanHeight * 0.5 + 10), new HTuple(0, 0));
                        ReplaceHobject(ref _hoFitBottomEdge, ref hoTmp);
                    }
                    else if (_measureParam.PlatePart == 1)
                    {
                        hvStartEdgePointBeginIdx = hvStartEdgePointHalfNum;
                        hvStartEdgePointEndIdx = _hvStartEdgeRows.TupleLength() - 1;
                        hvEndEdgePointBeginIdx = hvEndEdgePointHalfNum;
                        hvEndEdgePointEndIdx = _hvEndEdgeRows.TupleLength() - 1;

                        HOperatorSet.GenContourPolygonXld(out hoTmp, new HTuple(_measureParam.ScanHeight * 0.5 - 10, _measureParam.ScanHeight * 0.5 + 10),
                                                          new HTuple(_measureParam.ScanWidth, _measureParam.ScanWidth));
                        ReplaceHobject(ref _hoFitTopEdge, ref hoTmp);
                        _hoFitBottomEdge = new HObject(hoFitPlateTopBottomEdge);
                    }

                    // 扫描起始边缘采样点
                    HTuple hvHalfStartEdgeRows, hvHalfStartEdgeCols;
                    HObject hoTmpPlateStartEdge;
                    HTuple hvTmpStartEdgeRowBegin, hvTmpStartEdgeColBegin, hvTmpStartEdgeRowEnd, hvTmpStartEdgeColEnd;
                    HTuple hvTmpPlateStartEdgePhi;
                    HOperatorSet.TupleSelectRange(_hvStartEdgeRows, hvStartEdgePointBeginIdx, hvStartEdgePointEndIdx, out hvHalfStartEdgeRows);
                    HOperatorSet.TupleSelectRange(_hvStartEdgeCols, hvStartEdgePointBeginIdx, hvStartEdgePointEndIdx, out hvHalfStartEdgeCols);
                    if (hvStartEdgePointHalfNum > 1)
                    {
                        HObject hoTmpContour;

                        HOperatorSet.GenContourPolygonXld(out hoTmpContour, hvHalfStartEdgeRows, hvHalfStartEdgeCols);
                        HOperatorSet.FitLineContourXld(hoTmpContour, "tukey", -1, 0, 5, 2,
                                                       out hvTmpStartEdgeRowBegin, out hvTmpStartEdgeColBegin, out hvTmpStartEdgeRowEnd, out hvTmpStartEdgeColEnd,
                                                       out hv_Nr, out hv_Nc, out hv_Dist);
                        HOperatorSet.GenContourPolygonXld(out hoTmpPlateStartEdge, hvTmpStartEdgeRowBegin.TupleConcat(hvTmpStartEdgeRowEnd),
                                                                                   hvTmpStartEdgeColBegin.TupleConcat(hvTmpStartEdgeColEnd));
                        hoTmpContour.Dispose();
                    }
                    else
                    {
                        hvTmpStartEdgeRowBegin = _hvStartEdgeRows[0];
                        hvTmpStartEdgeColBegin = _hvStartEdgeCols[0];
                        HOperatorSet.ProjectionPl(hvTmpStartEdgeRowBegin, hvTmpStartEdgeColBegin, hvPlateEdgeRowBegin, hvPlateEdgeColBegin, hvPlateEdgeRowEnd, hvPlateEdgeRowColEnd,
                                                  out hvTmpStartEdgeRowEnd, out hvTmpStartEdgeColEnd);
                        HOperatorSet.GenContourPolygonXld(out hoTmpPlateStartEdge, hvTmpStartEdgeRowBegin.TupleConcat(hvTmpStartEdgeRowEnd),
                                                                                   hvTmpStartEdgeColBegin.TupleConcat(hvTmpStartEdgeColEnd));
                    }
                    HOperatorSet.AngleLx(hvTmpStartEdgeRowBegin, hvTmpStartEdgeColBegin, hvTmpStartEdgeRowEnd, hvTmpStartEdgeColEnd, out hvTmpPlateStartEdgePhi);

                    // 扫描结束边缘采样点
                    HTuple hvHalfEndEdgeRows, hvHalfEndEdgeCols;
                    HObject hoTmpPlateEndEdge;
                    HTuple hvTmpEndEdgeRowBegin, hvTmpEndEdgeColBegin, hvTmpEndEdgeRowEnd, hvTmpEndEdgeColEnd;
                    HTuple hvTmpPlateEndEdgePhi;
                    HOperatorSet.TupleSelectRange(_hvEndEdgeRows, hvEndEdgePointBeginIdx, hvEndEdgePointEndIdx, out hvHalfEndEdgeRows);
                    HOperatorSet.TupleSelectRange(_hvEndEdgeCols, hvEndEdgePointBeginIdx, hvEndEdgePointEndIdx, out hvHalfEndEdgeCols);
                    if (hvEndEdgePointHalfNum > 1)
                    {
                        HObject hoTmpContour;

                        HOperatorSet.GenContourPolygonXld(out hoTmpContour, hvHalfEndEdgeRows, hvHalfEndEdgeCols);
                        HOperatorSet.FitLineContourXld(hoTmpContour, "tukey", -1, 0, 5, 2, out hvTmpEndEdgeRowBegin, out hvTmpEndEdgeColBegin, out hvTmpEndEdgeRowEnd, out hvTmpEndEdgeColEnd,
                                                       out hv_Nr, out hv_Nc, out hv_Dist);
                        HOperatorSet.GenContourPolygonXld(out hoTmpPlateEndEdge, hvTmpEndEdgeRowBegin.TupleConcat(hvTmpEndEdgeRowEnd),
                                                                                 hvTmpEndEdgeColBegin.TupleConcat(hvTmpEndEdgeColEnd));
                        hoTmpContour.Dispose();
                    }
                    else
                    {
                        hvTmpEndEdgeRowBegin = _hvEndEdgeRows[0];
                        hvTmpEndEdgeColBegin = _hvEndEdgeCols[0];
                        HOperatorSet.ProjectionPl(hvTmpEndEdgeRowBegin, hvTmpEndEdgeColBegin, hvPlateEdgeRowBegin, hvPlateEdgeColBegin, hvPlateEdgeRowEnd, hvPlateEdgeRowColEnd,
                                                  out hvTmpEndEdgeRowEnd, out hvTmpEndEdgeColEnd);
                        HOperatorSet.GenContourPolygonXld(out hoTmpPlateEndEdge, hvTmpEndEdgeRowBegin.TupleConcat(hvTmpEndEdgeRowEnd),
                                                                                 hvTmpEndEdgeColBegin.TupleConcat(hvTmpEndEdgeColEnd));
                    }
                    HOperatorSet.AngleLx(hvTmpEndEdgeRowBegin, hvTmpEndEdgeColBegin, hvTmpEndEdgeRowEnd, hvTmpEndEdgeColEnd, out hvTmpPlateEndEdgePhi);

                    // 比较修正前后的极片扫描起始边缘与参考线之间的角度大小
                    HTuple hvReferStartEdgeRowBegin, hvReferStartEdgeColBegin;
                    HTuple hvReferStartEdgeRowEnd, hvReferStartEdgeColEnd;
                    HTuple hvReferStartEdgeAngle1, hvReferStartEdgeAngle2;
                    if (_hvStartRowEdge.TupleLength() > 0)
                    {
                        hvReferStartEdgeRowBegin = _hvStartRowEdge;
                        hvReferStartEdgeColBegin = _hvStartColEdge;
                    }
                    else
                    {
                        hvReferStartEdgeRowBegin = _hvStartEdgeRows[0];
                        hvReferStartEdgeColBegin = _hvStartEdgeCols[0];
                    }
                    HOperatorSet.ProjectionPl(hvReferStartEdgeRowBegin, hvReferStartEdgeColBegin, hvPlateEdgeRowBegin, hvPlateEdgeColBegin,
                                              hvPlateEdgeRowEnd, hvPlateEdgeRowColEnd, out hvReferStartEdgeRowEnd, out hvReferStartEdgeColEnd);
                    HOperatorSet.AngleLl(hvReferStartEdgeRowBegin, hvReferStartEdgeColBegin, hvReferStartEdgeRowEnd, hvReferStartEdgeColEnd,
                                         _hvStartEdgeRowBegin, _hvStartEdgeColBegin, _hvStartEdgeRowEnd, _hvStartEdgeColEnd, out hvReferStartEdgeAngle1);
                    HOperatorSet.AngleLl(hvReferStartEdgeRowBegin, hvReferStartEdgeColBegin, hvReferStartEdgeRowEnd, hvReferStartEdgeColEnd,
                                         hvTmpStartEdgeRowBegin, hvTmpStartEdgeColBegin, hvTmpStartEdgeRowEnd, hvTmpStartEdgeColEnd, out hvReferStartEdgeAngle2);
                    if (hvReferStartEdgeAngle1.TupleAbs().D > hvReferStartEdgeAngle2.TupleAbs().D)
                    {
                        _hoFitStartEdge = new HObject(hoTmpPlateStartEdge);
                        _hvStartEdgeRowBegin = new HTuple(hvTmpStartEdgeRowBegin);
                        _hvStartEdgeColBegin = new HTuple(hvTmpStartEdgeColBegin);
                        _hvStartEdgeRowEnd = new HTuple(hvTmpStartEdgeRowEnd);
                        _hvStartEdgeColEnd = new HTuple(hvTmpStartEdgeColEnd);
                        _hvPlateStartEdgePhi = new HTuple(hvTmpPlateStartEdgePhi);
                    }

                    HTuple hvReferEndEdgeRowBegin, hvReferEndEdgeColBegin;
                    HTuple hvReferEndEdgeRowEnd, hvReferEndEdgeColEnd;
                    HTuple hvReferEndEdgeAngle1, hvReferEndEdgeAngle2;
                    if (_hvEndRowEdge.TupleLength() > 0)
                    {
                        hvReferEndEdgeRowBegin = _hvEndRowEdge;
                        hvReferEndEdgeColBegin = _hvEndColEdge;
                    }
                    else
                    {
                        hvReferEndEdgeRowBegin = _hvEndEdgeRows[0];
                        hvReferEndEdgeColBegin = _hvEndEdgeCols[0];
                    }
                    HOperatorSet.ProjectionPl(hvReferEndEdgeRowBegin, hvReferEndEdgeColBegin, hvPlateEdgeRowBegin, hvPlateEdgeColBegin,
                                              hvPlateEdgeRowEnd, hvPlateEdgeRowColEnd, out hvReferEndEdgeRowEnd, out hvReferEndEdgeColEnd);
                    HOperatorSet.AngleLl(hvReferEndEdgeRowBegin, hvReferEndEdgeColBegin, hvReferEndEdgeRowEnd, hvReferEndEdgeColEnd,
                                         _hvEndEdgeRowBegin, _hvEndEdgeColBegin, _hvEndEdgeRowEnd, _hvEndEdgeColEnd, out hvReferEndEdgeAngle1);
                    HOperatorSet.AngleLl(hvReferEndEdgeRowBegin, hvReferEndEdgeColBegin, hvReferEndEdgeRowEnd, hvReferEndEdgeColEnd,
                                         hvTmpEndEdgeRowBegin, hvTmpEndEdgeColBegin, hvTmpEndEdgeRowEnd, hvTmpEndEdgeColEnd, out hvReferEndEdgeAngle2);
                    if (hvReferEndEdgeAngle1.TupleAbs().D > hvReferEndEdgeAngle2.TupleAbs().D)
                    {
                        _hoFitEndEdge = new HObject(hoTmpPlateEndEdge);
                        _hvEndEdgeRowBegin = new HTuple(hvTmpEndEdgeRowBegin);
                        _hvEndEdgeColBegin = new HTuple(hvTmpEndEdgeColBegin);
                        _hvEndEdgeRowEnd = new HTuple(hvTmpEndEdgeRowEnd);
                        _hvEndEdgeColEnd = new HTuple(hvTmpEndEdgeColEnd);
                        _hvPlateEndEdgePhi = new HTuple(hvTmpPlateEndEdgePhi);
                    }
                }
                finally
                {
                    hoTmp?.Dispose();
                    hoPlateTopBottomEdgeContour?.Dispose(); 
                    hoFitPlateTopBottomEdge?.Dispose();

                    hvSamplingPointRows.Dispose(); hvSamplingPointCol.Dispose(); hvSamplingPointRow.Dispose(); 
                    hvTmpEdgeRow.Dispose(); hvTmpEdgeCol.Dispose(); hvAmplitudeNeg.Dispose(); hvDistanceNeg.Dispose();
                    hvPlateEdgeRowBegin.Dispose(); hvPlateEdgeColBegin.Dispose(); hvPlateEdgeRowEnd.Dispose();
                    hvPlateEdgeRowColEnd.Dispose(); hv_Nr.Dispose(); hv_Nc.Dispose(); hv_Dist.Dispose();
                    hvSampleNum.Dispose(); hvTmpBeginRow.Dispose(); hvTmpEndRow.Dispose();
                }
            }

            return 0;
        }



        /// <summary>
        /// 定位图片中极片区域的四个边缘与四个顶点
        /// </summary>
        protected virtual int LocationPlatRegionAndKeypoint()
        {
            using (var dh = new HDevDisposeHelper())
            {
                HObject? hoTmp = null;

                HObject? hoRegionDifference = null;

                HTuple hvDetRegW = (_measureParam.ScanWidth * 0.05) * 0.5;
                HTuple hvDetRegH = _measureParam.ScanHeight * 0.5;
                HTuple hvPlateCenterR = new HTuple();
                HTuple hvPlateCenterC = new HTuple();
                HTuple hvPlateRegionWidth = new HTuple();
                HTuple hvPlateRegionheight = new HTuple();
                HTuple hvStartEdgeMeasureHandle = new HTuple();
                HTuple hvEndEdgeMeasureHandle = new HTuple();
                HTuple hvStartEdgeMeasureHandleTmp = new HTuple();
                HTuple hvEndEdgeMeasureHandleTmp = new HTuple();
                HTuple hvAmplitudePos = new HTuple();
                HTuple hvDistancePos = new HTuple();

                HTuple hvRowBegin = new HTuple();
                HTuple hvColumnBegin = new HTuple();
                HTuple hvRowEnd = new HTuple();
                HTuple hvColumnEnd = new HTuple();
                HTuple hvMeasureNum = new HTuple();
                HTuple hvMeasureW = new HTuple();
                HTuple hvMeasureH = new HTuple();

                try
                {
                    if (_measureParam.PlatePart == 0)
                    {
                        hvPlateCenterR = _measureParam.ScanHeight * 0.5;
                        hvPlateCenterC = _measureParam.ScanWidth * 0.5;
                    }
                    else if (_measureParam.PlatePart == -1)
                    {
                        hvPlateCenterR = _measureParam.ScanHeight * 0.5;
                        hvPlateCenterC = hvDetRegW;
                    }
                    else
                    {
                        hvPlateCenterR = _measureParam.ScanHeight * 0.5;
                        hvPlateCenterC = _measureParam.ScanWidth - hvDetRegW - 1;
                    }


                    // 粗定位极片扫描的起始边缘
                    HOperatorSet.GenMeasureRectangle2(hvPlateCenterR, hvPlateCenterC, (new HTuple(-90)).TupleRad(), hvDetRegH, hvDetRegW,
                                                      _measureParam.ScanWidth, _measureParam.ScanHeight, "bilinear", out hvStartEdgeMeasureHandle);
                    HOperatorSet.MeasurePos(_hoHeightImage, hvStartEdgeMeasureHandle, _measureParam.HeightAmplitudeSigma, _measureParam.HeightAmplitudeThr,
                                            "positive", "first", out _hvStartRowEdge, out _hvStartColEdge, out hvAmplitudePos, out hvDistancePos);
                    HOperatorSet.CloseMeasure(hvStartEdgeMeasureHandle);

                    if(_hvStartRowEdge.TupleLength() <= 0 && _measureParam.PlatePart == 0)
                    {
                        HTuple hvPlateCenterR1 = _measureParam.ScanHeight * 0.5;
                        HTuple hvPlateCenterC1 = hvDetRegW;
                        HTuple hvPlateCenterR2 = _measureParam.ScanHeight * 0.5;
                        HTuple hvPlateCenterC2 = _measureParam.ScanWidth - hvDetRegW - 1;

                        HOperatorSet.GenMeasureRectangle2(hvPlateCenterR1, hvPlateCenterC1, (new HTuple(-90)).TupleRad(), hvDetRegH, hvDetRegW,
                                                      _measureParam.ScanWidth, _measureParam.ScanHeight, "bilinear", out hvStartEdgeMeasureHandleTmp);
                        HOperatorSet.MeasurePos(_hoHeightImage, hvStartEdgeMeasureHandleTmp, _measureParam.HeightAmplitudeSigma, _measureParam.HeightAmplitudeThr,
                                                "positive", "first", out HTuple hvStartRowEdge1, out HTuple hvStartColEdge1, out hvAmplitudePos, out hvDistancePos);
                        HOperatorSet.CloseMeasure(hvStartEdgeMeasureHandleTmp);

                        HOperatorSet.GenMeasureRectangle2(hvPlateCenterR2, hvPlateCenterC2, (new HTuple(-90)).TupleRad(), hvDetRegH, hvDetRegW,
                                                      _measureParam.ScanWidth, _measureParam.ScanHeight, "bilinear", out hvStartEdgeMeasureHandleTmp);
                        HOperatorSet.MeasurePos(_hoHeightImage, hvStartEdgeMeasureHandleTmp, _measureParam.HeightAmplitudeSigma, _measureParam.HeightAmplitudeThr,
                                                "positive", "first", out HTuple hvStartRowEdge2, out HTuple hvStartColEdge2, out hvAmplitudePos, out hvDistancePos);
                        HOperatorSet.CloseMeasure(hvStartEdgeMeasureHandleTmp);

                        if(hvStartRowEdge1.TupleLength() > 0 && hvStartRowEdge2.TupleLength() > 0)
                        {
                            HOperatorSet.TupleMean(hvStartRowEdge1.TupleConcat(hvStartRowEdge2), out _hvStartRowEdge);
                            HOperatorSet.TupleMean(hvStartColEdge1.TupleConcat(hvStartColEdge2), out _hvStartColEdge);
                        }
                    }

                    // 粗定位极片扫描的结束边缘
                    HOperatorSet.GenMeasureRectangle2(hvPlateCenterR, hvPlateCenterC, (new HTuple(90)).TupleRad(), hvDetRegH, hvDetRegW,
                                                      _measureParam.ScanWidth, _measureParam.ScanHeight, "bilinear", out hvEndEdgeMeasureHandle);
                    HOperatorSet.MeasurePos(_hoHeightImage, hvEndEdgeMeasureHandle, _measureParam.HeightAmplitudeSigma, _measureParam.HeightAmplitudeThr,
                                            "positive", "first", out _hvEndRowEdge, out _hvEndColEdge, out hvAmplitudePos, out hvDistancePos);
                    HOperatorSet.CloseMeasure(hvEndEdgeMeasureHandle);

                    if (_hvEndRowEdge.TupleLength() <= 0 && _measureParam.PlatePart == 0)
                    {
                        HTuple hvPlateCenterR1 = _measureParam.ScanHeight * 0.5;
                        HTuple hvPlateCenterC1 = hvDetRegW;
                        HTuple hvPlateCenterR2 = _measureParam.ScanHeight * 0.5;
                        HTuple hvPlateCenterC2 = _measureParam.ScanWidth - hvDetRegW - 1;

                        HOperatorSet.GenMeasureRectangle2(hvPlateCenterR1, hvPlateCenterC1, (new HTuple(90)).TupleRad(), hvDetRegH, hvDetRegW,
                                                      _measureParam.ScanWidth, _measureParam.ScanHeight, "bilinear", out hvEndEdgeMeasureHandleTmp);
                        HOperatorSet.MeasurePos(_hoHeightImage, hvEndEdgeMeasureHandleTmp, _measureParam.HeightAmplitudeSigma, _measureParam.HeightAmplitudeThr,
                                                "positive", "first", out HTuple hvEndRowEdge1, out HTuple hvEndColEdge1, out hvAmplitudePos, out hvDistancePos);
                        HOperatorSet.CloseMeasure(hvEndEdgeMeasureHandleTmp);

                        HOperatorSet.GenMeasureRectangle2(hvPlateCenterR2, hvPlateCenterC2, (new HTuple(90)).TupleRad(), hvDetRegH, hvDetRegW,
                                                      _measureParam.ScanWidth, _measureParam.ScanHeight, "bilinear", out hvEndEdgeMeasureHandleTmp);
                        HOperatorSet.MeasurePos(_hoHeightImage, hvEndEdgeMeasureHandleTmp, _measureParam.HeightAmplitudeSigma, _measureParam.HeightAmplitudeThr,
                                                "positive", "first", out HTuple hvEndRowEdge2, out HTuple hvEndColEdge2, out hvAmplitudePos, out hvDistancePos);
                        HOperatorSet.CloseMeasure(hvEndEdgeMeasureHandleTmp);

                        if (hvEndRowEdge1.TupleLength() > 0 && hvEndRowEdge2.TupleLength() > 0)
                        {
                            HOperatorSet.TupleMean(hvEndRowEdge1.TupleConcat(hvEndRowEdge2), out _hvEndRowEdge);
                            HOperatorSet.TupleMean(hvEndColEdge1.TupleConcat(hvEndColEdge2), out _hvEndColEdge);
                        }
                    }


                    // 边缘卡尺对极片扫描起始边缘精定位
                    if (_hvStartRowEdge.TupleLength() > 0)
                    {
                        hvRowBegin = _hvStartRowEdge;
                        hvColumnBegin = 0;
                        hvRowEnd = _hvStartRowEdge;
                        hvColumnEnd = _measureParam.ScanWidth;
                        hvMeasureNum = _measureParam.PlateSEEdgeSamplingMum;
                        hvMeasureW = (_measureParam.ScanWidth * 0.25) / hvMeasureNum;
                        hvMeasureH = 200;

                        //LineCalipers(_hoHeightImage, hvRowBegin, hvColumnBegin, hvRowEnd, hvColumnEnd, hvMeasureW, hvMeasureH,
                        //                   _measureParam.HeightAmplitudeSigma, _measureParam.HeightAmplitudeThr, "positive", hvMeasureNum, "first", 0.7,
                        //                   out hoTmp, out _hvStartEdgeRows, out _hvStartEdgeCols);
                        LineCalipers(_hoHeightImage, hvRowEnd, hvColumnEnd, hvRowBegin, hvColumnBegin, hvMeasureW, hvMeasureH,
                                           _measureParam.HeightAmplitudeSigma, _measureParam.HeightAmplitudeThr, "negative", hvMeasureNum, "first", 0.7,
                                           out hoTmp, out _hvStartEdgeRows, out _hvStartEdgeCols);
                        ReplaceHobject(ref _hoFitStartEdge, ref hoTmp);

                        HTuple hvTmpNum = new HTuple();
                        HTuple hvNr = new HTuple();
                        HTuple hvNc = new HTuple();
                        HTuple hvDist = new HTuple();
                        HOperatorSet.CountObj(_hoFitStartEdge, out hvTmpNum);
                        if (hvTmpNum.I > 0)
                        {
                            HOperatorSet.FitLineContourXld(_hoFitStartEdge, "tukey", -1, 0, 5, 2, out _hvStartEdgeRowBegin, out _hvStartEdgeColBegin, out _hvStartEdgeRowEnd,
                                                           out _hvStartEdgeColEnd, out hvNr, out hvNc, out hvDist);

                            if (_hvStartEdgeColEnd < _hvStartEdgeColBegin)
                            {
                                (_hvStartEdgeRowBegin, _hvStartEdgeRowEnd) = (_hvStartEdgeRowEnd, _hvStartEdgeRowBegin);
                                (_hvStartEdgeColBegin, _hvStartEdgeColEnd) = (_hvStartEdgeColEnd, _hvStartEdgeColBegin);
                            }

                            HOperatorSet.GenContourPolygonXld(out hoTmp, _hvStartEdgeRowBegin.TupleConcat(_hvStartEdgeRowEnd),
                                                                                   _hvStartEdgeColBegin.TupleConcat(_hvStartEdgeColEnd));
                            ReplaceHobject(ref _hoFitStartEdge, ref hoTmp);
                            HOperatorSet.AngleLx(_hvStartEdgeRowBegin, _hvStartEdgeColBegin, _hvStartEdgeRowEnd, _hvStartEdgeColEnd, out _hvPlateStartEdgePhi);
                        }
                        else
                        {
                            HOperatorSet.GenContourPolygonXld(out hoTmp, (new HTuple(_hvStartRowEdge)).TupleConcat(_hvStartRowEdge),
                                                                                   (new HTuple(0)).TupleConcat(_measureParam.ScanWidth));
                            ReplaceHobject(ref _hoFitStartEdge, ref hoTmp);
                            _hvStartEdgeRowBegin = 0;
                            _hvStartEdgeColBegin = 0;
                            _hvStartEdgeRowEnd = 0;
                            _hvStartEdgeColEnd = new HTuple(_measureParam.ScanWidth);
                            _hvPlateStartEdgePhi = (new HTuple(0)).TupleRad();
                            _hvStartEdgeRows = new HTuple();
                            _hvStartEdgeRows = _hvStartEdgeRows.TupleConcat(_hvStartEdgeRowBegin, _hvStartEdgeRowEnd);
                            _hvStartEdgeCols = new HTuple();
                            _hvStartEdgeCols = _hvStartEdgeCols.TupleConcat(_hvStartEdgeColBegin, _hvStartEdgeColEnd);
                        }

                        hvTmpNum.Dispose();
                        hvNr.Dispose();
                        hvNc.Dispose();
                        hvDist.Dispose();
                    }
                    else
                    {
                        HOperatorSet.GenContourPolygonXld(out hoTmp, (new HTuple(0)).TupleConcat(0), (new HTuple(0)).TupleConcat(_measureParam.ScanWidth));
                        ReplaceHobject(ref _hoFitStartEdge, ref hoTmp);
                        _hvStartEdgeRowBegin = 0;
                        _hvStartEdgeColBegin = 0;
                        _hvStartEdgeRowEnd = 0;
                        _hvStartEdgeColEnd = new HTuple(_measureParam.ScanWidth);
                        _hvPlateStartEdgePhi = (new HTuple(0)).TupleRad();
                        _hvStartEdgeRows = new HTuple();
                        _hvStartEdgeRows = _hvStartEdgeRows.TupleConcat(_hvStartEdgeRowBegin, _hvStartEdgeRowEnd);
                        _hvStartEdgeCols = new HTuple();
                        _hvStartEdgeCols = _hvStartEdgeCols.TupleConcat(_hvStartEdgeColBegin, _hvStartEdgeColEnd);
                    }

                    // 边缘卡尺对极片扫描结束边缘精定位
                    if (_hvEndRowEdge.TupleLength() > 0)
                    {
                        hvRowBegin = _hvEndRowEdge;
                        hvColumnBegin = _hvEndColEdge - 0.5 * _measureParam.ScanWidth;
                        hvRowEnd = _hvEndRowEdge;
                        hvColumnEnd = _hvEndColEdge + 0.5 * _measureParam.ScanWidth;
                        hvMeasureNum = _measureParam.PlateSEEdgeSamplingMum;
                        hvMeasureW = (_measureParam.ScanWidth * 0.25) / hvMeasureNum;
                        hvMeasureH = 200;

                        LineCalipers(_hoHeightImage, hvRowBegin, hvColumnBegin, hvRowEnd, hvColumnEnd, hvMeasureW, hvMeasureH,
                                           _measureParam.HeightAmplitudeSigma, _measureParam.HeightAmplitudeThr, "negative", hvMeasureNum, "first", 0.7,
                                           out hoTmp, out _hvEndEdgeRows, out _hvEndEdgeCols);
                        ReplaceHobject(ref _hoFitEndEdge, ref hoTmp);

                        HTuple hvTmpNum = new HTuple();
                        HTuple hvNr = new HTuple();
                        HTuple hvNc = new HTuple();
                        HTuple hvDist = new HTuple();
                        HOperatorSet.CountObj(_hoFitEndEdge, out hvTmpNum);

                        if (hvTmpNum.I > 0)
                        {
                            HOperatorSet.FitLineContourXld(_hoFitEndEdge, "tukey", -1, 0, 5, 2, out _hvEndEdgeRowBegin, out _hvEndEdgeColBegin, out _hvEndEdgeRowEnd,
                                                           out _hvEndEdgeColEnd, out hvNr, out hvNc, out hvDist);

                            if (_hvStartEdgeColEnd < _hvStartEdgeColBegin)
                            {
                                (_hvStartEdgeRowBegin, _hvStartEdgeRowEnd) = (_hvStartEdgeRowEnd, _hvStartEdgeRowBegin);
                                (_hvStartEdgeColBegin, _hvStartEdgeColEnd) = (_hvStartEdgeColEnd, _hvStartEdgeColBegin);
                            }

                            HOperatorSet.GenContourPolygonXld(out hoTmp, _hvEndEdgeRowBegin.TupleConcat(_hvEndEdgeRowEnd),
                                                                                   _hvEndEdgeColBegin.TupleConcat(_hvEndEdgeColEnd));
                            ReplaceHobject(ref _hoFitEndEdge, ref hoTmp);
                            HOperatorSet.AngleLx(_hvEndEdgeRowBegin, _hvEndEdgeColBegin, _hvEndEdgeRowEnd, _hvEndEdgeColEnd, out _hvPlateEndEdgePhi);
                        }
                        else
                        {
                            HOperatorSet.GenContourPolygonXld(out hoTmp, (new HTuple(_hvEndRowEdge)).TupleConcat(_hvEndRowEdge),
                                                              (new HTuple(0)).TupleConcat(_measureParam.ScanWidth));
                            ReplaceHobject(ref _hoFitEndEdge, ref hoTmp);
                            _hvEndEdgeRowBegin = new HTuple(_measureParam.ScanHeight);
                            _hvEndEdgeColBegin = 0;
                            _hvEndEdgeRowEnd = new HTuple(_measureParam.ScanHeight);
                            _hvEndEdgeColEnd = new HTuple(_measureParam.ScanWidth);
                            _hvPlateEndEdgePhi = (new HTuple(0)).TupleRad();
                            _hvEndEdgeRows = new HTuple();
                            _hvEndEdgeRows = _hvEndEdgeRows.TupleConcat(_hvEndEdgeRowBegin, _hvEndEdgeRowEnd);
                            _hvEndEdgeCols = new HTuple();
                            _hvEndEdgeCols = _hvEndEdgeCols.TupleConcat(_hvEndEdgeColBegin, _hvEndEdgeColEnd);
                        }

                        hvTmpNum.Dispose();
                        hvNr.Dispose();
                        hvNc.Dispose();
                        hvDist.Dispose();
                    }
                    else
                    {
                        HOperatorSet.GenContourPolygonXld(out hoTmp, (new HTuple(_measureParam.ScanHeight)).TupleConcat(_measureParam.ScanHeight),
                                                         (new HTuple(0)).TupleConcat(_measureParam.ScanWidth));
                        ReplaceHobject(ref _hoFitEndEdge, ref hoTmp);
                        _hvEndEdgeRowBegin = new HTuple(_measureParam.ScanHeight);
                        _hvEndEdgeColBegin = 0;
                        _hvEndEdgeRowEnd = new HTuple(_measureParam.ScanHeight);
                        _hvEndEdgeColEnd = new HTuple(_measureParam.ScanWidth);
                        _hvPlateEndEdgePhi = (new HTuple(0)).TupleRad();
                        _hvEndEdgeRows = new HTuple();
                        _hvEndEdgeRows = _hvEndEdgeRows.TupleConcat(_hvEndEdgeRowBegin, _hvEndEdgeRowEnd);
                        _hvEndEdgeCols = new HTuple();
                        _hvEndEdgeCols = _hvEndEdgeCols.TupleConcat(_hvEndEdgeColBegin, _hvEndEdgeColEnd);
                    }


                    // 判断是否为极片的顶部或底部,拟合顶部或底部边缘,调整对极片边缘的拟合
                    _hvPlateTopBottomEdgeSamplePointRows = new HTuple();
                    _hvPlateTopBottomEdgeSamplePointCols = new HTuple();
                    if (_measureParam.PlatePart != 0)
                    {

                        FitPlateTopBottomEdgeFixSEEdge();

                    }
                    else
                    {
                        HTuple hvSampleNum = _measureParam.PlateTDEdgeSamplingMum;
                        HTuple hvTmpBeginRow = (_hvStartEdgeRowBegin + _hvStartEdgeRowEnd) * 0.5;
                        HTuple hvTmpEndRow = (_hvEndEdgeRowBegin + _hvEndEdgeRowEnd) * 0.5;

                        HTuple hvSamplingPointRows, hvSamplingPointCols;
                        // HTuple hvPlateTopBottomEdgeSampleLineRows, hvPlateTopBottomEdgeSampleLineCols;

                        HOperatorSet.TupleGenSequence(hvTmpBeginRow, hvTmpEndRow, (((hvTmpEndRow - hvTmpBeginRow) / hvSampleNum)).TupleInt(), out hvSamplingPointRows);
                        HOperatorSet.TupleGenConst(new HTuple(hvSamplingPointRows.TupleLength()), 0.5 * _measureParam.ScanWidth, out hvSamplingPointCols);
                        HOperatorSet.TupleSelectRange(hvSamplingPointRows, 1, hvSampleNum - 1, out _hvPlateTopBottomEdgeSamplePointRows);
                        HOperatorSet.TupleSelectRange(hvSamplingPointCols, 1, hvSampleNum - 1, out _hvPlateTopBottomEdgeSamplePointCols);

                        HOperatorSet.GenContourPolygonXld(out hoTmp, new HTuple((_measureParam.ScanHeight * 0.5) - 10, (_measureParam.ScanHeight * 0.5) + 10),
                                                          new HTuple(0, 0));
                        ReplaceHobject(ref _hoFitBottomEdge, ref hoTmp);

                        HOperatorSet.GenContourPolygonXld(out hoTmp, new HTuple((_measureParam.ScanHeight * 0.5) - 10, (_measureParam.ScanHeight * 0.5) + 10),
                                                          new HTuple(_measureParam.ScanWidth, _measureParam.ScanWidth));
                        ReplaceHobject(ref _hoFitTopEdge, ref hoTmp);
                    }

                    // 计算当前图片中极片边缘线的交点及极片所在区域多边形
                    HTuple hvPlateBottomEdgeRows, hvPlateBottomEdgeCols;
                    HTuple hvPlateStartEdgeRows, hvPlateStartEdgeCols;
                    HTuple hvPlateTopEdgeRows, hvPlateTopEdgeCols;
                    HTuple hvPlateEndEdgeRows, hvPlateEndEdgeCols;
                    HOperatorSet.GetContourXld(_hoFitBottomEdge, out hvPlateBottomEdgeRows, out hvPlateBottomEdgeCols);
                    HOperatorSet.GetContourXld(_hoFitStartEdge, out hvPlateStartEdgeRows, out hvPlateStartEdgeCols);
                    HOperatorSet.GetContourXld(_hoFitTopEdge, out hvPlateTopEdgeRows, out hvPlateTopEdgeCols);
                    HOperatorSet.GetContourXld(_hoFitEndEdge, out hvPlateEndEdgeRows, out hvPlateEndEdgeCols);

                    HTuple hvIsOverlapping;
                    //HTuple hvLeftTopRow, hvLeftTopColumn, hvRightTopRow, hvRightTopColumn;
                    //HTuple hvRightDownRow, hvRightDownColumn, hvLeftDownRow, hvLeftDownColumn;
                    HOperatorSet.IntersectionLines(hvPlateBottomEdgeRows.TupleSelect(0), hvPlateBottomEdgeCols.TupleSelect(0),
                                                   hvPlateBottomEdgeRows.TupleSelect(1), hvPlateBottomEdgeCols.TupleSelect(1),
                                                   hvPlateStartEdgeRows.TupleSelect(0), hvPlateStartEdgeCols.TupleSelect(0),
                                                   hvPlateStartEdgeRows.TupleSelect(1), hvPlateStartEdgeCols.TupleSelect(1),
                                                   out _hvLeftTopRow, out _hvLeftTopColumn, out hvIsOverlapping);
                    HOperatorSet.IntersectionLines(hvPlateStartEdgeRows.TupleSelect(0), hvPlateStartEdgeCols.TupleSelect(0),
                                                   hvPlateStartEdgeRows.TupleSelect(1), hvPlateStartEdgeCols.TupleSelect(1),
                                                   hvPlateTopEdgeRows.TupleSelect(0), hvPlateTopEdgeCols.TupleSelect(0),
                                                   hvPlateTopEdgeRows.TupleSelect(1), hvPlateTopEdgeCols.TupleSelect(1),
                                                   out _hvRightTopRow, out _hvRightTopColumn, out hvIsOverlapping);
                    HOperatorSet.IntersectionLines(hvPlateTopEdgeRows.TupleSelect(0), hvPlateTopEdgeCols.TupleSelect(0),
                                                   hvPlateTopEdgeRows.TupleSelect(1), hvPlateTopEdgeCols.TupleSelect(1),
                                                   hvPlateEndEdgeRows.TupleSelect(0), hvPlateEndEdgeCols.TupleSelect(0),
                                                   hvPlateEndEdgeRows.TupleSelect(1), hvPlateEndEdgeCols.TupleSelect(1),
                                                   out _hvRightDownRow, out _hvRightDownColumn, out hvIsOverlapping);
                    HOperatorSet.IntersectionLines(hvPlateEndEdgeRows.TupleSelect(0), hvPlateEndEdgeCols.TupleSelect(0),
                                                   hvPlateEndEdgeRows.TupleSelect(1), hvPlateEndEdgeCols.TupleSelect(1),
                                                   hvPlateBottomEdgeRows.TupleSelect(0), hvPlateBottomEdgeCols.TupleSelect(0),
                                                   hvPlateBottomEdgeRows.TupleSelect(1), hvPlateBottomEdgeCols.TupleSelect(1),
                                                   out _hvLeftDownRow, out _hvLeftDownColumn, out hvIsOverlapping);
                    HOperatorSet.GenRegionPolygonFilled(out hoTmp, new HTuple(_hvLeftTopRow, _hvRightTopRow, _hvRightDownRow, _hvLeftDownRow),
                                                        new HTuple(_hvLeftTopColumn, _hvRightTopColumn, _hvRightDownColumn, _hvLeftDownColumn));
                    ReplaceHobject(ref _hoPlateRegion, ref hoTmp);

                    HOperatorSet.Intersection(_hoPlateRegion, _hoValidRegion, out hoTmp);
                    ReplaceHobject(ref _hoPlateRegion, ref hoTmp);
                    HOperatorSet.FillUp(_hoPlateRegion, out hoTmp);
                    ReplaceHobject(ref _hoPlateRegion, ref hoTmp);

                    // 计算极片偏转角
                    if (_measureParam.PlatePart == -1)
                    {
                        HTuple hvTmpRowBegin, hvTmpColBegin, hvTmpRowEnd, hvTmpColEnd;
                        HTuple hvNr, hvNc, hvDist;
                        HTuple hvPlatePhi;
                        HOperatorSet.FitLineContourXld(_hoFitTopEdge, "tukey", -1, 0, 5, 2, out hvTmpRowBegin, out hvTmpColBegin, out hvTmpRowEnd, out hvTmpColEnd,
                                                       out hvNr, out hvNc, out hvDist);
                        HOperatorSet.AngleLx(hvTmpRowBegin, hvTmpColBegin, hvTmpRowEnd, hvTmpColEnd, out hvPlatePhi);
                        _hvPlatePhi = new HTuple(((new HTuple(90)).TupleRad()) + hvPlatePhi);
                    }
                    else if (_measureParam.PlatePart == 1)
                    {
                        HTuple hvTmpRowBegin, hvTmpColBegin, hvTmpRowEnd, hvTmpColEnd;
                        HTuple hvNr, hvNc, hvDist;
                        HTuple hvPlatePhi;
                        HOperatorSet.FitLineContourXld(_hoFitBottomEdge, "tukey", -1, 0, 5, 2, out hvTmpRowBegin, out hvTmpColBegin, out hvTmpRowEnd, out hvTmpColEnd,
                                                       out hvNr, out hvNc, out hvDist);
                        HOperatorSet.AngleLx(hvTmpRowBegin, hvTmpColBegin, hvTmpRowEnd, hvTmpColEnd, out hvPlatePhi);
                        _hvPlatePhi = new HTuple(((new HTuple(90)).TupleRad()) + hvPlatePhi);
                    }
                    else
                    {
                        _hvPlatePhi = (_hvPlateStartEdgePhi + _hvPlateEndEdgePhi) * 0.5;
                    }

                    // 计算深度图阈值范围
                    HTuple hvPlateRange, hvBackGroundRange;
                    HTuple hvPlateHeightMinValue, hvPlateHeightMaxValue;
                    HTuple hvBackGroundHeightMinValue, hvBackGroundHeightMaxValue;
                    HTuple hvArea;
                    HOperatorSet.GenEmptyObj(out hoRegionDifference);
                    HOperatorSet.MinMaxGray(_hoPlateRegion, _hoHeightImage, 0, out hvPlateHeightMinValue, out hvPlateHeightMaxValue, out hvPlateRange);
                    HOperatorSet.Difference(_hoValidRegion, _hoPlateRegion, out hoTmp);
                    ReplaceHobject(ref hoRegionDifference, ref hoTmp);
                    HOperatorSet.AreaCenter(hoRegionDifference, out hvArea, out HTuple hvTmpRow, out HTuple hvTmpCol);
                    if (hvArea.D > 0)
                    {
                        HOperatorSet.MinMaxGray(hoRegionDifference, _hoHeightImage, 0, out hvBackGroundHeightMinValue, out hvBackGroundHeightMaxValue, out hvBackGroundRange);
                        _hvDepthMapMinValue = hvBackGroundHeightMinValue + hvBackGroundRange * 0.2;
                    }
                    else
                    {
                        _hvDepthMapMinValue = hvPlateHeightMinValue;
                    }
                    _hvDepthMapMaxValue = hvPlateHeightMaxValue;

                }
                finally
                {
                    hoTmp?.Dispose();
                    hoRegionDifference?.Dispose();

                    hvDetRegW.Dispose();
                    hvDetRegH.Dispose();
                    hvPlateCenterR.Dispose();
                    hvPlateCenterC.Dispose();
                    hvPlateRegionWidth.Dispose();
                    hvPlateRegionheight.Dispose();
                    hvStartEdgeMeasureHandle.Dispose();
                    hvEndEdgeMeasureHandle.Dispose();
                    hvStartEdgeMeasureHandleTmp.Dispose();
                    hvEndEdgeMeasureHandleTmp.Dispose();
                    hvAmplitudePos.Dispose();
                    hvDistancePos.Dispose();

                    hvRowBegin.Dispose();
                    hvColumnBegin.Dispose();
                    hvRowEnd.Dispose();
                    hvColumnEnd.Dispose();
                    hvMeasureNum.Dispose();
                    hvMeasureW.Dispose();
                    hvMeasureH.Dispose();
                }
            }

            return 0;
        }


        /// <summary>
        /// 测量过程(测试)
        /// </summary>
        /// <param name="grayDate">输入的灰度图数据</param>
        /// <param name="heightData">输入深度图数据</param>
        /// <param name="param">测量参数</param>
        /// <returns>result</returns>
        public abstract KCJC0_MeasureResult Process(List<float[]> grayDate, List<float[]> heightData, KCJC0_MeasureParam param);


        /// <summary>
        /// 绘制结果
        /// </summary>
        public abstract Mat CvDrawResult(KCJC0_MeasureResult measureResult, bool showGuides = false);


        public enum ImageType
        {
            Gray,    // 灰度图
            Depth,   // 深度图
            RGB,     // 三通道RGB图
            BGR      // 三通道BGR图
        }


        // 自定义线
        public class Line
        {
            public OpenCvSharp.Point StartPoint { get; }
            public OpenCvSharp.Point EndPoint { get; }
            public double Radian { get; }
            public double Degree { get; }


            public Line(OpenCvSharp.Point2d startPoint, OpenCvSharp.Point2d endPoint)
            {
                StartPoint = new OpenCvSharp.Point((int)startPoint.X, (int)startPoint.Y);
                EndPoint = new OpenCvSharp.Point((int)endPoint.X, (int)endPoint.Y);

                Radian = Math.Atan2(endPoint.Y - startPoint.Y, endPoint.X - startPoint.X);
                Degree = Radian * (180 / Math.PI);
            }

        }

        // 自定义旋转矩形
        public class RotatedRect
        {
            // 矩形中心点坐标
            public OpenCvSharp.Point2d Center { get; }
            // 矩形旋转角度
            public double Phi { get; }
            // 矩形宽
            public double Width { get; }
            // 矩形高
            public double Height { get; }
            // 矩形顶点坐标
            public List<OpenCvSharp.Point> Corners { get; }

            public int CornersNum { get; }


            public RotatedRect(OpenCvSharp.Point2d center, double phi, double width, double height)
            {
                using (var dh = new HDevDisposeHelper())
                {
                    HObject? hoRect = null;
                    HTuple? hvRow = null;
                    HTuple? hvCol = null;

                    try
                    {
                        Center = center;
                        Phi = phi;
                        Width = width;
                        Height = height;

                        HOperatorSet.GenEmptyObj(out hoRect);
                        HOperatorSet.GenRectangle2ContourXld(out hoRect, center.Y, center.X, phi, width, height);
                        HOperatorSet.GetContourXld(hoRect, out hvRow, out hvCol);

                        CornersNum = hvRow.Length;
                        Corners = new List<OpenCvSharp.Point>();
                        for (int i = 0; i < CornersNum; i++)
                        {
                            Corners.Add(new OpenCvSharp.Point((int)hvCol.TupleSelect(i).D, (int)hvRow.TupleSelect(i).D));
                        }
                    }
                    finally
                    {
                        hoRect?.Dispose();
                        hvRow?.Dispose();
                        hvCol?.Dispose();
                    }
                }
                

            }

        }


        public class Polygon
        {
            // 轮廓中心点坐标
            public OpenCvSharp.Point2d Center { get; }

            // 轮廓半径
            public double Radius { get; }

            // 轮廓点集
            public OpenCvSharp.Point[][] Contours { get; }

            public Polygon(HObject region)
            {
                if (region != null && region.IsInitialized() && region.CountObj() > 0)
                {
                    // 根据轮廓拟合圆
                    HOperatorSet.GenContourRegionXld(region, out HObject hoRegionContour, "border");
                    HOperatorSet.FitCircleContourXld(hoRegionContour, "geotukey", -1, 0, 0, 3, 2,
                                                    out HTuple hvRow, out HTuple hvColumn, out HTuple hvRadius,
                                                    out HTuple hvStartPhi, out HTuple hvEndPhi, out HTuple hvPointOrder);
                    Center = new Point2d(hvColumn.D, hvRow.D);
                    Radius = hvRadius.D;

                    // 提取轮廓点集
                    HOperatorSet.GenPolygonsXld(hoRegionContour, out HObject hoRegionPolygon, "ramer", 2);
                    HOperatorSet.GetPolygonXld(hoRegionPolygon, out HTuple hvPolygonRows, out HTuple hvPolygonCols,
                                               out HTuple hvTmpLength, out HTuple hvTmpPhi);

                    List<OpenCvSharp.Point[]> tmpContours = new List<OpenCvSharp.Point[]>();
                    OpenCvSharp.Point[] tmpContour = new OpenCvSharp.Point[hvPolygonRows.Length];
                    for (int i = 0; i < hvPolygonRows.Length; i++)
                    {
                        tmpContour[i] = new OpenCvSharp.Point((int)hvPolygonCols.TupleSelect(i).D, (int)hvPolygonRows.TupleSelect(i).D);
                    }
                    tmpContours.Add(tmpContour);
                    Contours = tmpContours.ToArray();

                    hoRegionContour.Dispose();
                    hoRegionPolygon.Dispose();
                }
                else
                {
                    Center = new Point2d(-1, -1);
                    Radius = -1;
                    Contours = new OpenCvSharp.Point[][] { };
                }
            }


            public Polygon()
            {
                Center = new Point2d(-1, -1);
                Radius = -1;
                Contours = new OpenCvSharp.Point[][] { };
            }


        }



        /// <summary>
        /// 测量算法配置参数
        /// </summary>
        [Serializable]
        public class KCJC0_MeasureParam
        {
            /// <summary>
            /// 配方管理显示用检测方式，枚举值必须和 AlgorithmType 的 0/1/2 保持一致。
            /// </summary>
            public enum KCJC0_DetectMode
            {
                刻槽检测 = 0,
                压花检测 = 1,
                刻槽和压花 = 2
            }

            // 刻槽检测还是压花检测
            private int _algorithmType = 0;    // 0: 刻槽检测 1: 压花检测 2：刻槽+压花

            // 相机参数
            private double _intervalX = 2.9;   //X方向的像素当量(>0)
            private double _intervalY = 5;     //Y方向的像素当量(>0)
            private double _intervalZ = 1;     //Z方向的像素当量(μm)

            private int _scanWidth = 2048;     //相机扫描宽度(>0)
            private int _scanHeight = 7600;    //相机扫描行数(>0)

            private bool _isFlip = false;       //图片是否需要翻转
            private int _platePart = -1;        //扫描极片的部位(-1:极片顶部, 0:极片中间, 1:极片底部)

            // 测量参数
            private double _standardEtchingLineWidthReal = 500;          // 刻蚀线标准宽(μm)
            private double _standardEtchingPointDistReal = 1050;           // 刻蚀线标准间距(μm)
            private double _standardEtchingLineDepthReal = 15;            // 刻蚀线标准深度(μm)
            private double _grayAmplitudeThr = 50.0;      //灰度图中定位槽边缘的梯度阈值(>=0)
            private double _grayAmplitudeSigma = 10.0;     //灰度图中定位槽边缘的平滑系数(>=0.4 && <=32)
            private double _heightAmplitudeThr = 100.0;  //深度图中定位极片边缘的梯度阈值(>=0)
            private double _heightAmplitudeSigma = 5.0;   //深度图中定位极片边缘的平滑系数(>=0.4 && <=32)

            private int _plateSEEdgeSamplingMum = 20;      // 拟合极片扫描起始边缘采样点数量(>0 && <=100)
            private int _plateTDEdgeSamplingMum = 20;      // 拟合极片顶部底部边缘采样点数量(>0 && <=100)

            private int _samplePointNum = 6;                 // 刻蚀线测量分区数(>1 && <=30)
            private int _etchingLineNumMax = 5;              // 分区刻蚀线最大数量(>1)

            private int _plateEdgeMaskSize = 50;             // 极片边缘屏蔽区域（用于屏蔽极片边缘毛刺、缺口等异常的干扰(>=0)
            private int _imageEdgeMaskSize = 40;             // 图片边缘屏蔽区域（深度图在图片边缘会存在失真区域(>=0)

            private double _EtchingLineMeasureMaskRate = 0.3;   // 刻蚀线测量屏蔽区比率(>=0 && <=1)

            private double _plateLeftEdgeMaskSizeReal = 1450;   // 头部去除区(μm)(>=0)
            private double _plateRightEdgeMaskSizeReal = 1450;  // 尾部去除区(μm)(>=0)

            private double _depthMapSampleDownSizeW = 1;           // 深度图宽度下采样率(>=1)
            private double _depthMapSampleDownSizeH = 1;           // 深度图高度下采样率(>=1)

            private int _grayImageRotateAngle = 0;             // 灰度图显示旋转角度(0、90、180、-90、-180)

            // 刻点测量参数
            private double _standardPointRadius = 1050;    // 刻蚀点标准半径(μm)
            private double _standardPointHeight = 80;      // 刻蚀点标准高度(μm)

            private int _smallKernelSize = 30;             // 刻蚀点提取小滤波核尺寸(>=3 && <bigKernelSize)(废弃)
            private int _bigKernelSize = 300;              // 刻蚀点提取大滤波核尺寸(>smallKernelSize && <=1000)(废弃)
            private int _dynThresh = 5;                    // 刻蚀点提取动态阈值(>-255 && <=255))(废弃)
            private int _regionFilterKernelSize = 50;      // 刻点区域平滑元素半径(>=1 && <=511)(废弃)
            private double _etchingPointThresh = 30;       // 刻蚀点分割阈值(微米)(废弃)


            /// <summary>
            /// 刻槽检测or刻点检测(0: 刻槽检测 1: 刻点检测 2: 刻槽+刻点检测)
            /// </summary>
            public int AlgorithmType
            {
                get { return _algorithmType; }
                set { _algorithmType = value; }
            }

            /// <summary>
            /// 配方专用检测方式，保持配方下拉显示，实际仍写入 AlgorithmType。
            /// </summary>
            [RecipeParam("AlgorithmType", "检测方式")]
            private KCJC0_DetectMode AlgorithmTypeRecipe
            {
                get { return (KCJC0_DetectMode)_algorithmType; }
                set { _algorithmType = (int)value; }
            }

            /// <summary>
            /// 传感器参数：X方向点间隔
            /// </summary>
            public double IntervalX
            {
                get { return _intervalX; }
                set
                {
                    if (value > 0)
                    {
                        _intervalX = value;
                    }
                    else
                    {
                        _intervalX = 1;
                    }
                }
            }

            /// <summary>
            /// 传感器参数：Y方向点间隔
            /// </summary>
            [RecipeParam("CollectIntervalY", "采集Y方向像素当量")]
            public double IntervalY
            {
                get { return _intervalY; }
                set
                {
                    if (value > 0)
                    {
                        _intervalY = value;
                    }
                    else
                    {
                        _intervalY = 1;
                    }
                }
            }

            /// <summary>
            /// 传感器参数：Z方向点间隔
            /// </summary>
            public double IntervalZ
            {
                get { return _intervalZ; }
                set
                {
                    if (value > 0)
                    {
                        _intervalZ = value;
                    }
                    else
                    {
                        _intervalZ = 1;
                    }
                }
            }

            /// <summary>
            /// 废弃
            /// </summary>
            public int ScanWidth
            {
                get { return _scanWidth; }
                set
                {
                    if (value > 0)
                    {
                        _scanWidth = value;
                    }
                    else
                    {
                        _scanWidth = 1;
                    }
                }
            }

            /// <summary>
            /// 废弃
            /// </summary>
            public int ScanHeight
            {
                get { return _scanHeight; }
                set
                {
                    if (value > 0)
                    {
                        _scanHeight = value;
                    }
                    else
                    {
                        _scanHeight = 1;
                    }
                }
            }

            /// <summary>
            /// 传感器参数：图片是否需要翻转(需要翻转：ture; 不需要翻转：false)
            /// </summary>
            public bool IsFlip
            {
                get { return _isFlip; }
                set { _isFlip = value; }
            }

            /// <summary>
            /// 废弃
            /// </summary>
            public int PlatePart
            {
                get { return _platePart; }
                set { _platePart = value; }
            }

            /// <summary>
            /// 刻蚀线标准宽(μm)
            /// </summary>
            public double StandardEtchingLineWidthReal
            {
                get { return _standardEtchingLineWidthReal; }
                set { _standardEtchingLineWidthReal = value; }
            }

            /// <summary>
            /// 刻蚀线标准间距(μm)
            /// </summary>
            public double StandardEtchingPointDistReal
            {
                get { return _standardEtchingPointDistReal; }
                set { _standardEtchingPointDistReal = value; }
            }

            /// <summary>
            /// 刻蚀线标准深度(μm)
            /// </summary>
            public double StandardEtchingLineDepthReal
            {
                get { return _standardEtchingLineDepthReal; }
                set { _standardEtchingLineDepthReal = value; }
            }

            /// <summary>
            /// 灰度图边缘梯度阈值
            /// </summary>
            public double GrayAmplitudeThr
            {
                get { return _grayAmplitudeThr; }
                set { _grayAmplitudeThr = value; }
            }

            /// <summary>
            /// 灰度图边缘边缘平滑系数
            /// </summary>
            public double GrayAmplitudeSigma
            {
                get { return _grayAmplitudeSigma; }
                set { _grayAmplitudeSigma = value; }
            }

            /// <summary>
            /// 高度图边缘梯度阈值
            /// </summary>
            public double HeightAmplitudeThr
            {
                get { return _heightAmplitudeThr; }
                set { _heightAmplitudeThr = value; }
            }

            /// <summary>
            /// 高度图边缘边缘平滑系数
            /// </summary>
            public double HeightAmplitudeSigma
            {
                get { return _heightAmplitudeSigma; }
                set { _heightAmplitudeSigma = value; }
            }

            /// <summary>
            /// 极片扫描始末边缘检测采样点数
            /// </summary>
            public int PlateSEEdgeSamplingMum
            {
                get { return _plateSEEdgeSamplingMum; }
                set { _plateSEEdgeSamplingMum = value; }
            }

            /// <summary>
            /// 极片顶部、底部边缘检测采样点数
            /// </summary>
            public int PlateTDEdgeSamplingMum
            {
                get { return _plateTDEdgeSamplingMum; }
                set { _plateTDEdgeSamplingMum = value; }
            }

            /// <summary>
            /// 刻蚀线测量分区数
            /// </summary>
            public int SamplePointNum
            {
                get { return _samplePointNum; }
                set
                {
                    if (value > 0)
                    {
                        _samplePointNum = value;
                    }
                    else
                    {
                        _samplePointNum = 1;
                    }
                }
            }

            /// <summary>
            /// 分区内最大刻蚀线条数
            /// </summary>
            public int EtchingLineNumMax
            {
                get { return _etchingLineNumMax; }
                set
                {
                    if (value > 0)
                    {
                        _etchingLineNumMax = value;
                    }
                    else
                    {
                        _etchingLineNumMax = 1;
                    }
                }
            }

            /// <summary>
            /// 极片边缘屏蔽区大小(像素)
            /// </summary>
            public int PlateEdgeMaskSize
            {
                get { return _plateEdgeMaskSize; }
                set { _plateEdgeMaskSize = value; }
            }

            /// <summary>
            /// 图片边缘屏蔽区大小(像素)
            /// </summary>
            public int ImageEdgeMaskSize
            {
                get { return _imageEdgeMaskSize; }
                set { _imageEdgeMaskSize = value; }
            }

            /// <summary>
            /// 刻蚀线测量屏蔽区比率(数值越大屏蔽区域越大)(>=0 && <=1)
            /// </summary>
            public double EtchingLineMeasureMaskRate
            {
                get { return _EtchingLineMeasureMaskRate; }
                set { _EtchingLineMeasureMaskRate = value; }
            }


            /// <summary>
            /// 极片头部去除区大小(微米)
            /// </summary>
            public double PlateLeftEdgeMaskSizeReal
            {
                get { return _plateLeftEdgeMaskSizeReal; }
                set { _plateLeftEdgeMaskSizeReal = value; }
            }

            /// <summary>
            /// 极片尾部去除区大小(微米)
            /// </summary>
            public double PlateRightEdgeMaskSizeReal
            {
                get { return _plateRightEdgeMaskSizeReal; }
                set { _plateRightEdgeMaskSizeReal = value; }
            }

            /// <summary>
            /// 深度图宽度下采样率
            /// </summary>
            public double DepthMapSampleDownSizeW
            {
                get { return _depthMapSampleDownSizeW; }
                set { _depthMapSampleDownSizeW = value; }
            }

            /// <summary>
            /// 深度图高度下采样率
            /// </summary>
            public double DepthMapSampleDownSizeH
            {
                get { return _depthMapSampleDownSizeH; }
                set { _depthMapSampleDownSizeH = value; }
            }

            /// <summary>
            /// 灰度图显示旋转角度
            /// </summary>
            public int GrayImageRotateAngle
            {
                get { return _grayImageRotateAngle; }
                set { _grayImageRotateAngle = value; }
            }

            /// <summary>
            /// 刻蚀点标准半径(μm)
            /// </summary>
            public double StandardPointRadius
            {
                get { return _standardPointRadius; }
                set { _standardPointRadius = value; }
            }

            /// <summary>
            /// 刻蚀点标准高度(μm)
            /// </summary>
            public double StandardPointHeight
            {
                get { return _standardPointHeight; }
                set { _standardPointHeight = value; }
            }

            /// <summary>
            /// (内部计算)
            /// </summary>
            public double StandardRadiusPixel
            {
                get { return _standardPointRadius / (_intervalX > 0 ? _intervalX : 1.0); }
            }

            /// <summary>
            /// (内部计算)
            /// </summary>
            public double StandardHeightPixel
            {
                get { return _standardPointHeight / (_intervalZ > 0 ? _intervalZ : 1.0); }
            }

            /// <summary>
            /// (内部计算)小滤波核尺寸
            /// </summary>
            public int AutoSmallKernelSize
            {
                get { return Math.Max(3, (int)Math.Round(StandardRadiusPixel * 0.3, MidpointRounding.AwayFromZero)); }
            }

            /// <summary>
            /// (内部计算)大滤波核尺寸
            /// </summary>
            public int AutoBigKernelSize
            {
                get { return Math.Max(AutoSmallKernelSize + 1, (int)Math.Round(StandardRadiusPixel * 2.0, MidpointRounding.AwayFromZero)); }
            }

            /// <summary>
            /// (内部计算)动态阈值
            /// </summary>
            public double AutoDynThresh
            {
                get { return StandardHeightPixel * 0.1; }
            }

            /// <summary>
            /// 刻蚀点提取小核滤波尺寸(废弃)
            /// </summary>
            public int SmallKernelSize
            {
                get { return _smallKernelSize; }
                set { _smallKernelSize = value; }
            }

            /// <summary>
            /// 刻蚀点提取大核滤波尺寸(废弃)
            /// </summary>
            public int BigKernelSize
            {
                get { return _bigKernelSize; }
                set { _bigKernelSize = value; }
            }

            /// <summary>
            /// 刻蚀点提取动态阈值(废弃)
            /// </summary>
            public int DynThresh
            {
                get { return _dynThresh; }
                set { _dynThresh = value; }
            }

            /// <summary>
            /// 刻点区域平滑元素半径(废弃)
            /// </summary>
            public int RegionFilterKernelSize
            {
                get { return _regionFilterKernelSize; }
                set { _regionFilterKernelSize = value; }
            }

            /// <summary>
            /// 刻蚀点分割阈值(微米)(废弃)
            /// </summary>
            public double EtchingPointThresh
            {
                get { return _etchingPointThresh; }
                set { _etchingPointThresh = value; }
            }

        }


        /// <summary>
        /// 极片分区测量值
        /// </summary>
        public class KCJC0_PartitionResult
        {
            /// <summary>
            /// 当前分区定位出刻蚀线的数量
            /// </summary>
            public int EtchingLineNum { get; set; }


            /*像素单位*/
            /// <summary>
            /// 测量区域的轮廓
            /// </summary>
            public RotatedRect MeasureRegion { get; set; }

            /// <summary>
            /// 测量区域的表面高度曲线X坐标
            /// </summary> 
            public double[] HeightCurveX { get; set; }

            /// <summary>
            /// 测量区域的表面高度曲线Y坐标
            /// </summary> 
            public double[] HeightCurveY { get; set; }

            /// <summary>
            /// 刻蚀线负边缘线
            /// </summary>
            public List<Line> EtchingLineNeg { get; set; }

            /// <summary>
            /// 刻蚀线正边缘线
            /// </summary>
            public List<Line> EtchingLinePos { get; set; }

            /// <summary>
            /// 刻蚀线负边缘点坐标
            /// </summary>
            public List<OpenCvSharp.Point2d> EtchingLineNegPoint { get; set; }

            /// <summary>
            /// 刻蚀线正边缘点坐标
            /// </summary>
            public List<OpenCvSharp.Point2d> EtchingLinePosPoint { get; set; }

            /// <summary>
            /// 刻蚀线中心(刻蚀点)
            /// </summary>
            public List<OpenCvSharp.Point2d> EtchingPoint { get; set; }

            /// <summary>
            /// 刻蚀线负边缘点在表面高度曲线投影
            /// </summary>
            public double[] EtchingLineNegProj { get; set; }

            /// <summary>
            /// 刻蚀线负边缘点在表面高度曲线投影
            /// </summary>
            public double[] EtchingLinePosProj { get; set; }

            /// <summary>
            /// 刻蚀线中心(刻蚀点)在表面高度曲线投影
            /// </summary>
            public double[] EtchingPointProj { get; set; }

            /// <summary>
            /// 刻蚀线宽度
            /// </summary>
            public double[] EtchingLineWidthList { get; set; }

            /// <summary>
            /// 刻蚀点间距
            /// </summary>
            public double[] EtchingPointDistList { get; set; }

            /// <summary>
            /// 平均刻蚀线宽度
            /// </summary>
            public double EtchingLineWidthMean { get; set; }

            /// <summary>
            /// 平均刻蚀点间距
            /// </summary>
            public double EtchingPointDistMean { get; set; }

            /// <summary>
            /// 分区的刻蚀区顶部与极片顶部缘间距
            /// </summary>
            public double EtchingRegionTopGap { get; set; }

            /// <summary>
            /// 分区的刻蚀区底部与极片底部缘间距
            /// </summary>
            public double EtchingRegionBottomGap { get; set; }



            /*变换矩阵*/
            /// <summary>
            /// 原图到分区的变换矩阵
            /// </summary>
            public double[] HomMat2D { get; set; }

            /// <summary>
            /// 分区到原图的变换矩阵
            /// </summary>
            public double[] HomMat2DInvert { get; set; }


            /*物理单位*/
            /// <summary>
            /// 刻蚀线实际宽度
            /// </summary>
            public double[] EtchingLineWidthRealList { get; set; }

            /// <summary>
            /// 刻蚀点实际间距
            /// </summary>
            public double[] EtchingPointDistRealList { get; set; }

            /// <summary>
            /// 平均刻蚀线实际宽度
            /// </summary>
            public double EtchingLineWidthRealMean { get; set; }

            /// <summary>
            /// 平均刻蚀点实际间距
            /// </summary>
            public double EtchingPointDistRealMean { get; set; }

            /// <summary>
            /// 刻蚀线实际深度
            /// </summary>
            public double[] EtchingLineDepthList { get; set; }

            /// <summary>
            /// 刻蚀线实际深度均值
            /// </summary>
            public double EtchingLineDepthMean { get; set; }

            /// <summary>
            /// 分区的刻蚀区顶部与极片顶部缘实际间距
            /// </summary>
            public double EtchingRegionTopGapReal { get; set; }

            /// <summary>
            /// 分区的刻蚀区底部与极片底部缘实际间距
            /// </summary>
            public double EtchingRegionBottomGapReal { get; set; }

            public KCJC0_PartitionResult()
            {
                HeightCurveX = new double[] { };
                HeightCurveY = new double[] { };
                EtchingLineNegProj = new double[] { };
                EtchingLinePosProj = new double[] { };
                EtchingPointProj = new double[] { };
                EtchingLineWidthList = new double[] { };
                EtchingPointDistList = new double[] { };
                HomMat2D = new double[] { };
                HomMat2DInvert = new double[] { };
                EtchingLineWidthRealList = new double[] { };
                EtchingPointDistRealList = new double[] { };
                EtchingLineDepthList = new double[] { };

                EtchingLineNeg = new List<Line>();
                EtchingLinePos = new List<Line>();
                EtchingLineNegPoint = new List<OpenCvSharp.Point2d>();
                EtchingLinePosPoint = new List<OpenCvSharp.Point2d>();
                EtchingPoint = new List<OpenCvSharp.Point2d>();

                EtchingRegionTopGap = -1;
                EtchingRegionBottomGap = -1;

                EtchingRegionTopGapReal = -1;
                EtchingRegionBottomGapReal = -1;
            }

        }


        /// <summary>
        /// 刻点测量值
        /// </summary>
        public class KCJC0_ConvexConcaveResult
        {
            /// <summary>
            /// 凸包或凹坑区域
            /// </summary>
            public string RegionType { get; set; }

            /// <summary>
            /// 动态阈值提取到的核心区域
            /// </summary>
            public Polygon CorePolygon { get; set; }

            /// <summary>
            /// 拟合出来的凸包凹坑区域
            /// </summary>
            public Polygon FitPolygon { get; set; }

            /// <summary>
            /// 凸包凸坑高度差
            /// </summary>
            public double HeightDiff { get; set; }

            /// <summary>
            /// 最高(最低)点X坐标
            /// </summary>
            public double MeasurePointX { get; set; }

            /// <summary>
            /// 最高(最低)点Y坐标
            /// </summary>
            public double MeasurePointY { get; set; }

            /// <summary>
            /// 半径
            /// </summary>
            public double Radius { get; set; }


            public KCJC0_ConvexConcaveResult()
            {
                RegionType = "";
                CorePolygon = new Polygon();
                FitPolygon = new Polygon();
                HeightDiff = Single.PositiveInfinity;

                MeasurePointX = -1;
                MeasurePointY = -1;
                Radius = -1;
            }

        }


        /// <summary>
        /// 测量结果
        /// </summary>
        public class KCJC0_MeasureResult
        {
            /*common*/
            /// <summary>
            /// 拟合出的极片上边缘线
            /// </summary>
            public Line FitTopEdgeLine { get; set; }

            /// <summary>
            /// 拟合出的极片下边缘线
            /// </summary>
            public Line FitBottomEdgeLine { get; set; }

            /// <summary>
            /// 拟合出的极片起始扫描的边缘线
            /// </summary>
            public Line FitStartEdgeLine { get; set; }

            /// <summary>
            /// 拟合出的极片结束扫描的边缘线
            /// </summary>
            public Line FitEndEdgeLine { get; set; }

            /// <summary>
            /// 深度图
            /// </summary>
            public float[][] DepthMap { get; set; }

            /// <summary>
            /// 深度图显示阈值下限
            /// </summary>
            public double DepthMapMinValue { get; set; }

            /// <summary>
            /// 深度图显示阈值下限
            /// </summary>
            public double DepthMapMaxValue { get; set; }

            /// <summary>
            /// 图片宽度缩放因子
            /// </summary>
            public double ImageScaleW { get; set; }

            /// <summary>
            /// 图片高度缩放因子
            /// </summary>
            public double ImageScaleH { get; set; }

            /// <summary>
            /// 测量结果是否正常
            /// </summary>
            public bool IsOK { get; set; }

            /*刻槽*/
            /// <summary>
            /// 拟合出的刻蚀区上边缘线
            /// </summary>
            public Line FitEtchingRegionTopEdgeLine { get; set; }

            /// <summary>
            /// 拟合出的刻蚀区下边缘线
            /// </summary>
            public Line FitEtchingRegionBottomEdgeLine { get; set; }

            /// <summary>
            /// 拟合出的刻蚀区起始扫描的边缘线
            /// </summary>
            public Line FitEtchingRegionStartEdgeLine { get; set; }

            /// <summary>
            /// 拟合出的刻蚀区结束扫描的边缘线
            /// </summary>
            public Line FitEtchingRegionEndEdgeLine { get; set; }

            /// <summary>
            /// 所有分区测量结果
            /// </summary>
            public List<KCJC0_PartitionResult> PartitionResults { get; set; }


            // 刻蚀线在各分区的测量结果
            // 槽宽
            /// <summary>
            /// 各条刻蚀线宽度在各分区间的均值
            /// </summary>
            public double[] EtchingLineWidthRealMeanList { get; set; }
            /// <summary>
            /// 各条刻蚀线宽度在各分区间的最大值
            /// </summary>
            public double[] EtchingLineWidthRealMaxList { get; set; }
            /// <summary>
            /// 各条刻蚀线宽度在各分区间的最小值
            /// </summary>
            public double[] EtchingLineWidthRealMinList { get; set; }

            // 槽深
            /// <summary>
            /// 各条刻蚀线槽深在各分区间的均值
            /// </summary>
            public double[] EtchingLineDepthMeanList { get; set; }
            /// <summary>
            /// 各条刻蚀线槽深在各分区间的最大值
            /// </summary>
            public double[] EtchingLineDepthMaxList { get; set; }
            /// <summary>
            /// 各条刻蚀线槽深在各分区间的最小值
            /// </summary>
            public double[] EtchingLineDepthMinList { get; set; }

            // 槽宽间距
            /// <summary>
            /// 各条刻蚀线槽间距在各分区间的均值
            /// </summary>
            public double[] EtchingPointDistRealMeanList { get; set; }
            /// <summary>
            /// 各条刻蚀线槽间距在各分区间的最大值
            /// </summary>
            public double[] EtchingPointDistRealMaxList { get; set; }
            /// <summary>
            /// 各条刻蚀线槽间距在各分区间的最小值
            /// </summary>
            public double[] EtchingPointDistRealMinList { get; set; }

            /// <summary>
            /// 第一个分区的每条刻蚀线与极片起始扫描边缘间距
            /// </summary>
            public double[] EtchingRegionLeftGapList { get; set; }
            /// <summary>
            /// 最后一个分区的每条刻蚀线与极片结束扫描边缘间距
            /// </summary>
            public double[] EtchingRegionRightGapList { get; set; }

            // 全局
            /// <summary>
            /// 全局刻蚀区与极片起始扫描边缘间距
            /// </summary>
            public double GlobalEtchingRegionLeftGap { get; set; }

            /// <summary>
            /// 全局刻蚀区与极片结束扫描边缘间距
            /// </summary>
            public double GlobalEtchingRegionRightGap { get; set; }

            /// <summary>
            /// 全局刻蚀区顶部与极片顶部缘间距
            /// </summary>
            public double GlobalEtchingRegionTopGap { get; set; }

            /// <summary>
            /// 全局刻蚀区底部与极片底部缘间距
            /// </summary>
            public double GlobalEtchingRegionBottomGap { get; set; }

            /// <summary>
            /// 全局槽宽平均值
            /// </summary>
            public double GlobalEtchingLineWidthRealMean { get; set; }

            /// <summary>
            /// 槽宽分区最大值
            /// </summary>
            public double GlobalEtchingLineWidthRealMax { get; set; }

            /// <summary>
            /// 槽宽分区最小值
            /// </summary>
            public double GlobalEtchingLineWidthRealMin { get; set; }

            /// <summary>
            /// 槽宽分区极差
            /// </summary>
            public double GlobalEtchingLineWidthRealRange { get; set; }

            /// <summary>
            /// 槽深分区平均值
            /// </summary>
            public double GlobalEtchingLineDepthMean { get; set; }

            /// <summary>
            /// 槽深分区最大值
            /// </summary>
            public double GlobalEtchingLineDepthMax { get; set; }

            /// <summary>
            /// 槽深分区最小值
            /// </summary>
            public double GlobalEtchingLineDepthMin { get; set; }

            /// <summary>
            /// 槽深分区极差
            /// </summary>
            public double GlobalEtchingLineDepthRange { get; set; }

            /// <summary>
            /// 刻蚀区与极片起始扫描边缘实际间距
            /// </summary>
            public double EtchingRegionLeftGapReal { get; set; }

            /// <summary>
            /// 刻蚀区与极片结束扫描边缘实际间距
            /// </summary>
            public double EtchingRegionRightGapReal { get; set; }

            /// <summary>
            /// 刻蚀区顶部与极片顶部缘实际间距
            /// </summary>
            public double EtchingRegionTopGapReal { get; set; }

            /// <summary>
            /// 刻蚀区底部与极片底部缘实际间距
            /// </summary>
            public double EtchingRegionBottomGapReal { get; set; }

            /// <summary>
            /// 各分区刻蚀区顶部与极片顶部缘实际间距的均值
            /// </summary>
            public double EtchingRegionTopGapRealMean { get; set; }

            /// <summary>
            /// 各分区刻蚀区顶部与极片顶部缘实际间距的最大值
            /// </summary>
            public double EtchingRegionTopGapRealMax { get; set; }

            /// <summary>
            /// 各分区刻蚀区顶部与极片顶部缘实际间距的最小值
            /// </summary>
            public double EtchingRegionTopGapRealMin { get; set; }

            /// <summary>
            /// 各分区刻蚀区底部与极片底部缘实际间距的均值
            /// </summary>
            public double EtchingRegionBottomGapRealMean { get; set; }

            /// <summary>
            /// 各分区刻蚀区底部与极片底部缘实际间距的最大值
            /// </summary>
            public double EtchingRegionBottomGapRealMax { get; set; }

            /// <summary>
            /// 各分区刻蚀区底部与极片底部缘实际间距的最小值
            /// </summary>
            public double EtchingRegionBottomGapRealMin { get; set; }

            /// <summary>
            /// 第一个分区的每条刻蚀线与极片起始扫描边缘实际间距
            /// </summary>
            public double[] EtchingRegionLeftGapRealList { get; set; }

            /// <summary>
            /// 最后一个分区的每条刻蚀线与极片结束扫描边缘实际间距
            /// </summary>
            public double[] EtchingRegionRightGapRealList { get; set; }

            /// <summary>
            /// 极片刻蚀线与扫描方向夹角
            /// </summary>
            public double EtchingLineAngle { get; set; }


            /*刻点*/
            /// <summary>
            /// 各个凸包的测量结果
            /// </summary>
            public List<KCJC0_ConvexConcaveResult> ConvexResultsList { get; set; }

            /// <summary>
            /// 各个凹坑的测量结果
            /// </summary>
            public List<KCJC0_ConvexConcaveResult> ConcaveResultsList { get; set; }


            public KCJC0_MeasureResult()
            {
                FitTopEdgeLine = new Line(new OpenCvSharp.Point2d(0, 0), new OpenCvSharp.Point2d(0, 0));
                FitBottomEdgeLine = new Line(new OpenCvSharp.Point2d(0, 0), new OpenCvSharp.Point2d(0, 0));
                FitStartEdgeLine = new Line(new OpenCvSharp.Point2d(0, 0), new OpenCvSharp.Point2d(0, 0));
                FitEndEdgeLine = new Line(new OpenCvSharp.Point2d(0, 0), new OpenCvSharp.Point2d(0, 0));

                DepthMapMinValue = Single.NegativeInfinity;
                DepthMapMaxValue = Single.PositiveInfinity;

                ImageScaleW = 1;
                ImageScaleH = 1;

                FitEtchingRegionTopEdgeLine = new Line(new OpenCvSharp.Point2d(0, 0), new OpenCvSharp.Point2d(0, 0));
                FitEtchingRegionBottomEdgeLine = new Line(new OpenCvSharp.Point2d(0, 0), new OpenCvSharp.Point2d(0, 0));
                FitEtchingRegionStartEdgeLine = new Line(new OpenCvSharp.Point2d(0, 0), new OpenCvSharp.Point2d(0, 0));
                FitEtchingRegionEndEdgeLine = new Line(new OpenCvSharp.Point2d(0, 0), new OpenCvSharp.Point2d(0, 0));
                PartitionResults = new List<KCJC0_PartitionResult>();

                ConvexResultsList = new List<KCJC0_ConvexConcaveResult>();
                ConcaveResultsList = new List<KCJC0_ConvexConcaveResult>();

                GlobalEtchingRegionLeftGap = -1;
                GlobalEtchingRegionRightGap = -1;
                GlobalEtchingRegionTopGap = -1;
                GlobalEtchingRegionBottomGap = -1;

                GlobalEtchingLineWidthRealMean = -1;
                GlobalEtchingLineWidthRealMax = -1;
                GlobalEtchingLineWidthRealMin = -1;
                GlobalEtchingLineWidthRealRange = -1;

                GlobalEtchingLineDepthMean = -1;
                GlobalEtchingLineDepthMax = -1;
                GlobalEtchingLineDepthMin = -1;
                GlobalEtchingLineDepthRange = -1;

                EtchingRegionLeftGapReal = -1;
                EtchingRegionRightGapReal = -1;
                EtchingRegionTopGapReal = -1;
                EtchingRegionBottomGapReal = -1;

                EtchingRegionTopGapRealMean = -1;
                EtchingRegionTopGapRealMax = -1;
                EtchingRegionTopGapRealMin = -1;
                EtchingRegionBottomGapRealMean = -1;
                EtchingRegionBottomGapRealMax = -1;
                EtchingRegionBottomGapRealMin = -1;

                EtchingLineAngle = Single.NegativeInfinity;

                EtchingRegionLeftGapRealList = new double[] { };
                EtchingRegionRightGapRealList = new double[] { };

                EtchingLineWidthRealMeanList = new double[] { };
                EtchingLineWidthRealMaxList = new double[] { };
                EtchingLineWidthRealMinList = new double[] { };

                EtchingLineDepthMeanList = new double[] { };
                EtchingLineDepthMaxList = new double[] { };
                EtchingLineDepthMinList = new double[] { };

                EtchingPointDistRealMeanList = new double[] { };
                EtchingPointDistRealMaxList = new double[] { };
                EtchingPointDistRealMinList = new double[] { };

                EtchingRegionLeftGapList = new double[] { };
                EtchingRegionRightGapList = new double[] { };

                DepthMap = new float[][] { };

                IsOK = true;
            }

            /// <summary>
            /// 清除数据
            /// </summary>
            public void ClearData()
            {
                DepthMap = Array.Empty<float[]>();

                PartitionResults?.Clear();
                PartitionResults = new List<KCJC0_PartitionResult>();

                ConvexResultsList?.Clear();
                ConvexResultsList = new List<KCJC0_ConvexConcaveResult>();

                ConcaveResultsList?.Clear();
                ConcaveResultsList = new List<KCJC0_ConvexConcaveResult>();

                EtchingLineWidthRealMeanList = Array.Empty<double>();
                EtchingLineWidthRealMaxList = Array.Empty<double>();
                EtchingLineWidthRealMinList = Array.Empty<double>();

                EtchingLineDepthMeanList = Array.Empty<double>();
                EtchingLineDepthMaxList = Array.Empty<double>();
                EtchingLineDepthMinList = Array.Empty<double>();

                EtchingPointDistRealMeanList = Array.Empty<double>();
                EtchingPointDistRealMaxList = Array.Empty<double>();
                EtchingPointDistRealMinList = Array.Empty<double>();

                EtchingRegionLeftGapList = Array.Empty<double>();
                EtchingRegionRightGapList = Array.Empty<double>();
                EtchingRegionLeftGapRealList = Array.Empty<double>();
                EtchingRegionRightGapRealList = Array.Empty<double>();
            }

        }


    }
}
