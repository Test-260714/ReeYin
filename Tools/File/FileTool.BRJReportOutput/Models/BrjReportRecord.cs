using SqlSugar;
using System;

namespace FileTool.BRJReportOutput.Models
{
    [SugarTable("brj_report_record", TableDescription = "BRJ报表记录")]
    public class BrjReportRecord
    {
        [SugarColumn(ColumnName = "序号", IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "序号")]
        public int Id { get; set; }

        [SugarColumn(ColumnName = "SN", Length = 100, IsNullable = false, ColumnDescription = "SN", IsOnlyIgnoreUpdate = true)]
        public string SN { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "创建时间", ColumnDataType = "datetime", IsNullable = false, ColumnDescription = "创建时间")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        [SugarColumn(ColumnName = "缺陷个数", ColumnDescription = "缺陷个数")]
        public int DefectCount { get; set; }

        [SugarColumn(ColumnName = "检测米数", ColumnDescription = "检测米数")]
        public double DetectMeters { get; set; }

        [SugarColumn(ColumnName = "结束时间", ColumnDataType = "datetime", IsNullable = true, ColumnDescription = "结束时间")]
        public DateTime? EndTime { get; set; }

        [SugarColumn(ColumnName = "产品宽度mm", ColumnDescription = "产品宽度mm")]
        public double ProductWidthMm { get; set; }

        [SugarColumn(ColumnName = "换卷长度m", ColumnDescription = "换卷长度m")]
        public double RollLengthM { get; set; }

        [SugarColumn(ColumnName = "操作员", Length = 100, IsNullable = true, ColumnDescription = "操作员")]
        public string OperatorName { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "班次", Length = 50, IsNullable = true, ColumnDescription = "班次")]
        public string ShiftName { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "产品型号", Length = 100, IsNullable = true, ColumnDescription = "产品型号")]
        public string ProductModel { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "图像个数", ColumnDescription = "图像个数")]
        public int ImageCount { get; set; }

        [SugarColumn(ColumnName = "OK数量", ColumnDescription = "OK数量")]
        public int OkCount { get; set; }

        [SugarColumn(ColumnName = "NG数量", ColumnDescription = "NG数量")]
        public int NgCount { get; set; }

        [SugarColumn(ColumnName = "相机数", ColumnDescription = "相机数")]
        public int CameraCount { get; set; }

        [SugarColumn(ColumnName = "分辨率X", ColumnDataType = "TEXT", IsNullable = true, ColumnDescription = "分辨率X")]
        public string ResolutionX { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "分辨率Y", ColumnDataType = "TEXT", IsNullable = true, ColumnDescription = "分辨率Y")]
        public string ResolutionY { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "图像宽度", ColumnDataType = "TEXT", IsNullable = true, ColumnDescription = "图像宽度")]
        public string ImageWidth { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "图像高度", ColumnDataType = "TEXT", IsNullable = true, ColumnDescription = "图像高度")]
        public string ImageHeight { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "缺陷图像Min宽", ColumnDescription = "缺陷图像Min宽")]
        public double DefectImageMinWidth { get; set; }

        [SugarColumn(ColumnName = "缺陷图像Min高", ColumnDescription = "缺陷图像Min高")]
        public double DefectImageMinHeight { get; set; }

        [SugarColumn(ColumnName = "分切左坐标", ColumnDataType = "TEXT", IsNullable = true, ColumnDescription = "分切左坐标")]
        public string SlitLeftCoordinates { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "分切右坐标", ColumnDataType = "TEXT", IsNullable = true, ColumnDescription = "分切右坐标")]
        public string SlitRightCoordinates { get; set; } = string.Empty;
    }
}
