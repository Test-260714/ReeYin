using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.ControlCard.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Hardware.ControlCard.Googol.App
{
    public partial class GoogolControlCard : ControlCardBase
    {
        ///// <summary>
        ///// 获取当前报警
        ///// </summary>
        ///// <param name="alarms"></param>
        ///// <exception cref="NotImplementedException"></exception>
        //public void GetCurAlarm(ref List<AlarmInfoMessage> alarms)
        //{
        //    if (!IsConnected)
        //    {
        //        alarms.Add(new AlarmInfoMessage()
        //        {
        //            Module = Define.EN_Module.GENMotion,
        //            ErrMsg = "[固高] 连接断开" + $" {DevIdx + 1}",
        //            Time = DateTime.Now
        //        });
        //        for (int i = 0; i < _ctrlSts.Length; i++)
        //        {
        //            for (int j = 0; j < 12; j++)
        //            {
        //                if ((_ctrlSts[i] & 0x01 << j) > 0x00)
        //                {
        //                    if (0x01 << j == (int)En_GetAxisClrSts.Bit1_DriverAlarm)
        //                    {
        //                        alarms.Add(new AlarmInfoMessage()
        //                        {
        //                            Module = Define.EN_Module.GENMotion,
        //                            ErrMsg = $"[固高]第{i}号轴驱动器报警",
        //                            Time = DateTime.Now
        //                        });
        //                    }
        //                    else if (0x01 << j == (int)En_GetAxisClrSts.Bit4_FollowOverLimited)
        //                    {
        //                        alarms.Add(new AlarmInfoMessage()
        //                        {
        //                            Module = Define.EN_Module.GENMotion,
        //                            ErrMsg = $"[固高]第{i}号轴跟随误差越限",
        //                            Time = DateTime.Now
        //                        });
        //                    }
        //                    else if (0x01 << j == (int)En_GetAxisClrSts.Bit5_PositiveLimitTriggered)
        //                    {
        //                        alarms.Add(new AlarmInfoMessage()
        //                        {
        //                            Module = Define.EN_Module.GENMotion,
        //                            ErrMsg = $"[固高]第{i}号轴正限位触发",
        //                            Time = DateTime.Now
        //                        });
        //                    }
        //                    else if (0x01 << j == (int)En_GetAxisClrSts.Bit6_NegtiveLimitTriggered)
        //                    {
        //                        //负限位触发标志
        //                        alarms.Add(new AlarmInfoMessage()
        //                        {
        //                            Module = Define.EN_Module.GENMotion,
        //                            ErrMsg = $"[固高]第{i}号轴负限位触发",
        //                            Time = DateTime.Now
        //                        });
        //                    }
        //                    else if (0x01 << j == (int)En_GetAxisClrSts.Bit7_IOSmoothStopTriggered)
        //                    {
        //                        //IO 平滑停止触发标志
        //                        alarms.Add(new AlarmInfoMessage()
        //                        {
        //                            Module = Define.EN_Module.GENMotion,
        //                            ErrMsg = $"[固高]第{i}号轴IO 平滑停止触发",
        //                            Time = DateTime.Now
        //                        });
        //                    }
        //                    else if (0x01 << j == (int)En_GetAxisClrSts.Bit8_EmergencyStopTriggered)
        //                    {
        //                        //急停触发标志
        //                        alarms.Add(new AlarmInfoMessage()
        //                        {
        //                            Module = Define.EN_Module.GENMotion,
        //                            ErrMsg = $"[固高]第{i}号轴急停触发",
        //                            Time = DateTime.Now
        //                        });
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    else
        //    {
        //        if (IsNeedReset)
        //        {
        //            alarms.Add(new AlarmInfoMessage()
        //            {
        //                Module = Define.EN_Module.GENMotion,
        //                ErrMsg = "[固高] 需要复位" + $" {DevIdx + 1}",
        //                Time = DateTime.Now
        //            });
        //        }
        //    }
        //}

        private double[] GetSpeed(En_AxisNum axis, EN_SpeedType speed)
        {

            double[] rtnSpeed = 
                [ConvertAxis(axis).SpeedDict1.Where(p => p.SpeedType == speed).FirstOrDefault().StartSpeed,
                ConvertAxis(axis).SpeedDict1.Where(p => p.SpeedType == speed).FirstOrDefault().MaxSpeed,
                ConvertAxis(axis).SpeedDict1.Where(p => p.SpeedType == speed).FirstOrDefault().AccSpeed,];
            return rtnSpeed;
        }

        /// <summary>
        /// 获取电机状态  false没信号标志位为0  ture有信号标志位为1
        /// </summary>
        /// <param name="axisNum">轴号从1开始</param>
        /// <param name="clrSts">需要获取的信息</param>
        /// <returns></returns>
        public bool GetAxisClrSts(short axisNum, int count, En_GetAxisClrSts clrSts)
        {
            int[] sts = new int[count];
            foreach (var core in Cores)
            {
                if (!Motion.AxisGetSts(axisNum, ref sts, count,core))
                {
                    return false;
                }
            }

            // 解析并获取指定范围内的轴参数，过滤掉可能的null值
            var allAxisParam = Enumerable.Range(axisNum, count)
                .Select(i => Config.AllAxis.FirstOrDefault(m => m.AxisNo == i))
                .Where(axis => axis != null)
                .ToList();

            // 为每个轴参数设置状态值（确保索引不越界）
            for (int i = 0; i < Math.Min(allAxisParam.Count, sts.Length); i++)
            {
                allAxisParam[i].AxisStatus = sts[i];

                //Console.WriteLine(allAxisParam[i].GetStatusDescriptions(allAxisParam[i].AxisStatus));
                
            }

            return !sts.All(item => (item & (int)clrSts) <= 0);//所有轴状态恢复，才会返回false
        }

        #region 判断轴是否静止
        /// <summary>
        /// 判断轴是否静止  true静止  false在运动
        /// </summary>
        /// <param name="axis">轴号从1开始</param>
        /// <returns></returns>
        public bool IsAxisStop(En_AxisNum axis)
        {
            return !GetAxisClrSts(ConvertAxis(axis).AxisNo, 1, En_GetAxisClrSts.Bit10_MoveEnabled);
        }

        /// <summary>
        /// 判断多个轴是否都静止  true都静止  false有轴在运动
        /// </summary>
        /// <param name="axis">轴号从1开始</param>
        /// <returns></returns>
        public bool IsAxisStop(En_AxisNum[] axes)
        {
            foreach (En_AxisNum axis in axes)
            {
                if (!IsAxisStop(axis))
                {
                    return false; // 发现有轴在运动，立即返回false
                }
            }
            // 所有轴都静止
            return true;
        }

        /// <summary>
        /// 判断所有轴是否都静止 true都静止  false有轴在运动
        /// </summary>
        /// <returns></returns>
        public bool IsAxisStop()
        {
            return !GetAxisClrSts(1, Config.AllAxis.Count, En_GetAxisClrSts.Bit10_MoveEnabled);
        }

        public double SafetyZ()
        {
            return ConvertAxis(En_AxisNum.Z).SafetyDis;
        }

        public double SafetyZ(En_AxisNum axisNum)
        {
            return ConvertAxis(axisNum).SafetyDis;
        }

        public float HeightZ()
        {
            return Config.Height_Z;
        }

        public float HeightContentZ()
        {
            return Config.Height_Content_Z;
        }
        #endregion
    }
}
