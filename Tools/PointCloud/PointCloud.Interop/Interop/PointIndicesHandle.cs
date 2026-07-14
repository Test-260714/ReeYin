using Microsoft.Win32.SafeHandles;

namespace PointCloud.Interop;

public sealed class PointIndicesHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public PointIndicesHandle()
        : base(ownsHandle: true)
    {
    }

    internal PointIndicesHandle(IntPtr preexistingHandle, bool ownsHandle)
        : base(ownsHandle)
    {
        SetHandle(preexistingHandle);
    }

    protected override bool ReleaseHandle()
    {
        if (!IsInvalid)
        {
            Native.PclCoreNative.DeletePointIndices(handle);
        }

        return true;
    }
}
