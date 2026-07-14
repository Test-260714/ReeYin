using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ReeYin.Hardware.Sensor.Truelight3D.API
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct Truelight3DNativeImage
    {
        public IntPtr Data;
        public int Width;
        public int Height;
        public int Channel;
        public Truelight3DPixelFormat Format;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Truelight3DNativeDepthMap
    {
        public IntPtr Data;
        public int Width;
        public int Height;
        public float XScale;
        public float YScale;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Truelight3DNativePointXYZ
    {
        public float X;
        public float Y;
        public float Z;
        public float Padding;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Truelight3DNativePointCloudXYZ
    {
        public uint Width;
        public uint Height;

        [MarshalAs(UnmanagedType.I1)]
        public bool IsDense;

        public IntPtr Points;
    }

    internal static class Truelight3DNativeMethods
    {
        private const string DllName = "AMSDK.dll";
        private static readonly object ZMotionResolveLock = new();
        private static IntPtr _dllHandle = IntPtr.Zero;
        private static MoveZDelegate? _moveZDelegate;
        private static MoveZDistanceDelegate? _moveZRelativeDelegate;
        private static MoveZDistanceDelegate? _moveZAbsoluteDelegate;
        private static IsZMotorSupportedDelegate? _isZMotorSupportedDelegate;

        [DllImport(DllName, EntryPoint = "?instance@AMSDK@AM@@SAPEAV12@XZ", ExactSpelling = true)]
        internal static extern IntPtr Instance();

        [DllImport(DllName, EntryPoint = "?initialize@AMSDK@AM@@SA?AW4Status@2@XZ", ExactSpelling = true)]
        internal static extern Truelight3DStatus Initialize();

        [DllImport(DllName, EntryPoint = "?destory@AMSDK@AM@@SAXXZ", ExactSpelling = true)]
        internal static extern void Destroy();

        [DllImport(DllName, EntryPoint = "?connect@AMSDK@AM@@QEAA?AW4Status@2@XZ", ExactSpelling = true)]
        internal static extern Truelight3DStatus Connect(IntPtr sdk);

        [DllImport(DllName, EntryPoint = "?disconnect@AMSDK@AM@@QEAA?AW4Status@2@XZ", ExactSpelling = true)]
        internal static extern Truelight3DStatus Disconnect(IntPtr sdk);

        [DllImport(DllName, EntryPoint = "?isConnected@AMSDK@AM@@QEAA_NXZ", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool IsConnected(IntPtr sdk);

        [DllImport(DllName, EntryPoint = "?startScan@AMSDK@AM@@QEAA?AW4Status@2@XZ", ExactSpelling = true)]
        internal static extern Truelight3DStatus StartScan(IntPtr sdk);

        [DllImport(DllName, EntryPoint = "?stopScan@AMSDK@AM@@QEAA?AW4Status@2@XZ", ExactSpelling = true)]
        internal static extern Truelight3DStatus StopScan(IntPtr sdk);

        [DllImport(DllName, EntryPoint = "?setScanType@AMSDK@AM@@QEAA?AW4Status@2@W4ScanType@2@@Z", ExactSpelling = true)]
        internal static extern Truelight3DStatus SetScanType(IntPtr sdk, Truelight3DScanType type);

        [DllImport(DllName, EntryPoint = "?setExposureTime@AMSDK@AM@@QEAA?AW4Status@2@I@Z", ExactSpelling = true)]
        internal static extern Truelight3DStatus SetExposureTime(IntPtr sdk, uint exposureTimeUs);

        [DllImport(DllName, EntryPoint = "?setScanStep@AMSDK@AM@@QEAA?AW4Status@2@M@Z", ExactSpelling = true)]
        internal static extern Truelight3DStatus SetScanStep(IntPtr sdk, float stepMm);

        [DllImport(DllName, EntryPoint = "?setScanPosition@AMSDK@AM@@QEAA?AW4Status@2@MM@Z", ExactSpelling = true)]
        internal static extern Truelight3DStatus SetScanPosition(IntPtr sdk, float startPositionMm, float endPositionMm);

        [DllImport(DllName, EntryPoint = "?setScanRange@AMSDK@AM@@QEAA?AW4Status@2@M@Z", ExactSpelling = true)]
        internal static extern Truelight3DStatus SetScanRange(IntPtr sdk, float scanRangeMm);

        [DllImport(DllName, EntryPoint = "?setLightRGB@AMSDK@AM@@QEAA?AW4Status@2@EEE@Z", ExactSpelling = true)]
        internal static extern Truelight3DStatus SetLightRgb(IntPtr sdk, byte red, byte green, byte blue);

        [DllImport(DllName, EntryPoint = "?isCircleLightSupported@AMSDK@AM@@QEAA_NXZ", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool IsCircleLightSupported(IntPtr sdk);

        [DllImport(DllName, EntryPoint = "?setCircleLight@AMSDK@AM@@QEAA?AW4Status@2@I@Z", ExactSpelling = true)]
        internal static extern Truelight3DStatus SetCircleLight(IntPtr sdk, uint value);

        [DllImport(DllName, EntryPoint = "?setObjectiveLensMagnification@AMSDK@AM@@QEAA?AW4Status@2@W4ObjectiveMagnification@2@@Z", ExactSpelling = true)]
        internal static extern Truelight3DStatus SetObjectiveLensMagnification(IntPtr sdk, Truelight3DObjectiveMagnification magnification);

        [DllImport(DllName, EntryPoint = "?readImage@AMSDK@AM@@QEAA?AW4Status@2@PEAUImage@2@I@Z", ExactSpelling = true)]
        internal static extern Truelight3DStatus ReadImage(IntPtr sdk, ref Truelight3DNativeImage image, uint timeoutMs);

        [DllImport(DllName, EntryPoint = "?isZMotorSupported@AMSDK@AM@@QEBA_NXZ", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool IsZMotorSupportedPrimary(IntPtr sdk);

        internal static bool IsZMotorSupported(IntPtr sdk)
        {
            try
            {
                return IsZMotorSupportedPrimary(sdk);
            }
            catch (EntryPointNotFoundException)
            {
                return ResolveIsZMotorSupportedDelegate()(sdk);
            }
        }

        [DllImport(DllName, EntryPoint = "?moveZRelative@AMSDK@AM@@QEAA?AW4Status@2@M@Z", ExactSpelling = true)]
        private static extern Truelight3DStatus MoveZRelativePrimary(IntPtr sdk, float positionMm);

        [DllImport(DllName, EntryPoint = "?moveZAbsolute@AMSDK@AM@@QEAA?AW4Status@2@M@Z", ExactSpelling = true)]
        private static extern Truelight3DStatus MoveZAbsolutePrimary(IntPtr sdk, float positionMm);

        [DllImport(DllName, EntryPoint = "?moveZ@AMSDK@AM@@QEAA?AW4Status@2@W4MotionDirection@2@@Z", ExactSpelling = true)]
        private static extern Truelight3DStatus MoveZPrimary(IntPtr sdk, Truelight3DMotionDirection direction);

        internal static Truelight3DStatus MoveZRelative(IntPtr sdk, float positionMm)
        {
            try
            {
                return MoveZRelativePrimary(sdk, positionMm);
            }
            catch (EntryPointNotFoundException)
            {
                return ResolveMoveZRelativeDelegate()(sdk, positionMm);
            }
        }

        internal static Truelight3DStatus MoveZAbsolute(IntPtr sdk, float positionMm)
        {
            try
            {
                return MoveZAbsolutePrimary(sdk, positionMm);
            }
            catch (EntryPointNotFoundException)
            {
                return ResolveMoveZAbsoluteDelegate()(sdk, positionMm);
            }
        }

        internal static Truelight3DStatus MoveZ(IntPtr sdk, Truelight3DMotionDirection direction)
        {
            try
            {
                return MoveZPrimary(sdk, direction);
            }
            catch (EntryPointNotFoundException)
            {
                return ResolveMoveZDelegate()(sdk, direction);
            }
        }

        [DllImport(DllName, EntryPoint = "?moveZHome@AMSDK@AM@@QEAA?AW4Status@2@_N@Z", ExactSpelling = true)]
        internal static extern Truelight3DStatus MoveZHome(IntPtr sdk, [MarshalAs(UnmanagedType.I1)] bool isWaiting);

        [DllImport(DllName, EntryPoint = "?stopZ@AMSDK@AM@@QEAA?AW4Status@2@XZ", ExactSpelling = true)]
        internal static extern Truelight3DStatus StopZ(IntPtr sdk);

        [DllImport(DllName, EntryPoint = "?getZPosition@AMSDK@AM@@QEBA?AW4Status@2@AEAM@Z", ExactSpelling = true)]
        internal static extern Truelight3DStatus GetZPosition(IntPtr sdk, out float positionMm);

        [DllImport(DllName, EntryPoint = "?setZSpeed@AMSDK@AM@@QEAA?AW4Status@2@M@Z", ExactSpelling = true)]
        internal static extern Truelight3DStatus SetZSpeed(IntPtr sdk, float speedMmPerSec);

        [DllImport(DllName, EntryPoint = "?getZSpeed@AMSDK@AM@@QEAA?AW4Status@2@AEAM@Z", ExactSpelling = true)]
        internal static extern Truelight3DStatus GetZSpeed(IntPtr sdk, out float speedMmPerSec);

        [DllImport(DllName, EntryPoint = "?getPointCloud@AMSDK@AM@@QEAA?AW4Status@2@PEAUPointCloudXYZ@2@@Z", ExactSpelling = true)]
        internal static extern Truelight3DStatus GetPointCloud(IntPtr sdk, ref Truelight3DNativePointCloudXYZ cloud);

        [DllImport(DllName, EntryPoint = "?getTexture@AMSDK@AM@@QEAA?AW4Status@2@PEAUImage@2@@Z", ExactSpelling = true)]
        internal static extern Truelight3DStatus GetTexture(IntPtr sdk, ref Truelight3DNativeImage image);

        [DllImport(DllName, EntryPoint = "?getDepthMap@AMSDK@AM@@QEAA?AW4Status@2@PEAUDepthMap@2@@Z", ExactSpelling = true)]
        internal static extern Truelight3DStatus GetDepthMap(IntPtr sdk, ref Truelight3DNativeDepthMap depthMap);

        [DllImport(DllName, EntryPoint = "?setAcquisitionParameter@AMSDK@AM@@QEAA?AW4Status@2@IM@Z", ExactSpelling = true)]
        internal static extern Truelight3DStatus SetAcquisitionParameter(IntPtr sdk, uint windowSize, float zFilter);

        [DllImport(DllName, EntryPoint = "?getWidth@AMSDK@AM@@QEBA?AW4Status@2@AEAI@Z", ExactSpelling = true)]
        internal static extern Truelight3DStatus GetWidth(IntPtr sdk, out uint width);

        [DllImport(DllName, EntryPoint = "?getHeight@AMSDK@AM@@QEBA?AW4Status@2@AEAI@Z", ExactSpelling = true)]
        internal static extern Truelight3DStatus GetHeight(IntPtr sdk, out uint height);

        [DllImport(DllName, EntryPoint = "?getLayerMaxNumber@AMSDK@AM@@QEBA?AW4Status@2@AEAH@Z", ExactSpelling = true)]
        internal static extern Truelight3DStatus GetLayerMaxNumber(IntPtr sdk, out int layerMaxNumber);

        [DllImport(DllName, EntryPoint = "?getScanRangeMin@AMSDK@AM@@QEAA?AW4Status@2@AEAM@Z", ExactSpelling = true)]
        internal static extern Truelight3DStatus GetScanRangeMin(IntPtr sdk, out float minRange);

        [DllImport(DllName, EntryPoint = "?getScanRangeMax@AMSDK@AM@@QEAA?AW4Status@2@AEAM@Z", ExactSpelling = true)]
        internal static extern Truelight3DStatus GetScanRangeMax(IntPtr sdk, out float maxRange);

        [DllImport(DllName, EntryPoint = "?isConfocalSupported@AMSDK@AM@@QEAA_NXZ", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool IsConfocalSupported(IntPtr sdk);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate Truelight3DStatus MoveZDelegate(IntPtr sdk, Truelight3DMotionDirection direction);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate Truelight3DStatus MoveZDistanceDelegate(IntPtr sdk, float positionMm);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.I1)]
        private delegate bool IsZMotorSupportedDelegate(IntPtr sdk);

        private static MoveZDelegate ResolveMoveZDelegate()
        {
            lock (ZMotionResolveLock)
            {
                _moveZDelegate ??= ResolveExportDelegate<MoveZDelegate>(
                    MoveZExportCandidates(),
                    "moveZ");
                return _moveZDelegate;
            }
        }

        private static MoveZDistanceDelegate ResolveMoveZRelativeDelegate()
        {
            lock (ZMotionResolveLock)
            {
                _moveZRelativeDelegate ??= ResolveExportDelegate<MoveZDistanceDelegate>(
                    MoveZRelativeExportCandidates(),
                    "moveZRelative");
                return _moveZRelativeDelegate;
            }
        }

        private static MoveZDistanceDelegate ResolveMoveZAbsoluteDelegate()
        {
            lock (ZMotionResolveLock)
            {
                _moveZAbsoluteDelegate ??= ResolveExportDelegate<MoveZDistanceDelegate>(
                    MoveZAbsoluteExportCandidates(),
                    "moveZAbsolute");
                return _moveZAbsoluteDelegate;
            }
        }

        private static IsZMotorSupportedDelegate ResolveIsZMotorSupportedDelegate()
        {
            lock (ZMotionResolveLock)
            {
                _isZMotorSupportedDelegate ??= ResolveExportDelegate<IsZMotorSupportedDelegate>(
                    IsZMotorSupportedExportCandidates(),
                    "isZMotorSupported");
                return _isZMotorSupportedDelegate;
            }
        }

        private static T ResolveExportDelegate<T>(IEnumerable<string> candidates, string symbolName)
            where T : Delegate
        {
            EnsureLibraryLoaded();
            foreach (string candidate in candidates)
            {
                if (NativeLibrary.TryGetExport(_dllHandle, candidate, out IntPtr proc) && proc != IntPtr.Zero)
                {
                    return Marshal.GetDelegateForFunctionPointer<T>(proc);
                }
            }

            throw new EntryPointNotFoundException($"{symbolName} symbol not found in {DllName}.");
        }

        private static void EnsureLibraryLoaded()
        {
            if (_dllHandle != IntPtr.Zero)
            {
                return;
            }

            if (!NativeLibrary.TryLoad(DllName, out _dllHandle) || _dllHandle == IntPtr.Zero)
            {
                throw new DllNotFoundException($"{DllName} load failed.");
            }
        }

        private static IEnumerable<string> MoveZExportCandidates()
        {
            yield return "?moveZ@AMSDK@AM@@QEAA?AW4Status@2@W4MotionDirection@2@@Z";
            yield return "?moveZ@AMSDK@AM@@QEBA?AW4Status@2@W4MotionDirection@2@@Z";
            yield return "?moveZ@AMSDK@AM@@QEAA?AW4Status@2@W4MotionDirection@AM@@@Z";
            yield return "?moveZ@AMSDK@AM@@QEBA?AW4Status@2@W4MotionDirection@AM@@@Z";
            yield return "moveZ";
        }

        private static IEnumerable<string> MoveZRelativeExportCandidates()
        {
            yield return "?moveZRelative@AMSDK@AM@@QEAA?AW4Status@2@M@Z";
            yield return "?moveZRelative@AMSDK@AM@@QEBA?AW4Status@2@M@Z";
            yield return "moveZRelative";
        }

        private static IEnumerable<string> MoveZAbsoluteExportCandidates()
        {
            yield return "?moveZAbsolute@AMSDK@AM@@QEAA?AW4Status@2@M@Z";
            yield return "?moveZAbsolute@AMSDK@AM@@QEBA?AW4Status@2@M@Z";
            yield return "moveZAbsolute";
        }

        private static IEnumerable<string> IsZMotorSupportedExportCandidates()
        {
            yield return "?isZMotorSupported@AMSDK@AM@@QEBA_NXZ";
            yield return "?isZMotorSupported@AMSDK@AM@@QEAA_NXZ";
            yield return "isZMotorSupported";
        }
    }
}
