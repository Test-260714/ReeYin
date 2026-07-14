using System.Runtime.InteropServices;
using PointCloud.Interop;

namespace PointCloud.Algorithms.Native;

internal static class PclAlgorithmsNative
{
    private const string DllName = "ALGO.PCLAlgorithmsNative.dll";

    [DllImport(DllName, EntryPoint = "voxelDownSample", CallingConvention = CallingConvention.StdCall)]
    internal static extern void VoxelDownSample(PointCloudHandle input, double leafSize, PointCloudHandle output);

    [DllImport(DllName, EntryPoint = "approximateVoxelDownSample", CallingConvention = CallingConvention.StdCall)]
    internal static extern void ApproximateVoxelDownSample(PointCloudHandle input, double leafSize, PointCloudHandle output);

    [DllImport(DllName, EntryPoint = "uniformDownSample", CallingConvention = CallingConvention.StdCall)]
    internal static extern void UniformDownSample(PointCloudHandle input, double radius, PointCloudHandle output);

    [DllImport(DllName, EntryPoint = "passThroughFilter", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    internal static extern void PassThroughFilter(
        PointCloudHandle input,
        [MarshalAs(UnmanagedType.LPStr)] string axisName,
        float limitMin,
        float limitMax,
        int negative,
        PointCloudHandle output);

    [DllImport(DllName, EntryPoint = "staFilter", CallingConvention = CallingConvention.StdCall)]
    internal static extern void StatisticalFilter(PointCloudHandle input, int neighborNum, float threshold, PointCloudHandle output);

    [DllImport(DllName, EntryPoint = "radiusFilter", CallingConvention = CallingConvention.StdCall)]
    internal static extern void RadiusFilter(PointCloudHandle input, double radius, int numThreshold, PointCloudHandle output);

    [DllImport(DllName, EntryPoint = "constrainedIcpRegistration", CallingConvention = CallingConvention.StdCall)]
    internal static extern int ConstrainedIcpRegistration(
        PointCloudHandle source,
        PointCloudHandle target,
        double initialX,
        double initialY,
        double initialZ,
        double initialYawDeg,
        int maxIterations,
        double maxCorrespondenceDistance,
        double transformationEpsilon,
        double fitnessEpsilon,
        int optimizationMask,
        [Out] double[] outTransform,
        [Out] double[] outMetrics);

    [DllImport(DllName, EntryPoint = "modifiedGrowRegion", CallingConvention = CallingConvention.StdCall)]
    internal static extern void ModifiedGrowRegion(
        PointCloudHandle input,
        int neighborNum,
        float smoothThreshold,
        float curvaThreshold,
        int minClusterSize,
        int maxClusterSize,
        PointCloudHandle output);

    [DllImport(DllName, EntryPoint = "oriGrowRegion", CallingConvention = CallingConvention.StdCall)]
    internal static extern void OriginalGrowRegion(
        PointCloudHandle input,
        int neighborNum,
        float smoothThreshold,
        float curvaThreshold,
        int minClusterSize,
        int maxClusterSize,
        PointIndicesHandle output);

    [DllImport(DllName, EntryPoint = "euclideanCluster", CallingConvention = CallingConvention.StdCall)]
    internal static extern void EuclideanCluster(
        PointCloudHandle input,
        double distanceThreshold,
        int minClusterSize,
        int maxClusterSize,
        PointIndicesHandle output);

    [DllImport(DllName, EntryPoint = "fitPlane", CallingConvention = CallingConvention.StdCall)]
    internal static extern float FitPlane(PointCloudHandle input, float distanceThreshold, int maxIterations, [Out] float[] normal);

    [DllImport(DllName, EntryPoint = "calculatePointsToPlaneDistance", CallingConvention = CallingConvention.StdCall)]
    internal static extern void CalculatePointsToPlaneDistance(
        PointCloudHandle input,
        [In] float[] planeCoefficients,
        [Out] double[] distances,
        [Out] double[] statistics);

    [DllImport(DllName, EntryPoint = "measureHeightFromBasePlane", CallingConvention = CallingConvention.StdCall)]
    internal static extern double MeasureHeightFromBasePlane(PointCloudHandle input, [In] float[] basePlane, out int maxPointIndex);

    [DllImport(DllName, EntryPoint = "separateBaseAndProtrusion", CallingConvention = CallingConvention.StdCall)]
    internal static extern void SeparateBaseAndProtrusion(
        PointCloudHandle input,
        float heightThreshold,
        [In] float[] basePlane,
        PointCloudHandle outputBase,
        PointCloudHandle outputProtrusion);

    [DllImport(DllName, EntryPoint = "filterPointsByROI", CallingConvention = CallingConvention.StdCall)]
    internal static extern void FilterPointsByRoi(
        PointCloudHandle input,
        double centerX,
        double centerY,
        double width,
        double height,
        PointCloudHandle output);

    [DllImport(DllName, EntryPoint = "suggestROIForProtrusion", CallingConvention = CallingConvention.StdCall)]
    internal static extern int SuggestRoiForProtrusion(
        PointCloudHandle input,
        [In] float[] basePlane,
        double heightThreshold,
        out double roiX,
        out double roiY,
        out double roiWidth,
        out double roiHeight);

    [DllImport(DllName, EntryPoint = "measureHeightWithROI", CallingConvention = CallingConvention.StdCall)]
    internal static extern double MeasureHeightWithRoi(
        PointCloudHandle input,
        [In] float[] basePlane,
        [MarshalAs(UnmanagedType.I1)] bool useAutoRoi,
        double roiX,
        double roiY,
        double roiWidth,
        double roiHeight,
        out int maxPointIndex,
        out double usedRoiX,
        out double usedRoiY,
        out double usedRoiWidth,
        out double usedRoiHeight);

    [DllImport(DllName, EntryPoint = "createMaxPointMarker", CallingConvention = CallingConvention.StdCall)]
    internal static extern void CreateMaxPointMarker(
        PointCloudHandle input,
        int maxPointIndex,
        double markerRadius,
        PointCloudHandle outputMarker);

    [DllImport(DllName, EntryPoint = "calculateFlatness", CallingConvention = CallingConvention.StdCall)]
    internal static extern double CalculateFlatness(PointCloudHandle input, int flatnessType, [Out] float[] plane);

    [DllImport(DllName, EntryPoint = "calculateRegionalFlatness", CallingConvention = CallingConvention.StdCall)]
    internal static extern double CalculateRegionalFlatness(
        PointCloudHandle input,
        int gridSize,
        [Out] double[] flatnessMap,
        [Out] int[] pointCounts);

    [DllImport(DllName, EntryPoint = "pointToPlaneDistance", CallingConvention = CallingConvention.StdCall)]
    internal static extern double PointToPlaneDistance([In] double[] point, [In] float[] plane);

    [DllImport(DllName, EntryPoint = "projectPointsToPlane", CallingConvention = CallingConvention.StdCall)]
    internal static extern void ProjectPointsToPlane(PointCloudHandle input, [In] float[] plane, PointCloudHandle output);

    [DllImport(DllName, EntryPoint = "normalizePlaneCoefficients", CallingConvention = CallingConvention.StdCall)]
    internal static extern void NormalizePlaneCoefficients([In, Out] float[] plane);

    [DllImport(DllName, EntryPoint = "measureMultipleHeights", CallingConvention = CallingConvention.StdCall)]
    internal static extern double MeasureMultipleHeights(
        PointCloudHandle input,
        [In] float[] basePlane,
        double clusteringDistance,
        [Out] double[] heights,
        out int protrusionCount,
        int maxProtrusions);

    [DllImport(DllName, EntryPoint = "generateFlatnessHeatmap", CallingConvention = CallingConvention.StdCall)]
    internal static extern void GenerateFlatnessHeatmap(PointCloudHandle input, int gridSize, [In] float[] plane, PointCloudHandle output);

    [DllImport(DllName, EntryPoint = "correctPlane", CallingConvention = CallingConvention.StdCall)]
    internal static extern void CorrectPlane(PointCloudHandle input, [In] float[] normal, PointCloudHandle output);

    [DllImport(DllName, EntryPoint = "sigamFilter", CallingConvention = CallingConvention.StdCall)]
    internal static extern void SigmaFilter(PointCloudHandle input, int sigmaThreshold, PointCloudHandle output);

    [DllImport(DllName, EntryPoint = "getRunoutPoints", CallingConvention = CallingConvention.StdCall)]
    internal static extern void GetRunoutPoints(PointCloudHandle input, int count, PointCloudHandle output);

    [DllImport(DllName, EntryPoint = "getRunoutPointsWithResult", CallingConvention = CallingConvention.StdCall)]
    internal static extern float GetRunoutPointsWithResult(PointCloudHandle input, int count, PointCloudHandle output);

    [DllImport(DllName, EntryPoint = "calculateRunout", CallingConvention = CallingConvention.StdCall)]
    internal static extern double CalculateRunout(PointCloudHandle input, [Out] int[] indices);

    [DllImport(DllName, EntryPoint = "copyPcBaseOnIndice", CallingConvention = CallingConvention.StdCall)]
    internal static extern void CopyPointCloud(PointCloudHandle input, PointCloudHandle output);

    [DllImport(DllName, EntryPoint = "extractClusterPointCloud", CallingConvention = CallingConvention.StdCall)]
    internal static extern void ExtractClusterPointCloud(
        PointCloudHandle input,
        PointIndicesHandle indices,
        int clusterIndex,
        PointCloudHandle output);
}
