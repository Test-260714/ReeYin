using GoogolMotion;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.ControlCard.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static ReeYin_V.Hardware.ControlCard.ControlCardConfig;

namespace ReeYin_V.Hardware.ControlCard.Googol.App
{
    /// <summary>
    /// 固高运动控制卡
    /// </summary>
    public partial class GoogolControlCard : ControlCardBase
    {
        #region Fields
        private Dictionary<string, IOModel> IOModels = new Dictionary<string, IOModel>();

        private static object _stcobj = new object();

        /// <summary>
        /// 控制卡实例（不允许外部操作）
        /// </summary>
        private GoogolGTMotion Motion = new GoogolGTMotion(2);

        int warningTimes = 0;//连续报警五次，暂停日志输出

        /// <summary>
        /// 核数
        /// 目前核1用来实现位置比较功能
        ///     核2用来实现运动功能
		///		核1和核2都使用FIFO0操作
        /// </summary>
        private short[] Cores = [2,1];
        #endregion

        #region Constructor
        public GoogolControlCard()
        {

            
        }
        #endregion

        #region Override
        protected override void DoClose()
        {
            //断开所有组件
            Console.WriteLine("正在关闭控制卡连接...");

        }

        protected override void DoConfigure()
        {
            try
            {
                var ConnectFlag = false;
                if (Connect(ConnectFlag))
                {
                    Console.WriteLine("控制卡连接成功！");
                }
                else
                {
                    Console.WriteLine("控制卡连接失败！");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DoConfigure()_获取配置失败{ex.StackTrace}");
            }
        }

        protected override bool DoGetAxisEnable(En_AxisNum axisType)
        {
            try
            {




                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DoGetAxisEnable()_获取轴使能状态失败{ex.StackTrace}");
                return false;
            }
        }

        protected override bool DoGetAxisStopped(En_AxisNum axisType)
        {
            try
            {
                //清除轴报警
                CleanAlarm();

                //检查使能，无使能上使能
                if (!GetAxisClrSts(1, Config.AllAxis.Count, En_GetAxisClrSts.Bit9_MotorEnabled))
                {
                    Console.WriteLine("轴无使能，重新上使能！！");
                    Motion.SetAxisEnabled(0, true);
                }

                //停止所有轴运动
                StopAxisMove();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DoGetAxisStopped()_获取轴停止状态失败{ex.StackTrace}");
                return false;
            }
        }

        protected override bool DoGoHome(out string message)
        {
            message = string.Empty;

            try
            {
                if (!ResetRobot())
                {
                    message = "固高控制卡复位失败。";
                    return false;
                }

                message = "固高控制卡复位完成。";
                return true;
            }
            catch (Exception ex)
            {
                message = $"固高复位异常：{ex.Message}";
                Console.WriteLine($"DoGoHome()_轴回零失败{ex}");
                return false;
            }
        }

        private static bool IsHighPriorityHomeAxis(SingleAxisParam axis)
        {
            return axis.Priority == En_Priority.Top || axis.Priority == En_Priority.High;
        }

        private static bool IsEncoderPlannerOnlyAxis(SingleAxisParam axis)
        {
            return axis.AxisNum == En_AxisNum.X || axis.AxisNum == En_AxisNum.Y;
        }

        protected override bool DoInit()
        {
            try
            {
                DoConfigure();


                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DoInit()_控制卡初始化失败{ex.StackTrace}");
                return false;
            }
        }

        protected override bool DoMoveAxis(En_AxisNum axisType, double um)
        {
            try
            {

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DoMoveAxis()_轴移动失败{ex.StackTrace}");
                return false;
            }
        }

        protected override bool DoMoveContinue(En_AxisNum axisType, MoveDirection moveDirection)
        {
            try
            {

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DoMoveContinue()_轴连续运动失败{ex.StackTrace}");
                return false;
            }
        }

        protected override bool DoSetAxisEnable(En_AxisNum axisType, bool v)
        {
            try
            {

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DoSetAxisEnable()_轴使能失败{ex.StackTrace}");
                return false;
            }
        }

        protected override void DoStop(En_AxisNum? axisType, AxisStopMode stopMode)
        {
            try
            {


            }
            catch (Exception ex)
            {
                Console.WriteLine($"DoStop()_轴停止失败{ex.StackTrace}");
            }
        }

        public override bool GetAllPosInfos(short core = 2)
        {
            try
            {
                lock (_stcobj)
                {
                    if (IsConnected)
                    {
                        //bool rs = false;
                        //var tmpAllPosInfos = new double[allPosInfos.Length];
                        //rs = Motion.GetPrfPos(1, ref tmpAllPosInfos,core);
                        //for (int i = 0; i < allPosInfos.Length; i++)
                        //{
                        //    allPosInfos[i] = Math.Round(tmpAllPosInfos[Config.AllAxis[i].AxisNo-1] / Config.AllAxis[i].PulseEquivalent, 2);
                        //}
                        bool rs = false;
                        var tmpAllPosInfos = new double[Config.AllAxis.Count];
                        rs = Motion.GetPrfPos(1, ref tmpAllPosInfos, core);
                        for (int i = 0; i < Config.AllAxis.Count; i++)
                        {
                            Config.AllAxis[i].CurPos = Math.Round(tmpAllPosInfos[Config.AllAxis[i].AxisNo - 1] / Config.AllAxis[i].PulseEquivalent, 2);
                        }

                        return rs;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetAllPosInfos()_获取所有轴的位置信息失败{ex.StackTrace}");
                return false;
            }
        }

        public override bool GetAllSpeedInfos(short core = 2)
        {
            try
            {
                lock (_stcobj)
                {
                    if (!IsConnected)
                    {
                        return false;
                    }

                    EnsureSpeedBuffers();
                    var maxAxisNo = Config.AllAxis.Count == 0
                        ? 0
                        : Config.AllAxis.Max(axis => Math.Max(1, (int)axis.AxisNo));

                    if (maxAxisNo == 0)
                    {
                        return true;
                    }

                    var tmpAllSpeedInfos = new double[maxAxisNo];
                    var rs = Motion.GetEncVel(1, ref tmpAllSpeedInfos, core);
                    if (!rs)
                    {
                        return false;
                    }

                    for (var i = 0; i < Config.AllAxis.Count; i++)
                    {
                        var axisConfig = Config.AllAxis[i];
                        var sourceIndex = axisConfig.AxisNo - 1;
                        if (sourceIndex < 0 || sourceIndex >= tmpAllSpeedInfos.Length)
                        {
                            continue;
                        }

                        var pulseEquivalent = Math.Abs(axisConfig.PulseEquivalent) > double.Epsilon
                            ? axisConfig.PulseEquivalent
                            : 1d;
                        // 固高反馈速度单位为 pulse/ms，AxisView 显示单位为 mm/s。
                        var speed = Math.Round(tmpAllSpeedInfos[sourceIndex] * 1000d / pulseEquivalent, 2);
                        axisConfig.CurSpeed = speed;

                        var axisIndex = axisConfig.AxisNo - 1;
                        if (axisIndex >= 0 && axisIndex < CurSpeed.Length)
                        {
                            CurSpeed[axisIndex] = speed;
                        }
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetAllSpeedInfos()_获取所有轴的实际速度失败{ex.StackTrace}");
                return false;
            }
        }

        public override bool GetAllSpeedInfos(ref double[] allSpeedInfos, short core = 2)
        {
            if (!GetAllSpeedInfos(core))
            {
                return false;
            }

            for (var i = 0; i < allSpeedInfos.Length && i < Config.AllAxis.Count; i++)
            {
                allSpeedInfos[i] = Config.AllAxis[i].CurSpeed;
            }

            return true;
        }

        #endregion

    }
}
