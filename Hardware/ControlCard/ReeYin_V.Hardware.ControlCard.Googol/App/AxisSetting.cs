
using Dm.util;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.ControlCard.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ReeYin_V.Hardware.ControlCard.ControlCardConfig;

namespace ReeYin_V.Hardware.ControlCard.Googol.App
{
    /// <summary>
    /// 轴设置
    /// </summary>
    public partial class GoogolControlCard : ControlCardBase
    {
        #region Basic
        /// <summary>
        /// 设置轴使能/失能
        /// 0：设置所有轴
        /// </summary>
        /// <param name="axisId"></param>
        /// <param name="isEnabled"></param>
        /// <returns></returns>
        public override bool SetAxisEnabled(short axisId, bool isEnabled)
        {
            if (Motion.SetAxisEnabled(axisId, isEnabled))
            {
                Config.AllAxis.FirstOrDefault(p => p.AxisNo == axisId).IsEnable = isEnabled;
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        #region 轴运动速度设置
        /// <summary>
        /// 设置单个轴运行速度
        /// </summary>
        /// <param name="axis">轴号从1开始</param>
        /// <param name="spd">速度</param>
        /// <returns></returns>
        public bool SetSpeed(En_AxisNum axis, double[] spd)
        {
            bool rs = false;
            try
            {
                double[] speedpluse = new double[spd.Length];
                for (int i = 0; i < spd.Length; i++)
                {
                    speedpluse[i] = spd[i] * GetPulseequ(axis);
                }
                rs = Motion.SetMoveSpeed(ConvertAxis(axis).AxisNo, speedpluse);
                Console.WriteLine($"SetSpeed Axis {axis.ToString()} Speed {spd} rs {rs.ToString()}");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"SetCustomSingleSpeed  {ex.ToString()},{ex.StackTrace}");
            }
            return true;
        }

        /// <summary>
        /// 设置单个轴运行速度
        /// </summary>
        /// <param name="axis">轴的编号从1开始</param>
        /// <param name="speedtype">设置的速度类型</param>
        /// <returns></returns>
        public bool SetSpeed(En_AxisNum axis, EN_SpeedType speedtype)
        {
            bool rs = false;
            try
            {
                double[] speed = GetSpeed(axis, speedtype);
                rs = Motion.SetMoveSpeed(ConvertAxis(axis).AxisNo, ConvertToPluse(axis, speed));
                if (!rs)
                {
                    Console.WriteLine($"SetSpeed Axis {axis} Speed {speed} rs {rs.ToString()}");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"SetSpeed {ex},{ex.StackTrace}");
            }
            return rs;

        }

        /// <summary>
        /// 设置所有轴的运行速度
        /// </summary>
        /// <param name="speedtype">速度类型</param>
        /// <returns></returns>
        public bool SetSpeedAll(EN_SpeedType speedtype)
        {
            try
            {
                bool rs = false;
                foreach (SingleAxisParam axis in Config.AllAxis)
                {
                    double[] speed = GetSpeed(axis.AxisNum, speedtype);
                    rs = Motion.SetMoveSpeed(axis.AxisNo, ConvertToPluse(axis.AxisNum, speed), 25);
                    if (!rs)
                    {
                        Console.WriteLine($"SetSpeedAll()_core  Axis {axis} Speed {speed}");
                        return false;
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SetSpeedAll {ex.Message}, {ex.StackTrace}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 单独设置单个轴的最大运行速度，默认修改工作速度并立即下发。
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <param name="maxSpeed">最大运行速度</param>
        /// <param name="speedtype">速度类型，默认工作速度</param>
        /// <returns></returns>
        public bool SetMaxSpeed(En_AxisNum axis, double maxSpeed, EN_SpeedType speedtype = EN_SpeedType.Work)
        {
            try
            {
                if (maxSpeed < 0)
                {
                    Console.WriteLine($"SetMaxSpeed Axis {axis} 最大运行速度不能小于0，当前值：{maxSpeed}");
                    return false;
                }

                var axisModel = ConvertAxis(axis);
                if (axisModel == null)
                {
                    Console.WriteLine($"SetMaxSpeed 当前轴{axis}未配置");
                    return false;
                }

                var speedSetting = axisModel.SpeedDict1?.FirstOrDefault(p => p.SpeedType == speedtype);
                if (speedSetting == null)
                {
                    Console.WriteLine($"SetMaxSpeed Axis {axis} 未找到{speedtype}速度配置");
                    return false;
                }

                speedSetting.MaxSpeed = maxSpeed;
                return SetSpeed(axis, speedtype);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SetMaxSpeed {ex},{ex.StackTrace}");
                return false;
            }
        }
        #endregion

        #region JOG
        /// <summary>
        /// 设置JOG速度
        /// </summary>
        /// <param name="axisId">轴号从1开始</param>
        /// <param name="speed">速度</param>
        /// <returns></returns>
        public bool SetJogSpeed(En_AxisNum axis, double[] speed)
        {
            return Motion.SetJogSpeed(ConvertAxis(axis).AxisNo, ConvertToPluse(axis, speed));
        }

        /// <summary>
        /// 设置Jog速度
        /// </summary>
        /// <param name="axis">轴号从1开始</param>
        /// <param name="speedtype">速度类型</param>
        /// <param name="dir">false负脉冲 true正脉冲</param>
        /// <returns></returns>
        public bool SetJogSpeed(En_AxisNum axis, EN_SpeedType speedtype, bool dir = true)
        {
            double[] speed = new double[3];
            bool rs = false;
            try
            {
                var tmpSpd = ConvertToPluse(axis, GetSpeed(axis, speedtype));

                if (dir == false)
                {
                    speed[0] = tmpSpd[0];
                    speed[1] = tmpSpd[1] * -1;
                    speed[2] = tmpSpd[2];
                }
                else
                {
                    speed[0] = tmpSpd[0];
                    speed[1] = tmpSpd[1];
                    speed[2] = tmpSpd[2];
                }

                foreach (var core in Cores)
                {
                    rs = Motion.SetJogSpeed(ConvertAxis(axis).AxisNo, speed, core);
                    //Console.WriteLine($"内核{core}，SetJogSpeed Axis {axis.ToString()} Speed {speed} rs {rs.ToString()}");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"SetSpeed {ex.ToString()},{ex.StackTrace}");
            }
            return rs;

        }

        /// <summary>
        /// 单个轴开始JOG运动
        /// </summary>
        /// <param name="axisId"></param>
        /// <returns></returns>
        public bool JogAxis(En_AxisNum axisId)
        {
            return Motion.JogAxis(ConvertAxis(axisId).AxisNo);
        }

        public override bool JogAxis(En_AxisNum axisId, MoveDirection dir, EN_SpeedType spdType, bool isRunStop)
        {
            if (!IsConnected)
            {
                return false;
            }
            if (isRunStop)
            {
                SetJogSpeed(axisId, spdType, (dir == MoveDirection.正向 ? true : false) ^ ConvertAxis(axisId).MovingDirNe);

                var tasks = Cores.Select(core => Task.Run(() =>
                    Motion.JogAxis(ConvertAxis(axisId).AxisNo, core))).ToArray();

                bool[] results = Task.WhenAll(tasks).Result;

                return results.All(r => r);
            }
            else
            {
                var tasks = Cores.Select(core => Task.Run(() =>
                    Motion.Stop(1, Config.AllAxis.Count, core))).ToArray();

                bool[] results = Task.WhenAll(tasks).Result;

                return results.All(r => r);
            }
        }

        public override bool JogAxis(En_AxisNum axisId, MoveDirection dir, float step)
        {
            if (!IsConnected)
            {
                return false;
            }
            //切换到点动重新设置低速
            if (!SetSpeedAll(EN_SpeedType.Low))
            {
                return false;
            }

            if (!(dir == MoveDirection.正向))
            {
                step *= -1; // 反方向取反
            }

            var disPulse = step * ConvertAxis(axisId).PulseEquivalent;
            CleanAlarm();//清除轴报警
            var tasks = Cores.Select(core => Task.Run(() =>
    Motion.MoveRelativeSingleAxis(ConvertAxis(axisId).AxisNo, (int)disPulse, true, core))).ToArray();

            bool[] results = Task.WhenAll(tasks).Result;

            return results.All(r => r);

        }
        #endregion

        #region 轴运动
        private const int WaitAxisMoveEndTimeoutMs = 999 * 1000;

        private bool WaitAxisMoveEndByAxisNos(IEnumerable<short> axisNos)
        {
            var sortedAxisNos = axisNos?
                .Where(axisNo => axisNo > 0)
                .Distinct()
                .OrderBy(axisNo => axisNo)
                .ToArray();

            if (sortedAxisNos == null || sortedAxisNos.Length == 0)
            {
                return true;
            }

            if (!Motion.CleanAlarm())
            {
                return false;
            }

            short rangeStart = sortedAxisNos[0];
            short rangeEnd = rangeStart;

            for (int i = 1; i <= sortedAxisNos.Length; i++)
            {
                bool isRangeBreak = i == sortedAxisNos.Length || sortedAxisNos[i] != rangeEnd + 1;
                if (isRangeBreak)
                {
                    short rangeCount = (short)(rangeEnd - rangeStart + 1);
                    if (!Motion.WaitAxesMoveEnd(rangeStart, rangeCount, WaitAxisMoveEndTimeoutMs))
                    {
                        return false;
                    }

                    if (i < sortedAxisNos.Length)
                    {
                        rangeStart = sortedAxisNos[i];
                        rangeEnd = rangeStart;
                    }

                    continue;
                }

                rangeEnd = sortedAxisNos[i];
            }

            return true;
        }

        /// <summary>
        /// 等待所有轴运动结束
        /// </summary>
        /// <returns></returns>
        public bool WaitAxisMoveEnd()
        {
            return WaitAxisMoveEndByAxisNos(Config.AllAxis.Select(axis => axis.AxisNo));
        }
        public bool WaitALLAxisMoveEnd()
        {
            return WaitAxisMoveEnd();
        }


        /// <summary>
        /// 等待单个轴运动结束
        /// </summary>
        /// <param name="axisNum">轴号从1开始</param>
        /// <returns></returns>
        public bool WaitAxisMoveEnd(En_AxisNum axisNum)
        {
            var axis = ConvertAxis(axisNum);
            if (axis == null)
            {
                Console.WriteLine($"当前轴{axisNum}未配置");
                return false;
            }

            return WaitAxisMoveEndByAxisNos(new short[] { axis.AxisNo });
        }

        /// <summary>
        /// 等待多个轴运动结束
        /// </summary>
        /// <param name="axisNum">轴号从1开始</param>
        /// <returns></returns>
        public bool WaitAxisMoveEnd(En_AxisNum[] axisNum)
        {
            if (axisNum == null || axisNum.Length == 0)
            {
                return true;
            }

            List<short> axisNos = new List<short>(axisNum.Length);
            foreach (var axis in axisNum)
            {
                var axisModel = ConvertAxis(axis);
                if (axisModel == null)
                {
                    Console.WriteLine($"当前轴{axis}未配置");
                    return false;
                }

                axisNos.Add(axisModel.AxisNo);
            }

            return WaitAxisMoveEndByAxisNos(axisNos);
        }

        /// <summary>
        /// 单个轴绝对坐标移动
        /// </summary>
        /// <param name="axisId">轴号从1开始</param>
        /// <param name="fpos">点位(mm)</param>
        /// <param name="waitforend">是否需要等待</param>
        /// <returns></returns>
        public bool MoveAbsoluteToPointSingleAxis(En_AxisNum axisId, double fpos, bool waitforend)
        {
            if (!Motion.CleanAlarm()) return false;
            foreach (var core in Cores)
            {
                if (!Motion.MoveAbsoluteSingleAxis(ConvertAxis(axisId).AxisNo, (int)ConvertToPluse(axisId, fpos), waitforend, core))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 多个轴绝对坐标移动
        /// </summary>
        /// <param name="axisId">需要移动的轴</param>
        /// <param name="pos">点位(mm)</param>
        /// <returns></returns>
        public bool MoveAbsoluteToPointMultiAxis(En_AxisNum[] axisId, double[] pos)
        {
            if (!Motion.CleanAlarm()) return false;
            for (int i = 0; i < pos.Length; i++)
            {
                if (!Motion.MoveAbsoluteSingleAxis(ConvertAxis(axisId[i]).AxisNo, (int)ConvertToPluse(axisId[i], pos[i]), false))
                {
                    return false;
                }
            }
            WaitAxisMoveEnd(axisId);
            return true;
        }

        /// <summary>
        /// 指定单个轴移动到绝对位置
        /// </summary>
        /// <param name="axisId"></param>
        /// <param name="fpos"></param>
        /// <param name="waitforend"></param>
        /// <returns></returns>
        public override bool MoveAbsoluteAxis(En_AxisNum axisId, double fpos, bool waitforend = false)
        {
            return MoveAbsoluteToPointSingleAxis(axisId, fpos, waitforend);
        }

        public bool MoveAbsoluteXY(float[] xy)
        {
            double[] dxy = xy.Select(item => (double)item).Take(2).ToArray();
            return MoveAbsoluteToPointMultiAxis(new En_AxisNum[] { En_AxisNum.X, En_AxisNum.Y }, dxy);
        }

        public bool MoveAbsoluteXYZ(float[] xyz)
        {
            if (xyz.Length < 3)
            {
                return false;
            }
            double[] dxyz = xyz.Select(item => (double)item).ToArray();
            if (!MoveAbsoluteToPointMultiAxis(new En_AxisNum[] { En_AxisNum.X, En_AxisNum.Y, En_AxisNum.Z }, new double[] { dxyz[0], dxyz[1], dxyz[2] }))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 单个轴相对坐标移动
        /// </summary>
        /// <param name="axisId">轴号从1开始</param>
        /// <param name="fDis">需要移动的距离(mm)</param>
        /// <param name="waitforend">是否需要等待</param>
        /// <returns></returns>
        public bool MoveRelativeToPointSingleAxis(En_AxisNum axisId, double fDis, bool waitforend)
        {
            double fDispluse = 0;
            fDispluse = fDis * GetPulseequ(axisId);
            if (!Motion.CleanAlarm()) return false;
            return Motion.MoveRelativeSingleAxis((short)axisId, (int)fDispluse, waitforend);
        }

        /// <summary>
        /// 多个轴相对坐标移动
        /// </summary>
        /// <param name="axisId">轴号从1开始</param>
        /// <param name="fDis">需要移动的距离</param>
        /// <returns></returns>
        public bool MoveRelativeToPointMultiAxis(En_AxisNum[] axisId, double[] fDis)
        {
            if (!Motion.CleanAlarm()) return false;
            for (int i = 0; i < axisId.Length; i++)
            {
                if (!Motion.MoveRelativeSingleAxis((short)(axisId[i]), (int)(fDis[i] * GetPulseequ(axisId[i])), false))
                {
                    return false;
                }
            }
            WaitAxisMoveEnd(axisId);
            return true;
        }
        #endregion

        #region 停止轴运动
        /// <summary>
        /// 停止轴运动
        /// </summary>
        /// <param name="axisId">轴的编号</param>
        /// <param name="count">轴的数量</param>
        /// <returns></returns>
        public bool StopAxisMove(En_AxisNum axisId, int count = 1)
        {
            bool ret = Motion.Stop(ConvertAxis(axisId).AxisNo, count);
            return ret;
        }
        /// <summary>
        /// 停止所有轴的运动
        /// </summary>
        /// <returns></returns>
        public bool StopAxisMove()
        {
            return IsConnected ? Motion.Stop(1, Config.AllAxis.Count) : false;
        }

        public bool StopCrdMove(short csid, En_AxisNum AxisId)
        {
            return IsConnected ? Motion.StopCrdMove(csid, ConvertAxis(AxisId).AxisNo) : false;
        }

        public bool PauseCrdMove(En_AxisNum AxisId)
        {
            return IsConnected ? Motion.PauseCrdMove(ConvertAxis(AxisId).AxisNo) : false;
        }

        #endregion


    }
}
