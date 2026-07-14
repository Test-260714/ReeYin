using Custom.WaferFlatnessMeasure.ViewModels;
using HalconDotNet;
using HandyControl.Controls;
using MathNet.Numerics.LinearAlgebra;
using OpenCvSharp;
using ReeYin_V.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Custom.WaferFlatnessMeasure
{
    public class Flatness_Algorithm
    {
        private Flatness_MeasureParam _measureParam;

        static Flatness_Algorithm()
        {
            try
            {
                MathNet.Numerics.Control.UseNativeMKL();
                Console.WriteLine("MathNet MKL provider loaded.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MathNet MKL provider not available, using managed provider. {ex.Message}");
            }
        }


        public Flatness_Algorithm() : this(new Flatness_MeasureParam())
        {
        }

        public Flatness_Algorithm(Flatness_MeasureParam param)
        {
            SetMeasureParam(param);
        }

        public void SetMeasureParam(Flatness_MeasureParam param)
        {
            _measureParam = param ?? new Flatness_MeasureParam();
        }


        /// <summary>
        /// 极坐标转直角坐标
        /// </summary>
        public List<double[]> PolarToCartesian(List<double[]> pointsSet)
        {
            var cartesianPoints = new List<double[]>(pointsSet.Count);

            foreach (var point in pointsSet)
            {
                double r = point[0];                        // 旋转半径
                double theta = point[1] * (Math.PI / 180);  // 旋转角度（转弧度）
                double z = point[2];                        // 测量高度

                double x = r * Math.Cos(theta);
                double y = r * Math.Sin(theta);

                cartesianPoints.Add(new[] { x, y, z });
            }

            return cartesianPoints;
        }


        /// <summary>
        /// 转点集
        /// </summary>
        public List<double[]> ToPoint3D(List<double[]> pointsSet, bool isPolar)
        {
            List<double[]> points = new List<double[]>(pointsSet.Count);

            if (isPolar)
            {
                points = PolarToCartesian(pointsSet);
            }
            else
            {
                foreach (var point in pointsSet)
                {
                    double x = point[0];
                    double y = point[1];
                    double z = point[2];
                    points.Add(new[] { x, y, z });
                }
            }

            return points;
        }


        public Plane FitPlaneFast(List<double[]> points)
        {
            long n = points.Count;
            if (n < 3) throw new ArgumentException("Need >= 3 points.");

            double sx = 0, sy = 0, sz = 0, sxx = 0, syy = 0, szz = 0, sxy = 0, sxz = 0, syz = 0;
            foreach (var p in points)
            {
                double x = p[0];
                double y = p[1];
                double z = p[2];
                sx += x; sy += y; sz += z;
                sxx += x * x; syy += y * y; szz += z * z;
                sxy += x * y; sxz += x * z; syz += y * z;
            }

            double cx = sx / n, cy = sy / n, cz = sz / n;

            double cxx = sxx / n - cx * cx;
            double cyy = syy / n - cy * cy;
            double czz = szz / n - cz * cz;
            double cxy = sxy / n - cx * cy;
            double cxz = sxz / n - cx * cz;
            double cyz = syz / n - cy * cz;

            var M = Matrix<double>.Build.DenseOfArray(new[,] { { cxx, cxy, cxz }, { cxy, cyy, cyz }, { cxz, cyz, czz } });

            var evd = M.Evd(Symmetricity.Symmetric);
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
        public Plane FitPlane(List<double[]> points)
        {
            var matrix = Matrix<double>.Build;
            var vector = Vector<double>.Build;

            int count = points.Count;
            var data = matrix.Dense(count, 3);
            double sumX = 0;
            double sumY = 0;
            double sumZ = 0;
            for (int i = 0; i < count; i++)
            {
                var p = points[i];
                double x = p[0];
                double y = p[1];
                double z = p[2];

                data[i, 0] = x;
                data[i, 1] = y;
                data[i, 2] = z;

                sumX += x;
                sumY += y;
                sumZ += z;
            }

            var centroid = vector.DenseOfArray(new[] { sumX / count, sumY / count, sumZ / count });
            var centered = data - matrix.Dense(data.RowCount, 3, (i, j) => centroid[j]);

            // 奇异值分解
            var svd = centered.Svd();
            // 平面法向量
            var normal = svd.VT.Row(2);
            normal = normal.Normalize(2);

            double d = -normal.DotProduct(centroid);

            return new Plane(normal[0], normal[1], normal[2], d);
        }


        /// <summary>
        /// 计算中位面点集(确保pointsA、pointsB点的X与Y坐标一一对应,且在同一坐标系)
        /// <param name="pointsA">A面点云坐标</param>
        /// <param name="pointsB">B面点云坐标</param>
        /// </summary>
        public int CalculateMedianPointSet(List<double[]> pointsA, List<double[]> pointsB, out List<double[]> medianPointSet)
        {
            if (pointsA.Count > 2 && pointsB.Count > 2 && pointsA.Count == pointsB.Count)
            {
                medianPointSet = new List<double[]>(pointsA.Count);
                for (int i = 0; i < pointsA.Count; i++)
                {
                    var a = pointsA[i];
                    var b = pointsB[i];
                    medianPointSet.Add(new[] { a[0], a[1], (a[2] + b[2]) / 2 });
                }

                return 0;
            }
            else
            {
                medianPointSet = new List<double[]>();

                return -1;
            }
        }


        /// <summary>
        /// 原始数据渲染
        /// </summary>
        /// <param name="points">原始点集List<double[x,y,z]></param>
        /// <param name="surfacePCD">原始点云插值结果</param>
        /// <param name="surfaceNormalPCD">点集规范化后的插值结果</param>
        /// <returns></returns>
        public int Renderer(List<double[]> points, out List<double[]> surfacePCD, out List<double[]> surfaceNormalPCD)
        {
            surfacePCD = new List<double[]>();
            surfaceNormalPCD = new List<double[]>();

            if (points != null && points.Count > 2)
            {
                Plane referencePlane = FitPlane(points);

                surfacePCD = new List<double[]>(points.Count);
                surfaceNormalPCD = new List<double[]>(points.Count);
                bool canSolveZ = Math.Abs(referencePlane.C) >= 1e-12;

                for (int i = 0; i < points.Count; i++)
                {
                    var p = points[i];
                    double x = p[0];
                    double y = p[1];
                    double z = p[2];
                    double normalZ = canSolveZ ? z - referencePlane.GetZ(x, y) : z;

                    surfacePCD.Add(new[] { x, y, z });
                    surfaceNormalPCD.Add(new[] { x, y, normalZ });
                }

                if (_measureParam.Interpolate)
                {
                    // 生成密集点云
                    double epsilon = _measureParam.Epsilon;
                    RBF_Function function = _measureParam.Function;
                    double smooth = _measureParam.Smooth;
                    int resolution = _measureParam.Resolution;

                    RbfInterpolator surfaceRbf = new RbfInterpolator(surfacePCD, epsilon, function, smooth);
                    surfacePCD = surfaceRbf.GenerateDenseGrid(resolution, _measureParam.OuterRadius, _measureParam.InnerRadius);

                    RbfInterpolator surfaceNormalRbf = new RbfInterpolator(surfaceNormalPCD, epsilon, function, smooth);
                    surfaceNormalPCD = surfaceNormalRbf.GenerateDenseGrid(resolution, _measureParam.OuterRadius, _measureParam.InnerRadius);
                }

                return 0;
            }

            return -1;
        }


        /// <summary>
        /// 计算平面度（平面上各点到拟合平面的最大距离与最小距离之差）
        /// </summary>
        /// <param name="points"></param>
        /// <param name="flatness">平面度</param>
        /// <param name="flatnessPCD">平面度状态点云</param>
        /// <returns></returns>
        public int Flatness(List<double[]> points, out double flatness, out List<double[]> flatnessPCD)
        {
            if (points.Count > 2)
            {
                Plane referencePlane = FitPlane(points);

                List<double[]> distances = new List<double[]>(points.Count);
                double distancesMin = double.MaxValue;
                double distancesMax = double.MinValue;
                for (int i = 0; i < points.Count; i++)
                {
                    var p = points[i];
                    double x = p[0];
                    double y = p[1];
                    double z = referencePlane.DistanceTo(p);

                    distances.Add(new[] { x, y, z });
                    if (z < distancesMin) distancesMin = z;
                    if (z > distancesMax) distancesMax = z;
                }
                flatness = Math.Abs(distancesMax - distancesMin);

                var flatnessPoints = new List<double[]>(distances.Count);
                for (int i = 0; i < distances.Count; i++)
                {
                    var d = distances[i];
                    double x = d[0];
                    double y = d[1];
                    double z = d[2] - distancesMin;

                    flatnessPoints.Add(new[] { x, y, z });
                }

                if (_measureParam.Interpolate)
                {
                    // 生成密集点云
                    double epsilon = _measureParam.Epsilon;
                    RBF_Function function = _measureParam.Function;
                    double smooth = _measureParam.Smooth;
                    int resolution = _measureParam.Resolution;

                    RbfInterpolator rbf = new RbfInterpolator(flatnessPoints, epsilon, function, smooth);
                    flatnessPoints = rbf.GenerateDenseGrid(resolution, _measureParam.OuterRadius, _measureParam.InnerRadius);
                }

                flatnessPCD = flatnessPoints;

                return 0;
            }
            else
            {
                flatness = -1;
                flatnessPCD = new List<double[]>();

                return -1;
            }
        }


        /// <summary>
        /// 平面平行度测量算法(确保pointsA、pointsB点的X与Y坐标一一对应,且在同一坐标系)
        /// 说明：以A面为参考面，B面所有测点，必须落在两张与A平行的包容平面之间，
        ///      这两张平面的最小间距，就是平行度误差
        /// <param name="pointsA">A面点云坐标(基准面)</param>
        /// <param name="pointsB">B面点云坐标(测量面)</param>
        /// <param name="parallelism">平行度误差</param>
        /// <param name="parallelismPCD">平行度状态点云</param>
        /// </summary>
        public int Parallelism(List<double[]> pointsA, List<double[]> pointsB,
                               out double parallelism, out List<double[]> parallelismPCD)
        {
            if (pointsA.Count > 2 && pointsB.Count > 2 && pointsA.Count == pointsB.Count)
            {
                Plane referencePlane = FitPlane(pointsA);
                List<double[]> distances = new List<double[]>(pointsB.Count);
                double distanceMin = double.MaxValue;
                double distanceMax = double.MinValue;

                for (int i = 0; i < pointsB.Count; i++)
                {
                    var p = pointsB[i];
                    double x = p[0];
                    double y = p[1];
                    double z = referencePlane.DistanceTo(p);

                    distances.Add(new[] { x, y, z });
                    if (z < distanceMin)
                        distanceMin = z;
                    if (z > distanceMax)
                        distanceMax = z;
                }

                parallelism = Math.Abs(distanceMax - distanceMin);

                var parallelismPoints = new List<double[]>(distances.Count);
                for (int i = 0; i < distances.Count; i++)
                {
                    var d = distances[i];
                    double x = d[0];
                    double y = d[1];
                    double z = d[2] - distanceMin;

                    parallelismPoints.Add(new[] { x, y, z });
                }

                if (_measureParam.Interpolate)
                {
                    // 生成密集点云
                    double epsilon = _measureParam.Epsilon;
                    RBF_Function function = _measureParam.Function;
                    double smooth = _measureParam.Smooth;
                    int resolution = _measureParam.Resolution;

                    RbfInterpolator rbf = new RbfInterpolator(parallelismPoints, epsilon, function, smooth);
                    parallelismPoints = rbf.GenerateDenseGrid(resolution, _measureParam.OuterRadius, _measureParam.InnerRadius);
                }

                parallelismPCD = parallelismPoints;

                return 0;
            }
            else
            {
                parallelism = -1;
                parallelismPCD = new List<double[]>();

                return -1;
            }
        }


        /// <summary>
        /// THK厚度测量算法(确保pointsA、pointsB点的X与Y坐标一一对应,且在同一坐标系)
        /// <param name="pointsA">A面点云坐标</param>
        /// <param name="pointsB">B面点云坐标</param>
        /// <param name="thicknessMin">厚度最小值</param>
        /// <param name="thicknessMax">厚度最大值</param>
        /// <param name="thicknessPCD">厚度状态点云</param>
        /// </summary>
        public int THK(List<double[]> pointsA, List<double[]> pointsB,
                        out double thicknessMin, out double thicknessMax, out List<double[]> thicknessPCD)
        {
            if (pointsA.Count > 2 && pointsB.Count > 2 && pointsA.Count == pointsB.Count)
            {
                var thicknessPoints = new List<double[]>(pointsA.Count);
                double minVal = double.MaxValue;
                double maxVal = double.MinValue;

                for (int i = 0; i < pointsA.Count; i++)
                {
                    var a = pointsA[i];
                    var b = pointsB[i];
                    if (a[0] == b[0] && a[1] == b[1])
                    {
                        double z = a[2] - b[2];
                        thicknessPoints.Add(new[] { a[0], a[1], z });
                        if (z < minVal) minVal = z;
                        if (z > maxVal) maxVal = z;
                    }
                }

                thicknessMax = maxVal;
                thicknessMin = minVal;

                if (_measureParam.Interpolate)
                {
                    // 生成密集点云
                    double epsilon = _measureParam.Epsilon;
                    RBF_Function function = _measureParam.Function;
                    double smooth = _measureParam.Smooth;
                    int resolution = _measureParam.Resolution;

                    RbfInterpolator rbf = new RbfInterpolator(thicknessPoints, epsilon, function, smooth);
                    thicknessPoints = rbf.GenerateDenseGrid(resolution, _measureParam.OuterRadius, _measureParam.InnerRadius);
                }

                thicknessPCD = thicknessPoints;

                return 0;
            }
            else
            {
                thicknessMin = -1;
                thicknessMax = -1;
                thicknessPCD = new List<double[]>();

                return -1;
            }
        }


        /// <summary>
        /// tir
        /// </summary>
        public int TIR(List<double[]> pointsA, List<double[]> pointsB, out double tir, out List<double[]> tirPCD)
        {
            if (pointsA.Count > 2 && pointsB.Count > 2 && pointsA.Count == pointsB.Count)
            {
                // 计算厚度
                List<double[]> thickness = new List<double[]>(pointsA.Count);
                for (int i = 0; i < pointsA.Count; i++)
                {
                    var a = pointsA[i];
                    var b = pointsB[i];
                    if (a[0] == b[0] && a[1] == b[1])
                    {
                        double z = Math.Abs(a[2] - b[2]);
                        thickness.Add(new[] { a[0], a[1], z });
                    }
                }
                // 拟合tir的虚拟参考面
                Plane referencePlane = FitPlane(thickness);

                List<double[]> distances = new List<double[]>(thickness.Count);
                double tirMin = double.MaxValue;
                double tirMax = double.MinValue;
                for (int i = 0; i < thickness.Count; i++)
                {
                    var p = thickness[i];
                    double x = p[0];
                    double y = p[1];
                    double z = referencePlane.AbsDistanceTo(p);

                    distances.Add(new[] { x, y, z });
                    if (z < tirMin) tirMin = z;
                    if (z > tirMax) tirMax = z;
                }

                tir = Math.Abs(tirMax) + Math.Abs(tirMin);

                var tirPoints = new List<double[]>(distances.Count);
                for (int i = 0; i < distances.Count; i++)
                {
                    var d = distances[i];
                    double x = d[0];
                    double y = d[1];
                    double z = Math.Abs(d[2] - tirMin);

                    tirPoints.Add(new[] { x, y, z });
                }

                if (_measureParam.Interpolate)
                {
                    // 生成密集点云
                    double epsilon = _measureParam.Epsilon;
                    RBF_Function function = _measureParam.Function;
                    double smooth = _measureParam.Smooth;
                    int resolution = _measureParam.Resolution;

                    RbfInterpolator rbf = new RbfInterpolator(tirPoints, epsilon, function, smooth);
                    tirPoints = rbf.GenerateDenseGrid(resolution, _measureParam.OuterRadius, _measureParam.InnerRadius);
                }

                tirPCD = tirPoints;

                return 0;

            }
            else
            {
                tir = -1;
                tirPCD = new List<double[]>();

                return -1;
            }
        }


        /// <summary>
        /// focal plane deviation(焦平面偏差)
        ///FPD=±Max(|a|, |b|)
        ///说明：
        ///1、如果|a| > |b|,FPD取正值, 反之取负值
        ///2、只计算上表面, 不关注下表面, 参考平面为上表面所有点截距之和最小的平面
        /// </summary>
        public int FPD(List<double[]> points, out double fpd)
        {
            if (points.Count > 2)
            {
                Plane referencePlane = FitPlane(points);

                double distanceMin = double.MaxValue;
                double distanceMax = double.MinValue;
                for (int i = 0; i < points.Count; i++)
                {
                    double z = referencePlane.AbsDistanceTo(points[i]);
                    if (z < distanceMin) distanceMin = z;
                    if (z > distanceMax) distanceMax = z;
                }

                if (distanceMax > distanceMin)
                {
                    fpd = Math.Max(distanceMax, distanceMin);
                }
                else
                {
                    fpd = -Math.Max(distanceMax, distanceMin);
                }

                return 0;
            }
            else
            {
                fpd = double.NegativeInfinity;

                return -1;
            }
        }


        /// <summary>
        /// TTV算法(确保pointsA、pointsB点的X与Y坐标一一对应,且在同一坐标系)
        /// <param name="pointsA">A面点云坐标</param>
        /// <param name="pointsB">B面点云坐标</param>
        /// <param name="TTV">ttv</param>
        /// <param name="ttvPCD">ttv状态点云</param>
        /// </summary>
        public int TTV(List<double[]> pointsA, List<double[]> pointsB, out double TTV, out List<double[]> ttvPCD)
        {
            if (pointsA.Count > 2 && pointsB.Count > 2 && pointsA.Count == pointsB.Count)
            {
                List<double[]> thickness = new List<double[]>(pointsA.Count);
                double thicknessMin = double.MaxValue;
                double thicknessMax = double.MinValue;

                for (int i = 0; i < pointsA.Count; i++)
                {
                    var a = pointsA[i];
                    var b = pointsB[i];
                    if (a[0] == b[0] && a[1] == b[1])
                    {
                        double z = Math.Abs(a[2] - b[2]);
                        thickness.Add(new[] { a[0], a[1], z });
                        if (z < thicknessMin) thicknessMin = z;
                        if (z > thicknessMax) thicknessMax = z;
                    }
                }

                TTV = Math.Abs(thicknessMax - thicknessMin);

                var ttvPoints = new List<double[]>(thickness.Count);
                for (int i = 0; i < thickness.Count; i++)
                {
                    var t = thickness[i];
                    double x = t[0];
                    double y = t[1];
                    double z = Math.Abs(t[2] - thicknessMin);

                    ttvPoints.Add(new[] { x, y, z });
                }

                if (_measureParam.Interpolate)
                {
                    // 生成密集点云
                    double epsilon = _measureParam.Epsilon;
                    RBF_Function function = _measureParam.Function;
                    double smooth = _measureParam.Smooth;
                    int resolution = _measureParam.Resolution;

                    RbfInterpolator rbf = new RbfInterpolator(ttvPoints, epsilon, function, smooth);
                    ttvPoints = rbf.GenerateDenseGrid(resolution, _measureParam.OuterRadius, _measureParam.InnerRadius);
                }

                ttvPCD = ttvPoints;

                return 0;
            }
            else
            {
                TTV = -1;
                ttvPCD = new List<double[]>();

                return -1;
            }

        }


        /// <summary>
        /// TTV算法
        /// <param name="thickness">各点位的厚度</param>
        /// <param name="TTV">ttv</param>
        /// <param name="ttvPCD">ttv状态点云</param>
        /// </summary>
        public int TTV(List<double[]> thickness, out double TTV, out List<double[]> ttvPCD)
        {
            if (thickness.Count > 2)
            {
                double thicknessMax = double.MinValue;
                double thicknessMin = double.MaxValue;
                for (int i = 0; i < thickness.Count; i++)
                {
                    double z = thickness[i][2];
                    if (z > thicknessMax) thicknessMax = z;
                    if (z < thicknessMin) thicknessMin = z;
                }
                TTV = Math.Abs(thicknessMax - thicknessMin);

                var ttvPoints = new List<double[]>(thickness.Count);
                for (int i = 0; i < thickness.Count; i++)
                {
                    var t = thickness[i];
                    double x = t[0];
                    double y = t[1];
                    double z = Math.Abs(t[2] - thicknessMin);

                    ttvPoints.Add(new[] { x, y, z });
                }

                if (_measureParam.Interpolate)
                {
                    // 生成密集点云
                    double epsilon = _measureParam.Epsilon;
                    RBF_Function function = _measureParam.Function;
                    double smooth = _measureParam.Smooth;
                    int resolution = _measureParam.Resolution;

                    RbfInterpolator rbf = new RbfInterpolator(ttvPoints, epsilon, function, smooth);
                    ttvPoints = rbf.GenerateDenseGrid(resolution, _measureParam.OuterRadius, _measureParam.InnerRadius);
                }

                ttvPCD = ttvPoints;

                return 0;
            }
            else
            {
                TTV = -1;
                ttvPCD = new List<double[]>();

                return -1;
            }

        }


        /// <summary>
        /// LTV算法(确保pointsA、pointsB点的X与Y坐标一一对应,且在同一坐标系)
        /// <param name="pointsA">A面点云坐标</param>
        /// <param name="pointsB">B面点云坐标</param>
        /// <param name="centerX">选择区域的圆心坐标X</param>
        /// <param name="centerY">选择区域的圆心坐标Y</param>
        /// <param name="radius">选择区域的圆半径</param>
        /// <param name="LTV">ltv</param>
        /// <param name="ltvPCD">ltv状态点云</param>
        /// </summary>
        public int LTV(List<double[]> pointsA, List<double[]> pointsB, double centerX, double centerY, double radius,
                       out double LTV, out List<double[]> ltvPCD)
        {
            if (pointsA.Count > 2 && pointsB.Count > 2 && pointsA.Count == pointsB.Count)
            {
                List<double[]> domainPoints = new List<double[]>();

                for (int i = 0; i < pointsA.Count; i++)
                {
                    var a = pointsA[i];
                    var b = pointsB[i];
                    double distance2D = Math.Sqrt(Math.Pow(a[0] - centerX, 2) + Math.Pow(a[1] - centerY, 2));
                    if (distance2D <= radius)
                    {
                        double z = Math.Abs(a[2] - b[2]);
                        domainPoints.Add(new[] { a[0], a[1], z });
                    }
                }

                if (domainPoints.Count > 2)
                {
                    int status = TTV(domainPoints, out LTV, out ltvPCD);

                    return status;
                }
                else
                {
                    LTV = -1;
                    ltvPCD = new List<double[]>();

                    return -1;
                }
            }
            else
            {
                LTV = -1;
                ltvPCD = new List<double[]>();

                return -1;
            }

        }


        /// <summary>
        /// LTV算法
        /// <param name="thickness">各点位的厚度</param>
        /// <param name="centerX">选择区域的圆心坐标X</param>
        /// <param name="centerY">选择区域的圆心坐标Y</param>
        /// <param name="radius">选择区域的圆半径</param>
        /// <param name="LTV">ltv</param>
        /// <param name="ltvPCD">ltv状态点云</param>
        /// </summary>
        public int LTV(List<double[]> thickness, double centerX, double centerY, double radius,
                       out double LTV, out List<double[]> ltvPCD)
        {
            List<double[]> domainPoints = new List<double[]>();

            for (int i = 0; i < thickness.Count; i++)
            {
                var t = thickness[i];
                double distance2D = Math.Sqrt(Math.Pow(t[0] - centerX, 2) + Math.Pow(t[1] - centerY, 2));
                if (distance2D <= radius)
                {
                    domainPoints.Add(t);
                }
            }

            if (domainPoints.Count > 2)
            {
                int status = TTV(domainPoints, out LTV, out ltvPCD);

                return status;
            }
            else
            {
                LTV = -1;
                ltvPCD = new List<double[]>();

                return -1;
            }
        }



        /// <summary>
        /// STIR算法
        /// <param name="pointsA">A面点云坐标</param>
        /// <param name="pointsB">B面点云坐标</param>
        /// <param name="centerX">选择区域的圆心坐标X</param>
        /// <param name="centerY">选择区域的圆心坐标Y</param>
        /// <param name="radius">选择区域的圆半径</param>
        /// <param name="stir">ltv</param>
        /// <param name="stirPCD">ltv状态点云</param>
        /// </summary>
        public int STIR(List<double[]> pointsA, List<double[]> pointsB, double centerX, double centerY, double radius,
                        out double stir, out List<double[]> stirPCD)
        {
            if (pointsA.Count > 2 && pointsB.Count > 2 && pointsA.Count == pointsB.Count)
            {
                List<double[]> domainPointsA = new List<double[]>();
                List<double[]> domainPointsB = new List<double[]>();

                for (int i = 0; i < pointsA.Count; i++)
                {
                    var a = pointsA[i];
                    var b = pointsB[i];
                    if (a[0] == b[0] && a[1] == b[1])
                    {
                        double distance2D = Math.Sqrt(Math.Pow(a[0] - centerX, 2) + Math.Pow(a[1] - centerY, 2));
                        if (distance2D <= radius)
                        {
                            domainPointsA.Add(a);
                            domainPointsB.Add(b);
                        }
                    }
                }

                if (domainPointsA.Count > 2 && domainPointsB.Count > 2)
                {
                    int status = TIR(domainPointsA, domainPointsB, out stir, out stirPCD);

                    return status;
                }
                else
                {
                    stir = -1;
                    stirPCD = new List<double[]>();

                    return -1;
                }
            }
            else
            {
                stir = -1;
                stirPCD = new List<double[]>();

                return -1;
            }


        }



        /// <summary>
        /// BOW算法
        /// <param name="referencePoints">晶圆最外圈采样点作为参考点</param>
        /// <param name="observationPoint">晶圆所有采样点作为观测点</param>
        /// <param name="centerPoint">晶圆中心点采样点作为bow值的测量点</param>
        /// <param name="BOW">bow</param>
        /// <param name="bowPCD">bow状态点云</param>
        /// </summary>
        public int BOW(List<double[]> referencePoints, List<double[]> observationPoint, double[] centerPoint,
                       out double BOW, out List<double[]> bowPCD)
        {
            if (referencePoints.Count > 2 && observationPoint.Count > 2)
            {
                Plane referencePlane = FitPlane(referencePoints);

                BOW = referencePlane.AbsDistanceTo(centerPoint);

                var bowPoints = new List<double[]>(observationPoint.Count);
                for (int i = 0; i < observationPoint.Count; i++)
                {
                    var p = observationPoint[i];
                    double x = p[0];
                    double y = p[1];
                    double z = referencePlane.AbsDistanceTo(p);

                    bowPoints.Add(new[] { x, y, z });
                }

                if (_measureParam.Interpolate)
                {
                    // 生成密集点云
                    double epsilon = _measureParam.Epsilon;
                    RBF_Function function = _measureParam.Function;
                    double smooth = _measureParam.Smooth;
                    int resolution = _measureParam.Resolution;

                    RbfInterpolator rbf = new RbfInterpolator(bowPoints, epsilon, function, smooth);
                    bowPoints = rbf.GenerateDenseGrid(resolution, _measureParam.OuterRadius, _measureParam.InnerRadius);
                }

                bowPCD = bowPoints;

                return 0;
            }
            else
            {
                BOW = -1;
                bowPCD = new List<double[]>();

                return -1;
            }

        }


        /// <summary>
        /// warp算法(GBT6620-2009)(确保pointsA、pointsB点的X与Y坐标一一对应,且在同一坐标系)
        /// <param name="pointsA">A面点云坐标</param>
        /// <param name="pointsB">B面点云坐标</param>
        /// <param name="warp">warp</param>
        /// <param name="warpPCD">warp状态点云</param>
        /// </summary>
        public int Warp1(List<double[]> pointsA, List<double[]> pointsB, out double warp, out List<double[]> warpPCD)
        {
            if (pointsA.Count > 2 && pointsB.Count > 2 && pointsA.Count == pointsB.Count)
            {
                List<double[]> distances = new List<double[]>(pointsA.Count);
                double distancesMin = double.MaxValue;
                double distancesMax = double.MinValue;
                for (int i = 0; i < pointsA.Count; i++)
                {
                    var a = pointsA[i];
                    var b = pointsB[i];
                    if (a[0] == b[0] && a[1] == b[1])
                    {
                        double z = -b[2] - a[2];
                        distances.Add(new[] { a[0], a[1], z });
                        if (z < distancesMin) distancesMin = z;
                        if (z > distancesMax) distancesMax = z;
                    }
                }

                warp = Math.Abs(distancesMax - distancesMin) * 0.5;

                var warpPoints = new List<double[]>(distances.Count);
                for (int i = 0; i < distances.Count; i++)
                {
                    var d = distances[i];
                    double x = d[0];
                    double y = d[1];
                    double z = Math.Abs(d[2] - distancesMin);

                    warpPoints.Add(new[] { x, y, z });
                }

                if (_measureParam.Interpolate)
                {
                    // 生成密集点云
                    double epsilon = _measureParam.Epsilon;
                    RBF_Function function = _measureParam.Function;
                    double smooth = _measureParam.Smooth;
                    int resolution = _measureParam.Resolution;

                    RbfInterpolator rbf = new RbfInterpolator(warpPoints, epsilon, function, smooth);
                    warpPoints = rbf.GenerateDenseGrid(resolution, _measureParam.OuterRadius, _measureParam.InnerRadius);
                }

                warpPCD = warpPoints;

                return 0;
            }
            else
            {
                warp = -1;
                warpPCD = new List<double[]>();

                return -1;
            }

        }


        /// <summary>
        /// warp算法(GBT32280-2022)
        /// <param name="pointsA">A面点云坐标</param>
        /// <param name="pointsB">B面点云坐标</param>
        /// <param name="warp">warp</param>
        /// <param name="warpPCD">warp状态点云</param>
        /// </summary>
        public int Warp2(List<double[]> pointsA, List<double[]> pointsB, out double warp, out List<double[]> warpPCD)
        {
            if (pointsA.Count > 2 && pointsB.Count > 2 && pointsA.Count == pointsB.Count)
            {
                int ret = CalculateMedianPointSet(pointsA, pointsB, out List<double[]> points);

                Plane referencePlane = FitPlane(points);

                List<double[]> distances = new List<double[]>(points.Count);
                double distancesMin = double.MaxValue;
                double distancesMax = double.MinValue;
                for (int i = 0; i < points.Count; i++)
                {
                    var p = points[i];
                    double x = p[0];
                    double y = p[1];
                    double z = referencePlane.AbsDistanceTo(p);

                    distances.Add(new[] { x, y, z });
                    if (z < distancesMin) distancesMin = z;
                    if (z > distancesMax) distancesMax = z;
                }

                warp = Math.Abs(distancesMax - distancesMin);

                var warpPoints = new List<double[]>(distances.Count);
                for (int i = 0; i < distances.Count; i++)
                {
                    var d = distances[i];
                    double x = d[0];
                    double y = d[1];
                    double z = Math.Abs(d[2] - distancesMin);
                    warpPoints.Add(new[] { x, y, z });
                }

                if (_measureParam.Interpolate)
                {
                    // 生成密集点云
                    double epsilon = _measureParam.Epsilon;
                    RBF_Function function = _measureParam.Function;
                    double smooth = _measureParam.Smooth;
                    int resolution = _measureParam.Resolution;

                    RbfInterpolator rbf = new RbfInterpolator(warpPoints, epsilon, function, smooth);
                    warpPoints = rbf.GenerateDenseGrid(resolution, _measureParam.OuterRadius, _measureParam.InnerRadius);
                }

                warpPCD = warpPoints;

                return 0;
            }
            else
            {
                warp = -1;
                warpPCD = new List<double[]>();

                return -1;
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

            /// <summary>
            /// 根据输入的X、Y坐标计算平面上的Z值
            /// 平面方程：Ax + By + Cz + D = 0
            /// </summary>
            public double GetZ(double x, double y)
            {
                if (Math.Abs(C) < 1e-12)
                    throw new InvalidOperationException("Plane C is too close to zero, cannot solve z from x and y.");

                return -(A * x + B * y + D) / C;
            }

            public double AbsDistanceTo(double[] point)
            {
                double x = point[0];
                double y = point[1];
                double z = point[2];
                return Math.Abs(A * x + B * y + C * z + D) / Math.Sqrt(A * A + B * B + C * C);
            }

            public double DistanceTo(double[] point)
            {
                double x = point[0];
                double y = point[1];
                double z = point[2];
                return (A * x + B * y + C * z + D) / Math.Sqrt(A * A + B * B + C * C);
            }
        }


        public class RbfInterpolator
        {
            private readonly double[,] points;
            private readonly Vector<double> weights;
            private readonly Func<double, double> phi;

            public RbfInterpolator(List<double[]> inputPoints, double epsilon = 1.0, RBF_Function function = RBF_Function.quintic, double smooth = 1e-6)
            {
                int n = inputPoints.Count;
                points = new double[n, 3];
                for (int i = 0; i < n; i++)
                {
                    var p = inputPoints[i];
                    points[i, 0] = p[0];
                    points[i, 1] = p[1];
                    points[i, 2] = p[2];
                }

                // 选择核函数
                if (function == RBF_Function.gaussian)
                    phi = (r) => Math.Exp(-(epsilon * r) * (epsilon * r));
                else if (function == RBF_Function.multiquadric)
                    phi = (r) => Math.Sqrt((r / epsilon) * (r / epsilon) + 1);
                else if (function == RBF_Function.inverse)
                    phi = (r) => 1.0 / Math.Sqrt((r / epsilon) * (r / epsilon) + 1);
                else if (function == RBF_Function.linear)
                    phi = (r) => r;
                else if (function == RBF_Function.cubic)
                    phi = (r) => r * r * r;
                else if (function == RBF_Function.quintic)
                    phi = (r) => Math.Pow(r, 5);
                else if (function == RBF_Function.thin_plate)
                    phi = (r) => r * r * Math.Log(r + 1e-8);
                else
                    throw new ArgumentException($"Unsupported RBF function: {function}");

                var A = Matrix<double>.Build.Dense(n, n);
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        double dx = points[i, 0] - points[j, 0];
                        double dy = points[i, 1] - points[j, 1];
                        double r = Math.Sqrt(dx * dx + dy * dy);
                        A[i, j] = phi(r);
                    }
                }

                // 添加正则化项，防止矩阵病态
                for (int i = 0; i < n; i++)
                {
                    A[i, i] += smooth;
                }

                var z = Vector<double>.Build.Dense(n);
                for (int i = 0; i < n; i++)
                {
                    z[i] = points[i, 2];
                }

                weights = A.Solve(z);
            }

            public double Predict(double x, double y)
            {
                int n = weights.Count;
                double sum = 0.0;
                for (int i = 0; i < n; i++)
                {
                    double dx = x - points[i, 0];
                    double dy = y - points[i, 1];
                    double r = Math.Sqrt(dx * dx + dy * dy);
                    sum += weights[i] * phi(r);
                }
                return sum;
            }

            /// <summary>
            /// 批量预测多个点的Z值（MathNet优化版本）
            /// </summary>
            /// <param name="xValues">X坐标数组</param>
            /// <param name="yValues">Y坐标数组</param>
            /// <returns>预测的Z值数组</returns>
            public double[] PredictBatch(double[] xValues, double[] yValues)
            {
                if (xValues.Length != yValues.Length)
                    throw new ArgumentException("xValues and yValues must have the same length");

                int batchSize = xValues.Length;
                int n = weights.Count;

                // 构建RBF核矩阵 [batchSize × n]
                var kernelMatrix = Matrix<double>.Build.Dense(batchSize, n);

                // 并行计算距离和核函数值
                System.Threading.Tasks.Parallel.For(0, batchSize, idx =>
                {
                    double x = xValues[idx];
                    double y = yValues[idx];

                    for (int i = 0; i < n; i++)
                    {
                        double dx = x - points[i, 0];
                        double dy = y - points[i, 1];
                        double r = Math.Sqrt(dx * dx + dy * dy);
                        kernelMatrix[idx, i] = phi(r);
                    }
                });

                // 矩阵-向量乘法：[batchSize × n] × [n × 1] = [batchSize × 1]
                // 利用MKL加速的BLAS运算
                var results = kernelMatrix.Multiply(weights);

                return results.ToArray();
            }

            /// <summary>
            /// 生成稠密点云（支持圆形/圆环区域）
            /// </summary>
            /// <param name="resolution">网格分辨率</param>
            /// <param name="outerRadius">外半径（null时自动计算）</param>
            /// <param name="innerRadius">内半径（null时为圆形区域，非null时为圆环区域）</param>
            /// <param name="centerX">圆心X坐标（null时自动计算）</param>
            /// <param name="centerY">圆心Y坐标（null时自动计算）</param>
            /// <param name="batchSize">批处理大小（用于分批插值，避免内存溢出）</param>
            /// <returns>稠密点云列表</returns>
            public List<double[]> GenerateDenseGrid(
                int resolution = 100,
                double? outerRadius = null,
                double? innerRadius = null,
                double? centerX = null,
                double? centerY = null,
                int batchSize = 15000)
            {
                int n = points.GetLength(0);

                // 使用向量化方式计算边界（MathNet优化）
                double minX = double.MaxValue, minY = double.MaxValue;
                double maxX = double.MinValue, maxY = double.MinValue;

                for (int i = 0; i < n; i++)
                {
                    double x = points[i, 0];
                    double y = points[i, 1];
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }

                // 计算圆心
                double cx = centerX ?? (minX + maxX) / 2.0;
                double cy = centerY ?? (minY + maxY) / 2.0;

                // 计算外半径
                double r = outerRadius ?? Math.Min(maxX - cx, maxY - cy);
                double rSq = r * r;
                double innerRSq = innerRadius.HasValue ? innerRadius.Value * innerRadius.Value : 0.0;

                // 构建正方形网格范围
                double gridMin = Math.Min(minX, minY);
                double gridMax = Math.Max(maxX, maxY);
                double step = (resolution > 1) ? (gridMax - gridMin) / (resolution - 1) : 0.0;

                // 第一步：并行筛选出圆形/圆环区域内的点
                var gridPoints = new System.Collections.Concurrent.ConcurrentBag<(double x, double y)>();

                System.Threading.Tasks.Parallel.For(0, resolution, i =>
                {
                    double xi = gridMin + i * step;
                    for (int j = 0; j < resolution; j++)
                    {
                        double yi = gridMin + j * step;
                        double dx = xi - cx;
                        double dy = yi - cy;
                        double distSq = dx * dx + dy * dy;

                        // 判断是否在圆形/圆环区域内
                        if (innerRadius.HasValue)
                        {
                            // 圆环区域：内半径 <= r <= 外半径
                            if (distSq <= rSq && distSq >= innerRSq)
                            {
                                gridPoints.Add((xi, yi));
                            }
                        }
                        else
                        {
                            // 圆形区域：r <= 外半径
                            if (distSq <= rSq)
                            {
                                gridPoints.Add((xi, yi));
                            }
                        }
                    }
                });

                // 转换为列表以便后续处理
                var gridPointsList = gridPoints.ToList();
                int totalPoints = gridPointsList.Count;
                var densePoints = new List<double[]>(totalPoints);

                // 第二步：分批插值（使用优化的PredictBatch）
                if (totalPoints <= batchSize)
                {
                    // 小数据量，使用批量预测
                    double[] xBatch = new double[totalPoints];
                    double[] yBatch = new double[totalPoints];

                    for (int i = 0; i < totalPoints; i++)
                    {
                        var pt = gridPointsList[i];
                        xBatch[i] = pt.x;
                        yBatch[i] = pt.y;
                    }

                    // 批量预测（利用MathNet矩阵运算）
                    double[] zBatch = PredictBatch(xBatch, yBatch);

                    for (int i = 0; i < totalPoints; i++)
                    {
                        densePoints.Add(new[] { xBatch[i], yBatch[i], zBatch[i] });
                    }
                }
                else
                {
                    // 大数据量，分批处理
                    int numBatches = (totalPoints + batchSize - 1) / batchSize;

                    for (int batchIdx = 0; batchIdx < numBatches; batchIdx++)
                    {
                        int startIdx = batchIdx * batchSize;
                        int endIdx = Math.Min((batchIdx + 1) * batchSize, totalPoints);
                        int currentBatchSize = endIdx - startIdx;

                        // 准备批量数据
                        double[] xBatch = new double[currentBatchSize];
                        double[] yBatch = new double[currentBatchSize];

                        for (int i = 0; i < currentBatchSize; i++)
                        {
                            var pt = gridPointsList[startIdx + i];
                            xBatch[i] = pt.x;
                            yBatch[i] = pt.y;
                        }

                        // 批量预测（利用MathNet矩阵运算）
                        double[] zBatch = PredictBatch(xBatch, yBatch);

                        // 添加到结果列表
                        for (int i = 0; i < currentBatchSize; i++)
                        {
                            densePoints.Add(new[] { xBatch[i], yBatch[i], zBatch[i] });
                        }
                    }
                }

                return densePoints;
            }
        }
    }


    /// <summary>
    /// 径向基核函
    /// </summary>
    public enum RBF_Function
    {
        gaussian,
        multiquadric,
        inverse,
        linear,
        cubic,
        quintic,
        thin_plate
    }


    /// <summary>
    /// 算法配置参数
    /// </summary>
    [Serializable]
    public class Flatness_MeasureParam
    {
        // 点云插值参数
        private bool _interpolate = true;                          //是否进行插值
        private int _resolution = 600;                             //插值分辨率
        private RBF_Function _function = RBF_Function.thin_plate;  //径向基核函数
        private double _epsilon = 0.1;                             //径向基核函数参数
        private double _smooth = 0.1;                              //平滑系数
        private double? _outerRadius = null;                       //插值外半径（null时自动计算）
        private double? _innerRadius = null;                       //插值内半径（null时为圆形区域）

        /// <summary>
        /// 是否进行插值
        /// </summary>
        public bool Interpolate
        {
            get { return _interpolate; }
            set { _interpolate = value; }
        }

        /// <summary>
        /// 插值分辨率
        /// </summary>
        public int Resolution
        {
            get { return _resolution; }
            set { _resolution = value; }
        }

        /// <summary>
        /// 径向基核函数
        /// </summary>
        public RBF_Function Function
        {
            get { return _function; }
            set { _function = value; }
        }

        /// <summary>
        /// 径向基核函数参数
        /// </summary>
        public double Epsilon
        {
            get { return _epsilon; }
            set { _epsilon = value; }
        }

        /// <summary>
        /// 平滑系数
        /// </summary>
        public double Smooth
        {
            get { return _smooth; }
            set { _smooth = value; }
        }

        /// <summary>
        /// 插值外半径（null时自动计算）
        /// </summary>
        public double? OuterRadius
        {
            get { return _outerRadius; }
            set { _outerRadius = value; }
        }

        /// <summary>
        /// 插值内半径（null时为圆形区域）
        /// </summary>
        public double? InnerRadius
        {
            get { return _innerRadius; }
            set { _innerRadius = value; }
        }


    }


}
