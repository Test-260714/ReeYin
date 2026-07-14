using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Share.Config
{
    /// <summary>
    /// 软件配置提供者
    /// </summary>
    public interface ISoftwareConfigProvider
    {
        /// <summary>
        /// 软件参数
        /// </summary>
        SoftwareConfig SoftwareConfig { get; }

        /// <summary>
        /// 参数改变事件
        /// </summary>
        event Action OnChanged;
    }
}
