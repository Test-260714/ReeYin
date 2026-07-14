using HalconDotNet;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Helper.ImageOP
{
    public static class ImageHelper
    {

        /// <summary>
        /// OpenCv转为Halcon
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        public static HObject ConvertMatToHObject(Mat img)
        {
            HObject halconImg = null;

            if (img == null || img.Empty())
            {
                Console.WriteLine("Input Mat is null or empty.");
                return null;
            }

            try
            {
                int width = img.Width;
                int height = img.Height;
                string imageType = "";

                // Halcon GenImage1 接受的图像类型字符串
                // 'byte' 对应 8-bit, 'uint2' 对应 16-bit, 'real' 对应 float/double

                // 确保 Mat 是单通道 8-bit 灰度图 (CV_8UC1)
                if (img.Channels() == 1 && img.Type() == MatType.CV_8UC1)
                {
                    imageType = "byte";
                    IntPtr dataPtr = img.Data;

                    // GenImage1：从内存指针创建单通道 Halcon 图像
                    HOperatorSet.GenImage1(out halconImg, imageType, width, height, dataPtr);
                }
                // 如果是三通道彩色图像 (CV_8UC3)
                else if (img.Channels() == 3 && img.Type() == MatType.CV_8UC3)
                {
                    // OpenCV 默认是 BGR 顺序。Halcon 需要 R, G, B 三个单独的通道
                    // 1. 分割 Mat 到三个通道
                    Mat[] channels = img.Split();

                    // 2. Halcon 需要 R, G, B 顺序。Halcon 的 GenImage3 默认期望 R, G, B
                    // 注意：OpenCV 的 Split() 返回的顺序是 B, G, R
                    IntPtr bPtr = channels[0].Data;
                    IntPtr gPtr = channels[1].Data;
                    IntPtr rPtr = channels[2].Data;

                    // GenImage3：从三个内存指针创建彩色 Halcon 图像
                    HOperatorSet.GenImage3(out halconImg, "byte", width, height, rPtr, gPtr, bPtr);

                    // 记得释放分割出的 Mat
                    foreach (var ch in channels) ch.Dispose();
                }
                else
                {
                    Console.WriteLine($"Unsupported Mat type or channel count: {img.Type()}, Channels: {img.Channels()}");
                }
            }
            catch (HOperatorException ex)
            {
                Console.WriteLine($"Halcon conversion error: {ex.Message}");
            }

            return halconImg;
        }
    }

    public class DepthMapToCloudUm
    {
        private double _DxUm = 2.9;    // X 像素当量：2.9 µm / pixel
        private double _DyUm = 5.0;    // Y 像素当量：5.0 µm / pixel
        private double _DzUm = 0.1;   // Z 原始值单位：0.1 µm / count

        public DepthMapToCloudUm(double IntervalX, double IntervalY, double IntervalZ)
        {
            _DxUm = IntervalX;
            _DyUm = IntervalY;
            _DzUm = IntervalZ * 0.1;
        }


        // 取原始深度值（count），并判定是否有效
        private bool TryGetRawDepth(Mat depth, int v, int u, out double raw)
        {
            raw = 0;

            var t = depth.Type();
            if (t == MatType.CV_16UC1)
            {
                ushort d = depth.Get<ushort>(v, u);
                if (d == 0)
                    return false;        // 常见：0 表示无效（如不是这样请改这里）
                raw = d;
                return true;
            }
            if (t == MatType.CV_16SC1)
            {
                short d = depth.Get<short>(v, u);
                if (d == short.MinValue)
                    return false; // 常见无效哨兵（按你的数据改）
                raw = d;
                return true; // 允许负值（很多高度图会有负值）
            }
            if (t == MatType.CV_32FC1)
            {
                float d = depth.Get<float>(v, u);
                if (!float.IsFinite(d) || d == 8888880)
                    return false;
                raw = d;
                return true;
            }

            throw new NotSupportedException($"Unsupported MatType: {t}");
        }

        public List<double[]> ToPointListUm(Mat depth, bool centerOrigin = false, int stride = 1)
        {
            if (depth == null) throw new ArgumentNullException(nameof(depth));
            if (depth.Empty()) return new List<double[]>(0);
            if (stride < 1) stride = 1;

            int w = depth.Cols, h = depth.Rows;
            double cx = (w - 1) * 0.5;
            double cy = (h - 1) * 0.5;

            int count = 0;
            for (int v = 0; v < h; v += stride)
                for (int u = 0; u < w; u += stride)
                    if (TryGetRawDepth(depth, v, u, out _))
                        count++;

            var points = new List<double[]>(count);

            for (int v = 0; v < h; v += stride)
                for (int u = 0; u < w; u += stride)
                {
                    if (!TryGetRawDepth(depth, v, u, out double raw))
                        continue;

                    double uu = centerOrigin ? (u - cx) : u;
                    double vv = centerOrigin ? (v - cy) : v;

                    double x = uu * _DxUm;
                    double y = -vv * _DyUm;
                    double z = raw * _DzUm;

                    points.Add(new[] { x, y, z });
                }

            return points;
        }

        // PLY（不写无效点：文件更小）
        public void SavePlyAsciiUm(string plyPath, Mat depth, bool centerOrigin = false, int stride = 1)
        {
            int w = depth.Cols, h = depth.Rows;
            double cx = (w - 1) * 0.5;
            double cy = (h - 1) * 0.5;

            int count = 0;
            for (int v = 0; v < h; v += stride)
                for (int u = 0; u < w; u += stride)
                    if (TryGetRawDepth(depth, v, u, out _))
                        count++;

            using var sw = new StreamWriter(plyPath);
            sw.WriteLine("ply");
            sw.WriteLine("format ascii 1.0");
            sw.WriteLine($"element vertex {count}");
            sw.WriteLine("property float x");
            sw.WriteLine("property float y");
            sw.WriteLine("property float z");
            sw.WriteLine("end_header");

            var ci = CultureInfo.InvariantCulture;

            for (int v = 0; v < h; v += stride)
                for (int u = 0; u < w; u += stride)
                {
                    if (!TryGetRawDepth(depth, v, u, out double raw))
                        continue;

                    double uu = centerOrigin ? (u - cx) : u;
                    double vv = centerOrigin ? (v - cy) : v;

                    double x = uu * _DxUm;
                    double y = -vv * _DyUm;
                    double z = raw * _DzUm; // => µm

                    sw.WriteLine($"{x.ToString(ci)} {y.ToString(ci)} {z.ToString(ci)}");
                }
        }

        // PLY（从HObject，不写无效点）
        public void SavePlyAsciiUm(string plyPath, HObject hoDepth, bool centerOrigin = false, int stride = 1)
        {
            HOperatorSet.GetImageSize(hoDepth, out HTuple hvWidth, out HTuple hvHeight);
            int w = hvWidth.I;
            int h = hvHeight.I;
            double cx = (w - 1) * 0.5;
            double cy = (h - 1) * 0.5;

            HOperatorSet.GetImagePointer1(hoDepth, out HTuple hvPointer, out HTuple hvType, out HTuple _, out HTuple _);
            float[] depthData = new float[h * w];
            Marshal.Copy(hvPointer.IP, depthData, 0, h * w);

            int count = 0;
            for (int v = 0; v < h; v += stride)
                for (int u = 0; u < w; u += stride)
                    if (TryGetRawDepthFromHObject(depthData, w, v, u, out _))
                        count++;

            using var sw = new StreamWriter(plyPath);
            sw.WriteLine("ply");
            sw.WriteLine("format ascii 1.0");
            sw.WriteLine($"element vertex {count}");
            sw.WriteLine("property float x");
            sw.WriteLine("property float y");
            sw.WriteLine("property float z");
            sw.WriteLine("end_header");

            var ci = CultureInfo.InvariantCulture;

            for (int v = 0; v < h; v += stride)
            {
                for (int u = 0; u < w; u += stride)
                {
                    if (!TryGetRawDepthFromHObject(depthData, w, v, u, out double raw))
                        continue;

                    double uu = centerOrigin ? (u - cx) : u;
                    double vv = centerOrigin ? (v - cy) : v;

                    double x = uu * _DxUm;
                    double y = -vv * _DyUm;
                    double z = raw * _DzUm;

                    sw.WriteLine($"{x.ToString(ci)} {y.ToString(ci)} {z.ToString(ci)}");
                }
            }
        }



        // PCD（可选 organized：保留行列结构；无效点写 nan）
        public void SavePcdAsciiUm(string pcdPath, Mat depth, bool centerOrigin = false, int stride = 1, bool organized = false)
        {
            int w = depth.Cols, h = depth.Rows;
            double cx = (w - 1) * 0.5;
            double cy = (h - 1) * 0.5;

            int outW = (w + stride - 1) / stride;
            int outH = (h + stride - 1) / stride;

            int points;
            if (organized)
            {
                points = outW * outH;
            }
            else
            {
                points = 0;
                for (int v = 0; v < h; v += stride)
                    for (int u = 0; u < w; u += stride)
                        if (TryGetRawDepth(depth, v, u, out _))
                            points++;
                outW = points;
                outH = 1;
            }

            using var sw = new StreamWriter(pcdPath);
            sw.WriteLine("# .PCD v0.7 - Point Cloud Data file format");
            sw.WriteLine("VERSION .7");
            sw.WriteLine("FIELDS x y z");
            sw.WriteLine("SIZE 4 4 4");
            sw.WriteLine("TYPE F F F");
            sw.WriteLine("COUNT 1 1 1");
            sw.WriteLine($"WIDTH {outW}");
            sw.WriteLine($"HEIGHT {outH}");
            sw.WriteLine("VIEWPOINT 0 0 0 1 0 0 0");
            sw.WriteLine($"POINTS {points}");
            sw.WriteLine("DATA ascii");

            var ci = CultureInfo.InvariantCulture;

            for (int v = 0; v < h; v += stride)
                for (int u = 0; u < w; u += stride)
                {
                    bool ok = TryGetRawDepth(depth, v, u, out double raw);
                    if (!ok)
                    {
                        if (organized)
                            sw.WriteLine("nan nan nan");
                        continue;
                    }

                    double uu = centerOrigin ? (u - cx) : u;
                    double vv = centerOrigin ? (v - cy) : v;

                    double x = uu * _DxUm;
                    double y = -vv * _DyUm;
                    double z = raw * _DzUm;

                    sw.WriteLine($"{x.ToString(ci)} {y.ToString(ci)} {z.ToString(ci)}");
                }
        }

        // 判断 List<float[]> 中的深度值是否有效
        private bool TryGetRawDepthFromList(List<float[]> heightData, int v, int u, out double raw)
        {
            raw = 0;
            if (v < 0 || v >= heightData.Count)
                return false;
            if (u < 0 || u >= heightData[v].Length)
                return false;

            float d = heightData[v][u];
            if (!float.IsFinite(d) || d == 8888880)
                return false;

            raw = d;
            return true;
        }

        public List<double[]> ToPointListUm(List<float[]> heightData, bool centerOrigin = false, int stride = 1)
        {
            if (heightData == null || heightData.Count == 0)
                throw new ArgumentException("heightData is null or empty", nameof(heightData));
            if (stride < 1) stride = 1;

            int h = heightData.Count;
            int w = heightData[0].Length;
            double cx = (w - 1) * 0.5;
            double cy = (h - 1) * 0.5;

            int count = 0;
            for (int v = 0; v < h; v += stride)
                for (int u = 0; u < w; u += stride)
                    if (TryGetRawDepthFromList(heightData, v, u, out _))
                        count++;

            var points = new List<double[]>(count);

            for (int v = 0; v < h; v += stride)
                for (int u = 0; u < w; u += stride)
                {
                    if (!TryGetRawDepthFromList(heightData, v, u, out double raw))
                        continue;

                    double uu = centerOrigin ? (u - cx) : u;
                    double vv = centerOrigin ? (v - cy) : v;

                    double x = uu * _DxUm;
                    double y = -vv * _DyUm;
                    double z = raw * _DzUm;

                    points.Add(new[] { x, y, z });
                }

            return points;
        }

        // PLY（List<float[]> 版本）
        public void SavePlyAsciiUm(string plyPath, List<float[]> heightData, bool centerOrigin = false, int stride = 1)
        {
            if (heightData == null || heightData.Count == 0)
                throw new ArgumentException("heightData is null or empty");

            int h = heightData.Count;
            int w = heightData[0].Length;
            double cx = (w - 1) * 0.5;
            double cy = (h - 1) * 0.5;

            int count = 0;
            for (int v = 0; v < h; v += stride)
                for (int u = 0; u < w; u += stride)
                    if (TryGetRawDepthFromList(heightData, v, u, out _))
                        count++;

            using var sw = new StreamWriter(plyPath);
            sw.WriteLine("ply");
            sw.WriteLine("format ascii 1.0");
            sw.WriteLine($"element vertex {count}");
            sw.WriteLine("property float x");
            sw.WriteLine("property float y");
            sw.WriteLine("property float z");
            sw.WriteLine("end_header");

            var ci = CultureInfo.InvariantCulture;

            for (int v = 0; v < h; v += stride)
                for (int u = 0; u < w; u += stride)
                {
                    if (!TryGetRawDepthFromList(heightData, v, u, out double raw))
                        continue;

                    double uu = centerOrigin ? (u - cx) : u;
                    double vv = centerOrigin ? (v - cy) : v;

                    double x = uu * _DxUm;
                    double y = -vv * _DyUm;
                    double z = raw * _DzUm;

                    sw.WriteLine($"{x.ToString(ci)} {y.ToString(ci)} {z.ToString(ci)}");
                }
        }

        // PCD（List<float[]> 版本）
        public void SavePcdAsciiUm(string pcdPath, List<float[]> heightData, bool centerOrigin = false, int stride = 1, bool organized = false)
        {
            if (heightData == null || heightData.Count == 0)
                throw new ArgumentException("heightData is null or empty");

            int h = heightData.Count;
            int w = heightData[0].Length;
            double cx = (w - 1) * 0.5;
            double cy = (h - 1) * 0.5;

            int outW = (w + stride - 1) / stride;
            int outH = (h + stride - 1) / stride;

            int points;
            if (organized)
            {
                points = outW * outH;
            }
            else
            {
                points = 0;
                for (int v = 0; v < h; v += stride)
                    for (int u = 0; u < w; u += stride)
                        if (TryGetRawDepthFromList(heightData, v, u, out _))
                            points++;
                outW = points;
                outH = 1;
            }

            using var sw = new StreamWriter(pcdPath);
            sw.WriteLine("# .PCD v0.7 - Point Cloud Data file format");
            sw.WriteLine("VERSION .7");
            sw.WriteLine("FIELDS x y z");
            sw.WriteLine("SIZE 4 4 4");
            sw.WriteLine("TYPE F F F");
            sw.WriteLine("COUNT 1 1 1");
            sw.WriteLine($"WIDTH {outW}");
            sw.WriteLine($"HEIGHT {outH}");
            sw.WriteLine("VIEWPOINT 0 0 0 1 0 0 0");
            sw.WriteLine($"POINTS {points}");
            sw.WriteLine("DATA ascii");

            var ci = CultureInfo.InvariantCulture;

            for (int v = 0; v < h; v += stride)
                for (int u = 0; u < w; u += stride)
                {
                    bool ok = TryGetRawDepthFromList(heightData, v, u, out double raw);
                    if (!ok)
                    {
                        if (organized)
                            sw.WriteLine("nan nan nan");
                        continue;
                    }

                    double uu = centerOrigin ? (u - cx) : u;
                    double vv = centerOrigin ? (v - cy) : v;

                    double x = uu * _DxUm;
                    double y = -vv * _DyUm;
                    double z = raw * _DzUm;

                    sw.WriteLine($"{x.ToString(ci)} {y.ToString(ci)} {z.ToString(ci)}");
                }
        }

        // 从HObject获取深度值（用于real类型图像）
        private bool TryGetRawDepthFromHObject(float[] depthData, int width, int v, int u, out double raw)
        {
            raw = 0;
            int index = v * width + u;
            if (index < 0 || index >= depthData.Length)
                return false;

            float d = depthData[index];
            if (!float.IsFinite(d) || d == 8888880)
                return false;
            raw = d;
            return true;
        }

        public List<double[]> ToPointListUm(HObject hoDepth, bool centerOrigin = false, int stride = 1)
        {
            if (hoDepth == null) throw new ArgumentNullException(nameof(hoDepth));
            if (stride < 1) stride = 1;

            HOperatorSet.GetImageSize(hoDepth, out HTuple hvWidth, out HTuple hvHeight);
            int w = hvWidth.I;
            int h = hvHeight.I;
            double cx = (w - 1) * 0.5;
            double cy = (h - 1) * 0.5;

            HOperatorSet.GetImagePointer1(hoDepth, out HTuple hvPointer, out HTuple hvType, out HTuple _, out HTuple _);
            float[] depthData = new float[h * w];
            Marshal.Copy(hvPointer.IP, depthData, 0, h * w);

            int count = 0;
            for (int v = 0; v < h; v += stride)
                for (int u = 0; u < w; u += stride)
                    if (TryGetRawDepthFromHObject(depthData, w, v, u, out _))
                        count++;

            var points = new List<double[]>(count);

            for (int v = 0; v < h; v += stride)
                for (int u = 0; u < w; u += stride)
                {
                    if (!TryGetRawDepthFromHObject(depthData, w, v, u, out double raw))
                        continue;

                    double uu = centerOrigin ? (u - cx) : u;
                    double vv = centerOrigin ? (v - cy) : v;

                    double x = uu * _DxUm;
                    double y = -vv * _DyUm;
                    double z = raw * _DzUm;

                    points.Add(new[] { x, y, z });
                }

            return points;
        }



        // PCD（从HObject，可选organized）
        public void SavePcdAsciiUm(string pcdPath, HObject hoDepth, bool centerOrigin = false, int stride = 1, bool organized = false)
        {
            HOperatorSet.GetImageSize(hoDepth, out HTuple hvWidth, out HTuple hvHeight);
            int w = hvWidth.I;
            int h = hvHeight.I;
            double cx = (w - 1) * 0.5;
            double cy = (h - 1) * 0.5;

            HOperatorSet.GetImagePointer1(hoDepth, out HTuple hvPointer, out HTuple hvType, out HTuple _, out HTuple _);
            float[] depthData = new float[h * w];
            Marshal.Copy(hvPointer.IP, depthData, 0, h * w);

            int outW = (w + stride - 1) / stride;
            int outH = (h + stride - 1) / stride;

            int points;
            if (organized)
            {
                points = outW * outH;
            }
            else
            {
                points = 0;
                for (int v = 0; v < h; v += stride)
                {
                    for (int u = 0; u < w; u += stride)
                    {
                        if (TryGetRawDepthFromHObject(depthData, w, v, u, out _))
                            points++;
                    }
                }

                outW = points;
                outH = 1;
            }

            using var sw = new StreamWriter(pcdPath);
            sw.WriteLine("# .PCD v0.7 - Point Cloud Data file format");
            sw.WriteLine("VERSION .7");
            sw.WriteLine("FIELDS x y z");
            sw.WriteLine("SIZE 4 4 4");
            sw.WriteLine("TYPE F F F");
            sw.WriteLine("COUNT 1 1 1");
            sw.WriteLine($"WIDTH {outW}");
            sw.WriteLine($"HEIGHT {outH}");
            sw.WriteLine("VIEWPOINT 0 0 0 1 0 0 0");
            sw.WriteLine($"POINTS {points}");
            sw.WriteLine("DATA ascii");

            var ci = CultureInfo.InvariantCulture;

            for (int v = 0; v < h; v += stride)
            {
                for (int u = 0; u < w; u += stride)
                {
                    bool ok = TryGetRawDepthFromHObject(depthData, w, v, u, out double raw);
                    if (!ok)
                    {
                        if (organized)
                            sw.WriteLine("nan nan nan");
                        continue;
                    }

                    double uu = centerOrigin ? (u - cx) : u;
                    double vv = centerOrigin ? (v - cy) : v;

                    double x = uu * _DxUm;
                    double y = -vv * _DyUm;
                    double z = raw * _DzUm;

                    sw.WriteLine($"{x.ToString(ci)} {y.ToString(ci)} {z.ToString(ci)}");
                }
            }
        }

    }
}
