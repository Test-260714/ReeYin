using HalconDotNet;
using PointCloudSharp;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ImageTool.VTKPCDisplay.Model
{
    /// <summary>
    /// Convert Halcon HObject image to PointCloudXYZ.
    /// </summary>
    public static class HObjectToPointCloudXYZ
    {
        public static PointCloudXYZ Convert(
            HObject image,
            double dxUm = 1,
            double dyUm = 1,
            double dzUm = 1,
            bool centerOrigin = false,
            int stride = 1,
            bool filterNonPositive = true)
        {
            var cloud = new PointCloudXYZ();
            ConvertInto(image, cloud, dxUm, dyUm, dzUm, centerOrigin, stride, filterNonPositive);
            return cloud;
        }

        public static void ConvertInto(
            HObject image,
            PointCloudXYZ cloud,
            double dxUm = 1,
            double dyUm = 1,
            double dzUm = 1,
            bool centerOrigin = false,
            int stride = 1,
            bool filterNonPositive = true)
        {
            ConvertIntoCore(image, cloud, dxUm, dyUm, dzUm, centerOrigin, stride, maxPoints: 0, progress: null, token: CancellationToken.None, filterNonPositive: filterNonPositive);
        }

        public static Task ConvertIntoAsync(
            HObject image,
            PointCloudXYZ cloud,
            IProgress<int> progress,
            CancellationToken token,
            double dxUm = 1,
            double dyUm = 1,
            double dzUm = 1,
            bool centerOrigin = false,
            int stride = 1,
            bool filterNonPositive = true)
        {
            return Task.Run(() =>
            {
                ConvertIntoCore(image, cloud, dxUm, dyUm, dzUm, centerOrigin, stride, maxPoints: 0, progress: progress, token: token, filterNonPositive: filterNonPositive);
            }, token);
        }

        public static Task ConvertPreviewIntoAsync(
            HObject image,
            PointCloudXYZ previewCloud,
            int maxPoints,
            CancellationToken token,
            double dxUm = 1,
            double dyUm = 1,
            double dzUm = 1,
            bool centerOrigin = false,
            bool filterNonPositive = true)
        {
            return Task.Run(() =>
            {
                ConvertIntoCore(image, previewCloud, dxUm, dyUm, dzUm, centerOrigin, stride: 0, maxPoints: maxPoints, progress: null, token: token, filterNonPositive: filterNonPositive);
            }, token);
        }

        public static async Task ConvertProgressiveAsync(
            HObject image,
            PointCloudXYZ previewCloud,
            PointCloudXYZ fullCloud,
            int previewMaxPoints,
            Action onPreviewReady,
            Action onFullReady,
            IProgress<int> fullProgress,
            CancellationToken token,
            Action<Action> uiInvoke,
            double dxUm = 1,
            double dyUm = 1,
            double dzUm = 1,
            bool centerOrigin = false,
            bool filterNonPositive = true)
        {
            await ConvertPreviewIntoAsync(image, previewCloud, previewMaxPoints, token, dxUm, dyUm, dzUm, centerOrigin, filterNonPositive).ConfigureAwait(false);
            if (onPreviewReady != null) uiInvoke(onPreviewReady);

            await ConvertIntoAsync(image, fullCloud, fullProgress, token, dxUm, dyUm, dzUm, centerOrigin, stride: 1, filterNonPositive: filterNonPositive).ConfigureAwait(false);
            if (onFullReady != null) uiInvoke(onFullReady);
        }

        private static void ConvertIntoCore(
            HObject image,
            PointCloudXYZ cloud,
            double dxUm,
            double dyUm,
            double dzUm,
            bool centerOrigin,
            int stride,
            int maxPoints,
            IProgress<int> progress,
            CancellationToken token,
            bool filterNonPositive)
        {
            if (cloud == null) throw new ArgumentNullException(nameof(cloud));
            if (image == null || !image.IsInitialized()) throw new ArgumentNullException(nameof(image), "HObject image is null or not initialized.");

            HOperatorSet.GetImageSize(image, out HTuple hvWidth, out HTuple hvHeight);
            int w = hvWidth.I;
            int h = hvHeight.I;

            if (w <= 0 || h <= 0)
            {
                cloud.Clear();
                return;
            }

            if (stride <= 0)
            {
                if (maxPoints > 0)
                {
                    double total = (double)w * h;
                    stride = (int)Math.Ceiling(Math.Sqrt(total / Math.Max(1, maxPoints)));
                }
                else
                {
                    stride = 1;
                }
            }

            if (stride < 1) stride = 1;

            double cx = (w - 1) * 0.5;
            double cy = (h - 1) * 0.5;

            HOperatorSet.GetImagePointer1(image, out HTuple hvPointer, out HTuple hvType, out _, out _);
            string type = hvType.S?.ToLowerInvariant() ?? string.Empty;

            var reader = CreateValueReader(hvPointer, type, w * h);

            int count = 0;
            const int reportRowStep = 32;
            for (int v = 0; v < h; v += stride)
            {
                if ((v & 0x3F) == 0) token.ThrowIfCancellationRequested();

                int row = v * w;
                for (int u = 0; u < w; u += stride)
                {
                    double val = reader(row + u);
                    if (!IsValid(val, filterNonPositive)) continue;
                    count++;
                }

                if (progress != null && (v % reportRowStep) == 0)
                    progress.Report(Math.Min(v * w, w * h));
            }

            cloud.Clear();
            cloud.ReSize(count);

            int idx = 0;
            for (int v = 0; v < h; v += stride)
            {
                if ((v & 0x3F) == 0) token.ThrowIfCancellationRequested();

                int row = v * w;
                for (int u = 0; u < w; u += stride)
                {
                    double val = reader(row + u);
                    if (!IsValid(val, filterNonPositive)) continue;

                    double uu = centerOrigin ? (u - cx) : u;
                    double vv = centerOrigin ? (v - cy) : v;

                    double x = uu * dxUm;
                    double y = -vv * dyUm;
                    double z = val * dzUm;

                    cloud.SetX(idx, x);
                    cloud.SetY(idx, y);
                    cloud.SetZ(idx, z);
                    idx++;
                }

                if (progress != null && (v % reportRowStep) == 0)
                    progress.Report(idx);
            }

            if (progress != null) progress.Report(idx);
        }

        private static Func<int, double> CreateValueReader(HTuple pointer, string type, int length)
        {
            if (pointer == null || pointer.IP == IntPtr.Zero)
                throw new InvalidOperationException("Failed to get image pointer.");

            switch (type)
            {
                case "byte":
                    {
                        byte[] data = new byte[length];
                        Marshal.Copy(pointer.IP, data, 0, data.Length);
                        return i => data[i];
                    }
                case "int2":
                    {
                        short[] data = new short[length];
                        Marshal.Copy(pointer.IP, data, 0, data.Length);
                        return i => data[i];
                    }
                case "uint2":
                    {
                        short[] data = new short[length];
                        Marshal.Copy(pointer.IP, data, 0, data.Length);
                        return i => (ushort)data[i];
                    }
                case "int4":
                    {
                        int[] data = new int[length];
                        Marshal.Copy(pointer.IP, data, 0, data.Length);
                        return i => data[i];
                    }
                case "uint4":
                    {
                        int[] data = new int[length];
                        Marshal.Copy(pointer.IP, data, 0, data.Length);
                        return i => (uint)data[i];
                    }
                case "float":
                    {
                        float[] data = new float[length];
                        Marshal.Copy(pointer.IP, data, 0, data.Length);
                        return i => data[i];
                    }
                case "real":
                    {
                        double[] data = new double[length];
                        Marshal.Copy(pointer.IP, data, 0, data.Length);
                        return i => data[i];
                    }
                default:
                    throw new InvalidOperationException($"Unsupported Halcon image type: {type}");
            }
        }

        private static bool IsValid(double val, bool filterNonPositive)
        {
            if (double.IsNaN(val) || double.IsInfinity(val))
                return false;

            if (filterNonPositive && val <= 0)
                return false;

            return true;
        }
    }
}
