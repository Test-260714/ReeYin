using HalconDotNet;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;
using MathNet.Numerics.Statistics.Mcmc;
using OpenCvSharp;

using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Services.CustomProject;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static OpenCvSharp.LineIterator;
using static System.Net.WebRequestMethods;
using ImageCommonAlgorithm = ReeYin_V.Core.Helper.ImageOP.Common_Algorithm;


namespace Custom.MFDJC.Models
{
    public class ElectroStaticChuck_Algorithm : ICustomAlgo
    {
        private ElectroStaticChuck_MeasureParam _measureParam = new ElectroStaticChuck_MeasureParam();

        private HObject _hoTileGrayImage = new HObject();
        private HObject _hoTileHeightImage = new HObject();
        private HObject _hoTileValidMask = new HObject();
        private HObject _hoPlaneRegion = new HObject();
        private HObject _hoConvexRegions = new HObject();

        private HTuple _hvConvexStandardDiameterPixel = new HTuple(); // 凸点标准直径(像素)
        private HTuple _hvConvexStandardHeightPixel = new HTuple();   // 凸点标准高度(像素)
        private HTuple _hvFilterAreathresh = new HTuple();            // 连通域面积过滤阈值

        private ElectroStaticChuck_MeasureResult _measureResult = new ElectroStaticChuck_MeasureResult();

        private bool _disposed = false;

        public ElectroStaticChuck_Algorithm()
        {

        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _hoTileGrayImage?.Dispose();
                _hoTileHeightImage?.Dispose();
                _hoTileValidMask?.Dispose();
                _hoPlaneRegion?.Dispose();
                _hoConvexRegions?.Dispose();

            }

            _disposed = true;

        }

        ~ElectroStaticChuck_Algorithm()
        {
            Dispose();
        }


        /// <summary>
        /// 初始化
        /// </summary>
        public int InitVariable()
        {
            HOperatorSet.GenEmptyObj(out _hoTileGrayImage);
            HOperatorSet.GenEmptyObj(out _hoTileHeightImage);
            HOperatorSet.GenEmptyObj(out _hoTileValidMask);
            HOperatorSet.GenEmptyObj(out _hoPlaneRegion);
            HOperatorSet.GenEmptyObj(out _hoConvexRegions);

            return 0;
        }


        public enum ImageType
        {
            Gray,    // 灰度图
            Depth,   // 深度图
            RGB,     // 三通道RGB图
            BGR      // 三通道BGR图
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

        public List<float[]> ConvertMatToList(Mat mat)
        {
            return ImageCommonAlgorithm.ConvertMatToList(mat);
        }

        public int GetFeature()
        {
            return 0;
        }

        public ElectroStaticChuck_MeasureResult GetMeasureResult()
        {
            return _measureResult;
        }

        public Mat CvDrawResult(ElectroStaticChuck_MeasureResult measureResult, bool _)
        {
            return CvDrawResult(measureResult);
        }

        private void ResetMeasureResult()
        {
            _measureResult.GrayImage?.Dispose();
            _measureResult.HeightImage?.Dispose();
            _measureResult.PlaneRegion?.Dispose();
            _measureResult.FitConvexRegion?.Dispose();

            _measureResult.GrayImage = new HObject();
            _measureResult.HeightImage = new HObject();
            _measureResult.PlaneRegion = new HObject();
            _measureResult.FitConvexRegion = new HObject();
            _measureResult.IntervalX = 1;
            _measureResult.IntervalY = 1;
            _measureResult.IntervalZ = 1;
            _measureResult.MinDepth = -5000;
            _measureResult.MaxDepth = 5000;
            _measureResult.ConvexsFlatness = -1;
            _measureResult.OverallFlatness = -1;
            _measureResult.ConvexResults.Clear();
        }

        private void CaptureMeasureResultImages()
        {
            _measureResult.IntervalX = _measureParam.IntervalX;
            _measureResult.IntervalY = _measureParam.IntervalY;
            _measureResult.IntervalZ = _measureParam.IntervalZ;
            _measureResult.MinDepth = _measureParam.MinDepth;
            _measureResult.MaxDepth = _measureParam.MaxDepth;

            _measureResult.GrayImage?.Dispose();
            _measureResult.HeightImage?.Dispose();
            _measureResult.PlaneRegion?.Dispose();

            _measureResult.GrayImage = _hoTileGrayImage.Clone();
            _measureResult.HeightImage = _hoTileHeightImage.Clone();
            _measureResult.PlaneRegion = _hoTileValidMask.Clone();
        }



        /// <summary>
        /// 将List<float[]>数组转换为halcon图片对象
        /// </summary>
        /// <param name="data">输入的List<float[]>数组</param>
        /// <param name="imageType">输入图片数据类型</param>
        /// <param name="hoObject">输出的halcon图片对象</param>
        /// <param name="usePitchCalib">是否使用俯仰角校准</param>
        /// <returns>状态标志</returns>
        public int ConvertListToHObject(List<float[]> data, ImageType imageType, out HObject hoObject)
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


        public static List<Point3d> ToCvPoint3d(double[] X, double[] Y, double[] Z)
        {
            if (X == null || Y == null || Z == null)
                throw new ArgumentNullException("X/Y/Z cannot be null.");

            int n = Math.Min(X.Length, Math.Min(Y.Length, Z.Length));
            var pts = new List<Point3d>(n);
            for (int i = 0; i < n; i++)
                pts.Add(new Point3d(X[i], Y[i], Z[i]));
            return pts;
        }


