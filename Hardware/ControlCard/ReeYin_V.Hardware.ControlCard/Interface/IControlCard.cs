using ReeYin_V.Core;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.ControlCard.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ReeYin_V.Hardware.ControlCard
{
    /// <summary>
    /// 运动控制卡的接口
    /// </summary>
    public interface IControlCard
    {
        /// <summary>
        /// 表示控制卡是否初始化完成
        /// </summary>
        bool Initialized { get; }

        /// <summary>
        /// 配置
        /// </summary>
        ControlCardConfig Config { get; set; }

        /// <summary>
        /// 设备状态
        /// </summary>
        HardwareState State { get; set; }

        /// <summary>
        /// 是否就绪
        /// </summary>
        bool IsReady { get; }

        /// <summary>
        /// 运动轴是否回零
        /// </summary>
        bool IsAxisHomed { get; }

        /// <summary>
        /// 初始化轴卡
        /// </summary>
        /// <returns></returns>
        bool Init(/*IControlCardConfigProvider provider*/);

        /// <summary>
        /// 关闭控制卡
        /// </summary>
        void Close();

        /// <summary>
        /// 设置运动轴速度模式
        /// </summary>
        void SetSpeedMode(EN_SpeedType mode);

        /// <summary>
        /// 运动轴回零
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        bool GoHome(out string message);

        /// <summary>
        /// 移动轴
        /// </summary>
        /// <param name="axisType">轴的类型</param>
        /// <param name="dir">移动方向</param>
        /// <param name="um">距离</param>
        /// <param name="message"></param>
        /// <returns></returns>
        bool Move(En_AxisNum axisType, MoveDirection moveDirection, double um, out string message);

        /// <summary>
        /// 移动轴
        /// </summary>
        /// <param name="axisType">轴的类型</param>
        /// <param name="dir">移动方向</param>
        /// <param name="um">距离</param>
        /// <param name="message"></param>
        /// <returns></returns>
        bool JogAxis(En_AxisNum axisId, MoveDirection dir, float step);

        /// <summary>
        /// 移动轴
        /// </summary>
        /// <param name="axisType">轴的类型</param>
        /// <param name="dir">移动方向</param>
        /// <param name="um">距离</param>
        /// <param name="message"></param>
        /// <returns></returns>
        bool JogAxis(En_AxisNum axisId, MoveDirection dir, EN_SpeedType spdType, bool isRunStop);

        /// <summary>
        /// 连续运动
        /// </summary>
        /// <param name="axisType"></param>
        /// <param name="moveDirection"></param>
        /// <returns></returns>
        bool Move(En_AxisNum axisType, MoveDirection moveDirection);

        /// <summary>
        /// 停止轴运动
        /// </summary>
        /// <param name="axisType"></param>
        void Stop(En_AxisNum? axisType);

        /// <summary>
        /// 获取所有轴的位置信息
        /// </summary>
        /// <param name="allPosInfos"></param>
        /// <returns></returns>s
        bool GetAllPosInfos(short core = 2);

        /// <summary>
        /// 获取所有轴的实际速度信息
        /// </summary>
        bool GetAllSpeedInfos(short core = 2);

        /// <summary>
        /// 获取所有轴的实际速度信息
        /// </summary>
        bool GetAllSpeedInfos(ref double[] allSpeedInfos, short core = 2);

        /// <summary>
        /// 直线插补运动
        /// </summary>
        bool LineInterpoMoving(LineInterPoParam param);

        /// <summary>
        /// 获取所有输入状态
        /// </summary>
        /// <param name="iSlave"></param>
        /// <param name="Status"></param>
        /// <returns></returns>
        bool GetAllInput(out bool[] Status);

        /// <summary>
        /// 获取所有输出状态
        /// </summary>
        /// <param name="iSlave"></param>
        /// <param name="Status"></param>
        /// <returns></returns>
        bool GetAllOutput(out bool[] Status);

        /// <summary>
        /// 设置指定IO开关
        /// </summary>
        /// <param name="Type"></param>
        /// <param name="Part"></param>
        /// <param name="OnOrOff"></param>
        /// <returns></returns>
        bool SetSpecifiedIO(int Part, bool OnOrOff);

        /// <summary>
        /// 获取指定IO状态
        /// </summary>
        /// <param name="InOrOut">true为输入；false为输出</param>
        /// <param name="Part">端口号</param>
        /// <param name="OnOrOff">true为开，false为关</param>
        /// <returns></returns>
        bool GetSpecifiedIO(bool InOrOut,int Part, out bool OnOrOff);

        /// <summary>
        /// 设置轴使能
        /// </summary>
        /// <param name="axisId"></param>
        /// <param name="isEnabled"></param>
        /// <returns></returns>
        bool SetAxisEnabled(short axisId, bool isEnabled);

        #region 针对固高
        /// <summary>
        /// 开启位置比较功能
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public bool ControlPosComparison(bool On_Off, PosComparisonOutputParam param);

        /// <summary>
        /// 获取实际的比较位置
        /// </summary>
        /// <param name="posCompareIndex"></param>
        /// <param name="ActualX"></param>
        /// <param name="ActualY"></param>
        /// <returns></returns>
        public bool GetActualComparePos(short posCompareIndex, ref int[] ActualX, ref int[] ActualY);

        /// <summary>
        /// 自定义插补运动
        /// </summary>
        /// <param name="param"></param>
        /// <param name="ConstomOrder"></param>
        /// <returns></returns>
        public bool CustomInterpolationMoving(CustomInterPoParam param, Func<string> ConstomOrder, bool waitend = false);

        public bool MoveAbsoluteAxis(En_AxisNum axisId, double fpos, bool waitforend = false);

        /// <summary>
        /// 圆弧插补运动
        /// </summary>
        /// <returns></returns>
        public bool ArcInterpoMoving(ArcInterPoParam param);

        ///// <summary>
        ///// 设置指定IO开关
        ///// </summary>
        ///// <param name="Type"></param>
        ///// <param name="Part"></param>
        ///// <param name="OnOrOff"></param>
        ///// <returns></returns>
        //bool SetSpecifiedIO(ExpansionCardType Type, int Part, bool OnOrOff);

        ///// <summary>
        ///// 获取指定IO状态
        ///// </summary>
        ///// <param name="Type"></param>
        ///// <param name="Part"></param>
        ///// <param name="OnOrOff"></param>
        ///// <returns></returns>
        //bool GetSpecifiedIO(ExpansionCardType Type, int Part, out bool OnOrOff);
        #endregion

    }

    /// <summary>
    /// 拓展卡类型
    /// </summary>
    public enum ExpansionCardType
    {
        Exp_GNM402_00,
        GLink,
    }
}
