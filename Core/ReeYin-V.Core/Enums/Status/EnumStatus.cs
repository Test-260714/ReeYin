using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core
{
    /// <summary>
    /// 设备状态
    /// </summary>
    public enum HardwareState
    {
        NotConnected,                       // 未连接
        Connecting,                         // 正在连接
        Connected,                          // 已建立物理连接
        Initializing,                       // 初始化中
        Ready,                              // 可以工作
        Running,                            // 正在执行任务
        Paused,                             // 暂停（可选）
        Complete,                           // 完成
        Idle,                               // 空闲
        Error,                              // 错误
        Recovering,                         // 错误恢复中
        Disconnecting,                      // 正在断开
        Closed                              // 已关闭
    }

}
