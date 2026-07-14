using Microsoft.Win32.SafeHandles;

namespace PointCloud.Interop;

public sealed class PointCloudHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public PointCloudHandle()
        : base(ownsHandle: true)
    {
    }

    internal PointCloudHandle(IntPtr preexistingHandle, bool ownsHandle)
        : base(ownsHandle)
    {
        SetHandle(preexistingHandle);
    }

    protected override bool ReleaseHandle()
    {
        if (!IsInvalid)
        {
            Native.PclCoreNative.DeletePointCloud(handle);
        }

        return true;
    }
}
