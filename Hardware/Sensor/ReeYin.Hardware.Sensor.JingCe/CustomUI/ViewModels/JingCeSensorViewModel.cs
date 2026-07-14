using HalconDotNet;
using OpenCvSharp;
using ReeYin.Hardware.Sensor.JingCe.CustomUI.Models;
using ReeYin_V.Core.Helper.ImageOP;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.ResultsDisplay;
using ReeYin_V.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ReeYin.Hardware.Sensor.JingCe.CustomUI.ViewModels
{
    [Serializable]
    public class JingCeSensorViewModel : DialogViewModelBase
    {
        #region Properties
        private JingCeSensorModel _modelParam = new JingCeSensorModel();

        public JingCeSensorModel ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        private readonly List<List<float[]>> _heightLayers = new List<List<float[]>>();
        private readonly List<List<float[]>> _grayLayers = new List<List<float[]>>();
        private HObject? _grayImage;
        private ImageResultsDisplay? _heightDisplayResult;
        private int _selectedLayerIndex;

        public HObject? GrayImage
        {
            get { return _grayImage; }
            set
            {
                if (ReferenceEquals(_grayImage, value))
                {
                    return;
                }

                _grayImage?.Dispose();
                _grayImage = value;
                RaisePropertyChanged();
            }
        }

        public ImageResultsDisplay? HeightDisplayResult
        {
            get { return _heightDisplayResult; }
            set
            {
                if (ReferenceEquals(_heightDisplayResult, value))
                {
                    return;
                }

                _heightDisplayResult?.HeightImage?.Dispose();
                _heightDisplayResult = value;
                RaisePropertyChanged();
            }
        }

        public int SelectedLayerIndex
        {
            get { return _selectedLayerIndex; }
            set
            {
                if (_selectedLayerIndex == value)
                {
                    return;
                }

                _selectedLayerIndex = Math.Max(0, value);
                RaisePropertyChanged();
                RenderSelectedLayer();
            }
        }

        public string LayerCountText => $"层数：{Math.Min(_heightLayers.Count, _grayLayers.Count)}";

        private Common_Algorithm CommonAlgorithm { get; set; }
        #endregion


        #region  Constructor
        /// <summary>
        /// 初始化参数
        /// </summary>
        public override void InitParam()
        {
            if (Param != null && (Param is JingCeSensor))
                ModelParam.JingCeSensor = Param as JingCeSensor ?? new JingCeSensor();
            else
                ModelParam.JingCeSensor = new JingCeSensor();

            Init();
        }

        public JingCeSensorViewModel()
        {
            CommonAlgorithm = PrismProvider.Container.Resolve(typeof(Common_Algorithm)) as Common_Algorithm ?? new Common_Algorithm();
        }
        #endregion

        #region Methods
        private void Init()
        {
            ModelParam.JingCeSensor.GetSensorParameters();
        }

        public override void OnDialogClosed()
        {
            GrayImage = null;
            HeightDisplayResult = null;
            base.OnDialogClosed();
        }

        private void RenderCollectedData()
        {
            _heightLayers.Clear();
            _grayLayers.Clear();

            var dataCollect = ModelParam.JingCeSensor.ReceiveSensorData();
            foreach (var item in dataCollect)
            {
                if (item?.AreaData == null || item.AreaData.Count < 2)
                {
                    continue;
                }

                int layerCount = item.AreaData.Count / 2;
                for (int layerIndex = 0; layerIndex < layerCount; layerIndex++)
                {
                    while (_heightLayers.Count <= layerIndex)
                    {
                        _heightLayers.Add(new List<float[]>());
                        _grayLayers.Add(new List<float[]>());
                    }

                    float[] heightRow = item.AreaData[layerIndex];
                    float[] grayRow = item.AreaData[layerCount + layerIndex];
                    if (heightRow != null && heightRow.Length > 0)
                    {
                        _heightLayers[layerIndex].Add(heightRow);
                    }

                    if (grayRow != null && grayRow.Length > 0)
                    {
                        _grayLayers[layerIndex].Add(grayRow);
                    }
                }
            }

            int displayLayerCount = Math.Min(_heightLayers.Count, _grayLayers.Count);
            if (_selectedLayerIndex >= displayLayerCount)
            {
                _selectedLayerIndex = 0;
                RaisePropertyChanged(nameof(SelectedLayerIndex));
            }

            RaisePropertyChanged(nameof(LayerCountText));
            RenderSelectedLayer();
        }

        private void RenderSelectedLayer()
        {
            int displayLayerCount = Math.Min(_heightLayers.Count, _grayLayers.Count);
            if (displayLayerCount == 0)
            {
                return;
            }

            int layerIndex = Math.Min(_selectedLayerIndex, displayLayerCount - 1);
            if (layerIndex != _selectedLayerIndex)
            {
                _selectedLayerIndex = layerIndex;
                RaisePropertyChanged(nameof(SelectedLayerIndex));
            }

            List<float[]> grayRows = _grayLayers[layerIndex];
            if (grayRows.Count > 0 && CommonAlgorithm.ConvertListToHObject(grayRows, ImageType.Gray, out HObject grayImage) == 0)
            {
                GrayImage = grayImage;
            }

            List<float[]> heightRows = _heightLayers[layerIndex];
            if (heightRows.Count > 0 && Common_Algorithm.ConvertListToMat(heightRows, ImageType.Depth, out Mat heightImage) == 0)
            {
                HeightDisplayResult = new ImageResultsDisplay
                {
                    HeightImage = heightImage
                };
            }
        }


        #endregion

        #region Commands
        /// <summary>
        /// 设置参数变更指令
        /// </summary>
        public DelegateCommand<string> ValueChangedCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "占空比":
                    {
                        Console.WriteLine("设置占空比为:" + ModelParam.JingCeSensor.CmosConfig.DutyRatio.ToString());    
                        ModelParam.JingCeSensor.CmosConfig.ILedDuration = ModelParam.JingCeSensor.CmosConfig.Frequency == 0 ? 0 : (ModelParam.JingCeSensor.CmosConfig.DutyRatio * (1000000 / ModelParam.JingCeSensor.CmosConfig.Frequency)) / 100;
                        ModelParam.JingCeSensor.SetSensorParameters("int", "SetLEDPulseWidth", new int[] { ModelParam.JingCeSensor.CmosConfig.ILedDuration });
                    }
                    break;
                case "光源频率":
                    {
                        ModelParam.JingCeSensor.SetSensorParameters("int", "SetFrequency", new int[] { ModelParam.JingCeSensor.CmosConfig.Frequency });
                        ModelParam.JingCeSensor.CmosConfig.ILedDuration = ModelParam.JingCeSensor.CmosConfig.Frequency == 0 ? 0 : (ModelParam.JingCeSensor.CmosConfig.DutyRatio * (1000000 / ModelParam.JingCeSensor.CmosConfig.Frequency)) / 100;
                        ModelParam.JingCeSensor.SetSensorParameters("int", "SetLEDPulseWidth", new int[] { ModelParam.JingCeSensor.CmosConfig.ILedDuration });
                    }
                    break;
                case "数据点格式":
                    {
                        ModelParam.JingCeSensor.SetSensorParameters("int", "SetPointDataFormat", new int[] { (int)ModelParam.JingCeSensor.CmosConfig.PointDataForm });
                    }
                    break;
                case "Z范围":
                    {
                        ModelParam.JingCeSensor.SetSensorParameters("double", "SetZRange", ModelParam.JingCeSensor.CmosConfig.ZWidthRange);
                    }
                    break;
                case "无效值":
                    {
                        ModelParam.JingCeSensor.SetSensorParameters("double", "SetProfileInvalidValue", ModelParam.JingCeSensor.CmosConfig.ProfileInvalidValue);
                    }
                    break;

                case "桢速度模式":
                    {
                        ModelParam.JingCeSensor.SetSensorParameters("int", "SetSensorBinning", new int[] { (int)ModelParam.JingCeSensor.CmosConfig.Binning });
                    }
                    break;
                case "开窗范围最小值":
                    {
                        ModelParam.JingCeSensor.SetSensorParameters("int", "SetWindowRange", new int[] { (int)ModelParam.JingCeSensor.CmosConfig.ZPixelRangeMin , (int)ModelParam.JingCeSensor.CmosConfig.ZPixelRangeMax });
                    }
                    break;
                case "开窗范围最大值":
                    {
                        ModelParam.JingCeSensor.SetSensorParameters("int", "SetWindowRange", new int[] { (int)ModelParam.JingCeSensor.CmosConfig.ZPixelRangeMin, (int)ModelParam.JingCeSensor.CmosConfig.ZPixelRangeMax });
                    }
                    break;
                case "曝光":
                    {
                        ModelParam.JingCeSensor.SetSensorParameters("int", "SetExposure", new int[] { ModelParam.JingCeSensor.CmosConfig.ExposureTime });
                    }
                    break;
                case "本底阈值":
                    {
                        ModelParam.JingCeSensor.SetSensorParameters("int", "SetBias", new int[] { ModelParam.JingCeSensor.CmosConfig.Bias });
                    }
                    break;
                case "最低灰度阈值":
                    {
                        ModelParam.JingCeSensor.SetSensorParameters("int", "SetLeastGray", new int[] { ModelParam.JingCeSensor.CmosConfig.LeastGray });
                    }
                    break;
                case "最低数量阈值":
                    {
                        ModelParam.JingCeSensor.SetSensorParameters("int", "SetLeastNumber", new int[] { ModelParam.JingCeSensor.CmosConfig.LeastNumber });
                    }
                    break;
                case "外触发类型":
                    {
                        ModelParam.JingCeSensor.SetSensorParameters("int", "SetOuterTriggerType", new int[] { (int)ModelParam.JingCeSensor.CmosTriggerConfig.OutTriggerType });
                    }
                    break;
                case "触发间隔":
                    {
                        ModelParam.JingCeSensor.SetSensorParameters("int", "SetOuterTriggerFilter", new int[] { ModelParam.JingCeSensor.CmosTriggerConfig.OuterTriggerFilter });
                    }
                    break;
                case "外触发高电平抑制信号":
                    {
                        ModelParam.JingCeSensor.SetSensorParameters("int", "SetOuterTriggerHighlevelSwitch", new int[] { (int)ModelParam.JingCeSensor.CmosTriggerConfig.OuterTriggerHighlevelSwitch });
                    }
                    break;
                case "外触发开关":
                    {
                        if(ModelParam.JingCeSensor.CmosTriggerConfig.ExternalTriggerSwitch == Defines.General.ExternalTriggerSwitch.打开外触发)
                        {
                            ModelParam.JingCeSensor.SetSensorParameters("", "OpenTrigger", new int[] { });
                        }
                        else
                        {
                            ModelParam.JingCeSensor.SetSensorParameters("", "CloseTrigger", new int[] { });
                        }
                    }
                    break;
                default:
                    break;
            }
        });


        public DelegateCommand<string> SensorCtrlCommand => new DelegateCommand<string>((order) =>
        {
            if (ModelParam?.JingCeSensor == null)
                return;

            
            switch (order)
            {
                case "开始采集":
                    _heightLayers.Clear();
                    _grayLayers.Clear();
                    RaisePropertyChanged(nameof(LayerCountText));
                    GrayImage = null;
                    HeightDisplayResult = null;
                    ModelParam.JingCeSensor.StartCollect();
                    break;
                case "停止采集":
                    if (!ModelParam.JingCeSensor.TryStopCollect())
                    {
                        return;
                    }
                    RenderCollectedData();
                    break;
                default:
                    break;
            }
        });

        /// <summary>
        /// 通用指令
        /// </summary>
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "执行":
                    break;
                case "取消":
                    CloseDialog(ButtonResult.No);
                    break;
                case "确认":
                    CloseDialog(ButtonResult.OK, new DialogParameters()
                    {
                        { "Param", ModelParam.JingCeSensor },
                    });
                    break;
                default:
                    break;
            }

        });
        #endregion
    }
}
