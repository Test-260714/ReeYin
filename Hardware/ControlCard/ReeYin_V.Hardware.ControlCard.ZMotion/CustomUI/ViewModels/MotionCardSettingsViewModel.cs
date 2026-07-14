using Prism.Commands;
using Prism.Mvvm;
using ReeYin_V.Hardware.ControlCard.ZMotion.App;
using ReeYin_V.Hardware.ControlCard.ZMotion.Api;
using ReeYin_V.Hardware.ControlCard.ZMotion.CustomUI.Models;
using System;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace ReeYin_V.Hardware.ControlCard.ZMotion.CustomUI.ViewModels
{
    /// <summary>
    /// 运动控制卡设置ViewModel
    /// </summary>
    public class MotionCardSettingsViewModel : BindableBase
    {
        private enum EncoderCountSource
        {
            None,
            Soft,
            Enc,
            Mpos
        }

        private const int IoPointCount = 32;
        private const double IoRefreshIntervalMs = 300;

        #region Fields
        private IntPtr _handle = IntPtr.Zero;
        private bool _ownsHandle = false;
        private ZMotionControlCard? _controlCard;
        private DispatcherTimer _timer;
        private DateTime _lastEncoderLogTime = DateTime.MinValue;
        private DateTime _lastEncoderDiagTime = DateTime.MinValue;
        private int _lastEncA = -1;
        private int _lastEncB = -1;
        private int _lastEncZ = -1;
        private int _lastAB = -1;
        private long _softEncoderCount = 0;
        private DateTime _lastEncoderSpeedTime = DateTime.MinValue;
        private DateTime _speedWindowStartTime = DateTime.MinValue;
        private double _speedWindowStartPos = double.NaN;
        private double _smoothedEncoderSpeed = 0;
        // 中速场景：平衡稳定与响应
        private const double EncoderSpeedSmoothAlpha = 0.35;
        private const double EncoderSpeedWindowSeconds = 0.25;
        private const double EncoderSpeedResetSeconds = 0.6;
        private const double EncoderSpeedDeadbandCounts = 0.25;
        private float _lastHardwareEncPos = float.NaN;
        private float _lastHardwareMpos = float.NaN;
        private int _hardwareStableTicks = 0;
        private double _lastRawCount = double.NaN;
        private double _continuousCount = 0;
        private EncoderCountSource _lastCountSource = EncoderCountSource.None;
        private double _lastDelta1 = 0;
        private double _lastDelta2 = 0;
        private int _deltaHistoryCount = 0;
        private bool _usingSoftEncoder = false;
        private bool _isConnected = false;
        private string _ipAddress = "192.168.0.11";
        private int _connectionType = 1;
        private uint _comPort = 1;
        private DateTime _lastIoRefreshTime = DateTime.MinValue;
        private string _ioStatusText = "未连接";

        private double _encoderPosition;
        private double _encoderSpeed;
        private int _encoderPpr = 2000;
        private int _encoderMultiply = 4;
        private double _encoderPulsesPerMm = 0;
        private double _pixelSizeMm = 0;
        private int _encoderAxisIndex = 4;
        private double _lineRate;
        private int _encoderSignalType = 1;
        #endregion

        #region Properties
        public bool IsConnected
        {
            get { return _isConnected; }
            set { SetProperty(ref _isConnected, value); }
        }

        /// <summary>
        /// 连接方式：0-串口 1-以太网
        /// </summary>
        public int ConnectionType
        {
            get { return _connectionType; }
            set { SetProperty(ref _connectionType, value); }
        }

        public string IpAddress
        {
            get { return _ipAddress; }
            set { SetProperty(ref _ipAddress, value); }
        }

        public uint ComPort
        {
            get { return _comPort; }
            set { SetProperty(ref _comPort, value); }
        }

        public double EncoderPosition
        {
            get { return _encoderPosition; }
            set { SetProperty(ref _encoderPosition, value); }
        }

        public double EncoderSpeed
        {
            get { return _encoderSpeed; }
            set
            {
                if (SetProperty(ref _encoderSpeed, value))
                {
                    UpdateLineRate();
                }
            }
        }

        public int EncoderPpr
        {
            get { return _encoderPpr; }
            set
            {
                if (SetProperty(ref _encoderPpr, value))
                {
                    RaisePropertyChanged(nameof(EncoderCountsPerRev));
                }
            }
        }

        public int EncoderMultiply
        {
            get { return _encoderMultiply; }
            set
            {
                if (SetProperty(ref _encoderMultiply, value))
                {
                    RaisePropertyChanged(nameof(EncoderCountsPerRev));
                }
            }
        }

        public int EncoderCountsPerRev => EncoderPpr * EncoderMultiply;

        /// <summary>
        /// 编码器脉冲/毫米
        /// </summary>
        public double EncoderPulsesPerMm
        {
            get { return _encoderPulsesPerMm; }
            set
            {
                if (SetProperty(ref _encoderPulsesPerMm, value))
                {
                    UpdateLineRate();
                }
            }
        }

        /// <summary>
        /// 像素尺寸（mm/px）
        /// </summary>
        public double PixelSizeMm
        {
            get { return _pixelSizeMm; }
            set
            {
                if (SetProperty(ref _pixelSizeMm, value))
                {
                    UpdateLineRate();
                }
            }
        }

        public int EncoderAxisIndex
        {
            get { return _encoderAxisIndex; }
            set { SetProperty(ref _encoderAxisIndex, value); }
        }

        public double LineRate
        {
            get { return _lineRate; }
            set { SetProperty(ref _lineRate, value); }
        }

        /// <summary>
        /// 编码器输入类型：0-单端 1-差分
        /// </summary>
        public int EncoderSignalType
        {
            get { return _encoderSignalType; }
            set { SetProperty(ref _encoderSignalType, value); }
        }

        private MotionCardSettingsModel _settings = new MotionCardSettingsModel();
        public MotionCardSettingsModel Settings
        {
            get { return _settings; }
            set { SetProperty(ref _settings, value); }
        }

        private ObservableCollection<string> _logMessages = new ObservableCollection<string>();
        public ObservableCollection<string> LogMessages
        {
            get { return _logMessages; }
            set { SetProperty(ref _logMessages, value); }
        }

        public string LogText => string.Join(Environment.NewLine, LogMessages);

        public ObservableCollection<ZMotionIoPoint> InputPoints { get; } = new ObservableCollection<ZMotionIoPoint>();

        public ObservableCollection<ZMotionIoPoint> OutputPoints { get; } = new ObservableCollection<ZMotionIoPoint>();

        public string IoStatusText
        {
            get { return _ioStatusText; }
            set { SetProperty(ref _ioStatusText, value); }
        }
        #endregion

        #region Constructor
        public MotionCardSettingsViewModel()
        {
            InitIoPoints();
            InitTimer();
        }

        public void SetHandle(IntPtr handle)
        {
            _handle = handle;
            _ownsHandle = false;
            if (_handle != IntPtr.Zero)
            {
                IsConnected = true;
                _timer.Start();
                ReadAllParams();
                RefreshIoStatus(false);
            }
            else
            {
                IsConnected = false;
                IoStatusText = "未连接";
            }
        }
        #endregion

        #region Methods
        private void InitTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(100);
            _timer.Tick += (s, e) => UpdateStatus();
        }

        private void UpdateStatus()
        {
            if (_handle == IntPtr.Zero) return;
            try
            {
                float[] fdata = new float[1];
                UInt16[] idata = new UInt16[1];

                Zmcaux.ZAux_Modbus_Get4x_Float(_handle, 32, 1, fdata);
                Settings.CurrentPosition = fdata[0];

                Zmcaux.ZAux_Modbus_Get4x(_handle, 36, 1, idata);
                Settings.WorkState = idata[0];

                Zmcaux.ZAux_Modbus_Get4x(_handle, 35, 1, idata);
                Settings.CurrentCycles = idata[0];

                int a, b, z;
                bool inputsChanged;
                ReadEncoderInputSignals(out a, out b, out z, out inputsChanged);
                UpdateSoftEncoderCount(a, b);

                float encPos = 0;
                int retEnc = Zmcaux.ZAux_Direct_GetEncoder(_handle, EncoderAxisIndex, ref encPos);

                float mpos = 0;
                int retMpos = Zmcaux.ZAux_Direct_GetMpos(_handle, EncoderAxisIndex, ref mpos);

                int atype = -1;
                int retAtype = Zmcaux.ZAux_Direct_GetAtype(_handle, EncoderAxisIndex, ref atype);

                bool encChanged = !float.IsNaN(_lastHardwareEncPos) && Math.Abs(encPos - _lastHardwareEncPos) >= 0.0001f;
                bool mposChanged = !float.IsNaN(_lastHardwareMpos) && Math.Abs(mpos - _lastHardwareMpos) >= 0.0001f;

                if (!float.IsNaN(_lastHardwareEncPos) && !float.IsNaN(_lastHardwareMpos) && !encChanged && !mposChanged)
                    _hardwareStableTicks++;
                else
                    _hardwareStableTicks = 0;

                _lastHardwareEncPos = encPos;
                _lastHardwareMpos = mpos;

                if (inputsChanged && (DateTime.Now - _lastEncoderDiagTime).TotalMilliseconds >= 500)
                {
                    _lastEncoderDiagTime = DateTime.Now;
                    AddLog($"编码器状态 ATYPE={atype} (ret={retAtype}), ENC={encPos} (ret={retEnc}), MPOS={mpos} (ret={retMpos})");
                }

                if ((encChanged || mposChanged) && _usingSoftEncoder)
                {
                    _usingSoftEncoder = false;
                }

                if (inputsChanged && _hardwareStableTicks >= 3)
                {
                    if (!_usingSoftEncoder)
                        AddLog("硬件编码器无变化，使用软件计数");
                    _usingSoftEncoder = true;
                }

                double rawCount;
                EncoderCountSource countSource;
                if (_usingSoftEncoder)
                {
                    rawCount = _softEncoderCount;
                    countSource = EncoderCountSource.Soft;
                }
                else if (retEnc == 0 && encChanged)
                {
                    rawCount = encPos;
                    countSource = EncoderCountSource.Enc;
                }
                else if (retMpos == 0)
                {
                    rawCount = mpos;
                    countSource = EncoderCountSource.Mpos;
                }
                else
                {
                    rawCount = encPos;
                    countSource = EncoderCountSource.Enc;
                }

                double continuousCount = UpdateContinuousCount(rawCount, countSource);
                double posDisplay = continuousCount;
                double speedDeadband = 0;
                if (EncoderPulsesPerMm > 0)
                {
                    posDisplay = continuousCount / EncoderPulsesPerMm;
                    speedDeadband = (EncoderSpeedDeadbandCounts / EncoderPulsesPerMm) / EncoderSpeedWindowSeconds;
                }

                EncoderPosition = posDisplay;
                EncoderSpeed = CalcEncoderSpeed(posDisplay, speedDeadband);

                if ((DateTime.Now - _lastIoRefreshTime).TotalMilliseconds >= IoRefreshIntervalMs)
                {
                    RefreshIoStatus(false);
                }

                RaisePropertyChanged(nameof(Settings));
            }
            catch { }
        }

        private void InitIoPoints()
        {
            if (InputPoints.Count > 0 || OutputPoints.Count > 0)
                return;

            for (int i = 0; i < IoPointCount; i++)
            {
                InputPoints.Add(new ZMotionIoPoint { Port = i, Name = $"IN{i}" });
                OutputPoints.Add(new ZMotionIoPoint { Port = i, Name = $"OUT{i}" });
            }
        }

        private void RefreshIoStatus(bool writeLog)
        {
            _lastIoRefreshTime = DateTime.Now;

            if (_handle == IntPtr.Zero)
            {
                IoStatusText = "未连接";
                return;
            }

            var failedCount = 0;
            foreach (var input in InputPoints)
            {
                if (TryReadIo(true, input.Port, out var state))
                    input.State = state;
                else
                    failedCount++;
            }

            foreach (var output in OutputPoints)
            {
                if (TryReadIo(false, output.Port, out var state))
                    output.State = state;
                else
                    failedCount++;
            }

            IoStatusText = failedCount == 0
                ? $"已刷新 {IoPointCount} 路输入 / {IoPointCount} 路输出"
                : $"IO刷新完成，失败 {failedCount} 点";

            if (writeLog)
                AddLog(IoStatusText);
        }

        private bool TryReadIo(bool input, int port, out bool state)
        {
            state = false;
            if (_handle == IntPtr.Zero || !IsValidIoPort(port))
                return false;

            UInt32 value = 0;
            int ret = input
                ? Zmcaux.ZAux_Direct_GetIn(_handle, port, ref value)
                : Zmcaux.ZAux_Direct_GetOp(_handle, port, ref value);

            if (ret != 0)
                return false;

            state = value != 0;
            return true;
        }

        private static bool IsValidIoPort(int port) => port >= 0 && port < IoPointCount;

        private void ToggleOutput(ZMotionIoPoint? point)
        {
            if (point == null)
                return;

            if (_handle == IntPtr.Zero)
            {
                AddLog("控制卡未连接，不能切换OUT");
                point.State = false;
                return;
            }

            if (!IsValidIoPort(point.Port))
            {
                AddLog($"OUT点位越界：OUT{point.Port}");
                return;
            }

            UInt32 value = point.State ? (UInt32)1 : 0;
            int ret = Zmcaux.ZAux_Direct_SetOp(_handle, point.Port, value);
            if (ret == 0)
            {
                AddLog($"{point.Name}={(point.State ? "ON" : "OFF")}");
                RefreshIoStatus(false);
            }
            else
            {
                AddLog($"{point.Name}切换失败，错误码：{ret}");
                if (TryReadIo(false, point.Port, out var state))
                    point.State = state;
            }
        }

        private void ReadEncoderInputSignals(out int a, out int b, out int z, out bool changed)
        {
            int ez = EncoderAxisIndex == 1 ? 10 : 13;
            int eb = EncoderAxisIndex == 1 ? 11 : 14;
            int ea = EncoderAxisIndex == 1 ? 12 : 15;

            UInt32 va = 0, vb = 0, vz = 0;
            Zmcaux.ZAux_Direct_GetIn(_handle, ea, ref va);
            Zmcaux.ZAux_Direct_GetIn(_handle, eb, ref vb);
            Zmcaux.ZAux_Direct_GetIn(_handle, ez, ref vz);

            a = (int)(va & 1);
            b = (int)(vb & 1);
            z = (int)(vz & 1);

            changed = a != _lastEncA || b != _lastEncB || z != _lastEncZ;
            if (changed && (DateTime.Now - _lastEncoderLogTime).TotalMilliseconds >= 200)
            {
                _lastEncoderLogTime = DateTime.Now;
                AddLog($"编码器输入 A/B/Z={a}/{b}/{z} (IN{ea}/IN{eb}/IN{ez})");
            }
            _lastEncA = a; _lastEncB = b; _lastEncZ = z;
        }

        private void UpdateSoftEncoderCount(int a, int b)
        {
            int curr = (a << 1) | b;
            if (_lastAB >= 0)
            {
                int key = (_lastAB << 2) | curr;
                switch (key)
                {
                    case 0b0001:
                    case 0b0111:
                    case 0b1110:
                    case 0b1000:
                        _softEncoderCount++;
                        break;
                    case 0b0010:
                    case 0b0100:
                    case 0b1101:
                    case 0b1011:
                        _softEncoderCount--;
                        break;
                }
            }
            _lastAB = curr;
        }

        private double CalcEncoderSpeed(double pos, double deadband)
        {
            var now = DateTime.Now;
            if (_lastEncoderSpeedTime == DateTime.MinValue || double.IsNaN(_speedWindowStartPos))
            {
                _lastEncoderSpeedTime = now;
                _speedWindowStartTime = now;
                _speedWindowStartPos = pos;
                _smoothedEncoderSpeed = 0;
                return 0;
            }

            var dtSinceLast = (now - _lastEncoderSpeedTime).TotalSeconds;
            if (dtSinceLast <= 0) return _smoothedEncoderSpeed;

            // 间隔过长时重置，避免跳变
            if (dtSinceLast > EncoderSpeedResetSeconds)
            {
                _lastEncoderSpeedTime = now;
                _speedWindowStartTime = now;
                _speedWindowStartPos = pos;
                _smoothedEncoderSpeed = 0;
                return 0;
            }

            var windowDt = (now - _speedWindowStartTime).TotalSeconds;
            if (windowDt < EncoderSpeedWindowSeconds)
            {
                _lastEncoderSpeedTime = now;
                return _smoothedEncoderSpeed;
            }

            var delta = pos - _speedWindowStartPos;
            _speedWindowStartPos = pos;
            _speedWindowStartTime = now;
            _lastEncoderSpeedTime = now;

            var rawSpeed = delta / windowDt;
            if (Math.Abs(rawSpeed) < deadband) rawSpeed = 0;

            _smoothedEncoderSpeed = (_smoothedEncoderSpeed * (1 - EncoderSpeedSmoothAlpha)) +
                                    (rawSpeed * EncoderSpeedSmoothAlpha);
            return _smoothedEncoderSpeed;
        }

        private double UpdateContinuousCount(double rawCount, EncoderCountSource source)
        {
            if (source != _lastCountSource)
            {
                _lastCountSource = source;
                _lastRawCount = rawCount;
                _continuousCount = rawCount;
                return _continuousCount;
            }

            if (double.IsNaN(_lastRawCount))
            {
                _lastRawCount = rawCount;
                _continuousCount = rawCount;
                return _continuousCount;
            }

            const double wrapRange32 = 4294967296.0;
            const double wrapHalf32 = wrapRange32 / 2.0;
            const double wrapRange16 = 65536.0;
            const double wrapHalf16 = wrapRange16 / 2.0;
            const double wrapDetectThreshold = 50000.0;
            var delta = rawCount - _lastRawCount;
            if (Math.Abs(delta) > wrapDetectThreshold)
            {
                if (delta > wrapHalf16) delta -= wrapRange16;
                else if (delta < -wrapHalf16) delta += wrapRange16;
            }
            else
            {
                if (delta > wrapHalf32) delta -= wrapRange32;
                else if (delta < -wrapHalf32) delta += wrapRange32;
            }

            double filteredDelta = delta;
            if (_deltaHistoryCount >= 2)
            {
                filteredDelta = Median(delta, _lastDelta1, _lastDelta2);
            }

            _lastDelta2 = _lastDelta1;
            _lastDelta1 = delta;
            if (_deltaHistoryCount < 2) _deltaHistoryCount++;

            _continuousCount += filteredDelta;
            _lastRawCount = rawCount;
            return _continuousCount;
        }

        private static double Median(double a, double b, double c)
        {
            if (a > b)
            {
                if (b > c) return b;
                return a > c ? c : a;
            }
            else
            {
                if (a > c) return a;
                return b > c ? c : b;
            }
        }

        private void UpdateLineRate()
        {
            if (EncoderPulsesPerMm > 0 && PixelSizeMm > 0)
            {
                LineRate = EncoderSpeed / PixelSizeMm;
            }
            else
            {
                LineRate = 0;
            }
        }

        private void ResetEncoderPosition()
        {
            if (_handle == IntPtr.Zero) { AddLog("控制卡未连接"); return; }
            try
            {
                int ret = Zmcaux.ZAux_Direct_SetMpos(_handle, EncoderAxisIndex, 0);
                if (ret == 0)
                {
                    EncoderPosition = 0;
                    _lastEncoderSpeedTime = DateTime.MinValue;
                    _speedWindowStartTime = DateTime.MinValue;
                    _speedWindowStartPos = double.NaN;
                    _smoothedEncoderSpeed = 0;
                    _lastRawCount = double.NaN;
                    _continuousCount = 0;
                    _lastCountSource = EncoderCountSource.None;
                    _lastDelta1 = 0;
                    _lastDelta2 = 0;
                    _deltaHistoryCount = 0;
                    AddLog("编码器位置已清零");
                }
                else
                {
                    AddLog($"编码器清零失败，错误码：{ret}");
                }
            }
            catch (Exception ex)
            {
                AddLog($"编码器清零异常：{ex.Message}");
            }
        }

        private void ApplyEncoderMapping()
        {
            if (_handle == IntPtr.Zero) { AddLog("控制卡未连接"); return; }
            try
            {
                const int fixedAtype = 3;
                int atype = -1;
                int retSetAtype = Zmcaux.ZAux_Direct_SetAtype(_handle, EncoderAxisIndex, fixedAtype);
                int retGetAtype = Zmcaux.ZAux_Direct_GetAtype(_handle, EncoderAxisIndex, ref atype);

                int retRatio = Zmcaux.ZAux_Direct_EncoderRatio(_handle, EncoderAxisIndex, 1, 1);
                float encPos = 0;
                int retEnc = Zmcaux.ZAux_Direct_GetEncoder(_handle, EncoderAxisIndex, ref encPos);
                AddLog($"应用编码器映射：SetAtype={retSetAtype}, GetAtype={retGetAtype}/{atype}, EncoderRatioRet={retRatio}, GetEncoder={retEnc}/{encPos}");
            }
            catch (Exception ex)
            {
                AddLog($"应用编码器映射异常：{ex.Message}");
            }
        }

        private void AddLog(string msg)
        {
            LogMessages.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {msg}");
            if (LogMessages.Count > 50) LogMessages.RemoveAt(LogMessages.Count - 1);
            RaisePropertyChanged(nameof(LogText));
        }

        private void Connect()
        {
            if (_handle != IntPtr.Zero)
            {
                AddLog("控制卡已连接");
                return;
            }

            if (_controlCard != null)
            {
                try
                {
                    _controlCard.ConnectionType = ConnectionType;
                    _controlCard.IpAddress = IpAddress;
                    _controlCard.ComPort = ComPort;

                    var connected = _controlCard.Init();
                    SetHandle(_controlCard.Handle);
                    IsConnected = connected && _controlCard.Handle != IntPtr.Zero;
                    AddLog(IsConnected
                        ? "Main control card connected."
                        : "Main control card connection failed.");
                    return;
                }
                catch (Exception ex)
                {
                    AddLog($"Main control card connection exception: {ex.Message}");
                    return;
                }
            }

            try
            {
                int ret;
                if (ConnectionType == 0)
                {
                    ret = Zmcaux.ZAux_OpenCom(ComPort, out _handle);
                }
                else
                {
                    ret = Zmcaux.ZAux_OpenEth(IpAddress, out _handle);
                }

                if (ret == 0 && _handle != IntPtr.Zero)
                {
                    _ownsHandle = true;
                    IsConnected = true;
                    _timer.Start();
                    ReadAllParams();
                    AddLog("控制卡连接成功");
                }
                else
                {
                    AddLog($"控制卡连接失败，错误码：{ret}");
                }
            }
            catch (Exception ex)
            {
                AddLog($"连接异常：{ex.Message}");
            }
        }

        private void Disconnect()
        {
            try
            {
                if (_controlCard != null)
                {
                    _timer.Stop();
                    _controlCard.Close();
                    _handle = IntPtr.Zero;
                    _ownsHandle = false;
                    IsConnected = false;
                    AddLog("Main control card disconnected.");
                    return;
                }

                if (_handle != IntPtr.Zero)
                {
                    if (!_ownsHandle)
                    {
                        AddLog("当前连接来自主控制卡，参数页不关闭主连接");
                        return;
                    }

                    _timer.Stop();
                    Zmcaux.ZAux_Close(_handle);
                    _handle = IntPtr.Zero;
                    _ownsHandle = false;
                    IsConnected = false;
                    AddLog("控制卡已断开连接");
                }
            }
            catch (Exception ex)
            {
                AddLog($"断开失败：{ex.Message}");
            }
        }

        public void ReadAllParams()
        {
            if (_handle == IntPtr.Zero) { AddLog("控制卡未连接"); return; }
            try
            {
                float[] fdata = new float[1];
                int[] ldata = new int[1];
                UInt16[] idata = new UInt16[1];

                // 定位参数
                Zmcaux.ZAux_Modbus_Get4x_Float(_handle, 0, 1, fdata); Settings.PositionSpeed = fdata[0];
                Zmcaux.ZAux_Modbus_Get4x_Float(_handle, 2, 1, fdata); Settings.PositionAccel = fdata[0];
                Zmcaux.ZAux_Modbus_Get4x_Float(_handle, 4, 1, fdata); Settings.PositionDecel = fdata[0];
                Zmcaux.ZAux_Modbus_Get4x_Float(_handle, 6, 1, fdata); Settings.StartSpeed = fdata[0];
                Zmcaux.ZAux_Modbus_Get4x_Float(_handle, 8, 1, fdata); Settings.PositionTarget = fdata[0];
                Zmcaux.ZAux_Modbus_Get4x_Float(_handle, 38, 1, fdata); Settings.PositionTrigger = fdata[0];
                Zmcaux.ZAux_Modbus_Get4x_Float(_handle, 42, 1, fdata); Settings.PositionLength = fdata[0];

                // 回原参数
                Zmcaux.ZAux_Modbus_Get4x_Float(_handle, 10, 1, fdata); Settings.HomeFastSpeed = fdata[0];
                Zmcaux.ZAux_Modbus_Get4x_Float(_handle, 12, 1, fdata); Settings.HomeSlowSpeed = fdata[0];

                // 往复参数
                Zmcaux.ZAux_Modbus_Get4x_Float(_handle, 26, 1, fdata); Settings.ReciprocateSpeed = fdata[0];
                Zmcaux.ZAux_Modbus_Get4x_Float(_handle, 22, 1, fdata); Settings.ReciprocateAccel = fdata[0];
                Zmcaux.ZAux_Modbus_Get4x_Float(_handle, 24, 1, fdata); Settings.ReciprocateDecel = fdata[0];
                Zmcaux.ZAux_Modbus_Get4x_Float(_handle, 28, 1, fdata); Settings.ReciprocateNegPos = fdata[0];
                Zmcaux.ZAux_Modbus_Get4x_Float(_handle, 30, 1, fdata); Settings.ReciprocatePosPos = fdata[0];

                // 触发位置
                Zmcaux.ZAux_Modbus_Get4x_Float(_handle, 18, 1, fdata); Settings.ForwardTrigger1 = fdata[0];
                Zmcaux.ZAux_Modbus_Get4x_Float(_handle, 14, 1, fdata); Settings.ForwardTrigger2 = fdata[0];
                Zmcaux.ZAux_Modbus_Get4x_Float(_handle, 40, 1, fdata); Settings.ReturnTrigger = fdata[0];

                // 循环与延时
                Zmcaux.ZAux_Modbus_Get4x(_handle, 34, 1, idata); Settings.ReciprocateCycles = idata[0];
                Zmcaux.ZAux_Modbus_Get4x_Long(_handle, 44, 1, ldata); Settings.ReciprocateDelay = ldata[0];

                AddLog("参数读取完成");
            }
            catch (Exception ex) { AddLog($"读取参数失败：{ex.Message}"); }
        }

        public void SaveAllParams()
        {
            if (_handle == IntPtr.Zero) { AddLog("控制卡未连接"); return; }
            try
            {
                float[] fdata = new float[1];
                int[] ldata = new int[1];
                UInt16[] idata = new UInt16[1];

                // 定位参数
                fdata[0] = Settings.PositionSpeed; Zmcaux.ZAux_Modbus_Set4x_Float(_handle, 0, 1, fdata);
                fdata[0] = Settings.PositionAccel; Zmcaux.ZAux_Modbus_Set4x_Float(_handle, 2, 1, fdata);
                fdata[0] = Settings.PositionDecel; Zmcaux.ZAux_Modbus_Set4x_Float(_handle, 4, 1, fdata);
                fdata[0] = Settings.StartSpeed; Zmcaux.ZAux_Modbus_Set4x_Float(_handle, 6, 1, fdata);
                fdata[0] = Settings.PositionTarget; Zmcaux.ZAux_Modbus_Set4x_Float(_handle, 8, 1, fdata);
                fdata[0] = Settings.PositionTrigger; Zmcaux.ZAux_Modbus_Set4x_Float(_handle, 38, 1, fdata);
                fdata[0] = Settings.PositionLength; Zmcaux.ZAux_Modbus_Set4x_Float(_handle, 42, 1, fdata);

                // 回原参数
                fdata[0] = Settings.HomeFastSpeed; Zmcaux.ZAux_Modbus_Set4x_Float(_handle, 10, 1, fdata);
                fdata[0] = Settings.HomeSlowSpeed; Zmcaux.ZAux_Modbus_Set4x_Float(_handle, 12, 1, fdata);

                // 往复参数
                fdata[0] = Settings.ReciprocateSpeed; Zmcaux.ZAux_Modbus_Set4x_Float(_handle, 26, 1, fdata);
                fdata[0] = Settings.ReciprocateAccel; Zmcaux.ZAux_Modbus_Set4x_Float(_handle, 22, 1, fdata);
                fdata[0] = Settings.ReciprocateDecel; Zmcaux.ZAux_Modbus_Set4x_Float(_handle, 24, 1, fdata);
                fdata[0] = Settings.ReciprocateNegPos; Zmcaux.ZAux_Modbus_Set4x_Float(_handle, 28, 1, fdata);
                fdata[0] = Settings.ReciprocatePosPos; Zmcaux.ZAux_Modbus_Set4x_Float(_handle, 30, 1, fdata);

                // 触发位置
                fdata[0] = Settings.ForwardTrigger1; Zmcaux.ZAux_Modbus_Set4x_Float(_handle, 18, 1, fdata);
                fdata[0] = Settings.ForwardTrigger2; Zmcaux.ZAux_Modbus_Set4x_Float(_handle, 14, 1, fdata);
                fdata[0] = Settings.ReturnTrigger; Zmcaux.ZAux_Modbus_Set4x_Float(_handle, 40, 1, fdata);

                // 循环与延时
                idata[0] = (UInt16)Settings.ReciprocateCycles; Zmcaux.ZAux_Modbus_Set4x(_handle, 34, 1, idata);
                ldata[0] = Settings.ReciprocateDelay; Zmcaux.ZAux_Modbus_Set4x_Long(_handle, 44, 1, ldata);

                AddLog("参数已写入控制卡");
            }
            catch (Exception ex) { AddLog($"保存参数失败：{ex.Message}"); }
        }

        public void SaveToFlash()
        {
            if (_handle == IntPtr.Zero) { AddLog("控制卡未连接"); return; }
            try
            {
                UInt16[] cmd = new UInt16[1];
                cmd[0] = 6;
                Zmcaux.ZAux_Modbus_Set4x(_handle, 50, 1, cmd);
                AddLog("参数已保存到控制器Flash");
            }
            catch (Exception ex) { AddLog($"保存到Flash失败：{ex.Message}"); }
        }

        private void SendCommand(int cmd, string cmdName)
        {
            if (_handle == IntPtr.Zero) { AddLog("控制卡未连接"); return; }
            try
            {
                UInt16[] data = new UInt16[1];
                data[0] = (UInt16)cmd;
                Zmcaux.ZAux_Modbus_Set4x(_handle, 50, 1, data);
                AddLog($"{cmdName}命令已发送");
            }
            catch (Exception ex) { AddLog($"{cmdName}失败：{ex.Message}"); }
        }

        public void StopTimer() => _timer?.Stop();

        public void ReleaseOwnedConnection()
        {
            _timer?.Stop();
            if (_ownsHandle && _handle != IntPtr.Zero)
            {
                Zmcaux.ZAux_Close(_handle);
                _handle = IntPtr.Zero;
                _ownsHandle = false;
                IsConnected = false;
            }
        }

        public void SetControlCard(ZMotionControlCard controlCard)
        {
            _controlCard = controlCard;
            ConnectionType = controlCard.ConnectionType;
            IpAddress = controlCard.IpAddress;
            ComPort = controlCard.ComPort;
            SetHandle(controlCard.Handle);
            IsConnected = controlCard.IsConnected && controlCard.Handle != IntPtr.Zero;
        }
        #endregion

        #region Commands
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "刷新IO": RefreshIoStatus(true); break;
                case "连接": Connect(); break;
                case "断开": Disconnect(); break;
                case "读取参数": ReadAllParams(); break;
                case "保存参数": SaveAllParams(); break;
                case "保存到Flash": SaveToFlash(); break;
                case "清零编码器": ResetEncoderPosition(); break;
                case "应用编码器": ApplyEncoderMapping(); break;
                case "回原": SendCommand(1, "回原"); break;
                case "定位": SendCommand(2, "定位"); break;
                case "开始往复": SendCommand(3, "开始往复"); break;
                case "暂停往复": SendCommand(4, "暂停往复"); break;
                case "急停": SendCommand(5, "急停"); break;
                case "清空循环": SendCommand(7, "清空循环次数"); break;
                case "正等距": SendCommand(20, "正等距运动"); break;
                case "负等距": SendCommand(21, "负等距运动"); break;
            }
        });

        public DelegateCommand<ZMotionIoPoint> ToggleOutputCommand => new DelegateCommand<ZMotionIoPoint>(ToggleOutput);

        public DelegateCommand<string> JogCommand => new DelegateCommand<string>((dir) =>
        {
            if (_handle == IntPtr.Zero) return;
            try
            {
                byte[] pdata = new byte[1];
                if (dir == "Start_Left") { pdata[0] = 1; Zmcaux.ZAux_Modbus_Set0x(_handle, 0, 1, pdata); }
                else if (dir == "Stop_Left") { pdata[0] = 0; Zmcaux.ZAux_Modbus_Set0x(_handle, 0, 1, pdata); }
                else if (dir == "Start_Right") { pdata[0] = 1; Zmcaux.ZAux_Modbus_Set0x(_handle, 1, 1, pdata); }
                else if (dir == "Stop_Right") { pdata[0] = 0; Zmcaux.ZAux_Modbus_Set0x(_handle, 1, 1, pdata); }
            }
            catch { }
        });
        #endregion
    }
}
