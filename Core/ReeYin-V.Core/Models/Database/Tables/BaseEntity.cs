using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;
using SqlSugar;

namespace ReeYin_V.Core.Models.Database.Tables
{
    public abstract class BaseEntity
    {
        /// <summary>
        /// 创建人ID
        /// </summary>
        [SugarColumn(IsNullable = false,ColumnDescription = "创建人ID（关联sys_user.user_id）")]
        public int? CreateBy { get; set; }

        /// <summary>
        /// 创建时间（默认当前时间）
        /// </summary>
        [SugarColumn(IsNullable = false,IsIdentity = false,ColumnDescription = "创建时间", ColumnDataType = "datetime", 
            DefaultValue = "CURRENT_TIMESTAMP")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新人ID
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "更新人ID（关联sys_user.user_id）")]
        public int? UpdateBy { get; set; }

        /// <summary>
        /// 更新时间（默认当前时间，更新时自动修改）
        [SugarColumn(IsNullable = false,ColumnDescription = "更新时间", ColumnDataType = "datetime",
                     DefaultValue = "CURRENT_TIMESTAMP")]
        public DateTime UpdateTime { get; set; } = DateTime.Now;


        /// <summary>
        /// 描述
        /// </summary>
        [SugarColumn(IsNullable = true, Length = 200, ColumnDescription = "描述")]
        public string Description { get; set; }

    }
}
