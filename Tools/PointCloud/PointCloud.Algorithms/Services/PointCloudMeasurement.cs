using PointCloud.Algorithms.Dtos;
using PointCloud.Algorithms.Native;
using PointCloud.Interop;

namespace PointCloud.Algorithms.Services;

public static class PointCloudMeasurement
{
    public static PointDistanceAnalysisResult CalculatePointsToPlaneDistance(PointCloudHandle input, PlaneModel plane)
    {
        ArgumentNullException.ThrowIfNull(plane);

        var count = PointClouds.Count(input);
        var distances = new double[count];
        var statistics = new double[5];
        PclAlgorithmsNative.CalculatePointsToPlaneDistance(input, plane.ToArray(), distances, statistics);

        return new PointDistanceAnalysisResult
        {
            Distances = distances,
            Statistics = PointDistanceStatistics.FromArray(statistics),
        };
    }

    public static HeightMeasurementResult MeasureHeightFromBasePlane(PointCloudHandle input, PlaneModel basePlane)
    {
        ArgumentNullException.ThrowIfNull(basePlane);

        var height = PclAlgorithmsNative.MeasureHeightFromBasePlane(input, basePlane.ToArray(), out var maxPointIndex);
        return new HeightMeasurementResult
        {
            Height = height,
            MaxPointIndex = maxPointIndex,
        };
    }

    public static SeparatedPointCloudResult SeparateBaseAndProtrusion(
        PointCloudHandle input,
        float heightThreshold,
        PlaneModel basePlane)
    {
        ArgumentNullException.ThrowIfNull(basePlane);

        var baseCloud = PointClouds.Create();
        var protrusionCloud = PointClouds.Create();
        PclAlgorithmsNative.SeparateBaseAndProtrusion(input, heightThreshold, basePlane.ToArray(), baseCloud, protrusionCloud);

        return new SeparatedPointCloudResult
        {
            BasePointCloud = baseCloud,
            ProtrusionPointCloud = protrusionCloud,
        };
    }

    public static PointCloudHandle FilterByRoi(PointCloudHandle input, RoiRectangle roi)
    {
        ArgumentNullException.ThrowIfNull(roi);

        var output = PointClouds.Create();
        PclAlgorithmsNative.FilterPointsByRoi(input, roi.CenterX, roi.CenterY, roi.Width, roi.Height, output);
        return output;
    }

    public static RoiSuggestionResult SuggestRoiForProtrusion(
        PointCloudHandle input,
        PlaneModel basePlane,
        double heightThreshold)
    {
        ArgumentNullException.ThrowIfNull(basePlane);

        var count = PclAlgorithmsNative.SuggestRoiForProtrusion(
            input,
            basePlane.ToArray(),
            heightThreshold,
            out var roiX,
            out var roiY,
            out var roiWidth,
            out var roiHeight);

        return new RoiSuggestionResult
        {
            ProtrusionPointCount = count,
            Roi = new RoiRectangle
            {
                CenterX = roiX,
                CenterY = roiY,
                Width = roiWidth,
                Height = roiHeight,
            },
        };
    }

    public static HeightMeasurementResult MeasureHeightWithRoi(
        PointCloudHandle input,
        PlaneModel basePlane,
        bool useAutoRoi,
        RoiRectangle? roi = null)
    {
        ArgumentNullException.ThrowIfNull(basePlane);

        var roiX = roi?.CenterX ?? 0;
        var roiY = roi?.CenterY ?? 0;
        var roiWidth = roi?.Width ?? 0;
        var roiHeight = roi?.Height ?? 0;

        var height = PclAlgorithmsNative.MeasureHeightWithRoi(
            input,
            basePlane.ToArray(),
            useAutoRoi,
            roiX,
            roiY,
            roiWidth,
            roiHeight,
            out var maxPointIndex,
            out var usedRoiX,
            out var usedRoiY,
            out var usedRoiWidth,
            out var usedRoiHeight);

        return new HeightMeasurementResult
        {
            Height = height,
            MaxPointIndex = maxPointIndex,
            UsedRoi = new RoiRectangle
            {
                CenterX = usedRoiX,
                CenterY = usedRoiY,
                Width = usedRoiWidth,
                Height = usedRoiHeight,
            },
        };
    }

    public static PointCloudHandle CreateMaxPointMarker(PointCloudHandle input, int maxPointIndex, double markerRadius)
    {
        var output = PointClouds.Create();
        PclAlgorithmsNative.CreateMaxPointMarker(input, maxPointIndex, markerRadius, output);
        return output;
    }

    public static FlatnessResult CalculateFlatness(PointCloudHandle input, int flatnessType)
    {
        var plane = new float[4];
        var value = PclAlgorithmsNative.CalculateFlatness(input, flatnessType, plane);

        return new FlatnessResult
        {
            FlatnessType = flatnessType,
            Value = value,
            Plane = new PlaneModel(plane[0], plane[1], plane[2], plane[3]),
        };
    }

    public static FlatnessMapResult CalculateRegionalFlatness(PointCloudHandle input, int gridSize)
    {
        var length = gridSize * gridSize;
        var map = new double[length];
        var counts = new int[length];
        var overall = PclAlgorithmsNative.CalculateRegionalFlatness(input, gridSize, map, counts);

        return new FlatnessMapResult
        {
            GridSize = gridSize,
            OverallFlatness = overall,
            FlatnessMap = map,
            PointCounts = counts,
        };
    }

    public static double PointToPlaneDistance(Point3d point, PlaneModel plane)
    {
        ArgumentNullException.ThrowIfNull(plane);

        return PclAlgorithmsNative.PointToPlaneDistance(
            [point.X, point.Y, point.Z],
            plane.ToArray());
    }

    public static PointCloudHandle ProjectPointsToPlane(PointCloudHandle input, PlaneModel plane)
    {
        ArgumentNullException.ThrowIfNull(plane);

        var output = PointClouds.Create();
        PclAlgorithmsNative.ProjectPointsToPlane(input, plane.ToArray(), output);
        return output;
    }

    public static MultipleHeightMeasurementResult MeasureMultipleHeights(
        PointCloudHandle input,
        PlaneModel basePlane,
        double clusteringDistance,
        int maxProtrusions)
    {
        ArgumentNullException.ThrowIfNull(basePlane);

        var heights = new double[maxProtrusions];
        var maxHeight = PclAlgorithmsNative.MeasureMultipleHeights(
            input,
            basePlane.ToArray(),
            clusteringDistance,
            heights,
            out var protrusionCount,
            maxProtrusions);

        return new MultipleHeightMeasurementResult
        {
            MaxHeight = maxHeight,
            ProtrusionCount = protrusionCount,
            Heights = heights.Take(Math.Max(protrusionCount, 0)).ToArray(),
        };
    }

    public static PointCloudHandle GenerateFlatnessHeatmap(PointCloudHandle input, int gridSize, PlaneModel plane)
    {
        ArgumentNullException.ThrowIfNull(plane);

        var output = PointClouds.Create();
        PclAlgorithmsNative.GenerateFlatnessHeatmap(input, gridSize, plane.ToArray(), output);
        return output;
    }
}
