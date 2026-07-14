using Prism.Commands;
using ReeYin_V.Core.IOC;
using ReeYin_V.Hardware.ControlCard.ZMotion.Api;
using ReeYin_V.Hardware.ControlCard.ZMotion.CustomUI.Models;
using ReeYin_V.Hardware.ControlCard.ZMotion.CustomUI.Views;
using System;
using System.Windows;
using System.Windows.Threading;

namespace ReeYin_V.Hardware.ControlCard.ZMotion.CustomUI.ViewModels
{
    public class ZMotionCustomViewModel : DialogViewModelBase
    {
        #region Fields
        private DispatcherTimer _timer;
        private IntPtr g_handle = IntPtr.Zero;
        private int nAxis = 0; // 当前操作的轴号
        private float _lastEncoderPosition;
        private DateTime _lastEncoderSpeedTime = DateTime.MinValue;
        private bool _hasLastEncoderPosition;
        #endregion

        #region Override
        public override void OnDialogClosed()
        {
            _timer?.Stop();
            if (g_handle != IntPtr.Zero)
            {
                Zmcaux.ZAux_Close(g_handle);
                g_handle = IntPtr.Zero;
            }
            base.OnDialogClosed();
        }
        #endregion

        #region Properties
        private ZMotionCustomModel _modelParam = new ZMotionCustomModel();
        /// <summary>
        /// 模块参数
        /// </summary>
        public ZMotionCustomModel ModelParam
        {
            get { return _modelParam; }
            set { SetProperty(ref _modelParam, value); }
        }
        #endregion

        #region Constructor
        public ZMotionCustomViewModel()
        {
            ModelParam = new ZMotionCustomModel();
            InitTimer();
        }
        #endregion

        #region Methods
        private void InitTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = new TimeSpan(0, 0, 0, 0, 100); // 100ms更新一次
            _timer.Tick += (s, e) =>
            {
                if (g_handle != IntPtr.Zero)
                {
                    UpdateAxisStatus();
                }
            };
        }

