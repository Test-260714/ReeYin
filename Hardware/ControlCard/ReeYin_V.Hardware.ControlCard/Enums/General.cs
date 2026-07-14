using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Hardware.ControlCard
{
    /// <summary>
    /// 灯柱
    /// </summary>
    public enum En_LampPost
    {
        None = 0,
        Red = 1,
        Green = 2,
        Yellow = 3,
        GreenYellow = 4,//4及以前都是持续
        GreenFlash = 5,
        YellowFlash = 6,
        RedFlash = 7,
        GreenYellowFlash = 8,
    }

    /// <summary>
    /// 蜂鸣器
    /// </summary>
    public enum En_Buzzer
    {
        None = 0,
        Mute = 1,
        Normal = 2, //鸣0.5，停1
        Slow = 3,   //鸣0.5，停2
    }

    /// <summary>
    /// 轴速度类型
    /// </summary>
    public enum EN_SpeedType
    {
        [Description("低速")]
        Low,
        Mid,
        High,
        Work,
        Reset,
        [Description("自定义速度")]
        Custom,
    }

    /// <summary>
    /// 轴状态字
    /// </summary>
    public enum En_GetAxisClrSts
    {
        //Bit0 保留
        Bit0_Reserved = 0x01,
        //Bit1 驱动器报警标志 控制轴连接的驱动器报警时置 1
        Bit1_DriverAlarm = 0x02,
        //Bit2 保留
        Bit2_Reserved = 0x04,
        //Bit3 保留
        Bit3_Reserved = 0x08,
        //Bit4 跟随误差越限标志 控制轴规划位置和实际位置的误差大于设定极限时置 1 
        Bit4_FollowOverLimited = 0x10,
        //Bit5 正限位触发标志 正限位开关电平状态为限位触发电平时置 1规划位置大于正向软限位时置 1
        Bit5_PositiveLimitTriggered = 0x20,
        //Bit6 负限位触发标志 负限位开关电平状态为限位触发电平时置 1规划位置小于负向软限位时置 1
        Bit6_NegtiveLimitTriggered = 0x40,
        //Bit7 IO 平滑停止触发标志 如果轴设置了平滑停止 IO，当其输入为触发电平时置 1，并自动平滑停止该轴
        Bit7_IOSmoothStopTriggered = 0x80,
        //Bit8 IO 急停触发标志 如果轴设置了急停 IO，当其输入为触发电平时置 1，并自动急停该轴
        Bit8_EmergencyStopTriggered = 0x100,
        //Bit9 电机使能标志 电机使能时置 1
        Bit9_MotorEnabled = 0x200,
        //Bit10 规划运动标志 规划器运动时置 1
        Bit10_MoveEnabled = 0x400,
        //Bit11 电机到位标志 规划器静止，规划位置和实际位置的误差小于设定误差带，并且在误差带内保持设定时间后，置起到位标志
        Bit11_MotorInPlaceReady = 0x800,
    }

}
