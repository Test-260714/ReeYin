using ReeYin_V.Core;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.ControlCard;
using ReeYin_V.Hardware.ControlCard.Models;
using ReeYin_V.Hardware.ControlCard.ZMotion.Api;
using System;
using System.Linq;

namespace ReeYin_V.Hardware.ControlCard.ZMotion.App
{
    /// <summary>
    /// 正运动控制卡
    /// </summary>
    public partial class ZMotionControlCard : ControlCardBase
    {
        private const int DigitalInputCount = 32;
        private const int DigitalOutputCount = 32;

        #region Fields
        /// <summary>
        /// 控制卡句柄
        /// </summary>
        private IntPtr g_handle = IntPtr.Zero;

        private readonly object diagnosticSync = new();

        /// <summary>
        /// 连接类型 0-串口 1-以太网
        /// </summary>
        private int connectionType = 1;

        /// <summary>
        /// IP地址
        /// </summary>
        private string ipAddress = "192.168.0.11";

        /// <summary>
        /// 串口号
        /// </summary>
        private uint comPort = 1;
        #endregion

        #region Properties
        public IntPtr Handle => g_handle;

        public int ConnectionType
        {
            get => connectionType;
            set => connectionType = value;
        }

        public string IpAddress
        {
            get => ipAddress;
            set => ipAddress = value ?? string.Empty;
        }

        public uint ComPort
        {
            get => comPort;
            set => comPort = value;
        }
        #endregion

        #region Constructor
        public ZMotionControlCard()
        {
            VenderName = "ZMotion";
            CardType = "ZMotion";
            NickName = "正运动控制卡";
        }
        #endregion

        #region Override Methods
        protected override bool DoInit()
        {
            try
            {
                State = HardwareState.Connecting;
                int ret = 0;
                if (connectionType == 0)
                {
                    ret = Zmcaux.ZAux_OpenCom(comPort, out g_handle);
                }
                else
                {
                    ret = Zmcaux.ZAux_OpenEth(ipAddress, out g_handle);
                }

                if (ret != 0 || g_handle == IntPtr.Zero)
                {
                    Console.WriteLine($"正运动控制卡连接失败，错误码：{ret}");
                    IsConnected = false;
                    State = HardwareState.NotConnected;
                    return false;
                }

                IsConnected = true;
                State = HardwareState.Connected;
                Console.WriteLine("正运动控制卡连接成功！");

                InitializeAxes();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DoInit()_控制卡初始化失败：{ex.Message}");
                IsConnected = false;
                State = HardwareState.Error;
                return false;
            }
        }

        protected override void DoConfigure()
        {
            // 配置控制卡参数
            try
            {
                foreach (var axis in Config.AllAxis)
                {
                    // 获取速度设置
                    var speedSetting = axis.SpeedDict1?.FirstOrDefault(s => s.SpeedType == EN_SpeedType.Work);
                    if (speedSetting != null)
                    {
                        // 设置轴加速度
                        Zmcaux.ZAux_Direct_SetAccel(g_handle, axis.AxisNo - 1, (float)speedSetting.AccSpeed);
                        // 设置轴减速度
                        Zmcaux.ZAux_Direct_SetDecel(g_handle, axis.AxisNo - 1, (float)speedSetting.AccSpeed);
                        // 设置轴起始速度
                        Zmcaux.ZAux_Direct_SetLspeed(g_handle, axis.AxisNo - 1, (float)speedSetting.StartSpeed);
                        // 设置轴运行速度
                        Zmcaux.ZAux_Direct_SetSpeed(g_handle, axis.AxisNo - 1, (float)speedSetting.MaxSpeed);
                    }
                }

                Console.WriteLine("控制卡配置完成！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DoConfigure()_配置失败：{ex.Message}");
            }
        }

