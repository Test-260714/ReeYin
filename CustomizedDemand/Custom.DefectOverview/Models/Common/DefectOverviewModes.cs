namespace Custom.DefectOverview.Models.Common
{
    /// <summary>
    /// 缺陷总览的数据输入方式，避免把输入来源和现场物理拓扑混在一起。
    /// </summary>
    public enum DefectOverviewInputMode
    {
        SingleResult = 0,
        DualDirect = 1,
        MergedResultsByImage = 2,
        MultiPostProcessBinding = 3
    }

    /// <summary>
    /// 缺陷总览对应的现场物理拓扑。
    /// </summary>
    public enum DefectOverviewTopologyMode
    {
        SingleCamera = 0,
        DualCameraWidth = 1,
        GroupedDualCamera = 2
    }

    /// <summary>
    /// 缺陷总览页面展示布局，不参与输入解析和坐标映射。
    /// </summary>
    public enum DefectOverviewDisplayMode
    {
        SinglePanel = 0,
        DualPanel = 1,
        GroupedMatrix = 2
    }
}