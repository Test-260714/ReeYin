using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Custom.WaferFlatnessMeasure
{
    public class FlatCalib_Algorithm
    {
        private const double NumericTolerance = 1e-8;

        private readonly FlatCalib_MeasureParam _measureParam;
        private readonly Flatness_Algorithm _planeFitter;
        private List<double[]> _rawCalibrationPoints = new();
        private List<double[]> _residualMapPoints = new();
        private double[]? _xAxis;
        private double[]? _yAxis;
        private double[,]? _residualGrid;
        private FlatCalibBoundary? _boundary;

        public bool IsReady => _xAxis != null && _yAxis != null && _residualGrid != null && _boundary != null;



        public FlatCalib_Algorithm(FlatCalib_MeasureParam measureParam)
        {
            _measureParam = measureParam ?? throw new ArgumentNullException(nameof(measureParam));

            _planeFitter = new Flatness_Algorithm(new Flatness_MeasureParam{Interpolate = false});
        }


        /// <summary>
        /// 根据标准平面采样点创建标定模型。
        /// </summary>
        public int CreateCalibrationModel(List<double[]> rawCalibrationPoints,
                                          string rawPointCloudPlyPath,
                                          string residualPointCloudPlyPath)
        {
            ClearModel();

            try
            {
                ArgumentNullException.ThrowIfNull(rawCalibrationPoints);

                (List<double[]> normalizedRawPoints, List<double[]> residualPoints) =
                    BuildCalibrationModelFromRawPoints(rawCalibrationPoints, nameof(rawCalibrationPoints));

                InitializeCalibrationModel(normalizedRawPoints, residualPoints);

                if (!string.IsNullOrWhiteSpace(rawPointCloudPlyPath) || !string.IsNullOrWhiteSpace(residualPointCloudPlyPath))
                {
                    ExportCalibrationPointClouds(rawPointCloudPlyPath, residualPointCloudPlyPath);
                }

                return 0;
            }
            catch (Exception ex)
            {
                ClearModel();

                Console.WriteLine($"Failed to create calibration model: {ex.Message}", ex);

                return -1;
            }
        }


        /// <summary>
        /// 加载标定模型。
        /// </summary>
        public int LoadCalibrationModel(string rawPointCloudPlyPath, string residualPointCloudPlyPath)
        {
            ClearModel();

            try
            {
                bool hasRawPointCloud = !string.IsNullOrWhiteSpace(rawPointCloudPlyPath);
                bool hasResidualPointCloud = !string.IsNullOrWhiteSpace(residualPointCloudPlyPath);

                if (!hasRawPointCloud && !hasResidualPointCloud)
                {
                    throw new ArgumentException("At least one calibration point cloud path must be provided.");
                }

                List<double[]>? rawPoints = hasRawPointCloud
                    ? ReadPlyPoints(rawPointCloudPlyPath, nameof(rawPointCloudPlyPath))
                    : null;
                List<double[]>? residualPoints = hasResidualPointCloud
                    ? ReadPlyPoints(residualPointCloudPlyPath, nameof(residualPointCloudPlyPath))
                    : null;

                if (rawPoints != null)
                {
                    (List<double[]> normalizedRawPoints, List<double[]> computedResidualPoints) =
                        BuildCalibrationModelFromRawPoints(rawPoints, nameof(rawPointCloudPlyPath));

                    if (residualPoints != null)
                    {
                        ValidateResidualPointCloud(computedResidualPoints, residualPoints);
                        InitializeCalibrationModel(normalizedRawPoints, residualPoints);
                    }
                    else
                    {
                        InitializeCalibrationModel(normalizedRawPoints, computedResidualPoints);
                    }
                }
                else
                {
                    InitializeCalibrationModel(new List<double[]>(), residualPoints!);
                }

                return 0;
            }
            catch (Exception ex)
            {
                ClearModel();

                Console.WriteLine($"Failed to load calibration model: {ex.Message}", ex);

                return -1;
            }
        }


        public void ExportCalibrationPointClouds(string? rawPointCloudPlyPath, string? residualPointCloudPlyPath)
        {
            if (!IsReady)
                throw new InvalidOperationException("Calibration model is not ready.");

            if (!string.IsNullOrWhiteSpace(rawPointCloudPlyPath))
            {
                WritePlyAscii(rawPointCloudPlyPath, _rawCalibrationPoints);
            }

            if (!string.IsNullOrWhiteSpace(residualPointCloudPlyPath))
            {
                WritePlyAscii(residualPointCloudPlyPath, _residualMapPoints);
            }
        }


        /// <summary>
        /// 查询单个 XY 位置对应的补偿值。
        /// </summary>
        public bool TryGetCompensationValue(double x, double y, out double compensation)
        {
            FlatCalibPointQueryDetail detail;

            compensation = 0;
            detail = BuildNotReadyDetail(x, y);

            if (!IsReady || _xAxis == null || _yAxis == null || _residualGrid == null || _boundary == null)
            {
                Console.WriteLine($"Failed to query compensation value: {detail.FailureReason}");

                return false;
            }

            detail = ResolveQueryPoint(x, y, _boundary, _measureParam.BoundaryToleranceUm);
            if (!detail.WithinAllowedBoundary)
            {
                Console.WriteLine($"Failed to query compensation value: {detail.FailureReason}");

                return false;
            }

            compensation = BilinearInterpolate(detail.EffectiveX, detail.EffectiveY, _xAxis, _yAxis, _residualGrid);
            return true;
        }


        /// <summary>
        /// 检测前先调用获取结果，true在执行
        /// 对一批测量点执行补偿。
        /// 'Formal'模式在遇到首个超范围点时立即失败。
        /// 'Diagnostic'模式会跳过超范围点，并返回降级结果。
        /// </summary>
        public FlatCalibCompensationResult CompensatePoints(List<double[]> rawMeasurePoints, 
                                                            FlatCalibCompensationMode mode = FlatCalibCompensationMode.Formal)
        {
            if (!IsReady)
            {
                return new FlatCalibCompensationResult
                {
                    Success = false
                };
            }

            try
            {
                ArgumentNullException.ThrowIfNull(rawMeasurePoints);

                List<double[]> inputPoints = ClonePoints(rawMeasurePoints, nameof(rawMeasurePoints));
                var compensatedPoints = new List<double[]>(inputPoints.Count);

                for (int index = 0; index < inputPoints.Count; index++)
                {
                    double[] point = inputPoints[index];
                    if (!TryGetCompensationValue(point[0], point[1], out double compensation))
                    {
                        if (mode == FlatCalibCompensationMode.Formal)
                        {
                            return new FlatCalibCompensationResult
                            {
                                Success = false
                            };
                        }

                        continue;
                    }

                    compensatedPoints.Add(new[]
                    {
                        point[0],
                        point[1],
                        point[2] - compensation
                    });
                }

                if (compensatedPoints.Count < 3)
                {
                    return new FlatCalibCompensationResult
                    {
                        Success = false
                    };
                }

                return new FlatCalibCompensationResult
                {
                    Success = true,
                    IsDegraded = mode == FlatCalibCompensationMode.Diagnostic,
                    CompensatedPoints = compensatedPoints,
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to compensate points: {ex.Message}", ex);

                return new FlatCalibCompensationResult
                {
                    Success = false,
                };
            }
        }


        private static FlatCalibPointQueryDetail BuildNotReadyDetail(double x, double y)
        {
            return new FlatCalibPointQueryDetail
            {
                OriginalX = x,
                OriginalY = y,
                EffectiveX = x,
                EffectiveY = y,
                FailureReason = "Calibration model is not ready."
            };
        }

        private static List<double[]> BuildResidualMapPoints(
            IEnumerable<double[]> rawCalibrationPoints,
            Flatness_Algorithm.Plane plane)
        {
            var residualPoints = new List<double[]>();
            foreach (double[] point in rawCalibrationPoints)
            {
                double x = point[0];
                double y = point[1];
                double residual = point[2] - plane.GetZ(x, y);
                residualPoints.Add(new[] { x, y, residual });
            }

            return residualPoints;
        }

        private (List<double[]> RawPoints, List<double[]> ResidualPoints) BuildCalibrationModelFromRawPoints(
            IEnumerable<double[]> rawCalibrationPoints,
            string parameterName)
        {
            List<double[]> normalizedRawPoints = ClonePoints(rawCalibrationPoints, parameterName);
            if (normalizedRawPoints.Count < 4)
            {
                throw new InvalidOperationException("Calibration requires at least 4 points.");
            }

            Flatness_Algorithm.Plane plane = _planeFitter.FitPlane(normalizedRawPoints);
            if (Math.Abs(plane.C) < NumericTolerance)
            {
                throw new InvalidOperationException("The fitted reference plane cannot solve z from x/y.");
            }

            List<double[]> residualPoints = BuildResidualMapPoints(normalizedRawPoints, plane);
            return (normalizedRawPoints, residualPoints);
        }

        private void InitializeCalibrationModel(
            List<double[]> rawCalibrationPoints,
            List<double[]> residualPoints)
        {
            ArgumentNullException.ThrowIfNull(rawCalibrationPoints);
            ArgumentNullException.ThrowIfNull(residualPoints);

            if (residualPoints.Count < 4)
            {
                throw new InvalidOperationException("Calibration requires at least 4 points.");
            }

            BuildGrid(residualPoints, out double[] xAxis, out double[] yAxis, out double[,] residualGrid);

            _rawCalibrationPoints = rawCalibrationPoints;
            _residualMapPoints = residualPoints;
            _xAxis = xAxis;
            _yAxis = yAxis;
            _residualGrid = residualGrid;
            _boundary = new FlatCalibBoundary
            {
                MinX = _xAxis[0],
                MaxX = _xAxis[^1],
                MinY = _yAxis[0],
                MaxY = _yAxis[^1]
            };
        }

        private void ValidateResidualPointCloud(
            List<double[]> expectedResidualPoints,
            List<double[]> loadedResidualPoints)
        {
            BuildGrid(expectedResidualPoints, out double[] expectedXAxis, out double[] expectedYAxis, out double[,] expectedGrid);
            BuildGrid(loadedResidualPoints, out double[] loadedXAxis, out double[] loadedYAxis, out double[,] loadedGrid);

            if (expectedXAxis.Length != loadedXAxis.Length || expectedYAxis.Length != loadedYAxis.Length)
            {
                throw new InvalidDataException("Residual point cloud does not match the raw calibration grid.");
            }

            for (int index = 0; index < expectedXAxis.Length; index++)
            {
                if (Math.Abs(expectedXAxis[index] - loadedXAxis[index]) > _measureParam.CoordinateMatchToleranceUm)
                {
                    throw new InvalidDataException("Residual point cloud X coordinates do not match the raw calibration grid.");
                }
            }

            for (int index = 0; index < expectedYAxis.Length; index++)
            {
                if (Math.Abs(expectedYAxis[index] - loadedYAxis[index]) > _measureParam.CoordinateMatchToleranceUm)
                {
                    throw new InvalidDataException("Residual point cloud Y coordinates do not match the raw calibration grid.");
                }
            }

            for (int row = 0; row < expectedYAxis.Length; row++)
            {
                for (int column = 0; column < expectedXAxis.Length; column++)
                {
                    if (!AreNearlyEqual(expectedGrid[row, column], loadedGrid[row, column]))
                    {
                        throw new InvalidDataException("Residual point cloud values do not match the raw calibration points.");
                    }
                }
            }
        }

        private void BuildGrid(
            List<double[]> residualPoints,
            out double[] xAxis,
            out double[] yAxis,
            out double[,] residualGrid)
        {
            List<double> xValues = BuildAxisValues(residualPoints.Select(point => point[0]), _measureParam.CoordinateMatchToleranceUm);
            List<double> yValues = BuildAxisValues(residualPoints.Select(point => point[1]), _measureParam.CoordinateMatchToleranceUm);

            if (xValues.Count < 2 || yValues.Count < 2)
            {
                throw new InvalidOperationException("Calibration points must form at least a 2 x 2 grid.");
            }

            if (xValues.Count * yValues.Count != residualPoints.Count)
            {
                throw new InvalidOperationException("Calibration points do not form a complete rectangular grid.");
            }

            xAxis = xValues.ToArray();
            yAxis = yValues.ToArray();
            residualGrid = new double[yAxis.Length, xAxis.Length];
            bool[,] assigned = new bool[yAxis.Length, xAxis.Length];

            foreach (double[] point in residualPoints)
            {
                int xIndex = FindAxisIndex(xAxis, point[0], _measureParam.CoordinateMatchToleranceUm);
                int yIndex = FindAxisIndex(yAxis, point[1], _measureParam.CoordinateMatchToleranceUm);

                if (assigned[yIndex, xIndex])
                {
                    throw new InvalidOperationException("Calibration grid contains duplicate XY positions.");
                }

                residualGrid[yIndex, xIndex] = point[2];
                assigned[yIndex, xIndex] = true;
            }

            for (int row = 0; row < yAxis.Length; row++)
            {
                for (int column = 0; column < xAxis.Length; column++)
                {
                    if (!assigned[row, column])
                    {
                        throw new InvalidOperationException("Calibration grid contains missing XY positions.");
                    }
                }
            }
        }

        private static FlatCalibPointQueryDetail ResolveQueryPoint(
            double x,
            double y,
            FlatCalibBoundary boundary,
            double boundaryToleranceUm)
        {
            double effectiveX = Math.Clamp(x, boundary.MinX, boundary.MaxX);
            double effectiveY = Math.Clamp(y, boundary.MinY, boundary.MaxY);
            double exceedXUm = CalculateExceedDistance(x, boundary.MinX, boundary.MaxX);
            double exceedYUm = CalculateExceedDistance(y, boundary.MinY, boundary.MaxY);
            bool withinAllowedBoundary = exceedXUm <= boundaryToleranceUm && exceedYUm <= boundaryToleranceUm;

            return new FlatCalibPointQueryDetail
            {
                OriginalX = x,
                OriginalY = y,
                EffectiveX = effectiveX,
                EffectiveY = effectiveY,
                ExceedXUm = exceedXUm,
                ExceedYUm = exceedYUm,
                WasBoundaryClamped = (effectiveX != x) || (effectiveY != y),
                WithinAllowedBoundary = withinAllowedBoundary,
                FailureReason = withinAllowedBoundary ? string.Empty : "Point exceeds the calibration boundary tolerance."
            };
        }

        private static double BilinearInterpolate(
            double x,
            double y,
            IReadOnlyList<double> xAxis,
            IReadOnlyList<double> yAxis,
            double[,] residualGrid)
        {
            int x0Index = FindCellLowerIndex(xAxis, x);
            int y0Index = FindCellLowerIndex(yAxis, y);
            int x1Index = x0Index + 1;
            int y1Index = y0Index + 1;

            double x0 = xAxis[x0Index];
            double x1 = xAxis[x1Index];
            double y0 = yAxis[y0Index];
            double y1 = yAxis[y1Index];

            double tx = Math.Abs(x1 - x0) < NumericTolerance ? 0 : (x - x0) / (x1 - x0);
            double ty = Math.Abs(y1 - y0) < NumericTolerance ? 0 : (y - y0) / (y1 - y0);

            double z00 = residualGrid[y0Index, x0Index];
            double z10 = residualGrid[y0Index, x1Index];
            double z01 = residualGrid[y1Index, x0Index];
            double z11 = residualGrid[y1Index, x1Index];

            double z0 = z00 + (z10 - z00) * tx;
            double z1 = z01 + (z11 - z01) * tx;
            return z0 + (z1 - z0) * ty;
        }

        private static int FindCellLowerIndex(IReadOnlyList<double> axis, double value)
        {
            if (axis.Count < 2)
                throw new InvalidOperationException("Axis must contain at least 2 values.");

            if (value <= axis[0])
                return 0;

            if (value >= axis[^1])
                return axis.Count - 2;

            int lower = 0;
            int upper = axis.Count - 1;
            while (upper - lower > 1)
            {
                int middle = lower + (upper - lower) / 2;
                if (axis[middle] <= value)
                {
                    lower = middle;
                }
                else
                {
                    upper = middle;
                }
            }

            return lower;
        }

        private static int FindAxisIndex(IReadOnlyList<double> axis, double value, double tolerance)
        {
            if (axis.Count == 0)
                throw new InvalidOperationException("Axis cannot be empty.");

            int lower = 0;
            int upper = axis.Count - 1;
            while (lower <= upper)
            {
                int middle = lower + (upper - lower) / 2;
                double current = axis[middle];

                if (Math.Abs(current - value) <= tolerance)
                    return middle;

                if (current < value)
                {
                    lower = middle + 1;
                }
                else
                {
                    upper = middle - 1;
                }
            }

            throw new InvalidOperationException($"Failed to match grid axis value: {value.ToString("G17", CultureInfo.InvariantCulture)}.");
        }

        private static List<double> BuildAxisValues(IEnumerable<double> values, double tolerance)
        {
            List<double> sortedValues = values.OrderBy(value => value).ToList();
            if (sortedValues.Count == 0)
                throw new InvalidOperationException("Axis values cannot be empty.");

            var axisValues = new List<double>();
            double clusterStart = sortedValues[0];
            double clusterSum = sortedValues[0];
            int clusterCount = 1;

            for (int index = 1; index < sortedValues.Count; index++)
            {
                double current = sortedValues[index];
                if (Math.Abs(current - clusterStart) <= tolerance)
                {
                    clusterSum += current;
                    clusterCount++;
                    continue;
                }

                axisValues.Add(clusterSum / clusterCount);
                clusterStart = current;
                clusterSum = current;
                clusterCount = 1;
            }

            axisValues.Add(clusterSum / clusterCount);
            return axisValues;
        }

        private static List<double[]> ClonePoints(IEnumerable<double[]> points, string parameterName)
        {
            var clonedPoints = new List<double[]>();
            int index = 0;

            foreach (double[]? point in points)
            {
                if (point == null || point.Length < 3)
                {
                    throw new ArgumentException($"Point at index {index} must contain at least 3 values.", parameterName);
                }

                if (!double.IsFinite(point[0]) || !double.IsFinite(point[1]) || !double.IsFinite(point[2]))
                {
                    throw new ArgumentException($"Point at index {index} contains non-finite values.", parameterName);
                }

                clonedPoints.Add(new[] { point[0], point[1], point[2] });
                index++;
            }

            if (clonedPoints.Count == 0)
            {
                throw new ArgumentException("Point collection cannot be empty.", parameterName);
            }

            return clonedPoints;
        }

        private static List<double[]> ReadPlyPoints(string path, string parameterName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path, parameterName);

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Calibration point cloud file was not found.", path);
            }

            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(stream, Encoding.ASCII, false, 4096, leaveOpen: true);

            PlyHeaderInfo header = ParsePlyHeader(reader);
            if (!string.Equals(header.Format, "ascii", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException("Only ASCII PLY point clouds are supported.");
            }

            var points = new List<double[]>(header.VertexCount);
            int maxIndex = Math.Max(header.XIndex, Math.Max(header.YIndex, header.ZIndex));

            while (points.Count < header.VertexCount)
            {
                string? line = reader.ReadLine();
                if (line == null)
                {
                    throw new EndOfStreamException("PLY vertex data ended before the declared vertex count was reached.");
                }

                string trimmed = line.Trim();
                if (trimmed.Length == 0)
                {
                    continue;
                }

                string[] tokens = SplitWhitespace(trimmed);
                if (tokens.Length <= maxIndex)
                {
                    throw new InvalidDataException($"PLY vertex record {points.Count} does not contain x/y/z.");
                }

                points.Add(new[]
                {
                    ParseFiniteDouble(tokens[header.XIndex], path),
                    ParseFiniteDouble(tokens[header.YIndex], path),
                    ParseFiniteDouble(tokens[header.ZIndex], path)
                });
            }

            return ClonePoints(points, parameterName);
        }

        private static PlyHeaderInfo ParsePlyHeader(StreamReader reader)
        {
            string? line = reader.ReadLine();
            if (!string.Equals(line?.Trim(), "ply", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("The file is not a valid PLY point cloud.");
            }

            string format = string.Empty;
            int vertexCount = -1;
            int xIndex = -1;
            int yIndex = -1;
            int zIndex = -1;
            int propertyIndex = 0;
            bool inVertexElement = false;

            while (true)
            {
                line = reader.ReadLine();
                if (line == null)
                {
                    throw new EndOfStreamException("PLY header ended unexpectedly.");
                }

                string trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith("comment", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.Equals(trimmed, "end_header", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                string[] tokens = SplitWhitespace(trimmed);
                if (tokens.Length == 0)
                {
                    continue;
                }

                if (string.Equals(tokens[0], "format", StringComparison.OrdinalIgnoreCase))
                {
                    if (tokens.Length < 2)
                    {
                        throw new InvalidDataException("PLY format header is invalid.");
                    }

                    format = tokens[1];
                    continue;
                }

                if (string.Equals(tokens[0], "element", StringComparison.OrdinalIgnoreCase))
                {
                    if (tokens.Length < 3)
                    {
                        throw new InvalidDataException("PLY element header is invalid.");
                    }

                    inVertexElement = string.Equals(tokens[1], "vertex", StringComparison.OrdinalIgnoreCase);
                    propertyIndex = 0;

                    if (inVertexElement &&
                        !int.TryParse(tokens[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out vertexCount))
                    {
                        throw new InvalidDataException("PLY vertex count is invalid.");
                    }

                    continue;
                }

                if (string.Equals(tokens[0], "property", StringComparison.OrdinalIgnoreCase))
                {
                    if (!inVertexElement)
                    {
                        continue;
                    }

                    if (tokens.Length < 3 || string.Equals(tokens[1], "list", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidDataException("PLY vertex property definition is invalid.");
                    }

                    string propertyName = tokens[2];
                    if (string.Equals(propertyName, "x", StringComparison.OrdinalIgnoreCase))
                    {
                        xIndex = propertyIndex;
                    }
                    else if (string.Equals(propertyName, "y", StringComparison.OrdinalIgnoreCase))
                    {
                        yIndex = propertyIndex;
                    }
                    else if (string.Equals(propertyName, "z", StringComparison.OrdinalIgnoreCase))
                    {
                        zIndex = propertyIndex;
                    }

                    propertyIndex++;
                }
            }

            if (vertexCount < 0)
            {
                throw new InvalidDataException("PLY header does not define a valid vertex count.");
            }

            if (xIndex < 0 || yIndex < 0 || zIndex < 0)
            {
                throw new InvalidDataException("PLY vertex properties must include x, y, and z.");
            }

            return new PlyHeaderInfo(format, vertexCount, xIndex, yIndex, zIndex);
        }

        private static string[] SplitWhitespace(string value)
        {
            return value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        }

        private static double ParseFiniteDouble(string token, string path)
        {
            if (!double.TryParse(token, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double value))
            {
                throw new InvalidDataException($"PLY contains an invalid numeric value: {token} ({path}).");
            }

            if (!double.IsFinite(value))
            {
                throw new InvalidDataException($"PLY contains a non-finite numeric value: {token} ({path}).");
            }

            return value;
        }

        private static bool AreNearlyEqual(double left, double right)
        {
            double diff = Math.Abs(left - right);
            double scale = Math.Max(1d, Math.Max(Math.Abs(left), Math.Abs(right)));
            return diff <= NumericTolerance * scale * 10;
        }

        private static double CalculateExceedDistance(double value, double min, double max)
        {
            if (value < min)
                return min - value;

            if (value > max)
                return value - max;

            return 0;
        }

        private void ClearModel()
        {
            _rawCalibrationPoints = new List<double[]>();
            _residualMapPoints = new List<double[]>();
            _xAxis = null;
            _yAxis = null;
            _residualGrid = null;
            _boundary = null;
        }

        private static void WritePlyAscii(string path, IReadOnlyList<double[]> points)
        {
            string? directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var writer = new StreamWriter(path, false, Encoding.ASCII);
            writer.WriteLine("ply");
            writer.WriteLine("format ascii 1.0");
            writer.WriteLine($"element vertex {points.Count}");
            writer.WriteLine("property float x");
            writer.WriteLine("property float y");
            writer.WriteLine("property float z");
            writer.WriteLine("end_header");

            foreach (double[] point in points)
            {
                writer.WriteLine(
                    $"{point[0].ToString("G17", CultureInfo.InvariantCulture)} {point[1].ToString("G17", CultureInfo.InvariantCulture)} {point[2].ToString("G17", CultureInfo.InvariantCulture)}");
            }
        }

        private sealed record PlyHeaderInfo(
            string Format,
            int VertexCount,
            int XIndex,
            int YIndex,
            int ZIndex);
    }

    /// <summary>
    /// 算法配置参数
    /// </summary>
    [Serializable]
    public class FlatCalib_MeasureParam
    {
        private double _coordinateMatchToleranceUm = 1e-8;
        private double _boundaryToleranceUm = 0;

        /// <summary>
        /// 重建标定网格时用于聚类和匹配坐标轴的容差
        /// </summary>
        public double CoordinateMatchToleranceUm
        {
            get { return _coordinateMatchToleranceUm; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "CoordinateMatchToleranceUm must be >= 0.");

                _coordinateMatchToleranceUm = value;
            }
        }

        /// <summary>
        /// 查询点在被判定为越界失败前允许超出标定边界的距离
        /// </summary>
        public double BoundaryToleranceUm
        {
            get { return _boundaryToleranceUm; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "BoundaryToleranceUm must be >= 0.");

                _boundaryToleranceUm = value;
            }
        }
    }

    public enum FlatCalibCompensationMode
    {
        Formal,
        Diagnostic
    }

    
    /// <summary>
    /// 
    /// </summary>
    public class FlatCalibCompensationResult
    {
        /// <summary>
        /// true在往下执行
        /// </summary>
        public bool Success { get; set; }

        public bool IsDegraded { get; set; }

        public List<double[]> CompensatedPoints { get; set; } = new();
    }

    public class FlatCalibPointIssue
    {
        public int Index { get; set; }

        public double X { get; set; }

        public double Y { get; set; }

        public double Z { get; set; }

        public double ExceedXUm { get; set; }

        public double ExceedYUm { get; set; }

        public double EffectiveX { get; set; }

        public double EffectiveY { get; set; }

        public string FailureReason { get; set; } = string.Empty;
    }

    public class FlatCalibPointQueryDetail
    {
        public double OriginalX { get; set; }

        public double OriginalY { get; set; }

        public double EffectiveX { get; set; }

        public double EffectiveY { get; set; }

        public double ExceedXUm { get; set; }

        public double ExceedYUm { get; set; }

        public bool WasBoundaryClamped { get; set; }

        public bool WithinAllowedBoundary { get; set; }

        public string FailureReason { get; set; } = string.Empty;
    }

    public class FlatCalibBoundary
    {
        public double MinX { get; set; }

        public double MaxX { get; set; }

        public double MinY { get; set; }

        public double MaxY { get; set; }
    }
}