        public Plane FitPlaneFast(List<Point3d> points)
        {
            long n = points.Count;
            if (n < 3) throw new ArgumentException("Need >= 3 points.");

            double sx = 0, sy = 0, sz = 0, sxx = 0, syy = 0, szz = 0, sxy = 0, sxz = 0, syz = 0;
            foreach (var p in points)
            {
                sx += p.X; sy += p.Y; sz += p.Z;
                sxx += p.X * p.X; syy += p.Y * p.Y; szz += p.Z * p.Z;
                sxy += p.X * p.Y; sxz += p.X * p.Z; syz += p.Y * p.Z;
            }

            double cx = sx / n, cy = sy / n, cz = sz / n;

            double cxx = sxx / n - cx * cx;
            double cyy = syy / n - cy * cy;
            double czz = szz / n - cz * cz;
            double cxy = sxy / n - cx * cy;
            double cxz = sxz / n - cx * cz;
            double cyz = syz / n - cy * cz;

            var M = MathNet.Numerics.LinearAlgebra.Matrix<double>.Build.DenseOfArray(new[,]
                                                                                        {{ cxx, cxy, cxz },
                                                                                         { cxy, cyy, cyz },
                                                                                         { cxz, cyz, czz }});

            var evd = M.Evd(MathNet.Numerics.LinearAlgebra.Symmetricity.Symmetric);
            // 取最小特征值对应的特征向量为法向量
            int minIdx = 0;
            double minVal = double.MaxValue;
            for (int i = 0; i < 3; i++)
            {
                double v = evd.EigenValues[i].Real;
                if (v < minVal) { minVal = v; minIdx = i; }
            }
            var normal = evd.EigenVectors.Column(minIdx).Normalize(2);
            double d = -(normal[0] * cx + normal[1] * cy + normal[2] * cz);

            return new Plane(normal[0], normal[1], normal[2], d);
        }


        /// <summary>
        /// 最小二乘法拟合平面
        /// </summary>
        /// <param name="points"></param>
        /// <returns>平面参数 [a, b, c, d] 对应于平面方程 ax + by + cz + d = 0</returns>
        public Plane FitPlane(List<Point3d> points)
        {
            var matrix = MathNet.Numerics.LinearAlgebra.Matrix<double>.Build;
            var vector = MathNet.Numerics.LinearAlgebra.Vector<double>.Build;

            var data = matrix.Dense(points.Count, 3, (i, j) =>
            {
                return j switch
                {
                    0 => points[i].X,
                    1 => points[i].Y,
                    2 => points[i].Z,
                    _ => 0
                };
            });

            var centroid = vector.DenseOfEnumerable(new[] { points.Average(p => p.X), points.Average(p => p.Y), points.Average(p => p.Z) });
            var centered = data - matrix.Dense(data.RowCount, 3, (i, j) => centroid[j]);

            // 奇异值分解
            var svd = centered.Svd();
            // 平面法向量
            var normal = svd.VT.Row(2);
            normal = normal.Normalize(2);

            double d = -normal.DotProduct(centroid);

            return new Plane(normal[0], normal[1], normal[2], d);
        }


        // 1.4826 * MAD 作为 sigma 的一致性估计（正态分布下常用）
        public static int MadStats(List<double> values, out double median, out double mad, out double sigmaHat)
        {
            double tmpMed = values.Median();

            List<double> abs = values.Select(v => Math.Abs(v - tmpMed)).ToList();

            median = tmpMed;
            mad = abs.Median();
            sigmaHat = 1.4826 * (mad + 1e-12);

            return 0;
        }


        /// <summary>
        /// 基于残差的统计剔除
        /// </summary>
        public List<Point3d> TrimByMad(List<Point3d> pts, double k = 3.0)
        {
            var keep = new List<Point3d>();

            if (pts.Count < 3)
                return pts;

            Plane plane = FitPlaneFast(pts);

            List<double> r = pts.Select(p => plane.DistanceTo(p)).ToList();
            MadStats(r, out double med, out double mad, out double sigmaHat);


            for (int i = 0; i < pts.Count; i++)
                if (Math.Abs(r[i] - med) <= k * sigmaHat)
                    keep.Add(pts[i]);

            return keep;
        }



        // IRLS(Huber)+加权PCA的鲁棒平面拟合
        public Plane FitPlaneIrlsPCA(List<Point3d> pts, int maxIter = 30, double tol = 1e-6)
        {
            var plane = FitPlaneFast(pts);

            for (int it = 0; it < maxIter; it++)
            {
                List<double> r = pts.Select(p => plane.DistanceTo(p)).ToList();
                MadStats(r, out double med, out double mad, out double sigmaHat);
                double delta = 1.345 * sigmaHat + 1e-12;   // Huber阈值

                // 权重
                var w = r.Select(a => { var t = Math.Abs(a); return (t <= delta) ? 1.0 : (delta / t); }).ToArray();

                // 加权质心
                double sw = w.Sum();
                double mx = 0, my = 0, mz = 0;
                for (int i = 0; i < pts.Count; i++)
                {
                    mx += w[i] * pts[i].X;
                    my += w[i] * pts[i].Y;
                    mz += w[i] * pts[i].Z;
                }
                mx /= sw;
                my /= sw;
                mz /= sw;

                // 计算加权协方差矩阵
                double sxx = 0, sxy = 0, sxz = 0, syy = 0, syz = 0, szz = 0;
                for (int i = 0; i < pts.Count; i++)
                {
                    double dx = pts[i].X - mx, dy = pts[i].Y - my, dz = pts[i].Z - mz, wi = w[i];
                    sxx += wi * dx * dx;
                    sxy += wi * dx * dy;
                    sxz += wi * dx * dz;
                    syy += wi * dy * dy;
                    syz += wi * dy * dz;
                    szz += wi * dz * dz;
                }
                var M = Matrix<double>.Build.DenseOfArray(new double[,] { { sxx, sxy, sxz }, { sxy, syy, syz }, { sxz, syz, szz } });

                // 最小特征向量等于法向
                var evd = M.Evd(Symmetricity.Symmetric);
                var evals = evd.EigenValues.Select(z => z.Real).ToArray();
                int k = Array.IndexOf(evals, evals.Min());
                var n = evd.EigenVectors.Column(k).Normalize(2);

                var newPlane = new Plane(n[0], n[1], n[2], -(n[0] * mx + n[1] * my + n[2] * mz));

                if (Math.Abs(newPlane.A - plane.A) + Math.Abs(newPlane.B - plane.B) +
                    Math.Abs(newPlane.C - plane.C) + Math.Abs(newPlane.D - plane.D) < tol)
                    return newPlane;

                plane = newPlane;
            }

            return plane;
        }


        /// <summary>
        /// 计算平面度（平面上各点到拟合平面的最大距离与最小距离之差）
        /// </summary>
        /// <param name="points"></param>
        /// <param name="plane"></param>
        /// <returns></returns>
        private double CalculateFlatness(List<Point3d> points, Plane plane)
        {
            var distances = points.Select(p => plane.DistanceTo(p)).ToList();
            return distances.Max() - distances.Min();
        }



