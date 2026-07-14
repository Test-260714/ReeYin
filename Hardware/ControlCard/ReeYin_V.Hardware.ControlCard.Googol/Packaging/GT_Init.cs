using GTN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GoogolMotion
{
    /// <summary>
    /// 初始化
    /// </summary>
    public partial class GoogolGTMotion
    {
        #region GTS_EC_Init
        /// <summary>
        /// 初始化总线通讯
        /// </summary>
        /// <returns></returns>
        //public short EtherCATInit(short core)
        //{
        //    // 指令返回值变量
        //    short sRtn = -999;
        //    short sEcatSts;
        //    short sTime = 0;
        //    // 初始化EtherCAT总线
        //    sRtn = mc.GTN_InitEcatComm(core);
        //    if (sRtn != 0)
        //    {
        //        ErrMessage(sRtn);
        //        return sRtn;
        //    }
        //    do
        //    {
        //        // 读取EtherCAT总线状态
        //        sRtn = mc.GTN_IsEcatReady(core, out sEcatSts);
        //        Thread.Sleep(10);
        //        sTime++;
        //    } while (sEcatSts != 1 && sTime <= 1500);
        //    // 启动EtherCAT通讯
        //    sRtn = mc.GTN_StartEcatComm(core);
        //    return sRtn;
        //}

        #endregion

        /// <summary>
        /// 初始化板卡
        /// </summary>
        /// <param name="axisNum">轴的数量</param>
        /// <param name="etherCATIONum">总线IO卡的数量</param>
        /// <param name="errCode">错误代码</param>
        /// <returns></returns>
        public bool Init(short axisNum, int etherCATIONum, ref short errCode)
        {
            // 指令返回值变量
            short sRtn = -999;
            errCode = 0;

            long pStatus;
            short sTime = 0;

            //打开运动控制器
            sRtn = mc.GTN_OpenCard(mc.CHANNEL_PCIE, null, null);
            OutputMessage($"GTN_OpenCard()_信息：{sRtn}");

            //初始化运动控制器
            sRtn = mc.GTN_NetInit(99, null, sTime, out pStatus);

            Dictionary<long, string> _errorMessages = new Dictionary<long, string>
            {
                {0, "{0}:初始化网络成功"},
                {17701, "{0}:overTime 参数设置错误"},
                {11702, "{0}:EtherCAT 网络初始化错误"},
                {11703, "{0}:获取控制器类型错误"},
                {11704, "{0}:急停所有轴失败"},
                {11705, "{0}:等环网初始化错误"},
                {11706, "{0}:初始化控制器资源错误"},
                {11902, "{0}:缺少主站库/没有Gecat.xml/Gecat.xml里的文件不对"},
                {11905, "{0}:未知？？？？"},
            };

            // 从字典获取对应模板，若不存在则使用默认信息
            string messageTemplate = _errorMessages.TryGetValue(sRtn, out var template)
                ? template : "{0}:未知返回值,请联系固高官方!";

            OutputMessage($"GTN_NetInit()_信息：{messageTemplate}");

            //初始化两个内核
            foreach (var core in _cores)
            {
                //读UUID
                byte[] pcode = new byte[16];
                sRtn = mc.GTN_GetUuid(core, out pcode[0], 16);
                string str = Encoding.UTF8.GetString(pcode);
                OutputMessage($"核{core}_GTN_GetUuid()_信息：{str}");

                sRtn = mc.GTN_Reset(core);
                if (sRtn != 0)
                {
                    ErrMessage(sRtn);
                    errCode = 3;
                    return false;
                }
                //两个核需要加载不同配置
                //暂时从Demo中拷贝的配置
                sRtn = mc.GTN_LoadConfig(core, $"gsne_core{core}.cfg");
                if (sRtn != 0)
                {
                    ErrMessage(sRtn);
                    errCode = 4;
                    return false;
                }

                sRtn = mc.GTN_ClrSts(core, 1, _axisCount);
                if (sRtn != 0)
                {
                    ErrMessage(sRtn);
                    errCode = 7;
                    return false;
                }
            }

            //sRtn = mc.GTN_GetResCount(_core, 30, out _axisCount);
            //if (axisNum != _axisCount)
            //{
            //    ErrMessage($"总线扫描到的轴数量:{_axisCount}和传入数量:{axisNum}不匹配!");
            //    errCode = 6;
            //    return false;
            //}

            //sRtn = GTN.glink.GT_GLinkInit(0);
            //#define GLINK_MODE_DLL 0 //DLL线程刷新方式 
            //#define GLINK_MODE_DSP 1 //DSP硬件刷新方式 
            //#define GLINK_MODE_DLL_MAINTAIN_DO 2 //DLL线程刷新方式，保持DO输出 
            //#define GLINK_MODE_DSP_MAINTAIN_DO 3 //DSP硬件刷新方式，保持DO输出
            //sRtn = glink.GT_GLinkInitEx(0, 3);//cardNum卡号,目前只支持0;可保持IO，支持插补

            //if (sRtn != 0)
            //{
            //    ErrMessage(sRtn);
            //    errCode = 8;
            //    return false;
            //}

            if (etherCATIONum != _ioModuleCount)
            {
                ErrMessage($"总线扫描到的IO模块数量:{_ioModuleCount}和传入数量:{etherCATIONum}不匹配!");
                errCode = 9;
                return false;
            }

            for (short i = 1; i <= axisNum; i++)
            {
                if (!SetPrf(i))
                {
                    ErrMessage($"编码器与规划器绑定失败!");
                    errCode = 10;
                    return false;
                }
            }

            //sRtn = GTN.mc.GTN_ClrSts(1, 1, 1);//清除轴状态

            //sRtn = GTN.mc.GTN_AxisOn(1, 1);//使能


            return true;
        }

        /// <summary>
        /// 关闭控制卡
        /// </summary>
        /// <returns></returns>
        public bool Close(short core = 2)
        {
            short sRtn;

            //按位表示，mask表示对应轴(1表示停止对应轴)，option对mask中需要停止的轴的停止方式：1表示平滑停止/0表示
            sRtn = mc.GTN_Stop(core, 255, 0);
            OutputMessage($"GTN_Stop()_信息：{sRtn}");

            sRtn = mc.GTN_MultiAxisOff(core, 255);
            OutputMessage($"GTN_MultiAxisOff()_信息：{sRtn}");

            Thread.Sleep(100);
            sRtn = mc.GTN_Close();
            OutputMessage($"GTN_Close()_信息：{sRtn}");
            return true;
        }

    }
}
