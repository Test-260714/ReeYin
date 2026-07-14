using Microsoft.Data.Sqlite;
using ReeYin_V.Core.Services.Project;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GTN.mc;

namespace ReeYin_V.Hardware.ControlCard.Googol.App
{
    /// <summary>
    /// 初始化操作
    /// </summary>
    public partial class GoogolControlCard : ControlCardBase
    {
        //定义错误码-提示映射表（可抽为类常量，便于全局维护）
        private static readonly Dictionary<int, string> _errorMsgMap = new()
        {
            {1, "打开运动控制器失败"},
            {2, "EtherCAT初始化，通讯未完全建立，启动总线通讯失败"},
            {3, "复位运动控制器失败"},
            {4, "下载配置信息到运动控制器失败（调用后需执行 GTN_ClrSts 生效）"},
            {5, "读取EtherCAT总线的在线从站数目失败或数目不对"},
            {6, $"读取EtherCAT总线的在线从站数目不对，当前数目：5"}, // 若5是固定值
            // {6, $"读取EtherCAT总线的在线从站数目不对，当前数目：{动态值}"}, // 若需动态值，可调整为委托
            {7, "清除控制器报警失败"},
            {8, "扩展模块初始化失败"},
            //{9, $"扩展模块连接数目不对，当前数目:{Motion.GetiOCount()}"},
            {10, "编码器与规划器绑定失败"}
        };

