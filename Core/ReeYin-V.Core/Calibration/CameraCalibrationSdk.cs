using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Calibration
{
    using static OpenCvSharp.LineIterator;
    // 定义CameraCalibrationFrameworkHandle类型
    using CameraCalibHandle = System.IntPtr;

    public class CameraCalibrationSdk : IDisposable
    {
        // 标定板类型
        public enum CalibrationBoardType
        {
            BOARD_UNKNOWN = -1,
            PIXEL_RATIO = 0,
            CHESSBOARD = 1,           // OpenCV棋盘格
            CHARUCO,                  // Charuco标定板
            CIRCLES_GRID,             // 圆点标定板
            ASYMMETRIC_CIRCLES_GRID   // 非对称圆点标定板
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CalibrationBoardParams
        {
            public CalibrationBoardType type;  // 标定板类型
            public int PatternCols;            // 棋盘格列数
            public int PatternRows;            // 棋盘格行数
            public double squareSize;          // 棋盘格方块尺寸(单位m)
            public int dictionaryId;           // ArUco码字典ID
            public double markerSize;          // ArUco码尺寸(单位m)
            public double squareSizePixel;     // Charuco square side length in pixels
            public double markerSizePixel;     // Charuco marker side length in pixels
            public double distanceReal;        // 距离(单位m)
            public double distancePixel;       // 像素距离
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MeasurementPlaneParams
        {
            public double heightCompensation;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CameraParams
        {
            // 标定误差
            public double error;

            // X方向像素当量
            public double intervalX;

            // Y方向像素当量
            public double intervalY;

            // 相机ID
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string cameraId;

            // 内参矩阵 (3x3)
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
            public double[] intrinsic;

            // 畸变参数 (最多8个)
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public double[] distortion;

            // 旋转向量 (3x1)
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public double[] rvec;

            // 平移向量 (3x1)
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public double[] tvec;

            // 外参矩阵 (4x4)
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public double[] extrinsic;

            // 单应性矩阵 (3x3)
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
            public double[] homographyMatrix;

            // 矩阵的有效标识
            public int hasError;
            public int hasIntervalX;
            public int hasIntervalY;
            public int hasIntrinsic;
            public int hasDistortion;
            public int hasRvec;
            public int hasTvec;
            public int hasExtrinsic;
            public int hasHomographyMatrix;
        }



        internal class CalibrationNative
        {
            [DllImport("ALGO.CalibrationNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr getLastError(); // returns const char*

            [DllImport("ALGO.CalibrationNative.dll")]
            public static extern CameraCalibHandle createCalibrationFramework();

            [DllImport("ALGO.CalibrationNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void destroyCalibrationFramework(CameraCalibHandle handle);

            [DllImport("ALGO.CalibrationNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int setCalibrationBoardParams(IntPtr handle, ref CalibrationBoardParams param);

            [DllImport("ALGO.CalibrationNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int getCalibrationBoardParams(IntPtr handle, ref CalibrationBoardParams param);

            [DllImport("ALGO.CalibrationNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int setMeasurementPlaneParams(IntPtr handle, ref MeasurementPlaneParams param);

            [DllImport("ALGO.CalibrationNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int getMeasurementPlaneParams(IntPtr handle, ref MeasurementPlaneParams param);

            [DllImport("ALGO.CalibrationNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int getCameraParams(IntPtr handle, ref CameraParams param);

            [DllImport("ALGO.CalibrationNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int addCamera(CameraCalibHandle handle, [MarshalAs(UnmanagedType.LPStr)] string cameraId);

            [DllImport("ALGO.CalibrationNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int addCalibrationImagePath(CameraCalibHandle handle,
                                                             [MarshalAs(UnmanagedType.LPStr)] string cameraId,
                                                             [MarshalAs(UnmanagedType.LPStr)] string imagePath);

            [DllImport("ALGO.CalibrationNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int calibrate(CameraCalibHandle handle);

            [DllImport("ALGO.CalibrationNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int saveCalibrationResults(CameraCalibHandle handle, [MarshalAs(UnmanagedType.LPStr)] string outputPath);

            [DllImport("ALGO.CalibrationNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int loadCalibrationFile(CameraCalibHandle handle,
                                                         [MarshalAs(UnmanagedType.LPStr)] string filePath);

            [DllImport("ALGO.CalibrationNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int pixelToWorld(CameraCalibHandle handle, [MarshalAs(UnmanagedType.LPStr)] string cameraId,
                                                  double pixelX, double pixelY, out double worldX, out double worldY, out double worldZ);

            [DllImport("ALGO.CalibrationNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int worldToPixel(CameraCalibHandle handle, [MarshalAs(UnmanagedType.LPStr)] string cameraId,
                                                  double worldX, double worldY, double worldZ, out double pixelX, out double pixelY);

            [DllImport("ALGO.CalibrationNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int imageCorrection(CameraCalibHandle handle, [MarshalAs(UnmanagedType.LPStr)] string cameraId,
                                                     IntPtr inImageData, int inW, int inH, int inC, int inType,
                                                     out IntPtr outImageData, out int outW, out int outH, out int outC, out int outType);

            [DllImport("ALGO.CalibrationNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int freePtr(IntPtr ptr);
        }

        private CameraCalibHandle _cameraCalibHandle;
        private bool _disposed;

        public CameraCalibrationSdk()
        {
            _disposed = false;
            _cameraCalibHandle = CalibrationNative.createCalibrationFramework();
        }

        // 实现 IDisposable
        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (_cameraCalibHandle != IntPtr.Zero)
                {
                    CalibrationNative.destroyCalibrationFramework(_cameraCalibHandle);
                    _cameraCalibHandle = IntPtr.Zero;
                }

                if (disposing)
                {

                }

                _disposed = true;
            }
        }


        ~CameraCalibrationSdk()
        {
            Dispose(false);
        }

        /// <summary>
        /// 设置标定板参数
        /// </summary>
        /// <param name="param"></param>
        /// <exception cref="ArgumentException"></exception>
        public void setCalibrationBoardParams(CalibrationBoardParams param)
        {
            int rc = CalibrationNative.setCalibrationBoardParams(_cameraCalibHandle, ref param);

            if (rc != 0)
            {
                IntPtr p = CalibrationNative.getLastError();
                string msg = p == IntPtr.Zero
                    ? "Unknown native error"
                    : (Marshal.PtrToStringAnsi(p) ?? "Unknown native error");
                throw new ArgumentException($"error {rc}: {msg}");
            }
        }


        /// <summary>
        /// 获取标定板参数
        /// </summary>
        /// <param name="param"></param>
        /// <exception cref="ArgumentException"></exception>
        public void getCalibrationBoardParams(ref CalibrationBoardParams param)
        {
            int rc = CalibrationNative.getCalibrationBoardParams(_cameraCalibHandle, ref param);

            if (rc != 0)
            {
                IntPtr p = CalibrationNative.getLastError();
                string msg = p == IntPtr.Zero
                    ? "Unknown native error"
                    : (Marshal.PtrToStringAnsi(p) ?? "Unknown native error");
                throw new ArgumentException($"error {rc}: {msg}");
            }
        }

        public void setMeasurementPlaneParams(MeasurementPlaneParams param)
        {
            int rc = CalibrationNative.setMeasurementPlaneParams(_cameraCalibHandle, ref param);

            if (rc != 0)
            {
                IntPtr p = CalibrationNative.getLastError();
                string msg = p == IntPtr.Zero
                    ? "Unknown native error"
                    : (Marshal.PtrToStringAnsi(p) ?? "Unknown native error");
                throw new ArgumentException($"error {rc}: {msg}");
            }
        }

        public void getMeasurementPlaneParams(ref MeasurementPlaneParams param)
        {
            int rc = CalibrationNative.getMeasurementPlaneParams(_cameraCalibHandle, ref param);

            if (rc != 0)
            {
                IntPtr p = CalibrationNative.getLastError();
                string msg = p == IntPtr.Zero
                    ? "Unknown native error"
                    : (Marshal.PtrToStringAnsi(p) ?? "Unknown native error");
                throw new ArgumentException($"error {rc}: {msg}");
            }
        }

        /// <summary>
        /// 获取相机参数
        /// </summary>
        /// <param name="param"></param>
        /// <exception cref="ArgumentException"></exception>
        public void getCameraParams(ref CameraParams param)
        {
            int rc = CalibrationNative.getCameraParams(_cameraCalibHandle, ref param);

            if (rc != 0)
            {
                IntPtr p = CalibrationNative.getLastError();
                string msg = p == IntPtr.Zero
                    ? "Unknown native error"
                    : (Marshal.PtrToStringAnsi(p) ?? "Unknown native error");
                throw new ArgumentException($"error {rc}: {msg}");
            }
        }


        /// <summary>
        /// 添加相机名称
        /// </summary>
        /// <param name="cameraId"></param>
        /// <exception cref="ArgumentException"></exception>
        public void addCamera(string cameraId)
        {
            int rc = CalibrationNative.addCamera(_cameraCalibHandle, cameraId);

            if (rc != 0)
            {
                IntPtr p = CalibrationNative.getLastError();
                string msg = p == IntPtr.Zero
                    ? "Unknown native error"
                    : (Marshal.PtrToStringAnsi(p) ?? "Unknown native error");
                throw new ArgumentException($"error {rc}: {msg}");
            }
        }

        /// <summary>
        /// 添加指定相机对应的标定板图像
        /// </summary>
        /// <param name="cameraId"></param>
        /// <param name="imagePath"></param>
        /// <exception cref="ArgumentException"></exception>
        public void addCalibrationImagePath(string cameraId, string imagePath)
        {
            int rc = CalibrationNative.addCalibrationImagePath(_cameraCalibHandle, cameraId, imagePath);

            if (rc != 0)
            {
                IntPtr p = CalibrationNative.getLastError();
                string msg = p == IntPtr.Zero
                    ? "Unknown native error"
                    : (Marshal.PtrToStringAnsi(p) ?? "Unknown native error");
                throw new ArgumentException($"error {rc}: {msg}");
            }
        }

        /// <summary>
        /// 执行标定
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public void calibrate()
        {
            int rc = CalibrationNative.calibrate(_cameraCalibHandle);

            if (rc != 0)
            {
                IntPtr p = CalibrationNative.getLastError();
                string msg = p == IntPtr.Zero
                    ? "Unknown native error"
                    : (Marshal.PtrToStringAnsi(p) ?? "Unknown native error");
                throw new ArgumentException($"error {rc}: {msg}");
            }
        }

        /// <summary>
        /// 保存标定结果
        /// </summary>
        /// <param name="outputPath"></param>
        /// <exception cref="ArgumentException"></exception>
        public void saveCalibrationResults(string outputPath)
        {
            int rc = CalibrationNative.saveCalibrationResults(_cameraCalibHandle, outputPath);

            if (rc != 0)
            {
                IntPtr p = CalibrationNative.getLastError();
                string msg = p == IntPtr.Zero
                    ? "Unknown native error"
                    : (Marshal.PtrToStringAnsi(p) ?? "Unknown native error");
                throw new ArgumentException($"error {rc}: {msg}");
            }
        }

        /// <summary>
        /// 加载对应相机的校准结果
        /// </summary>
        /// <param name="filePath">标定文件路径</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public void loadCalibrationFile(string filePath)
        {
            int rc = CalibrationNative.loadCalibrationFile(_cameraCalibHandle, filePath);

            if (rc != 0)
            {
                IntPtr p = CalibrationNative.getLastError();
                string msg = p == IntPtr.Zero
                    ? "Unknown native error"
                    : (Marshal.PtrToStringAnsi(p) ?? "Unknown native error");
                throw new ArgumentException($"error {rc}: {msg}");
            }
        }

        /// <summary>
        /// 像素坐标转世界坐标
        /// </summary>
        /// <param name="cameraId"></param>
        /// <param name="pixelX"></param>
        /// <param name="pixelY"></param>
        /// <param name="worldX"></param>
        /// <param name="worldY"></param>
        /// <param name="worldZ"></param>
        /// <exception cref="ArgumentException"></exception>
        public void pixelToWorld(string cameraId, double pixelX, double pixelY, out double worldX, out double worldY, out double worldZ)
        {
            worldX = double.NegativeInfinity;
            worldY = double.NegativeInfinity;
            worldZ = double.NegativeInfinity;

            int rc = CalibrationNative.pixelToWorld(_cameraCalibHandle, cameraId, pixelX, pixelY, out worldX, out worldY, out worldZ);

            if (rc != 0)
            {
                IntPtr p = CalibrationNative.getLastError();
                string msg = p == IntPtr.Zero
                    ? "Unknown native error"
                    : (Marshal.PtrToStringAnsi(p) ?? "Unknown native error");
                throw new ArgumentException($"error {rc}: {msg}");
            }
        }


        /// <summary>
        /// 世界坐标转像素坐标
        /// </summary>
        /// <param name="cameraId"></param>
        /// <param name="worldX"></param>
        /// <param name="worldY"></param>
        /// <param name="worldZ"></param>
        /// <param name="pixelX"></param>
        /// <param name="pixelY"></param>
        /// <exception cref="ArgumentException"></exception>
        public void worldToPixel(string cameraId, double worldX, double worldY, double worldZ, out double pixelX, out double pixelY)
        {
            pixelX = double.NegativeInfinity;
            pixelY = double.NegativeInfinity;

            int rc = CalibrationNative.worldToPixel(_cameraCalibHandle, cameraId, worldX, worldY, worldZ, out pixelX, out pixelY);

            if (rc != 0)
            {
                IntPtr p = CalibrationNative.getLastError();
                string msg = p == IntPtr.Zero
                    ? "Unknown native error"
                    : (Marshal.PtrToStringAnsi(p) ?? "Unknown native error");
                throw new ArgumentException($"error {rc}: {msg}");
            }
        }

        /// <summary>
        /// 图片校正
        /// </summary>
        public void imageCorrection(string cameraId, IntPtr inImageData, int inW, int inH, int inC, int inType,
                                    out IntPtr outImageData, out int outW, out int outH, out int outC, out int outType)
        {
            int rc = CalibrationNative.imageCorrection(_cameraCalibHandle, cameraId, inImageData, inW, inH, inC, inType,
                                                       out outImageData, out outW, out outH, out outC, out outType);

            if (rc != 0)
            {
                IntPtr p = CalibrationNative.getLastError();
                string msg = p == IntPtr.Zero
                    ? "Unknown native error"
                    : (Marshal.PtrToStringAnsi(p) ?? "Unknown native error");
                throw new ArgumentException($"error {rc}: {msg}");
            }
        }

        /// <summary>
        /// 释放指定指针
        /// </summary>
        public void freePtr(IntPtr ptr)
        {
            int rc = CalibrationNative.freePtr(ptr);

            if (rc != 0)
            {
                IntPtr p = CalibrationNative.getLastError();
                string msg = p == IntPtr.Zero
                    ? "Unknown native error"
                    : (Marshal.PtrToStringAnsi(p) ?? "Unknown native error");
                throw new ArgumentException($"error {rc}: {msg}");
            }
        }


        /// <summary>
        /// 标定接口指针释放
        /// </summary>
        public void destroyCameraCalibration()
        {
            CalibrationNative.destroyCalibrationFramework(_cameraCalibHandle);
        }


    }
}