        protected override void DoClose()
        {
            try
            {
                if (g_handle != IntPtr.Zero)
                {
                    State = HardwareState.Disconnecting;
                    // 停止所有轴
                    Zmcaux.ZAux_Direct_Rapid_Stop(g_handle, Config.AllAxis.Count);
                    
                    // 关闭连接
                    Zmcaux.ZAux_Close(g_handle);
                    g_handle = IntPtr.Zero;
                    IsConnected = false;
                    State = HardwareState.Closed;
                    
                    Console.WriteLine("正运动控制卡连接已关闭");
                }
                else
                {
                    IsConnected = false;
                    State = HardwareState.Closed;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DoClose()_关闭失败：{ex.Message}");
            }
        }

        protected override bool DoGetAxisEnable(En_AxisNum axisType)
        {
            try
            {
                int axisIndex = GetAxisIndex(axisType);
                int enableStatus = 0;
                int ret = Zmcaux.ZAux_Direct_GetAxisEnable(g_handle, axisIndex, ref enableStatus);
                
                return ret == 0 && enableStatus == 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DoGetAxisEnable()_获取轴使能状态失败：{ex.Message}");
                return false;
            }
        }

        protected override bool DoSetAxisEnable(En_AxisNum axisType, bool v)
        {
            try
            {
                int axisIndex = GetAxisIndex(axisType);
                int ret = Zmcaux.ZAux_Direct_SetAxisEnable(g_handle, axisIndex, v ? 1 : 0);
                
                return ret == 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DoSetAxisEnable()_设置轴使能失败：{ex.Message}");
                return false;
            }
        }

        protected override bool DoGetAxisStopped(En_AxisNum axisType)
        {
            try
            {
                int axisIndex = GetAxisIndex(axisType);
                int idleStatus = 0;
                int ret = Zmcaux.ZAux_Direct_GetIfIdle(g_handle, axisIndex, ref idleStatus);
                
                // idleStatus: 0-运动中 -1-停止
                return ret == 0 && idleStatus == -1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DoGetAxisStopped()_获取轴停止状态失败：{ex.Message}");
                return false;
            }
        }

        protected override bool DoMoveAxis(En_AxisNum axisType, double um)
        {
            try
            {
                int axisIndex = GetAxisIndex(axisType);
                float targetPos = (float)um;
                
                // 绝对运动
                int ret = Zmcaux.ZAux_Direct_Single_Move(g_handle, axisIndex, targetPos);
                
                return ret == 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DoMoveAxis()_轴移动失败：{ex.Message}");
                return false;
            }
        }

        protected override bool DoMoveContinue(En_AxisNum axisType, MoveDirection moveDirection)
        {
            try
            {
                int axisIndex = GetAxisIndex(axisType);
                float speed = GetAxisSpeed(axisType);
                
                // 根据方向设置速度正负
                if (moveDirection == MoveDirection.反向)
                {
                    speed = -speed;
                }
                
                // 连续运动
                int ret = Zmcaux.ZAux_Direct_Single_Vmove(g_handle, axisIndex, speed);
                
                return ret == 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DoMoveContinue()_轴连续运动失败：{ex.Message}");
                return false;
            }
        }

        protected override void DoStop(En_AxisNum? axisType, AxisStopMode stopMode)
        {
            try
            {
                if (axisType.HasValue)
                {
                    // 停止单个轴
                    int axisIndex = GetAxisIndex(axisType.Value);
                    int mode = stopMode == AxisStopMode.立即停止 ? 1 : 2;
                    Zmcaux.ZAux_Direct_Single_Cancel(g_handle, axisIndex, mode);
                }
                else
                {
                    // 停止所有轴
                    Zmcaux.ZAux_Direct_Rapid_Stop(g_handle, Config.AllAxis.Count);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DoStop()_轴停止失败：{ex.Message}");
            }
        }


        protected override bool DoGoHome(out string message)
        {
            message = "正运动通用控制卡未配置回零方式，请由具体设备模块实现回零流程";
            return false;
        }

        public override bool GetAllPosInfos(ref double[] allPosInfos, short core = 2)
        {
            try
            {
                if (!IsConnected)
                {
                    return false;
                }

                for (int i = 0; i < allPosInfos.Length && i < Config.AllAxis.Count; i++)
                {
                    float pos = 0;
                    int ret = Zmcaux.ZAux_Direct_GetDpos(g_handle, Config.AllAxis[i].AxisNo - 1, ref pos);
                    
                    if (ret == 0)
                    {
                        allPosInfos[i] = Math.Round(pos, 2);
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetAllPosInfos()_获取所有轴位置信息失败：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取编码器反馈位置（MPOS）
        /// </summary>
        /// <param name="axisIndex">轴索引（从0开始）</param>
        /// <returns>编码器位置（脉冲数）</returns>
        public float GetEncoderPosition(int axisIndex)
        {
            try
            {
                if (!IsConnected || g_handle == IntPtr.Zero)
                    return 0;

                float pos = 0;
                int ret = Zmcaux.ZAux_Direct_GetMpos(g_handle, axisIndex, ref pos);
                
                if (ret != 0)
                {
                    Console.WriteLine($"GetEncoderPosition()_读取编码器位置失败，错误码：{ret}");
                    return 0;
                }
                
                return pos;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetEncoderPosition()_异常：{ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 清零编码器位置
        /// </summary>
        /// <param name="axisIndex">轴索引（从0开始）</param>
        public bool ResetEncoderPosition(int axisIndex)
        {
            try
            {
                if (!IsConnected || g_handle == IntPtr.Zero)
                    return false;

                // TODO: 需要确认正运动SDK中清零编码器位置的正确API
                // int ret = Zmcaux.ZAux_Direct_SetMpos(g_handle, axisIndex, 0);
                // return ret == 0;
                
                Console.WriteLine("ResetEncoderPosition()_待实现");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ResetEncoderPosition()_异常：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取编码器速度（脉冲/秒）
        /// </summary>
        /// <param name="axisIndex">轴索引（从0开始）</param>
        /// <returns>速度（脉冲/秒）</returns>
        public float GetEncoderSpeed(int axisIndex)
        {
            try
            {
                if (!IsConnected || g_handle == IntPtr.Zero)
                    return 0;

                float speed = 0;
                int ret = Zmcaux.ZAux_Direct_GetVpSpeed(g_handle, axisIndex, ref speed);
                
                if (ret != 0)
                {
                    Console.WriteLine($"GetEncoderSpeed()_读取编码器速度失败，错误码：{ret}");
                    return 0;
                }
                
                return Math.Abs(speed);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetEncoderSpeed()_异常：{ex.Message}");
                return 0;
            }
        }

        public override bool GetAllInput(out bool[] Status)
        {
            Status = new bool[DigitalInputCount];
            try
            {
                if (!EnsureIoReady(nameof(GetAllInput)))
                {
                    return false;
                }

                for (int i = 0; i < Status.Length; i++)
                {
                    if (!TryReadIo(true, i, out var state))
                    {
                        return false;
                    }

                    Status[i] = state;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetAllInput()_获取输入状态失败：{ex.Message}");
                return false;
            }
        }

        public override bool GetAllOutput(out bool[] Status)
        {
            Status = new bool[DigitalOutputCount];
            try
            {
                if (!EnsureIoReady(nameof(GetAllOutput)))
                {
                    return false;
                }

                for (int i = 0; i < Status.Length; i++)
                {
                    if (!TryReadIo(false, i, out var state))
                    {
                        return false;
                    }

                    Status[i] = state;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetAllOutput()_获取输出状态失败：{ex.Message}");
                return false;
            }
        }

        public override bool SetSpecifiedIO(int Part, bool OnOrOff)
        {
            try
            {
                if (!EnsureIoReady(nameof(SetSpecifiedIO)))
                    return false;

                if (!IsValidOutputPort(Part))
                {
                    Console.WriteLine($"SetSpecifiedIO()_输出点位越界：OUT{Part}");
                    return false;
                }

                UInt32 value = OnOrOff ? (UInt32)1 : (UInt32)0;
                int ret = Zmcaux.ZAux_Direct_SetOp(g_handle, Part, value);
                if (ret != 0)
                {
                    Console.WriteLine($"SetSpecifiedIO()_设置OUT{Part}失败，错误码：{ret}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SetSpecifiedIO()_设置IO失败：{ex.Message}");
                return false;
            }
        }

        public override bool GetSpecifiedIO(bool InOrOut, int Part, out bool OnOrOff)
        {
            OnOrOff = false;
            try
            {
                if (!EnsureIoReady(nameof(GetSpecifiedIO)))
                    return false;

                if (InOrOut && !IsValidInputPort(Part))
                {
                    Console.WriteLine($"GetSpecifiedIO()_输入点位越界：IN{Part}");
                    return false;
                }

                if (!InOrOut && !IsValidOutputPort(Part))
                {
                    Console.WriteLine($"GetSpecifiedIO()_输出点位越界：OUT{Part}");
                    return false;
                }

                return TryReadIo(InOrOut, Part, out OnOrOff);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetSpecifiedIO()_获取IO状态失败：{ex.Message}");
                return false;
            }
        }

        public override bool SetAxisEnabled(short axisId, bool isEnabled)
        {
            try
            {
                int ret = Zmcaux.ZAux_Direct_SetAxisEnable(g_handle, axisId, isEnabled ? 1 : 0);
                return ret == 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SetAxisEnabled()_设置轴使能失败：{ex.Message}");
                return false;
            }
        }

        public bool TryGetAxisDiagnostic(
            short axisId,
            out float position,
            out bool isEnabled,
            out bool isIdle,
            out int errorCode,
            out string message)
        {
            position = 0;
            isEnabled = false;
            isIdle = false;
            errorCode = 0;

            if (!TryGetConfiguredAxis(axisId, out _, out message))
            {
                return false;
            }

            lock (diagnosticSync)
            {
                int enableStatus = 0;
                int idleStatus = 0;

                errorCode = Zmcaux.ZAux_Direct_GetDpos(g_handle, axisId, ref position);
                if (errorCode == 0)
                {
                    errorCode = Zmcaux.ZAux_Direct_GetAxisEnable(g_handle, axisId, ref enableStatus);
                }

                if (errorCode == 0)
                {
                    errorCode = Zmcaux.ZAux_Direct_GetIfIdle(g_handle, axisId, ref idleStatus);
                }

                if (errorCode != 0)
                {
                    message = $"读取轴 {axisId} 状态失败，SDK 错误码：{errorCode}";
                    return false;
                }

                isEnabled = enableStatus == 1;
                isIdle = idleStatus == -1;
                message = string.Empty;
                return true;
            }
        }

        public bool TrySetAxisEnabled(short axisId, bool isEnabled, out int errorCode, out string message)
        {
            errorCode = 0;
            if (!TryGetConfiguredAxis(axisId, out _, out message))
            {
                return false;
            }

            lock (diagnosticSync)
            {
                errorCode = Zmcaux.ZAux_Direct_SetAxisEnable(g_handle, axisId, isEnabled ? 1 : 0);
            }

            message = errorCode == 0
                ? $"轴 {axisId} 已{(isEnabled ? "使能" : "去使能")}" 
                : $"设置轴 {axisId} 使能状态失败，SDK 错误码：{errorCode}";
            return errorCode == 0;
        }

        public bool TryStartJog(short axisId, MoveDirection direction, out int errorCode, out string message)
        {
            errorCode = 0;
            if (!TryGetConfiguredAxis(axisId, out var axis, out message))
            {
                return false;
            }

            lock (diagnosticSync)
            {
                if (!TryEnsureAxisCanMove(axisId, out errorCode, out message))
                {
                    return false;
                }

                float speed = GetAxisSpeed(axis.AxisNum);
                if (!float.IsFinite(speed) || speed <= 0)
                {
                    message = $"轴 {axisId} 的工作速度配置无效";
                    return false;
                }

                bool reverse = direction == MoveDirection.反向;
                if (axis.MovingDirNe)
                {
                    reverse = !reverse;
                }

                errorCode = Zmcaux.ZAux_Direct_Single_Vmove(g_handle, axisId, reverse ? -speed : speed);
            }

            message = errorCode == 0
                ? $"轴 {axisId} 开始{(direction == MoveDirection.反向 ? "反向" : "正向")}点动"
                : $"轴 {axisId} 点动失败，SDK 错误码：{errorCode}";
            return errorCode == 0;
        }

        public bool TryMoveAbsolute(short axisId, double targetPosition, out int errorCode, out string message)
        {
            errorCode = 0;
            if (!double.IsFinite(targetPosition))
            {
                message = "目标位置必须是有效数值";
                return false;
            }

            if (targetPosition < -float.MaxValue || targetPosition > float.MaxValue)
            {
                message = "目标位置超出控制器可接受范围";
                return false;
            }

            if (!TryGetConfiguredAxis(axisId, out var axis, out message))
            {
                return false;
            }

            if (!TryRefreshAxisPositions(out errorCode, out message))
            {
                return false;
            }

            if (!ValidateLimitPosition(axis.AxisNum, targetPosition, out message))
            {
                return false;
            }

            lock (diagnosticSync)
            {
                if (!TryEnsureAxisCanMove(axisId, out errorCode, out message))
                {
                    return false;
                }

                errorCode = Zmcaux.ZAux_Direct_Single_MoveAbs(g_handle, axisId, (float)targetPosition);
            }

            message = errorCode == 0
                ? $"轴 {axisId} 已下发绝对定位：{targetPosition:F3}"
                : $"轴 {axisId} 绝对定位失败，SDK 错误码：{errorCode}";
            return errorCode == 0;
        }

        public bool TryStopAxis(short axisId, out int errorCode, out string message)
        {
            errorCode = 0;
            if (!TryGetConfiguredAxis(axisId, out _, out message))
            {
                return false;
            }

            lock (diagnosticSync)
            {
                errorCode = Zmcaux.ZAux_Direct_Single_Cancel(g_handle, axisId, 2);
            }

            message = errorCode == 0
                ? $"轴 {axisId} 已停止"
                : $"停止轴 {axisId} 失败，SDK 错误码：{errorCode}";
            return errorCode == 0;
        }
        #endregion

        #region Helper Methods
        private void InitializeAxes()
        {
            // 初始化轴数组
            CurPos = new double[Config.AllAxis.Count];
            CurPulse = new int[Config.AllAxis.Count];
        }

        private int GetAxisIndex(En_AxisNum axisType)
        {
            var axis = Config.AllAxis.FirstOrDefault(a => a.AxisNum == axisType);
            return axis != null ? axis.AxisNo - 1 : 0;
        }

        private float GetAxisSpeed(En_AxisNum axisType)
        {
            var axis = Config.AllAxis.FirstOrDefault(a => a.AxisNum == axisType);
            if (axis?.SpeedDict1 != null)
            {
                var speedSetting = axis.SpeedDict1.FirstOrDefault(s => s.SpeedType == EN_SpeedType.Work);
                if (speedSetting != null)
                {
                    return (float)speedSetting.MaxSpeed;
                }
            }
            return 100f;
        }

        private bool TryGetConfiguredAxis(short axisId, out SingleAxisParam axis, out string message)
        {
            axis = null!;
            if (!IsConnected || g_handle == IntPtr.Zero)
            {
                message = "ZMotion 控制卡未连接";
                return false;
            }

            axis = Config.AllAxis.FirstOrDefault(item => item.AxisNo - 1 == axisId)!;
            if (axis == null)
            {
                message = $"轴 {axisId} 未配置";
                return false;
            }

            message = string.Empty;
            return true;
        }

        private bool TryEnsureAxisCanMove(short axisId, out int errorCode, out string message)
        {
            int enableStatus = 0;
            errorCode = Zmcaux.ZAux_Direct_GetAxisEnable(g_handle, axisId, ref enableStatus);
            if (errorCode != 0)
            {
                message = $"读取轴 {axisId} 使能状态失败，SDK 错误码：{errorCode}";
                return false;
            }

            if (enableStatus != 1)
            {
                message = $"轴 {axisId} 未使能";
                return false;
            }

            int idleStatus = 0;
            errorCode = Zmcaux.ZAux_Direct_GetIfIdle(g_handle, axisId, ref idleStatus);
            if (errorCode != 0)
            {
                message = $"读取轴 {axisId} 运行状态失败，SDK 错误码：{errorCode}";
                return false;
            }

            if (idleStatus != -1)
            {
                message = $"轴 {axisId} 正在运动";
                return false;
            }

            message = string.Empty;
            return true;
        }

        private bool TryRefreshAxisPositions(out int errorCode, out string message)
        {
            errorCode = 0;
            var configuredAxes = Config.AllAxis.Where(item => item.AxisNo > 0).ToList();
            int requiredLength = configuredAxes.Count == 0
                ? 0
                : configuredAxes.Max(item => item.AxisNo);
            if (CurPos == null || CurPos.Length < requiredLength)
            {
                CurPos = new double[requiredLength];
            }

            lock (diagnosticSync)
            {
                foreach (var axis in configuredAxes)
                {
                    float position = 0;
                    int axisId = axis.AxisNo - 1;
                    errorCode = Zmcaux.ZAux_Direct_GetDpos(g_handle, axisId, ref position);
                    if (errorCode != 0)
                    {
                        message = $"读取轴 {axisId} 位置失败，SDK 错误码：{errorCode}";
                        return false;
                    }

                    CurPos[axisId] = position;
                }
            }

            message = string.Empty;
            return true;
        }

        private bool EnsureIoReady(string caller)
        {
            if (IsConnected && g_handle != IntPtr.Zero)
            {
                return true;
            }

            Console.WriteLine($"{caller}()_控制卡未连接或句柄无效");
            return false;
        }

        private static bool IsValidInputPort(int port) => port >= 0 && port < DigitalInputCount;

        private static bool IsValidOutputPort(int port) => port >= 0 && port < DigitalOutputCount;

        private bool TryReadIo(bool input, int port, out bool state)
        {
            state = false;
            UInt32 value = 0;
            int ret = input
                ? Zmcaux.ZAux_Direct_GetIn(g_handle, port, ref value)
                : Zmcaux.ZAux_Direct_GetOp(g_handle, port, ref value);

            if (ret != 0)
            {
                Console.WriteLine($"TryReadIo()_读取{(input ? "IN" : "OUT")}{port}失败，错误码：{ret}");
                return false;
            }

            state = value != 0;
            return true;
        }
        #endregion
    }
}
