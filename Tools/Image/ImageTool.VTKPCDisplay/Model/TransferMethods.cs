using HalconDotNet;
using PointCloudSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ImageTool.VTKPCDisplay.Model
{
    public static class TransferMethods
    {
        public static void FillPointCloudFromDepthHObject_Fast(
HObject hoDepth,
PointCloudXYZ cloud,
double dxUm, double dyUm, double dzUm,
bool centerOrigin = false,
int stride = 1)
        {
            if (stride < 1) stride = 1;

            HOperatorSet.GetImageSize(hoDepth, out HTuple hvWidth, out HTuple hvHeight);
            int w = hvWidth.I;
            int h = hvHeight.I;

            double cx = (w - 1) * 0.5;
            double cy = (h - 1) * 0.5;

            HOperatorSet.GetImagePointer1(hoDepth, out HTuple hvPointer, out HTuple hvType, out _, out _);
            string type = hvType.S;

            if (!type.Equals("real", StringComparison.OrdinalIgnoreCase) &&
                !type.Equals("float", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"hoDepth type is '{type}', expected float/real.");

            float[] depth = new float[w * h];
            Marshal.Copy(hvPointer.IP, depth, 0, depth.Length);

            // 1) 先计数（为了一次性 ReSize）
            int count = 0;
            for (int v = 0; v < h; v += stride)
            {
                int row = v * w;
                for (int u = 0; u < w; u += stride)
                {
                    float raw = depth[row + u];
                    if (!(raw > 0) || float.IsNaN(raw) || float.IsInfinity(raw))
                        continue;
                    count++;
                }
            }

            // 2) 清空并扩容到目标点数
            cloud.Clear();
            cloud.ReSize(count);

            // 3) 填充
            int idx = 0;
            for (int v = 0; v < h; v += stride)
            {
                int row = v * w;
                for (int u = 0; u < w; u += stride)
                {
                    float raw = depth[row + u];
                    if (!(raw > 0) || float.IsNaN(raw) || float.IsInfinity(raw))
                        continue;

                    double uu = centerOrigin ? (u - cx) : u;
                    double vv = centerOrigin ? (v - cy) : v;

                    double x = uu * dxUm;
                    double y = -vv * dyUm;
                    double z = raw * dzUm;

                    cloud.SetX(idx, x);
                    cloud.SetY(idx, y);
                    cloud.SetZ(idx, z);

                    idx++;
                }
            }

            // 可选：记录宽高（你的 DLL 里 getPointCloudW/H 看起来是内部属性；
            // 如果你希望点云带宽高，需要 DLL 提供 setW/setH 或类似接口才行）
        }
    }
}
