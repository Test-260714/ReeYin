using Custom.ElectroStaticChuckMeasure.ALGO.Parameters;
using Custom.ElectroStaticChuckMeasure.ALGO.Api;
using Custom.ElectroStaticChuckMeasure.ALGO.Common;
using Custom.ElectroStaticChuckMeasure.ALGO.Frames;
using HalconDotNet;
using System.Runtime.InteropServices;

namespace Custom.ElectroStaticChuckMeasure.ALGO.Calibration;

public sealed class PitchCalibrationService
{
    public PitchCalibrationState Calibrate(
        List<float[]> grayData,
        List<float[]> heightData,
        ElectroStaticChuckParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(grayData);
        ArgumentNullException.ThrowIfNull(heightData);
        ArgumentNullException.ThrowIfNull(parameters);

        ValidateRowData(grayData, nameof(grayData));
        ValidateRowData(heightData, nameof(heightData));
        if (grayData.Count == 0 || heightData.Count == 0)
            return CreateUncalibrated();
        if (grayData.Count != heightData.Count)
            throw new ArgumentException("Pitch calibration gray and height row counts must match.", nameof(heightData));
        if (grayData[0].Length != heightData[0].Length)
            throw new ArgumentException("Pitch calibration gray and height widths must match.", nameof(heightData));

        IReadOnlyList<float[]> calibrationHeightData = parameters.Sensor.IsFlip
            ? heightData.AsEnumerable().Reverse().ToArray()
            : heightData;

        return CalibrateHeightRows(
            calibrationHeightData,
            parameters.Sensor.IntervalX,
            parameters.Sensor.MinDepth,
            parameters.Sensor.MaxDepth,
            parameters.Sensor.InvalidValue);
    }

    public PitchCalibrationState Calibrate(
        FrameSet frames,
        ElectroStaticChuckParameters parameters,
        IAlgoProgressReporter? progressReporter = null)
    {
        ArgumentNullException.ThrowIfNull(frames);
        ArgumentNullException.ThrowIfNull(parameters);

        var heightRows = new List<float[]>();
        int expectedWidth = 0;
        double intervalX = 0.0;
        double minDepth = 0.0;
        double maxDepth = 0.0;

        for (int i = 0; i < frames.Frames.Count; i++)
        {
            FrameDescriptor descriptor = frames.Frames[i];
            progressReporter?.Report(new AlgoProgressEvent(
                ElectroStaticChuckStage.Calibration,
                "reading calibration frame",
                FrameIndex: i,
                FrameCount: frames.Frames.Count));
            using ImageFrame frame = FrameLoader.Load(descriptor, parameters.Sensor);
            if (expectedWidth == 0)
            {
                expectedWidth = frame.Width;
                intervalX = descriptor.IntervalX;
                minDepth = descriptor.MinDepth;
                maxDepth = descriptor.MaxDepth;
            }
            else
            {
                if (frame.Width != expectedWidth)
                    throw new InvalidOperationException("Pitch calibration frames must have matching widths.");
                if (Math.Abs(descriptor.IntervalX - intervalX) > 1e-9)
                    throw new InvalidOperationException("Pitch calibration frames must have matching IntervalX values.");
            }

            heightRows.AddRange(ReadRealRows(frame.HeightImage, frame.Width, frame.Height));
        }

        if (heightRows.Count == 0)
            return CreateUncalibrated();

        return CalibrateHeightRows(
            heightRows,
            intervalX,
            minDepth,
            maxDepth,
            parameters.Sensor.InvalidValue);
    }