        /// <summary>
        /// 平面度测量(剔除离群点，鲁棒平面度)
        /// </summary>
        private double GetFlatnessRobust(double[] X, double[] Y, double[] Z)
        {
            List<Point3d> surfacePoints = ToCvPoint3d(X, Y, Z);

            double flatness = -1;
            if (surfacePoints.Count > 3)
            {
                List<Point3d> pointsIn = TrimByMad(surfacePoints, 3.0);
                var fitSurfacePlane = FitPlaneIrlsPCA(pointsIn);
                flatness = CalculateFlatness(pointsIn, fitSurfacePlane);
            }

            return flatness;
        }



        /// <summary>
        /// 输入图片前处理
        /// </summary> 
        public int Preprocess()
        {
            using (var dh = new HDevDisposeHelper())
            {
                HObject? hoTmp = null;
                HObject? hoIrregularRegion = null;


                HTuple hvScaleX, hvScaleY;
                bool fastModel = false;
                if (fastModel)
                {
                    if (_measureParam.IntervalX > _measureParam.IntervalY)
                    {
                        hvScaleX = 1;
                        hvScaleY = _measureParam.IntervalY / _measureParam.IntervalX;
                    }
                    else if (_measureParam.IntervalX < _measureParam.IntervalY)
                    {
                        hvScaleX = _measureParam.IntervalX / _measureParam.IntervalY;
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
                    if (_measureParam.IntervalX < _measureParam.IntervalY)
                    {
                        hvScaleX = 1;
                        hvScaleY = _measureParam.IntervalY / _measureParam.IntervalX;
                    }
                    else if (_measureParam.IntervalX > _measureParam.IntervalY)
                    {
                        hvScaleX = _measureParam.IntervalX / _measureParam.IntervalY;
                        hvScaleY = 1;
                    }
                    else
                    {
                        hvScaleX = 1;
                        hvScaleY = 1;
                    }
                }

                _measureParam.IntervalX = _measureParam.IntervalX / hvScaleX;
                _measureParam.IntervalY = _measureParam.IntervalY / hvScaleY;
                _measureParam.IntervalZ = _measureParam.IntervalZ / _measureParam.IntervalZ;


                // 测量参数转化
                // 凸点标准直径(像素)
                _hvConvexStandardDiameterPixel = _measureParam.ConvexStandardDiameter / _measureParam.IntervalX;
                // 凸点标准高度(像素)
                _hvConvexStandardHeightPixel = _measureParam.ConvexStandardHeight / _measureParam.IntervalZ;

                try
                {
                    HOperatorSet.ZoomImageFactor(_hoTileGrayImage, out hoTmp, hvScaleX, hvScaleY, "bilinear");
                    ReplaceHobject(ref _hoTileGrayImage, ref hoTmp);
                    HOperatorSet.ZoomImageFactor(_hoTileHeightImage, out hoTmp, hvScaleX, hvScaleY, "nearest_neighbor");
                    ReplaceHobject(ref _hoTileHeightImage, ref hoTmp);

                    // 去除深度图异常区域
                    HOperatorSet.GetImageSize(_hoTileHeightImage, out HTuple hvTmpTileW, out HTuple hvTmpTileH);
                    HOperatorSet.GenRectangle1(out _hoTileValidMask, 0, 0, hvTmpTileH - 1, hvTmpTileW - 1);
                    HOperatorSet.Threshold(_hoTileHeightImage, out hoIrregularRegion, _measureParam.InvalidValue - 1, _measureParam.InvalidValue + 1);
                    HOperatorSet.Difference(_hoTileValidMask, hoIrregularRegion, out _hoTileValidMask);

                    return 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);

                    return -1;
                }
                finally
                {
                    hoTmp?.Dispose();
                    hoIrregularRegion?.Dispose();
                }
            }
        }

        /// <summary>
        /// 凸点区域提取
        /// </summary>
        public int GetConvexRegions()
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
                    HOperatorSet.GetImageSize(_hoTileHeightImage, out HTuple hvImageWidth, out HTuple hvImageHeight);
                    HOperatorSet.GenImageConst(out hoSampleValidMaskImage, "real", hvImageWidth, hvImageHeight);
                    HOperatorSet.PaintRegion(_hoTileValidMask, hoSampleValidMaskImage, out hoTmp, 1.0, "fill");
                    ReplaceHobject(ref hoSampleValidMaskImage, ref hoTmp);

                    // 分子：有效像素保留原高度，无效像素变 0
                    HOperatorSet.MultImage(_hoTileHeightImage, hoSampleValidMaskImage, out hoHeightImageWeighted, 1.0, 0.0);

                    // 凸点提取小滤波核尺寸
                    HTuple hvSmallKernelSize = _hvConvexStandardDiameterPixel * 0.1 * 0.5;
                    // 凸点提取大滤波核尺寸
                    HTuple hvBigKernelSize = _hvConvexStandardDiameterPixel * 2;

                    // Small kernel normalized mean
                    HOperatorSet.MeanImage(hoHeightImageWeighted, out hoHeightWeightedMeanSmall, hvSmallKernelSize, hvSmallKernelSize);
                    HOperatorSet.MeanImage(hoSampleValidMaskImage, out hoValidMaskMeanSmall, hvSmallKernelSize, hvSmallKernelSize);
                    HOperatorSet.ReduceDomain(hoHeightWeightedMeanSmall, _hoTileValidMask, out hoTmp);
                    ReplaceHobject(ref hoHeightWeightedMeanSmall, ref hoTmp);
                    HOperatorSet.ReduceDomain(hoValidMaskMeanSmall, _hoTileValidMask, out hoTmp);
                    ReplaceHobject(ref hoValidMaskMeanSmall, ref hoTmp);
                    HOperatorSet.DivImage(hoHeightWeightedMeanSmall, hoValidMaskMeanSmall, out hoHeightImageSampleSmall, 1.0, 0.0);

                    // Big kernel normalized mean
                    HOperatorSet.MeanImage(hoHeightImageWeighted, out hoHeightWeightedMeanBig, hvBigKernelSize, hvBigKernelSize);
                    HOperatorSet.MeanImage(hoSampleValidMaskImage, out hoValidMaskMeanBig, hvBigKernelSize, hvBigKernelSize);
                    HOperatorSet.ReduceDomain(hoHeightWeightedMeanBig, _hoTileValidMask, out hoTmp);
                    ReplaceHobject(ref hoHeightWeightedMeanBig, ref hoTmp);
                    HOperatorSet.ReduceDomain(hoValidMaskMeanBig, _hoTileValidMask, out hoTmp);
                    ReplaceHobject(ref hoValidMaskMeanBig, ref hoTmp);
                    HOperatorSet.DivImage(hoHeightWeightedMeanBig, hoValidMaskMeanBig, out hoHeightImageSampleBig, 1.0, 0.0);

