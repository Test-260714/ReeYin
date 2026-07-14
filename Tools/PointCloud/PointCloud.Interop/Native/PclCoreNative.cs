using System.Runtime.InteropServices;

namespace PointCloud.Interop.Native;

internal static class PclCoreNative
{
    private const string DllName = "ALGO.PCLCoreNative.dll";

    [DllImport(DllName, EntryPoint = "CreatePointCloud", CallingConvention = CallingConvention.StdCall)]
    internal static extern PointCloudHandle CreatePointCloud();

    [DllImport(DllName, EntryPoint = "loadPlyFile", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    internal static extern int LoadPlyFile([MarshalAs(UnmanagedType.LPStr)] string path, PointCloudHandle pointCloud);

    [DllImport(DllName, EntryPoint = "loadPcdFile", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    internal static extern int LoadPcdFile([MarshalAs(UnmanagedType.LPStr)] string path, PointCloudHandle pointCloud);

    [DllImport(DllName, EntryPoint = "loadObjFile", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    internal static extern int LoadObjFile([MarshalAs(UnmanagedType.LPStr)] string path, PointCloudHandle pointCloud);

    [DllImport(DllName, EntryPoint = "loadTxtFile", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    internal static extern int LoadTxtFile([MarshalAs(UnmanagedType.LPStr)] string path, PointCloudHandle pointCloud);

    [DllImport(DllName, EntryPoint = "loadPointCloudFile", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    internal static extern int LoadPointCloudFile([MarshalAs(UnmanagedType.LPStr)] string path, PointCloudHandle pointCloud);

    [DllImport(DllName, EntryPoint = "loadDepthTiffFile", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    internal static extern int LoadDepthTiffFile(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string path,
        PointCloudHandle pointCloud,
        double spacingX,
        double spacingY,
        double spacingZ,
        double invalidValue,
        int useInvalidValue);

    [DllImport(DllName, EntryPoint = "savePcdFile", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    internal static extern void SavePcdFile([MarshalAs(UnmanagedType.LPStr)] string path, PointCloudHandle pointCloud, int binaryMode);

    [DllImport(DllName, EntryPoint = "savePlyFile", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    internal static extern void SavePlyFile([MarshalAs(UnmanagedType.LPStr)] string path, PointCloudHandle pointCloud, int binaryMode);

    [DllImport(DllName, EntryPoint = "stl2PointCloud", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    internal static extern void StlToPointCloud([MarshalAs(UnmanagedType.LPStr)] string path, PointCloudHandle pointCloud);

    [DllImport(DllName, EntryPoint = "saveObjFile", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    internal static extern void SaveObjFile([MarshalAs(UnmanagedType.LPStr)] string path, PointCloudHandle pointCloud);

    [DllImport(DllName, EntryPoint = "DeletePointCloud", CallingConvention = CallingConvention.StdCall)]
    internal static extern void DeletePointCloud(IntPtr pointCloud);

    [DllImport(DllName, EntryPoint = "CountPointCloud", CallingConvention = CallingConvention.StdCall)]
    internal static extern int CountPointCloud(PointCloudHandle pointCloud);

    [DllImport(DllName, EntryPoint = "getPointCloudH", CallingConvention = CallingConvention.StdCall)]
    internal static extern int GetPointCloudHeight(PointCloudHandle pointCloud);

    [DllImport(DllName, EntryPoint = "getPointCloudW", CallingConvention = CallingConvention.StdCall)]
    internal static extern int GetPointCloudWidth(PointCloudHandle pointCloud);

    [DllImport(DllName, EntryPoint = "getMinMaxXYZ", CallingConvention = CallingConvention.StdCall)]
    internal static extern void GetMinMaxXYZ(PointCloudHandle pointCloud, [Out] double[] result);

    [DllImport(DllName, EntryPoint = "getX", CallingConvention = CallingConvention.StdCall)]
    internal static extern double GetX(PointCloudHandle pointCloud, int index);

    [DllImport(DllName, EntryPoint = "getY", CallingConvention = CallingConvention.StdCall)]
    internal static extern double GetY(PointCloudHandle pointCloud, int index);

    [DllImport(DllName, EntryPoint = "getZ", CallingConvention = CallingConvention.StdCall)]
    internal static extern double GetZ(PointCloudHandle pointCloud, int index);

    [DllImport(DllName, EntryPoint = "setX", CallingConvention = CallingConvention.StdCall)]
    internal static extern void SetX(PointCloudHandle pointCloud, int index, double x);

    [DllImport(DllName, EntryPoint = "setY", CallingConvention = CallingConvention.StdCall)]
    internal static extern void SetY(PointCloudHandle pointCloud, int index, double y);

    [DllImport(DllName, EntryPoint = "setZ", CallingConvention = CallingConvention.StdCall)]
    internal static extern void SetZ(PointCloudHandle pointCloud, int index, double z);

    [DllImport(DllName, EntryPoint = "reSize", CallingConvention = CallingConvention.StdCall)]
    internal static extern void Resize(PointCloudHandle pointCloud, int size);

    [DllImport(DllName, EntryPoint = "push", CallingConvention = CallingConvention.StdCall)]
    internal static extern void Push(PointCloudHandle pointCloud, double x, double y, double z);

    [DllImport(DllName, EntryPoint = "pop", CallingConvention = CallingConvention.StdCall)]
    internal static extern void Pop(PointCloudHandle pointCloud);

    [DllImport(DllName, EntryPoint = "clear", CallingConvention = CallingConvention.StdCall)]
    internal static extern void Clear(PointCloudHandle pointCloud);

    [DllImport(DllName, EntryPoint = "getPointCloudInterleavedF32Ptr", CallingConvention = CallingConvention.StdCall)]
    internal static extern IntPtr GetPointCloudInterleavedF32Ptr(PointCloudHandle pointCloud);

    [DllImport(DllName, EntryPoint = "getPointCloudInterleavedStrideBytes", CallingConvention = CallingConvention.StdCall)]
    internal static extern int GetPointCloudInterleavedStrideBytes();

    [DllImport(DllName, EntryPoint = "copyPointCloudToSplitF64", CallingConvention = CallingConvention.StdCall)]
    internal static extern void CopyPointCloudToSplitF64(
        PointCloudHandle pointCloud,
        [Out] double[] outX,
        [Out] double[] outY,
        [Out] double[] outZ,
        int count);

    [DllImport(DllName, EntryPoint = "setPointCloudFromSplitF64", CallingConvention = CallingConvention.StdCall)]
    internal static extern void SetPointCloudFromSplitF64(
        PointCloudHandle pointCloud,
        [In] double[] inX,
        [In] double[] inY,
        [In] double[] inZ,
        int count);

    [DllImport(DllName, EntryPoint = "setPointCloudFromInterleavedF32", CallingConvention = CallingConvention.StdCall)]
    internal static extern void SetPointCloudFromInterleavedF32(
        PointCloudHandle pointCloud,
        [In] float[] xyzInterleaved,
        int count,
        int strideBytes);

    [DllImport(DllName, EntryPoint = "CreatePointIndices", CallingConvention = CallingConvention.StdCall)]
    internal static extern PointIndicesHandle CreatePointIndices();

    [DllImport(DllName, EntryPoint = "DeletePointIndices", CallingConvention = CallingConvention.StdCall)]
    internal static extern void DeletePointIndices(IntPtr pointIndices);

    [DllImport(DllName, EntryPoint = "CountPointIndices", CallingConvention = CallingConvention.StdCall)]
    internal static extern int CountPointIndices(PointIndicesHandle pointIndices);

    [DllImport(DllName, EntryPoint = "getSizeOfIndice", CallingConvention = CallingConvention.StdCall)]
    internal static extern int GetSizeOfIndice(PointIndicesHandle pointIndices, int position);
}
