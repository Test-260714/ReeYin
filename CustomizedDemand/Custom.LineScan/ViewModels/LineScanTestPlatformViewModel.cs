using Custom.LineScan.Models;
using Custom.LineScan.Services;
using Custom.LineScan.Views;
using IKapBoardDotNet;
using IKapCDotNet;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Ioc;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using ReeYin.Hardware.LightController.CST;
using ReeYin.Hardware.LightController.CST.CustomUI.Views;
using ReeYin.Hardware.LightController.Models;
using ReeYin.Hardware.LightController.Rsee;
using ReeYin.Hardware.LightController.Rsee.CustomUI.Views;
using ReeYin_V.Core.Enums;
using ReeYin_V.UI;
using ReeYin_V.UI.Style.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Custom.LineScan.ViewModels
{
    /// <summary>
    /// 线扫测试平台ViewModel - 集成正运动控制卡和埃科相机
    /// </summary>
    [Serializable]
    public class LineScanTestPlatformViewModel : DialogViewModelBase, IViewModuleParam
    {
        #region Static Fields - 硬件句柄（界面关闭后保持）
        // 埃科相机句柄（静态，界面关闭后保持连接）
        private static ITKDEVICE s_hDev;
        private static ITKSTREAM s_hStream;
        private static IntPtr s_pBoard = new IntPtr(-1);
        private static IntPtr s_pUserBuffer = IntPtr.Zero;
        private static int s_nWidth = 0;
        private static int s_nHeight = 0;
        private static int s_nBufferSize = 0;
        private static bool s_bStreamCreated = false;
        private static bool s_cameraConnected = false; // 相机连接状态标志

        // 光源控制器（静态，界面关闭后保持连接）
        private static LightControllerBase s_lightController;

        // 运动状态轮询相关（静态）
        private static CancellationTokenSource s_motionPollCts;
        private static Task s_motionPollTask;
        private static volatile float s_latestPosition = 0;
        private static volatile bool s_latestIsMoving = false;
        private static bool s_motionPollingRunning = false;
        private static int s_axisNumber = 0; // 轴号（静态，供轮询线程使用）
        
        // 静态构造函数 - 注册程序退出事件
        static LineScanTestPlatformViewModel()
        {
            // 程序退出时释放所有硬件资源
            Application.Current.Exit += (s, e) => CleanupAllResources();
        }
        
        /// <summary>
        /// 释放所有静态硬件资源（程序退出时调用）
        /// </summary>
        private static void CleanupAllResources()
        {
            try
            {
                // 停止运动轮询
                s_motionPollCts?.Cancel();
                s_motionPollingRunning = false;
                
                // 停止相机采集并释放资源
                if (s_cameraConnected)
                {
                    if (s_bStreamCreated && s_hStream != null)
                    {
                        IKapC.ItkStreamStop(s_hStream);
                        IKapC.ItkDevFreeStream(s_hStream);
                        s_bStreamCreated = false;
                    }
                    
                    if (s_pUserBuffer != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(s_pUserBuffer);
                        s_pUserBuffer = IntPtr.Zero;
                    }
                    
                    if (s_pBoard != new IntPtr(-1))
                    {
                        IKapBoard.IKapClose(s_pBoard);
                        s_pBoard = new IntPtr(-1);
                    }
                    
                    if (s_hDev != null)
                    {
                        IKapC.ItkDevClose(s_hDev);
                    }

                    ResetIKapHandles();
                    s_cameraConnected = false;
                }
                
                // 关闭光源控制器
                s_lightController?.Close();
                s_lightController = null;
            }
            catch
            {
                // 忽略清理时的异常
            }
        }
        #endregion

        #region Fields
        [NonSerialized]
        private readonly IZMotionPlatformService _platformService;

        // 相机采集相关（实例级别）
        private uint m_nFrameCount = 5;
        private bool m_bGrabbing = false;
        private CancellationTokenSource _grabCts;

        // 帧回调委托
        delegate void IKapCCallBackDelegate(uint eventType, IntPtr pContext);
        private IKapCCallBackDelegate _onFrameReadyDelegate;
        private GCHandle _thisGCHandle;
        private bool _callbackRegistered = false;

        [NonSerialized]
        private DispatcherTimer _timer;

        // 图像保存队列相关
        private System.Collections.Concurrent.BlockingCollection<ImageSaveItem> _saveQueue;
        private Task _saveTask;
        private CancellationTokenSource _saveCts;
        private int _savedImageCount = 0;
        #endregion

        #region Properties
        private LineScanTestPlatformModel _modelParam;
        public LineScanTestPlatformModel ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        private string _sltOutputParamName;
        public string SltOutputParamName
        {
            get { return _sltOutputParamName; }
            set { _sltOutputParamName = value; RaisePropertyChanged(); }
        }

        private TransmitParam _currentOutputParam;
        public TransmitParam CurrentOutputParam
        {
            get { return _currentOutputParam; }
            set { _currentOutputParam = value; RaisePropertyChanged(); }
        }

        private System.Windows.Media.ImageSource _displayImage;
        /// <summary>
        /// 显示图像
        /// </summary>
        public System.Windows.Media.ImageSource DisplayImage
        {
            get { return _displayImage; }
            set { _displayImage = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 日志文本（用于可复制的TextBox显示）
        /// </summary>
        public string LogText
        {
            get
            {
                if (ModelParam?.LogMessages == null)
                    return string.Empty;
                return string.Join(Environment.NewLine, ModelParam.LogMessages);
            }
        }

        /// <summary>
        /// 刷新日志显示
        /// </summary>
        private void RefreshLogText()
        {
            RaisePropertyChanged(nameof(LogText));
        }
        #endregion

        #region Constructor
        public LineScanTestPlatformViewModel(IZMotionPlatformService platformService)
        {
            _platformService = platformService ?? throw new ArgumentNullException(nameof(platformService));
            InitTimer();
        }
        #endregion

        #region Methods
        private void InitTimer()
        {
            // UI刷新定时器 - 只负责更新界面显示
            _timer = new DispatcherTimer();
            _timer.Interval = new TimeSpan(0, 0, 0, 0, 16); // 16ms更新UI（约60fps）
            _timer.Tick += (s, e) =>
            {
                // 从缓存的数据更新UI，不做网络通信
                ModelParam.CurrentPosition = s_latestPosition;
                ModelParam.IsMoving = s_latestIsMoving;
            };
        }

        /// <summary>
        /// 启动后台轮询线程
        /// </summary>
        private void StartMotionPolling()
        {
            if (s_motionPollingRunning)
            {
                // 轮询已在运行，只启动UI刷新
                _timer.Start();
                return;
            }
            
            // 保存轴号到静态变量，供轮询线程使用
            s_axisNumber = ModelParam.AxisNumber;
            s_motionPollCts = new CancellationTokenSource();
            s_motionPollTask = Task.Run(() => MotionPollingLoop(s_motionPollCts.Token));
            s_motionPollingRunning = true;
            _timer.Start();
        }

        /// <summary>
        /// 停止后台轮询线程
        /// </summary>
        private void StopMotionPolling()
        {
            _timer.Stop();
            s_motionPollCts?.Cancel();
            try
            {
                s_motionPollTask?.Wait(500); // 最多等500ms
            }
            catch { }
            s_motionPollCts?.Dispose();
            s_motionPollCts = null;
            s_motionPollingRunning = false;
        }

        /// <summary>
        /// 后台轮询循环 - 高频读取运动状态（使用静态变量，不依赖实例）
        /// </summary>
        private void MotionPollingLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (_platformService.IsConnected)
                    {
                        // 读取位置（使用静态轴号）
                        int ret = _platformService.GetAxisPosition(s_axisNumber, out var curpos);
                        if (ret == 0)
                        {
                            s_latestPosition = curpos;
                        }

                        // 读取运动状态
                        ret = _platformService.GetAxisIdle(s_axisNumber, out var idleStatus);
                        s_latestIsMoving = (ret == 0 && idleStatus == 0);
                    }
                }
                catch
                {
                    // 忽略轮询中的异常，继续下一次
                }

                // 后台轮询间隔 5ms（200Hz采样率）
                Thread.Sleep(16);
            }
        }

        public void Init()
        {
            // 获取数据点定义
            ModelParam.OutputParamResource.Clear();
            var dataPoints = OutputParamCollector.GetDataPoints(typeof(LineScanTestPlatformModel));
            foreach (var point in dataPoints)
            {
                ModelParam.OutputParamResource.Add(point.Name + $"[{point.Description}]", new TransmitParam
                {
                    Name = point.Name,
                    Type = DataType._object,
                    Resourece = ResoureceType.None,
                    Value = OutputParamCollector.GetDataPointValues(ModelParam)[point.Name],
                    Describe = point.Description,
                    ResourcePath = point.MemberInfo.DeclaringType.FullName + "." + point.Name
                });
            }
        }

        public override void InitParam()
        {
            if (Param != null && (Param is LineScanTestPlatformModel))
                ModelParam = Param as LineScanTestPlatformModel;
            else
                ModelParam = new LineScanTestPlatformModel();

            // 平台服务是模块单例，重新打开页面时可以恢复连接状态。
            bool zmotionConnected = _platformService.IsConnected;
            bool cameraConnected = s_cameraConnected && s_hDev != null;
            bool lightConnected = s_lightController?.IsConnected ?? false;
            
            ModelParam.ZMotionConnected = zmotionConnected;
            ModelParam.CameraConnected = cameraConnected;
            ModelParam.LightControllerConnected = lightConnected;
            
            // 添加恢复状态日志
            ModelParam.AddLog($"界面初始化 - 运动卡:{(zmotionConnected ? "已连接" : "未连接")}, 相机:{(cameraConnected ? "已连接" : "未连接")}, 光源:{(lightConnected ? "已连接" : "未连接")}");
            
            // 如果运动控制卡已连接，恢复轮询和UI刷新
            if (ModelParam.ZMotionConnected && !s_motionPollingRunning)
            {
                StartMotionPolling();
                ModelParam.AddLog("恢复运动状态轮询");
            }
            else if (ModelParam.ZMotionConnected)
            {
                // 轮询已在运行，只启动UI刷新定时器
                _timer.Start();
            }

            // 恢复相机图像参数
            if (ModelParam.CameraConnected)
            {
                ModelParam.ImageWidth = s_nWidth;
                ModelParam.ImageHeight = s_nHeight;
                ModelParam.AddLog($"恢复相机参数: {s_nWidth}x{s_nHeight}");
                ReadCameraParams(false);
            }

            // 监听日志变化
            ModelParam.LogMessages.CollectionChanged += (s, e) => RefreshLogText();

            Init();
            ModelParam.TransferParam();
        }
        #endregion

        #region 正运动控制卡方法
        private async void ConnectZMotion()
        {
            try
            {
                ModelParam.AddLog("正在连接正运动控制卡...");

                int ret = await Task.Run(() => _platformService.ConnectEthernet(ModelParam.ZMotionIpAddress));
                if (ret == 0 && _platformService.IsConnected)
                {
                    ModelParam.ZMotionConnected = true;
                    ModelParam.AddLog("正运动控制卡连接成功！");
                    StartMotionPolling(); // 启动后台轮询
                }
                else
                {
                    ModelParam.AddLog($"正运动控制卡连接失败，错误码：{ret}");
                }
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"连接正运动控制卡异常：{ex.Message}");
            }
        }

        private async void DisconnectZMotion()
        {
            try
            {
                if (_platformService.IsConnected)
                {
                    StopMotionPolling(); // 停止后台轮询
                    int ret = await Task.Run(() => _platformService.Disconnect());
                    ModelParam.ZMotionConnected = false;
                    ModelParam.AddLog(ret == 0
                        ? "正运动控制卡已断开连接"
                        : $"正运动控制卡断开失败，错误码：{ret}");
                }
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"断开正运动控制卡失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 打开运动控制卡设置界面
        /// </summary>
        private void OpenMotionCardSettings()
        {
            try
            {
                if (!ModelParam.ZMotionConnected || !_platformService.IsConnected)
                {
                    ModelParam.AddLog("请先连接运动控制卡");
                    return;
                }

                var settingsWindow = new MotionCardSettingsView(_platformService);
                settingsWindow.Owner = Application.Current.MainWindow;
                settingsWindow.ShowDialog();

                ModelParam.ZMotionConnected = _platformService.IsConnected;
                if (ModelParam.ZMotionConnected)
                    StartMotionPolling();
                else
                    StopMotionPolling();
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"打开设置界面失败：{ex.Message}");
            }
        }

        private void MoveToPosition()
        {
            try
            {
                if (!ModelParam.ZMotionConnected)
                {
                    ModelParam.AddLog("正运动控制卡未连接");
                    return;
                }

                int ret = _platformService.WriteFloat(ZMotionPlatformProtocol.PositionSpeed, ModelParam.MoveSpeed);
                if (ret != 0)
                {
                    ModelParam.AddLog($"设置速度失败，错误码：{ret}");
                    return;
                }

                ret = _platformService.WriteFloat(ZMotionPlatformProtocol.PositionTarget, ModelParam.TargetPosition);
                if (ret != 0)
                {
                    ModelParam.AddLog($"设置位置失败，错误码：{ret}");
                    return;
                }

                ret = _platformService.SendCommand((ushort)ZMotionPlatformCommand.Position);
                
                if (ret == 0)
                {
                    ModelParam.AddLog($"定位运动命令已发送，目标位置：{ModelParam.TargetPosition}，速度：{ModelParam.MoveSpeed}");
                }
                else
                {
                    ModelParam.AddLog($"发送定位命令失败，错误码：{ret}");
                }
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"运动异常：{ex.Message}");
            }
        }

        /// <summary>
        /// 开始运动 - 使用前进速度和工作距离
        /// </summary>
        private void StartMotion()
        {
            try
            {
                if (!ModelParam.ZMotionConnected)
                {
                    ModelParam.AddLog("正运动控制卡未连接");
                    return;
                }

                int ret = _platformService.WriteFloat(ZMotionPlatformProtocol.PositionSpeed, ModelParam.ForwardSpeed);
                if (ret != 0)
                {
                    ModelParam.AddLog($"设置前进速度失败，错误码：{ret}");
                    return;
                }

                ret = _platformService.WriteFloat(ZMotionPlatformProtocol.PositionTarget, ModelParam.WorkDistance);
                if (ret != 0)
                {
                    ModelParam.AddLog($"设置工作距离失败，错误码：{ret}");
                    return;
                }

                ret = _platformService.SendCommand((ushort)ZMotionPlatformCommand.Position);
                
                if (ret == 0)
                {
                    ModelParam.AddLog($"开始运动，前进速度：{ModelParam.ForwardSpeed} mm/s，工作距离：{ModelParam.WorkDistance} mm");
                }
                else
                {
                    ModelParam.AddLog($"发送运动命令失败，错误码：{ret}");
                }
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"开始运动异常：{ex.Message}");
            }
        }

        /// <summary>
        /// 开始往复运动 - 和设置界面一致，只发送命令不修改参数
        /// </summary>
        private void StartReciprocatingMotion()
        {
            if (!_platformService.IsConnected)
            {
                ModelParam.AddLog("正运动控制卡未连接");
                return;
            }
            try
            {
                int ret = _platformService.SendCommand((ushort)ZMotionPlatformCommand.StartReciprocating);
                ModelParam.AddLog(ret == 0 ? "开始往复运动命令已发送" : $"开始往复运动失败，错误码：{ret}");
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"开始往复运动失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 停止往复运动 - 照抄设置界面的实现（暂停往复=4）
        /// </summary>
        private void StopReciprocatingMotion()
        {
            if (!_platformService.IsConnected)
            {
                ModelParam.AddLog("正运动控制卡未连接");
                return;
            }
            try
            {
                int ret = _platformService.SendCommand((ushort)ZMotionPlatformCommand.PauseReciprocating);
                ModelParam.AddLog(ret == 0 ? "停止往复运动命令已发送" : $"停止往复运动失败，错误码：{ret}");
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"停止往复运动失败：{ex.Message}");
            }
        }

        private void StopMotion()
        {
            try
            {
                if (_platformService.IsConnected)
                {
                    int ret = _platformService.CancelAxis(ModelParam.AxisNumber, 2);
                    _platformService.WriteCoil(ZMotionPlatformProtocol.JogNegative, false);
                    _platformService.WriteCoil(ZMotionPlatformProtocol.JogPositive, false);
                    
                    if (ret == 0)
                    {
                        ModelParam.AddLog("运动已停止");
                    }
                }
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"停止运动失败：{ex.Message}");
            }
        }

        private void GoHome()
        {
            try
            {
                if (!ModelParam.ZMotionConnected)
                {
                    ModelParam.AddLog("正运动控制卡未连接");
                    return;
                }

                int ret = _platformService.SendCommand((ushort)ZMotionPlatformCommand.Home);
                
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

        /// <summary>
        /// 点动运动 - 按下时调用
        /// </summary>
        /// <param name="positive">true为正方向(右移)，false为负方向(左移)</param>
        public void JogMove(bool positive)
        {
            try
            {
                if (!ModelParam.ZMotionConnected || !_platformService.IsConnected)
                {
                    return;
                }

                ushort address = positive
                    ? ZMotionPlatformProtocol.JogPositive
                    : ZMotionPlatformProtocol.JogNegative;
                int ret = _platformService.WriteCoil(address, true);
                
                if (ret == 0)
                {
                    ModelParam.AddLog($"点动{(positive ? "右移" : "左移")}开始");
                }
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"点动失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 点动停止 - 松开时调用
        /// </summary>
        public void JogStop()
        {
            try
            {
                if (!ModelParam.ZMotionConnected || !_platformService.IsConnected)
                {
                    return;
                }

                _platformService.WriteCoil(ZMotionPlatformProtocol.JogNegative, false);
                _platformService.WriteCoil(ZMotionPlatformProtocol.JogPositive, false);
                
                ModelParam.AddLog("点动停止");
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"点动停止失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 读取所有运动参数（从控制卡读取实时值）
        /// </summary>
        private void ReadMotionParams()
        {
            try
            {
                if (!ModelParam.ZMotionConnected || !_platformService.IsConnected)
                {
                    ModelParam.AddLog("正运动控制卡未连接");
                    return;
                }

                int ret = _platformService.ReadFloat(ZMotionPlatformProtocol.ReciprocateSpeed, out var speed);
                if (ret == 0)
                {
                    ModelParam.ForwardSpeed = speed;
                }

                ret = _platformService.ReadFloat(ZMotionPlatformProtocol.ReciprocatePositivePosition, out var distance);
                if (ret == 0)
                {
                    ModelParam.WorkDistance = distance;
                }

                ModelParam.AddLog($"运动参数已读取 - 往复速度:{ModelParam.ForwardSpeed}, 工作距离:{ModelParam.WorkDistance}");
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"读取运动参数失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 保存所有运动参数（写入控制卡）
        /// </summary>
        private void SaveMotionParams()
        {
            try
            {
                if (!ModelParam.ZMotionConnected || !_platformService.IsConnected)
                {
                    ModelParam.AddLog("正运动控制卡未连接");
                    return;
                }

                int ret = _platformService.WriteFloat(ZMotionPlatformProtocol.ReciprocateSpeed, ModelParam.ForwardSpeed);
                if (ret != 0)
                {
                    ModelParam.AddLog($"设置往复速度失败，错误码：{ret}");
                }

                ret = _platformService.WriteFloat(ZMotionPlatformProtocol.ReciprocatePositivePosition, ModelParam.WorkDistance);
                if (ret != 0)
                {
                    ModelParam.AddLog($"设置工作距离失败，错误码：{ret}");
                }

                ModelParam.AddLog($"运动参数已保存 - 往复速度:{ModelParam.ForwardSpeed}, 工作距离:{ModelParam.WorkDistance}");
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"保存运动参数失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 读取回原速度
        /// </summary>
        private void ReadHomeSpeed()
        {
            try
            {
                if (!ModelParam.ZMotionConnected || !_platformService.IsConnected)
                {
                    ModelParam.AddLog("正运动控制卡未连接");
                    return;
                }

                int ret = _platformService.ReadFloat(ZMotionPlatformProtocol.HomeFastSpeed, out var homeSpeed);
                if (ret == 0)
                {
                    ModelParam.HomeSpeed = homeSpeed;
                    ModelParam.AddLog($"回原速度：{homeSpeed}");
                }
                else
                {
                    ModelParam.AddLog($"读取回原速度失败，错误码：{ret}");
                }
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"读取回原速度异常：{ex.Message}");
            }
        }

        /// <summary>
        /// 设置回原速度
        /// </summary>
        private void SetHomeSpeed()
        {
            try
            {
                if (!ModelParam.ZMotionConnected || !_platformService.IsConnected)
                {
                    ModelParam.AddLog("正运动控制卡未连接");
                    return;
                }

                int ret = _platformService.WriteFloat(ZMotionPlatformProtocol.HomeFastSpeed, ModelParam.HomeSpeed);
                if (ret == 0)
                {
                    ModelParam.AddLog($"回原速度已设置为：{ModelParam.HomeSpeed}");
                }
                else
                {
                    ModelParam.AddLog($"设置回原速度失败，错误码：{ret}");
                }
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"设置回原速度异常：{ex.Message}");
            }
        }
        #endregion

        #region 埃科相机方法
        // IKap SDK是否已初始化
        private static bool _ikapInitialized = false;
        private static readonly string[] s_requiredIKapDlls =
        {
            "IKapC.dll",
            "IKapBoard.dll",
            "IKapCDotNet2.dll",
            "IKapBoardDotNet2.dll"
        };

        private bool EnsureIKapHandles()
        {
            if (s_hDev != null && s_hStream != null)
            {
                return true;
            }

            try
            {
                s_hDev ??= new ITKDEVICE();
                s_hStream ??= new ITKSTREAM();
                return true;
            }
            catch (Exception ex) when (ex is DllNotFoundException || ex is TypeInitializationException)
            {
                LogIKapRuntimeException(ex);
                ResetIKapHandles();
                return false;
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"IKap SDK句柄创建失败：{ex.GetType().Name} - {ex.Message}");
                ResetIKapHandles();
                return false;
            }
        }

        private void LogIKapRuntimeException(Exception ex)
        {
            Exception root = ex.GetBaseException();
            ModelParam.AddLog($"IKap SDK运行库加载失败：{root.GetType().Name} - {root.Message}");
            ModelParam.AddLog("请确认 ReeYin.exe 同级目录存在 IKap 运行库，并在目标电脑安装 IKap 驱动和 VC++ x64 运行库。");

            string baseDirectory = AppContext.BaseDirectory;
            foreach (string dllName in s_requiredIKapDlls)
            {
                string dllPath = Path.Combine(baseDirectory, dllName);
                if (!File.Exists(dllPath))
                {
                    ModelParam.AddLog($"缺少 IKap 运行库文件：{dllPath}");
                }
            }
        }

        private static void ResetIKapHandles()
        {
            s_hDev = null;
            s_hStream = null;
        }
        
        /// <summary>
        /// 初始化IKap SDK环境
        /// </summary>
        private bool InitIKapEnvironment()
        {
            if (_ikapInitialized)
                return true;
                
            try
            {
                uint res = IKapC.ItkManInitialize();
                if (res == IKapC.ITKSTATUS_OK)
                {
                    _ikapInitialized = true;
                    ModelParam.AddLog("IKap SDK初始化成功");
                    return true;
                }
                else
                {
                    ModelParam.AddLog($"IKap SDK初始化失败，错误码：0x{res:X8}");
                    return false;
                }
            }
            catch (TypeInitializationException ex)
            {
                LogIKapRuntimeException(ex);
                // 获取更详细的内部异常信息
                string innerMsg = ex.InnerException?.Message ?? "无内部异常";
                string innerType = ex.InnerException?.GetType().Name ?? "未知";
                ModelParam.AddLog($"IKap SDK类型初始化失败：{innerType} - {innerMsg}");
                return false;
            }
            catch (DllNotFoundException ex)
            {
                LogIKapRuntimeException(ex);
                ModelParam.AddLog($"IKap SDK缺少DLL：{ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"IKap SDK初始化异常：{ex.GetType().Name} - {ex.Message}");
                if (ex.InnerException != null)
                {
                    ModelParam.AddLog($"  内部异常：{ex.InnerException.Message}");
                }
                return false;
            }
        }
        
        /// <summary>
        /// 枚举IKap相机设备
        /// </summary>
        private void EnumerateIKapDevices()
        {
            try
            {
                if (!InitIKapEnvironment())
                    return;
                    
                uint nDevCount = 0;
                uint res = IKapC.ItkManGetDeviceCount(ref nDevCount);
                if (res != IKapC.ITKSTATUS_OK)
                {
                    ModelParam.AddLog($"获取设备数量失败，错误码：0x{res:X8}");
                    return;
                }
                
                ModelParam.AddLog($"发现 {nDevCount} 个IKap设备");
                
                if (nDevCount == 0)
                {
                    ModelParam.AddLog("未找到IKap相机设备，请检查：");
                    ModelParam.AddLog("  1. 相机是否正确连接");
                    ModelParam.AddLog("  2. IKap驱动是否正确安装");
                    ModelParam.AddLog("  3. 相机是否被其他程序占用");
                    return;
                }
                
                // 枚举所有设备
                ITKDEV_INFO pDevInfo = new ITKDEV_INFO();
                for (uint i = 0; i < nDevCount; i++)
                {
                    res = IKapC.ItkManGetDeviceInfo(i, pDevInfo);
                    if (res == IKapC.ITKSTATUS_OK)
                    {
                        ModelParam.AddLog($"设备[{i}]: {pDevInfo.FullName}");
                        ModelParam.AddLog($"  序列号: {pDevInfo.SerialNumber}");
                        ModelParam.AddLog($"  设备类型: {pDevInfo.DeviceClass}");
                    }
                }
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"枚举设备异常：{ex.Message}");
            }
        }
        
        private async void ConnectIKapCamera()
        {
            try
            {
                ModelParam.AddLog("正在连接埃科相机...");
                
                bool success = await Task.Run(() =>
                {
                    // 首先初始化SDK环境
                    if (!InitIKapEnvironment())
                        return false;

                    if (!EnsureIKapHandles())
                        return false;
                    
                    // 获取设备数量
                    uint nDevCount = 0;
                    uint res = IKapC.ItkManGetDeviceCount(ref nDevCount);
                    if (res != IKapC.ITKSTATUS_OK)
                    {
                        Application.Current.Dispatcher.Invoke(() => 
                            ModelParam.AddLog($"获取设备数量失败，错误码：0x{res:X8}"));
                        return false;
                    }
                    
                    Application.Current.Dispatcher.Invoke(() => 
                        ModelParam.AddLog($"发现 {nDevCount} 个IKap设备"));
                    
                    if (nDevCount == 0)
                    {
                        Application.Current.Dispatcher.Invoke(() => 
                            ModelParam.AddLog("未找到IKap相机设备"));
                        return false;
                    }
                    
                    // 检查设备索引是否有效
                    if (ModelParam.CameraDeviceIndex < 0 || ModelParam.CameraDeviceIndex >= nDevCount)
                    {
                        Application.Current.Dispatcher.Invoke(() => 
                            ModelParam.AddLog($"设备索引 {ModelParam.CameraDeviceIndex} 无效，有效范围：0-{nDevCount - 1}"));
                        return false;
                    }
                    
                    // 获取设备信息
                    ITKDEV_INFO pDevInfo = new ITKDEV_INFO();
                    res = IKapC.ItkManGetDeviceInfo((uint)ModelParam.CameraDeviceIndex, pDevInfo);
                    if (res == IKapC.ITKSTATUS_OK)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            ModelParam.AddLog($"正在连接设备: {pDevInfo.FullName}");
                            ModelParam.AddLog($"  设备类型: {pDevInfo.DeviceClass}");
                            ModelParam.CameraSerialNumber = pDevInfo.SerialNumber;
                            
                            // 从FullName中提取IP地址（格式通常为 "设备名@IP地址"）
                            if (pDevInfo.DeviceClass == "GigEVision" && pDevInfo.FullName.Contains("@"))
                            {
                                var parts = pDevInfo.FullName.Split('@');
                                if (parts.Length > 1)
                                {
                                    ModelParam.CameraIpAddress = parts[parts.Length - 1];
                                }
                            }
                        });
                    }
                    
                    // 打开相机
                    res = IKapC.ItkDevOpen((uint)ModelParam.CameraDeviceIndex, IKapC.ITKDEV_VAL_ACCESS_MODE_EXCLUSIVE, ref s_hDev);
                    if (res != IKapC.ITKSTATUS_OK)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            ModelParam.AddLog($"打开相机失败，错误码：0x{res:X8}");
                            // 常见错误码说明
                            if (res == 0x410005)
                                ModelParam.AddLog("  可能原因：设备被其他程序占用");
                            else if (res == 0x410004)
                                ModelParam.AddLog("  可能原因：设备不存在或已断开");
                        });
                        return false;
                    }

                    // 如果使用采集卡
                    if (ModelParam.CameraBoardIndex != -1)
                    {
                        s_pBoard = IKapBoard.IKapOpen((uint)IKapBoard.IKBoardALL, (uint)ModelParam.CameraBoardIndex);
                        if (s_pBoard == new IntPtr(-1))
                        {
                            Application.Current.Dispatcher.Invoke(() => 
                                ModelParam.AddLog("打开采集卡失败"));
                            IKapC.ItkDevClose(s_hDev);
                            return false;
                        }
                    }

                    // 获取图像参数
                    GetCameraImageInfo();
                    return true;
                });

                if (success)
                {
                    s_cameraConnected = true;
                    ModelParam.CameraConnected = true;
                    ModelParam.AddLog("埃科相机连接成功！");
                    ReadCameraParams(false);
                }
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"连接埃科相机异常：{ex.Message}");
            }
        }

        private async void DisconnectIKapCamera()
        {
            try
            {
                ModelParam.AddLog("正在断开埃科相机...");
                
                // 先停止采集
                StopContinuousGrab();
                
                await Task.Run(() =>
                {
                    // 释放采集流
                    ReleaseStream();
                    
                    if (s_pBoard != new IntPtr(-1))
                    {
                        IKapBoard.IKapClose(s_pBoard);
                        s_pBoard = new IntPtr(-1);
                    }

                    if (s_hDev != null)
                    {
                        IKapC.ItkDevClose(s_hDev);
                    }

                    ResetIKapHandles();
                });

                s_cameraConnected = false;
                ModelParam.CameraConnected = false;
                ModelParam.ImageWidth = 0;
                ModelParam.ImageHeight = 0;
                ModelParam.CameraSerialNumber = "";
                ModelParam.CameraIpAddress = "";
                s_nWidth = 0;
                s_nHeight = 0;
                s_nBufferSize = 0;
                
                ModelParam.AddLog("埃科相机已断开连接");
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"断开埃科相机失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 获取相机图像信息
        /// </summary>
        private void GetCameraImageInfo()
        {
            try
            {
                uint res;
                
                // 获取图像宽度
                long width = 0;
                res = IKapC.ItkDevGetInt64(s_hDev, "Width", ref width);
                if (res == IKapC.ITKSTATUS_OK)
                {
                    s_nWidth = (int)width;
                    ModelParam.ImageWidth = s_nWidth;
                }
                
                // 获取图像高度
                long height = 0;
                res = IKapC.ItkDevGetInt64(s_hDev, "Height", ref height);
                if (res == IKapC.ITKSTATUS_OK)
                {
                    s_nHeight = (int)height;
                    ModelParam.ImageHeight = s_nHeight;
                }
                
                // 获取PayloadSize
                long payloadSize = 0;
                res = IKapC.ItkDevGetInt64(s_hDev, "PayloadSize", ref payloadSize);
                if (res == IKapC.ITKSTATUS_OK)
                {
                    s_nBufferSize = (int)payloadSize;
                }
                
                ModelParam.AddLog($"图像参数: {s_nWidth} x {s_nHeight}, 缓冲区大小: {s_nBufferSize}");
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"获取图像参数失败：{ex.Message}");
            }
        }

        private bool TrySetCameraStringParam(string key, string value, bool logOnFail = false)
        {
            uint res = IKapC.ItkDevFromString(s_hDev, key, value);
            if (res == IKapC.ITKSTATUS_OK)
                return true;

            if (logOnFail)
                ModelParam.AddLog($"设置参数 {key}={value} 失败：0x{res:X8}");

            return false;
        }

        private bool TrySetCameraDoubleParam(double value, params string[] keys)
        {
            foreach (string key in keys)
            {
                uint res = IKapC.ItkDevSetDouble(s_hDev, key, value);
                if (res == IKapC.ITKSTATUS_OK)
                    return true;
            }

            return false;
        }

        private bool TrySetCameraLongParam(long value, params string[] keys)
        {
            foreach (string key in keys)
            {
                uint res = IKapC.ItkDevSetInt64(s_hDev, key, value);
                if (res == IKapC.ITKSTATUS_OK)
                    return true;
            }

            return false;
        }

        private bool TryGetCameraDoubleParam(out double value, params string[] keys)
        {
            foreach (string key in keys)
            {
                double temp = 0;
                uint res = IKapC.ItkDevGetDouble(s_hDev, key, ref temp);
                if (res == IKapC.ITKSTATUS_OK)
                {
                    value = temp;
                    return true;
                }
            }

            value = 0;
            return false;
        }

        private bool TryGetCameraLongParam(out long value, params string[] keys)
        {
            foreach (string key in keys)
            {
                long temp = 0;
                uint res = IKapC.ItkDevGetInt64(s_hDev, key, ref temp);
                if (res == IKapC.ITKSTATUS_OK)
                {
                    value = temp;
                    return true;
                }
            }

            value = 0;
            return false;
        }

        private void PrepareCameraForManualParamApply()
        {
            // 不同型号支持的节点不完全一致，尽量切到手动控制模式，失败则忽略。
            TrySetCameraStringParam("ExposureAuto", "Off");
            TrySetCameraStringParam("GainAuto", "Off");
            TrySetCameraStringParam("GainSelector", "All");
            TrySetCameraStringParam("ExposureMode", "Timed");
        }

        private bool TrySetExposureTime(double exposureTime)
        {
            if (TrySetCameraDoubleParam(exposureTime, "ExposureTime", "ExposureTimeAbs"))
                return true;

            return TrySetCameraLongParam((long)Math.Round(exposureTime), "ExposureTimeRaw");
        }

        private bool TryReadExposureTime(out double exposureTime)
        {
            if (TryGetCameraDoubleParam(out exposureTime, "ExposureTime", "ExposureTimeAbs"))
                return true;

            if (TryGetCameraLongParam(out long rawExposure, "ExposureTimeRaw"))
            {
                exposureTime = rawExposure;
                return true;
            }

            exposureTime = 0;
            return false;
        }

        private bool TrySetGain(double gain)
        {
            if (TrySetCameraDoubleParam(gain, "Gain"))
                return true;

            return TrySetCameraLongParam((long)Math.Round(gain), "GainRaw");
        }

        private bool TryReadGain(out double gain)
        {
            if (TryGetCameraDoubleParam(out gain, "Gain"))
                return true;

            if (TryGetCameraLongParam(out long rawGain, "GainRaw"))
            {
                gain = rawGain;
                return true;
            }

            gain = 0;
            return false;
        }

        private bool TrySetLineRate(double lineRate)
        {
            if (TrySetCameraDoubleParam(lineRate, "AcquisitionLineRate", "LineRate"))
                return true;

            if (lineRate > 0)
            {
                double linePeriodTime = 1000000.0 / lineRate;
                return TrySetCameraDoubleParam(linePeriodTime, "LinePeriodTime");
            }

            return false;
        }

        private bool TryReadLineRate(out double lineRate)
        {
            if (TryGetCameraDoubleParam(out lineRate, "AcquisitionLineRate", "LineRate"))
                return true;

            if (TryGetCameraDoubleParam(out double linePeriodTime, "LinePeriodTime") && linePeriodTime > 0)
            {
                lineRate = 1000000.0 / linePeriodTime;
                return true;
            }

            lineRate = 0;
            return false;
        }

        private void ReadCameraParams(bool logResult = true)
        {
            try
            {
                if (!ModelParam.CameraConnected)
                {
                    if (logResult)
                        ModelParam.AddLog("相机未连接");
                    return;
                }

                bool hasAnyValue = false;

                if (TryReadExposureTime(out double exposureTime))
                {
                    ModelParam.ExposureTime = (float)exposureTime;
                    hasAnyValue = true;
                }

                if (TryReadGain(out double gain))
                {
                    ModelParam.Gain = (float)gain;
                    hasAnyValue = true;
                }

                if (TryReadLineRate(out double lineRate))
                {
                    ModelParam.LineRate = lineRate;
                    hasAnyValue = true;
                }

                GetCameraImageInfo();
                if (s_nHeight > 0)
                {
                    ModelParam.ScanLineCount = s_nHeight;
                    hasAnyValue = true;
                }

                if (logResult)
                {
                    if (hasAnyValue)
                    {
                        ModelParam.AddLog(
                            $"相机参数已读取 - 曝光:{ModelParam.ExposureTime:F0} μs, 增益:{ModelParam.Gain:F2}, 行频:{ModelParam.LineRate:F2} Hz, 行数:{ModelParam.ScanLineCount}");
                    }
                    else
                    {
                        ModelParam.AddLog("未能读取到相机采集参数");
                    }
                }
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"读取相机参数失败：{ex.Message}");
            }
        }

        private bool ApplyScanLineCountToCamera()
        {
            try
            {
                if (ModelParam.ScanLineCount <= 0)
                {
                    ModelParam.AddLog("扫描行数必须大于 0");
                    return false;
                }

                uint res = IKapC.ItkDevSetInt64(s_hDev, "Height", ModelParam.ScanLineCount);
                if (res != IKapC.ITKSTATUS_OK)
                {
                    ModelParam.AddLog($"设置扫描行数失败：0x{res:X8}");
                    return false;
                }

                GetCameraImageInfo();

                if (s_nHeight > 0)
                {
                    ModelParam.ScanLineCount = s_nHeight;
                    ModelParam.AddLog($"扫描行数已设置为：{ModelParam.ScanLineCount}");
                }

                return true;
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"设置扫描行数异常：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 创建采集流（按照官方示例GeneralGrab）
        /// </summary>
        private bool CreateStream()
        {
            if (s_bStreamCreated)
                return true;
                
            try
            {
                // 申请数据流资源
                uint res = IKapC.ItkDevAllocStreamEx(s_hDev, 0, m_nFrameCount, ref s_hStream);
                if (res != IKapC.ITKSTATUS_OK)
                {
                    ModelParam.AddLog($"创建采集流失败，错误码：0x{res:X8}");
                    return false;
                }
                
                // 获取缓冲区对象
                ITKBUFFER hBuffer = new ITKBUFFER();
                res = IKapC.ItkStreamGetBuffer(s_hStream, 0, ref hBuffer);
                if (res != IKapC.ITKSTATUS_OK)
                {
                    ModelParam.AddLog($"获取缓冲区失败，错误码：0x{res:X8}");
                    return false;
                }
                
                // 获取缓冲区信息
                ITK_BUFFER_INFO bufferInfo = new ITK_BUFFER_INFO();
                res = IKapC.ItkBufferGetInfo(hBuffer, bufferInfo);
                if (res != IKapC.ITKSTATUS_OK)
                {
                    ModelParam.AddLog($"获取缓冲区信息失败，错误码：0x{res:X8}");
                    return false;
                }
                
                // 申请用户缓冲区
                s_pUserBuffer = Marshal.AllocHGlobal((int)bufferInfo.TotalSize);
                if (s_pUserBuffer == IntPtr.Zero)
                {
                    ModelParam.AddLog("申请用户缓冲区失败");
                    return false;
                }
                
                s_bStreamCreated = true;
                ModelParam.AddLog("采集流创建成功");
                return true;
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"创建采集流异常：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 释放采集流
        /// </summary>
        private void ReleaseStream()
        {
            if (!s_bStreamCreated)
                return;
                
            try
            {
                IKapC.ItkDevFreeStream(s_hStream);
                
                if (s_pUserBuffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(s_pUserBuffer);
                    s_pUserBuffer = IntPtr.Zero;
                }
                
                s_bStreamCreated = false;
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"释放采集流异常：{ex.Message}");
            }
        }

        private void StartLineScan()
        {
            try
            {
                if (!ModelParam.CameraConnected)
                {
                    ModelParam.AddLog("埃科相机未连接");
                    return;
                }

                if (ModelParam.ScanLineCount <= 0)
                {
                    ModelParam.AddLog("扫描行数必须大于 0");
                    return;
                }

                if (m_bGrabbing || ModelParam.IsContinuousGrab)
                {
                    m_bGrabbing = false;
                    ModelParam.IsContinuousGrab = false;
                    UnregisterFrameCallback();

                    if (s_bStreamCreated)
                    {
                        IKapC.ItkStreamStop(s_hStream);
                        ReleaseStream();
                    }
                }
                else if (s_bStreamCreated)
                {
                    IKapC.ItkStreamStop(s_hStream);
                    ReleaseStream();
                }

                if (!ApplyScanLineCountToCamera())
                    return;

                if (!CreateStream())
                    return;

                ModelParam.IsScanning = true;
                ModelParam.CurrentLineCount = 0;
                ModelParam.AddLog($"开始线扫采集，目标行数：{ModelParam.ScanLineCount}");

                // 线扫高度由相机 Height 决定，这里只采集 1 帧线扫图像。
                uint res = IKapC.ItkStreamStart(s_hStream, 1);
                if (res != IKapC.ITKSTATUS_OK)
                {
                    ModelParam.AddLog($"启动采集失败，错误码：0x{res:X8}");
                    ModelParam.IsScanning = false;
                    return;
                }

                // 异步等待采集完成
                Task.Run(() => WaitForLineScanComplete());
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"启动线扫失败：{ex.Message}");
                ModelParam.IsScanning = false;
            }
        }

        private void WaitForLineScanComplete()
        {
            try
            {
                uint res = IKapC.ItkStreamWait(s_hStream);
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (res == IKapC.ITKSTATUS_OK)
                    {
                        ModelParam.AddLog("线扫采集完成");
                        // 获取并显示图像
                        GrabAndDisplayImage();
                    }
                    else
                    {
                        ModelParam.AddLog($"线扫采集异常，错误码：0x{res:X8}");
                    }
                    ModelParam.IsScanning = false;
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ModelParam.AddLog($"等待采集完成异常：{ex.Message}");
                    ModelParam.IsScanning = false;
                });
            }
        }

        private void StopLineScan()
        {
            try
            {
                if (s_bStreamCreated)
                {
                    IKapC.ItkStreamStop(s_hStream);
                }
                ModelParam.IsScanning = false;
                ModelParam.AddLog("线扫采集已停止");
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"停止线扫失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 单次采集
        /// </summary>
        private void GrabSingleImage()
        {
            try
            {
                if (!ModelParam.CameraConnected)
                {
                    ModelParam.AddLog("相机未连接");
                    return;
                }

                // 先停止之前的采集（如果有）
                if (m_bGrabbing || ModelParam.IsContinuousGrab)
                {
                    UnregisterFrameCallback();
                    IKapC.ItkStreamStop(s_hStream);
                    m_bGrabbing = false;
                    ModelParam.IsContinuousGrab = false;
                }

                if (s_bStreamCreated)
                {
                    ReleaseStream();
                }

                ModelParam.AddLog("执行单次采集...");

                if (!ApplyScanLineCountToCamera())
                    return;

                if (!CreateStream())
                    return;

                // 设置触发模式为软触发（如果相机支持）
                SetSoftwareTriggerMode();

                // 启动单帧采集
                uint res = IKapC.ItkStreamStart(s_hStream, 1);
                if (res != IKapC.ITKSTATUS_OK)
                {
                    ModelParam.AddLog($"启动采集失败，错误码：0x{res:X8}");
                    return;
                }

                m_bGrabbing = true;
                // 发送软触发命令
                SendSoftwareTrigger();

                // 异步等待采集完成（带超时）
                Task.Run(() =>
                {
                    try
                    {
                        // 等待采集完成，最多等待5秒
                        var waitTask = Task.Run(() => IKapC.ItkStreamWait(s_hStream));
                        if (waitTask.Wait(5000))
                        {
                            uint waitRes = waitTask.Result;
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                m_bGrabbing = false;
                                if (waitRes == IKapC.ITKSTATUS_OK)
                                {
                                    GrabAndDisplayImage();
                                    ModelParam.AddLog("单次采集完成");
                                }
                                else
                                {
                                    ModelParam.AddLog($"等待采集失败，错误码：0x{waitRes:X8}");
                                }
                            });
                        }
                        else
                        {
                            // 超时，停止采集
                            IKapC.ItkStreamStop(s_hStream);
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                m_bGrabbing = false;
                                ModelParam.AddLog("采集超时（5秒），请检查触发信号或切换为连续模式");
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            m_bGrabbing = false;
                            ModelParam.AddLog($"采集异常：{ex.Message}");
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"单次采集失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 设置软触发模式（按照官方示例GeneralGrab）
        /// </summary>
        private void SetSoftwareTriggerMode()
        {
            try
            {
                uint res;
                res = IKapC.ItkDevFromString(s_hDev, "TriggerSelector", "FrameStart");
                res = IKapC.ItkDevFromString(s_hDev, "TriggerMode", "On");
                res = IKapC.ItkDevFromString(s_hDev, "TriggerSource", "Software");
                ModelParam.AddLog("设置软触发模式");
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"设置触发模式失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 发送软触发命令（按照官方示例GeneralGrab）
        /// </summary>
        private void SendSoftwareTrigger()
        {
            try
            {
                uint res = IKapC.ItkDevExecuteCommand(s_hDev, "TriggerSoftware");
                if (res == IKapC.ITKSTATUS_OK)
                {
                    ModelParam.AddLog("发送软触发成功");
                }
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"发送软触发失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 获取并显示图像（按照官方示例GeneralGrab）
        /// </summary>
        private void GrabAndDisplayImage()
        {
            try
            {
                uint res;
                
                // 尝试获取当前缓冲区
                ITKBUFFER hCurrBuffer = new ITKBUFFER();
                res = IKapC.ItkStreamGetCurrentBuffer(s_hStream, ref hCurrBuffer);
                
                // 如果获取当前缓冲区失败，尝试获取索引为0的缓冲区
                if (res != IKapC.ITKSTATUS_OK)
                {
                    res = IKapC.ItkStreamGetBuffer(s_hStream, 0, ref hCurrBuffer);
                    if (res != IKapC.ITKSTATUS_OK)
                    {
                        ModelParam.AddLog($"获取缓冲区失败，错误码：0x{res:X8}");
                        return;
                    }
                }

                // 获取缓冲区信息
                ITK_BUFFER_INFO bufferInfo = new ITK_BUFFER_INFO();
                res = IKapC.ItkBufferGetInfo(hCurrBuffer, bufferInfo);
                if (res != IKapC.ITKSTATUS_OK)
                {
                    // 如果仍然失败，尝试直接读取图像数据
                    ModelParam.AddLog($"获取缓冲区信息失败(0x{res:X8})，尝试直接读取...");
                    
                    // 使用创建流时获取的缓冲区大小
                    if (s_pUserBuffer != IntPtr.Zero && s_nBufferSize > 0)
                    {
                        res = IKapC.ItkBufferRead(hCurrBuffer, 0, s_pUserBuffer, (uint)s_nBufferSize);
                        if (res == IKapC.ITKSTATUS_OK)
                        {
                            ConvertAndDisplayImage(s_nBufferSize);
                            return;
                        }
                    }
                    return;
                }

                uint bufferStatus = bufferInfo.State;
                // 当图像缓冲区满或者图像缓冲区非满但是无法采集完整的一帧图像时
                if (bufferStatus == IKapC.ITKBUFFER_VAL_STATE_FULL || bufferStatus == IKapC.ITKBUFFER_VAL_STATE_UNCOMPLETED)
                {
                    // 读取缓冲区数据
                    ulong nImageSize = bufferInfo.ValidImageSize;
                    if (s_pUserBuffer != IntPtr.Zero && nImageSize > 0)
                    {
                        res = IKapC.ItkBufferRead(hCurrBuffer, 0, s_pUserBuffer, (uint)nImageSize);
                        if (res != IKapC.ITKSTATUS_OK)
                        {
                            ModelParam.AddLog($"读取缓冲区数据失败，错误码：0x{res:X8}");
                            return;
                        }
                        
                        ConvertAndDisplayImage((int)nImageSize);
                    }
                    else
                    {
                        ModelParam.AddLog($"缓冲区数据无效: size={nImageSize}");
                    }
                }
                else
                {
                    ModelParam.AddLog($"缓冲区状态：{bufferStatus} (等待数据...)");
                }
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"获取图像失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 转换并显示图像
        /// </summary>
        // 图像显示节流相关
        private DateTime _lastDisplayTime = DateTime.MinValue;
        private const int DisplayIntervalMs = 100; // 显示间隔100ms，约10fps显示刷新
        private const int MaxDisplayWidth = 1024; // 显示用的最大宽度
        private const int MaxDisplayHeight = 1024; // 显示用的最大高度
        
        private void ConvertAndDisplayImage(int validImageSize)
        {
            try
            {
                if (s_pUserBuffer == IntPtr.Zero || s_nWidth <= 0 || s_nBufferSize <= 0)
                    return;

                int sourceSize = validImageSize > 0 ? Math.Min(validImageSize, s_nBufferSize) : s_nBufferSize;
                int effectiveSize = sourceSize - (sourceSize % s_nWidth);
                int effectiveHeight = effectiveSize / s_nWidth;

                if (effectiveSize <= 0 || effectiveHeight <= 0)
                    return;

                // 复制数据到托管数组（在后台线程完成）
                byte[] imageData = new byte[effectiveSize];
                Marshal.Copy(s_pUserBuffer, imageData, 0, effectiveSize);

                // 如果开启了连续保存，将原始图像数据加入保存队列
                if (ModelParam.IsContinuousSave && _saveQueue != null)
                {
                    byte[] dataToSave = new byte[imageData.Length];
                    Array.Copy(imageData, dataToSave, imageData.Length);
                    
                    var item = new ImageSaveItem
                    {
                        ImageData = dataToSave,
                        Width = s_nWidth,
                        Height = effectiveHeight,
                        SavePath = ModelParam.ImageSavePath,
                        Prefix = ModelParam.ImagePrefix,
                        Format = ModelParam.ImageFormat,
                        Timestamp = DateTime.Now
                    };
                    _saveQueue.TryAdd(item);
                }

                // 节流：限制显示刷新频率，避免UI卡顿
                var now = DateTime.Now;
                if ((now - _lastDisplayTime).TotalMilliseconds < DisplayIntervalMs)
                    return;
                _lastDisplayTime = now;

                // 在后台线程创建缩略图用于显示
                int displayWidth = s_nWidth;
                int displayHeight = effectiveHeight;
                byte[] displayData = imageData;
                int displayStride = s_nWidth;
                
                // 如果图像太大，创建缩略图
                if (s_nWidth > MaxDisplayWidth || effectiveHeight > MaxDisplayHeight)
                {
                    float scaleX = (float)MaxDisplayWidth / s_nWidth;
                    float scaleY = (float)MaxDisplayHeight / effectiveHeight;
                    float scale = Math.Min(scaleX, scaleY);
                    
                    displayWidth = (int)(s_nWidth * scale);
                    displayHeight = (int)(effectiveHeight * scale);
                    displayStride = displayWidth;
                    
                    // 简单的最近邻缩放（快速）
                    displayData = DownscaleImage(imageData, s_nWidth, effectiveHeight, displayWidth, displayHeight);
                }

                // 在后台线程创建BitmapSource
                BitmapSource bitmap = BitmapSource.Create(
                    displayWidth, displayHeight,
                    96, 96,
                    PixelFormats.Gray8,
                    null,
                    displayData,
                    displayStride);
                
                bitmap.Freeze(); // 冻结以便跨线程使用

                // 使用BeginInvoke异步更新UI，不阻塞
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        DisplayImage = bitmap;
                        ModelParam.ImageWidth = s_nWidth;
                        ModelParam.ImageHeight = effectiveHeight;
                        ModelParam.CurrentLineCount = effectiveHeight;
                    }
                    catch (Exception ex)
                    {
                        ModelParam.AddLog($"显示图像失败：{ex.Message}");
                    }
                }), DispatcherPriority.Render);
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"转换图像失败：{ex.Message}");
            }
        }
        
        /// <summary>
        /// 快速缩放图像（最近邻算法）
        /// </summary>
        private byte[] DownscaleImage(byte[] source, int srcWidth, int srcHeight, int dstWidth, int dstHeight)
        {
            byte[] dest = new byte[dstWidth * dstHeight];
            float xRatio = (float)srcWidth / dstWidth;
            float yRatio = (float)srcHeight / dstHeight;
            
            for (int y = 0; y < dstHeight; y++)
            {
                int srcY = (int)(y * yRatio);
                int srcRowOffset = srcY * srcWidth;
                int dstRowOffset = y * dstWidth;
                
                for (int x = 0; x < dstWidth; x++)
                {
                    int srcX = (int)(x * xRatio);
                    dest[dstRowOffset + x] = source[srcRowOffset + srcX];
                }
            }
            
            return dest;
        }

        /// <summary>
        /// 保存当前图像
        /// </summary>
        private void SaveCurrentImage()
        {
            try
            {
                if (DisplayImage == null)
                {
                    ModelParam.AddLog("没有可保存的图像");
                    return;
                }

                if (!(DisplayImage is BitmapSource bitmap))
                {
                    ModelParam.AddLog("图像格式不支持保存");
                    return;
                }

                SaveImageInternal(bitmap, false);
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"保存图像失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 内部保存图像方法（同步，用于单次保存）
        /// </summary>
        /// <param name="bitmap">要保存的图像</param>
        /// <param name="isAutoSave">是否为自动保存（连续保存模式）</param>
        private void SaveImageInternal(BitmapSource bitmap, bool isAutoSave)
        {
            try
            {
                // 确保保存目录存在
                if (!Directory.Exists(ModelParam.ImageSavePath))
                {
                    Directory.CreateDirectory(ModelParam.ImageSavePath);
                }

                // 生成文件名：前缀_日期时间_序号.格式
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                string fileName = $"{ModelParam.ImagePrefix}_{timestamp}.{ModelParam.ImageFormat}";
                string filePath = Path.Combine(ModelParam.ImageSavePath, fileName);

                // 根据格式选择编码器
                BitmapEncoder encoder = GetBitmapEncoder(ModelParam.ImageFormat);
                encoder.Frames.Add(BitmapFrame.Create(bitmap));

                // 保存文件
                using (FileStream fs = new FileStream(filePath, FileMode.Create))
                {
                    encoder.Save(fs);
                }

                ModelParam.SavedImageCount++;
                
                if (!isAutoSave)
                {
                    ModelParam.AddLog($"图像已保存：{fileName}");
                }
                else if (ModelParam.SavedImageCount % 10 == 0) // 连续保存时每10张输出一次日志
                {
                    ModelParam.AddLog($"已连续保存 {ModelParam.SavedImageCount} 张图像");
                }
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"保存图像失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 根据格式获取对应的编码器
        /// </summary>
        private BitmapEncoder GetBitmapEncoder(string format)
        {
            switch (format.ToLower())
            {
                case "png":
                    return new PngBitmapEncoder();
                case "jpg":
                case "jpeg":
                    var jpegEncoder = new JpegBitmapEncoder();
                    jpegEncoder.QualityLevel = 95;
                    return jpegEncoder;
                case "tiff":
                case "tif":
                    return new TiffBitmapEncoder();
                case "bmp":
                default:
                    return new BmpBitmapEncoder();
            }
        }

        /// <summary>
        /// 开始连续保存
        /// </summary>
        private void StartContinuousSave()
        {
            if (!ModelParam.CameraConnected)
            {
                ModelParam.AddLog("相机未连接");
                return;
            }

            // 确保保存目录存在
            if (!Directory.Exists(ModelParam.ImageSavePath))
            {
                try
                {
                    Directory.CreateDirectory(ModelParam.ImageSavePath);
                }
                catch (Exception ex)
                {
                    ModelParam.AddLog($"创建保存目录失败：{ex.Message}");
                    return;
                }
            }

            _savedImageCount = 0;
            ModelParam.SavedImageCount = 0;
            
            // 创建保存队列和后台保存线程
            _saveCts = new CancellationTokenSource();
            _saveQueue = new System.Collections.Concurrent.BlockingCollection<ImageSaveItem>(100); // 最多缓存100帧
            _saveTask = Task.Run(() => SaveWorker(_saveCts.Token));
            
            ModelParam.IsContinuousSave = true;
            ModelParam.AddLog($"开始连续保存图像到：{ModelParam.ImageSavePath}");
        }

        /// <summary>
        /// 停止连续保存
        /// </summary>
        private void StopContinuousSave()
        {
            ModelParam.IsContinuousSave = false;
            
            // 停止保存线程
            if (_saveCts != null)
            {
                _saveCts.Cancel();
                _saveQueue?.CompleteAdding();
                
                // 等待保存线程结束（最多等2秒）
                _saveTask?.Wait(2000);
                
                _saveQueue?.Dispose();
                _saveQueue = null;
                _saveCts.Dispose();
                _saveCts = null;
            }
            
            ModelParam.AddLog($"停止连续保存，共保存 {ModelParam.SavedImageCount} 张图像");
        }

        /// <summary>
        /// 后台保存工作线程
        /// </summary>
        private void SaveWorker(CancellationToken token)
        {
            try
            {
                foreach (var item in _saveQueue.GetConsumingEnumerable(token))
                {
                    if (token.IsCancellationRequested)
                        break;
                        
                    try
                    {
                        // 生成文件名
                        string timestamp = item.Timestamp.ToString("yyyyMMdd_HHmmss_fff");
                        string fileName = $"{item.Prefix}_{timestamp}.{item.Format}";
                        string filePath = Path.Combine(item.SavePath, fileName);

                        // 直接写入原始数据（BMP格式最快）
                        if (item.Format.ToLower() == "bmp")
                        {
                            SaveAsBmp(filePath, item.ImageData, item.Width, item.Height);
                        }
                        else
                        {
                            // 其他格式使用编码器
                            SaveWithEncoder(filePath, item);
                        }

                        int count = Interlocked.Increment(ref _savedImageCount);
                        
                        // 每20张更新一次UI
                        if (count % 20 == 0)
                        {
                            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                ModelParam.SavedImageCount = count;
                                ModelParam.AddLog($"已连续保存 {count} 张图像");
                            }));
                        }
                    }
                    catch
                    {
                        // 单张保存失败不影响后续
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消
            }
            
            // 最终更新计数
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                ModelParam.SavedImageCount = _savedImageCount;
            }));
        }

        /// <summary>
        /// 快速保存为BMP格式（不使用WPF编码器）
        /// </summary>
        private void SaveAsBmp(string filePath, byte[] imageData, int width, int height)
        {
            int stride = width;
            int imageSize = stride * height;
            int fileSize = 54 + 256 * 4 + imageSize; // 文件头 + 调色板 + 图像数据

            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 65536))
            using (var bw = new BinaryWriter(fs))
            {
                // BMP文件头 (14字节)
                bw.Write((byte)'B');
                bw.Write((byte)'M');
                bw.Write(fileSize);
                bw.Write((short)0);
                bw.Write((short)0);
                bw.Write(54 + 256 * 4); // 数据偏移

                // DIB头 (40字节)
                bw.Write(40);
                bw.Write(width);
                bw.Write(-height); // 负值表示从上到下
                bw.Write((short)1);
                bw.Write((short)8); // 8位灰度
                bw.Write(0);
                bw.Write(imageSize);
                bw.Write(2835);
                bw.Write(2835);
                bw.Write(256);
                bw.Write(256);

                // 灰度调色板 (256 * 4字节)
                for (int i = 0; i < 256; i++)
                {
                    bw.Write((byte)i);
                    bw.Write((byte)i);
                    bw.Write((byte)i);
                    bw.Write((byte)0);
                }

                // 图像数据
                bw.Write(imageData);
            }
        }

        /// <summary>
        /// 使用编码器保存（PNG/JPG/TIFF）
        /// </summary>
        private void SaveWithEncoder(string filePath, ImageSaveItem item)
        {
            var bitmap = BitmapSource.Create(
                item.Width, item.Height,
                96, 96,
                PixelFormats.Gray8,
                null,
                item.ImageData,
                item.Width);
            bitmap.Freeze();

            BitmapEncoder encoder = GetBitmapEncoder(item.Format);
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using (var fs = new FileStream(filePath, FileMode.Create))
            {
                encoder.Save(fs);
            }
        }

        /// <summary>
        /// 选择保存路径
        /// </summary>
        private void SelectSavePath()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "选择图像保存路径";
            dialog.SelectedPath = ModelParam.ImageSavePath;
            
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ModelParam.ImageSavePath = dialog.SelectedPath;
                ModelParam.AddLog($"保存路径已设置为：{ModelParam.ImageSavePath}");
            }
        }

        /// <summary>
        /// 应用采集参数到相机
        /// </summary>
        private void ApplyCameraParams()
        {
            try
            {
                if (!ModelParam.CameraConnected)
                {
                    ModelParam.AddLog("相机未连接");
                    return;
                }

                bool wasContinuousGrab = ModelParam.IsContinuousGrab;
                bool wasScanning = ModelParam.IsScanning;

                if (wasScanning)
                    StopLineScan();

                if (wasContinuousGrab)
                    StopContinuousGrab();

                if (s_bStreamCreated)
                {
                    IKapC.ItkStreamStop(s_hStream);
                    ReleaseStream();
                }

                PrepareCameraForManualParamApply();

                bool exposureApplied = TrySetExposureTime(ModelParam.ExposureTime);
                ModelParam.AddLog(exposureApplied
                    ? $"曝光时间设置请求已发送：{ModelParam.ExposureTime} μs"
                    : "设置曝光时间失败：未找到可写曝光参数");

                bool lineRateApplied = TrySetLineRate(ModelParam.LineRate);
                ModelParam.AddLog(lineRateApplied
                    ? $"行频设置请求已发送：{ModelParam.LineRate} Hz"
                    : "设置行频失败：未找到可写行频参数");

                bool gainApplied = TrySetGain(ModelParam.Gain);
                ModelParam.AddLog(gainApplied
                    ? $"增益设置请求已发送：{ModelParam.Gain} dB"
                    : "设置增益失败：未找到可写增益参数");

                bool scanLineApplied = ApplyScanLineCountToCamera();

                ReadCameraParams();

                if (wasContinuousGrab)
                {
                    StartContinuousGrab();
                }
                else if (wasScanning && scanLineApplied)
                {
                    ModelParam.AddLog("扫描参数已更新，重新点击开始扫描即可按新行数采集");
                }
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"应用参数失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 开始连续采集（使用回调方式）
        /// </summary>
        private void StartContinuousGrab()
        {
            try
            {
                if (!ModelParam.CameraConnected)
                {
                    ModelParam.AddLog("相机未连接");
                    return;
                }

                // 先停止之前的采集和释放流（确保干净的状态）
                if (m_bGrabbing || s_bStreamCreated)
                {
                    m_bGrabbing = false;
                    ModelParam.IsContinuousGrab = false;
                    UnregisterFrameCallback();
                    
                    if (s_bStreamCreated)
                    {
                        IKapC.ItkStreamStop(s_hStream);
                        // 释放旧的流，重新创建
                        ReleaseStream();
                    }
                }

                if (!ApplyScanLineCountToCamera())
                    return;

                // 创建新的流
                if (!CreateStream())
                    return;

                // 设置为内部触发/自由运行模式
                SetFreeRunMode();

                // 注册帧完成回调
                RegisterFrameCallback();
                
                ModelParam.AddLog("开始连续采集...");

                // 启动连续采集
                uint res = IKapC.ItkStreamStart(s_hStream, 0xFFFFFFFF); // 连续采集
                if (res != IKapC.ITKSTATUS_OK)
                {
                    ModelParam.AddLog($"启动连续采集失败，错误码：0x{res:X8}");
                    UnregisterFrameCallback();
                    ModelParam.IsContinuousGrab = false;
                    return;
                }

                m_bGrabbing = true;
                ModelParam.IsContinuousGrab = true;
                ModelParam.AddLog("连续采集已启动，等待图像...");
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"连续采集失败：{ex.Message}");
                ModelParam.IsContinuousGrab = false;
            }
        }
        
        /// <summary>
        /// 注册帧完成回调
        /// </summary>
        private void RegisterFrameCallback()
        {
            if (_callbackRegistered)
                return;
                
            try
            {
                // 保持 this 引用，防止被 GC 回收
                _thisGCHandle = GCHandle.Alloc(this);
                IntPtr pContext = GCHandle.ToIntPtr(_thisGCHandle);
                
                // 创建回调委托（必须保持引用，防止被 GC 回收）
                _onFrameReadyDelegate = new IKapCCallBackDelegate(OnFrameReadyCallback);
                
                // 注册帧完成回调
                uint res = IKapC.ItkStreamRegisterCallback(
                    s_hStream, 
                    IKapC.ITKSTREAM_VAL_EVENT_TYPE_END_OF_FRAME,
                    Marshal.GetFunctionPointerForDelegate(_onFrameReadyDelegate),
                    pContext);
                    
                if (res == IKapC.ITKSTATUS_OK)
                {
                    _callbackRegistered = true;
                    ModelParam.AddLog("帧回调注册成功");
                }
                else
                {
                    ModelParam.AddLog($"注册帧回调失败：0x{res:X8}");
                }
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"注册回调异常：{ex.Message}");
            }
        }
        
        /// <summary>
        /// 注销帧完成回调
        /// </summary>
        private void UnregisterFrameCallback()
        {
            if (!_callbackRegistered)
                return;
                
            try
            {
                IKapC.ItkStreamUnregisterCallback(s_hStream, IKapC.ITKSTREAM_VAL_EVENT_TYPE_END_OF_FRAME);
                
                if (_thisGCHandle.IsAllocated)
                {
                    _thisGCHandle.Free();
                }
                
                _callbackRegistered = false;
                ModelParam.AddLog("帧回调已注销");
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"注销回调异常：{ex.Message}");
            }
        }
        
        /// <summary>
        /// 帧完成回调函数（静态方法）
        /// </summary>
        private static int _frameCount = 0;
        private static void OnFrameReadyCallback(uint eventType, IntPtr pContext)
        {
            try
            {
                _frameCount++;
                
                if (pContext == IntPtr.Zero)
                    return;
                    
                GCHandle handle = GCHandle.FromIntPtr(pContext);
                LineScanTestPlatformViewModel vm = handle.Target as LineScanTestPlatformViewModel;
                
                if (vm == null || !vm.m_bGrabbing)
                    return;
                
                // 每10帧输出一次日志
                if (_frameCount % 10 == 1)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        vm.ModelParam.AddLog($"收到帧回调 #{_frameCount}");
                    }));
                }
                    
                // 在回调中获取图像
                vm.ProcessFrameInCallback();
            }
            catch
            {
                // 回调中不要抛出异常
            }
        }
        
        /// <summary>
        /// 在回调中处理帧数据
        /// </summary>
        private void ProcessFrameInCallback()
        {
            try
            {
                // 获取当前缓冲区
                ITKBUFFER hBuffer = new ITKBUFFER();
                uint res = IKapC.ItkStreamGetCurrentBuffer(s_hStream, ref hBuffer);
                if (res != IKapC.ITKSTATUS_OK)
                    return;

                // 获取缓冲区信息
                ITK_BUFFER_INFO bufferInfo = new ITK_BUFFER_INFO();
                res = IKapC.ItkBufferGetInfo(hBuffer, bufferInfo);
                if (res != IKapC.ITKSTATUS_OK)
                    return;

                uint bufferStatus = bufferInfo.State;
                if (bufferStatus == IKapC.ITKBUFFER_VAL_STATE_FULL || bufferStatus == IKapC.ITKBUFFER_VAL_STATE_UNCOMPLETED)
                {
                    ulong nImageSize = bufferInfo.ValidImageSize;
                    if (s_pUserBuffer != IntPtr.Zero && nImageSize > 0)
                    {
                        res = IKapC.ItkBufferRead(hBuffer, 0, s_pUserBuffer, (uint)nImageSize);
                        if (res == IKapC.ITKSTATUS_OK)
                        {
                            // 在 UI 线程显示图像
                            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                ConvertAndDisplayImage((int)nImageSize);
                            }));
                        }
                    }
                }
            }
            catch
            {
                // 回调中不要抛出异常
            }
        }

        /// <summary>
        /// 设置自由运行模式（内部触发）
        /// </summary>
        private void SetFreeRunMode()
        {
            try
            {
                uint res;
                
                // 线扫相机设置：关闭行触发，使用内部行频率
                // 1. 设置行触发源为内部（关闭外部触发）
                res = IKapC.ItkDevFromString(s_hDev, "TriggerSelector", "LineStart");
                if (res == IKapC.ITKSTATUS_OK)
                {
                    res = IKapC.ItkDevFromString(s_hDev, "TriggerMode", "Off");
                    ModelParam.AddLog($"LineStart触发模式: {(res == IKapC.ITKSTATUS_OK ? "已关闭" : $"设置失败0x{res:X8}")}");
                }
                
                // 2. 设置帧触发
                res = IKapC.ItkDevFromString(s_hDev, "TriggerSelector", "FrameStart");
                if (res == IKapC.ITKSTATUS_OK)
                {
                    res = IKapC.ItkDevFromString(s_hDev, "TriggerMode", "Off");
                    ModelParam.AddLog($"FrameStart触发模式: {(res == IKapC.ITKSTATUS_OK ? "已关闭" : $"设置失败0x{res:X8}")}");
                }
                
                // 3. 尝试设置内部行频率（让相机自动采集）
                // 先读取当前行频率
                double lineRate = 0;
                res = IKapC.ItkDevGetDouble(s_hDev, "AcquisitionLineRate", ref lineRate);
                if (res == IKapC.ITKSTATUS_OK)
                {
                    ModelParam.AddLog($"当前行频率: {lineRate} Hz");
                    
                    // 如果行频率为0，设置一个默认值
                    if (lineRate < 100)
                    {
                        res = IKapC.ItkDevSetDouble(s_hDev, "AcquisitionLineRate", 10000); // 10kHz
                        ModelParam.AddLog($"设置行频率10kHz: {(res == IKapC.ITKSTATUS_OK ? "成功" : $"失败0x{res:X8}")}");
                    }
                }
                else
                {
                    // 尝试其他参数名
                    res = IKapC.ItkDevGetDouble(s_hDev, "LineRate", ref lineRate);
                    if (res == IKapC.ITKSTATUS_OK)
                    {
                        ModelParam.AddLog($"当前LineRate: {lineRate} Hz");
                    }
                }
                ModelParam.AddLog("设置自由运行模式");
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"设置运行模式异常：{ex.Message}");
            }
        }

        private int _grabLoopCount = 0;
        private void ContinuousGrabLoop(CancellationToken token)
        {
            _grabLoopCount = 0;
            while (!token.IsCancellationRequested && m_bGrabbing)
            {
                try
                {
                    _grabLoopCount++;
                    
                    // 每100次循环输出一次状态（避免日志刷屏）
                    bool shouldLog = (_grabLoopCount % 100 == 1);
                    
                    // 获取当前缓冲区
                    ITKBUFFER hCurrBuffer = new ITKBUFFER();
                    uint res = IKapC.ItkStreamGetCurrentBuffer(s_hStream, ref hCurrBuffer);
                    
                    if (res != IKapC.ITKSTATUS_OK)
                    {
                        if (shouldLog)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                                ModelParam.AddLog($"[{_grabLoopCount}] GetCurrentBuffer失败: 0x{res:X8}"));
                        }
                        Thread.Sleep(30);
                        continue;
                    }
                    
                    ITK_BUFFER_INFO bufferInfo = new ITK_BUFFER_INFO();
                    res = IKapC.ItkBufferGetInfo(hCurrBuffer, bufferInfo);
                    
                    if (res != IKapC.ITKSTATUS_OK)
                    {
                        if (shouldLog)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                                ModelParam.AddLog($"[{_grabLoopCount}] GetBufferInfo失败: 0x{res:X8}"));
                        }
                        Thread.Sleep(30);
                        continue;
                    }
                    
                    uint bufferStatus = bufferInfo.State;
                    
                    if (shouldLog)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                            ModelParam.AddLog($"[{_grabLoopCount}] 缓冲区状态: {bufferStatus}, 大小: {bufferInfo.ValidImageSize}"));
                    }
                    
                    // 检查缓冲区状态
                    if (bufferStatus == IKapC.ITKBUFFER_VAL_STATE_FULL || bufferStatus == IKapC.ITKBUFFER_VAL_STATE_UNCOMPLETED)
                    {
                        ulong nImageSize = bufferInfo.ValidImageSize;
                        if (s_pUserBuffer != IntPtr.Zero && nImageSize > 0)
                        {
                            res = IKapC.ItkBufferRead(hCurrBuffer, 0, s_pUserBuffer, (uint)nImageSize);
                            if (res == IKapC.ITKSTATUS_OK)
                            {
                                ConvertAndDisplayImage((int)nImageSize);
                            }
                            else if (shouldLog)
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                    ModelParam.AddLog($"[{_grabLoopCount}] BufferRead失败: 0x{res:X8}"));
                            }
                        }
                    }
                    
                    Thread.Sleep(30);
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                        ModelParam.AddLog($"采集循环异常: {ex.Message}"));
                    break;
                }
            }
            
            Application.Current.Dispatcher.Invoke(() =>
                ModelParam.AddLog($"采集循环结束，共执行 {_grabLoopCount} 次"));
        }

        /// <summary>
        /// 停止连续采集
        /// </summary>
        private void StopContinuousGrab()
        {
            try
            {
                m_bGrabbing = false;
                _grabCts?.Cancel();
                
                if (s_bStreamCreated)
                {
                    IKapC.ItkStreamStop(s_hStream);
                }
                
                // 注销回调
                UnregisterFrameCallback();
                
                ModelParam.IsContinuousGrab = false;
                ModelParam.AddLog("停止连续采集");
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"停止采集失败：{ex.Message}");
            }
        }

        [DllImport("kernel32.dll")]
        private static extern void CopyMemory(IntPtr Destination, IntPtr Source, int Length);

        #endregion

        #region Commands
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "连接控制卡":
                    ConnectZMotion();
                    break;
                case "断开控制卡":
                    DisconnectZMotion();
                    break;
                case "控制卡设置":
                    OpenMotionCardSettings();
                    break;
                case "连接相机":
                    ConnectIKapCamera();
                    break;
                case "枚举相机":
                    EnumerateIKapDevices();
                    break;
                case "断开相机":
                    DisconnectIKapCamera();
                    break;
                case "运动到位":
                    MoveToPosition();
                    break;
                case "开始运动":
                    StartMotion();
                    break;
                case "开始往复运动":
                    StartReciprocatingMotion();
                    break;
                case "停止运动":
                    StopMotion();
                    break;
                case "停止往复运动":
                    StopReciprocatingMotion();
                    break;
                case "回零":
                    GoHome();
                    break;
                case "读取参数":
                    ReadMotionParams();
                    break;
                case "读取采集参数":
                    ReadCameraParams();
                    break;
                case "保存参数":
                    SaveMotionParams();
                    break;
                case "读取回零速度":
                    ReadHomeSpeed();
                    break;
                case "设置回零速度":
                    SetHomeSpeed();
                    break;
                case "开始扫描":
                    StartLineScan();
                    break;
                case "停止扫描":
                    StopLineScan();
                    break;
                case "单次采集":
                    GrabSingleImage();
                    break;
                case "连续采集":
                    StartContinuousGrab();
                    break;
                case "停止采集":
                    StopContinuousGrab();
                    break;
                case "保存图像":
                    SaveCurrentImage();
                    break;
                case "开始连续保存":
                    StartContinuousSave();
                    break;
                case "停止连续保存":
                    StopContinuousSave();
                    break;
                case "选择保存路径":
                    SelectSavePath();
                    break;
                case "应用采集参数":
                    ApplyCameraParams();
                    break;
                case "清空日志":
                    ModelParam.LogMessages.Clear();
                    break;
                case "执行测试":
                    ModelParam.ExecuteModule();
                    break;
                // 光源控制命令
                case "连接光源":
                    ConnectLightController();
                    break;
                case "断开光源":
                    DisconnectLightController();
                    break;
                case "光源设置":
                    OpenLightControllerSettings();
                    break;
                case "应用光源脉宽":
                    ApplyLightPulseWidth();
                    break;
                case "光源全开":
                    SetAllLightChannels(true);
                    break;
                case "光源全关":
                    SetAllLightChannels(false);
                    break;
                case "取消":
                    CloseDialog(ButtonResult.No);
                    break;
                case "确认":
                    {
                        ModelParam.moduleOutputParam.TransmitParams = ModelParam.OutputParams.ToDictionary(
                            item => item.Guid.ToString(),
                            item => (object)item);

                        CloseDialog(ButtonResult.OK, new DialogParameters()
                        {
                            { "Param", ModelParam },
                        });
                    }
                    break;
                default:
                    break;
            }
        });

        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            if (ModelParam.Serial == -999)
                ModelParam.Serial = Serial;

            // 刷新设备连接状态
            RefreshDeviceStatus();

            if (Visibility == Visibility.Hidden)
            {
                CloseDialog(ButtonResult.OK, new DialogParameters()
                {
                    { "Param", ModelParam },
                });
            }
        });

        /// <summary>
        /// 刷新所有设备的连接状态
        /// </summary>
        private void RefreshDeviceStatus()
        {
            // 刷新运动控制卡状态
            if (_platformService.IsConnected)
            {
                // 句柄有效，尝试读取状态验证连接
                try
                {
                    int ret = _platformService.GetAxisPosition(ModelParam.AxisNumber, out var curpos);
                    ModelParam.ZMotionConnected = (ret == 0);
                    if (ModelParam.ZMotionConnected && !_timer.IsEnabled)
                    {
                        StartMotionPolling(); // 启动后台轮询
                    }
                }
                catch
                {
                    ModelParam.ZMotionConnected = false;
                }
            }
            else
            {
                ModelParam.ZMotionConnected = false;
            }

            // 刷新相机状态 - CameraConnected 已在连接/断开时设置，这里不需要重复检查

            // 刷新光源控制器状态
            if (s_lightController != null)
            {
                ModelParam.LightControllerConnected = s_lightController.IsConnected;
            }
            else
            {
                ModelParam.LightControllerConnected = false;
            }
        }

        public DelegateCommand<object> DataOperateCommand => new DelegateCommand<object>((obj) =>
        {
            switch (obj?.ToString())
            {
                case "Add":
                    var curSltParam = ModelParam.OutputParamResource[SltOutputParamName] as TransmitParam;

                    if (ModelParam.OutputParams.Where(item => item.Name == SltOutputParamName).ToList().Count >= 1)
                    {
                        System.Windows.MessageBox.Show("已包含重名参数，请重新输入！");
                    }
                    else
                    {
                        ModelParam.OutputParams.Add(new TransmitParam
                        {
                            ParamName = curSltParam.Name,
                            Serial = ModelParam.Serial,
                            Name = Serial + "_" + Name + "_" + SltOutputParamName,
                            Type = DataType._object,
                            Value = OutputParamCollector.GetDataPointValues(ModelParam)[curSltParam.Name].DeepCopy(),
                            ResourcePath = curSltParam.ResourcePath,
                        });
                    }
                    break;
                case "Delete":
                    if (CurrentOutputParam != null)
                    {
                        ModelParam.OutputParams.Remove(CurrentOutputParam);
                        PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Remove(CurrentOutputParam);
                        CurrentOutputParam = null;
                    }
                    break;
                default:
                    break;
            }
        });
        #endregion

        #region 光源控制器方法
        /// <summary>
        /// 连接光源控制器
        /// </summary>
        private async void ConnectLightController()
        {
            try
            {
                ModelParam.AddLog("正在连接光源控制器...");
                
                bool success = await Task.Run(() =>
                {
                    if (s_lightController == null)
                    {
                        s_lightController = CreateLightController();
                    }

                    s_lightController.IP = ModelParam.LightControllerIp;
                    s_lightController.Port = 8234;
                    s_lightController.ConnectionType = 0; // 网口连接

                    return s_lightController.Init();
                });

                if (success)
                {
                    ModelParam.LightControllerConnected = true;
                    ModelParam.LightChannelCount = s_lightController.ChannelCount;
                    ModelParam.AddLog("光源控制器连接成功！");
                }
                else
                {
                    ModelParam.AddLog("光源控制器连接失败，请检查IP地址和网络连接");
                }
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"连接光源控制器异常：{ex.Message}");
            }
        }

        /// <summary>
        /// 断开光源控制器
        /// </summary>
        private async void DisconnectLightController()
        {
            try
            {
                if (s_lightController != null)
                {
                    await Task.Run(() => s_lightController.Close());
                    ModelParam.LightControllerConnected = false;
                    ModelParam.AddLog("光源控制器已断开连接");
                }
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"断开光源控制器失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 打开光源控制器设置界面
        /// </summary>
        private void OpenLightControllerSettings()
        {
            try
            {
                if (s_lightController == null || !ModelParam.LightControllerConnected)
                {
                    ModelParam.AddLog("请先连接光源控制器");
                    return;
                }

                if (s_lightController is CSTLightController)
                {
                    var dialogService = ContainerLocator.Container.Resolve<IDialogService>();
                    dialogService.ShowDialog(nameof(CSTLightControllerView), new DialogParameters
                    {
                        { "Param", s_lightController }
                    }, result =>
                    {
                        if (result.Result == ButtonResult.OK)
                        {
                            // 刷新状态
                            ModelParam.LightControllerConnected = s_lightController.IsConnected;
                            ModelParam.AddLog("光源控制器设置已更新");
                        }
                    });
                    return;
                }

                if (s_lightController is RseeLightController)
                {
                    var dialogService = ContainerLocator.Container.Resolve<IDialogService>();
                    dialogService.ShowDialog(nameof(RseeLightControllerView), new DialogParameters
                    {
                        { "Param", s_lightController }
                    }, result =>
                    {
                        if (result.Result == ButtonResult.OK)
                        {
                            ModelParam.LightControllerConnected = s_lightController.IsConnected;
                            ModelParam.AddLog("锐视光源控制器设置已更新");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"打开光源设置界面失败：{ex.Message}");
            }
        }

        private static LightControllerBase CreateLightController()
        {
            return new RseeLightController();
        }

        /// <summary>
        /// 应用光源脉宽
        /// </summary>
        private void ApplyLightPulseWidth()
        {
            try
            {
                if (s_lightController == null || !ModelParam.LightControllerConnected)
                {
                    ModelParam.AddLog("光源控制器未连接");
                    return;
                }

                var channelValues = new Dictionary<int, int>();
                for (int i = 1; i <= ModelParam.LightChannelCount; i++)
                {
                    channelValues[i] = ModelParam.LightBrightness;
                }

                if (s_lightController.SetMultiBrightness(channelValues))
                {
                    ModelParam.AddLog($"光源脉宽已设置为约：{ModelParam.LightPulseWidthUs}us");
                }
                else
                {
                    ModelParam.AddLog("设置光源脉宽失败");
                }
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"设置光源脉宽异常：{ex.Message}");
            }
        }

        /// <summary>
        /// 设置所有光源通道开关
        /// </summary>
        private void SetAllLightChannels(bool isOn)
        {
            try
            {
                if (s_lightController == null || !ModelParam.LightControllerConnected)
                {
                    ModelParam.AddLog("光源控制器未连接");
                    return;
                }

                for (int i = 1; i <= ModelParam.LightChannelCount; i++)
                {
                    s_lightController.SetChannelOnOff(i, isOn);
                }

                ModelParam.AddLog(isOn ? "光源已全部打开" : "光源已全部关闭");
            }
            catch (Exception ex)
            {
                ModelParam.AddLog($"设置光源开关异常：{ex.Message}");
            }
        }
        #endregion
    }

    /// <summary>
    /// 图像保存项
    /// </summary>
    internal class ImageSaveItem
    {
        public byte[] ImageData { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string SavePath { get; set; }
        public string Prefix { get; set; }
        public string Format { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
