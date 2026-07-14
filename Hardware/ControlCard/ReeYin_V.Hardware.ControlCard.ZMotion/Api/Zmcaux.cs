using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ReeYin_V.Hardware.ControlCard.ZMotion.Api
{
    /// <summary>
    /// 正运动控制卡API封装
    /// </summary>
    public class Zmcaux
    {
        /// <summary>
        /// 控制器串口链接
        /// </summary>
        [DllImport("zauxdll.dll", EntryPoint = "ZAux_OpenCom", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 ZAux_OpenCom(UInt32 comid, out IntPtr phandle);

        /// <summary>
        /// 以太网方式连接控制器
        /// </summary>
        [DllImport("zauxdll.dll", EntryPoint = "ZAux_OpenEth", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 ZAux_OpenEth(string ipaddr, out IntPtr phandle);

        /// <summary>
        /// 关闭控制器链接
        /// </summary>
        [DllImport("zauxdll.dll", EntryPoint = "ZAux_Close", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 ZAux_Close(IntPtr handle);

        /// <summary>
        /// Execute在线命令
        /// </summary>
        [DllImport("zauxdll.dll", EntryPoint = "ZAux_Execute", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 ZAux_Execute(IntPtr handle, string pszCommand, StringBuilder psResponse, UInt32 uiResponseLength);

        /// <summary>
        /// 读取输入信号
        /// </summary>
        [DllImport("zauxdll.dll", EntryPoint = "ZAux_Direct_GetIn", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 ZAux_Direct_GetIn(IntPtr handle, int ionum, ref UInt32 piValue);

        /// <summary>
        /// 设置输出口
        /// </summary>
        [DllImport("zauxdll.dll", EntryPoint = "ZAux_Direct_SetOp", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 ZAux_Direct_SetOp(IntPtr handle, int ionum, UInt32 iValue);

        /// <summary>
        /// 读取输出口状态
        /// </summary>
        [DllImport("zauxdll.dll", EntryPoint = "ZAux_Direct_GetOp", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 ZAux_Direct_GetOp(IntPtr handle, int ionum, ref UInt32 piValue);

        /// <summary>
        /// 设置轴加速度
        /// </summary>
        [DllImport("zauxdll.dll", EntryPoint = "ZAux_Direct_SetAccel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 ZAux_Direct_SetAccel(IntPtr handle, int iaxis, float fValue);

        /// <summary>
        /// 读取轴加速度
        /// </summary>
        [DllImport("zauxdll.dll", EntryPoint = "ZAux_Direct_GetAccel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 ZAux_Direct_GetAccel(IntPtr handle, int iaxis, ref float pfValue);

        /// <summary>
        /// 设置轴减速度
        /// </summary>
        [DllImport("zauxdll.dll", EntryPoint = "ZAux_Direct_SetDecel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 ZAux_Direct_SetDecel(IntPtr handle, int iaxis, float fValue);

        /// <summary>
        /// 读取轴减速度
        /// </summary>
        [DllImport("zauxdll.dll", EntryPoint = "ZAux_Direct_GetDecel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 ZAux_Direct_GetDecel(IntPtr handle, int iaxis, ref float pfValue);

        /// <summary>
        /// 设置轴命令位置坐标
        /// </summary>
        [DllImport("zauxdll.dll", EntryPoint = "ZAux_Direct_SetDpos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 ZAux_Direct_SetDpos(IntPtr handle, int iaxis, float fValue);

        /// <summary>
        /// 读取轴命令位置坐标
        /// </summary>
        [DllImport("zauxdll.dll", EntryPoint = "ZAux_Direct_GetDpos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 ZAux_Direct_GetDpos(IntPtr handle, int iaxis, ref float pfValue);

        /// <summary>
        /// 读取编码轴反馈位置坐标
        /// </summary>
        [DllImport("zauxdll.dll", EntryPoint = "ZAux_Direct_GetMpos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 ZAux_Direct_GetMpos(IntPtr handle, int iaxis, ref float pfValue);

        /// <summary>
        /// 读取轴内部编码器值（部分型号用于外接编码器反馈）
        /// </summary>
        [DllImport("zauxdll.dll", EntryPoint = "ZAux_Direct_GetEncoder", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 ZAux_Direct_GetEncoder(IntPtr handle, int iaxis, ref float pfValue);

        /// <summary>
        /// 设置编码轴反馈位置坐标
        /// </summary>
        [DllImport("zauxdll.dll", EntryPoint = "ZAux_Direct_SetMpos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 ZAux_Direct_SetMpos(IntPtr handle, int iaxis, float fValue);

        /// <summary>
        /// 读取轴运动完成状态
        /// </summary>
        [DllImport("zauxdll.dll", EntryPoint = "ZAux_Direct_GetIfIdle", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 ZAux_Direct_GetIfIdle(IntPtr handle, int iaxis, ref int piValue);

        /// <summary>
        /// 设置轴类型（部分功能需要编码器轴）
        /// </summary>
        [DllImport("zauxdll.dll", EntryPoint = "ZAux_Direct_SetAtype", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 ZAux_Direct_SetAtype(IntPtr handle, int iaxis, int iValue);

        /// <summary>
        /// 读取轴类型
        /// </summary>
        [DllImport("zauxdll.dll", EntryPoint = "ZAux_Direct_GetAtype", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 ZAux_Direct_GetAtype(IntPtr handle, int iaxis, ref int piValue);

        /// <summary>
        /// 设置编码器比例（MPOS 与编码器输入比）
        /// </summary>
        [DllImport("zauxdll.dll", EntryPoint = "ZAux_Direct_EncoderRatio", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 ZAux_Direct_EncoderRatio(IntPtr handle, int iaxis, int mpos_count, int input_count);

        /// <summary>
        /// 读取轴当前规划速度
        /// </summary>
        [DllImport("zauxdll.dll", EntryPoint = "ZAux_Direct_GetVpSpeed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 ZAux_Direct_GetVpSpeed(IntPtr handle, int iaxis, ref float pfValue);

        /// <summary>
        /// 设置轴使能
        /// </summary>
        [DllImport("zauxdll.dll", EntryPoint = "ZAux_Direct_SetAxisEnable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 ZAux_Direct_SetAxisEnable(IntPtr handle, int iaxis, int iValue);

        /// <summary>
        /// 读取轴使能状态
        /// </summary>
        [DllImport("zauxdll.dll", EntryPoint = "ZAux_Direct_GetAxisEnable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 ZAux_Direct_GetAxisEnable(IntPtr handle, int iaxis, ref int piValue);

        /// <summary>
        /// 设置轴起始速度
        /// </summary>
        [DllImport("zauxdll.dll", EntryPoint = "ZAux_Direct_SetLspeed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 ZAux_Direct_SetLspeed(IntPtr handle, int iaxis, float fValue);

        /// <summary>
        /// 读取轴起始速度
        /// </summary>
        [DllImport("zauxdll.dll", EntryPoint = "ZAux_Direct_GetLspeed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 ZAux_Direct_GetLspeed(IntPtr handle, int iaxis, ref float pfValue);

        /// <summary>
        /// 设置轴运行速度
        /// </summary>
        [DllImport("zauxdll.dll", EntryPoint = "ZAux_Direct_SetSpeed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 ZAux_Direct_SetSpeed(IntPtr handle, int iaxis, float fValue);

        /// <summary>
        /// 读取轴运行速度
        /// </summary>
        [DllImport("zauxdll.dll", EntryPoint = "ZAux_Direct_GetSpeed", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 ZAux_Direct_GetSpeed(IntPtr handle, int iaxis, ref float pfValue);

        /// <summary>
        /// 单轴绝对运动
        /// </summary>
        [DllImport("zauxdll.dll", EntryPoint = "ZAux_Direct_Single_Move", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 ZAux_Direct_Single_Move(IntPtr handle, int iaxis, float fValue);

        /// <summary>
        /// 单轴相对运动
        /// </summary>
        [DllImport("zauxdll.dll", EntryPoint = "ZAux_Direct_Single_MoveAbs", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 ZAux_Direct_Single_MoveAbs(IntPtr handle, int iaxis, float fValue);

        /// <summary>
        /// 单轴连续运动
        /// </summary>
        [DllImport("zauxdll.dll", EntryPoint = "ZAux_Direct_Single_Vmove", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 ZAux_Direct_Single_Vmove(IntPtr handle, int iaxis, float fValue);

        /// <summary>
        /// 单轴停止
        /// </summary>
        [DllImport("zauxdll.dll", EntryPoint = "ZAux_Direct_Single_Cancel", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 ZAux_Direct_Single_Cancel(IntPtr handle, int iaxis, int imode);

        /// <summary>
        /// 快速停止所有轴
        /// </summary>
        [DllImport("zauxdll.dll", EntryPoint = "ZAux_Direct_Rapid_Stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 ZAux_Direct_Rapid_Stop(IntPtr handle, int imaxaxis);

        /// <summary>
        /// 设置Modbus位寄存器
        /// </summary>
        [DllImport("zauxdll.dll", EntryPoint = "ZAux_Modbus_Set0x", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 ZAux_Modbus_Set0x(IntPtr handle, UInt16 addr, UInt16 num, byte[] pdata);

        /// <summary>
        /// 读取Modbus位寄存器
        /// </summary>
        [DllImport("zauxdll.dll", EntryPoint = "ZAux_Modbus_Get0x", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 ZAux_Modbus_Get0x(IntPtr handle, UInt16 addr, UInt16 num, byte[] pdata);

        /// <summary>
        /// 设置Modbus寄存器(REG)
        /// </summary>
        [DllImport("zauxdll.dll", EntryPoint = "ZAux_Modbus_Set4x", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 ZAux_Modbus_Set4x(IntPtr handle, UInt16 addr, UInt16 num, UInt16[] pdata);

        /// <summary>
        /// 读取Modbus寄存器(REG)
        /// </summary>
        [DllImport("zauxdll.dll", EntryPoint = "ZAux_Modbus_Get4x", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 ZAux_Modbus_Get4x(IntPtr handle, UInt16 addr, UInt16 num, UInt16[] pdata);

        /// <summary>
        /// 设置Modbus寄存器(float类型)
        /// </summary>
        [DllImport("zauxdll.dll", EntryPoint = "ZAux_Modbus_Set4x_Float", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 ZAux_Modbus_Set4x_Float(IntPtr handle, UInt16 addr, UInt16 num, float[] pdata);

        /// <summary>
        /// 读取Modbus寄存器(float类型)
        /// </summary>
        [DllImport("zauxdll.dll", EntryPoint = "ZAux_Modbus_Get4x_Float", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 ZAux_Modbus_Get4x_Float(IntPtr handle, UInt16 addr, UInt16 num, float[] pdata);

        /// <summary>
        /// 设置Modbus寄存器(LONG类型)
        /// </summary>
        [DllImport("zauxdll.dll", EntryPoint = "ZAux_Modbus_Set4x_Long", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 ZAux_Modbus_Set4x_Long(IntPtr handle, UInt16 addr, UInt16 num, int[] pdata);

        /// <summary>
        /// 读取Modbus寄存器(LONG类型)
        /// </summary>
        [DllImport("zauxdll.dll", EntryPoint = "ZAux_Modbus_Get4x_Long", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 ZAux_Modbus_Get4x_Long(IntPtr handle, UInt16 addr, UInt16 num, int[] pdata);
    }
}
