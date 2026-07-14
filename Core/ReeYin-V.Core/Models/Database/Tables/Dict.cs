using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SqlSugar;

namespace ReeYin_V.Core.Models.Database.Tables
{
    [SugarTable("dict", TableDescription = "字典表")]
    public class Dict: BaseEntity
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "字典ID（主键）")]
        public int DictId { get; set; }

        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "字典类型（如“user_status”）")]
        public string DictType { get; set; }

        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "字典码（如“1”）")]
        public string DictCode { get; set; }

        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "字典值（如“启用”）")]
        public string DictValue { get; set; }

        [SugarColumn(ColumnDescription = "父级ID（顶级项为0）", DefaultValue = "0")]
        public int ParentId { get; set; } = 0;

        [SugarColumn(ColumnDescription = "排序", DefaultValue = "0")]
        public int Sort { get; set; } = 0;

        [SugarColumn(ColumnDescription = "是否启用（1=启用，0=禁用）", DefaultValue = "1")]
        public byte IsEnabled { get; set; } = 1;

        // 组合唯一约束（DictType + DictCode）
        public override bool Equals(object obj) =>
            obj is Dict other && DictType == other.DictType && DictCode == other.DictCode;

        public override int GetHashCode() => HashCode.Combine(DictType, DictCode);
    }
}
