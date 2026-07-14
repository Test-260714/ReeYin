using Prism.Commands;
using Prism.Dialogs;
using ReeYin.Hardware.LightController.CST.CustomUI.Models;
using ReeYin.Hardware.LightController.Models;
using ReeYin_V.Core.Base;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.IOC;
using ReeYin_V.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ReeYin.Hardware.LightController.CST.CustomUI.ViewModels
{
    public class CSTLightControllerViewModel : DialogViewModelBase
    {
        #region Fields
        private CSTLightController _controller;
        #endregion

        #region Properties
        private bool _isConnected;
        public bool IsConnected
        {
            get { return _isConnected; }
            set { _isConnected = value; RaisePropertyChanged(); }
        }

        private int _globalBrightness = 128;
        public int GlobalBrightness
        {
            get { return _globalBrightness; }
            set { _globalBrightness = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<ChannelInfo> _channelList = new ObservableCollection<ChannelInfo>();
        public ObservableCollection<ChannelInfo> ChannelList
        {
            get { return _channelList; }
            set { _channelList = value; RaisePropertyChanged(); }
        }

        private int _triggerMode = 0;
        /// <summary>
        /// 触发模式: 0=常亮, 1=外触发, 2=内触发
        /// </summary>
        public int TriggerMode
        {
            get { return _triggerMode; }
            set { _triggerMode = value; RaisePropertyChanged(); }
        }

        private int _globalStrobeTime = 100;
        /// <summary>
        /// 全局频闪时间(us)
        /// </summary>
        public int GlobalStrobeTime
        {
            get { return _globalStrobeTime; }
            set { _globalStrobeTime = value; RaisePropertyChanged(); }
        }

        private int _internalTriggerCycle = 100;
        /// <summary>
        /// 内触发周期(ms)
        /// </summary>
        public int InternalTriggerCycle
        {
            get { return _internalTriggerCycle; }
            set { _internalTriggerCycle = value; RaisePropertyChanged(); }
        }

        private bool _isInternalTriggerTesting = false;
        /// <summary>
        /// 是否正在进行内触发测试
        /// </summary>
        public bool IsInternalTriggerTesting
        {
            get { return _isInternalTriggerTesting; }
            set { _isInternalTriggerTesting = value; RaisePropertyChanged(); }
        }

        private bool _supportsTriggerMode = false;
        /// <summary>
        /// 控制器是否支持触发模式（高级功能）
        /// </summary>
        public bool SupportsTriggerMode
        {
            get { return _supportsTriggerMode; }
            set { _supportsTriggerMode = value; RaisePropertyChanged(); }
        }

        private bool _isBusy = false;
        /// <summary>
        /// 是否正在执行操作
        /// </summary>
        public bool IsBusy
        {
            get { return _isBusy; }
            set { _isBusy = value; RaisePropertyChanged(); }
        }

        private int _lightMode = 0;
        /// <summary>
        /// 常亮/常灭模式: 0=常亮(H), 1=常灭(L)
        /// </summary>
        public int LightMode
        {
            get { return _lightMode; }
            set { _lightMode = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Constructor
        public CSTLightControllerViewModel()
        {
        }
        #endregion

        #region Override
        public override void OnDialogOpened(IDialogParameters parameters)
        {
            base.OnDialogOpened(parameters);

            if (parameters.ContainsKey("Param"))
            {
                _controller = parameters.GetValue<object>("Param") as CSTLightController;
                if (_controller != null)
                {
                    IsConnected = _controller.IsConnected;
                    InitChannelList();
                }
            }
        }

        public override void InitParam()
        {
        }
        #endregion

        #region Commands
        private DelegateCommand _loadCommand;
        public DelegateCommand LoadCommand => _loadCommand ?? (_loadCommand = new DelegateCommand(async () =>
        {
            await AutoConnectAndRefreshAsync();
        }));

        /// <summary>
        /// 自动连接并刷新状态
        /// </summary>
        private async Task AutoConnectAndRefreshAsync()
        {
            if (_controller == null || IsBusy) return;
            IsBusy = true;

            try
            {
                // 如果已连接，直接刷新状态
                if (_controller.IsConnected)
                {
                    IsConnected = true;
                    await CheckTriggerSupportAsync(false);
                    await RefreshStatusAsync(false);
                    return;
                }

                // 如果未连接，尝试自动连接
                bool success = await Task.Run(() => _controller.Init());
                
                if (success)
                {
                    IsConnected = true;
                    InitChannelList();
                    await CheckTriggerSupportAsync(false);
                    await RefreshStatusAsync(false);
                    MessageView.Ins.MessageBoxShow("自动连接成功", eMsgType.Info);
                }
                else
                {
                    IsConnected = false;
                    // 自动连接失败不显示错误，用户可以手动点击连接
                }
            }
            catch
            {
                IsConnected = false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private DelegateCommand<string> _generalCommand;
        public DelegateCommand<string> GeneralCommand => _generalCommand ?? (_generalCommand = new DelegateCommand<string>(async (order) =>
        {
            switch (order)
            {
                case "连接":
                    await ConnectAsync();
                    break;

                case "断开":
                    await DisconnectAsync();
                    break;

                case "刷新":
                    await RefreshStatusAsync();
                    break;

                case "全部应用":
                    await ApplyAllBrightnessAsync();
                    break;

                case "全部打开":
                    await SetAllChannelsAsync(true);
                    break;

                case "全部关闭":
                    await SetAllChannelsAsync(false);
                    break;

                case "设置触发":
                    await SetTriggerModeAsync();
                    break;

                case "读取触发":
                    await GetTriggerModeAsync();
                    break;

                case "应用频闪":
                    await ApplyAllStrobeTimeAsync();
                    break;

                case "开始内触发测试":
                    await StartInternalTriggerTestAsync();
                    break;

                case "停止内触发测试":
                    await StopInternalTriggerTestAsync();
                    break;

                case "读取周期":
                    await ReadInternalTriggerCycleAsync();
                    break;

                case "设置周期":
                    await SetInternalTriggerCycleOnlyAsync();
                    break;

                case "确定":
                    CloseDialog(ButtonResult.OK, new DialogParameters()
                    {
                        { "Param", _controller }
                    });
                    break;

                case "取消":
                    CloseDialog(ButtonResult.Cancel);
                    break;
            }
        }));

        private DelegateCommand<ChannelInfo> _setChannelCommand;
        public DelegateCommand<ChannelInfo> SetChannelCommand => _setChannelCommand ?? (_setChannelCommand = new DelegateCommand<ChannelInfo>(async (channel) =>
        {
            if (_controller == null || !IsConnected || channel == null || IsBusy) return;
            IsBusy = true;

            try
            {
                await Task.Run(() =>
                {
                    // 设置亮度
                    _controller.SetBrightness(channel.ChannelIndex, channel.Brightness);
                    
                    // 设置频闪时间（网口模式）
                    if (_controller.ConnectionType == 0)
                    {
                        _controller.SetStrobeTime(channel.ChannelIndex, channel.StrobeTime);
                        _controller.SetLightDelay(channel.ChannelIndex, channel.LightDelay);
                        _controller.SetCameraDelay(channel.ChannelIndex, channel.CameraDelay);
                    }
                    
                    // 设置开关（串口模式）
                    if (_controller.ConnectionType == 1)
                    {
                        _controller.SetChannelOnOff(channel.ChannelIndex, channel.IsOn);
                    }
                });
                
                MessageView.Ins.MessageBoxShow($"通道{channel.ChannelIndex}参数设置成功", eMsgType.Info);
            }
            finally
            {
                IsBusy = false;
            }
        }));

        private DelegateCommand<ChannelInfo> _getChannelCommand;
        public DelegateCommand<ChannelInfo> GetChannelCommand => _getChannelCommand ?? (_getChannelCommand = new DelegateCommand<ChannelInfo>(async (channel) =>
        {
            if (_controller == null || !IsConnected || channel == null || IsBusy) return;
            IsBusy = true;

            try
            {
                int channelIndex = channel.ChannelIndex;
                int connectionType = _controller.ConnectionType;
                
                // 在后台线程读取所有参数
                int brightness = -1;
                int strobeTime = -1;
                int lightDelay = -1;
                int cameraDelay = -1;
                bool isOn = false;

                await Task.Run(() =>
                {
                    brightness = _controller.GetBrightness(channelIndex);

                    if (connectionType == 0)
                    {
                        strobeTime = _controller.GetStrobeTime(channelIndex);
                        lightDelay = _controller.GetLightDelay(channelIndex);
                        cameraDelay = _controller.GetCameraDelay(channelIndex);
                    }

                    if (connectionType == 1)
                    {
                        isOn = _controller.GetChannelOnOff(channelIndex);
                    }
                });

                // 在UI线程一次性更新所有值
                if (brightness >= 0) channel.Brightness = brightness;
                if (connectionType == 0)
                {
                    if (strobeTime >= 0) channel.StrobeTime = strobeTime;
                    if (lightDelay >= 0) channel.LightDelay = lightDelay;
                    if (cameraDelay >= 0) channel.CameraDelay = cameraDelay;
                }
                if (connectionType == 1)
                {
                    channel.IsOn = isOn;
                }
            }
            finally
            {
                IsBusy = false;
            }
        }));
        #endregion

        #region Methods
        private void InitChannelList()
        {
            ChannelList.Clear();
            if (_controller == null) return;

            for (int i = 1; i <= _controller.ChannelCount; i++)
            {
                ChannelList.Add(new ChannelInfo
                {
                    ChannelIndex = i,
                    Brightness = 0,
                    IsOn = false
                });
            }
        }

        private void RefreshStatus()
        {
            if (_controller != null)
            {
                IsConnected = _controller.IsConnected;
                if (IsConnected)
                {
                    foreach (var channel in ChannelList)
                    {
                        // 读取亮度
                        int brightness = _controller.GetBrightness(channel.ChannelIndex);
                        if (brightness >= 0)
                        {
                            channel.Brightness = brightness;
                        }
                        
                        // 读取频闪参数（网口模式）
                        if (_controller.ConnectionType == 0)
                        {
                            int strobeTime = _controller.GetStrobeTime(channel.ChannelIndex);
                            if (strobeTime >= 0) channel.StrobeTime = strobeTime;
                            
                            int lightDelay = _controller.GetLightDelay(channel.ChannelIndex);
                            if (lightDelay >= 0) channel.LightDelay = lightDelay;
                            
                            int cameraDelay = _controller.GetCameraDelay(channel.ChannelIndex);
                            if (cameraDelay >= 0) channel.CameraDelay = cameraDelay;
                        }
                    }
                }
            }
        }

        private void ApplyAllBrightness()
        {
            if (_controller == null || !IsConnected) return;

            var channelValues = new Dictionary<int, int>();
            foreach (var channel in ChannelList)
            {
                channel.Brightness = GlobalBrightness;
                channelValues[channel.ChannelIndex] = GlobalBrightness;
            }

            _controller.SetMultiBrightness(channelValues);
        }

        private void SetAllChannels(bool isOn)
        {
            if (_controller == null || !IsConnected) return;

            foreach (var channel in ChannelList)
            {
                channel.IsOn = isOn;
                _controller.SetChannelOnOff(channel.ChannelIndex, isOn);
            }
        }

        /// <summary>
        /// 设置触发模式
        /// </summary>
        private void SetTriggerMode()
        {
            if (_controller == null || !IsConnected) return;

            if (_controller.SetTriggerMode(TriggerMode))
            {
                string modeName = TriggerMode switch
                {
                    0 => "常亮模式",
                    1 => "外触发模式",
                    2 => "内触发模式",
                    _ => "未知模式"
                };
                MessageView.Ins.MessageBoxShow($"触发模式设置为: {modeName}", eMsgType.Info);
            }
            else
            {
                string errorMsg = _controller.LastErrorCode switch
                {
                    10022 => "该控制器型号不支持触发模式设置",
                    _ => $"错误码: {_controller.LastErrorCode}"
                };
                MessageView.Ins.MessageBoxShow($"触发模式设置失败，{errorMsg}", eMsgType.Warn);
            }
        }

        /// <summary>
        /// 读取触发模式
        /// </summary>
        private void GetTriggerMode()
        {
            if (_controller == null || !IsConnected) return;

            int mode = _controller.GetTriggerMode();
            if (mode >= 0)
            {
                TriggerMode = mode;
                string modeName = mode switch
                {
                    0 => "常亮模式",
                    1 => "外触发模式",
                    2 => "内触发模式",
                    _ => "未知模式"
                };
                MessageView.Ins.MessageBoxShow($"当前触发模式: {modeName}", eMsgType.Info);
            }
            else
            {
                MessageView.Ins.MessageBoxShow("读取触发模式失败", eMsgType.Warn);
            }
        }

        /// <summary>
        /// 应用全局频闪时间到所有通道
        /// </summary>
        private void ApplyAllStrobeTime()
        {
            if (_controller == null || !IsConnected) return;

            bool success = true;
            foreach (var channel in ChannelList)
            {
                channel.StrobeTime = GlobalStrobeTime;
                if (!_controller.SetStrobeTime(channel.ChannelIndex, GlobalStrobeTime))
                {
                    success = false;
                }
            }

            if (success)
            {
                MessageView.Ins.MessageBoxShow($"所有通道频闪时间设置为: {GlobalStrobeTime}us", eMsgType.Info);
            }
            else
            {
                MessageView.Ins.MessageBoxShow("部分通道频闪时间设置失败", eMsgType.Warn);
            }
        }

        /// <summary>
        /// 开始内触发测试
        /// </summary>
        private void StartInternalTriggerTest()
        {
            if (_controller == null || !IsConnected) return;

            // 1. 先切换到内触发模式 (mode=2)
            if (!_controller.SetTriggerMode(2))
            {
                string errorMsg = _controller.LastErrorCode == 10022 
                    ? "该控制器型号不支持触发模式" 
                    : $"错误码: {_controller.LastErrorCode}";
                MessageView.Ins.MessageBoxShow($"切换到内触发模式失败，{errorMsg}", eMsgType.Warn);
                return;
            }
            TriggerMode = 2;

            // 2. 设置内触发周期
            if (!_controller.SetInternalTriggerCycle(InternalTriggerCycle))
            {
                // 尝试读取当前周期值
                int currentCycle = _controller.GetInternalTriggerCycle();
                if (currentCycle > 0)
                {
                    InternalTriggerCycle = currentCycle;
                    IsInternalTriggerTesting = true;
                    MessageView.Ins.MessageBoxShow($"设置周期失败，使用当前周期: {currentCycle}ms，内触发已启动", eMsgType.Info);
                    return;
                }
                MessageView.Ins.MessageBoxShow($"设置内触发周期失败，错误码: {_controller.LastErrorCode}", eMsgType.Warn);
            }

            IsInternalTriggerTesting = true;
            MessageView.Ins.MessageBoxShow($"内触发测试已启动，周期: {InternalTriggerCycle}ms", eMsgType.Info);
        }

        /// <summary>
        /// 停止内触发测试
        /// </summary>
        private void StopInternalTriggerTest()
        {
            if (_controller == null || !IsConnected) return;

            // 切换回常亮模式 (mode=0)
            if (_controller.SetTriggerMode(0))
            {
                TriggerMode = 0;
                IsInternalTriggerTesting = false;
                MessageView.Ins.MessageBoxShow("内触发测试已停止，已切换回常亮模式", eMsgType.Info);
            }
            else
            {
                MessageView.Ins.MessageBoxShow("切换回常亮模式失败", eMsgType.Warn);
            }
        }

        /// <summary>
        /// 读取内触发周期
        /// </summary>
        private void ReadInternalTriggerCycle()
        {
            if (_controller == null || !IsConnected) return;

            int cycle = _controller.GetInternalTriggerCycle();
            if (cycle >= 0)
            {
                InternalTriggerCycle = cycle;
                MessageView.Ins.MessageBoxShow($"当前内触发周期: {cycle}ms", eMsgType.Info);
            }
            else
            {
                MessageView.Ins.MessageBoxShow("读取内触发周期失败", eMsgType.Warn);
            }
        }

        /// <summary>
        /// 仅设置内触发周期（不切换模式）
        /// </summary>
        private void SetInternalTriggerCycleOnly()
        {
            if (_controller == null || !IsConnected) return;

            if (_controller.SetInternalTriggerCycle(InternalTriggerCycle))
            {
                MessageView.Ins.MessageBoxShow($"内触发周期设置为: {InternalTriggerCycle}ms", eMsgType.Info);
            }
            else
            {
                MessageView.Ins.MessageBoxShow("设置内触发周期失败", eMsgType.Warn);
            }
        }
        #endregion

        #region Async Methods
        /// <summary>
        /// 异步连接
        /// </summary>
        private async Task ConnectAsync()
        {
            if (_controller == null || IsBusy) return;
            IsBusy = true;

            try
            {
                bool success = await Task.Run(() => _controller.Init());
                
                if (success)
                {
                    IsConnected = true;
                    InitChannelList();
                    // 检测是否支持触发模式
                    await CheckTriggerSupportAsync(false);
                    await RefreshStatusAsync(false);
                    MessageView.Ins.MessageBoxShow("连接成功", eMsgType.Info);
                }
                else
                {
                    MessageView.Ins.MessageBoxShow("连接失败，请检查硬件连接", eMsgType.Warn);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 异步断开
        /// </summary>
        private async Task DisconnectAsync()
        {
            if (_controller == null || IsBusy) return;
            IsBusy = true;

            try
            {
                await Task.Run(() => _controller.Close());
                IsConnected = false;
                SupportsTriggerMode = false;
                MessageView.Ins.MessageBoxShow("已断开连接", eMsgType.Info);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 检测控制器是否支持触发模式
        /// </summary>
        private async Task CheckTriggerSupportAsync(bool checkBusy = true)
        {
            if (_controller == null || !IsConnected) return;
            if (checkBusy && IsBusy) return;

            try
            {
                int mode = await Task.Run(() => _controller.GetTriggerMode());
                SupportsTriggerMode = (mode >= 0);
                
                if (!SupportsTriggerMode)
                {
                    Console.WriteLine("该控制器不支持触发模式功能");
                }
            }
            catch
            {
                SupportsTriggerMode = false;
            }
        }

        /// <summary>
        /// 异步刷新状态
        /// </summary>
        private async Task RefreshStatusAsync(bool checkBusy = true)
        {
            if (_controller == null) return;
            if (checkBusy && IsBusy) return;
            
            bool needSetBusy = checkBusy && !IsBusy;
            if (needSetBusy) IsBusy = true;

            try
            {
                IsConnected = _controller.IsConnected;
                if (IsConnected)
                {
                    // 在后台线程批量读取所有亮度值
                    var channelIndices = ChannelList.Select(c => c.ChannelIndex).ToList();
                    var brightnessValues = new Dictionary<int, int>();
                    
                    await Task.Run(() =>
                    {
                        foreach (var index in channelIndices)
                        {
                            int brightness = _controller.GetBrightness(index);
                            if (brightness >= 0)
                            {
                                brightnessValues[index] = brightness;
                            }
                        }
                    });

                    // 在UI线程一次性更新所有值
                    foreach (var channel in ChannelList)
                    {
                        if (brightnessValues.TryGetValue(channel.ChannelIndex, out int brightness))
                        {
                            channel.Brightness = brightness;
                        }
                    }
                }
            }
            finally
            {
                if (needSetBusy) IsBusy = false;
            }
        }

        /// <summary>
        /// 异步应用全局亮度
        /// </summary>
        private async Task ApplyAllBrightnessAsync()
        {
            if (_controller == null || !IsConnected || IsBusy) return;
            IsBusy = true;

            try
            {
                var channelValues = new Dictionary<int, int>();
                foreach (var channel in ChannelList)
                {
                    channel.Brightness = GlobalBrightness;
                    channelValues[channel.ChannelIndex] = GlobalBrightness;
                }

                await Task.Run(() => _controller.SetMultiBrightness(channelValues));
                MessageView.Ins.MessageBoxShow($"全局亮度已设置为: {GlobalBrightness}", eMsgType.Info);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 异步设置所有通道开关
        /// </summary>
        private async Task SetAllChannelsAsync(bool isOn)
        {
            if (_controller == null || !IsConnected || IsBusy) return;
            IsBusy = true;

            try
            {
                // 先在UI线程更新所有通道状态
                foreach (var channel in ChannelList)
                {
                    channel.IsOn = isOn;
                }

                // 然后在后台线程执行硬件操作
                var channelIndices = ChannelList.Select(c => c.ChannelIndex).ToList();
                await Task.Run(() =>
                {
                    foreach (var index in channelIndices)
                    {
                        _controller.SetChannelOnOff(index, isOn);
                    }
                });
                MessageView.Ins.MessageBoxShow(isOn ? "已全部打开" : "已全部关闭", eMsgType.Info);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 异步设置触发模式
        /// </summary>
        private async Task SetTriggerModeAsync()
        {
            if (_controller == null || !IsConnected || IsBusy) return;
            
            if (!SupportsTriggerMode)
            {
                MessageView.Ins.MessageBoxShow("该控制器不支持触发模式功能", eMsgType.Warn);
                return;
            }

            IsBusy = true;
            try
            {
                bool success = await Task.Run(() => _controller.SetTriggerMode(TriggerMode));
                if (success)
                {
                    string modeName = TriggerMode switch
                    {
                        0 => "常亮模式",
                        1 => "外触发模式",
                        2 => "内触发模式",
                        _ => "未知模式"
                    };
                    MessageView.Ins.MessageBoxShow($"触发模式设置为: {modeName}", eMsgType.Info);
                }
                else
                {
                    MessageView.Ins.MessageBoxShow($"触发模式设置失败，错误码: {_controller.LastErrorCode}", eMsgType.Warn);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 异步读取触发模式
        /// </summary>
        private async Task GetTriggerModeAsync()
        {
            if (_controller == null || !IsConnected || IsBusy) return;
            
            if (!SupportsTriggerMode)
            {
                MessageView.Ins.MessageBoxShow("该控制器不支持触发模式功能", eMsgType.Warn);
                return;
            }

            IsBusy = true;
            try
            {
                int mode = await Task.Run(() => _controller.GetTriggerMode());
                if (mode >= 0)
                {
                    TriggerMode = mode;
                    string modeName = mode switch
                    {
                        0 => "常亮模式",
                        1 => "外触发模式",
                        2 => "内触发模式",
                        _ => "未知模式"
                    };
                    MessageView.Ins.MessageBoxShow($"当前触发模式: {modeName}", eMsgType.Info);
                }
                else
                {
                    MessageView.Ins.MessageBoxShow("读取触发模式失败", eMsgType.Warn);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 异步应用频闪时间
        /// </summary>
        private async Task ApplyAllStrobeTimeAsync()
        {
            if (_controller == null || !IsConnected || IsBusy) return;
            
            if (!SupportsTriggerMode)
            {
                MessageView.Ins.MessageBoxShow("该控制器不支持频闪功能", eMsgType.Warn);
                return;
            }

            IsBusy = true;
            try
            {
                // 先在UI线程更新所有通道状态
                foreach (var channel in ChannelList)
                {
                    channel.StrobeTime = GlobalStrobeTime;
                }

                // 然后在后台线程执行硬件操作
                var channelIndices = ChannelList.Select(c => c.ChannelIndex).ToList();
                bool allSuccess = true;
                await Task.Run(() =>
                {
                    foreach (var index in channelIndices)
                    {
                        if (!_controller.SetStrobeTime(index, GlobalStrobeTime))
                        {
                            allSuccess = false;
                        }
                    }
                });

                if (allSuccess)
                {
                    MessageView.Ins.MessageBoxShow($"所有通道频闪时间设置为: {GlobalStrobeTime}us", eMsgType.Info);
                }
                else
                {
                    MessageView.Ins.MessageBoxShow("部分通道频闪时间设置失败", eMsgType.Warn);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 异步开始内触发测试
        /// </summary>
        private async Task StartInternalTriggerTestAsync()
        {
            if (_controller == null || !IsConnected || IsBusy) return;
            
            if (!SupportsTriggerMode)
            {
                MessageView.Ins.MessageBoxShow("该控制器不支持内触发功能", eMsgType.Warn);
                return;
            }

            IsBusy = true;
            try
            {
                bool success = await Task.Run(() =>
                {
                    if (!_controller.SetTriggerMode(2)) return false;
                    return _controller.SetInternalTriggerCycle(InternalTriggerCycle);
                });

                if (success)
                {
                    TriggerMode = 2;
                    IsInternalTriggerTesting = true;
                    MessageView.Ins.MessageBoxShow($"内触发测试已启动，周期: {InternalTriggerCycle}ms", eMsgType.Info);
                }
                else
                {
                    MessageView.Ins.MessageBoxShow($"启动内触发测试失败，错误码: {_controller.LastErrorCode}", eMsgType.Warn);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 异步停止内触发测试
        /// </summary>
        private async Task StopInternalTriggerTestAsync()
        {
            if (_controller == null || !IsConnected || IsBusy) return;
            IsBusy = true;

            try
            {
                bool success = await Task.Run(() => _controller.SetTriggerMode(0));
                if (success)
                {
                    TriggerMode = 0;
                    IsInternalTriggerTesting = false;
                    MessageView.Ins.MessageBoxShow("内触发测试已停止", eMsgType.Info);
                }
                else
                {
                    MessageView.Ins.MessageBoxShow("停止内触发测试失败", eMsgType.Warn);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 异步读取内触发周期
        /// </summary>
        private async Task ReadInternalTriggerCycleAsync()
        {
            if (_controller == null || !IsConnected || IsBusy) return;
            
            if (!SupportsTriggerMode)
            {
                MessageView.Ins.MessageBoxShow("该控制器不支持内触发功能", eMsgType.Warn);
                return;
            }

            IsBusy = true;
            try
            {
                int cycle = await Task.Run(() => _controller.GetInternalTriggerCycle());
                if (cycle >= 0)
                {
                    InternalTriggerCycle = cycle;
                    MessageView.Ins.MessageBoxShow($"当前内触发周期: {cycle}ms", eMsgType.Info);
                }
                else
                {
                    MessageView.Ins.MessageBoxShow("读取内触发周期失败", eMsgType.Warn);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 异步设置内触发周期
        /// </summary>
        private async Task SetInternalTriggerCycleOnlyAsync()
        {
            if (_controller == null || !IsConnected || IsBusy) return;
            
            if (!SupportsTriggerMode)
            {
                MessageView.Ins.MessageBoxShow("该控制器不支持内触发功能", eMsgType.Warn);
                return;
            }

            IsBusy = true;
            try
            {
                bool success = await Task.Run(() => _controller.SetInternalTriggerCycle(InternalTriggerCycle));
                if (success)
                {
                    MessageView.Ins.MessageBoxShow($"内触发周期设置为: {InternalTriggerCycle}ms", eMsgType.Info);
                }
                else
                {
                    MessageView.Ins.MessageBoxShow("设置内触发周期失败", eMsgType.Warn);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }
        #endregion
    }
}
