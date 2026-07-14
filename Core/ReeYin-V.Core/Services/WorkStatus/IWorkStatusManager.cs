using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Services.WorkStatus
{
    public interface IWorkStatusManager
    {
        /// <summary>
        /// 当前工作状态(只读，通过方法设置值)
        /// </summary>
        public WorkStatus CurStatus { get;}

        /// <summary>
        /// 切换工作状态
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public bool SwitchWorkStatus(WorkStatus status);

    }

    public enum WorkStatus
    {
        None = 0,

        /// <summary>
        /// 空闲
        /// </summary>
        /// 
        Idle = 1,

        /// <summary>
        /// 运行
        /// </summary>
        Running = 2,

        /// <summary>
        /// 复位
        /// </summary>
        Reset = 4,

        /// <summary>
        /// 停止
        /// </summary>
        Stopped,

        /// <summary>
        /// 暂停
        /// </summary>
        Paused,

        /// <summary>
        /// 异常
        /// </summary>
        Error = 8,
    }
}