    private static PitchCalibrationState CalibrateHeightRows(
        IReadOnlyList<float[]> heightData,
        double intervalX,
        double minDepth,
        double maxDepth,
        double invalidValue)
    {
        if (heightData.Count == 0)
            return CreateUncalibrated();
        if (!double.IsFinite(intervalX) || intervalX <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(intervalX), intervalX, "Pitch calibration IntervalX must be finite and positive.");
        if (!double.IsFinite(minDepth) || !double.IsFinite(maxDepth) || minDepth > maxDepth)
            throw new ArgumentOutOfRangeException(nameof(minDepth), $"Pitch calibration depth range must be finite and ordered. Min={minDepth}, Max={maxDepth}.");
        if (!double.IsFinite(invalidValue))
            throw new ArgumentOutOfRangeException(nameof(invalidValue), invalidValue, "Pitch calibration invalid depth value must be finite.");

        int width = heightData[0].Length;
        if (width == 0)
            return CreateUncalibrated();

        var fitColValues = new Dictionary<int, List<double>>();
        var slopes = new List<double>();
        int minPointsPerRow = Math.Max(10, width / 10);

        foreach (float[] rowData in heightData)
        {
            if (rowData.Length != width)
                throw new ArgumentException("Pitch calibration height rows must have matching widths.", nameof(heightData));

            var cols = new List<double>();
            var vals = new List<double>();
            for (int col = 0; col < rowData.Length; col++)
            {
                double value = rowData[col];
                if (!IsValidDepth(value, minDepth, maxDepth, invalidValue))
                    continue;

                cols.Add(col * intervalX);
                vals.Add(value);
            }

            if (cols.Count < minPointsPerRow)
                continue;

            List<PointSetWithScore> pointSets = SegmentPoints(cols, vals);
            if (pointSets.Count == 0)
                continue;

            PointSetWithScore pointSet = pointSets[0];
            if (pointSet.Points.Count < minPointsPerRow)
                continue;

            if (!TryFitSlope(pointSet.Points, out double rowSlope))
                continue;

            slopes.Add(rowSlope);
            foreach (Point point in pointSet.Points)
            {
                int colIndex = (int)(point.X / intervalX);
                if (colIndex < 0 || colIndex >= width)
                    continue;

                if (!fitColValues.TryGetValue(colIndex, out List<double>? values))
                {
                    values = new List<double>();
                    fitColValues.Add(colIndex, values);
                }

                values.Add(point.Y);
            }
        }

        if (slopes.Count == 0)
            return CreateUncalibrated();

        slopes.Sort();
        int trimCount = slopes.Count / 10;
        List<double> trimmedSlopes = slopes
            .Skip(trimCount)
            .Take(slopes.Count - 2 * trimCount)
            .ToList();
        if (trimmedSlopes.Count == 0)
            return CreateUncalibrated();

        double pitchSlope = trimmedSlopes.Average();
        float[] depthBase = BuildDepthBase(width, intervalX, pitchSlope, fitColValues);
        return new PitchCalibrationState(true, pitchSlope, depthBase);
    }

    private static float[] BuildDepthBase(
        int width,
        double intervalX,
        double pitchSlope,
        IReadOnlyDictionary<int, List<double>> fitColValues)
    {
        float[] colMeanValues = new float[width];
        double sumX = 0.0;
        double sumY = 0.0;
        int validColCount = 0;

        foreach ((int col, List<double> values) in fitColValues)
        {
            if (col < 0 || col >= width || values.Count == 0)
                continue;

            double mean = values.Sum() / values.Count;
            colMeanValues[col] = (float)mean;
            sumX += col;
            sumY += mean;
            validColCount++;
        }

        if (validColCount == 0)
            return colMeanValues;

        double centerX = sumX / validColCount;
        double centerY = sumY / validColCount;
        for (int col = 0; col < width; col++)
        {
            if (fitColValues.ContainsKey(col))
                continue;

            double interpolatedY = centerY + pitchSlope * intervalX * (col - centerX);
            colMeanValues[col] = (float)interpolatedY;
        }

        return colMeanValues;
    }

    private static bool TryFitSlope(IReadOnlyList<Point> points, out double slope)
    {
        slope = 0.0;
        if (points.Count < 2)
            return false;

        Point[] ordered = points.OrderBy(point => point.X).ToArray();
        double[] fitRows = ordered.Select(point => point.Y).ToArray();
        double[] fitCols = ordered.Select(point => point.X).ToArray();
        using var rowTuple = new HTuple(fitRows);
        using var colTuple = new HTuple(fitCols);
        var lineXld = new HXLDCont(rowTuple, colTuple);
        try
        {
            lineXld.FitLineContourXld(
                "tukey",
                -1,
                0,
                5,
                2,
                out double rowBegin,
                out double colBegin,
                out double rowEnd,
                out double colEnd,
                out _,
                out _,
                out _);

            double deltaCol = colEnd - colBegin;
            if (Math.Abs(deltaCol) <= 1e-10)
                return false;

            slope = (rowEnd - rowBegin) / deltaCol;
            return double.IsFinite(slope);
        }
        finally
        {
            lineXld.Dispose();
        }
    }

    private static List<PointSetWithScore> SegmentPoints(List<double> xList, List<double> yList)
    {
        var points = new List<Point>(xList.Count);
        for (int i = 0; i < xList.Count; i++)
            points.Add(new Point(xList[i], yList[i]));

        double minDist = CalculateMinDistance(points);
        double threshold = minDist * 1;
        const int minInliers = 1;
        const int iterations = 50;

        var remainingPoints = new List<Point>(points);
        var pointSets = new List<List<Point>>();
        while (remainingPoints.Count >= minInliers)
        {
            List<Point> inliers = Ransac(remainingPoints, threshold, iterations);
            if (inliers.Count < minInliers)
                break;

            pointSets.Add(inliers);
            remainingPoints = remainingPoints.Except(inliers).ToList();
        }

        var result = new List<PointSetWithScore>(pointSets.Count);
        foreach (List<Point> pointSet in pointSets)
            result.Add(new PointSetWithScore(pointSet, CalculateScore(pointSet)));

        result.Sort((a, b) => b.Score.CompareTo(a.Score));
        return result;
    }

