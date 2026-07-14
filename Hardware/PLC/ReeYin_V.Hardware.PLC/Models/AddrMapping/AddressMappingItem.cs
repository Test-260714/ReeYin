using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Hardware.PLC.Models
{
    /// <summary>
    /// 地址映射
    /// </summary>
    [Serializable]
    public class AddressMappingItem : BindableBase
    {
        /// <summary>
        /// 定制化指Key
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// 轴
        /// </summary>
        public EnumAxisType MotionAxis { get; set; }

        /// <summary>
        /// 运动类型
        /// </summary>
        public EnumMotionType MotionType { get; set; }

        /// <summary>
        /// 地址
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// 值
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 数据类型
        /// </summary>
        public EnumParaInfoModelParaType DataType { get; set; }

        public string DisplaySelectedKey { get; set; }

        public string DisplayMotionType { get; set; }


        public static List<EnumAxisType> ListEnumMotionAxis { get; set; } = new()
        {
            EnumAxisType.X,
            EnumAxisType.Y,
            EnumAxisType.ZTop,
            EnumAxisType.ZBottom,
            EnumAxisType.R,
            EnumAxisType.Undefined,
        };

        public AddressMappingItem()
        {

        }

        public AddressMappingItem(EnumAxisType motionAxis, EnumMotionType MotionType, string address, object value, EnumParaInfoModelParaType dataType, string description)
        {
            MotionAxis = motionAxis;
            this.MotionType = MotionType;
            Address = address;
            Value = value;
            DataType = dataType;
            Description = description;
            DisplaySelectedKey = $"{address}_{description}";
        }

        public AddressMappingItem(string key, EnumAxisType motionAxis, EnumMotionType MotionType, string address, object value, EnumParaInfoModelParaType dataType, string description)
        {
            Key = key;
            MotionAxis = motionAxis;
            this.MotionType = MotionType;
            Address = address;
            Value = value;
            DataType = dataType;
            Description = description;
            DisplaySelectedKey = $"{address}_{description}";
        }
    }
}
