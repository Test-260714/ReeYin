using Custom.DefectOverview.Models.Common;
using System;

namespace Custom.DefectOverview.Services.GroupedDualCamera
{
    /// <summary>
    /// 双相机幅宽坐标映射。Group 只用于统计和筛选，不参与幅宽展开。
    /// </summary>
    public static class DualWidthCoordinateMapper
    {
        public static double ToFullWidthRatio(WidthSide side, double localXRatio)
        {
            localXRatio = Math.Clamp(localXRatio, 0d, 1d);

            return side switch
            {
                WidthSide.Left => localXRatio * 0.5d,
                WidthSide.Right => 0.5d + localXRatio * 0.5d,
                _ => localXRatio
            };
        }
    }
}