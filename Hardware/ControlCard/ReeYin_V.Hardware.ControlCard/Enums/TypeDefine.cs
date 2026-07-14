using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Hardware.ControlCard
{
    /// <summary>
    /// 轴停止模式
    /// </summary>
    public enum AxisStopMode
    {
        减速停止,
        立即停止,
    }

    /// <summary>
    /// 运动轴在连续运动的方向
    /// </summary>
    public enum MoveDirection
    {
        正向 = 0,
        反向 = 1,
    }

    /// <summary>
    /// 速度模式
    /// </summary>
    public enum SpeedMode
    {
        Low,
        Midian,
        High,
        Work
    }
}