                    // 提取凸点区域
                    // 连通域面积过滤阈值
                    _hvFilterAreathresh = (_hvConvexStandardDiameterPixel / 2) * (_hvConvexStandardDiameterPixel / 2) * 3.14159265359;
                    HOperatorSet.DynThreshold(hoHeightImageSampleSmall, hoHeightImageSampleBig, out _hoConvexRegions, _hvConvexStandardHeightPixel * 0.1, "light");
                    HOperatorSet.Connection(_hoConvexRegions, out hoTmp);
                    ReplaceHobject(ref _hoConvexRegions, ref hoTmp);
                    HOperatorSet.SelectShape(_hoConvexRegions, out hoTmp, (new HTuple("circularity")).TupleConcat("area"), "and",
                                             (new HTuple(0.5)).TupleConcat(_hvFilterAreathresh * 0.5), (new HTuple(1)).TupleConcat(_hvFilterAreathresh * 2));
                    ReplaceHobject(ref _hoConvexRegions, ref hoTmp);

                    return 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);

                    return -1;
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


        /// <summary>
        /// 凸点特征计算
        /// </summary>
        public int ConvexFeatures()
        {
            using (var dh = new HDevDisposeHelper())
            {
                HObject? hoTmp = null;

                HTuple? hvMetrologyHandle = null;
                HObject? hoHeightImageSampleExpanded = null;

                HObject? hoConvexRegion = null;
                HObject? hoFitConvexRegions = null;

                HObject? hoConvexContour = null;

                HObject? hoSampleRegion = null;
                HObject? hoSampleReduce = null, hoSamplePart = null;
                HObject? hoPlaneMeasureRegion = null, hoConvexMeasureRegion = null, hoPlaneMeasureRegionRing = null, hoPlaneMeasureReduced = null;
                HObject? hoPartSurface = null, hoSamplePartSub = null, hoSamplePartSmooth = null, hoSamplePartSubSmooth = null;
                HObject? hoConvexPartRegion = null, hoConvexFlatnessRegion = null;
                try
                {
                    HOperatorSet.GetImageSize(_hoTileHeightImage, out HTuple hvImageWidth, out HTuple hvImageHeight);

                    HOperatorSet.ReduceDomain(_hoTileHeightImage, _hoTileValidMask, out hoHeightImageSampleExpanded);
                    HOperatorSet.ExpandDomainGray(hoHeightImageSampleExpanded, out hoTmp, _hvConvexStandardDiameterPixel);
                    ReplaceHobject(ref hoHeightImageSampleExpanded, ref hoTmp);

                    HOperatorSet.CountObj(_hoConvexRegions, out HTuple hvConvexNum);
                    HOperatorSet.GenEmptyObj(out hoFitConvexRegions);
                    for (int idx = 0; idx < hvConvexNum; idx++)
                    {
                        ConvexFeatures convexResult = new ConvexFeatures();

                        HOperatorSet.SelectObj(_hoConvexRegions, out hoConvexRegion, idx + 1);

                        HTuple hvConvexCenterRow, hvConvexCenterCol, hvConvexRoundness;
                        HTuple hvConvexInnerRadius, hvConvexOuterRadius;

                        HOperatorSet.RegionFeatures(hoConvexRegion, "roundness", out hvConvexRoundness);

                        HOperatorSet.RegionFeatures(hoConvexRegion, "row", out hvConvexCenterRow);
                        HOperatorSet.RegionFeatures(hoConvexRegion, "column", out hvConvexCenterCol);
                        HOperatorSet.RegionFeatures(hoConvexRegion, "inner_radius", out hvConvexInnerRadius);
                        HOperatorSet.RegionFeatures(hoConvexRegion, "outer_radius", out hvConvexOuterRadius);

                        HTuple hvConvexRadius = (hvConvexInnerRadius + hvConvexOuterRadius) * 0.5;

                        // 拟合凸点轮廓圆
                        HTuple hvConvexParam;

                        HOperatorSet.CreateMetrologyModel(out hvMetrologyHandle);
                        HOperatorSet.AddMetrologyObjectCircleMeasure(hvMetrologyHandle, hvConvexCenterRow, hvConvexCenterCol, hvConvexRadius,
                                                                     hvConvexRadius * 0.5, 2, 1.5, _hvConvexStandardHeightPixel, new HTuple(), new HTuple(), out HTuple hvIndex);
                        HOperatorSet.SetMetrologyObjectParam(hvMetrologyHandle, 0, "measure_transition", "negative");
                        HOperatorSet.SetMetrologyObjectParam(hvMetrologyHandle, "all", "min_score", 0.01);
                        HOperatorSet.SetMetrologyObjectParam(hvMetrologyHandle, "all", "measure_select", "last");
                        HOperatorSet.ApplyMetrologyModel(hoHeightImageSampleExpanded, hvMetrologyHandle);
                        //直接获取拟合结果
                        HOperatorSet.GetMetrologyObjectResult(hvMetrologyHandle, "all", "all", "result_type", "all_param", out hvConvexParam);

                        hoConvexContour?.Dispose();
                        HOperatorSet.GetMetrologyObjectResultContour(out hoConvexContour, hvMetrologyHandle, "all", "all", 1.5);
                        if (hvConvexParam.Length > 0)
                        {
                            hvConvexCenterRow = hvConvexParam[0];
                            hvConvexCenterCol = hvConvexParam[1];
                            hvConvexRadius = hvConvexParam[2];
                        }
                        HOperatorSet.ConcatObj(hoFitConvexRegions, hoConvexContour, out hoFitConvexRegions);


                        // 高度
                        HTuple hvSampleRadius = hvConvexRadius * 3;
                        HTuple hvMeasureRadius = hvConvexRadius * 1.5;

                        hoSampleRegion?.Dispose();
                        hoSampleReduce?.Dispose(); hoSamplePart?.Dispose();
                        hoPlaneMeasureRegion?.Dispose(); hoConvexMeasureRegion?.Dispose(); hoPlaneMeasureRegionRing?.Dispose(); hoPlaneMeasureReduced?.Dispose();
                        hoPartSurface?.Dispose(); hoSamplePartSub?.Dispose(); hoSamplePartSmooth?.Dispose(); hoSamplePartSubSmooth?.Dispose();
                        hoConvexPartRegion?.Dispose(); hoConvexFlatnessRegion?.Dispose();

                        HOperatorSet.GenCircle(out hoSampleRegion, hvConvexCenterRow, hvConvexCenterCol, hvSampleRadius);
                        HOperatorSet.RegionFeatures(hoSampleRegion, "width", out HTuple hvSampleWidth);
                        HOperatorSet.RegionFeatures(hoSampleRegion, "height", out HTuple hvSampleHeight);
                        HOperatorSet.ReduceDomain(_hoTileHeightImage, hoSampleRegion, out hoSampleReduce);

                        HOperatorSet.CropDomain(hoSampleReduce, out hoSamplePart);
                        HOperatorSet.GetImageSize(hoSamplePart, out HTuple hvPartWidth, out HTuple hvPartHeight);

                        HTuple hvPartCenterRow, hvPartCenterCol;
                        if (hvConvexCenterCol.D < hvSampleRadius.D)
                        {
                            hvPartCenterCol = hvConvexCenterCol - (hvSampleWidth - hvPartWidth);
                        }
                        else
                        {
                            HOperatorSet.DistancePl(hvConvexCenterRow, hvConvexCenterCol, 0, 0, hvImageHeight, 0, out HTuple hvCenter2EdgeDistance);
                            if (hvCenter2EdgeDistance > hvSampleRadius)
                            {
                                hvPartCenterCol = new HTuple(hvSampleRadius);
                            }
                            else
                            {
                                hvPartCenterCol = hvSampleRadius - (hvSampleWidth - hvPartWidth);
                            }
                        }
                        if (hvConvexCenterRow < hvSampleRadius)
                        {
                            hvPartCenterRow = hvConvexCenterRow - (hvSampleHeight - hvPartHeight);
                        }
                        else
                        {
                            HOperatorSet.DistancePl(hvConvexCenterRow, hvConvexCenterCol, 0, 0, 0, hvImageWidth, out HTuple hvCenter2EdgeDistance);
                            if (hvCenter2EdgeDistance > hvSampleRadius)
                            {
                                hvPartCenterRow = new HTuple(hvSampleRadius);
                            }
                            else
                            {
                                hvPartCenterRow = hvSampleRadius - (hvSampleHeight - hvPartHeight);
                            }
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
                        HOperatorSet.MoveRegion(hoConvexRegion, out hoConvexPartRegion, (-hvConvexCenterRow) + hvPartCenterRow, (-hvConvexCenterCol) + hvPartCenterCol);

                        HTuple hvConvexPointRows, hvConvexPointColumns, hvH;
                        HTuple hvIndicesInc;
                        HOperatorSet.GetRegionPoints(hoConvexPartRegion, out hvConvexPointRows, out hvConvexPointColumns);
                        HOperatorSet.GetGrayval(hoSamplePartSubSmooth, hvConvexPointRows, hvConvexPointColumns, out hvH);

                        HOperatorSet.TupleSortIndex(hvH, out hvIndicesInc);
                        HOperatorSet.TupleLength(hvIndicesInc, out HTuple hvN);
                        double pLow = 0.01;   // 排除最高 0~1%
                        double pHigh = 0.05;  // 保留到最高 5%
                        int start = (int)Math.Floor((1.0 - pHigh) * (hvN.D - 1));
                        int end = (int)Math.Floor((1.0 - pLow) * (hvN.D - 1));
                        start = Math.Max(0, start);
                        end = Math.Max(0, end);
                        if (end < start) end = start;
                        // 取“顶部比例”的索引
                        HOperatorSet.TupleSelectRange(hvIndicesInc, start, end, out HTuple hvTopIdx);
                        // 顶部比例的“相对高度”（用于凸点高度）
                        HOperatorSet.TupleSelect(hvH, hvTopIdx, out HTuple hvTop);
                        HOperatorSet.TupleMean(hvTop, out HTuple hvPeak);
                        HTuple hvConvexHeight = (hvPeak - hvPlaneHeightMean).TupleAbs();
                        // 计算凸点平面度
                        HTuple hvMeasureFlatnessRadius = hvConvexRadius * 0.5;
                        HOperatorSet.GenCircle(out hoConvexFlatnessRegion, hvPartCenterRow, hvPartCenterCol, hvMeasureFlatnessRadius);

                        HTuple hvConvexFlatnessPointRows, hvConvexFlatnessPointColumns, hvFlatnessH, hvFlatnessHReal;
                        HOperatorSet.GetRegionPoints(hoConvexFlatnessRegion, out hvConvexFlatnessPointRows, out hvConvexFlatnessPointColumns);
                        HOperatorSet.GetGrayval(hoSamplePartSubSmooth, hvConvexFlatnessPointRows, hvConvexFlatnessPointColumns, out hvFlatnessH);
                        HOperatorSet.GetGrayval(hoSamplePartSmooth, hvConvexFlatnessPointRows, hvConvexFlatnessPointColumns, out hvFlatnessHReal);

                        double[] X = Array.ConvertAll(hvConvexFlatnessPointColumns.LArr, v => v * _measureResult.IntervalX);
                        double[] Y = Array.ConvertAll(hvConvexFlatnessPointRows.LArr, v => v * _measureResult.IntervalY);
                        double[] Z = Array.ConvertAll(hvFlatnessH.TupleReal().DArr, v => v * _measureResult.IntervalZ);

                        double ConvexFlatnessSingle = GetFlatnessRobust(X, Y, Z);

                        // 顶面候选区域内按相对高度排序，再取对应原始高度作为接触高度
                        HOperatorSet.TupleSortIndex(hvFlatnessH, out HTuple hvFlatnessIndicesInc);
                        HOperatorSet.TupleLength(hvFlatnessIndicesInc, out HTuple hvFlatnessN);
                        int flatnessStart = (int)Math.Floor((1.0 - pHigh) * (hvFlatnessN.D - 1));
                        int flatnessEnd = (int)Math.Floor((1.0 - pLow) * (hvFlatnessN.D - 1));
                        flatnessStart = Math.Max(0, flatnessStart);
                        flatnessEnd = Math.Max(0, flatnessEnd);
                        if (flatnessEnd < flatnessStart) flatnessEnd = flatnessStart;
                        HOperatorSet.TupleSelectRange(hvFlatnessIndicesInc, flatnessStart, flatnessEnd, out HTuple hvFlatnessTopIdx);
                        HOperatorSet.TupleSelect(hvFlatnessHReal, hvFlatnessTopIdx, out HTuple hvFlatnessTopReal);
                        HOperatorSet.TupleMean(hvFlatnessTopReal, out HTuple hvSurfaceValue);

                        convexResult.PixelX = hvConvexCenterCol.D;
                        convexResult.PixelY = hvConvexCenterRow.D;
                        convexResult.X = hvConvexCenterCol.D * _measureResult.IntervalX;
                        convexResult.Y = hvConvexCenterRow.D * _measureResult.IntervalY;
                        convexResult.Z = hvSurfaceValue.D * _measureResult.IntervalZ;
                        convexResult.Diameter = hvConvexRadius.D * 2 * _measureResult.IntervalX;
                        convexResult.Roundness = hvConvexRoundness.D;
                        convexResult.Height = hvConvexHeight.D * _measureResult.IntervalZ;
                        convexResult.Flatness = ConvexFlatnessSingle;

                        _measureResult.ConvexResults.Add(convexResult);

                    }

                    _measureResult.FitConvexRegion = hoFitConvexRegions.Clone();

                    // 凸点平面度
                    double[] ConvexX = _measureResult.ConvexResults.Select(c => c.X).ToArray();
                    double[] ConvexY = _measureResult.ConvexResults.Select(c => c.Y).ToArray();
                    double[] ConvexZ = _measureResult.ConvexResults.Select(c => c.Z).ToArray();
                    double ConvexFlatness = GetFlatnessRobust(ConvexX, ConvexY, ConvexZ);

                    _measureResult.ConvexsFlatness = ConvexFlatness;

                    return 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);

                    return -1;
                }
                finally
                {
                    hvMetrologyHandle?.Dispose();

                    hoTmp?.Dispose();
                    hoHeightImageSampleExpanded?.Dispose();

                    hoConvexRegion?.Dispose();
                    hoFitConvexRegions?.Dispose();

                    hoConvexContour?.Dispose();

                    hoSampleRegion?.Dispose();
                    hoSampleReduce?.Dispose(); hoSamplePart?.Dispose();
                    hoPlaneMeasureRegion?.Dispose(); hoConvexMeasureRegion?.Dispose(); hoPlaneMeasureRegionRing?.Dispose(); hoPlaneMeasureReduced?.Dispose();
                    hoPartSurface?.Dispose(); hoSamplePartSub?.Dispose(); hoSamplePartSmooth?.Dispose(); hoSamplePartSubSmooth?.Dispose();
                    hoConvexPartRegion?.Dispose(); hoConvexFlatnessRegion?.Dispose();
                }
            }
        }


