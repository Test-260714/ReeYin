using Newtonsoft.Json;
using ReeYin_V.Hardware.PLC.Services;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace ReeYin_V.Hardware.PLC.Models
{
    /// <summary>
    /// PLC轴组运动调试模型
    /// </summary>
    public class PLCAxisGroupMoveModel : BindableBase
    {
        private bool _isUpdatingAxisParameters;

        [JsonIgnore]
        private PLCBase? _plcDevice;
        /// <summary>
        /// 当前PLC设备
        /// </summary>
        [JsonIgnore]
        public PLCBase? PlcDevice
        {
            get { return _plcDevice; }
            set
            {
                if (_plcDevice?.Config != null)
                {
                    _plcDevice.Config.PropertyChanged -= OnPlcConfigPropertyChanged;
                }

                _plcDevice = value;
                if (_plcDevice?.Config != null)
                {
                    _plcDevice.Config.PropertyChanged += OnPlcConfigPropertyChanged;
                }

                RaisePropertyChanged();
                RefreshConnectionStatus();
                AutoReadSelectedAxisParameters();
            }
        }

        [JsonIgnore]
        private PLCAxisGroup? _axisGroup;
        /// <summary>
        /// 当前操作的轴组
        /// </summary>
        [JsonIgnore]
        public PLCAxisGroup? AxisGroup
        {
            get { return _axisGroup; }
            set
            {
                if (ReferenceEquals(_axisGroup, value))
                {
                    return;
                }

                UnsubscribeAxisGroup(_axisGroup);
                _axisGroup = value;
                SubscribeAxisGroup(_axisGroup);
                RaisePropertyChanged();
                RaiseAxisRuntimeProperties();
                EnsureSelectedAxis();
            }
        }

        private double _jogSpeed = 10;
        /// <summary>
        /// 点动速度
        /// </summary>
        public double JogSpeed
        {
            get { return _jogSpeed; }
            set { _jogSpeed = value; RaisePropertyChanged(); }
        }

        private double _jogStep = 1;
        /// <summary>
        /// 点动量
        /// </summary>
        public double JogStep
        {
            get { return _jogStep; }
            set { _jogStep = value; RaisePropertyChanged(); }
        }

        private double _moveSpeed = 10;
        /// <summary>
        /// 定位速度
        /// </summary>
        public double MoveSpeed
        {
            get { return _moveSpeed; }
            set { _moveSpeed = value; RaisePropertyChanged(); }
        }

        private double _moveAcc = 10;
        /// <summary>
        /// 定位加速度
        /// </summary>
        public double MoveAcc
        {
            get { return _moveAcc; }
            set { _moveAcc = value; RaisePropertyChanged(); }
        }

        private double _moveDec = 10;
        /// <summary>
        /// 定位减速度（无速度档时的备用值）
        /// </summary>
        public double MoveDec
        {
            get { return _moveDec; }
            set { _moveDec = value; RaisePropertyChanged(); }
        }

        private PLCSpeedProfile? _selectedProfileX;
        /// <summary>
        /// X轴选中的速度档
        /// </summary>
        public PLCSpeedProfile? SelectedProfileX
        {
            get { return _selectedProfileX; }
            set { _selectedProfileX = value; RaisePropertyChanged(); }
        }

        private PLCSpeedProfile? _selectedProfileY;
        /// <summary>
        /// Y轴选中的速度档
        /// </summary>
        public PLCSpeedProfile? SelectedProfileY
        {
            get { return _selectedProfileY; }
            set { _selectedProfileY = value; RaisePropertyChanged(); }
        }

        private PLCSpeedProfile? _selectedProfileZ;
        /// <summary>
        /// Z轴选中的速度档
        /// </summary>
        public PLCSpeedProfile? SelectedProfileZ
        {
            get { return _selectedProfileZ; }
            set { _selectedProfileZ = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        public System.Collections.ObjectModel.ObservableCollection<PLCSpeedProfile>? SpeedProfilesX => FindAxis(EnumAxisType.X)?.SpeedProfiles;
        [JsonIgnore]
        public System.Collections.ObjectModel.ObservableCollection<PLCSpeedProfile>? SpeedProfilesY => FindAxis(EnumAxisType.Y)?.SpeedProfiles;
        [JsonIgnore]
        public System.Collections.ObjectModel.ObservableCollection<PLCSpeedProfile>? SpeedProfilesZ => FindAxis(EnumAxisType.ZTop)?.SpeedProfiles;

        [JsonIgnore]
        public System.Collections.Generic.IEnumerable<PLCAxisItem> ActiveAxes => AxisGroup?.AxisItems?.Where(axis => axis.IsUsing) ?? Enumerable.Empty<PLCAxisItem>();

        [JsonIgnore]
        private PLCAxisItem? _selectedAxis;
        /// <summary>
        /// 当前运动调试选中的轴
        /// </summary>
        [JsonIgnore]
        public PLCAxisItem? SelectedAxis
        {
            get { return _selectedAxis; }
            set
            {
                if (ReferenceEquals(_selectedAxis, value))
                {
                    return;
                }

                _selectedAxis = value;
                RaisePropertyChanged();
                AutoReadSelectedAxisParameters();
            }
        }

        private double _targetX;
        public double TargetX
        {
            get { return _targetX; }
            set { _targetX = value; RaisePropertyChanged(); }
        }

        private double _targetY;
        public double TargetY
        {
            get { return _targetY; }
            set { _targetY = value; RaisePropertyChanged(); }
        }

        private double _targetZ;
        public double TargetZ
        {
            get { return _targetZ; }
            set { _targetZ = value; RaisePropertyChanged(); }
        }

        private string _statusMessage = "未选择PLC";
        public string StatusMessage
        {
            get { return _statusMessage; }
            set { _statusMessage = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        public double CurrentX => GetCurrentPosition(EnumAxisType.X);

        [JsonIgnore]
        public double CurrentY => GetCurrentPosition(EnumAxisType.Y);

        [JsonIgnore]
        public double CurrentZ => GetCurrentPosition(EnumAxisType.ZTop);

        public bool StartJog(EnumAxisType axisType, bool positive)
        {
            var axis = FindAxis(axisType);
            if (axis == null)
            {
                StatusMessage = $"未配置{axisType}轴点动地址";
                return false;
            }

            var service = GetMotionService();
            if (service == null)
            {
                StatusMessage = "PLC未连接";
                return false;
            }

            double step = axis.RuntimeState.InputJogStep > 0 ? axis.RuntimeState.InputJogStep : (JogStep > 0 ? JogStep : axis.DefaultJogStep);
            double speed = axis.RuntimeState.InputJogSpeed > 0 ? axis.RuntimeState.InputJogSpeed : (JogSpeed > 0 ? JogSpeed : axis.DefaultRunSpeed);
            bool result = service.StartJog(axis, positive, step, speed);
            StatusMessage = result ? $"{axis.AxisName}开始点动" : $"{axis.AxisName}点动失败";
            return result;
        }

        public bool StartJog(PLCAxisItem axis, bool positive)
        {
            if (axis == null)
            {
                return false;
            }

            var service = GetMotionService();
            if (service == null)
            {
                StatusMessage = "PLC未连接";
                return false;
            }

            double step = axis.RuntimeState.InputJogStep > 0 ? axis.RuntimeState.InputJogStep : (JogStep > 0 ? JogStep : axis.DefaultJogStep);
            double speed = axis.RuntimeState.InputJogSpeed > 0 ? axis.RuntimeState.InputJogSpeed : (JogSpeed > 0 ? JogSpeed : axis.DefaultRunSpeed);
            bool result = service.StartJog(axis, positive, step, speed);
            StatusMessage = result ? $"{axis.AxisName}开始点动" : $"{axis.AxisName}点动失败";
            return result;
        }

        public bool StopJog(EnumAxisType axisType)
        {
            var axis = FindAxis(axisType);
            if (axis == null)
            {
                return false;
            }

            var service = GetMotionService();
            if (service == null)
            {
                StatusMessage = "PLC未连接";
                return false;
            }

            bool result = service.StopJog(axis);
            StatusMessage = result ? $"{axis.AxisName}已停止" : $"{axis.AxisName}停止失败";
            return result;
        }

        public bool StopAxis(PLCAxisItem axis)
        {
            if (axis == null)
            {
                return false;
            }

            var service = GetMotionService();
            if (service == null)
            {
                StatusMessage = "PLC未连接";
                return false;
            }

            bool result = service.StopAxis(axis);
            StatusMessage = result ? $"{axis.AxisName}已停止" : $"{axis.AxisName}停止失败";
            return result;
        }

        public bool StopJog(PLCAxisItem axis)
        {
            if (axis == null)
            {
                return false;
            }

            var service = GetMotionService();
            if (service == null)
            {
                StatusMessage = "PLC未连接";
                return false;
            }

            bool result = service.StopJog(axis);
            StatusMessage = result ? $"{axis.AxisName}点动已停止" : $"{axis.AxisName}点动停止失败";
            return result;
        }

        public bool StopAll()
        {
            var service = GetMotionService();
            if (service == null)
            {
                StatusMessage = "PLC未连接";
                return false;
            }

            if (AxisGroup == null)
            {
                StatusMessage = "未选择轴组";
                return false;
            }

            bool result = service.StopAll(AxisGroup);
            StatusMessage = result ? "全部轴已停止" : "停止失败";
            return result;
        }

        public bool MoveAxis(EnumAxisType axisType)
        {
            var axis = FindAxis(axisType);
            if (axis == null)
            {
                StatusMessage = $"未配置{axisType}轴定位地址";
                return false;
            }

            var service = GetMotionService();
            if (service == null)
            {
                StatusMessage = "PLC未连接";
                return false;
            }

            double target = axisType switch
            {
                EnumAxisType.X => TargetX,
                EnumAxisType.Y => TargetY,
                EnumAxisType.ZTop => TargetZ,
                _ => axis.RuntimeState.TargetPos,
            };

            var profile = axisType switch
            {
                EnumAxisType.X => SelectedProfileX,
                EnumAxisType.Y => SelectedProfileY,
                EnumAxisType.ZTop => SelectedProfileZ,
                _ => null,
            };

            double speed = profile?.RunSpeed ?? (MoveSpeed > 0 ? MoveSpeed : axis.DefaultRunSpeed);
            double acc = profile?.Acc ?? (MoveAcc > 0 ? MoveAcc : axis.DefaultAcc);
            double dec = profile?.Dec ?? (MoveDec > 0 ? MoveDec : axis.DefaultDec);

            bool result = service.MoveAxis(axis, target, speed, acc, dec);
            StatusMessage = result ? $"{axis.AxisName}定位指令已下发" : $"{axis.AxisName}定位失败";
            return result;
        }

        public bool RunAxis(PLCAxisItem axis)
        {
            if (axis == null)
            {
                return false;
            }

            var service = GetMotionService();
            if (service == null)
            {
                StatusMessage = "PLC未连接";
                return false;
            }

            double target = axis.RuntimeState.InputTargetPos;
            double speed = axis.RuntimeState.InputRunSpeed > 0 ? axis.RuntimeState.InputRunSpeed : (MoveSpeed > 0 ? MoveSpeed : axis.DefaultRunSpeed);
            double acc = axis.RuntimeState.InputAcc > 0 ? axis.RuntimeState.InputAcc : (MoveAcc > 0 ? MoveAcc : axis.DefaultAcc);
            double dec = axis.RuntimeState.InputDec > 0 ? axis.RuntimeState.InputDec : (MoveDec > 0 ? MoveDec : axis.DefaultDec);

            bool result = service.RunAxis(axis, target, speed, acc, dec);
            StatusMessage = result ? $"{axis.AxisName}跑位指令已下发" : $"{axis.AxisName}跑位失败";
            return result;
        }

        public bool MoveAll()
        {
            bool result = false;
            foreach (var axis in ActiveAxes)
            {
                result |= RunAxis(axis);
            }
            return result;
        }

        public bool HomeAxis(PLCAxisItem axis)
        {
            if (axis == null)
            {
                return false;
            }

            var service = GetMotionService();
            if (service == null)
            {
                StatusMessage = "PLC未连接";
                return false;
            }

            bool result = service.HomeAxis(axis);
            StatusMessage = result ? $"{axis.AxisName}回原指令已下发" : $"{axis.AxisName}回原失败";
            return result;
        }

        public bool EnableShield(PLCAxisItem axis)
        {
            if (axis == null)
            {
                return false;
            }

            var service = GetMotionService();
            if (service == null)
            {
                StatusMessage = "PLC未连接";
                return false;
            }

            bool result = service.EnableShield(axis);
            StatusMessage = result ? $"{axis.AxisName}使能屏蔽指令已下发" : $"{axis.AxisName}使能屏蔽失败";
            return result;
        }

        public bool DisableShield(PLCAxisItem axis)
        {
            if (axis == null)
            {
                return false;
            }

            var service = GetMotionService();
            if (service == null)
            {
                StatusMessage = "PLC未连接";
                return false;
            }

            bool result = service.DisableShield(axis);
            StatusMessage = result ? $"{axis.AxisName}解除屏蔽指令已下发" : $"{axis.AxisName}解除屏蔽失败";
            return result;
        }

        public bool SetAxisPosition(PLCAxisItem axis)
        {
            if (axis == null)
            {
                return false;
            }

            var service = GetMotionService();
            if (service == null)
            {
                StatusMessage = "PLC未连接";
                return false;
            }

            bool result = service.SetAxisPosition(axis, axis.RuntimeState.InputTargetPos);
            StatusMessage = result ? $"{axis.AxisName}设定位置指令已下发" : $"{axis.AxisName}设定位置失败";
            return result;
        }

        public bool ReadAxisParameters(PLCAxisItem axis)
        {
            if (axis == null)
            {
                return false;
            }

            var service = GetMotionService();
            if (service == null)
            {
                StatusMessage = "PLC未连接";
                return false;
            }

            bool result = false;
            SetAxisInputsSilently(() =>
            {
                result = service.ReadAxisParameters(axis);
            });
            StatusMessage = result ? $"{axis.AxisName}参数已读取" : $"{axis.AxisName}参数读取失败";
            return result;
        }

        private void AutoReadSelectedAxisParameters()
        {
            if (SelectedAxis == null || PlcDevice?.Config.IsConnected != true)
            {
                return;
            }

            ReadAxisParameters(SelectedAxis);
        }

        private void WriteAxisParameter(PLCAxisItem axis, string propertyName)
        {
            var service = GetMotionService();
            if (service == null)
            {
                StatusMessage = "PLC未连接";
                return;
            }

            if (!service.WriteAxisParameter(axis, propertyName))
            {
                StatusMessage = $"{axis.AxisName}参数写入失败";
            }
        }

        public bool Reset()
        {
            var service = GetMotionService();
            if (service == null)
            {
                StatusMessage = "PLC未连接";
                return false;
            }

            bool result = service.Reset();
            StatusMessage = result ? "复位指令已下发" : "复位失败";
            return result;
        }

        public bool ClearAlarm()
        {
            var service = GetMotionService();
            if (service == null)
            {
                StatusMessage = "PLC未连接";
                return false;
            }

            bool result = service.ClearAlarm();
            StatusMessage = result ? "报警清除指令已下发" : "报警清除失败";
            return result;
        }

        public bool WriteSpeedProfile(EnumAxisType axisType)
        {
            var axis = FindAxis(axisType);
            if (axis == null)
            {
                StatusMessage = $"未配置{axisType}轴";
                return false;
            }

            var service = GetMotionService();
            if (service == null)
            {
                StatusMessage = "PLC未连接";
                return false;
            }

            var profile = axisType switch
            {
                EnumAxisType.X => SelectedProfileX,
                EnumAxisType.Y => SelectedProfileY,
                EnumAxisType.ZTop => SelectedProfileZ,
                _ => null,
            };

            if (profile == null)
            {
                StatusMessage = $"{axis.AxisName}未选择速度档";
                return false;
            }

            bool result = service.WriteSpeedProfile(axis, profile);
            StatusMessage = result ? $"{axis.AxisName}速度档已写入" : $"{axis.AxisName}速度写入失败";
            return result;
        }

        private void EnsureSelectedAxis()
        {
            if (SelectedAxis != null && ActiveAxes.Contains(SelectedAxis))
            {
                return;
            }

            SelectedAxis = ActiveAxes.FirstOrDefault();
        }

        public void SyncTargetFromRuntime(PLCAxisItem axis)
        {
            if (axis == null)
            {
                return;
            }

            SetAxisInputsSilently(() =>
            {
                axis.RuntimeState.InputTargetPos = axis.RuntimeState.CurrentPos;
                axis.RuntimeState.InputRunSpeed = axis.DefaultRunSpeed;
                axis.RuntimeState.InputJogSpeed = axis.DefaultRunSpeed;
                axis.RuntimeState.InputJogStep = axis.DefaultJogStep;
                axis.RuntimeState.InputAcc = axis.DefaultAcc;
                axis.RuntimeState.InputDec = axis.DefaultDec;
            });
        }

        private void SetAxisInputsSilently(Action updateAction)
        {
            bool oldValue = _isUpdatingAxisParameters;
            _isUpdatingAxisParameters = true;
            try
            {
                updateAction();
            }
            finally
            {
                _isUpdatingAxisParameters = oldValue;
            }
        }

        private PLCAxisItem? FindAxis(EnumAxisType axisType)
        {
            if (AxisGroup == null)
            {
                return null;
            }

            return AxisGroup.AxisItems.FirstOrDefault(a => a.AxisType == axisType && a.IsUsing);
        }

        private PLCMotionService? GetMotionService()
        {
            if (PlcDevice == null || !PlcDevice.Config.IsConnected)
            {
                return null;
            }

            return PlcDevice.MotionService;
        }

        private double GetCurrentPosition(EnumAxisType axisType)
        {
            return FindAxis(axisType)?.RuntimeState.CurrentPos ?? 0;
        }

        private void RefreshConnectionStatus()
        {
            if (PlcDevice == null)
            {
                StatusMessage = "未选择PLC";
                return;
            }

            StatusMessage = PlcDevice.Config.IsConnected ? "PLC已连接" : "PLC未连接";
        }

        private void OnPlcConfigPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BasePlcConfigPara.IsConnected))
            {
                RefreshConnectionStatus();
                AutoReadSelectedAxisParameters();
            }
        }

        private void SubscribeAxisGroup(PLCAxisGroup? axisGroup)
        {
            if (axisGroup == null)
            {
                return;
            }

            axisGroup.AxisItems.CollectionChanged += OnAxisItemsCollectionChanged;
            foreach (var axis in axisGroup.AxisItems)
            {
                SubscribeAxis(axis);
            }
        }

        private void UnsubscribeAxisGroup(PLCAxisGroup? axisGroup)
        {
            if (axisGroup == null)
            {
                return;
            }

            axisGroup.AxisItems.CollectionChanged -= OnAxisItemsCollectionChanged;
            foreach (var axis in axisGroup.AxisItems)
            {
                UnsubscribeAxis(axis);
            }
        }

        private void SubscribeAxis(PLCAxisItem? axis)
        {
            if (axis == null)
            {
                return;
            }

            axis.PropertyChanged += OnAxisPropertyChanged;
            if (axis.RuntimeState != null)
            {
                axis.RuntimeState.PropertyChanged += OnAxisRuntimeStatePropertyChanged;
            }
        }

        private void UnsubscribeAxis(PLCAxisItem? axis)
        {
            if (axis == null)
            {
                return;
            }

            axis.PropertyChanged -= OnAxisPropertyChanged;
            if (axis.RuntimeState != null)
            {
                axis.RuntimeState.PropertyChanged -= OnAxisRuntimeStatePropertyChanged;
            }
        }

        private void OnAxisItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var axis in e.OldItems.OfType<PLCAxisItem>())
                {
                    UnsubscribeAxis(axis);
                }
            }

            if (e.NewItems != null)
            {
                foreach (var axis in e.NewItems.OfType<PLCAxisItem>())
                {
                    SubscribeAxis(axis);
                }
            }

            RaiseAxisRuntimeProperties();
            EnsureSelectedAxis();
        }

        private void OnAxisPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PLCAxisItem.AxisType) ||
                e.PropertyName == nameof(PLCAxisItem.IsUsing))
            {
                RaiseAxisRuntimeProperties();
                EnsureSelectedAxis();
                return;
            }

            if (e.PropertyName == nameof(PLCAxisItem.RuntimeState))
            {
                UnsubscribeAxisGroup(AxisGroup);
                SubscribeAxisGroup(AxisGroup);
                RaiseAxisRuntimeProperties();
                EnsureSelectedAxis();
            }
        }

        private void OnAxisRuntimeStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            string propertyName = e.PropertyName ?? string.Empty;
            if (e.PropertyName == nameof(PLCAxisRuntimeState.CurrentPos))
            {
                RaiseAxisRuntimeProperties();
            }

            if (_isUpdatingAxisParameters || !IsInputParameterProperty(propertyName))
            {
                return;
            }

            if (sender is PLCAxisRuntimeState runtimeState)
            {
                var axis = FindAxis(runtimeState);
                if (axis != null)
                {
                    if (propertyName == nameof(PLCAxisRuntimeState.InputTargetPos))
                    {
                        axis.RuntimeState.TargetPos = axis.RuntimeState.InputTargetPos;
                    }

                    WriteAxisParameter(axis, propertyName);
                }
            }
        }

        private PLCAxisItem? FindAxis(PLCAxisRuntimeState runtimeState)
        {
            return ActiveAxes.FirstOrDefault(axis => ReferenceEquals(axis.RuntimeState, runtimeState));
        }

        private static bool IsInputParameterProperty(string? propertyName)
        {
            return propertyName == nameof(PLCAxisRuntimeState.InputTargetPos) ||
                   propertyName == nameof(PLCAxisRuntimeState.InputJogStep) ||
                   propertyName == nameof(PLCAxisRuntimeState.InputJogSpeed) ||
                   propertyName == nameof(PLCAxisRuntimeState.InputRunSpeed) ||
                   propertyName == nameof(PLCAxisRuntimeState.InputAcc) ||
                   propertyName == nameof(PLCAxisRuntimeState.InputDec);
        }

        private void RaiseAxisRuntimeProperties()
        {
            RaisePropertyChanged(nameof(CurrentX));
            RaisePropertyChanged(nameof(CurrentY));
            RaisePropertyChanged(nameof(CurrentZ));
            RaisePropertyChanged(nameof(SpeedProfilesX));
            RaisePropertyChanged(nameof(SpeedProfilesY));
            RaisePropertyChanged(nameof(SpeedProfilesZ));
            RaisePropertyChanged(nameof(ActiveAxes));
            RaisePropertyChanged(nameof(SelectedAxis));
        }
    }
}
