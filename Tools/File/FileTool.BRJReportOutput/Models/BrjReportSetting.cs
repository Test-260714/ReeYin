using SqlSugar;

namespace FileTool.BRJReportOutput.Models
{
    [SugarTable("brj_report_setting", TableDescription = "BRJ报表参数")]
    public class BrjReportSetting
    {
        public const string DiameterGroupType = "DiameterGroup";
        public const string ReportOutputType = "ReportOutput";
        public const string ReportOutputDirectoryKey = "ReportOutputDirectory";

        [SugarColumn(ColumnName = "序号", IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "序号")]
        public int Id { get; set; }

        [SugarColumn(ColumnName = "配置类型", Length = 50, IsNullable = true, ColumnDescription = "配置类型")]
        public string SettingType { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "配置键", Length = 100, IsNullable = true, ColumnDescription = "配置键")]
        public string SettingKey { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "配置值", Length = 500, IsNullable = true, ColumnDescription = "配置值")]
        public string SettingValue { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "排序", ColumnDescription = "排序")]
        public int SortIndex { get; set; }

        [SugarColumn(ColumnName = "分组名称", Length = 100, IsNullable = true, ColumnDescription = "分组名称")]
        public string GroupName { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "最小直径mm", IsNullable = true, ColumnDescription = "最小直径mm")]
        public double? MinDiameterMm { get; set; }

        [SugarColumn(ColumnName = "最大直径mm", IsNullable = true, ColumnDescription = "最大直径mm")]
        public double? MaxDiameterMm { get; set; }

        [SugarColumn(ColumnName = "颜色", Length = 20, IsNullable = true, ColumnDescription = "颜色")]
        public string ColorHex { get; set; } = string.Empty;

        public static List<BrjReportSetting> CreateDefaultDiameterGroups()
        {
            return new List<BrjReportSetting>
            {
                new() { SettingType = DiameterGroupType, SortIndex = 1, MinDiameterMm = 0d, MaxDiameterMm = 0.1d, ColorHex = "#C43C39" },
                new() { SettingType = DiameterGroupType, SortIndex = 2, MinDiameterMm = 0.1d, MaxDiameterMm = 0.2d, ColorHex = "#2BA84A" },
                new() { SettingType = DiameterGroupType, SortIndex = 3, MinDiameterMm = 0.2d, MaxDiameterMm = 0.5d, ColorHex = "#3858D6" },
                new() { SettingType = DiameterGroupType, SortIndex = 4, MinDiameterMm = 0.5d, MaxDiameterMm = null, ColorHex = "#9A9F00" },
            };
        }
    }
}
