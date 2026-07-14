namespace Custom.DefectOverview.Models.Common
{
    /// <summary>
    /// 双相机幅宽侧别。只有 Side 参与幅宽坐标映射，Group 只用于统计和筛选。
    /// </summary>
    public enum WidthSide
    {
        Unknown = 0,
        Left = 1,
        Right = 2
    }
}