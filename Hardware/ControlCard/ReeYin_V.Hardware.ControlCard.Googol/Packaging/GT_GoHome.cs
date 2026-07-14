using GoogolMotion.Models;
using GTN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogolMotion
{
    public partial class GoogolGTMotion
    {
        private double[,] _goHomeSpeed = new double[64, 3];	//回零速度

        /// <summary>
        /// 查询轴的回零状态
        /// </summary>
        /// <param name="axisId">轴号从1开始</param>
        /// <param name="sts">回零过程的状态值</param>
        /// <returns></returns>
        public bool EcatGetHomingStatus(short axisId, ref ushort sts,short core = 2)
        {
            short sRtn;
            //BIT 状态
            //bit0, 0：正在回零 1：回零完成
            //bit1, 0：无意义 1：回零成功完成
            //bit2, 0：无意义 1：回零过程出错
            sRtn = mc.GTN_GetEcatHomingStatus(core, axisId, out sts);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            return true;
        }
        /// <summary>
        /// 切换EtherCAT轴的回零模式
        /// </summary>
        /// <param name="axisId">轴号从1开始</param>
        /// <param name="mode">模式选择。6：回零模式；8：周期同步位置模式（CSP）</param>
        /// <returns></returns>
        public bool EcatSetHomingMode(short axisId, short mode, short core = 2)
        {
            short sRtn;
            sRtn = mc.GTN_SetHomingMode(core, axisId, mode);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            return true;
        }
        /// <summary>
        /// 清零规划位置和实际位置，并进行零漂补偿
        /// </summary>
        /// <param name="axisId">轴号从1开始</param>
        /// <param name="count">要清零的轴数量</param>
        /// <returns></returns>
        public bool ZeroPos(short core, short axisId, short count)
        {
            short sRtn;
            if (!Stop(axisId, count))
            {
                return false;
            }

            sRtn = mc.GTN_ZeroPos(core, axisId, count);
            if (sRtn != 0)
            {
                Console.WriteLine($"核{core}_GTN_ZeroPos()_Result:{sRtn}");
                ErrMessage(sRtn);
                return false;
            }

            return true;
        }
        /// <summary>
        /// 设置回零速度
        /// </summary>
        /// <param name="axisId">轴号从1开始</param>
        /// <param name="speed">速度</param>
        /// <returns></returns>
        public bool SetGoHomeSpeed(short axisId, double[] speed)
        {
            _goHomeSpeed[axisId - 1, 0] = speed[0];
            _goHomeSpeed[axisId - 1, 1] = speed[1];
            _goHomeSpeed[axisId - 1, 2] = speed[2];
            return true;
        }

        /// <summary>
        /// 单轴回原点
        /// </summary>
        /// <param name="axisId">轴号从1开始</param>
        /// <param name="waitforend">是否需要等待回原完成</param>
        /// <param name="offset">原点偏移量</param>
        /// <returns></returns>
        public bool ResetAxis(short axisId, short GoHomeMode, bool waitforend, int offset = 0, short core = 2)
        {
            short sRtn;

            sRtn = mc.GTN_SetHomingMode(core, axisId, 6);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            double[] dSpeed = [0, 0, 0];
            dSpeed[0] = _goHomeSpeed[axisId - 1, 0];//搜索开关速度，单位：驱动器设置的用户速度单位
            dSpeed[1] = _goHomeSpeed[axisId - 1, 1];//搜索 index 标识速度，单位：驱动器设置的用户速度单位
            dSpeed[2] = _goHomeSpeed[axisId - 1, 2];//搜索加速度，单位：驱动器设置的用户加速度单位 

            //short GoHomeMode = 5;
            sRtn = mc.GTN_SetEcatHomingPrm(core, axisId, GoHomeMode, dSpeed[0], dSpeed[1], dSpeed[2], offset, 0);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            sRtn = mc.GTN_StartEcatHoming(core, axisId);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }

            if (waitforend)
            {
                return WaitGoHomeEnd(axisId);
            }

            sRtn = mc.GTN_SetEncPos(1, axisId, 0);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
            }
            //double[] core1dEncPos = new double[3];
            //GetEncPos(axisId, ref core1dEncPos, 1);
            return true;
        }

        /// <summary>
        /// 等待回原点完成
        /// </summary>
        /// <param name="axisId">轴号从1开始</param>
        /// <param name="timeoutMs">轴号从1开始</param>
        /// <returns></returns>         
        public bool WaitGoHomeEnd(short axisId, int timeoutMs = 60 * 1000,short core = 2)
        {
            short sRtn;
            ushort sts;
            _isExits[axisId - 1] = false;

            DateTime starttime = DateTime.Now;
            while (true)
            {
                if ((DateTime.Now - starttime).TotalMilliseconds > timeoutMs)
                {
                    ErrMessage($"{axisId}轴回原点运动超时退出!");
                    mc.GTN_StopEcatHoming(core, axisId);
                    return false;
                }
                //bit0, 0：正在回零 1：回零完成
                //bit1, 0：无意义 1：回零成功完成
                //bit2, 0：无意义 1：回零过程出错
                sRtn = mc.GTN_GetEcatHomingStatus(core, axisId, out sts);
                if (sRtn != 0)
                {
                    ErrMessage(sRtn);
                    return false;
                }
                if (sts == 3 || sts == 1)
                {
                    break;
                }
                if (_isExits[axisId - 1])
                {
                    _isExits[axisId - 1] = false;
                    var axisStatuses = new int[5];
                    AxisGetSts(1, ref axisStatuses, axisStatuses.Length, core);
                    ErrMessage($"等待回零完成时，{axisId}轴主动退出，轴状态为：{sts}!");
                    mc.GTN_StopEcatHoming(core, axisId);
                    return false;
                }
                Thread.Sleep(10);
            }
            if (_isExits[axisId - 1])
            {
                _isExits[axisId - 1] = false;
                ErrMessage($"等待回零完成时，{axisId}轴主动退出!");
                mc.GTN_StopEcatHoming(core, axisId);
                return false;
            }
            sRtn = mc.GTN_SetHomingMode(core, axisId, 8);// 切换到位置控制模式 
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            return true;
        }
    }
}
