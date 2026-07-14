using HalconDotNet;
using ReeYin_V.Core.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Custom.KCJC.Models.ALGO
{
    public class LinearSpectralSplicing_Algorithm
    {
        private LinearSpectralSplicing_MeasureParam _measureParam;

        private HTuple _hvScaleX = 1;
        private HTuple _hvScaleY = 1;

        private PitchCalibMode _pitchCalibMode = PitchCalibMode.Fitting;
        private bool _isPitchCalib = false;
        private double _pitchSlope = 0;

        private float[] _depthBase = new float[0];

        private List<ImageData> _imageData = new List<ImageData>();

        private HObject _hoTileGrayImage = new HObject();
        private HObject _hoTileHeightImage = new HObject();
        private HObject _hoPlaneRegion = new HObject();

        private LinearSpectralSplicing_MeasureResult _measureResult = new LinearSpectralSplicing_MeasureResult();

        private bool _disposed = false;

        public LinearSpectralSplicing_Algorithm(LinearSpectralSplicing_MeasureParam param)
        {
            _measureParam = param;

            bool fastModel = false;
            if (fastModel)
            {
                if (_measureParam.IntervalX > _measureParam.IntervalY)
                {
                    _hvScaleX = 1;
                    _hvScaleY = _measureParam.IntervalY / _measureParam.IntervalX;
                }
                else
                {
                    _hvScaleX = _measureParam.IntervalX / _measureParam.IntervalY;
                    _hvScaleY = 1;
                }
            }
            else
            {
                if (_measureParam.IntervalX < _measureParam.IntervalY)
                {
                    _hvScaleX = 1;
                    _hvScaleY = _measureParam.IntervalY / _measureParam.IntervalX;
                }
                else
                {
                    _hvScaleX = _measureParam.IntervalX / _measureParam.IntervalY;
                    _hvScaleY = 1;
                }
            }
            _pitchCalibMode = _measureParam.PitchCalibMode;
            _isPitchCalib = _measureParam.IsPitchCalib;
            _pitchSlope = _measureParam.PitchSlope;

        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_imageData != null)
                {
                    foreach (var data in _imageData)
                    {
                        data?.Dispose();
                    }
                    _imageData.Clear();
                }

                _hoTileGrayImage?.Dispose();
                _hoTileHeightImage?.Dispose();
                _hoPlaneRegion?.Dispose();

                _depthBase = new float[0];
            }

            _disposed = true;

            GC.SuppressFinalize(this);

        }

        ~LinearSpectralSplicing_Algorithm()
        {
            Dispose();
        }


        public enum ImageType
        {
            Gray,    // 灰度图
            Depth,   // 深度图
            RGB,     // 三通道RGB图
            BGR      // 三通道BGR图
        }


        static void ReplaceHobject(ref HObject? target, ref HObject? source)
        {
            if (!ReferenceEquals(target, source))
                target?.Dispose();

            target = source;
            source = null;
        }



        /// <summary>
        /// 将List<float[]>数组转换为halcon图片对象
        /// </summary>
        /// <param name="data">输入的List<float[]>数组</param>
        /// <param name="imageType">输入图片数据类型</param>
        /// <param name="hoObject">输出的halcon图片对象</param>
        /// <param name="usePitchCalib">是否使用俯仰角校准</param>
        /// <returns>状态标志</returns>
        public int ConvertListToHObject(List<float[]> data, ImageType imageType, out HObject hoObject, bool usePitchCalib = false)
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
                    float[] imageData;

                    if (usePitchCalib && _isPitchCalib && Math.Abs(_pitchSlope) > 1e-10)
                    {
                        imageData = new float[height * width];
                        double intervalX = _measureParam.IntervalX;

                        for (int row = 0; row < height; row++)
                        {
                            float[] rowData = data[row];
                            int rowOffset = row * width;

                            for (int col = 0; col < width; col++)
                            {
                                double compensationBase = 0;
                                double compensation;
                                if (_pitchCalibMode == PitchCalibMode.Hybrid)
                                {
                                    compensation = col * intervalX * _pitchSlope;
                                    compensationBase = _depthBase[col] - compensation;
                                }
                                else if (_pitchCalibMode == PitchCalibMode.Fitting)
                                {
                                    compensation = col * intervalX * _pitchSlope;
                                    compensationBase = 0;
                                }
                                else
                                {
                                    compensation = _depthBase[col];
                                    compensationBase = 0;
                                }

                                imageData[rowOffset + col] = (float)(rowData[col] - compensation - compensationBase);
                            }
                        }
                    }
                    else
                    {
                        imageData = data.SelectMany(row => row).ToArray();
                    }

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
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                HOperatorSet.GenEmptyObj(out hoObject);
                return -1;
            }
            finally
            {
                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }
        }


        /// <summary>
        /// 获取高度图有效值区域
        /// </summary>
        public void GetDepthValidMask(HObject hoHeightImage, out HObject hoValidMask)
        {
            using (var dh = new HDevDisposeHelper())
            {
                HObject? hoIrregularRegion = null;
                HObject? hoReducedImage = null;

                try
                {
                    HOperatorSet.Threshold(hoHeightImage, out hoValidMask, _measureParam.MinDepth, _measureParam.MaxDepth);
                    HOperatorSet.Threshold(hoHeightImage, out hoIrregularRegion, 8888880, 8888880);
                    HOperatorSet.Difference(hoValidMask, hoIrregularRegion, out hoValidMask);
                }
                finally
                {
                    hoIrregularRegion?.Dispose();
                    hoReducedImage?.Dispose();
                }
            }
        }


        /// <summary>
        /// 图片前处理
        /// </summary>
        public int PreProcess(List<float[]> grayDate, List<float[]> heightData, out HObject? hoGrayImage,
                              out HObject? hoHeightImage, out HObject? hoValidMask)
        {
            using (var dh = new HDevDisposeHelper())
            {

                HObject? hoTmp;

                try
                {
                    int statusGrayDate, statusHeightData;
                    statusGrayDate = ConvertListToHObject(grayDate, ImageType.Gray, out hoGrayImage);
                    statusHeightData = ConvertListToHObject(heightData, ImageType.Depth, out hoHeightImage);

                    int ret = 0;
                    if (statusGrayDate == 0 && statusHeightData == 0)
                    {
                        if (_measureParam.IsFlip)
                        {
                            HOperatorSet.MirrorImage(hoGrayImage, out hoTmp, "row");
                            ReplaceHobject(ref hoGrayImage, ref hoTmp);

                            HOperatorSet.MirrorImage(hoHeightImage, out hoTmp, "row");
                            ReplaceHobject(ref hoHeightImage, ref hoTmp);
                        }
                        _measureParam.IntervalZ = (_measureParam.IntervalZ * 1000) / (_measureParam.IntervalZ * 10);
                        _measureParam.IntervalZ *= 0.001;

                        HOperatorSet.ZoomImageFactor(hoGrayImage, out hoGrayImage, _measureParam.ScaleFactor, _measureParam.ScaleFactor, "bilinear");
                        HOperatorSet.ZoomImageFactor(hoHeightImage, out hoHeightImage, _measureParam.ScaleFactor, _measureParam.ScaleFactor, "nearest_neighbor");

                        _measureParam.IntervalX = (_measureParam.IntervalX * 1000) / _measureParam.ScaleFactor;
                        _measureParam.IntervalY = (_measureParam.IntervalY * 1000) / _measureParam.ScaleFactor;
                        _measureParam.IntervalX *= 0.001;
                        _measureParam.IntervalY *= 0.001;

                        GetDepthValidMask(hoHeightImage!, out hoValidMask);
                        //HOperatorSet.ReduceDomain(hoHeightImage, hoValidMask, out hoHeightImage);

                        HOperatorSet.GetImageSize(hoHeightImage, out HTuple hv_Height, out HTuple hv_Width);

                        ImageData imageData = new ImageData();
                        imageData.hoGrayImage = hoGrayImage!.Clone();
                        imageData.hoHeightImage = hoHeightImage!.Clone();
                        imageData.hoValidMask = hoValidMask!.Clone();
                        imageData.hvIntervalX = _measureParam.IntervalX;
                        imageData.hvIntervalY = _measureParam.IntervalY;
                        imageData.hvIntervalZ = _measureParam.IntervalZ;
                        imageData.OffsetX = _measureParam.OffsetX / _measureParam.IntervalX;
                        imageData.OffsetY = _measureParam.OffsetY / _measureParam.IntervalY;
                        imageData.CompensationX = _measureParam.CompensationX / _measureParam.IntervalX;
                        imageData.CompensationY = _measureParam.CompensationY / _measureParam.IntervalY;
                        _imageData.Add(imageData);
                    }
                    else
                    {
                        HOperatorSet.GenEmptyObj(out hoGrayImage);
                        HOperatorSet.GenEmptyObj(out hoHeightImage);
                        HOperatorSet.GenEmptyObj(out hoValidMask);

                        ret = -1;
                    }

                    return ret;
                }
                finally
                {

                }
            }
        }


        public struct Point
        {
            public double X { get; set; }
            public double Y { get; set; }
        }

        public class PointSetWithScore
        {
            public List<Point> Points { get; set; } = new List<Point>();
            public double Score { get; set; }
        }


        // 计算两点之间的欧氏距离
        private static double Distance(Point a, Point b)
        {
            return Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
        }


        private static double CalculateMinDistance(List<Point> points)
        {
            if (points.Count < 2)
                return 0;

            double minDist = double.MaxValue;
            for (int i = 0; i < points.Count; i++)
            {
                for (int j = 0; j < points.Count; j++)
                {
                    if (i == j)
                        continue;

                    double dist = Distance(points[i], points[j]);

                    if (dist < minDist)
                        minDist = dist;
                }
            }
            return minDist;
        }


        // 计算点到直线的距离
        private static double DistancePointToLine(Point p, Point a, Point b)
        {
            double numerator = Math.Abs((b.X - a.X) * (a.Y - p.Y) - (a.X - p.X) * (b.Y - a.Y));
            double denominator = Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));
            if (denominator == 0)
                return Distance(p, a);
            return numerator / denominator;
        }


        // RANSAC算法提取内点
        private static List<Point> RANSAC(List<Point> points, double threshold, int iterations, int minInliers)
        {
            Random random = new Random();
            List<Point> bestInliers = new List<Point>();
            for (int i = 0; i < iterations; i++)
            {
                // 随机选择两个点
                int index1 = random.Next(points.Count);
                int index2 = random.Next(points.Count);
                if (index1 == index2)
                    continue;
                Point p1 = points[index1];
                Point p2 = points[index2];

                // 如果两点相同，跳过
                if (p1.X == p2.X && p1.Y == p2.Y)
                    continue;

                List<Point> inliers = new List<Point>();
                foreach (Point p in points)
                {
                    double dist = DistancePointToLine(p, p1, p2);
                    if (dist < threshold)
                        inliers.Add(p);
                }

                if (inliers.Count > bestInliers.Count)
                    bestInliers = inliers;
            }
            return bestInliers;
        }


        // 计算点集的得分
        private static double CalculateScore(List<Point> points)
        {
            int pointCount = points.Count;
            if (pointCount < 2)
                return pointCount;

            // PCA：求主方向向量
            double xMean = points.Average(p => p.X);
            double yMean = points.Average(p => p.Y);

            double ssxx = points.Sum(p => (p.X - xMean) * (p.X - xMean));
            double ssyy = points.Sum(p => (p.Y - yMean) * (p.Y - yMean));
            double ssxy = points.Sum(p => (p.X - xMean) * (p.Y - yMean));

            // 特征向量（主方向）
            double theta = 0.5 * Math.Atan2(2 * ssxy, ssxx - ssyy);
            double dx = Math.Cos(theta);
            double dy = Math.Sin(theta);

            // 计算点在主方向上的投影长度（用于归一化）
            var projections = points.Select(p =>
            {
                double px = p.X - xMean;
                double py = p.Y - yMean;
                return dx * px + dy * py;
            }).ToList();
            double projLength = projections.Max() - projections.Min();

            // 避免除零
            if (projLength < 1e-6)
                return pointCount;

            // 计算点到直线的投影残差
            var distances = points.Select(p =>
            {
                double px = p.X - xMean;
                double py = p.Y - yMean;
                // 点到方向向量的垂直距离
                return Math.Abs(-dy * px + dx * py);
            }).ToList();

            // 计算归一化的平均残差（相对于点集长度）
            double meanDist = distances.Sum() / distances.Count;
            double normalizedDist = meanDist / projLength;

            // 得分 = 点数 * e^(-λ*归一化残差)
            // 使用归一化后的残差，lambda 可以设置更大以增加区分度
            double lambda = 1.0;
            double score = pointCount * Math.Exp(-lambda * normalizedDist);
            return score;
        }


        public static List<PointSetWithScore> SegmentPoints(List<double> xList, List<double> yList)
        {
            List<Point> points = new List<Point>();
            for (int i = 0; i < xList.Count; i++)
            {
                points.Add(new Point { X = xList[i], Y = yList[i] });
            }

            double minDist = CalculateMinDistance(points);
            double threshold = minDist * 1.5;
            int minInliers = 1;
            int iterations = 50;

            List<Point> remainingPoints = new List<Point>(points);
            List<List<Point>> pointSets = new List<List<Point>>();
            // RANSAC
            while (remainingPoints.Count >= minInliers)
            {
                List<Point> inliers = RANSAC(remainingPoints, threshold, iterations, minInliers);
                if (inliers.Count < minInliers)
                    break;

                pointSets.Add(inliers);

                remainingPoints = remainingPoints.Except(inliers).ToList();
            }

            // 计算每个点集的得分
            List<PointSetWithScore> result = new List<PointSetWithScore>();
            foreach (var pointSet in pointSets)
            {
                double score = CalculateScore(pointSet);
                result.Add(new PointSetWithScore { Points = pointSet, Score = score });
            }

            result.Sort((a, b) => b.Score.CompareTo(a.Score));
            return result;
        }


        /// <summary>
        /// 点排序
        /// </summary>
        /// <param name="hv_T1"></param>
        /// <param name="hv_T2"></param>
        public static void SortPairs(ref HTuple hv_T1, ref HTuple hv_T2)
        {
            HTuple hv_Sorted1 = new HTuple();
            HTuple hv_Sorted2 = new HTuple();
            HTuple hv_SortMode = new HTuple();
            HTuple hv_Indices1 = new HTuple(), hv_Indices2 = new HTuple();

            if ((hv_T1.TupleMax().D - hv_T1.TupleMin().D) > (hv_T2.TupleMax().D - hv_T2.TupleMin().D))
                hv_SortMode = new HTuple("1");
            else
                hv_SortMode = new HTuple("2");
            if ((int)((new HTuple(hv_SortMode.TupleEqual("1"))).TupleOr(new HTuple(hv_SortMode.TupleEqual(1)))) != 0)
            {
                HOperatorSet.TupleSortIndex(hv_T1, out hv_Indices1);
                hv_Sorted1 = hv_T1.TupleSelect(hv_Indices1);
                hv_Sorted2 = hv_T2.TupleSelect(hv_Indices1);
            }
            else if ((int)((new HTuple((new HTuple(hv_SortMode.TupleEqual("column"))).TupleOr(new HTuple(hv_SortMode.TupleEqual("2"))))
                           ).TupleOr(new HTuple(hv_SortMode.TupleEqual(2)))) != 0)
            {
                HOperatorSet.TupleSortIndex(hv_T2, out hv_Indices2);
                hv_Sorted1 = hv_T1.TupleSelect(hv_Indices2);
                hv_Sorted2 = hv_T2.TupleSelect(hv_Indices2);
            }
            hv_T1 = hv_Sorted1;
            hv_T2 = hv_Sorted2;
        }


        /// <summary>
        /// 点排序
        /// </summary>
        /// <param name="rows"></param>
        /// <param name="cols"></param>
        public static void SortPairs(ref List<double> rows, ref List<double> cols)
        {
            HTuple hv_T1 = new HTuple(rows.ToArray());
            HTuple hv_T2 = new HTuple(cols.ToArray());
            //相同的方法 直接使用htuple返回结果
            SortPairs(ref hv_T1, ref hv_T2);
            rows = hv_T1.ToDArr().ToList();
            cols = hv_T2.ToDArr().ToList();
            return;
        }


        /// <summary>
        /// 最小二乘法线性拟合
        /// </summary>
        /// <param name="data">数据点列表</param>
        /// <param name="intervalX">X方向像素间距</param>
        /// <returns>拟合直线的斜率和截距</returns>
        private (double slope, double intercept) LinearFit(List<(int col, double val)> data, double intervalX)
        {
            int n = data.Count;
            double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;

            foreach (var (col, val) in data)
            {
                double x = col * intervalX;
                sumX += x;
                sumY += val;
                sumXY += x * val;
                sumX2 += x * x;
            }

            double denominator = n * sumX2 - sumX * sumX;
            if (Math.Abs(denominator) < 1e-10)
                return (0, sumY / n);

            double slope = (n * sumXY - sumX * sumY) / denominator;
            double intercept = (sumY - slope * sumX) / n;

            return (slope, intercept);
        }


        /// <summary>
        /// 基于拟合残差的异常点剔除
        /// </summary>
        /// <param name="data">输入数据点列表</param>
        /// <param name="slope">拟合直线斜率</param>
        /// <param name="intercept">拟合直线截距</param>
        /// <param name="intervalX">X方向像素间距</param>
        /// <param name="threshold">剔除阈值（MAD的倍数）</param>
        /// <returns>剔除异常点后的数据</returns>
        private List<(int col, double val)> RemoveOutliersByResidual(List<(int col, double val)> data, double slope, double intercept,
                                                                     double intervalX, double threshold = 3.0)
        {
            if (data.Count < 3)
                return data;

            var residuals = data.Select(p => p.val - (slope * p.col * intervalX + intercept)).ToList();

            residuals.Sort((a, b) => Math.Abs(a).CompareTo(Math.Abs(b)));
            double medianAbsResidual = Math.Abs(residuals[residuals.Count / 2]);

            if (medianAbsResidual < 1e-10)
                return data;

            double scale = 1.4826 * medianAbsResidual;

            return data.Where(p =>
            {
                double residual = p.val - (slope * p.col * intervalX + intercept);
                return Math.Abs(residual) <= threshold * scale;
            }).ToList();
        }


        /// <summary>
        /// 传感器俯仰标定过程
        /// </summary>
        public void PitchCalibration(List<float[]> grayDate, List<float[]> heightData, LinearSpectralSplicing_MeasureParam param,
                                     out double pitchSlope, out float[] depthBase)
        {
            _measureParam = param.DeepCopy();

            using (var dh = new HDevDisposeHelper())
            {
                HObject? hoTmp = null;
                HObject? hoGrayImage = null;
                HObject? hoHeightImage = null;

                HObject? hoValidMask = null;

                try
                {
                    int statusGrayDate, statusHeightData;
                    statusGrayDate = ConvertListToHObject(grayDate, ImageType.Gray, out hoGrayImage);
                    statusHeightData = ConvertListToHObject(heightData, ImageType.Depth, out hoHeightImage);

                    if (statusGrayDate == 0 && statusHeightData == 0)
                    {
                        if (_measureParam.IsFlip)
                        {
                            HOperatorSet.MirrorImage(hoGrayImage, out hoTmp, "row");
                            ReplaceHobject(ref hoGrayImage, ref hoTmp);

                            HOperatorSet.MirrorImage(hoHeightImage, out hoTmp, "row");
                            ReplaceHobject(ref hoHeightImage, ref hoTmp);
                        }
                        _measureParam.IntervalZ = (_measureParam.IntervalZ * 1000) / (_measureParam.IntervalZ * 10);
                        _measureParam.IntervalZ *= 0.001;

                        HOperatorSet.ZoomImageFactor(hoGrayImage, out hoGrayImage, _measureParam.ScaleFactor, _measureParam.ScaleFactor, "bilinear");
                        HOperatorSet.ZoomImageFactor(hoHeightImage, out hoHeightImage, _measureParam.ScaleFactor, _measureParam.ScaleFactor, "nearest_neighbor");

                        _measureParam.IntervalX = (_measureParam.IntervalX * 1000) / _measureParam.ScaleFactor;
                        _measureParam.IntervalY = (_measureParam.IntervalY * 1000) / _measureParam.ScaleFactor;
                        _measureParam.IntervalX *= 0.001;
                        _measureParam.IntervalY *= 0.001;

                        GetDepthValidMask(hoHeightImage!, out hoValidMask);
                        HOperatorSet.ReduceDomain(hoHeightImage, hoValidMask, out hoHeightImage);

                        HOperatorSet.GetImageSize(hoHeightImage, out HTuple hvWidth, out HTuple hvHeight);
                        int width = hvWidth.I;
                        int height = hvHeight.I;

                        HOperatorSet.GetRegionPoints(hoValidMask, out HTuple hvRows, out HTuple hvCols);
                        HOperatorSet.GetGrayval(hoHeightImage, hvRows, hvCols, out HTuple hvGrayVals);

                        // 用于收集所有行筛选出来的拟合点（按列统计）
                        var fitColValues = new Dictionary<int, List<double>>();

                        var validPoints = new Dictionary<int, List<(int col, double val)>>();
                        for (int i = 0; i < hvRows.Length; i++)
                        {
                            int row = hvRows[i].I;
                            int col = hvCols[i].I;
                            double val = hvGrayVals[i].D;

                            if (!validPoints.ContainsKey(row))
                            {
                                validPoints[row] = new List<(int col, double val)>();
                            }
                            validPoints[row].Add((col, val));
                        }

                        var slopes = new List<double>();
                        int minPointsPerRow = Math.Max(10, width / 10);

                        foreach (var kvp in validPoints)
                        {
                            var rowData = kvp.Value;
                            if (rowData.Count < minPointsPerRow)
                                continue;

                            List<double> cols = new List<double>();
                            List<double> vals = new List<double>();
                            foreach (var (col, val) in rowData)
                            {
                                cols.Add(col * _measureParam.IntervalX);
                                vals.Add(val);
                            }

                            List<PointSetWithScore> pointSets = SegmentPoints(cols, vals);

                            if (pointSets.Count > 0)
                            {
                                PointSetWithScore pointSet = pointSets[0];

                                if (pointSet.Points.Count < minPointsPerRow)
                                    continue;

                                List<double> fitRows = new List<double>();
                                List<double> fitCols = new List<double>();
                                for (int i = 0; i < pointSet.Points.Count; i++)
                                {
                                    fitRows.Add(pointSet.Points[i].Y);
                                    fitCols.Add(pointSet.Points[i].X);

                                    // 收集用于拟合的点（按列统计）
                                    int colIndex = (int)(pointSet.Points[i].X / _measureParam.IntervalX);
                                    double valY = pointSet.Points[i].Y;
                                    if (!fitColValues.ContainsKey(colIndex))
                                    {
                                        fitColValues[colIndex] = new List<double>();
                                    }
                                    fitColValues[colIndex].Add(valY);
                                }

                                SortPairs(ref fitRows, ref fitCols);

                                HXLDCont lineXLD = new HXLDCont(new HTuple(fitRows.ToArray()), new HTuple(fitCols.ToArray()));

                                lineXLD.FitLineContourXld(
                                    "tukey",
                                    -1,
                                    0,
                                    5,
                                    2,
                                    out double rowBegin,
                                    out double colBegin,
                                    out double rowEnd,
                                    out double colEnd,
                                    out double nr,
                                    out double nc,
                                    out double dist
                                );

                                double deltaCol = colEnd - colBegin;
                                if (Math.Abs(deltaCol) > 1e-10)
                                {
                                    double finalSlope = (rowEnd - rowBegin) / deltaCol;
                                    slopes.Add(finalSlope);
                                }

                                lineXLD.Dispose();
                            }
                        }

                        if (slopes.Count > 0)
                        {
                            slopes.Sort();
                            int trimCount = slopes.Count / 10;
                            var trimmedSlopes = slopes.Skip(trimCount).Take(slopes.Count - 2 * trimCount).ToList();

                            if (trimmedSlopes.Count > 0)
                            {
                                _pitchSlope = trimmedSlopes.Average();
                                double pitchAngle = Math.Atan(_pitchSlope) * 180.0 / Math.PI;
                                _isPitchCalib = true;

                                Console.WriteLine($"俯仰标定完成: 斜率={_pitchSlope:F6}, 角度={pitchAngle:F4}°");

                                // 计算每列拟合点的均值，缺失列使用直线插值
                                float[] colMeanValues = new float[width];
                                // 先计算已有列的均值，并找出用于插值的参考点
                                double sumX = 0, sumY = 0;
                                int validColCount = 0;
                                foreach (var kvp in fitColValues)
                                {
                                    int col = kvp.Key;
                                    if (col >= 0 && col < width)
                                    {
                                        double mean = kvp.Value.Sum() / kvp.Value.Count;
                                        colMeanValues[col] = (float)mean;
                                        sumX += col;
                                        sumY += mean;
                                        validColCount++;
                                    }
                                }
                                // 使用拟合斜率和均值中心点构建插值直线
                                if (validColCount > 0)
                                {
                                    double centerX = sumX / validColCount;
                                    double centerY = sumY / validColCount;
                                    // 对缺失列使用直线方程插值: y = centerY + slope * (x - centerX)
                                    for (int col = 0; col < width; col++)
                                    {
                                        if (!fitColValues.ContainsKey(col))
                                        {
                                            double interpolatedY = centerY + _pitchSlope * _measureParam.IntervalX * (col - centerX);
                                            colMeanValues[col] = (float)interpolatedY;
                                        }
                                    }
                                }
                                _depthBase = colMeanValues;
                            }
                            else
                            {
                                _pitchSlope = 0;
                                _isPitchCalib = false;
                                Console.WriteLine($"俯仰标定失败:无有效点。");
                            }
                        }
                        else
                        {
                            _pitchSlope = 0;
                            _isPitchCalib = false;
                            Console.WriteLine($"俯仰标定失败:无有效点。");
                        }
                    }
                    else
                    {
                        _pitchSlope = 0;
                        _isPitchCalib = false;
                        Console.WriteLine($"俯仰标定失败:无有效标定图片。");
                    }
                }
                catch (Exception e)
                {
                    _pitchSlope = 0;
                    _isPitchCalib = false;
                    Console.WriteLine($"俯仰标定失败:{e.Message}");
                }
                finally
                {
                    hoTmp?.Dispose();
                    hoGrayImage?.Dispose();
                    hoHeightImage?.Dispose();
                    hoValidMask?.Dispose();
                }
            }
            pitchSlope = _pitchSlope;
            depthBase = _depthBase;
        }


        /// <summary>
        /// 俯仰校正+缓存图片
        /// </summary>
        public void cache_images(List<float[]> grayDate, List<float[]> heightData, LinearSpectralSplicing_MeasureParam param)
        {
            _measureParam = param.DeepCopy();

            using (var dh = new HDevDisposeHelper())
            {
                HObject? hoGrayImage = null;
                HObject? hoHeightImage = null;

                HObject? hoValidMask = null;

                try
                {
                    int ret = PreProcess(grayDate, heightData, out hoGrayImage, out hoHeightImage, out hoValidMask);
                }
                finally
                {
                    hoGrayImage?.Dispose(); hoHeightImage?.Dispose();
                    hoValidMask?.Dispose();
                }
            }
        }


        /// <summary>
        /// 俯仰校正
        /// </summary>
        public void PitchCorrection(HObject hoImage, HObject hoValidMask, out HObject hoImageCorrected)
        {
            // 对hoHeightImage进行俯仰校正
            if (_isPitchCalib && Math.Abs(_pitchSlope) > 1e-10)
            {
                HOperatorSet.GetImageSize(hoImage, out HTuple hvWidth, out HTuple hvHeight);
                int width = hvWidth.I;
                int height = hvHeight.I;
                double intervalX = _measureParam.IntervalX;

                HOperatorSet.GetImagePointer1(hoImage, out HTuple hvPointer, out HTuple hvType, out HTuple _, out HTuple _);

                float[] imageData = new float[height * width];
                Marshal.Copy(hvPointer.IP, imageData, 0, height * width);

                for (int row = 0; row < height; row++)
                {
                    int rowOffset = row * width;
                    for (int col = 0; col < width; col++)
                    {
                        double compensationBase = 0;
                        double compensation;
                        if (_pitchCalibMode == PitchCalibMode.Hybrid)
                        {
                            compensation = col * intervalX * _pitchSlope;
                            if (col < _depthBase.Length)
                                compensationBase = _depthBase[col] - compensation;
                        }
                        else if (_pitchCalibMode == PitchCalibMode.Fitting)
                        {
                            compensation = col * intervalX * _pitchSlope;
                            compensationBase = 0;
                        }
                        else if (_pitchCalibMode == PitchCalibMode.Differential)
                        {
                            if (col < _depthBase.Length)
                                compensation = _depthBase[col];
                            else
                                compensation = 0;
                            compensationBase = 0;
                        }
                        else
                        {
                            compensation = 0;
                            compensationBase = 0;
                        }

                        imageData[rowOffset + col] = (float)(imageData[rowOffset + col] - compensation - compensationBase);
                    }
                }

                GCHandle handle = GCHandle.Alloc(imageData, GCHandleType.Pinned);
                try
                {
                    HOperatorSet.GenImage1(out hoImageCorrected, "real", width, height, handle.AddrOfPinnedObject());
                }
                finally
                {
                    handle.Free();
                }
            }
            else
            {
                hoImageCorrected = hoImage.Clone();
            }

            HOperatorSet.ReduceDomain(hoImageCorrected, hoValidMask, out hoImageCorrected);

        }


        /// <summary>
        /// 拼图
        /// </summary>
        public LinearSpectralSplicing_MeasureResult merge_images(LinearSpectralSplicing_MeasureParam param)
        {
            //_measureParam = param.DeepCopy();
            _pitchCalibMode = _measureParam.PitchCalibMode;
            _isPitchCalib = _measureParam.IsPitchCalib;
            _pitchSlope = _measureParam.PitchSlope;

            using (var dh = new HDevDisposeHelper())
            {
                HObject? hoGrayImages = null;
                HObject? hoHeightImages = null;
                HObject? hoValidMasks = null;

                try
                {
                    int imageNum = _imageData.Count;

                    if (imageNum > 0)
                    {
                        double minOffsetX = _imageData.Min(p => p.OffsetX);
                        double minOffsetY = _imageData.Min(p => p.OffsetY);

                        if (minOffsetX < 0)
                        {
                            for (int i = 0; i < imageNum; i++)
                            {
                                _imageData[i].OffsetX -= minOffsetX;
                            }
                        }
                        if (minOffsetY < 0)
                        {
                            for (int i = 0; i < imageNum; i++)
                            {
                                _imageData[i].OffsetY -= minOffsetY;
                            }
                        }


                        HOperatorSet.GenEmptyObj(out hoGrayImages);
                        HOperatorSet.GenEmptyObj(out hoHeightImages);
                        HOperatorSet.GenEmptyObj(out hoValidMasks);
                        HTuple hvOffsetRows = new HTuple();
                        HTuple hvOffsetCols = new HTuple();
                        HTuple hvTmpRow1 = new HTuple();
                        HTuple hvTmpCol1 = new HTuple();
                        HTuple hvTmpRow2 = new HTuple();
                        HTuple hvTmpCol2 = new HTuple();
                        HTuple hvConcatW = new HTuple(0);
                        HTuple hvConcatH = new HTuple(0);


                        for (int i = 0; i < imageNum; i++)
                        {
                            //图片俯仰校正
                            HObject hoGrayImage = _imageData[i].hoGrayImage;
                            HObject hoHeightImage = _imageData[i].hoHeightImage;
                            HObject hoValidMask = _imageData[i].hoValidMask;

                            PitchCorrection(hoHeightImage, hoValidMask, out HObject hoImageCorrected);
                            _imageData[i].hoHeightImage = hoImageCorrected.Clone();

                            HOperatorSet.ConcatObj(hoGrayImages, hoGrayImage, out hoGrayImages);
                            HOperatorSet.ConcatObj(hoHeightImages, hoImageCorrected, out hoHeightImages);

                            HOperatorSet.TupleConcat(hvOffsetCols, _imageData[i].OffsetX + _imageData[i].CompensationX, out hvOffsetCols);
                            HOperatorSet.TupleConcat(hvOffsetRows, _imageData[i].OffsetY + _imageData[i].CompensationY, out hvOffsetRows);

                            HOperatorSet.TupleConcat(hvTmpCol1, -1, out hvTmpCol1);
                            HOperatorSet.TupleConcat(hvTmpRow1, -1, out hvTmpRow1);
                            HOperatorSet.TupleConcat(hvTmpCol2, -1, out hvTmpCol2);
                            HOperatorSet.TupleConcat(hvTmpRow2, -1, out hvTmpRow2);

                            HOperatorSet.GetImageSize(_imageData[i].hoGrayImage, out HTuple hvTmpW, out HTuple hvTmpH);

                            HTuple hvW = _imageData[i].OffsetX + hvTmpW;
                            HTuple hvH = _imageData[i].OffsetY + hvTmpH;
                            if (hvW > hvConcatW)
                                hvConcatW = hvW;
                            if (hvH > hvConcatH)
                                hvConcatH = hvH;

                            hoImageCorrected.Dispose();
                        }
                        HOperatorSet.TileImagesOffset(hoGrayImages, out _hoTileGrayImage, hvOffsetRows, hvOffsetCols,
                                                      hvTmpRow1, hvTmpCol1, hvTmpRow2, hvTmpCol2, hvConcatW, hvConcatH);
                        HOperatorSet.TileImagesOffset(hoHeightImages, out _hoTileHeightImage, hvOffsetRows, hvOffsetCols,
                                                      hvTmpRow1, hvTmpCol1, hvTmpRow2, hvTmpCol2, hvConcatW, hvConcatH);

                        //HOperatorSet.WriteImage(_hoTileHeightImage, "tiff", 0, "D:\\workspace\\2_ReechiImageAlgorithm\\CPlusPlus\\01_Development\\01_Products\\12_ElectroStaticChuck\\99_output\\01\\test.tiff");

                        //HObject hoPlaneRegion;
                        HObject hoTileHeightImageMean, hoEdgeAmplitude;
                        HOperatorSet.MeanImage(_hoTileHeightImage, out hoTileHeightImageMean, _measureParam.PlaneFilterSize, _measureParam.PlaneFilterSize);
                        HOperatorSet.SobelAmp(hoTileHeightImageMean, out hoEdgeAmplitude, "sum_abs", 3);
                        HOperatorSet.Threshold(hoEdgeAmplitude, out _hoPlaneRegion, 0, _measureParam.PlaneFilterThreshold);
                        HOperatorSet.Connection(_hoPlaneRegion, out _hoPlaneRegion);
                        HOperatorSet.SelectShapeStd(_hoPlaneRegion, out _hoPlaneRegion, "max_area", 70);
                        hoTileHeightImageMean.Dispose(); hoEdgeAmplitude.Dispose();

                        if (_measureParam.EnableSelfCalib)
                        {
                            HObject hoFitSurfaces;

                            HOperatorSet.GenEmptyObj(out hoGrayImages);
                            HOperatorSet.GenEmptyObj(out hoHeightImages);
                            HOperatorSet.GenEmptyObj(out hoValidMasks);
                            HOperatorSet.GenEmptyObj(out hoFitSurfaces);
                            hvOffsetRows = new HTuple();
                            hvOffsetCols = new HTuple();
                            hvTmpRow1 = new HTuple();
                            hvTmpCol1 = new HTuple();
                            hvTmpRow2 = new HTuple();
                            hvTmpCol2 = new HTuple();
                            hvConcatW = new HTuple(0);
                            hvConcatH = new HTuple(0);
                            for (int i = 0; i < imageNum; i++)
                            {
                                HObject hoGrayImage = _imageData[i].hoGrayImage;
                                HObject hoHeightImage = _imageData[i].hoHeightImage;
                                HObject hoValidMask = _imageData[i].hoValidMask;

                                HObject hoFitSurface, hoImageSub;

                                //HOperatorSet.MoveRegion(hoValidMask, out hoValidMask, _imageData[i].OffsetY + _imageData[i].CompensationY,
                                //                                                      _imageData[i].OffsetX + _imageData[i].CompensationX);
                                //HOperatorSet.Intersection(hoValidMask, hoTargetRegion, out hoValidMask);
                                //HOperatorSet.MoveRegion(hoValidMask, out hoValidMask, -_imageData[i].OffsetY - _imageData[i].CompensationY,
                                //                                                      -_imageData[i].OffsetX - _imageData[i].CompensationX);
                                HOperatorSet.MoveRegion(_hoPlaneRegion, out HObject hoPlaneRegionMove, -_imageData[i].OffsetY - _imageData[i].CompensationY,
                                                                                      -_imageData[i].OffsetX - _imageData[i].CompensationX);
                                HOperatorSet.Intersection(hoValidMask, hoPlaneRegionMove, out HObject hoFitPlaneRegion);
                                _imageData[i].hoValidMask = hoValidMask.Clone();
                                hoPlaneRegionMove.Dispose();

                                HOperatorSet.GetImageSize(hoHeightImage, out HTuple hvSampleWidth, out HTuple hvSampleHeight);
                                HOperatorSet.ErosionCircle(hoFitPlaneRegion, out HObject hoFitPlaneRegionErosion, 25);
                                HOperatorSet.RegionFeatures(hoFitPlaneRegionErosion, "area", out HTuple hvTmpArea1);
                                HOperatorSet.RegionFeatures(hoFitPlaneRegion, "area", out HTuple hvTmpArea2);
                                if (hvTmpArea1.D > hvTmpArea2.D * 0.15)
                                {
                                    HOperatorSet.FitSurfaceFirstOrder(hoFitPlaneRegionErosion, hoHeightImage, "tukey", 5, 1, out HTuple hv_Alpha, out HTuple hv_Beta, out HTuple hv_Gamma);
                                    HOperatorSet.AreaCenter(hoFitPlaneRegionErosion, out HTuple hvTmpArea, out HTuple hvTmpCenterR, out HTuple hvTmpCenterC);
                                    HOperatorSet.GenImageSurfaceFirstOrder(out hoFitSurface, "real", hv_Alpha, hv_Beta, hv_Gamma, hvTmpCenterR, hvTmpCenterC, hvSampleWidth, hvSampleHeight);
                                    HOperatorSet.SubImage(hoHeightImage, hoFitSurface, out hoImageSub, 1, 0);
                                    HOperatorSet.ReduceDomain(hoImageSub, hoValidMask, out hoImageSub);
                                    HOperatorSet.ConcatObj(hoFitSurfaces, hoFitSurface, out hoFitSurfaces);
                                }
                                else
                                {
                                    if (i > 0)
                                    {
                                        HOperatorSet.SelectObj(hoFitSurfaces, out hoFitSurface, i);
                                    }
                                    else
                                    {
                                        HOperatorSet.GenImageConst(out hoFitSurface, "real", hvSampleWidth, hvSampleHeight);
                                    }
                                    HOperatorSet.SubImage(hoHeightImage, hoFitSurface, out hoImageSub, 1, 0);
                                    HOperatorSet.ReduceDomain(hoImageSub, hoValidMask, out hoImageSub);
                                    HOperatorSet.ConcatObj(hoFitSurfaces, hoFitSurface, out hoFitSurfaces);
                                }
                                hoFitPlaneRegion.Dispose(); hoFitPlaneRegionErosion.Dispose();

                                HOperatorSet.ConcatObj(hoGrayImages, hoGrayImage, out hoGrayImages);
                                HOperatorSet.ConcatObj(hoHeightImages, hoImageSub, out hoHeightImages);

                                HOperatorSet.TupleConcat(hvOffsetCols, _imageData[i].OffsetX + _imageData[i].CompensationX, out hvOffsetCols);
                                HOperatorSet.TupleConcat(hvOffsetRows, _imageData[i].OffsetY + _imageData[i].CompensationY, out hvOffsetRows);

                                HOperatorSet.TupleConcat(hvTmpCol1, -1, out hvTmpCol1);
                                HOperatorSet.TupleConcat(hvTmpRow1, -1, out hvTmpRow1);
                                HOperatorSet.TupleConcat(hvTmpCol2, -1, out hvTmpCol2);
                                HOperatorSet.TupleConcat(hvTmpRow2, -1, out hvTmpRow2);

                                HOperatorSet.GetImageSize(_imageData[i].hoGrayImage, out HTuple hvTmpW, out HTuple hvTmpH);

                                HTuple hvW = _imageData[i].OffsetX + hvTmpW;
                                HTuple hvH = _imageData[i].OffsetY + hvTmpH;
                                if (hvW > hvConcatW)
                                    hvConcatW = hvW;
                                if (hvH > hvConcatH)
                                    hvConcatH = hvH;
                            }

                            HOperatorSet.TileImagesOffset(hoGrayImages, out _hoTileGrayImage, hvOffsetRows, hvOffsetCols,
                                                      hvTmpRow1, hvTmpCol1, hvTmpRow2, hvTmpCol2, hvConcatW, hvConcatH);
                            HOperatorSet.TileImagesOffset(hoHeightImages, out _hoTileHeightImage, hvOffsetRows, hvOffsetCols,
                                                          hvTmpRow1, hvTmpCol1, hvTmpRow2, hvTmpCol2, hvConcatW, hvConcatH);

                            //GetDepthValidMask(_hoTileHeightImage, out hoTileValidMask);

                            hoFitSurfaces.Dispose();
                        }

                        //HOperatorSet.MeanImage(_hoTileHeightImage, out _hoTileHeightImage, 5, 1);

                        HObject hoTileValidMask, hoTileIrregularRegion;
                        HOperatorSet.GetImageSize(_hoTileHeightImage, out HTuple hvTmpTileW, out HTuple hvTmpTileH);
                        HOperatorSet.GenRectangle1(out hoTileIrregularRegion, 0, 0, hvTmpTileH - 1, hvTmpTileW - 1);
                        for (int i = 0; i < imageNum; i++)
                        {
                            HObject hoValidMask = _imageData[i].hoValidMask;

                            HOperatorSet.MoveRegion(hoValidMask, out hoValidMask, _imageData[i].OffsetY + _imageData[i].CompensationY,
                                                                                  _imageData[i].OffsetX + _imageData[i].CompensationX);
                            HOperatorSet.ConcatObj(hoValidMasks, hoValidMask, out hoValidMasks);
                        }
                        HOperatorSet.Union1(hoValidMasks, out hoTileValidMask);


                        HOperatorSet.Difference(hoTileIrregularRegion, hoTileValidMask, out hoTileIrregularRegion);
                        HOperatorSet.PaintRegion(hoTileIrregularRegion, _hoTileHeightImage, out _hoTileHeightImage, 8888880, "fill");

                        _measureResult.IntervalX = _imageData[0].hvIntervalX;
                        _measureResult.IntervalY = _imageData[0].hvIntervalY;
                        _measureResult.IntervalZ = _imageData[0].hvIntervalZ;
                    }

                    _measureResult.PitchSlope = _pitchSlope;
                    _measureResult.DepthBase = _depthBase;
                    _measureResult.GrayImage = _hoTileGrayImage;
                    _measureResult.HeightImage = _hoTileHeightImage;
                    _measureResult.PlaneRegion = _hoPlaneRegion;



                }
                finally
                {
                    hoGrayImages?.Dispose(); hoHeightImages?.Dispose(); hoValidMasks?.Dispose();
                }

                return _measureResult;
            }
        }
    }


    /// <summary>
    /// 图片与测量数据
    /// </summary>
    public class ImageData : IDisposable
    {
        private bool _disposed = false;

        /// <summary>
        /// 校正后的灰度图
        /// </summary>
        public HObject hoGrayImage { get; set; }

        /// <summary>
        /// 校正后的深度图
        /// </summary>
        public HObject hoHeightImage { get; set; }

        /// <summary>
        /// 深度图有效区域
        /// </summary>
        public HObject hoValidMask { get; set; }

        /// <summary>
        /// X方向像素当量
        /// </summary>
        public HTuple hvIntervalX { get; set; }

        /// <summary>
        /// Y方向像素当量
        /// </summary>
        public HTuple hvIntervalY { get; set; }

        /// <summary>
        /// Y方向像素当量
        /// </summary>
        public HTuple hvIntervalZ { get; set; }

        /// <summary>
        /// 图片拼接X方向偏移量(pixel)
        /// </summary>
        public double OffsetX { get; set; }

        /// <summary>
        /// 图片拼接Y方向偏移量(pixel)
        /// </summary>
        public double OffsetY { get; set; }

        /// <summary>
        /// 图片拼接X方向偏移补偿(pixel)
        /// </summary>
        public double CompensationX { get; set; }

        /// <summary>
        /// 图片拼接Y方向偏移补偿(pixel)
        /// </summary>
        public double CompensationY { get; set; }


        public ImageData()
        {
            hoGrayImage = new HObject();
            hoHeightImage = new HObject();
            hoValidMask = new HObject();
            hvIntervalX = new HTuple();
            hvIntervalY = new HTuple();
            hvIntervalZ = new HTuple();

            OffsetX = 0;
            OffsetY = 0;
            CompensationX = 0;
            CompensationY = 0;
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                hoGrayImage?.Dispose();
                hoHeightImage?.Dispose();
                hvIntervalX?.Dispose();
                hvIntervalY?.Dispose();
                hvIntervalZ?.Dispose();
            }

            _disposed = true;
        }

        ~ImageData()
        {
            Dispose(false);
        }
    }


    /// <summary>
    /// 线光谱俯仰姿态校正模式
    /// </summary>
    public enum PitchCalibMode
    {
        Fitting,
        Differential,
        Hybrid,
        None
    }


    /// <summary>
    /// 算法配置参数
    /// </summary>
    [Serializable]
    public class LinearSpectralSplicing_MeasureParam
    {
        //传感器参数
        private double _intervalX = 2.9;    //X方向的像素当量(μm)
        private double _intervalY = 5;      //Y方向的像素当量(μm)
        private double _intervalZ = 1;      //Y方向的像素当量(μm)
        private double _minDepth = -50000;  //深度图深度值下限(μm * 10)
        private double _maxDepth = 50000;   //深度图深度值上限(μm * 10)
        private bool _isFlip = false;       //图片是否需要翻转
        private PitchCalibMode _pitchCalibMode = PitchCalibMode.Fitting; //俯仰姿态校正模式
        private bool _isPitchCalib = false; //是否已完成俯仰标定
        private double _pitchSlope = 0;     //俯仰标定斜率
        private float[] _depthBase = new float[0];  //俯仰标定高度参考值

        //拼图参数
        private double _scaleFactor = 0.1;  //缩放系数
        private bool _isScanEnd = false;    //扫描是否结束
        private double _offsetX = 0;        //图片拼接X方向偏移量(μm)
        private double _offsetY = 0;        //图片拼接Y方向偏移量(μm)
        private double _compensationX = 0;  //图片拼接X方向偏移补偿(μm)
        private double _compensationY = 0;  //图片拼接Y方向偏移补偿(μm)
        private bool _enableSelfCalib = false; //是否启用自校准
        private int _planeFilterSize = 15;  //平面滤波器核大小
        private int _planeFilterThreshold = 10; //平面滤波器阈值

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
        /// 传感器参数：深度图深度有效值下限(μm)
        /// </summary>
        public double MinDepth
        {
            get { return _minDepth; }
            set
            {
                _minDepth = value;
            }
        }

        /// <summary>
        /// 传感器参数：深度图深度有效值上限(μm)
        /// </summary>
        public double MaxDepth
        {
            get { return _maxDepth; }
            set
            {
                _maxDepth = value;
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
        /// 传感器参数：俯仰姿态校正模式
        /// </summary>
        public PitchCalibMode PitchCalibMode
        {
            get { return _pitchCalibMode; }
            set { _pitchCalibMode = value; }
        }

        /// <summary>
        /// 传感器参数：是否已完成俯仰标定
        /// </summary>
        public bool IsPitchCalib
        {
            get { return _isPitchCalib; }
            set { _isPitchCalib = value; }
        }

        /// <summary>
        /// 传感器参数：俯仰标定斜率
        /// </summary>
        public double PitchSlope
        {
            get { return _pitchSlope; }
            set { _pitchSlope = value; }
        }

        /// <summary>
        /// 传感器参数：深度图高度参考值
        /// </summary>
        public float[] DepthBase
        {
            get { return _depthBase; }
            set { _depthBase = value; }
        }

        /// <summary>
        /// 拼图参数：缩放系数
        /// </summary>
        public double ScaleFactor
        {
            get { return _scaleFactor; }
            set { _scaleFactor = value; }
        }

        /// <summary>
        /// 拼图参数：扫描是否结束
        /// </summary>
        public bool IsScanEnd
        {
            get { return _isScanEnd; }
            set { _isScanEnd = value; }
        }

        /// <summary>
        /// 拼图参数：图片拼接X方向偏移量(μm)
        /// </summary>
        public double OffsetX
        {
            get { return _offsetX; }
            set { _offsetX = value; }
        }

        /// <summary>
        /// 拼图参数：图片拼接Y方向偏移量(μm)
        /// </summary>
        public double OffsetY
        {
            get { return _offsetY; }
            set { _offsetY = value; }
        }

        /// <summary>
        /// 拼图参数：图片拼接X方向偏移补偿(μm)
        /// </summary>
        public double CompensationX
        {
            get { return _compensationX; }
            set { _compensationX = value; }
        }

        /// <summary>
        /// 拼图参数：图片拼接Y方向偏移补偿(μm)
        /// </summary>
        public double CompensationY
        {
            get { return _compensationY; }
            set { _compensationY = value; }
        }

        /// <summary>
        /// 拼图参数：是否启用自校准
        /// </summary>
        public bool EnableSelfCalib
        {
            get { return _enableSelfCalib; }
            set { _enableSelfCalib = value; }
        }

        /// <summary>
        /// 拼图参数：平面滤波器核大小
        /// </summary>
        public int PlaneFilterSize
        {
            get { return _planeFilterSize; }
            set { _planeFilterSize = value; }
        }

        /// <summary>
        /// 拼图参数：平面滤波器阈值
        /// </summary>
        public int PlaneFilterThreshold
        {
            get { return _planeFilterThreshold; }
            set { _planeFilterThreshold = value; }
        }
    }

    /// <summary>
    /// 测量结果
    /// </summary>
    public class LinearSpectralSplicing_MeasureResult
    {
        /// <summary>
        /// 俯仰标定斜率
        /// </summary>
        public double PitchSlope { get; set; }

        /// <summary>
        /// 俯仰标定高度参考值
        /// </summary>
        public float[] DepthBase { get; set; }

        /// <summary>
        /// 完整的灰度图
        /// </summary>
        public HObject GrayImage { get; set; }

        /// <summary>
        /// 完整的高度图
        /// </summary>
        public HObject HeightImage { get; set; }

        /// <summary>
        /// 过滤出的平台区域
        /// </summary>
        public HObject PlaneRegion { get; set; }

        /// <summary>
        /// X方向点间隔
        /// </summary>
        public double IntervalX { get; set; }

        /// <summary>
        /// Y方向点间隔
        /// </summary>
        public double IntervalY { get; set; }

        /// <summary>
        /// Z方向点间隔
        /// </summary>
        public double IntervalZ { get; set; }


        public LinearSpectralSplicing_MeasureResult()
        {
            PitchSlope = 0;
            DepthBase = new float[0];
            GrayImage = new HObject();
            HeightImage = new HObject();
            PlaneRegion = new HObject();

            IntervalX = 1;
            IntervalY = 1;
            IntervalZ = 1;
        }

        ~LinearSpectralSplicing_MeasureResult()
        {
            GrayImage.Dispose();
            HeightImage.Dispose();
            PlaneRegion.Dispose();
        }
    }
}
