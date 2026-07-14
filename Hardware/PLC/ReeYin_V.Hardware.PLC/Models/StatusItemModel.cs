using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Hardware.PLC.Models
{
    public class StatusItemModel
    {
        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 键
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// 状态值
        /// </summary>
        public bool StatusValue { get; set; }

        /// <summary>
        /// 信息
        /// </summary>
        public string Message { get; set; }

        public StatusItemModel()
        {

        }


        public StatusItemModel(string keyText, string displayName, bool statusValue)
        {
            Key = keyText;
            StatusValue = statusValue;
            DisplayName = displayName;
        }
    }
}