    private static List<Point> Ransac(IReadOnlyList<Point> points, double threshold, int iterations)
    {
        var random = new Random();
        var bestInliers = new List<Point>();
        for (int i = 0; i < iterations; i++)
        {
            int index1 = random.Next(points.Count);
            int index2 = random.Next(points.Count);
            if (index1 == index2)
                continue;

            Point p1 = points[index1];
            Point p2 = points[index2];
            if (p1.X == p2.X && p1.Y == p2.Y)
                continue;

            var inliers = new List<Point>();
            foreach (Point point in points)
            {
                if (DistancePointToLine(point, p1, p2) < threshold)
                    inliers.Add(point);
            }

            if (inliers.Count > bestInliers.Count)
                bestInliers = inliers;
        }

        return bestInliers;
    }

    private static double CalculateMinDistance(IReadOnlyList<Point> points)
    {
        if (points.Count < 2)
            return 0.0;

        double minDist = double.MaxValue;
        for (int i = 0; i < points.Count; i++)
        {
            for (int j = 0; j < points.Count; j++)
            {
                if (i == j)
                    continue;

                minDist = Math.Min(minDist, Distance(points[i], points[j]));
            }
        }

        return minDist;
    }

    private static double CalculateScore(IReadOnlyList<Point> points)
    {
        int pointCount = points.Count;
        if (pointCount < 2)
            return pointCount;

        double xMean = points.Average(point => point.X);
        double yMean = points.Average(point => point.Y);
        double ssxx = points.Sum(point => (point.X - xMean) * (point.X - xMean));
        double ssyy = points.Sum(point => (point.Y - yMean) * (point.Y - yMean));
        double ssxy = points.Sum(point => (point.X - xMean) * (point.Y - yMean));
        double theta = 0.5 * Math.Atan2(2.0 * ssxy, ssxx - ssyy);
        double dx = Math.Cos(theta);
        double dy = Math.Sin(theta);

        List<double> projections = points
            .Select(point => dx * (point.X - xMean) + dy * (point.Y - yMean))
            .ToList();
        double projLength = projections.Max() - projections.Min();
        if (projLength < 1e-6)
            return pointCount;

        double meanDist = points
            .Select(point => Math.Abs(-dy * (point.X - xMean) + dx * (point.Y - yMean)))
            .Average();
        double normalizedDist = meanDist / projLength;
        return pointCount * Math.Exp(-normalizedDist);
    }

    private static double DistancePointToLine(Point p, Point a, Point b)
    {
        double numerator = Math.Abs((b.X - a.X) * (a.Y - p.Y) - (a.X - p.X) * (b.Y - a.Y));
        double denominator = Distance(a, b);
        if (denominator == 0.0)
            return Distance(p, a);

        return numerator / denominator;
    }

    private static double Distance(Point a, Point b)
    {
        return Math.Sqrt(Math.Pow(a.X - b.X, 2.0) + Math.Pow(a.Y - b.Y, 2.0));
    }

    private static bool IsValidDepth(double value, double minDepth, double maxDepth, double invalidValue)
    {
        return double.IsFinite(value)
            && value >= minDepth
            && value <= maxDepth
            && Math.Abs(value - invalidValue) >= 1e-6;
    }

    private static List<float[]> ReadRealRows(HObject image, int width, int height)
    {
        HOperatorSet.GetImagePointer1(image, out HTuple pointer, out HTuple type, out HTuple imageWidth, out HTuple imageHeight);
        try
        {
            if (imageWidth.I != width || imageHeight.I != height)
                throw new InvalidOperationException("Pitch calibration image dimensions changed while reading frame data.");

            var imageData = new float[width * height];
            Marshal.Copy(pointer.IP, imageData, 0, imageData.Length);

            var rows = new List<float[]>(height);
            for (int row = 0; row < height; row++)
            {
                var rowData = new float[width];
                Array.Copy(imageData, row * width, rowData, 0, width);
                rows.Add(rowData);
            }

            return rows;
        }
        finally
        {
            imageHeight.Dispose();
            imageWidth.Dispose();
            type.Dispose();
            pointer.Dispose();
        }
    }

    private static void ValidateRowData(IReadOnlyList<float[]> data, string paramName)
    {
        if (data.Count == 0)
            return;

        if (data[0] == null)
            throw new ArgumentException("Pitch calibration rows must not be null.", paramName);

        int width = data[0].Length;
        for (int i = 1; i < data.Count; i++)
        {
            if (data[i] == null)
                throw new ArgumentException("Pitch calibration rows must not be null.", paramName);
            if (data[i].Length != width)
                throw new ArgumentException("Pitch calibration rows must have matching widths.", paramName);
        }
    }

    private static PitchCalibrationState CreateUncalibrated()
    {
        return new PitchCalibrationState(false, 0.0, Array.Empty<float>());
    }

    private readonly record struct Point(double X, double Y);

    private sealed class PointSetWithScore
    {
        public PointSetWithScore(List<Point> points, double score)
        {
            Points = points;
            Score = score;
        }

        public List<Point> Points { get; }

        public double Score { get; }
    }
}
