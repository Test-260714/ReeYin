using HalconDotNet;
using Newtonsoft.Json;
using Prism.Mvvm;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.Camera.Models;
using ReeYin_V.Share;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Custom.LineScan.Models
{
    /// <summary>
    /// 线扫采集节点模型
    /// </summary>
    [Serializable]
    public class LineScanModel : BindableBase, IModuleParam
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

        #region Properties
        private CameraBase _selectedCamera;
        /// <summary>
        /// 选中的相机
        /// </summary>
        public CameraBase SelectedCamera
        {
            get { return _selectedCamera; }
            set { _selectedCamera = value; RaisePropertyChanged(); }
        }

        private string _selectedCameraName = "";
        /// <summary>
        /// 选中的相机名称
        /// </summary>
        public string SelectedCameraName
        {
            get { return _selectedCameraName; }
            set { _selectedCameraName = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<CameraBase> _cameras = new ObservableCollection<CameraBase>();
        /// <summary>
        /// 相机列表
        /// </summary>
        public ObservableCollection<CameraBase> Cameras
        {
            get { return _cameras; }
            set { _cameras = value; RaisePropertyChanged(); }
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

        private bool _triggerByMotion = true;
        /// <summary>
        /// 是否由运动触发
        /// </summary>
        public bool TriggerByMotion
        {
            get { return _triggerByMotion; }
            set { _triggerByMotion = value; RaisePropertyChanged(); }
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

        private bool _scanCompleted = false;
        /// <summary>
        /// 扫描完成标志（输出参数）
        /// </summary>
        [OutputParam(description: "扫描完成")]
        public bool ScanCompleted
        {
            get { return _scanCompleted; }
            set { _scanCompleted = value; RaisePropertyChanged(); }
        }

        private int _currentLineCount = 0;
        /// <summary>
        /// 当前扫描行数（输出参数）
        /// </summary>
        [OutputParam(description: "当前行数")]
        public int CurrentLineCount
        {
            get { return _currentLineCount; }
            set { _currentLineCount = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<TransmitParam> _inputParams = new ObservableCollection<TransmitParam>();
        /// <summary>
        /// 输入参数
        /// </summary>
        public ObservableCollection<TransmitParam> InputParams
        {
            get { return _inputParams; }
            set { _inputParams = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<TransmitParam> _outputParams = new ObservableCollection<TransmitParam>();
        /// <summary>
        /// 输出参数
        /// </summary>
        public ObservableCollection<TransmitParam> OutputParams
        {
            get { return _outputParams; }
            set { _outputParams = value; RaisePropertyChanged(); }
        }

        private Dictionary<string, TransmitParam> _outputParamResource = new Dictionary<string, TransmitParam>();
        /// <summary>
        /// 输出参数资源
        /// </summary>
        public Dictionary<string, TransmitParam> OutputParamResource
        {
            get { return _outputParamResource; }
            set { _outputParamResource = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Methods
        /// <summary>
        /// 执行线扫采集
        /// </summary>
        public Task<ExecuteModuleOutput> ExecuteModule()
        {
            return Task.Run(() =>
            {
                try
                {
                    if (SelectedCamera == null || !SelectedCamera.Connected)
                    {
                        Console.WriteLine("相机未连接");
                        return new ExecuteModuleOutput
                        {
                            RunStatus = NodeStatus.Error
                        };
                    }

                    Console.WriteLine($"开始线扫采集 - 相机:{SelectedCameraName}, 行数:{ScanLineCount}");
                    
                    IsScanning = true;
                    ScanCompleted = false;
                    CurrentLineCount = 0;

                    // 设置相机参数
                    SelectedCamera.SetSpecifiedParam("Double", "ExposureTime", ExposureTime);
                    SelectedCamera.SetSpecifiedParam("Double", "Gain", Gain);
                    SelectedCamera.CaptureImage(false);

                    return new ExecuteModuleOutput
                    {
                        RunStatus = NodeStatus.Success
                    };
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"线扫采集执行失败：{ex.Message}");
                    IsScanning = false;
                    return new ExecuteModuleOutput
                    {
                        RunStatus = NodeStatus.Error
                    };
                }
            });
        }

        public void TransferParam()
        {
  
        }

        public void Dispose()
        {
            _capturedImage?.Dispose();
            _capturedImage = null;
        }
        #endregion
    }
}
