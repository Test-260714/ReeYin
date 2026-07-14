using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SqlSugar;

namespace ReeYin_V.Core.Models.Database.Tables
{
    [SugarTable("role", TableDescription = "角色表")]
    public class Role : BaseEntity
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "角色ID（主键）")]
        public int RoleId { get; set; }

        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "角色名称（唯一）")]
        public string RoleName { get; set; }

        [SugarColumn(ColumnDescription = "状态（1=启用，0=禁用）", DefaultValue = "1")]
        public byte Status { get; set; } = 1;

        [SugarColumn(ColumnDescription = "权限等级（关联Permission.PermId）", DefaultValue = "3")]
        public int PermissionID { get; set; } = 1;
    }
}
