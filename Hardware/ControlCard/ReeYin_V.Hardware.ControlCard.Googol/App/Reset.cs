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
    /// 复位相关操作
    /// </summary>
    public partial class GoogolControlCard : ControlCardBase
    {
        #region 复位
        /// <summary>
        /// 复位单个轴
        /// </summary>
        /// <param name="axisId">轴的编号从1开始</param>
        /// <param name="waitforend">是否等待复位完成</param>
        /// <returns></returns>
        public bool ResetAxis(En_AxisNum axisId, bool waitforend = true, int offset = 0)
        {
            var axisParam = ConvertAxis(axisId);
            return Motion.ResetAxis(axisParam.AxisNo, axisParam.GoHomeMode, waitforend, offset);
        }

        /// <summary>
        /// 复位系统
        /// </summary>
        /// <returns></returns>
        public bool ResetRobot()
        {
            if (IsReseting)
            {
                return false;
            }
            IsReseting = true;

            //清除轴报警
            CleanAlarm();

            //检查使能，无使能上使能
            if (!GetAxisClrSts(1, 4, En_GetAxisClrSts.Bit9_MotorEnabled))
            {
                Console.WriteLine("轴无使能，重新上使能！！");
                Motion.SetAxisEnabled(0, true);
            }

            //停止所有轴运动
            StopAxisMove();

            var homeAxes = Config.AllAxis
                .Where(axis => !IsEncoderPlannerOnlyAxis(axis))
                .ToArray();

            #region 优先级较高的先复位
            foreach (SingleAxisParam axis in homeAxes)
            {
                if (axis.Priority == En_Priority.Top || axis.Priority == En_Priority.High)
                {
                    if (!ResetAxis(axis.AxisNum, false, (int)ConvertToPluse(axis.AxisNum, axis.OriginOffset)))
                    {
                        IsReseting = false;
                        return false;
                    }
                }
            }

            //等待优先级较高的轴复位完毕
            foreach (SingleAxisParam axis in homeAxes)
            {
                if (axis.Priority == En_Priority.Top || axis.Priority == En_Priority.High)
                {
                    if (!AxisWaitResetZero(axis.AxisNum))
                    {
                        IsReseting = false;
                        return false;
                    }
                }
            }

            //复位优先级正常一般的轴
            foreach (SingleAxisParam axis in homeAxes) 
            {
                if (!ResetAxis(axis.AxisNum, false, (int)ConvertToPluse(axis.AxisNum, axis.OriginOffset)))//所有的XYR轴复位
                {
                    IsReseting = false;
                    return false;
                }
            }

            foreach (SingleAxisParam axisNum in homeAxes)
            {

                if (!AxisWaitResetZero(axisNum.AxisNum))//等待 XYR 轴复位完毕
                {
                    IsReseting = false;
                    return false;
                }
            }

            #endregion

            foreach (SingleAxisParam axisModel in homeAxes)
            {
                if (!ZeroPos(axisModel.AxisNum))//复位轴清零，X/Y只做编码器与规划器绑定
                {
                    IsReseting = false;
                    return false;
                }
            }

            //重新绑定编码器规划器位置
            foreach (SingleAxisParam axisModel in Config.AllAxis)
            {

                Motion.SetPrf(axisModel.AxisNo);

            }

            foreach (SingleAxisParam axisModel in homeAxes)
            {
                SetSpeed(axisModel.AxisNum, EN_SpeedType.Mid);
            }

            foreach (SingleAxisParam axisModel in homeAxes)
            {
                //复位至偏移原点
                MoveAbsoluteAxis(axisModel.AxisNum, ConvertToPluse(axisModel.AxisNum, 0), true);
            }

            WaitAxisMoveEnd();

            IsNeedReset = false;
            IsReseting = false;

            return true;
        }

        /// <summary>
        /// 等待轴复位完成
        /// </summary>
        /// <param name="axisId">轴的编号从1开始</param>
        /// <returns></returns>
        public bool AxisWaitResetZero(En_AxisNum axisId)
        {
            return Motion.WaitGoHomeEnd(ConvertAxis(axisId).AxisNo);
        }
        /// <summary>
        /// 清零规划位置和实际位置
        /// </summary>
        /// <param name="axisId"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public bool ZeroPos(En_AxisNum axisId, short count = 1)
        {
            return Motion.ZeroPos(1,ConvertAxis(axisId).AxisNo, count);
        }

        #endregion


        /// <summary>
        /// 清除报警
        /// </summary>
        /// <returns></returns>
        public bool CleanAlarm()
        {
            foreach (var core in Cores)
            {
                if (!Motion.CleanAlarm(core))
                    return false;

            }
            return true;
        }
    }
}
