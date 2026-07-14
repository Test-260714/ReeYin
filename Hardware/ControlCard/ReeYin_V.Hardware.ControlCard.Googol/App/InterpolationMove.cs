using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.ControlCard.Models;
using ReeYin_V.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ReeYin_V.Hardware.ControlCard.Googol.App
{
    /// <summary>
    /// 必要的插补设置
    /// </summary>
    public partial class GoogolControlCard : ControlCardBase
    {

        #region 插补
        /// <summary>
        /// 建立XY插补坐标系
        /// </summary>
        /// <param name="csid">坐标系号1~8</param>
        /// <param name="xAxisId">X轴</param>
        /// <param name="yAxisId">Y轴</param>
        /// <returns></returns>
        public bool CrdXYSetPrm(short csid, En_AxisNum xAxisId, En_AxisNum yAxisId)
        {
            foreach (var core in Cores)
            {
                if (!Motion.CrdXYSetPrm(csid, ConvertAxis(xAxisId).AxisNo, ConvertAxis(yAxisId).AxisNo,
                    Config.DefaultInterpCS.MaxSpeed * Config.DefaultInterpCS.PulseEquivalent,
                    Config.DefaultInterpCS.AccSpeed * Config.DefaultInterpCS.PulseEquivalent, 50, core))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 建立XYZ插补坐标系
        /// </summary>
        /// <param name="csid">坐标系号1~8</param>
        /// <param name="xAxisId">X轴</param>
        /// <param name="yAxisId">Y轴</param>
        /// <param name="zAxisId">Z轴</param>
        /// <returns></returns>
        public bool CrdXYZSetPrm(short csid, En_AxisNum xAxisId, En_AxisNum yAxisId, En_AxisNum zAxisId)
        {
            foreach (var core in Cores)
            {
                if(!Motion.CrdXYZSetPrm(csid, ConvertAxis(xAxisId).AxisNo, ConvertAxis(yAxisId).AxisNo, ConvertAxis(zAxisId).AxisNo, 
                    Config.DefaultInterpCS.MaxSpeed * Config.DefaultInterpCS.PulseEquivalent, 
                    Config.DefaultInterpCS.AccSpeed * Config.DefaultInterpCS.PulseEquivalent, 50,core))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 建立XYZR插补坐标系
        /// </summary>
        /// <param name="csid">坐标系号1~8</param>
        /// <param name="xAxisId">X轴</param>
        /// <param name="yAxisId">Y轴</param>
        /// <param name="zAxisId">Z轴</param>
        /// <param name="rAxisId">R轴</param>
        /// <returns></returns>
        public bool CrdXYZRSetPrm(short csid, En_AxisNum xAxisId, En_AxisNum yAxisId, En_AxisNum zAxisId, En_AxisNum rAxisId)
        {
            foreach (var core in Cores)
            {
                if (!Motion.CrdXYZRSetPrm(csid, ConvertAxis(xAxisId).AxisNo, ConvertAxis(yAxisId).AxisNo, ConvertAxis(zAxisId).AxisNo, ConvertAxis(rAxisId).AxisNo,
                    Config.DefaultInterpCS.MaxSpeed * Config.DefaultInterpCS.PulseEquivalent,
                    Config.DefaultInterpCS.AccSpeed * Config.DefaultInterpCS.PulseEquivalent, 50, core))
                    return false;
            }
            return true;
            //return Motion.CrdXYZRSetPrm(csid, ConvertAxis(xAxisId).AxisNo, ConvertAxis(yAxisId).AxisNo, ConvertAxis(zAxisId).AxisNo, ConvertAxis(rAxisId).AxisNo, 
            //    velMax, accMax);
        }

        /// <summary>
        /// 初始化插补前瞻缓存区
        /// </summary>
        /// <param name="csid"></param>
        /// <returns></returns>
        public bool InitLookAhead(short csid)
        {
            foreach (var core in Cores)
            {
                if (!Motion.InitLookAhead(csid, 5, Config.DefaultInterpCS.AccSpeed * Config.DefaultInterpCS.PulseEquivalent, 200, 0, core))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 清除插补缓存区
        /// </summary>
        /// <param name="csid"></param>
        /// <returns></returns>
        public bool CrdBufClear(short csid)
        {
            foreach (var core in Cores)
            {
                if (!Motion.CrdBufClear(csid, 0, core))
                    return false;

            }
            return true;
        }

        /// <summary>
        /// 将插补指令压入前瞻缓存区
        /// </summary>
        /// <param name="csid"></param>
        /// <returns></returns>
        public bool CrdData(short csid)
        {
            foreach (var core in Cores)
            {
                if (!Motion.CrdData(csid, 0, core))
                {
                    Console.WriteLine($"内核{core}执行CrdData()失败！");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 插补运动开始
        /// </summary>
        /// <param name="csid"></param>
        /// <param name="waitforend"></param>
        /// <returns></returns>
        public bool CrdMoveStart(short csid, bool waitforend)
        {
            CleanAlarm();
            int coreCount = Cores.Length;
            var barrier = new Barrier(coreCount);

            var tasks = Cores.Select(core =>
                Task.Run(() =>
                {
                    // 等待所有 core 就绪
                    barrier.SignalAndWait();

                    // 同时启动
                    return Motion.CrdMoveStart(csid, waitforend, core);
                })
            ).ToArray();

            if (waitforend)
            {
                Task.WaitAll(tasks);
                return tasks.All(t => t.Result);
            }
            return true;
        }


        public bool CrdMoveResume(short csid)
        {
            CleanAlarm();
            foreach (var core in Cores)
            {
                if(!Motion.CrdMoveResume(csid))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 刀向跟随功能
        /// </summary>
        /// <param name="csid"></param>
        /// <param name="position"></param>
        /// <param name="speedType"></param>
        /// <returns></returns>
        public bool CrdRMove(short csid, double position, EN_SpeedType speedType = EN_SpeedType.Mid)
        {
            return Motion.CrdBufMove(csid, ConvertAxis(En_AxisNum.R).AxisNo, (int)ConvertToPluse(En_AxisNum.R, position), ConvertToPluse(En_AxisNum.R, GetSpeed(En_AxisNum.R, speedType)), 0);
        }
        #endregion

        #region 其他设置

        public override bool BufDelay(ushort time)
        {
            return CrdBufDelay(time);
        }

        public override bool BufIO(ushort doMask, ushort doValue)
        {
            return CrdBufIO(doMask, doValue);
        }

        /// <summary>
        /// 延时操作
        /// </summary>
        /// <param name="crd"></param>
        /// <param name="dTime"></param>
        /// <param name="fifo"></param>
        /// <returns></returns>
        public bool CrdBufDelay(ushort dTime, short fifo = 0)
        {
            foreach (var core in Cores)
            {
                if (!Motion.CrdBufDelay(Coordinate, dTime, fifo,core))
                {
                    Console.WriteLine($"内核{core}CrdBufDelay()失败！");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 缓存区内数字量 IO 输出设置指令
        /// </summary>
        /// <param name="crd"></param>
        /// <param name="doMask"></param>
        /// <param name="doValue"></param>
        /// <param name="fifo"></param>
        /// <returns></returns>
        public bool CrdBufIO(ushort doMask, ushort doValue, short fifo = 0)
        {
            foreach (var core in Cores)
            {
                if (!Motion.CrdBufIO(Coordinate, doMask, doValue,fifo,core))
                {
                    Console.WriteLine($"内核{core}CrdBufIO()失败！");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 查询剩余空间
        /// </summary>
        /// <returns></returns>
        public override int QuerySpace(short crd)
        {
            int OrderNum = -1;
            if (!Motion.CrdBufSpace(crd, ref OrderNum))
            {
                return -1;
            }
            return OrderNum;
        }
        #endregion

        #region 自定义插补运动

        /// <summary>
        /// 高优先级轴先移动到安全位置
        /// </summary>
        private void MoveOtherAxesToSafePosition(List<En_AxisNum> axesGroup)
        {
            var HighPriorityAxis = Config.AllAxis.Where(p => (p.Priority == En_Priority.Top || p.Priority == En_Priority.High)).ToList();
            foreach (var axis in HighPriorityAxis)
            {
                if (!axesGroup.Contains(axis.AxisNum)) break;
                MoveAbsoluteAxis(axis.AxisNum, ConvertAxis(axis.AxisNum).SafetyDis, true);
            }
            WaitAxisMoveEnd();
        }

        public override bool CustomInterpolationMoving(CustomInterPoParam param, Func<string> ConstomOrder, bool waitend = true)
        {
            try
            {
                // 检查是否需要复位
                if (IsNeedReset)
                {
                    Console.WriteLine("请先复位后，再移动轴");
                    return false;
                }

                SetSpeedAll(EN_SpeedType.Work);

                // 优先级高的轴先移动至安全位置
                //MoveOtherAxesToSafePosition(param.InterPoAxiss);

                if (param.TargetPosDic != null)
                {
                    SetSpeedAll(EN_SpeedType.Low);

                    foreach (var axis in param.TargetPosDic)
                    {
                        if (axis.Key == En_AxisNum.X || axis.Key == En_AxisNum.Y) continue;
                        //移动单个轴到目标位置
                        MoveAbsoluteAxis(axis.Key, axis.Value);
                    }

                    WaitALLAxisMoveEnd();
                }


                // 初始化坐标系参数
                if (param.InterPoAxiss.Count == 2)
                {
                    if (!CrdXYSetPrm(Coordinate, param.InterPoAxiss[0], param.InterPoAxiss[1]))
                        return false;
                }
                else if (param.InterPoAxiss.Count == 3)
                {
                    if (!CrdXYZSetPrm(Coordinate, param.InterPoAxiss[0], param.InterPoAxiss[1], param.InterPoAxiss[2]))
                        return false;
                }
                else if (param.InterPoAxiss.Count == 4)
                {
                    if (!CrdXYZRSetPrm(Coordinate, param.InterPoAxiss[0], param.InterPoAxiss[1], param.InterPoAxiss[2], param.InterPoAxiss[3]))
                        return false;
                }

                // 获取当前位置
                //float[] curPos = new float[3];
                //var tempInfos = new double[3];
                //if (!GetAllPosInfos())
                //{
                //    return false;
                //}
                //for (int i = 0; i < Math.Min(curPos.Length, tempInfos.Length); i++)
                //{
                //    curPos[i] = (float)tempInfos[i]; // 显式转换double到float
                //}

                // 清除缓冲区并初始化前瞻
                if (!CrdBufClear(Coordinate) || !InitLookAhead(Coordinate))
                    return false;

                #region 执行自定义指令

                if (ConstomOrder() != "OK")
                {
                    Console.WriteLine("自定义插补指令错误");
                }

                #endregion

                // 压入数据，开始执行
                if (CrdData(Coordinate) && CrdMoveStart(Coordinate, true))
                {

                    return true;
                }
                else
                {
                    return false;
                }



            }
            catch (Exception ex)
            {
                Console.WriteLine($"CustomInterpolationMoving()_异常信息如下：{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// 压入新指令到缓存区
        /// </summary>
        /// <returns></returns>
        public override bool PushOrder(Func<string> ConstomOrder)
        {
            try
            {
                #region 执行自定义指令
                //if (ConstomOrder() != "OK")
                //{
                //    Console.WriteLine("自定义插补指令错误");
                //}

                #endregion
                // 压入数据
                if (CrdData(Coordinate))
                {
                    Console.WriteLine("PushOrder()_已经执行！");
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex )
            {
                Logs.LogError(ex.StackTrace.ToString());
                return false;
            }
        }
        #endregion

    }
}
