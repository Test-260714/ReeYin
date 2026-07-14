using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SqlSugar;

namespace ReeYin_V.Core.Models.Database.Tables
{
    [SugarTable("permission", TableDescription = "权限表")]
    public class Permission : BaseEntity
    {
        [SugarColumn(ColumnDescription = "权限ID（主键）")]
        public int PermId { get; set; }

        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "权限名称（如“用户查看”）")]
        public string PermName { get; set; }

        [SugarColumn(Length = 100, IsNullable = false, ColumnDescription = "权限标识（唯一，如“user:view”）")]
        public string PermCode { get; set; }

        [SugarColumn(Length = 100, IsNullable = false, ColumnDescription = "创建对象")]
        public int CreateBy { get; set; }
        // 导航属性：一个权限对应多个菜单关联
        [SugarColumn(IsIgnore = true)]
        public List<PermMenuRelation> PermissionMenus { get; set; }
    }
}