        /// <summary>
        /// 连接设备
        /// </summary>
        /// <param name="createNew"></param>
        /// <returns></returns>
        public bool Connect(bool createNew)
        {
            lock (_stcobj)
            {
                try
                {
                    short error = 0;

                    //连接成功 
                    IsConnected = Motion.Init((short)3, 0, ref error);

                    // 匹配错误码，输出提示（不存在的错误码可加默认处理）
                    if (_errorMsgMap.TryGetValue(error, out string errorMsg))
                    {
                        Console.WriteLine(errorMsg);
                        return false;
                    }

                    #region 检测EC连接从站数量和信息(待封装)
                    short sRtn; // 指令返回值变量
                    short sSlaveMotionCnt; // 运动从站个数
                    short sSlaveIOCnt; // IO从站个数
                    TSlaveInfo tSlaveInfoCnt; // 从站信息表
                    TSlaveInfo tSlaveInfo; // 从站信息表
                    short sEcatAxisCnt = 0; // 所有伺服轴的个数

                    // 读取 EtherCAT 总线在线的从站数目
                    sRtn = GTN_GetEcatSlaves(2,out sSlaveMotionCnt, out sSlaveIOCnt);
                    Console.WriteLine($"sSlaveMotionCnt ={sSlaveMotionCnt}\n");
                    Console.WriteLine($"sSlaveIOCnt ={sSlaveIOCnt}\n");

                    sRtn = GTN_GetEcatSlaveInfo(2, -1, out tSlaveInfoCnt);
                    Console.WriteLine("sEcatSlaveCnt =%d\n", tSlaveInfoCnt.slave_cnt);
                    for (short i = 0; i < tSlaveInfoCnt.slave_cnt; i++)
                    {
                        //读取 Ecat 从站信息
                        sRtn = GTN_GetEcatSlaveInfo(2, i, out tSlaveInfo);
                        if (tSlaveInfo.slave_type == 1)
                        {
                            Console.WriteLine($"Station[{i}] => motion station\n");
                            Console.WriteLine($"motion_cnt={tSlaveInfo.motion_cnt}\n");
                        }
                        else
                        {
                            Console.WriteLine($"Station[{i}] => io station\n");
                            Console.WriteLine($"io_nmap={tSlaveInfo.io_nmap}\n");
                            Console.WriteLine($"io_length={tSlaveInfo.io_length}\n");

                        }
                        Console.WriteLine($"io_nmap={tSlaveInfo.io_nmap}\n");
                        Console.WriteLine($"io_length={tSlaveInfo.io_length}\n");
                        Console.WriteLine($"Vendor ID = {tSlaveInfo.Vid}\n");
                        Console.WriteLine($"Product code = {tSlaveInfo.Pid} \n");
                    }
                    #endregion

                    #region EC从站IO映射（待封装）
                    short axisIndex;
                    short slaveIndex;

                    //将第一个伺服轴的DI0（0x60fd的bit16）映射到本地DI1 
                    axisIndex = 0;// 伺服轴索引号（轴号减一）
                    sRtn = GTN_RelateEcatSlaveToMcGpiBit(2, 1, axisIndex, 1, 16, 0);
                    //将第0个从站，IO模块的DI的第0位映射到本地DI2
                    slaveIndex = 0;// 从站号
                    // 0x6000，Pdo偏移2字节，bit0位
                    sRtn = GTN_RelateEcatSlaveToMcGpiBit(2, 2, slaveIndex, 0, 0, 2);
                    //将第0个从站，IO模块的DI的第15位映射到本地DI3
                    slaveIndex = 0;// 从站号
                    // 0x6000，Pdo偏移3字节，bit7位
                    sRtn = GTN_RelateEcatSlaveToMcGpiBit(2, 2, slaveIndex, 0, 7, 3);
                    //将第一个伺服轴的DO0（0x60fe的bit16）映射到本地DO1 
                    axisIndex = 0;// 伺服轴索引号（轴号减一）
                    sRtn = GTN_RelateEcatSlaveToMcGpoBit(2, 1, (ushort)axisIndex, 1, 16, 0);
                    //将第0个从站，IO模块的DO的第0位映射到本地DO2
                    slaveIndex = 0;// 从站号
                    // 0x7000，Pdo偏移0字节，bit0位
                    sRtn = GTN_RelateEcatSlaveToMcGpoBit(2, 2, (ushort)slaveIndex, 0, 0, 0);
                    //将第0个从站，IO模块的DO的第15位映射到本地DO3
                    slaveIndex = 0;// 从站号
                    // 0x7000，Pdo偏移1字节，bit7位
                    sRtn = GTN_RelateEcatSlaveToMcGpoBit(2, 2, (ushort)slaveIndex, 0, 7, 1);
                    #endregion

                    #region EtherCAT 从站 Encoder 映射

                    #endregion

                    #region 螺距补偿
                    //if (workManager.GNWork.Param.Project == ProjectName.ST01_001线式点胶) {
                    //    ////对X方向进行补偿
                    //    int[] XcomPos = { 13, 9, 14, 16, 8 };
                    //    int[] XcomNeg = { -3, -7, -2, 1, 8 };

                    //    if (!qMotion.AxisOffset(1, 5, 90000, 450000, ref XcomPos, ref XcomNeg))
                    //    {

                    //    }

                    //    ////对Y方向进行补偿
                    //    int[] YcomPos = { -180, -88, 148, 300, 40 };
                    //    int[] YcomNeg = { -180, -132, 88, 273, 40 };
                    //    if (!qMotion.AxisOffset(2, 5, 140000, 700000, ref YcomPos, ref YcomNeg))
                    //    {

                    //    }
                    //} else if (workManager.GNWork.Param.Project == ProjectName.ST01_001线式贴合) {
                    //    int[] XcomPos = { 12, 4, 12, 14, 11 };
                    //    int[] XcomNeg = { 0, -7, 1, 0, 11 };
                    //    int[] YcomPos = { 4, -8, -14, -20, -17, -26, -23, -26, -25, -36,
                    //    -36,-32,-35,-27,-36,-29, -29, -25, -22, -23,
                    //        -14, -10, 0, -1, 8, 10, 16, 23, 25, 30,
                    //    29, 34, 34, 38, 43, 41, 39,34, 32, 36,
                    //    35, 28, 28, 27, 29, 35, 35, 39, 42, 51,
                    //    53, 65, 70, 75, 82, 90, 100, 111, 121, 127,
                    //    136, 142, 152, 161, 164};
                    //    int[] YcomNeg = { -8,-16, -30, -40, -42, -45, -47, -52, -50, -53, -51,
                    //        -60, -58, -58, -53, -61, -61, -53, -53, -46, -49,
                    //        -42, -34, -26, -26, -20, -13, -11, -2, -1, 4,
                    //        5, 12, 14, 13, 18, 12, 14, 4, 5, 2,
                    //        6, 1, 3, 1, 1, 10, 5, 12, 12, 24,
                    //        27, 35, 45, 49, 55, 62, 74, 86, 96, 104,
                    //        110, 120, 127, 137, 164
                    //    };
                    //    qMotion.AxisOffset(1, 5, 72000, 360000, ref XcomPos, ref XcomNeg);
                    //    qMotion.AxisOffset(2, 65, 72000, 640000, ref YcomPos, ref YcomNeg);

                    //}
                    #endregion

                    #region 使能所有轴
                    if (!Motion.SetAxisEnabled(0, true))
                    {
                        return false;
                    }
                    else
                    {
                        //将所有轴使能状态置为true
                        foreach (var axis in Config.AllAxis)
                        {
                            axis.IsEnable = true;
                        }
                    }

                    //核1使能所有轴（无法同时使能核1和核2的同一个轴）
                    if (!Motion.SetAxisEnabled(0, true, 1))
                    {
                        return false;
                    }

                    if (!ExistAlarmAxisClrSts())
                    {
                        return false;
                    }
                    Console.WriteLine("控制器伺服使能成功！");
                    #endregion

                    //设置误差带                 

                    //设置软限位
                    if (!SetSoftLimit())
                    {
                        return false;
                    }

                    if (!SetResetSpeed())
                    {
                        Console.WriteLine("设置回原点速度失败");
                        return false;
                    }

                    //MappingSingleDout(3);

                    SetSpeedAll(EN_SpeedType.Mid);

                    //Status.IsNeedReset = qMotion.GetNeedConnectReset(); 
                    //每次重启软件，直接强制要求复位。这里代码注销，等有好的办法再用

                    //灯柱状态控制

                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"连接控制卡异常，信息为：{ex.StackTrace}");
                    return false;
                }
            }
        }


