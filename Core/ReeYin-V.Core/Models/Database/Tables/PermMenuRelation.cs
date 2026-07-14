using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Models.Database.Tables
{
    [SugarTable("PermMenuRelation", TableDescription = "权限菜单关联表")]
    public class PermMenuRelation : BaseEntity
    {
        [SugarColumn(IsPrimaryKey = true)]
        public int PermId { get; set; }

        [SugarColumn(IsPrimaryKey = true)]
        public int MenuId { get; set; }
        /// <summary>
        /// 菜单是否可见
        /// </summary>
        public bool IsVisible { get; set; }
       /// <summary>
       /// 菜单是否可编辑
       /// </summary>
        public bool IsEnabled { get; set; }

        public bool CanRead { get; set; }

        public bool CanWrite { get; set; }

        // 导航属性：关联菜单
        [SugarColumn(IsIgnore = true)]
        public Menu Menu { get; set; }
    }
}