        private void UpdateAxisStatus()
        {
            try
            {
                int runstate = 0;
                float curpos = 0;
                float curspeed = 0;
                float encoderPosition = 0;

                Zmcaux.ZAux_Direct_GetIfIdle(g_handle, nAxis, ref runstate);
                Zmcaux.ZAux_Direct_GetDpos(g_handle, nAxis, ref curpos);
                Zmcaux.ZAux_Direct_GetVpSpeed(g_handle, nAxis, ref curspeed);
                int retEncoder = Zmcaux.ZAux_Direct_GetEncoder(g_handle, nAxis, ref encoderPosition);

                ModelParam.RunState = runstate == 0 ? "运行中" : "停止中";
                ModelParam.CurrentPosition = curpos;
                ModelParam.CurrentSpeed = curspeed;
                if (retEncoder == 0)
                {
                    ModelParam.EncoderSpeed = CalculateEncoderSpeed(encoderPosition);
                }
                UpdateIoStatus();
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"更新状态失败：{ex.Message}");
            }
        }

        private float CalculateEncoderSpeed(float encoderPosition)
        {
            DateTime now = DateTime.UtcNow;
            if (!_hasLastEncoderPosition || _lastEncoderSpeedTime == DateTime.MinValue)
            {
                _lastEncoderPosition = encoderPosition;
                _lastEncoderSpeedTime = now;
                _hasLastEncoderPosition = true;
                return 0;
            }

            double seconds = (now - _lastEncoderSpeedTime).TotalSeconds;
            if (seconds <= 0)
            {
                return ModelParam.EncoderSpeed;
            }

            float speed = (float)((encoderPosition - _lastEncoderPosition) / seconds);
            _lastEncoderPosition = encoderPosition;
            _lastEncoderSpeedTime = now;
            return speed;
        }

        private void ResetEncoderSpeedCache()
        {
            _lastEncoderPosition = 0;
            _lastEncoderSpeedTime = DateTime.MinValue;
            _hasLastEncoderPosition = false;
            ModelParam.EncoderSpeed = 0;
        }

        private void UpdateIoStatus()
        {
            foreach (var input in ModelParam.InputPoints)
            {
                if (TryReadIo(true, input.Port, out var state))
                {
                    input.State = state;
                }
            }

            foreach (var output in ModelParam.OutputPoints)
            {
                if (TryReadIo(false, output.Port, out var state))
                {
                    output.State = state;
                }
            }
        }

        private bool TryReadIo(bool input, int port, out bool state)
        {
            state = false;
            if (g_handle == IntPtr.Zero || port < 0 || port >= 32)
            {
                return false;
            }

            UInt32 value = 0;
            int ret = input
                ? Zmcaux.ZAux_Direct_GetIn(g_handle, port, ref value)
                : Zmcaux.ZAux_Direct_GetOp(g_handle, port, ref value);

            if (ret != 0)
            {
                return false;
            }

            state = value != 0;
            return true;
        }
        #endregion

        #region Commands
        /// <summary>
        /// 通用指令
        /// </summary>
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "连接":
                    ConnectController();
                    break;
                case "断开":
                    DisconnectController();
                    break;
                case "回零":
                    GoHome();
                    break;
                case "定位运动":
                    MoveToPosition();
                    break;
                case "停止":
                    StopMotion();
                    break;
                case "读取回零速度":
                    ReadHomeSpeed();
                    break;
                case "设置回零速度":
                    SetHomeSpeed();
                    break;
                case "读取运动参数":
                    ReadMoveParams();
                    break;
                case "设置运动参数":
                    SetMoveParams();
                    break;
                case "参数设置":
                    OpenSettingsWindow();
                    break;
                case "刷新IO":
                    UpdateIoStatus();
                    break;
                case "关闭":
                    _timer.Stop();
                    DisconnectController();
                    break;
                default:
                    break;
            }
        });

        public DelegateCommand<ZMotionIoPoint> ToggleOutputCommand => new DelegateCommand<ZMotionIoPoint>((point) =>
        {
            if (point == null)
            {
                return;
            }

            if (g_handle == IntPtr.Zero)
            {
                ModelParam.AddLog("控制器未连接，不能切换OUT");
                return;
            }

            if (point.Port < 0 || point.Port >= 32)
            {
                ModelParam.AddLog($"OUT点位越界：{point.Port}");
                return;
            }

            UInt32 value = point.State ? (UInt32)1 : 0;
            int ret = Zmcaux.ZAux_Direct_SetOp(g_handle, point.Port, value);
            if (ret == 0)
            {
                ModelParam.AddLog($"{point.Name}={(point.State ? "ON" : "OFF")}");
            }
            else
            {
                ModelParam.AddLog($"{point.Name}切换失败，错误码：{ret}");
                if (TryReadIo(false, point.Port, out var state))
                {
                    point.State = state;
                }
            }
        });

        /// <summary>
        /// 打开参数设置界面
        /// </summary>
        private void OpenSettingsWindow()
        {
            try
            {
                if (g_handle == IntPtr.Zero)
                {
                    ModelParam.AddLog("请先连接控制器");
                    return;
                }

                var settingsWindow = new MotionCardSettingsView(g_handle);
                settingsWindow.Owner = Application.Current.MainWindow;
                settingsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"打开设置界面失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 点动命令
        /// </summary>
        public DelegateCommand<string> JogCommand => new DelegateCommand<string>((direction) =>
        {
            try
            {
                byte[] pdata = new byte[1];
                
                if (direction == "Start_Left")
                {
                    pdata[0] = 1;
                    Zmcaux.ZAux_Modbus_Set0x(g_handle, 0, 1, pdata);
                }
                else if (direction == "Stop_Left")
                {
                    pdata[0] = 0;
                    Zmcaux.ZAux_Modbus_Set0x(g_handle, 0, 1, pdata);
                }
                else if (direction == "Start_Right")
                {
                    pdata[0] = 1;
                    Zmcaux.ZAux_Modbus_Set0x(g_handle, 1, 1, pdata);
                }
                else if (direction == "Stop_Right")
                {
                    pdata[0] = 0;
                    Zmcaux.ZAux_Modbus_Set0x(g_handle, 1, 1, pdata);
                }
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"点动失败：{ex.Message}");
            }
        });

        private void ConnectController()
        {
            try
            {
                int ret;
                if (ModelParam.ConnectionType == 0)
                {
                    ret = Zmcaux.ZAux_OpenCom(ModelParam.ComPort, out g_handle);
                }
                else
                {
                    ret = Zmcaux.ZAux_OpenEth(ModelParam.IpAddress, out g_handle);
                }

                if (ret == 0 && g_handle != IntPtr.Zero)
                {
                    ModelParam.AddLog("控制器连接成功！");
                    ResetEncoderSpeedCache();
                    UpdateIoStatus();
                    _timer.Start();
                }
                else
                {
                    ModelParam.AddLog($"控制器连接失败，错误码：{ret}");
                }
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"连接异常：{ex.Message}");
            }
        }

        private void DisconnectController()
        {
            try
            {
                if (g_handle != IntPtr.Zero)
                {
                    _timer.Stop();
                    Zmcaux.ZAux_Close(g_handle);
                    g_handle = IntPtr.Zero;
                    ResetEncoderSpeedCache();
                    ModelParam.AddLog("控制器已断开连接");
                }
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"断开连接失败：{ex.Message}");
            }
        }

        private void GoHome()
        {
            try
            {
                UInt16[] pdata = new UInt16[1];
                pdata[0] = 1; // 1表示回零命令
                int ret = Zmcaux.ZAux_Modbus_Set4x(g_handle, 50, 1, pdata);
                
                if (ret == 0)
                {
                    ModelParam.AddLog("回零命令已发送");
                }
                else
                {
                    ModelParam.AddLog($"回零命令发送失败，错误码：{ret}");
                }
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"回零失败：{ex.Message}");
            }
        }

        private void MoveToPosition()
        {
            try
            {
                UInt16[] pdata = new UInt16[1];
                pdata[0] = 2; // 2表示定位运动命令
                int ret = Zmcaux.ZAux_Modbus_Set4x(g_handle, 50, 1, pdata);
                
                if (ret == 0)
                {
                    ModelParam.AddLog($"定位运动命令已发送，目标位置：{ModelParam.TargetPosition}");
                }
                else
                {
                    ModelParam.AddLog($"定位运动命令发送失败，错误码：{ret}");
                }
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"定位运动失败：{ex.Message}");
            }
        }

        private void StopMotion()
        {
            try
            {
                int ret = Zmcaux.ZAux_Direct_Single_Cancel(g_handle, nAxis, 2);
                if (ret == 0)
                {
                    ModelParam.AddLog("停止命令已发送");
                }
                else
                {
                    ModelParam.AddLog($"停止命令发送失败，错误码：{ret}");
                }
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"停止失败：{ex.Message}");
            }
        }

        private void ReadHomeSpeed()
        {
            try
            {
                float[] pdata = new float[1];
                int ret = Zmcaux.ZAux_Modbus_Get4x_Float(g_handle, 10, 1, pdata);
                
                if (ret == 0)
                {
                    ModelParam.HomeSpeed = pdata[0];
                    ModelParam.AddLog($"回零速度：{pdata[0]}");
                }
                else
                {
                    ModelParam.AddLog($"读取回零速度失败，错误码：{ret}");
                }
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"读取回零速度异常：{ex.Message}");
            }
        }

        private void SetHomeSpeed()
        {
            try
            {
                float[] pdata = new float[1];
                pdata[0] = ModelParam.HomeSpeed;
                int ret = Zmcaux.ZAux_Modbus_Set4x_Float(g_handle, 10, 1, pdata);
                
                if (ret == 0)
                {
                    ModelParam.AddLog($"回零速度已设置为：{ModelParam.HomeSpeed}");
                }
                else
                {
                    ModelParam.AddLog($"设置回零速度失败，错误码：{ret}");
                }
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"设置回零速度异常：{ex.Message}");
            }
        }

        private void ReadMoveParams()
        {
            try
            {
                float[] pdata = new float[1];
                
                // 读取速度
                Zmcaux.ZAux_Modbus_Get4x_Float(g_handle, 0, 1, pdata);
                ModelParam.MoveSpeed = pdata[0];
                
                // 读取目标位置
                Zmcaux.ZAux_Modbus_Get4x_Float(g_handle, 8, 1, pdata);
                ModelParam.TargetPosition = pdata[0];
                
                ModelParam.AddLog($"运动参数已读取 - 速度:{ModelParam.MoveSpeed}, 位置:{ModelParam.TargetPosition}");
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"读取运动参数异常：{ex.Message}");
            }
        }

        private void SetMoveParams()
        {
            try
            {
                float[] pdata = new float[1];
                
                // 设置速度
                pdata[0] = ModelParam.MoveSpeed;
                Zmcaux.ZAux_Modbus_Set4x_Float(g_handle, 0, 1, pdata);
                
                // 设置目标位置
                pdata[0] = ModelParam.TargetPosition;
                Zmcaux.ZAux_Modbus_Set4x_Float(g_handle, 8, 1, pdata);
                
                ModelParam.AddLog($"运动参数已设置 - 速度:{ModelParam.MoveSpeed}, 位置:{ModelParam.TargetPosition}");
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"设置运动参数异常：{ex.Message}");
            }
        }
        #endregion
    }
}