        /// <summary>
        /// 断开连接
        /// </summary>
        /// <returns></returns>
        public bool DisConnect()
        {
            Motion.SetAxisEnabled(0, false);
            Motion.SetAxisEnabled(0, false,1);

            //_lampTimer?.Stop();
            //_lampTimer?.Dispose();
            //_handleBuzzerTask.Wait();
            //_handleLightsTask.Wait();
            foreach (var core in Cores)
            {
                if (!Motion.Close())
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 初始化IO映射表
        /// </summary>
        public void InitIOMapping()
        {
            Task.Factory.StartNew(() =>
            {
                //SQLiteDataReader reader;
                //if (!SQLiteHelper.ScalarCustom($"SELECT \r\n    M.FuntionName,\r\n    G.GlinkNum,\r\n  G.IOType,\r\n   G.Port\r\nFROM \r\n    MappingIO AS M\r\nINNER JOIN \r\n    GoogolsGNIO AS G ON M.Mappingid = G.Id;", out reader))
                //{
                //    return;
                //}
                //while (reader.Read())
                //{
                //    IOModels.Add(reader["FuntionName"] as string, new IOModel((int)(long)reader["GlinkNum"], (int)(long)reader["Port"], (string)reader["IOType"]));
                //}
            });
        }

        /// <summary>
        /// 查询所有电机是否有异常信息
        /// </summary>
        /// <returns></returns>
        public bool ExistAlarmAxisClrSts()
        {
            // 初始化状态数组
            int[] axisStatuses = new int[Config.AllAxis.Count];

            // 获取轴状态，失败直接返回false
            foreach (var core in Cores)
            {
                if (!Motion.AxisGetSts(1, ref axisStatuses, Config.AllAxis.Count,core))
                {
                    return false;
                }
            }

            // 需要检查的报警状态掩码（将所有需要关注的位组合起来）
            int alarmStatusMask =
                (int)En_GetAxisClrSts.Bit1_DriverAlarm |
                (int)En_GetAxisClrSts.Bit4_FollowOverLimited |
                (int)En_GetAxisClrSts.Bit5_PositiveLimitTriggered |
                (int)En_GetAxisClrSts.Bit6_NegtiveLimitTriggered |
                (int)En_GetAxisClrSts.Bit7_IOSmoothStopTriggered |
                (int)En_GetAxisClrSts.Bit8_EmergencyStopTriggered;

            // 检查每个轴的状态
            foreach (int status in axisStatuses)
            {
                // 如果存在任何报警状态，返回false
                if ((status & alarmStatusMask) != 0)
                {
                    return false;
                }
            }

            // 所有轴都没有报警状态，返回true
            return true;
        }

        /// <summary>
        /// 软限位设置
        /// </summary>
        /// <returns></returns>
        public bool SetSoftLimit()
        {
            foreach (var axis in Config.AllAxis)
            {
                foreach(var core in Cores)
                {
                    if (!Motion.AxisSetSoftLimit(axis.AxisNo, 1, Convert.ToInt32(axis.SoftLimitPositive * axis.PulseEquivalent), Convert.ToInt32(axis.SoftLimitNegative * axis.PulseEquivalent), core))
                    {
                        Console.WriteLine($"核{core}" + axis.AxisNum + "软限位设置失败");
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 设置回原点速度
        /// </summary>
        /// <param name="station"></param>
        /// <returns></returns>
        public bool SetResetSpeed()
        {
            foreach (var axis in Config.AllAxis)
            {
                double[] ResetSpeed = 
                [
                    axis.SpeedDict1.FirstOrDefault(p=>p.SpeedType == EN_SpeedType.Reset).StartSpeed,
                    axis.SpeedDict1.FirstOrDefault(p=>p.SpeedType == EN_SpeedType.Reset).MaxSpeed,
                    axis.SpeedDict1.FirstOrDefault(p=>p.SpeedType == EN_SpeedType.Reset).AccSpeed
                ];

                double[] pluses = new double[ResetSpeed.Length];
                for (int j = 0; j < ResetSpeed.Length; j++)
                {
                    pluses[j] = ResetSpeed[j] * axis.PulseEquivalent;
                }
                Motion.SetGoHomeSpeed(axis.AxisNo, pluses);
            }

            //for (int i = 0; i < Config.AxisModels.Count; i++)
            //{
            //    var axis = Config.AxisModels[i];
            //    double[] pluses = new double[axis.ResetSpeed.Length];
            //    for (int j = 0; j < axis.ResetSpeed.Length; j++)
            //    {
            //        pluses[j] = axis.ResetSpeed[j] * axis.Pulse;
            //    }
            //    Motion.SetGoHomeSpeed(axis.AxisNo, pluses);
            //}
            return true;
        }

    }
}
