using HalconDotNet;
using Newtonsoft.Json;
using Prism.Mvvm;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Custom.LineScan.Models
{
    /// <summary>
    /// 线扫测试平台模型 - 集成正运动控制卡和埃科相机
    /// </summary>
    [Serializable]
    public class LineScanTestPlatformModel : BindableBase, IModuleParam
    {
        #region IModuleParam
        public Guid Guid { get; set; } = Guid.NewGuid();
        
        private int _serial = -999;
        public int Serial
        {
            get { return _serial; }
            set { _serial = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        public ModuleParam moduleInputParam { get; set; } = new ModuleParam();

        public ModuleParam moduleOutputParam { get; set; } = new ModuleParam();

        [JsonIgnore]
        public Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }

        [JsonIgnore]
        public List<(int, NodeStatus)> InputNodeStatus { get; set; } = new List<(int, NodeStatus)>();

        [JsonIgnore]
        private ExecuteModuleOutput _output;
        [JsonIgnore]
        public ExecuteModuleOutput Output
        {
            get { return _output; }
            set { _output = value; RaisePropertyChanged(); }
        }
        #endregion

        #region 正运动控制卡参数
        private string _zmotionIpAddress = "192.168.0.11";
        /// <summary>
        /// 正运动控制卡IP地址
        /// </summary>
        public string ZMotionIpAddress
        {
            get { return _zmotionIpAddress; }
            set { _zmotionIpAddress = value; RaisePropertyChanged(); }
        }

        private bool _zmotionConnected = false;
        /// <summary>
        /// 正运动控制卡连接状态
        /// </summary>
        public bool ZMotionConnected
        {
            get { return _zmotionConnected; }
            set { _zmotionConnected = value; RaisePropertyChanged(); }
        }

        private int _axisNumber = 0;
        /// <summary>
        /// 运动轴号
        /// </summary>
        public int AxisNumber
        {
            get { return _axisNumber; }
            set { _axisNumber = value; RaisePropertyChanged(); }
        }

        private float _targetPosition = 0f;
        /// <summary>
        /// 目标位置
        /// </summary>
        public float TargetPosition
        {
            get { return _targetPosition; }
            set { _targetPosition = value; RaisePropertyChanged(); }
        }

        private float _moveSpeed = 100f;
        /// <summary>
        /// 运动速度（定位速度）
        /// </summary>
        public float MoveSpeed
        {
            get { return _moveSpeed; }
            set { _moveSpeed = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private float _forwardSpeed = 200f;
        /// <summary>
        /// 往复速度（不序列化，每次使用默认值200）
        /// </summary>
        [JsonIgnore]
        public float ForwardSpeed
        {
            get { return _forwardSpeed; }
            set { _forwardSpeed = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private float _workDistance = 550f;
        /// <summary>
        /// 工作距离（不序列化，每次使用默认值550）
        /// </summary>
        [JsonIgnore]
        public float WorkDistance
        {
            get { return _workDistance; }
            set { _workDistance = value; RaisePropertyChanged(); }
        }

        private float _homeSpeed = 50f;
        /// <summary>
        /// 回原速度
        /// </summary>
        public float HomeSpeed
        {
            get { return _homeSpeed; }
            set { _homeSpeed = value; RaisePropertyChanged(); }
        }

        private float _currentPosition = 0f;
        /// <summary>
        /// 当前位置
        /// </summary>
        public float CurrentPosition
        {
            get { return _currentPosition; }
            set { _currentPosition = value; RaisePropertyChanged(); }
        }

        private bool _isMoving = false;
        /// <summary>
        /// 是否正在运动
        /// </summary>
        public bool IsMoving
        {
            get { return _isMoving; }
            set { _isMoving = value; RaisePropertyChanged(); }
        }
        #endregion

        #region 埃科相机参数
        private int _cameraDeviceIndex = 0;
        /// <summary>
        /// 相机设备索引
        /// </summary>
        public int CameraDeviceIndex
        {
            get { return _cameraDeviceIndex; }
            set { _cameraDeviceIndex = value; RaisePropertyChanged(); }
        }

        private int _cameraBoardIndex = -1;
        /// <summary>
        /// 采集卡索引（-1表示不使用采集卡）
        /// </summary>
        public int CameraBoardIndex
        {
            get { return _cameraBoardIndex; }
            set { _cameraBoardIndex = value; RaisePropertyChanged(); }
        }

        private bool _cameraConnected = false;
        /// <summary>
        /// 相机连接状态
        /// </summary>
        public bool CameraConnected
        {
            get { return _cameraConnected; }
            set { _cameraConnected = value; RaisePropertyChanged(); }
        }

        private string _cameraSerialNumber = "";
        /// <summary>
        /// 相机序列号
        /// </summary>
        public string CameraSerialNumber
        {
            get { return _cameraSerialNumber; }
            set { _cameraSerialNumber = value; RaisePropertyChanged(); }
        }

        private string _cameraIpAddress = "";
        /// <summary>
        /// 相机IP地址
        /// </summary>
        public string CameraIpAddress
        {
            get { return _cameraIpAddress; }
            set { _cameraIpAddress = value; RaisePropertyChanged(); }
        }

        private float _exposureTime = 10000f;
        /// <summary>
        /// 曝光时间
        /// </summary>
        public float ExposureTime
        {
            get { return _exposureTime; }
            set { _exposureTime = value; RaisePropertyChanged(); }
        }

        private float _gain = 0f;
        /// <summary>
        /// 增益
        /// </summary>
        public float Gain
        {
            get { return _gain; }
            set { _gain = value; RaisePropertyChanged(); }
        }

        private double _lineRate = 10000;
        /// <summary>
        /// 行频 (Hz)
        /// </summary>
        public double LineRate
        {
            get { return _lineRate; }
            set { _lineRate = value; RaisePropertyChanged(); }
        }

        private int _scanLineCount = 1000;
        /// <summary>
        /// 扫描行数
        /// </summary>
        public int ScanLineCount
        {
            get { return _scanLineCount; }
            set { _scanLineCount = value; RaisePropertyChanged(); }
        }

        private bool _isScanning = false;
        /// <summary>
        /// 是否正在扫描
        /// </summary>
        public bool IsScanning
        {
            get { return _isScanning; }
            set { _isScanning = value; RaisePropertyChanged(); }
        }

        private int _currentLineCount = 0;
        /// <summary>
        /// 当前扫描行数
        /// </summary>
        public int CurrentLineCount
        {
            get { return _currentLineCount; }
            set { _currentLineCount = value; RaisePropertyChanged(); }
        }

        private int _imageWidth = 0;
        /// <summary>
        /// 图像宽度
        /// </summary>
        public int ImageWidth
        {
            get { return _imageWidth; }
            set { _imageWidth = value; RaisePropertyChanged(); }
        }

        private int _imageHeight = 0;
        /// <summary>
        /// 图像高度
        /// </summary>
        public int ImageHeight
        {
            get { return _imageHeight; }
            set { _imageHeight = value; RaisePropertyChanged(); }
        }

        private bool _isContinuousGrab = false;
        /// <summary>
        /// 是否连续采集
        /// </summary>
        public bool IsContinuousGrab
        {
            get { return _isContinuousGrab; }
            set { _isContinuousGrab = value; RaisePropertyChanged(); }
        }
        #endregion

        #region 光源控制器参数
        private string _lightControllerIp = "192.168.1.252";
        /// <summary>
        /// 光源控制器IP地址
        /// </summary>
        public string LightControllerIp
        {
            get { return _lightControllerIp; }
            set { _lightControllerIp = value; RaisePropertyChanged(); }
        }

        private bool _lightControllerConnected = false;
        /// <summary>
        /// 光源控制器连接状态
        /// </summary>
        public bool LightControllerConnected
        {
            get { return _lightControllerConnected; }
            set { _lightControllerConnected = value; RaisePropertyChanged(); }
        }

        private int _lightBrightness = 128;
        /// <summary>
        /// 光源亮度 (0-255)
        /// </summary>
        public int LightBrightness
        {
            get { return _lightBrightness; }
            set
            {
                _lightBrightness = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(LightPulseWidthUs));
            }
        }

        public int LightPulseWidthUs => (int)Math.Round(Math.Clamp(_lightBrightness, 0, 255) * 999d / 255d, MidpointRounding.AwayFromZero);

        private int _lightChannelCount = 4;
        /// <summary>
        /// 光源通道数量
        /// </summary>
        public int LightChannelCount
        {
            get { return _lightChannelCount; }
            set { _lightChannelCount = value; RaisePropertyChanged(); }
        }
        #endregion

        #region 图像保存参数
        private string _imageSavePath = @"D:\Images";
        /// <summary>
        /// 图像保存路径
        /// </summary>
        public string ImageSavePath
        {
            get { return _imageSavePath; }
            set { _imageSavePath = value; RaisePropertyChanged(); }
        }

        private string _imagePrefix = "LineScan";
        /// <summary>
        /// 图像文件名前缀
        /// </summary>
        public string ImagePrefix
        {
            get { return _imagePrefix; }
            set { _imagePrefix = value; RaisePropertyChanged(); }
        }

        private string _imageFormat = "bmp";
        /// <summary>
        /// 图像保存格式 (bmp, png, jpg, tiff)
        /// </summary>
        public string ImageFormat
        {
            get { return _imageFormat; }
            set { _imageFormat = value; RaisePropertyChanged(); }
        }

        private bool _isContinuousSave = false;
        /// <summary>
        /// 是否连续保存图像
        /// </summary>
        public bool IsContinuousSave
        {
            get { return _isContinuousSave; }
            set { _isContinuousSave = value; RaisePropertyChanged(); }
        }

        private int _savedImageCount = 0;
        /// <summary>
        /// 已保存图像数量
        /// </summary>
        public int SavedImageCount
        {
            get { return _savedImageCount; }
            set { _savedImageCount = value; RaisePropertyChanged(); }
        }
        #endregion

        #region 输出参数
        private HImage _capturedImage;
        /// <summary>
        /// 采集的图像（输出参数）
        /// </summary>
        [OutputParam(description: "采集图像")]
        public HImage CapturedImage
        {
            get { return _capturedImage; }
            set { _capturedImage = value; RaisePropertyChanged(); }
        }

        private bool _testCompleted = false;
        /// <summary>
        /// 测试完成标志（输出参数）
        /// </summary>
        [OutputParam(description: "测试完成")]
        public bool TestCompleted
        {
            get { return _testCompleted; }
            set { _testCompleted = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<TransmitParam> _inputParams = new ObservableCollection<TransmitParam>();
        public ObservableCollection<TransmitParam> InputParams
        {
            get { return _inputParams; }
            set { _inputParams = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<TransmitParam> _outputParams = new ObservableCollection<TransmitParam>();
        public ObservableCollection<TransmitParam> OutputParams
        {
            get { return _outputParams; }
            set { _outputParams = value; RaisePropertyChanged(); }
        }

        private Dictionary<string, TransmitParam> _outputParamResource = new Dictionary<string, TransmitParam>();
        public Dictionary<string, TransmitParam> OutputParamResource
        {
            get { return _outputParamResource; }
            set { _outputParamResource = value; RaisePropertyChanged(); }
        }
        #endregion

        #region 日志
        private ObservableCollection<string> _logMessages = new ObservableCollection<string>();
        /// <summary>
        /// 日志消息
        /// </summary>
        public ObservableCollection<string> LogMessages
        {
            get { return _logMessages; }
            set { _logMessages = value; RaisePropertyChanged(); }
        }

        public void AddLog(string message)
        {
            LogMessages.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");
            if (LogMessages.Count > 100)
            {
                LogMessages.RemoveAt(LogMessages.Count - 1);
            }
        }
        #endregion

        #region Methods
        public Task<ExecuteModuleOutput> ExecuteModule()
        {
            return Task.Run(() =>
            {
                try
                {
                    AddLog("开始执行线扫测试...");
                    TestCompleted = false;

                    return new ExecuteModuleOutput
                    {
                        RunStatus = NodeStatus.Success
                    };
                }
                catch (Exception ex)
                {
                    AddLog($"执行失败：{ex.Message}");
                    return new ExecuteModuleOutput
                    {
                        RunStatus = NodeStatus.Error
                    };
                }
            });
        }

        public void TransferParam()
        {
            // 参数传递逻辑
        }

        public void Dispose()
        {
            _capturedImage?.Dispose();
            _capturedImage = null;
        }
        #endregion
    }
}
