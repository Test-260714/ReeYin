namespace PointCloud.Interop;

public sealed class PointCloudInteropException : Exception
{
    public PointCloudInteropException(string message)
        : base(message)
    {
    }

    public PointCloudInteropException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
