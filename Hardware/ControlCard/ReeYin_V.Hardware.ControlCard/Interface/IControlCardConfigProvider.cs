using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Hardware.ControlCard
{
    /// <summary>
    /// 控制卡配置提供者
    /// </summary>
    public interface IControlCardConfigProvider
    {
        /// <summary>
        /// 控制卡参数
        /// </summary>
        ControlCardConfig ControlCardConfig { get; }

        /// <summary>
        /// 参数改变事件
        /// </summary>
        event Action OnChanged;
    }
}
