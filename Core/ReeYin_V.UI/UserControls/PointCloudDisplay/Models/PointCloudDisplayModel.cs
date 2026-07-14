using Arction.Wpf.ChartingMVVM;
using Arction.Wpf.ChartingMVVM.Series3D;
using Arction.Wpf.ChartingMVVM.Views.View3D;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace ReeYin_V.UI.UserControls.PointCloudDisplay
{
    public enum PointCloudColorSource
    {
        SingleColor,
        XAxis,
        YAxis,
        ZAxis,
        Intensity
    }

    public enum PointCloudPaletteType
    {
        Classic,
        Heatmap
    }

    public sealed class PointCloudPointData
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double? Intensity { get; set; }
    }

    public sealed class PointCloudBounds
    {
        public double MinX { get; set; }
        public double MaxX { get; set; }
        public double MinY { get; set; }
        public double MaxY { get; set; }
        public double MinZ { get; set; }
        public double MaxZ { get; set; }
    }

    public sealed class PointCloudLoadResult
    {
        public IReadOnlyList<PointCloudPointData> Points { get; set; } = Array.Empty<PointCloudPointData>();
        public PointCloudBounds Bounds { get; set; } = new PointCloudBounds();
        public int SourcePointCount { get; set; }
        public bool HasIntensity { get; set; }
        public string SourceName { get; set; } = string.Empty;
    }

    public sealed class PointCloudSeriesBuildResult
    {
        public PointLineSeries3DCollection SeriesCollection { get; set; } = new PointLineSeries3DCollection();
        public int StripCount { get; set; }
        public double AutoBreakDistance { get; set; } = double.PositiveInfinity;
    }

    public sealed class PointCloudPlaneProjectionResult
    {
        public IReadOnlyList<PointCloudPointData> Points { get; set; } = Array.Empty<PointCloudPointData>();
        public PointCloudBounds Bounds { get; set; } = new PointCloudBounds();
        public double NormalX { get; set; }
        public double NormalY { get; set; }
        public double NormalZ { get; set; } = 1d;
        public double PlaneOffset { get; set; }
        public double MeanAbsoluteDistance { get; set; }
        public double RootMeanSquareDistance { get; set; }
        public double MaxAbsoluteDistance { get; set; }
    }

    public sealed class PointCloudDisplayModel
    {
        public const int DefaultMaxDisplayPointCount = 150000;

        private static readonly Color[] ClassicPalette =
        {
            Colors.Blue,
            Color.FromRgb(0, 191, 255),
            Colors.Lime,
            Colors.Yellow,
            Colors.Red
        };

        private static readonly Color[] HeatmapPalette =
        {
            Colors.Black,
            Color.FromRgb(96, 0, 160),
            Colors.Blue,
            Colors.Green,
            Colors.Yellow,
            Colors.Red,
            Colors.White
        };

        private static readonly char[] DelimitedSeparators = { ' ', '\t', ',', ';' };

        public int MaxDisplayPointCount { get; set; } = DefaultMaxDisplayPointCount;

        public PointCloudLoadResult LoadFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be empty.", nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Point cloud file was not found.", filePath);
            }

            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            string sourceName = Path.GetFileName(filePath);

            return extension switch
            {
                ".ply" => LoadFromPly(filePath, sourceName),
                ".obj" => FinalizeResult(ParseObj(filePath), sourceName),
                ".xyz" => FinalizeResult(ParseDelimited(filePath), sourceName),
                ".txt" => FinalizeResult(ParseDelimited(filePath), sourceName),
                ".csv" => FinalizeResult(ParseDelimited(filePath), sourceName),
                _ => FinalizeResult(ParseDelimited(filePath), sourceName)
            };
        }

        public PointCloudLoadResult LoadFromArrays(
            double[] xValues,
            double[] yValues,
            double[] zValues,
            double[]? intensityValues,
            string sourceName)
        {
            if (xValues == null || yValues == null || zValues == null)
            {
                throw new ArgumentNullException(nameof(xValues), "XYZ arrays cannot be null.");
            }

            int count = Math.Min(xValues.Length, Math.Min(yValues.Length, zValues.Length));
            List<PointCloudPointData> points = new(count);

            for (int i = 0; i < count; i++)
            {
                if (!IsFinite(xValues[i], yValues[i], zValues[i]))
                {
                    continue;
                }

                points.Add(new PointCloudPointData
                {
                    X = xValues[i],
                    Y = yValues[i],
                    Z = zValues[i],
                    Intensity = intensityValues != null && i < intensityValues.Length ? intensityValues[i] : null
                });
            }

            return FinalizeResult(points, sourceName);
        }

        public PointCloudLoadResult LoadFromPoints(IEnumerable<PointCloudPointData> points, string sourceName)
        {
            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            List<PointCloudPointData> normalized = points
                .Where(static p => p != null && IsFinite(p.X, p.Y, p.Z))
                .Select(static p => new PointCloudPointData
                {
                    X = p.X,
                    Y = p.Y,
                    Z = p.Z,
                    Intensity = p.Intensity
                })
                .ToList();

            return FinalizeResult(normalized, sourceName);
        }

        public PointCloudSeriesBuildResult BuildSeriesCollection(
            IReadOnlyList<PointCloudPointData> points,
            PointCloudColorSource colorSource,
            PointCloudPaletteType paletteType,
            bool hasIntensity,
            PointShape3D pointShape,
            double pointSize,
            bool connectPoints,
            double lineWidth)
        {
            PointCloudSeriesBuildResult result = new();
            SeriesPoint3D[] seriesPoints = BuildSeriesPoints(points, colorSource, paletteType, hasIntensity);
            if (seriesPoints.Length == 0)
            {
                return result;
            }

            IReadOnlyList<SeriesPoint3D[]> strips;
            double autoBreakDistance = double.PositiveInfinity;
            if (connectPoints)
            {
                strips = BuildLineStrips(points, seriesPoints, out autoBreakDistance);
            }
            else
            {
                strips = new[] { seriesPoints };
            }

            PointLineSeries3DCollection collection = new();
            foreach (SeriesPoint3D[] strip in strips)
            {
                if (strip.Length == 0)
                {
                    continue;
                }

                collection.Add(CreateSeries(strip, pointShape, pointSize, connectPoints, lineWidth));
            }

            result.SeriesCollection = collection;
            result.StripCount = collection.Count;
            result.AutoBreakDistance = autoBreakDistance;
            return result;
        }

        public SeriesPoint3D[] BuildSeriesPoints(
            IReadOnlyList<PointCloudPointData> points,
            PointCloudColorSource colorSource,
            PointCloudPaletteType paletteType,
            bool hasIntensity)
        {
            if (points == null || points.Count == 0)
            {
                return Array.Empty<SeriesPoint3D>();
            }

            PointCloudColorSource effectiveColorSource = colorSource == PointCloudColorSource.Intensity && !hasIntensity
                ? PointCloudColorSource.ZAxis
                : colorSource;

            if (effectiveColorSource == PointCloudColorSource.SingleColor)
            {
                SeriesPoint3D[] solidColorPoints = new SeriesPoint3D[points.Count];
                for (int i = 0; i < points.Count; i++)
                {
                    PointCloudPointData point = points[i];
                    solidColorPoints[i] = new SeriesPoint3D(point.X, point.Y, point.Z, Color.FromRgb(0, 191, 255));
                }

                return solidColorPoints;
            }

            double minValue = double.MaxValue;
            double maxValue = double.MinValue;
            for (int i = 0; i < points.Count; i++)
            {
                double value = GetColorValue(points[i], effectiveColorSource);
                minValue = Math.Min(minValue, value);
                maxValue = Math.Max(maxValue, value);
            }

            Color[] palette = paletteType == PointCloudPaletteType.Heatmap ? HeatmapPalette : ClassicPalette;
            SeriesPoint3D[] seriesPoints = new SeriesPoint3D[points.Count];

            for (int i = 0; i < points.Count; i++)
            {
                PointCloudPointData point = points[i];
                Color color = InterpolateColor(GetColorValue(point, effectiveColorSource), minValue, maxValue, palette);
                seriesPoints[i] = new SeriesPoint3D(point.X, point.Y, point.Z, color);
            }

            return seriesPoints;
        }

        public PointCloudPlaneProjectionResult ProjectToBestFitPlane(IReadOnlyList<PointCloudPointData> points)
        {
            ArgumentNullException.ThrowIfNull(points);

            if (points.Count == 0)
            {
                throw new InvalidDataException("No point data available for plane smoothing.");
            }

            GetBestFitPlane(
                points,
                out double centerX,
                out double centerY,
                out double centerZ,
                out double normalX,
                out double normalY,
                out double normalZ);

            double planeOffset = -((normalX * centerX) + (normalY * centerY) + (normalZ * centerZ));
            List<PointCloudPointData> projectedPoints = new(points.Count);
            double absoluteDistanceSum = 0d;
            double squaredDistanceSum = 0d;
            double maxAbsoluteDistance = 0d;

            for (int i = 0; i < points.Count; i++)
            {
                PointCloudPointData point = points[i];
                double signedDistance = (normalX * point.X) + (normalY * point.Y) + (normalZ * point.Z) + planeOffset;
                double absoluteDistance = Math.Abs(signedDistance);
                absoluteDistanceSum += absoluteDistance;
                squaredDistanceSum += signedDistance * signedDistance;
                maxAbsoluteDistance = Math.Max(maxAbsoluteDistance, absoluteDistance);

                projectedPoints.Add(new PointCloudPointData
                {
                    X = point.X - (signedDistance * normalX),
                    Y = point.Y - (signedDistance * normalY),
                    Z = point.Z - (signedDistance * normalZ),
                    Intensity = point.Intensity
                });
            }

            return new PointCloudPlaneProjectionResult
            {
                Points = projectedPoints,
                Bounds = CalculateBounds(projectedPoints),
                NormalX = normalX,
                NormalY = normalY,
                NormalZ = normalZ,
                PlaneOffset = planeOffset,
                MeanAbsoluteDistance = absoluteDistanceSum / points.Count,
                RootMeanSquareDistance = Math.Sqrt(squaredDistanceSum / points.Count),
                MaxAbsoluteDistance = maxAbsoluteDistance
            };
        }

        private static PointLineSeries3D CreateSeries(
            SeriesPoint3D[] points,
            PointShape3D pointShape,
            double pointSize,
            bool connectPoints,
            double lineWidth)
        {
            PointLineSeries3D series = new()
            {
                Points = points,
                IndividualPointColors = true,
                MultiColorLine = true,
                PointsVisible = true,
                LineVisible = connectPoints && points.Length > 1,
                PointsType = PointsType3D.Points,
                ShowInLegendBox = false,
                PointStyle = CreatePointStyle(pointShape, pointSize),
                LineStyle = CreateLineStyle(lineWidth)
            };

            return series;
        }

        private static PointStyle3D CreatePointStyle(PointShape3D pointShape, double pointSize)
        {
            double normalizedSize = Math.Max(0.2d, pointSize);
            return new PointStyle3D
            {
                Shape3D = pointShape,
                Size3D = new SizeDoubleXYZ
                {
                    Width = normalizedSize,
                    Height = normalizedSize,
                    Depth = normalizedSize
                }
            };
        }

        private static LineStyle3D CreateLineStyle(double lineWidth)
        {
            return new LineStyle3D
            {
                Color = Color.FromArgb(210, 236, 244, 255),
                Width = Math.Max(0.2d, lineWidth)
            };
        }

        private static IReadOnlyList<SeriesPoint3D[]> BuildLineStrips(
            IReadOnlyList<PointCloudPointData> points,
            SeriesPoint3D[] seriesPoints,
            out double autoBreakDistance)
        {
            autoBreakDistance = CalculateAutoBreakDistance(points);
            if (!double.IsFinite(autoBreakDistance) || autoBreakDistance <= 0d)
            {
                return new[] { seriesPoints };
            }

            List<SeriesPoint3D[]> strips = new();
            List<SeriesPoint3D> currentStrip = new() { seriesPoints[0] };

            for (int i = 1; i < seriesPoints.Length; i++)
            {
                if (GetDistance(points[i - 1], points[i]) > autoBreakDistance)
                {
                    strips.Add(currentStrip.ToArray());
                    currentStrip = new List<SeriesPoint3D>();
                }

                currentStrip.Add(seriesPoints[i]);
            }

            if (currentStrip.Count > 0)
            {
                strips.Add(currentStrip.ToArray());
            }

            return strips;
        }

        private static double CalculateAutoBreakDistance(IReadOnlyList<PointCloudPointData> points)
        {
            if (points == null || points.Count < 3)
            {
                return double.PositiveInfinity;
            }

            List<double> distances = new(points.Count - 1);
            for (int i = 1; i < points.Count; i++)
            {
                double distance = GetDistance(points[i - 1], points[i]);
                if (double.IsFinite(distance))
                {
                    distances.Add(distance);
                }
            }

            if (distances.Count == 0)
            {
                return double.PositiveInfinity;
            }

            distances.Sort();
            double q1 = GetPercentile(distances, 0.25d);
            double q3 = GetPercentile(distances, 0.75d);
            double iqr = Math.Max(q3 - q1, 0d);
            double threshold = q3 + Math.Max(Math.Max(iqr * 1.5d, q1 * 0.25d), 1e-6d);

            return threshold >= distances[^1] ? double.PositiveInfinity : threshold;
        }

        private static double GetPercentile(IReadOnlyList<double> sortedValues, double percentile)
        {
            if (sortedValues.Count == 1)
            {
                return sortedValues[0];
            }

            double clampedPercentile = Math.Clamp(percentile, 0d, 1d);
            double position = (sortedValues.Count - 1) * clampedPercentile;
            int lowerIndex = (int)Math.Floor(position);
            int upperIndex = (int)Math.Ceiling(position);

            if (lowerIndex == upperIndex)
            {
                return sortedValues[lowerIndex];
            }

            double blend = position - lowerIndex;
            return sortedValues[lowerIndex] + ((sortedValues[upperIndex] - sortedValues[lowerIndex]) * blend);
        }

        private static double GetDistance(PointCloudPointData first, PointCloudPointData second)
        {
            double dx = second.X - first.X;
            double dy = second.Y - first.Y;
            double dz = second.Z - first.Z;
            return Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
        }

        private PointCloudLoadResult LoadFromPly(string filePath, string sourceName)
        {
            using FileStream stream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            PlyHeader header = ParsePlyHeader(stream);
            int sampleStep = GetSampleStep(header.VertexCount);

            List<PointCloudPointData> points = header.Format switch
            {
                PlyFormat.Ascii => ReadPlyAscii(stream, header, sampleStep),
                PlyFormat.BinaryLittleEndian => ReadPlyBinaryLittle(stream, header, sampleStep),
                PlyFormat.BinaryBigEndian => throw new NotSupportedException("binary_big_endian PLY is not supported."),
                _ => throw new NotSupportedException("Unsupported PLY format.")
            };

            return FinalizeResult(points, sourceName, header.VertexCount, alreadyLimited: true);
        }

        private PointCloudLoadResult FinalizeResult(
            IReadOnlyList<PointCloudPointData> rawPoints,
            string sourceName,
            int? sourcePointCount = null,
            bool alreadyLimited = false)
        {
            if (rawPoints == null || rawPoints.Count == 0)
            {
                throw new InvalidDataException("No valid point cloud points were parsed.");
            }

            IReadOnlyList<PointCloudPointData> displayPoints = rawPoints;
            if (!alreadyLimited && rawPoints.Count > MaxDisplayPointCount)
            {
                displayPoints = DownSample(rawPoints, MaxDisplayPointCount);
            }

            return new PointCloudLoadResult
            {
                Points = displayPoints,
                Bounds = CalculateBounds(displayPoints),
                SourcePointCount = sourcePointCount ?? rawPoints.Count,
                HasIntensity = displayPoints.Any(static p => p.Intensity.HasValue),
                SourceName = sourceName
            };
        }

        private List<PointCloudPointData> ParseDelimited(string filePath)
        {
            List<PointCloudPointData> points = new();

            foreach (string rawLine in File.ReadLines(filePath))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith("//", StringComparison.Ordinal))
                {
                    continue;
                }

                string[] parts = line.Split(DelimitedSeparators, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3)
                {
                    continue;
                }

                if (!TryParseDouble(parts[0], out double x) ||
                    !TryParseDouble(parts[1], out double y) ||
                    !TryParseDouble(parts[2], out double z) ||
                    !IsFinite(x, y, z))
                {
                    continue;
                }

                double? intensity = null;
                if (parts.Length >= 4 && TryParseDouble(parts[3], out double parsedIntensity))
                {
                    intensity = parsedIntensity;
                }

                points.Add(new PointCloudPointData
                {
                    X = x,
                    Y = y,
                    Z = z,
                    Intensity = intensity
                });
            }

            return points;
        }

        private List<PointCloudPointData> ParseObj(string filePath)
        {
            List<PointCloudPointData> points = new();

            foreach (string rawLine in File.ReadLines(filePath))
            {
                string line = rawLine.Trim();
                if (!line.StartsWith("v ", StringComparison.Ordinal))
                {
                    continue;
                }

                string[] parts = line.Split(DelimitedSeparators, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 4)
                {
                    continue;
                }

                if (!TryParseDouble(parts[1], out double x) ||
                    !TryParseDouble(parts[2], out double y) ||
                    !TryParseDouble(parts[3], out double z) ||
                    !IsFinite(x, y, z))
                {
                    continue;
                }

                points.Add(new PointCloudPointData
                {
                    X = x,
                    Y = y,
                    Z = z
                });
            }

            return points;
        }

        private IReadOnlyList<PointCloudPointData> DownSample(IReadOnlyList<PointCloudPointData> points, int maxCount)
        {
            if (points.Count <= maxCount)
            {
                return points;
            }

            int step = (int)Math.Ceiling(points.Count / (double)maxCount);
            List<PointCloudPointData> sampled = new(maxCount);
            for (int i = 0; i < points.Count; i += step)
            {
                sampled.Add(points[i]);
            }

            return sampled;
        }

        private static PointCloudBounds CalculateBounds(IReadOnlyList<PointCloudPointData> points)
        {
            double minX = double.MaxValue;
            double maxX = double.MinValue;
            double minY = double.MaxValue;
            double maxY = double.MinValue;
            double minZ = double.MaxValue;
            double maxZ = double.MinValue;

            for (int i = 0; i < points.Count; i++)
            {
                PointCloudPointData point = points[i];
                minX = Math.Min(minX, point.X);
                maxX = Math.Max(maxX, point.X);
                minY = Math.Min(minY, point.Y);
                maxY = Math.Max(maxY, point.Y);
                minZ = Math.Min(minZ, point.Z);
                maxZ = Math.Max(maxZ, point.Z);
            }

            return new PointCloudBounds
            {
                MinX = minX,
                MaxX = maxX,
                MinY = minY,
                MaxY = maxY,
                MinZ = minZ,
                MaxZ = maxZ
            };
        }

        private static double GetColorValue(PointCloudPointData point, PointCloudColorSource colorSource)
        {
            return colorSource switch
            {
                PointCloudColorSource.XAxis => point.X,
                PointCloudColorSource.YAxis => point.Y,
                PointCloudColorSource.ZAxis => point.Z,
                PointCloudColorSource.Intensity => point.Intensity ?? point.Z,
                _ => point.Z
            };
        }

        private static void GetBestFitPlane(
            IReadOnlyList<PointCloudPointData> points,
            out double centerX,
            out double centerY,
            out double centerZ,
            out double normalX,
            out double normalY,
            out double normalZ)
        {
            centerX = 0d;
            centerY = 0d;
            centerZ = 0d;

            for (int i = 0; i < points.Count; i++)
            {
                centerX += points[i].X;
                centerY += points[i].Y;
                centerZ += points[i].Z;
            }

            double count = points.Count;
            centerX /= count;
            centerY /= count;
            centerZ /= count;

            double covarianceXX = 0d;
            double covarianceXY = 0d;
            double covarianceXZ = 0d;
            double covarianceYY = 0d;
            double covarianceYZ = 0d;
            double covarianceZZ = 0d;

            for (int i = 0; i < points.Count; i++)
            {
                double dx = points[i].X - centerX;
                double dy = points[i].Y - centerY;
                double dz = points[i].Z - centerZ;

                covarianceXX += dx * dx;
                covarianceXY += dx * dy;
                covarianceXZ += dx * dz;
                covarianceYY += dy * dy;
                covarianceYZ += dy * dz;
                covarianceZZ += dz * dz;
            }

            if ((covarianceXX + covarianceYY + covarianceZZ) <= 1e-12d)
            {
                normalX = 0d;
                normalY = 0d;
                normalZ = 1d;
                return;
            }

            double[,] covariance =
            {
                { covarianceXX, covarianceXY, covarianceXZ },
                { covarianceXY, covarianceYY, covarianceYZ },
                { covarianceXZ, covarianceYZ, covarianceZZ }
            };

            if (!TryGetSmallestEigenVector(covariance, out normalX, out normalY, out normalZ))
            {
                normalX = 0d;
                normalY = 0d;
                normalZ = 1d;
                return;
            }

            if (normalZ < 0d ||
                (Math.Abs(normalZ) < 1e-12d && normalY < 0d) ||
                (Math.Abs(normalZ) < 1e-12d && Math.Abs(normalY) < 1e-12d && normalX < 0d))
            {
                normalX = -normalX;
                normalY = -normalY;
                normalZ = -normalZ;
            }
        }

        private static bool TryGetSmallestEigenVector(
            double[,] matrix,
            out double vectorX,
            out double vectorY,
            out double vectorZ)
        {
            double[,] values = (double[,])matrix.Clone();
            double[,] eigenVectors =
            {
                { 1d, 0d, 0d },
                { 0d, 1d, 0d },
                { 0d, 0d, 1d }
            };

            for (int iteration = 0; iteration < 24; iteration++)
            {
                GetLargestOffDiagonal(values, out int row, out int column, out double magnitude);
                if (magnitude <= 1e-12d)
                {
                    break;
                }

                double diagonalDifference = values[column, column] - values[row, row];
                double tangent = diagonalDifference == 0d
                    ? 1d
                    : Math.Sign(diagonalDifference) * values[row, column] /
                      (Math.Abs(diagonalDifference) / 2d + Math.Sqrt((diagonalDifference * diagonalDifference / 4d) + (values[row, column] * values[row, column])));
                double cosine = 1d / Math.Sqrt(1d + (tangent * tangent));
                double sine = tangent * cosine;
                ApplyJacobiRotation(values, eigenVectors, row, column, sine, cosine);
            }

            int smallestIndex = 0;
            double smallestValue = values[0, 0];
            for (int i = 1; i < 3; i++)
            {
                if (values[i, i] < smallestValue)
                {
                    smallestValue = values[i, i];
                    smallestIndex = i;
                }
            }

            vectorX = eigenVectors[0, smallestIndex];
            vectorY = eigenVectors[1, smallestIndex];
            vectorZ = eigenVectors[2, smallestIndex];
            return NormalizeVector(ref vectorX, ref vectorY, ref vectorZ);
        }

        private static void GetLargestOffDiagonal(double[,] matrix, out int row, out int column, out double magnitude)
        {
            row = 0;
            column = 1;
            magnitude = Math.Abs(matrix[0, 1]);

            double candidate = Math.Abs(matrix[0, 2]);
            if (candidate > magnitude)
            {
                row = 0;
                column = 2;
                magnitude = candidate;
            }

            candidate = Math.Abs(matrix[1, 2]);
            if (candidate > magnitude)
            {
                row = 1;
                column = 2;
                magnitude = candidate;
            }
        }

        private static void ApplyJacobiRotation(double[,] matrix, double[,] eigenVectors, int row, int column, double sine, double cosine)
        {
            double diagonalRow = matrix[row, row];
            double diagonalColumn = matrix[column, column];
            double offDiagonal = matrix[row, column];

            matrix[row, row] = (cosine * cosine * diagonalRow) - (2d * sine * cosine * offDiagonal) + (sine * sine * diagonalColumn);
            matrix[column, column] = (sine * sine * diagonalRow) + (2d * sine * cosine * offDiagonal) + (cosine * cosine * diagonalColumn);
            matrix[row, column] = 0d;
            matrix[column, row] = 0d;

            for (int i = 0; i < 3; i++)
            {
                if (i == row || i == column)
                {
                    continue;
                }

                double rowValue = matrix[i, row];
                double columnValue = matrix[i, column];
                matrix[i, row] = (cosine * rowValue) - (sine * columnValue);
                matrix[row, i] = matrix[i, row];
                matrix[i, column] = (sine * rowValue) + (cosine * columnValue);
                matrix[column, i] = matrix[i, column];
            }

            for (int i = 0; i < 3; i++)
            {
                double eigenRow = eigenVectors[i, row];
                double eigenColumn = eigenVectors[i, column];
                eigenVectors[i, row] = (cosine * eigenRow) - (sine * eigenColumn);
                eigenVectors[i, column] = (sine * eigenRow) + (cosine * eigenColumn);
            }
        }

        private static bool NormalizeVector(ref double x, ref double y, ref double z)
        {
            double length = Math.Sqrt((x * x) + (y * y) + (z * z));
            if (length <= 1e-12d)
            {
                return false;
            }

            x /= length;
            y /= length;
            z /= length;
            return true;
        }

        private static Color InterpolateColor(double value, double min, double max, Color[] palette)
        {
            if (palette.Length == 0)
            {
                return Colors.DeepSkyBlue;
            }

            if (palette.Length == 1 || Math.Abs(max - min) < double.Epsilon)
            {
                return palette[palette.Length / 2];
            }

            double normalized = Math.Clamp((value - min) / (max - min), 0d, 1d);
            double scaled = normalized * (palette.Length - 1);
            int lowerIndex = (int)Math.Floor(scaled);
            int upperIndex = Math.Min(lowerIndex + 1, palette.Length - 1);
            double blend = scaled - lowerIndex;
            Color lower = palette[lowerIndex];
            Color upper = palette[upperIndex];

            return Color.FromRgb(
                (byte)(lower.R + ((upper.R - lower.R) * blend)),
                (byte)(lower.G + ((upper.G - lower.G) * blend)),
                (byte)(lower.B + ((upper.B - lower.B) * blend)));
        }

        private static bool TryParseDouble(string value, out double result)
        {
            return double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result)
                   || double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out result);
        }

        private static bool IsFinite(double x, double y, double z)
        {
            return double.IsFinite(x) && double.IsFinite(y) && double.IsFinite(z);
        }

        private int GetSampleStep(int pointCount)
        {
            if (pointCount <= 0 || pointCount <= MaxDisplayPointCount)
            {
                return 1;
            }

            return (int)Math.Ceiling(pointCount / (double)MaxDisplayPointCount);
        }

        private sealed class PlyHeader
        {
            public PlyFormat Format { get; set; }
            public int VertexCount { get; set; }
            public List<PlyProperty> VertexProperties { get; } = new();
            public int XIndex { get; set; } = -1;
            public int YIndex { get; set; } = -1;
            public int ZIndex { get; set; } = -1;
            public int IntensityIndex { get; set; } = -1;
        }

        private sealed class PlyProperty
        {
            public string Name { get; set; } = string.Empty;
            public PlyScalarType Type { get; set; }
        }

        private enum PlyFormat
        {
            Ascii,
            BinaryLittleEndian,
            BinaryBigEndian
        }

        private enum PlyScalarType
        {
            Int8,
            UInt8,
            Int16,
            UInt16,
            Int32,
            UInt32,
            Float32,
            Float64
        }

        private static PlyHeader ParsePlyHeader(FileStream stream)
        {
            PlyHeader header = new();
            bool inVertexElement = false;
            string? firstLine = ReadAsciiLine(stream);

            if (!string.Equals(firstLine?.Trim(), "ply", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("This is not a valid PLY file.");
            }

            while (true)
            {
                string? line = ReadAsciiLine(stream);
                if (line == null)
                {
                    throw new EndOfStreamException("PLY header is incomplete.");
                }

                string trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith("comment", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (trimmed.StartsWith("format", StringComparison.OrdinalIgnoreCase))
                {
                    string[] parts = SplitByWhitespace(trimmed);
                    header.Format = parts[1] switch
                    {
                        "ascii" => PlyFormat.Ascii,
                        "binary_little_endian" => PlyFormat.BinaryLittleEndian,
                        "binary_big_endian" => PlyFormat.BinaryBigEndian,
                        _ => throw new NotSupportedException($"Unsupported PLY format: {parts[1]}")
                    };
                    continue;
                }

                if (trimmed.StartsWith("element", StringComparison.OrdinalIgnoreCase))
                {
                    string[] parts = SplitByWhitespace(trimmed);
                    inVertexElement = string.Equals(parts[1], "vertex", StringComparison.OrdinalIgnoreCase);
                    if (inVertexElement)
                    {
                        header.VertexCount = int.Parse(parts[2], CultureInfo.InvariantCulture);
                    }
                    continue;
                }

                if (trimmed.StartsWith("property", StringComparison.OrdinalIgnoreCase))
                {
                    if (!inVertexElement)
                    {
                        continue;
                    }

                    string[] parts = SplitByWhitespace(trimmed);
                    if (parts.Length < 3 || string.Equals(parts[1], "list", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new NotSupportedException("PLY vertex list properties are not supported.");
                    }

                    PlyProperty property = new()
                    {
                        Name = parts[2],
                        Type = ParsePlyScalarType(parts[1])
                    };

                    int index = header.VertexProperties.Count;
                    header.VertexProperties.Add(property);

                    if (string.Equals(property.Name, "x", StringComparison.OrdinalIgnoreCase)) header.XIndex = index;
                    else if (string.Equals(property.Name, "y", StringComparison.OrdinalIgnoreCase)) header.YIndex = index;
                    else if (string.Equals(property.Name, "z", StringComparison.OrdinalIgnoreCase)) header.ZIndex = index;
                    else if (string.Equals(property.Name, "intensity", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(property.Name, "scalar_intensity", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(property.Name, "scalar_Intensity", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(property.Name, "value", StringComparison.OrdinalIgnoreCase))
                    {
                        header.IntensityIndex = index;
                    }

                    continue;
                }

                if (string.Equals(trimmed, "end_header", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
            }

            if (header.VertexCount <= 0 || header.XIndex < 0 || header.YIndex < 0 || header.ZIndex < 0)
            {
                throw new InvalidDataException("PLY header does not define valid vertex/x/y/z properties.");
            }

            return header;
        }

        private static List<PointCloudPointData> ReadPlyAscii(FileStream stream, PlyHeader header, int sampleStep)
        {
            List<PointCloudPointData> points = new(Math.Min(header.VertexCount, DefaultMaxDisplayPointCount));
            using StreamReader reader = new(stream, Encoding.ASCII, false, 1 << 20, leaveOpen: true);

            for (int row = 0; row < header.VertexCount; row++)
            {
                string? line;
                do
                {
                    line = reader.ReadLine();
                    if (line == null)
                    {
                        throw new EndOfStreamException("PLY vertex data ended unexpectedly.");
                    }

                    line = line.Trim();
                }
                while (line.Length == 0);

                if (row % sampleStep != 0)
                {
                    continue;
                }

                string[] parts = SplitByWhitespace(line);
                int maxIndex = Math.Max(header.ZIndex, header.IntensityIndex);
                if (parts.Length <= maxIndex)
                {
                    continue;
                }

                if (!TryParseDouble(parts[header.XIndex], out double x) ||
                    !TryParseDouble(parts[header.YIndex], out double y) ||
                    !TryParseDouble(parts[header.ZIndex], out double z) ||
                    !IsFinite(x, y, z))
                {
                    continue;
                }

                double? intensity = null;
                if (header.IntensityIndex >= 0 &&
                    header.IntensityIndex < parts.Length &&
                    TryParseDouble(parts[header.IntensityIndex], out double parsedIntensity))
                {
                    intensity = parsedIntensity;
                }

                points.Add(new PointCloudPointData { X = x, Y = y, Z = z, Intensity = intensity });
            }

            return points;
        }

        private static List<PointCloudPointData> ReadPlyBinaryLittle(FileStream stream, PlyHeader header, int sampleStep)
        {
            List<PointCloudPointData> points = new(Math.Min(header.VertexCount, DefaultMaxDisplayPointCount));
            using BinaryReader reader = new(stream, Encoding.ASCII, leaveOpen: true);

            for (int row = 0; row < header.VertexCount; row++)
            {
                bool keepPoint = row % sampleStep == 0;
                double x = 0;
                double y = 0;
                double z = 0;
                double? intensity = null;

                for (int column = 0; column < header.VertexProperties.Count; column++)
                {
                    double value = ReadPlyScalarValue(reader, header.VertexProperties[column].Type);
                    if (!keepPoint)
                    {
                        continue;
                    }

                    if (column == header.XIndex) x = value;
                    else if (column == header.YIndex) y = value;
                    else if (column == header.ZIndex) z = value;
                    else if (column == header.IntensityIndex) intensity = value;
                }

                if (!keepPoint || !IsFinite(x, y, z))
                {
                    continue;
                }

                points.Add(new PointCloudPointData { X = x, Y = y, Z = z, Intensity = intensity });
            }

            return points;
        }

        private static PlyScalarType ParsePlyScalarType(string typeName)
        {
            return typeName switch
            {
                "char" or "int8" => PlyScalarType.Int8,
                "uchar" or "uint8" => PlyScalarType.UInt8,
                "short" or "int16" => PlyScalarType.Int16,
                "ushort" or "uint16" => PlyScalarType.UInt16,
                "int" or "int32" => PlyScalarType.Int32,
                "uint" or "uint32" => PlyScalarType.UInt32,
                "float" or "float32" => PlyScalarType.Float32,
                "double" or "float64" => PlyScalarType.Float64,
                _ => throw new NotSupportedException($"Unsupported PLY scalar type: {typeName}")
            };
        }

        private static double ReadPlyScalarValue(BinaryReader reader, PlyScalarType type)
        {
            return type switch
            {
                PlyScalarType.Int8 => (sbyte)reader.ReadByte(),
                PlyScalarType.UInt8 => reader.ReadByte(),
                PlyScalarType.Int16 => reader.ReadInt16(),
                PlyScalarType.UInt16 => reader.ReadUInt16(),
                PlyScalarType.Int32 => reader.ReadInt32(),
                PlyScalarType.UInt32 => reader.ReadUInt32(),
                PlyScalarType.Float32 => reader.ReadSingle(),
                PlyScalarType.Float64 => reader.ReadDouble(),
                _ => throw new NotSupportedException("Unsupported PLY scalar read mode.")
            };
        }

        private static string? ReadAsciiLine(FileStream stream)
        {
            List<byte> bytes = new(128);
            while (true)
            {
                int value = stream.ReadByte();
                if (value < 0 || value == '\n')
                {
                    break;
                }

                bytes.Add((byte)value);
            }

            if (bytes.Count == 0 && stream.Position >= stream.Length)
            {
                return null;
            }

            if (bytes.Count > 0 && bytes[^1] == '\r')
            {
                bytes.RemoveAt(bytes.Count - 1);
            }

            return Encoding.ASCII.GetString(bytes.ToArray());
        }

        private static string[] SplitByWhitespace(string value)
        {
            return value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
