using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ReeYin_V.Core.Calibration
{
    using MultiCameraCalibHandle = IntPtr;

    public sealed class MultiCameraCalibrationSdk : IDisposable
    {
        public enum MultiCameraAnchorMode
        {
            BoardPose = 0,
            Camera = 1,
            External = 2
        }

        public enum MultiCameraBlendMode
        {
            Overlay = 0,
            Average = 1
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct MultiCameraCalibrationOptions
        {
            public int anchorMode;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string referenceCameraId;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string referenceCaptureId;

            public int refineIntrinsics;
            public int refineDistortion;
            public int minCornersPerObservation;
            public double maxReprojectionErrorForInit;
            public double robustLossScale;
            public int maxIterations;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct MultiCameraCameraParams
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string cameraId;

            public int imageWidth;
            public int imageHeight;
            public double rmsError;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
            public double[] intrinsic;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public double[] distortion;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public double[] rvecCommonFromCamera;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public double[] tvecCommonFromCamera;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public double[] extrinsicCommonFromCamera;

            public int hasIntrinsic;
            public int hasDistortion;
            public int hasExtrinsicCommonFromCamera;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MultiCameraCalibrationReport
        {
            public int cameraCount;
            public int captureCount;
            public int observationCount;
            public int residualCount;
            public double initialRmsError;
            public double finalRmsError;
            public double maxReprojectionError;
            public int connectedComponentCount;
            public int converged;
            public int ceresTerminationType;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct MultiCameraImageInput
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string cameraId;

            public IntPtr imageData;
            public int width;
            public int height;
            public int channels;
            public int cvType;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MultiCameraImageOutput
        {
            public IntPtr imageData;
            public int width;
            public int height;
            public int channels;
            public int cvType;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MeasurementPlaneParams
        {
            public double heightCompensation;
        }

        private MultiCameraCalibHandle _handle;
        private bool _disposed;

        public MultiCameraCalibrationSdk()
        {
            _handle = Native.createMultiCameraCalibrationFramework();
            if (_handle == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to create multi-camera calibration framework.");
            }
        }

        ~MultiCameraCalibrationSdk()
        {
            Dispose(false);
        }

        public static MultiCameraCalibrationOptions CreateDefaultOptions()
        {
            return new MultiCameraCalibrationOptions
            {
                anchorMode = (int)MultiCameraAnchorMode.BoardPose,
                referenceCameraId = string.Empty,
                referenceCaptureId = string.Empty,
                refineIntrinsics = 0,
                refineDistortion = 0,
                minCornersPerObservation = 6,
                //maxReprojectionErrorForInit = 5.0,
                maxReprojectionErrorForInit = 1e12,
                robustLossScale = 1.0,
                maxIterations = 100
            };
        }

        public static MultiCameraCameraParams CreateCameraParams()
        {
            return new MultiCameraCameraParams
            {
                cameraId = string.Empty,
                intrinsic = new double[9],
                distortion = new double[8],
                rvecCommonFromCamera = new double[3],
                tvecCommonFromCamera = new double[3],
                extrinsicCommonFromCamera = new double[16]
            };
        }

        public void SetCalibrationBoardParams(CameraCalibrationSdk.CalibrationBoardParams param)
        {
            EnsureNotDisposed();
            int rc = Native.multiSetCalibrationBoardParams(_handle, ref param);
            ThrowIfError(rc);
        }

        public void SetMeasurementPlaneParams(MeasurementPlaneParams param)
        {
            EnsureNotDisposed();
            int rc = Native.multiSetMeasurementPlaneParams(_handle, ref param);
            ThrowIfError(rc);
        }

        public MeasurementPlaneParams GetMeasurementPlaneParams()
        {
            EnsureNotDisposed();
            MeasurementPlaneParams param = default;
            int rc = Native.multiGetMeasurementPlaneParams(_handle, ref param);
            ThrowIfError(rc);
            return param;
        }

        public void SetOptions(MultiCameraCalibrationOptions options)
        {
            EnsureNotDisposed();
            int rc = Native.multiSetCalibrationOptions(_handle, ref options);
            ThrowIfError(rc);
        }

        public void AddCamera(string cameraId, int imageWidth, int imageHeight)
        {
            EnsureNotDisposed();
            int rc = Native.multiAddCamera(_handle, cameraId, imageWidth, imageHeight);
            ThrowIfError(rc);
        }

        public void SetInitialCameraParams(string cameraId, CameraCalibrationSdk.CameraParams param, bool fixIntrinsic)
        {
            EnsureNotDisposed();
            int rc = Native.multiSetInitialCameraParams(_handle, cameraId, ref param, fixIntrinsic ? 1 : 0);
            ThrowIfError(rc);
        }

        public void AddObservationImagePath(string cameraId, string captureId, string imagePath)
        {
            EnsureNotDisposed();
            int rc = Native.multiAddObservationImagePath(_handle, cameraId, captureId, imagePath);
            ThrowIfError(rc);
        }

        public void Calibrate()
        {
            EnsureNotDisposed();
            int rc = Native.multiCalibrate(_handle);
            ThrowIfError(rc);
        }

        public MultiCameraCameraParams GetCameraParams(string cameraId)
        {
            EnsureNotDisposed();
            MultiCameraCameraParams param = CreateCameraParams();
            int rc = Native.multiGetCameraParams(_handle, cameraId, ref param);
            ThrowIfError(rc);
            return param;
        }

        public MultiCameraCalibrationReport GetReport()
        {
            EnsureNotDisposed();
            MultiCameraCalibrationReport report = default;
            int rc = Native.multiGetCalibrationReport(_handle, ref report);
            ThrowIfError(rc);
            return report;
        }

        public IReadOnlyList<string> GetCameraIds()
        {
            EnsureNotDisposed();

            int cameraCount = 0;
            int rc = Native.multiGetCameraCount(_handle, out cameraCount);
            ThrowIfError(rc);

            var cameraIds = new List<string>(Math.Max(cameraCount, 0));
            for (int index = 0; index < cameraCount; index++)
            {
                var buffer = new StringBuilder(256);
                rc = Native.multiGetCameraId(_handle, index, buffer, buffer.Capacity);
                ThrowIfError(rc);
                cameraIds.Add(buffer.ToString());
            }

            return cameraIds;
        }

        public void PixelToCommonWorld(
            string cameraId,
            double pixelX,
            double pixelY,
            out double worldX,
            out double worldY,
            out double worldZ)
        {
            EnsureNotDisposed();

            worldX = double.NegativeInfinity;
            worldY = double.NegativeInfinity;
            worldZ = double.NegativeInfinity;

            int rc = Native.multiPixelToCommonWorld(_handle, cameraId, pixelX, pixelY, out worldX, out worldY, out worldZ);
            ThrowIfError(rc);
        }

        public void CommonWorldToPixel(
            string cameraId,
            double worldX,
            double worldY,
            double worldZ,
            out double pixelX,
            out double pixelY)
        {
            EnsureNotDisposed();

            pixelX = double.NegativeInfinity;
            pixelY = double.NegativeInfinity;

            int rc = Native.multiCommonWorldToPixel(_handle, cameraId, worldX, worldY, worldZ, out pixelX, out pixelY);
            ThrowIfError(rc);
        }

        public MultiCameraImageOutput StitchImages(
            IEnumerable<MultiCameraImageInput> images,
            MultiCameraBlendMode blendMode)
        {
            EnsureNotDisposed();

            if (images == null)
            {
                throw new ArgumentNullException(nameof(images));
            }

            MultiCameraImageInput[] imageArray = images.ToArray();
            if (imageArray.Length == 0)
            {
                throw new ArgumentException("At least one image is required.", nameof(images));
            }

            MultiCameraImageOutput output = default;
            int rc = Native.multiStitchImages(_handle, imageArray, imageArray.Length, blendMode, ref output);
            ThrowIfError(rc);
            return output;
        }

        public void Save(string outputFile)
        {
            EnsureNotDisposed();
            int rc = Native.multiSaveCalibrationResults(_handle, outputFile);
            ThrowIfError(rc);
        }

        public void SaveCalibrationResults(string outputFile)
        {
            Save(outputFile);
        }

        public void Load(string filePath)
        {
            EnsureNotDisposed();
            int rc = Native.multiLoadCalibrationFile(_handle, filePath);
            ThrowIfError(rc);
        }

        public void LoadCalibrationFile(string filePath)
        {
            Load(filePath);
        }

        public static void FreePtr(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return;
            }

            int rc = Native.freePtr(ptr);
            ThrowIfError(rc);
        }

        public void DestroyMultiCameraCalibration()
        {
            if (_handle == IntPtr.Zero)
            {
                return;
            }

            Native.destroyMultiCameraCalibrationFramework(_handle);
            _handle = IntPtr.Zero;
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (_handle != IntPtr.Zero)
            {
                Native.destroyMultiCameraCalibrationFramework(_handle);
                _handle = IntPtr.Zero;
            }

            _disposed = true;
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(MultiCameraCalibrationSdk));
            }
        }

        private static void ThrowIfError(int rc)
        {
            if (rc == 0)
            {
                return;
            }

            IntPtr errorPtr = Native.getLastError();
            string message = errorPtr == IntPtr.Zero
                ? "Unknown native error"
                : Marshal.PtrToStringAnsi(errorPtr) ?? "Unknown native error";

            throw new ArgumentException($"error {rc}: {message}");
        }

        private static class Native
        {
            [DllImport("ALGO.CalibrationNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr getLastError();

            [DllImport("ALGO.CalibrationNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern MultiCameraCalibHandle createMultiCameraCalibrationFramework();

            [DllImport("ALGO.CalibrationNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void destroyMultiCameraCalibrationFramework(MultiCameraCalibHandle handle);

            [DllImport("ALGO.CalibrationNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int multiSetCalibrationBoardParams(
                MultiCameraCalibHandle handle,
                ref CameraCalibrationSdk.CalibrationBoardParams param);

            [DllImport("ALGO.CalibrationNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int multiSetMeasurementPlaneParams(
                MultiCameraCalibHandle handle,
                ref MeasurementPlaneParams param);

            [DllImport("ALGO.CalibrationNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int multiGetMeasurementPlaneParams(
                MultiCameraCalibHandle handle,
                ref MeasurementPlaneParams param);

            [DllImport("ALGO.CalibrationNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int multiSetCalibrationOptions(
                MultiCameraCalibHandle handle,
                ref MultiCameraCalibrationOptions options);

            [DllImport("ALGO.CalibrationNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int multiAddCamera(
                MultiCameraCalibHandle handle,
                [MarshalAs(UnmanagedType.LPStr)] string cameraId,
                int imageWidth,
                int imageHeight);

            [DllImport("ALGO.CalibrationNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int multiSetInitialCameraParams(
                MultiCameraCalibHandle handle,
                [MarshalAs(UnmanagedType.LPStr)] string cameraId,
                ref CameraCalibrationSdk.CameraParams param,
                int fixIntrinsic);

            [DllImport("ALGO.CalibrationNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int multiAddObservationImagePath(
                MultiCameraCalibHandle handle,
                [MarshalAs(UnmanagedType.LPStr)] string cameraId,
                [MarshalAs(UnmanagedType.LPStr)] string captureId,
                [MarshalAs(UnmanagedType.LPStr)] string imagePath);

            [DllImport("ALGO.CalibrationNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int multiCalibrate(MultiCameraCalibHandle handle);

            [DllImport("ALGO.CalibrationNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int multiGetCameraParams(
                MultiCameraCalibHandle handle,
                [MarshalAs(UnmanagedType.LPStr)] string cameraId,
                ref MultiCameraCameraParams param);

            [DllImport("ALGO.CalibrationNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int multiGetCalibrationReport(
                MultiCameraCalibHandle handle,
                ref MultiCameraCalibrationReport report);

            [DllImport("ALGO.CalibrationNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int multiGetCameraCount(
                MultiCameraCalibHandle handle,
                out int cameraCount);

            [DllImport("ALGO.CalibrationNative.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern int multiGetCameraId(
                MultiCameraCalibHandle handle,
                int index,
                [MarshalAs(UnmanagedType.LPStr)] StringBuilder cameraId,
                int cameraIdCapacity);

            [DllImport("ALGO.CalibrationNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int multiPixelToCommonWorld(
                MultiCameraCalibHandle handle,
                [MarshalAs(UnmanagedType.LPStr)] string cameraId,
                double pixelX,
                double pixelY,
                out double worldX,
                out double worldY,
                out double worldZ);

            [DllImport("ALGO.CalibrationNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int multiCommonWorldToPixel(
                MultiCameraCalibHandle handle,
                [MarshalAs(UnmanagedType.LPStr)] string cameraId,
                double worldX,
                double worldY,
                double worldZ,
                out double pixelX,
                out double pixelY);

            [DllImport("ALGO.CalibrationNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int multiStitchImages(
                MultiCameraCalibHandle handle,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] MultiCameraImageInput[] images,
                int imageCount,
                MultiCameraBlendMode blendMode,
                ref MultiCameraImageOutput output);

            [DllImport("ALGO.CalibrationNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int multiSaveCalibrationResults(
                MultiCameraCalibHandle handle,
                [MarshalAs(UnmanagedType.LPStr)] string outputFile);

            [DllImport("ALGO.CalibrationNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int multiLoadCalibrationFile(
                MultiCameraCalibHandle handle,
                [MarshalAs(UnmanagedType.LPStr)] string filePath);

            [DllImport("ALGO.CalibrationNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int freePtr(IntPtr ptr);
        }
    }
}
