using SqlSugar;
using System;

namespace FileTool.BRJReportOutput.Models
{
    [SugarTable("brj_defect_record", TableDescription = "BRJ缺陷明细记录")]
    public class BrjDefectRecord
    {
        [SugarColumn(ColumnName = "序号", IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "序号")]
        public int Id { get; set; }

        [SugarColumn(ColumnName = "SN", Length = 100, IsNullable = false, ColumnDescription = "SN")]
        public string SN { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "缺陷索引", ColumnDescription = "缺陷索引")]
        public int DefectIndex { get; set; }

        [SugarColumn(ColumnName = "相机索引", ColumnDescription = "相机索引")]
        public int CameraIndex { get; set; }

        [SugarColumn(ColumnName = "相机名称", Length = 50, IsNullable = true, ColumnDescription = "相机名称")]
        public string CameraName { get; set; } = string.Empty;

        [SugarColumn(IsIgnore = true)]
        public string CameraDisplayName => string.IsNullOrWhiteSpace(CameraName) ? CameraIndex.ToString() : CameraName;

        [SugarColumn(ColumnName = "分段号", ColumnDescription = "分段号")]
        public int SegmentIndex { get; set; }

        [SugarColumn(ColumnName = "分切号", ColumnDescription = "分切号")]
        public int SlitIndex { get; set; }

        [SugarColumn(ColumnName = "类型", Length = 50, IsNullable = true, ColumnDescription = "类型")]
        public string DefectType { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "面积/mm^2", ColumnDescription = "面积/mm^2")]
        public double AreaMm2 { get; set; }

        [SugarColumn(ColumnName = "直径/mm", ColumnDescription = "直径/mm")]
        public double DiameterMm { get; set; }

        [SugarColumn(ColumnName = "横位置/mm", ColumnDescription = "横位置/mm")]
        public double PositionXMm { get; set; }

        [SugarColumn(ColumnName = "纵位置/m", ColumnDescription = "纵位置/m")]
        public double PositionYM { get; set; }

        [SugarColumn(ColumnName = "缺陷图路径", ColumnDataType = "TEXT", IsNullable = true, ColumnDescription = "缺陷图路径")]
        public string DefectImagePath { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "创建时间", ColumnDataType = "datetime", IsNullable = false, ColumnDescription = "创建时间")]
        public DateTime CreateTime { get; set; } = DateTime.Now;
    }
}
