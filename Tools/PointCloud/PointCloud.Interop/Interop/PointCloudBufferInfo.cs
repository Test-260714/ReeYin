namespace PointCloud.Interop;

public readonly record struct PointCloudBufferInfo(
    int Count,
    int StrideBytes,
    IntPtr BufferPointer);
