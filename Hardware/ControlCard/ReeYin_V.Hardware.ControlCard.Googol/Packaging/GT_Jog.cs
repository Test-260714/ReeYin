using GTN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogolMotion
{
    /// <summary>
    /// Jog运动
    /// </summary>
    public partial class GoogolGTMotion
    {
        /// <summary>
        /// 设置JOG速度
        /// </summary>
        /// <param name="axisId">轴号从1开始</param>
        /// <param name="speed">速度</param>
        /// <returns></returns>
        public bool SetJogSpeed(short axisId, double[] speed,short core = 2)
        {
            short sRtn;
            mc.TJogPrm JogSpeed;
            JogSpeed.smooth = 0.5;
            double vel = speed[1] * 0.001;
            JogSpeed.acc = JogSpeed.dec = speed[2] * 0.001 * 0.001;
            sRtn = mc.GTN_PrfJog(core, axisId);//设置指定轴为 Jog 模式 
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            sRtn = mc.GTN_SetJogPrm(core, axisId, ref JogSpeed);//设置 Jog 运动模式下的运动参数
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            sRtn = mc.GTN_SetVel(core, axisId, vel);//设置目标速度
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            return true;
        }
        /// <summary>
        /// 开始JOG运动
        /// </summary>
        /// <param name="axisId">轴号从1开始</param>
        /// <returns></returns>
        public bool JogAxis(int axisId, short core = 2)
        {
            short sRtn;
            if (!ClearAxesSts())
            {
                return false;
            }
            sRtn = (mc.GTN_Update(core, 1 << (axisId - 1)));//启动 Jog 运动
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            return true;
        }

    }
}
