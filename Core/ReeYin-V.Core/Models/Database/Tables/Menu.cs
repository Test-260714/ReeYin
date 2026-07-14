using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SqlSugar;

namespace ReeYin_V.Core.Models.Database.Tables
{
    [SugarTable("menu", TableDescription = "菜单表")]
    public class Menu : BaseEntity
    {
        [SugarColumn(ColumnDescription = "菜单ID（主键）")]
        public int MenuId { get; set; }

        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "菜单名称")]
        public string MenuName { get; set; }

        [SugarColumn(ColumnDescription = "父菜单ID（0=根菜单）", DefaultValue = "0")]
        public int ParentId { get; set; } = 0;

        [SugarColumn(Length = 200, ColumnDescription = "前端路由路径（如“/user”）")]
        public string Path { get; set; }

        [SugarColumn(Length = 50, ColumnDescription = "菜单图标（如Font Awesome类名）")]
        public string Icon { get; set; }

        [SugarColumn(Length = 50, ColumnDescription = "事件名称（如“click”）")]
        public string Event { get; set; }

        [SugarColumn(Length = 50, ColumnDescription = "类型（用来区分导航/按钮/主菜单等）")]
        public string Type { get; set; }

        [SugarColumn(ColumnDescription = "排序（数值越小越靠前）", DefaultValue = "0")]
        public int Sort { get; set; } = 0;

        [SugarColumn(ColumnDescription = "状态（1=显示，0=隐藏）", DefaultValue = "1")]
        public byte Status { get; set; } = 1;
    }
}