        /// <summary>
        /// 测量过程
        /// </summary>
        public ElectroStaticChuck_MeasureResult Process(List<float[]> grayDate, List<float[]> heightData, ElectroStaticChuck_MeasureParam param)
        {
            _disposed = false;

            using (var dh = new HDevDisposeHelper())
            {
                HObject? hoTmp = null;

                try
                {
                    _measureParam = param.DeepCopy();
                    ResetMeasureResult();

                    // 将C#数组格式的图片数据转为HObject对象
                    int statusGrayDate, statusHeightData;
                    statusGrayDate = ConvertListToHObject(grayDate, ImageType.Gray, out _hoTileGrayImage);
                    statusHeightData = ConvertListToHObject(heightData, ImageType.Depth, out _hoTileHeightImage);
                    if (statusGrayDate != 0 || statusHeightData != 0)
                    {
                        throw new Exception("输入图片转换失败");
                    }

                    if (Preprocess() != 0)
                    {
                        throw new Exception("输入图片前处理失败");
                    }

                    CaptureMeasureResultImages();

                    if (GetConvexRegions() != 0)
                    {
                        throw new Exception("提取凸点区域失败");
                    }

                    if (ConvexFeatures() != 0)
                    {
                        throw new Exception("逐凸点特征计算失败");
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

            return _measureResult;
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
        /// 绘制测量结果
        /// </summary>
        public Mat CvDrawResult(ElectroStaticChuck_MeasureResult measureResult)
        {
            Mat image = new Mat();

            try
            {
                HobjectToMat(_hoTileGrayImage, out Mat grayImage);
                if (grayImage.Empty())
                {
                    return image;
                }

                if (grayImage.Channels() == 1)
                {
                    Cv2.CvtColor(grayImage, image, ColorConversionCodes.GRAY2BGR);
                }
                else
                {
                    image = grayImage.Clone();
                }

                grayImage.Dispose();

                Scalar contourColor = new Scalar(0, 255, 0);
                Scalar centerColor = new Scalar(0, 0, 255);
                Scalar textColor = new Scalar(0, 255, 255);
                Scalar shadowColor = new Scalar(0, 0, 0);
                HersheyFonts font = HersheyFonts.HersheySimplex;
                double fontScale = 0.5;
                int textThickness = 1;
                int shadowThickness = 3;
                int lineHeight = 18;

                HObject? hoObject = null;
                HObject? hoContourSource = null;
                HObject? hoContour = null;
                int drawnContourCount = 0;

                try
                {
                    HOperatorSet.CountObj(measureResult.FitConvexRegion, out HTuple hvObjectCount);

                    for (int i = 1; i <= hvObjectCount.I; i++)
                    {
                        hoObject?.Dispose();
                        hoContourSource?.Dispose();
                        hoContour?.Dispose();
                        hoObject = null;
                        hoContourSource = null;
                        hoContour = null;

                        HOperatorSet.SelectObj(measureResult.FitConvexRegion, out hoObject, i);
                        HOperatorSet.GetObjClass(hoObject, out HTuple hvObjectClass);
                        string objectClass = hvObjectClass.S.ToLowerInvariant();

                        if (objectClass == "region")
                        {
                            HOperatorSet.GenContourRegionXld(hoObject, out hoContourSource, "border");
                        }
                        else
                        {
                            hoContourSource = hoObject.Clone();
                        }

                        HOperatorSet.CountObj(hoContourSource, out HTuple hvContourCount);
                        for (int j = 1; j <= hvContourCount.I; j++)
                        {
                            hoContour?.Dispose();
                            hoContour = null;

                            HOperatorSet.SelectObj(hoContourSource, out hoContour, j);
                            HTuple hvRows;
                            HTuple hvCols;
                            try
                            {
                                HOperatorSet.GetContourXld(hoContour, out hvRows, out hvCols);
                            }
                            catch
                            {
                                continue;
                            }

                            int pointCount = Math.Min(hvRows.Length, hvCols.Length);
                            if (pointCount < 2)
                            {
                                continue;
                            }

                            OpenCvSharp.Point[] contourPoints = new OpenCvSharp.Point[pointCount];
                            for (int k = 0; k < pointCount; k++)
                            {
                                int x = Math.Clamp((int)Math.Round(hvCols[k].D), 0, image.Width - 1);
                                int y = Math.Clamp((int)Math.Round(hvRows[k].D), 0, image.Height - 1);
                                contourPoints[k] = new OpenCvSharp.Point(x, y);
                            }

                            Cv2.Polylines(image, new[] { contourPoints }, true, contourColor, 2, LineTypes.AntiAlias);
                            drawnContourCount++;
                        }
                    }
                }
                finally
                {
                    hoObject?.Dispose();
                    hoContourSource?.Dispose();
                    hoContour?.Dispose();
                }

                if (drawnContourCount == 0 && measureResult.ConvexResults.Count > 0)
                {
                    foreach (ConvexFeatures convexResult in measureResult.ConvexResults)
                    {
                        if (measureResult.IntervalX <= 0)
                        {
                            continue;
                        }

                        int centerX = Math.Clamp((int)Math.Round(convexResult.PixelX), 0, image.Width - 1);
                        int centerY = Math.Clamp((int)Math.Round(convexResult.PixelY), 0, image.Height - 1);
                        int radius = Math.Max(1, (int)Math.Round(convexResult.Diameter / (2.0 * measureResult.IntervalX)));
                        Cv2.Circle(image, new OpenCvSharp.Point(centerX, centerY), radius, contourColor, 2, LineTypes.AntiAlias);
                    }
                }

                string flatnessText = $"凸点整体平面度：{measureResult.ConvexsFlatness:F6} 微米";
                try
                {
                    using (var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(image))
                    using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
                    using (var fontFamily = new System.Drawing.Font("Microsoft YaHei", 250, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel))
                    using (var textBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Cyan))
                    {
                        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

                        System.Drawing.PointF textPoint = new System.Drawing.PointF(100, 200);
                        graphics.DrawString(flatnessText, fontFamily, textBrush, textPoint);

                        using Mat textImage = OpenCvSharp.Extensions.BitmapConverter.ToMat(bitmap);
                        image.Dispose();
                        image = textImage.Clone();
                    }
                }
                catch
                {
                    string fallbackText = $"ConvexsFlatness: {measureResult.ConvexsFlatness:F6} um";
                    Cv2.PutText(image, fallbackText, new OpenCvSharp.Point(20, 40), font, 0.8, shadowColor, shadowThickness, LineTypes.AntiAlias);
                    Cv2.PutText(image, fallbackText, new OpenCvSharp.Point(20, 40), font, 0.8, textColor, 2, LineTypes.AntiAlias);
                }

                foreach (ConvexFeatures convexResult in measureResult.ConvexResults)
                {
                    int centerX = Math.Clamp((int)Math.Round(convexResult.PixelX), 0, image.Width - 1);
                    int centerY = Math.Clamp((int)Math.Round(convexResult.PixelY), 0, image.Height - 1);
                    OpenCvSharp.Point center = new OpenCvSharp.Point(centerX, centerY);

                    Cv2.Circle(image, center, 3, centerColor, -1, LineTypes.AntiAlias);
                }

                try
                {
                    using (var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(image))
                    using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
                    using (var fontFamily = new System.Drawing.Font("Microsoft YaHei", 18, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel))
                    using (var textBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Cyan))
                    {
                        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

                        int localLineHeight = Math.Max(18, fontFamily.Height + 2);
                        foreach (ConvexFeatures convexResult in measureResult.ConvexResults)
                        {
                            int centerX = Math.Clamp((int)Math.Round(convexResult.PixelX), 0, image.Width - 1);
                            int centerY = Math.Clamp((int)Math.Round(convexResult.PixelY), 0, image.Height - 1);
                            string[] labels =
                            {
                                $"高度：{convexResult.Height:F2} 微米",
                                $"直径：{convexResult.Diameter:F2} 微米",
                                $"圆度：{convexResult.Roundness:F4}"
                            };

                            float maxTextWidth = 0;
                            foreach (string label in labels)
                            {
                                System.Drawing.SizeF textSize = graphics.MeasureString(label, fontFamily);
                                maxTextWidth = Math.Max(maxTextWidth, textSize.Width);
                            }

                            float textX = centerX + 10;
                            if (textX + maxTextWidth + 8 >= image.Width)
                            {
                                textX = centerX - maxTextWidth - 10;
                            }

                            textX = Math.Clamp(textX, 0, Math.Max(0, image.Width - maxTextWidth - 1));

                            float textY = centerY - localLineHeight;
                            if (textY < localLineHeight)
                            {
                                textY = centerY + localLineHeight;
                            }

                            int labelsHeight = localLineHeight * labels.Length;
                            if (textY + labelsHeight >= image.Height)
                            {
                                textY = Math.Max(localLineHeight, image.Height - labelsHeight - 2);
                            }

                            for (int i = 0; i < labels.Length; i++)
                            {
                                graphics.DrawString(labels[i], fontFamily, textBrush, new System.Drawing.PointF(textX - 50, textY + i * localLineHeight + 50));
                            }
                        }

                        using Mat textImage = OpenCvSharp.Extensions.BitmapConverter.ToMat(bitmap);
                        image.Dispose();
                        image = textImage.Clone();
                    }
                }
                catch
                {
                    foreach (ConvexFeatures convexResult in measureResult.ConvexResults)
                    {
                        int centerX = Math.Clamp((int)Math.Round(convexResult.PixelX), 0, image.Width - 1);
                        int centerY = Math.Clamp((int)Math.Round(convexResult.PixelY), 0, image.Height - 1);
                        Cv2.PutText(image, $"Height: {convexResult.Height:F2} um", new OpenCvSharp.Point(centerX + 10, centerY), font, fontScale, textColor, textThickness, LineTypes.AntiAlias);
                        Cv2.PutText(image, $"Diameter: {convexResult.Diameter:F2} um", new OpenCvSharp.Point(centerX + 10, centerY + lineHeight), font, fontScale, textColor, textThickness, LineTypes.AntiAlias);
                        Cv2.PutText(image, $"Roundness: {convexResult.Roundness:F4}", new OpenCvSharp.Point(centerX + 10, centerY + lineHeight * 2), font, fontScale, textColor, textThickness, LineTypes.AntiAlias);
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }

            return image;
        }

    }


    public class Plane
    {
        public double A { get; set; }
        public double B { get; set; }
        public double C { get; set; }
        public double D { get; set; }

        public Plane()
        {

        }

        public Plane(double a, double b, double c, double d)
        {
            A = a;
            B = b;
            C = c;
            D = d;
        }

        public double AbsDistanceTo(Point3d point)
        {
            return Math.Abs(A * point.X + B * point.Y + C * point.Z + D) / (Math.Sqrt(A * A + B * B + C * C) + 1e-12);
        }

        public double DistanceTo(Point3d point)
        {
            return (A * point.X + B * point.Y + C * point.Z + D) / (Math.Sqrt(A * A + B * B + C * C) + 1e-12);
        }
    }


    /// <summary>
    /// 算法配置参数
    /// </summary>
    [Serializable]
    public class ElectroStaticChuck_MeasureParam
    {
        //传感器参数
        private double _intervalX = 2.9;    //X方向的像素当量(μm)
        private double _intervalY = 5;      //Y方向的像素当量(μm)
        private double _intervalZ = 1;      //Y方向的像素当量(μm)
        private double _minDepth = -5000;  //深度图深度值下限(μm)
        private double _maxDepth = 5000;   //深度图深度值上限(μm)
        private double _invalidValue = 888888; //哨兵值
        private bool _isFlip = false;       //图片是否需要翻转
        private bool _isScanEnd = false;    //是否为扫描结束块
        private double _offsetX = 0;        //拼接偏移X
        private double _offsetY = 0;        //拼接偏移Y

        //测量参数       
        private double _convexStandardDiameter = 820;  //凸点标准直径(单位μm)
        private double _convexStandardHeight = 30;     //凸点标准高度(单位μm)


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
        /// 传感器参数: 无效值(μm)
        /// </summary>
        public double InvalidValue
        {
            get { return _invalidValue; }
            set
            {
                _invalidValue = value;
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
        /// 文件扫描模式兼容参数：是否为扫描结束块。
        /// </summary>
        public bool IsScanEnd
        {
            get { return _isScanEnd; }
            set { _isScanEnd = value; }
        }

        /// <summary>
        /// 文件扫描模式兼容参数：X方向偏移。
        /// </summary>
        public double OffsetX
        {
            get { return _offsetX; }
            set { _offsetX = value; }
        }

        /// <summary>
        /// 文件扫描模式兼容参数：Y方向偏移。
        /// </summary>
        public double OffsetY
        {
            get { return _offsetY; }
            set { _offsetY = value; }
        }

        /// <summary>
        /// 测量参数: 凸点标准直径(单位μm)
        /// </summary>
        public double ConvexStandardDiameter
        {
            get { return _convexStandardDiameter; }
            set
            {
                _convexStandardDiameter = value;
            }
        }

        /// <summary>
        /// 测量参数: 凸点标准直径(单位μm)
        /// </summary>
        public double ConvexStandardHeight
        {
            get { return _convexStandardHeight; }
            set
            {
                _convexStandardHeight = value;
            }
        }

    }


    /// <summary>
    /// 凸点测量结果
    /// </summary>
    public class ConvexFeatures
    {
        /// <summary>
        /// 凸点高度
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// 凸点圆度
        /// </summary>
        public double Roundness { get; set; }

        /// <summary>
        /// 凸点直径
        /// </summary>
        public double Diameter { get; set; }

        /// <summary>
        /// 平面度
        /// </summary>
        public double Flatness { get; set; }

        /// <summary>
        /// 凸点像素X轴坐标
        /// </summary>
        public double PixelX { get; set; }

        /// <summary>
        /// 凸点像素Y轴坐标
        /// </summary>
        public double PixelY { get; set; }

        /// <summary>
        /// 凸点X轴坐标
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// 凸点Y轴坐标
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// 凸点表面深度值
        /// </summary>
        public double Z { get; set; }

        public ConvexFeatures()
        {
            Height = -1;
            Roundness = -1;
            Diameter = -1;
            Flatness = -1;
            PixelX = 0;
            PixelY = 0;
            X = double.NegativeInfinity;
            Y = double.NegativeInfinity;
            Z = double.NegativeInfinity;
        }

        ~ConvexFeatures()
        {
        }
    }


    /// <summary>
    /// 测量结果
    /// </summary>
    public class ElectroStaticChuck_MeasureResult
    {
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
        /// 拟合出的凸点区域
        /// </summary>
        public HObject FitConvexRegion { get; set; }

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

        /// <summary>
        /// 深度图深度值下限。
        /// </summary>
        public double MinDepth { get; set; }

        /// <summary>
        /// 深度图深度值上限。
        /// </summary>
        public double MaxDepth { get; set; }

        /// <summary>
        /// 凸点平面度
        /// </summary>
        public double ConvexsFlatness { get; set; }

        /// <summary>
        /// 静电卡盘整体平面度
        /// </summary>
        public double OverallFlatness { get; set; }

        /// <summary>
        /// 所有凸点的测量值
        /// </summary>
        public List<ConvexFeatures> ConvexResults { get; set; }


        public ElectroStaticChuck_MeasureResult()
        {
            GrayImage = new HObject();
            HeightImage = new HObject();
            PlaneRegion = new HObject();
            FitConvexRegion = new HObject();

            IntervalX = 1;
            IntervalY = 1;
            IntervalZ = 1;
            MinDepth = -5000;
            MaxDepth = 5000;

            ConvexsFlatness = -1;
            OverallFlatness = -1;
            ConvexResults = new List<ConvexFeatures>();
        }

        ~ElectroStaticChuck_MeasureResult()
        {
            GrayImage.Dispose();
            HeightImage.Dispose();
            PlaneRegion.Dispose();
            FitConvexRegion.Dispose();

            ConvexResults.Clear();
        }
    }

}
