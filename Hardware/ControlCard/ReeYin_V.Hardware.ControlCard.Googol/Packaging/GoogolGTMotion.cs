using DryIoc.ImTools;
using GoogolMotion.Models;
using GTN;
using ReeYin_V.Hardware.ControlCard;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace GoogolMotion
{
    public enum EN_StopType
    {
        Emergency = 0,
        Smooth,
    }

    public enum EN_AxisStatus
    {
        Reserved0 = 0,               //Bit0 保留
        ServoAlarm = 1,              //Bit1 驱动器报警标志 控制轴连接的驱动器报警时置 1
        Reserved1 = 2,               //Bit2 保留
        Reserved2 = 3,               //Bit3 保留
        FollowOverLimit = 4,         //Bit4 跟随误差越限标志 控制轴规划位置和实际位置的误差大于设定极限时置 1 
        PLimitTrigger = 5,           //Bit5 正限位触发标志 正限位开关电平状态为限位触发电平时置 1规划位置大于正向软限位时置 1
        NLimitTrigger = 6,           //Bit6 负限位触发标志 负限位开关电平状态为限位触发电平时置 1规划位置小于负向软限位时置 1
        SmoothStopTrigger = 7,       //Bit7 IO 平滑停止触发标志 如果轴设置了平滑停止 IO，当其输入为触发电平时置 1，并自动平滑停止该轴
        EmergencyStopTrigger = 8,    //Bit8 IO 急停触发标志 如果轴设置了急停 IO，当其输入为触发电平时置 1，并自动急停该轴
        MotorEnable = 9,             //Bit9 电机使能标志 电机使能时置 1
        MotorPrfInPlace = 10,        //Bit10 规划运动标志 规划器运动时置 1
        MotorInPlace = 11            //Bit11 电机到位标志 规划器静止，规划位置和实际位置的误差小于设定误差带，并且在误差带内保持设定时间后，置起到位标志
    }

    /// <summary>
    /// 固高运动控制底层库
    /// 传入的速度为pul/s 轴号从1开始 索引从0开始
    /// </summary>
    public partial class GoogolGTMotion
    {
        private short _axisCount = 5;                       //系统总共有多少轴
        private short _ioModuleCount = 0;                   //系统一共多少个总线IO模块(非拓展模块)
        private short _core = 2;                            //内核
        private short[] _cores = [1,2];                       //内核们
        private uint _uiClock;                              //控制器时钟
        private int _moveTimeOut = 60000;	                //运动超时时间
        private bool[] _isExits = new bool[64];             //该轴是否立马退出
        private bool[] _isCrdExits = new bool[8];           //该坐标系是否立马退出

        private bool needConnectReset = true;               //连接后是否需要先复位，仅连接后需要判断

        /// <summary>
        /// 创建对象
        /// </summary>
        /// <param name="coreIndex">内核编号从1开始</param>
        /// <param name="timeOut">运动超时时间</param>
        public GoogolGTMotion(short coreIndex = 1, int timeOut = 60000)
        {
            _core = coreIndex;
            _moveTimeOut = timeOut;
        }
        /// <summary>
        /// 获取轴的数量
        /// </summary>
        /// <returns></returns>
        public short GetAxesCount()
        {
            return _axisCount;
        }
        /// <summary>
        /// 获取IO模块的数量
        /// </summary>
        /// <returns></returns>
        public short GetIOCount()
        {
            return _ioModuleCount;
        }
        /// <summary>
        /// 获取板卡系统时钟
        /// </summary>
        /// <returns></returns>
        public uint GetClock()
        {
            return _uiClock;
        }
        object obj = new object();
        /// <summary>
        /// 轴退出等待循环
        /// </summary>
        /// <param name="axisId">轴号从1开始</param>
        /// <param name="count">轴的数量</param> 
        /// <returns></returns>
        private bool Exit(short axisId, int count,short core = 2)
        {
            if (axisId > 64)
            {
                ErrMessage("轴传入的数量超过64个!");
                return false;
            }

            for (int i = 0; i < count; i++)
            {
                _isExits[axisId - 1 + i] = true;
            }
            return true;
            
        }
        /// <summary>
        /// 坐标系退出
        /// </summary>
        /// <param name="crd">坐标系号从1开始</param>
        /// <returns></returns>
        private bool ExitCrd(short crd,short core = 2)
        {
            if (crd > 8)
            {
                ErrMessage("坐标系传入的数量超过8个!");
                return false;
            }
            _isCrdExits[crd - 1] = true;
            return true;
        }

        /// <summary>
        /// 螺距误差补偿 
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="n"></param>
        /// <param name="startPos"></param>
        /// <param name="lenPos"></param>
        /// <param name="pPositive"></param>
        /// <param name="pNegative"></param>
        /// <returns></returns>
        public bool CompensateAxis(short axis, short n, int startPos, int lenPos, ref int[] pPositive, ref int[] pNegative,short core = 2)
        {
            short ret = -1;
            ret = mc.GTN_SetLeadScrewComp(core, axis, n, startPos, lenPos, ref pPositive[0], ref pNegative[0]);
            ret = mc.GTN_EnableLeadScrewComp(core, axis, 1);
            return true;
        }

        /// <summary>
        /// 编码器与规划器绑定
        /// </summary>
        /// <param name="axisId"></param>
        /// <returns></returns>
        public bool SetPrf(short axisId, short core = 2)
        {
            int encpos = 0;
            short sRtn;
            switch (core)
            {
                //核1是glink通信
                case 1:
                    {
                        double[] encpos1 = new double[4];
                        //获取编码器位置，核1没有编码器获取核2的
                        sRtn = mc.GTN_GetEncPos(2, axisId, out encpos1[0], (short)encpos1.Length, out _uiClock);
                        if (encpos > 10)
                        {
                            needConnectReset = false;
                        }
                        //设置规划器位置与编码器位置一致
                        sRtn = mc.GTN_SetPrfPos(core, axisId, (int)encpos1[0]);
                        if (sRtn != 0)
                        {
                            ErrMessage(sRtn);
                            return false;
                        }
                        int mask1 = 1 << (axisId - 1);
                        sRtn = mc.GTN_SynchAxisPos(core, mask1);
                        if (sRtn != 0)
                        {
                            ErrMessage(sRtn);
                            return false;
                        }

                        return true;
                    }
                //核2是EtherCAT通信
                case 2:
                    {
                        //获取编码器位置
                        if (!GetEcatEncPos(core, axisId, ref encpos))
                        {
                            return false;
                        }

                        if (encpos > 10)
                        {
                            needConnectReset = false;
                        }
                        //设置规划器位置与编码器位置一致
                        sRtn = mc.GTN_SetPrfPos(core, axisId, encpos);
                        if (sRtn != 0)
                        {
                            ErrMessage(sRtn);
                            return false;
                        }
                        int mask = 1 << (axisId - 1);
                        sRtn = mc.GTN_SynchAxisPos(core, mask);
                        if (sRtn != 0)
                        {
                            ErrMessage(sRtn);
                            return false;
                        }

                        return true;
                    }
                default:
                    break;
            }
            return true;
        }

        /// <summary>
        /// 获取连接后是否需要复位,仅用于连接后判断
        /// </summary>
        /// <param name="station"></param>
        /// <returns></returns>
        public bool GetNeedConnectReset()
        {
            return needConnectReset;
        }

        /// <summary>
        /// 打开驱动器使能/失能
        /// </summary>
        /// <param name="axisId">轴号从1开始，0控制所有轴使能 </param>
        /// <param name="isEnabled">True使能 false关闭使能</param>
        /// <returns></returns>
        public bool SetAxisEnabled(short axisId, bool isEnabled, short core = 2)
        {
            short sRtn;
            if (axisId == 0)
            {
                for (short i = 1; i <= _axisCount; i++)
                {
                    sRtn = GTN.mc.GTN_ClrSts(core, i, _axisCount);//清除轴状态
                    sRtn = isEnabled ? mc.GTN_AxisOn(core, i) : mc.GTN_AxisOff(core, i);
                    if (sRtn != 0)
                    {
                        ErrMessage(sRtn);
                        return false;
                    }
                    if (!SetPrf(i,core))
                    {
                        return false;
                    }
                }
            }
            else
            {
                sRtn = isEnabled ? mc.GTN_AxisOn(core, axisId) : mc.GTN_AxisOff(core, axisId);
                if (sRtn != 0)
                {
                    ErrMessage(sRtn);
                    return false;
                }
                if (!SetPrf(axisId))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 获取轴状态
        /// </summary>
        /// <param name="axisId">轴号从1开始</param>
        /// <param name="sts">轴状态</param>
        /// <returns></returns>
        public bool AxisGetSts(short axisId, ref int[] sts, int count, short core = 2)
        {
            lock (obj)
            {
                short sRtn;
                sRtn = mc.GTN_GetSts(core, axisId, out sts[0], (short)count, out _uiClock);
                if (sRtn != 0)
                {
                    ErrMessage(sRtn);
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// 设置软限位
        /// </summary>
        /// <param name="axisId">轴号从1开始</param>
        /// <param name="mode">软限位模式 0超越软限位位置后开始减速停止 1限制在软限位范围之内</param>
        /// <param name="fPositive">正限位</param>
        /// <param name="fNegative">负限位</param>
        /// <returns></returns>
        public bool AxisSetSoftLimit(short axisId, short mode, int fPositive, int fNegative,short core = 2)
        {
            short sRtn;
            sRtn = mc.GTN_SetSoftLimitMode(core, axisId, mode);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            sRtn = mc.GTN_SetSoftLimit(core, axisId, fPositive, fNegative);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            return true;
        }
        /// <summary>
        /// 获取编码器位置
        /// </summary>
        /// <param name="axisId"></param>
        /// <param name="dEncPos"></param>
        /// <returns></returns>
        public bool GetEncPos(short axisId, ref double[] dEncPos, short core = 2)
        {
            short sRtn;
            if (axisId + dEncPos.Length - 1 > _axisCount)
            {
                ErrMessage($"多轴获取超过了轴的数量:{_axisCount}");
                return false;
            }
            sRtn = mc.GTN_GetEncPos(core, axisId, out dEncPos[0], (short)dEncPos.Length, out _uiClock);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            return true;
        }
        /// <summary>
        /// 获取规划器位置
        /// </summary>
        /// <param name="axisId"></param>
        /// <param name="dEncPos"></param>
        /// <returns></returns>
        public bool GetPrfPos(short axisId, ref double[] dEncPos, short core = 2)
        {
            short sRtn;
            if (axisId + dEncPos.Length - 1 > _axisCount)
            {
                ErrMessage($"多轴获取超过了轴的数量:{_axisCount}");
                return false;
            }

            if(core == 1)
            {
                int[] PrfMode = new int[12];//运动模式
                double[] pEncPos = new double[12];
                double[] pPrfPos = new double[12];
                double[] pEncVel = new double[12];
                double[] pPrfVel = new double[12];
                uint pClock; //时钟信号

                sRtn = GTN.mc.GTN_GetPrfMode(core, 1, out PrfMode[0], 8, out pClock);
                sRtn = GTN.mc.GTN_GetPrfPos(core, 1, out pPrfPos[0], 8, out pClock);
                sRtn = GTN.mc.GTN_GetPrfVel(core, 1, out pPrfVel[0], 8, out pClock);
                sRtn = GTN.mc.GTN_GetEncPos(core, 1, out pEncPos[0], 8, out pClock);
                sRtn = GTN.mc.GTN_GetEncVel(core, 1, out pEncVel[0], 8, out pClock);
                sRtn = GTN.mc.GTN_GetPrfMode(core, 1, out PrfMode[8], 4, out pClock);
                sRtn = GTN.mc.GTN_GetPrfPos(core, 9, out pPrfPos[8], 4, out pClock);
                sRtn = GTN.mc.GTN_GetPrfVel(core, 9, out pPrfVel[8], 4, out pClock);
                sRtn = GTN.mc.GTN_GetEncPos(core, 9, out pEncPos[8], 4, out pClock);
                sRtn = GTN.mc.GTN_GetEncVel(core, 9, out pEncVel[8], 4, out pClock);

            }
            sRtn = mc.GTN_GetPrfPos(core, axisId, out dEncPos[0], (short)dEncPos.Length, out _uiClock);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 获取编码器实际速度
        /// </summary>
        /// <param name="axisId">轴号从1开始</param>
        /// <param name="encVel">编码器速度</param>
        /// <returns></returns>
        public bool GetEncVel(short axisId, ref double[] encVel, short core = 2)
        {
            short sRtn;
            if (axisId < 1 || axisId > _axisCount)
            {
                ErrMessage($"轴号:{axisId}超过了轴的数量:{_axisCount}");
                return false;
            }

            if (encVel == null || encVel.Length == 0)
            {
                ErrMessage("编码器速度缓存不能为空!");
                return false;
            }

            if (axisId + encVel.Length - 1 > _axisCount)
            {
                ErrMessage($"多轴获取超过了轴的数量:{_axisCount}");
                return false;
            }

            sRtn = mc.GTN_GetEncVel(core, axisId, out encVel[0], (short)encVel.Length, out _uiClock);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }

            return true;
        }

        /// <summary>
        ///  EtherCAT 轴读取并设置编码位置
        /// </summary>
        /// <param name="axisId">轴号从1开始</param>
        /// <param name="dEncPos">编码器位置</param>
        /// <returns></returns>     
        public bool GetEcatEncPos(short core,short axisId, ref int dEncPos)
        {
            short sRtn;
            if (axisId > _axisCount)
            {
                ErrMessage($"轴号:{axisId}超过了轴的数量:{_axisCount}");
                return false;
            }
            sRtn = mc.GTN_GetEcatEncPos(core, axisId, out dEncPos);

            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            sRtn = mc.GTN_SetEncPos(core, axisId, dEncPos);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            return true;
        }



        #region 轴运动
        /// <summary>
        /// 设置轴的运动速度
        /// </summary>
        /// <param name="axisId">轴号从1开始</param>
        /// <param name="speed">速度</param>
        /// <param name="smoothTime">平滑时间(默认25)</param>
        /// <returns></returns>
        public bool SetMoveSpeed(short axisId, double[] speed, short smoothTime = 25,short core = 2)
        {
            short sRtn;
            //acc: 点位运动的加速度。正数，单位：pulse / ms2。
            //dec: 点位运动的减速度。正数，单位：pulse / ms2。未设置减速度时，默认减速度和加速度
            //相同。
            //velStart：起跳速度。正数，单位：pulse / ms。默认值为 0。
            //smoothTime：平滑时间。正整数，单位 ms。平滑时间的数值越大，加减速过程越平稳。
            //平滑时间取值范围根据控制周期变化，例如：
            //250us 控制周期，平滑时间取值范围为：[0, 50]，单位 ms。
            //500us 控制周期，平滑时间取值范围为：[0, 100]，单位 ms。
            //1ms 控制周期，平滑时间取值范围为：[0, 200]，单位 ms。
            mc.TTrapPrm tSpeed;
            tSpeed.acc = 0; //点位运动的加速度
            tSpeed.dec = 0; //点位运动的减速度
            tSpeed.smoothTime = smoothTime; //平滑时间
            tSpeed.velStart = speed[0] * 0.001;//起跳速度，默认0
            double vel = speed[1] * 0.001; //运行速度
            tSpeed.acc = tSpeed.dec = speed[2] * 0.001 * 0.001;
            int lMode;
            //0：点位运动，控制器上电后默认为该模式；
            //1：Jog 模式；
            //2：PT 模式； 
            //3：电子齿轮模式；
            //4：Follow 模式；
            //5：插补模式；
            //6：Pvt 模式
            sRtn = mc.GTN_GetPrfMode(core, axisId, out lMode, 1, out _uiClock);//读取轴运动模式,默认读取一个轴
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            if (lMode != 0)
            {
                sRtn = mc.GTN_PrfTrap(core, axisId);//点位模式
                if (sRtn != 0)
                {
                    ErrMessage(sRtn);//设置点位模式失败
                    return false;
                }
            }

            if (!Stop(axisId, 1))
            {
                return false;
            }

            sRtn = mc.GTN_SetTrapPrm(core, axisId, ref tSpeed);//设置起跳速度和加减速度
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            //设置目标速度
            sRtn = mc.GTN_SetVel(core, axisId, vel);//pulse/s
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            return true;
        }
        /// <summary>
        /// 清除所有轴状态
        /// </summary>
        /// <returns></returns>
        public bool ClearAxesSts(short core = 2)
        {
            short sRtn;
            sRtn = mc.GTN_ClrSts(core, 1, _axisCount);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            return true;
        }
        /// <summary>
        /// 清除单轴报警
        /// </summary>
        /// <param name="axisId"></param>
        /// <returns></returns>
        public bool ClearAxisSts(short axisId,short core = 2)
        {
            short sRtn;
            sRtn = mc.GTN_ClrSts(core, axisId, 1);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            return true;
        }
        /// <summary>
        /// 单轴绝对运动
        /// </summary>
        /// <param name="axisId">轴号从1开始</param>
        /// <param name="fpos">目标位置</param>
        /// <param name="waitforend">是否需要等待</param>
        /// <returns></returns>
        public bool MoveAbsoluteSingleAxis(short axisId, int fpos, bool waitforend,short core = 2)
        {
            short sRtn;
            int lMode;
            sRtn = mc.GTN_GetPrfMode(core, axisId, out lMode, 1, out _uiClock);//读取轴运动模式,默认读取一个轴
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            //轴运动模式。
            //0：点位运动，控制器上电后默认为该模式；
            //1：Jog 模式		
            //2：PT 模式；
            //3：电子齿轮模式；
            //4：Follow 模式；
            //5：插补模式；
            //6：Pvt 模式
            if (lMode != 0)
            {
                sRtn = mc.GTN_PrfTrap(core, axisId);//点位模式
                if (sRtn != 0)
                {
                    ErrMessage(sRtn);//设置点位模式失败
                    return false;
                }
            }
            sRtn = mc.GTN_SetPos(core, axisId, fpos);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            //启动点位运动或 Jog 运动
            sRtn = mc.GTN_Update(core, 1 << (axisId - 1));
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            if (waitforend)
            {
                

                return WaitAxisMoveEnd(axisId);
            }
            return true;
        }
        /// <summary>
        /// 单轴相对运动
        /// </summary>
        /// <param name="axisId">轴号从1开始</param>
        /// <param name="dis">运动距离</param>
        /// <param name="waitforend">是否需要等待</param>
        /// <returns></returns>
        public bool MoveRelativeSingleAxis(short axisId, int dis, bool waitforend, short core = 2)
        {
            short sRtn;
            double pluse;
            sRtn = mc.GTN_GetPrfPos(core, axisId, out pluse, 1, out uint Clock);
            if (sRtn != 0)
            {
                ErrMessage(sRtn);
                return false;
            }
            int plusedis = dis + (int)pluse;

            return MoveAbsoluteSingleAxis(axisId, plusedis, waitforend,core);
        }
        /// <summary>
        /// 等待单轴运动结束
        /// </summary>
        /// <param name="axisId"></param>
        /// <param name="timeoutMs"></param>
        /// <returns></returns>
        public bool WaitAxisMoveEnd(short axisId, int timeoutMs = 60 * 1000,short core = 2)
        {
            //Bit0 保留
            //Bit1 驱动器报警标志 控制轴连接的驱动器报警时置 1
            //Bit2 保留
            //Bit3 保留
            //Bit4 跟随误差越限标志 控制轴规划位置和实际位置的误差大于设定极限时置 1 
            //Bit5 正限位触发标志 正限位开关电平状态为限位触发电平时置 1规划位置大于正向软限位时置 1
            //Bit6 负限位触发标志 负限位开关电平状态为限位触发电平时置 1规划位置小于负向软限位时置 1
            //Bit7 IO 平滑停止触发标志 如果轴设置了平滑停止 IO，当其输入为触发电平时置 1，并自动平滑停止该轴
            //Bit8 IO 急停触发标志 如果轴设置了急停 IO，当其输入为触发电平时置 1，并自动急停该轴
            //Bit9 电机使能标志 电机使能时置 1
            //Bit10 规划运动标志 规划器运动时置 1
            //Bit11 电机到位标志 规划器静止，规划位置和实际位置的误差小于设定误差带，并且在误差带内保持设定时间后，置起到位标志          
            int jointRunStatus;
            _isExits[axisId - 1] = false;
            DateTime starttime = DateTime.Now;
            while (true)
            {
                if ((DateTime.Now - starttime).TotalMilliseconds > timeoutMs)
                {
                    ErrMessage($"{axisId}轴运动超时退出!");
                    return false;
                }
                jointRunStatus = 0;
                short sRtn = mc.GTN_GetSts(core,axisId, out jointRunStatus, 1, out _uiClock);
                if (sRtn != 0)
                {
                    ErrMessage(sRtn);
                    return false;
                }
                if ((jointRunStatus & 0x400) == 0)
                {
                    //运动停止
                    break;
                }
                if (_isExits[axisId - 1])
                {
                    _isExits[axisId - 1] = false;
                    ErrMessage($"等待单轴运动结束时，{axisId}轴主动退出!");
                    return false;
                }
                Thread.Sleep(1);
            }
            if ((jointRunStatus & 0x400) != 0)
            {
                //还在运动
                int runStatus = (jointRunStatus & 0x400) > 0 ? 1 : 0;
                ErrMessage($"{axisId}轴运动状态:{runStatus}");
                return false;
            }
            if (_isExits[axisId - 1])
            {
                _isExits[axisId - 1] = false;
                ErrMessage($"等待单轴运动结束时，{axisId}轴主动退出!");
                return false;
            }
            return true;
        }
        /// <summary>
        /// 等待多轴运动结束
        /// </summary>
        /// <param name="axisId">轴号从1开始</param>
        /// <param name="count"></param>
        /// <param name="timeoutMs">单位毫秒</param>
        /// <returns></returns>
        public bool WaitAxesMoveEnd(short axisId, short count, int timeoutMs = 60 * 1000,short core = 2)
        {
            if (count <= 0)
            {
                return true;
            }

            if (axisId < 1 || axisId > _axisCount || axisId + count - 1 > _axisCount)
            {
                ErrMessage($"等待轴范围[{axisId},{axisId + count - 1}]超过了轴的数量:{_axisCount}");
                return false;
            }
            int jointRunStatus = 0;
            int[] runsStatus = new int[count];
            _isExits[axisId - 1] = false;
            DateTime starttime = DateTime.Now;
            while (true)
            {
                if ((DateTime.Now - starttime).TotalMilliseconds > timeoutMs)
                {
                    ErrMessage($"{axisId}轴运动超时退出!");
                    return false;
                }
                jointRunStatus = 0;
                short sRtn = mc.GTN_GetSts(core, axisId, out runsStatus[0], count, out _uiClock);
                if (sRtn != 0)
                {
                    ErrMessage(sRtn);
                    return false;
                }
                for (int i = 0; i < count; i++)
                {
                    jointRunStatus |= runsStatus[i];
                }
                if ((jointRunStatus & 0x400) == 0)
                {
                    //运动停止
                    break;
                }
                if (_isExits[axisId - 1])
                {
                    _isExits[axisId - 1] = false;
                    ErrMessage($"等待多轴运动结束，{axisId}轴主动退出!");
                    return false;
                }
                Thread.Sleep(1);
            }
            if ((jointRunStatus & 0x400) != 0)
            {
                //还在运动
                int runStatus = (jointRunStatus & 0x400) > 0 ? 1 : 0;
                ErrMessage($"{axisId}轴运动状态:{runStatus}");
                return false;
            }
            if (_isExits[axisId - 1])
            {
                _isExits[axisId - 1] = false;
                ErrMessage($"等待多轴运动结束，{axisId}轴主动退出!");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 停止轴运动
        /// </summary>
        /// <param name="axisId">轴号从1开始</param>
        /// <param name="count">要停止多少个轴</param>
        /// <returns></returns>
        public bool Stop(short axisId, int count, short core = 2)
        {
            lock (obj)
            {
                short sRtn;
                int mask = 0;
                for (int i = 0; i < count; i++)
                {
                    mask |= 1 << (axisId + i - 1);
                }
                sRtn = mc.GTN_Stop(core, mask, mask);
                if (sRtn != 0)
                {
                    ErrMessage(sRtn);
                    return false;
                }
                Exit(axisId, count);//先停止轴运动再退出等待循环  避免出现一个轴跑到一半 另一个轴同步开始动作
                return true;
            }
        }


        #endregion

        #region Discard

        //public bool Close()
        //{
        //    short sRtn = mc.GTN_Close();
        //    if (sRtn != 0)
        //    {
        //        ErrMessage(sRtn);
        //        return false;
        //    }
        //    return true;
        //}

        /// <summary>
        /// 打开所有轴 驱动器使能/失能
        /// </summary>      
        /// <param name="isEnabled">True使能 false关闭使能</param>
        /// <returns></returns>
        public bool SetAxesEnabled(bool isEnabled,short core = 2)
        {
            short sRtn;
            for (short i = 1; i <= _axisCount; i++)
            {
                sRtn = isEnabled ? mc.GTN_AxisOn(core, i) : mc.GTN_AxisOff(core, i);
                if (sRtn != 0)
                {
                    ErrMessage(sRtn);
                    return false;
                }
                if (!SetPrf( i))
                {
                    return false;
                }
            }
            return true;
        }
        #endregion
    }
}
