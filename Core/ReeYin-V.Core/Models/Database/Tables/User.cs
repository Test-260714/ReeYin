using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SqlSugar;

namespace ReeYin_V.Core.Models.Database.Tables
{
    [SugarTable("user", TableDescription = "用户表")]
    public class User : BaseEntity
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "用户ID（主键）")]
        public int UserId { get; set; }

        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "用户名（登录账号，唯一）")]
        public string Username { get; set; }

        [SugarColumn(Length = 60, IsNullable = false, ColumnDescription = "密码（BCrypt哈希值）")]
        public string PasswordHash { get; set; }

        [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "真实姓名")]
        public string RealName { get; set; }

        [SugarColumn(ColumnDescription = "角色ID（关联Role.RoleId）")]
        public int RoleId { get; set; }
        [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "卡号")]
        public string CardNo { get; set; }

        [SugarColumn(ColumnDescription = "状态（1=启用，0=禁用）", DefaultValue = "1")]
        public byte Status { get; set; } = 1;

    }
}
