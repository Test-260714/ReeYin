using HalconDotNet;
using OpenCvSharp;
using ReeYin.Hardware.Sensor.ChroCodile.Models;
using ReeYin.Hardware.Sensor.Models;
using ReeYin_V.Core.Helper.ImageOP;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.ResultsDisplay;
using ReeYin_V.Core.Services.DataCollectRelated;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using static OpenCvSharp.Stitcher;
using static PrecitecClass.PrecitecControllerClassSync;
using MessageBox = System.Windows.MessageBox;

namespace ReeYin.Hardware.Sensor.ChroCodile.ViewModels
{

    public class ChroCodileSensorViewModel : DialogViewModelBase
    {
        #region Properties
        private readonly Common_Algorithm _commonAlgorithm;

        public ChroCodileSensorViewModel()
        {
            _commonAlgorithm = PrismProvider.Container.Resolve(typeof(Common_Algorithm)) as Common_Algorithm ?? new Common_Algorithm();
        }

        private ChroCodileSensorModel _modelParam = new ChroCodileSensorModel();
        public ChroCodileSensorModel ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 灰度数据
        /// </summary>
        private HObject _grayImage;
        public HObject GrayImage
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

        /// <summary>
        /// 高度数据
        /// </summary>
        private ImageResultsDisplay _heightDisplayResult;
        public ImageResultsDisplay HeightDisplayResult
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

        /// <summary>
        /// 开启触发按钮开关
        /// </summary>
        private bool _isStartCollectEnabled = true;
        public bool IsStartCollectEnabled
        {
            get => _isStartCollectEnabled;
            set
            {
                _isStartCollectEnabled = value;
                RaisePropertyChanged();
            }
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
                case "开启触发":
                    try
                    {
                        ModelParam.ChroCodileSensor.StartCollect();
                        IsStartCollectEnabled = false;
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show($"{ex.Message}", "提示");
                    }
                    break;
                case "取消":
                    CloseDialog(ButtonResult.No);
                    break;
                case "确认":
                    CloseDialog(ButtonResult.OK, new DialogParameters()
                    {
                        { "Param", ModelParam.ChroCodileSensor },
                    });
                    break;
                case "数据渲染":
                    RenderCollectedData();
                    IsStartCollectEnabled = true;
                    break;
                default:
                    break;
            }

        });

        /// <summary>
        /// 设置参数变更指令
        /// </summary>
        public DelegateCommand<string> ValueChangedCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "输出信号":
                    {
                        string outsigs = ModelParam.ChroCodileSensor.CurrentConfig.OutputSignal.Replace(',', ' ');

                        ModelParam.ChroCodileSensor.SetParameters("输出信号", outsigs, true);

                    }
                    break;
                case "采样频率":
                    {
                        ModelParam.ChroCodileSensor.SetParameters("采样频率", ModelParam.ChroCodileSensor.CurrentConfig.SamplingFrequency, true);
                    }
                    break;
                case "光强":
                    {
                        ModelParam.ChroCodileSensor.SetParameters("光强", ModelParam.ChroCodileSensor.CurrentConfig.LightIntensity, true);
                    }
                    break;
                case "编码器触发":
                case "IO触发":
                    {
                        if(order == "编码器触发")
                        {
                            ModelParam.ChroCodileSensor.CurrentConfig.IsIOTrigger = false;
                            ModelParam.ChroCodileSensor.CurrentConfig.IsContinuouTrigger = false;
                        }
                        else
                        {
                            ModelParam.ChroCodileSensor.CurrentConfig.IsContinuouTrigger = false;
                            ModelParam.ChroCodileSensor.CurrentConfig.IsEncoderTrigger = false;
                        }
                        ModelParam.ChroCodileSensor.SetParameters("外部触发", "TRE", false);
                    }
                    break;
                case "连续采集":
                    {
                        ModelParam.ChroCodileSensor.CurrentConfig.IsIOTrigger = false;
                        ModelParam.ChroCodileSensor.CurrentConfig.IsEncoderTrigger = false;
                        ModelParam.ChroCodileSensor.SetParameters("连续采集", "CTN", false);
                    }
                    break;
                case "手动设置宽度":
                    {
                        MessageBoxResult messageboxresult = MessageBox.Show("确定要手动设置宽度吗？这个操作会引起编码器模式下的起始位置和结束位置两个参数失效!", "提示", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                        if (messageboxresult == MessageBoxResult.No)
                        {
                            ModelParam.ChroCodileSensor.CurrentConfig.ManualWidthEnabled = false;
                        }
                    }
                    break;
                case "阈值":
                    {
                        ModelParam.ChroCodileSensor.SetParameters("阈值", ModelParam.ChroCodileSensor.CurrentConfig.Threshold, true);
                    }
                    break;
                default:
                    break;
            }
        });
        #endregion

        #region Methods
        /// <summary>
        /// 初始化参数
        /// </summary>
        public override void InitParam()
        {
            if (Param != null && (Param is ChroCodileSensor))
                ModelParam.ChroCodileSensor = Param as ChroCodileSensor ?? new ChroCodileSensor();
            else
                ModelParam.ChroCodileSensor = new ChroCodileSensor();

            InitializeSensorParameters();
        }

        /// <summary>
        /// 初始化传感器参数
        /// </summary>
        public void InitializeSensorParameters()
        {
            if(ModelParam.ChroCodileSensor._controller.Status == ConnectStatus.Disconnected || ModelParam.ChroCodileSensor._controller.Status == ConnectStatus.Idle)
            {
                return;
            }

            string samplingfrequency =  ModelParam.ChroCodileSensor.GetParameters("SHZ?") == "" ? ModelParam.ChroCodileSensor.CurrentConfig.SamplingFrequency.ToString() : ModelParam.ChroCodileSensor.GetParameters("SHZ?");

            Match match = Regex.Match(samplingfrequency, "\\d+\\.\\d+|\\d+");

            if (match.Value == "")
            {
                return;
            }

            ModelParam.ChroCodileSensor.CurrentConfig.SamplingFrequency = double.Parse(match.Value);

            string rowcount = ModelParam.ChroCodileSensor.GetParameters("LAI?") == "" ? ModelParam.ChroCodileSensor.CurrentConfig.LightIntensity.ToString() : ModelParam.ChroCodileSensor.GetParameters("LAI?");

            match = Regex.Match(rowcount, "\\d+");

            ModelParam.ChroCodileSensor.CurrentConfig.LightIntensity = int.Parse(match.Value);

            string measurementmode = ModelParam.ChroCodileSensor.GetParameters("MMD?");  //测量模式

            match = Regex.Match(measurementmode, "\\d+");

            string cmd = match.Value == "1" ? "QTH" : "THR";

            string Threshold = ModelParam.ChroCodileSensor.GetParameters($"{cmd}?");

            match = Regex.Match(Threshold, "\\d+");

            ModelParam.ChroCodileSensor.CurrentConfig.Threshold = int.Parse(match.Value);
        }

        private void RenderCollectedData()
        {
            List<MeasureData> measureData = ModelParam.ChroCodileSensor.ReceiveSensorData();
            if (measureData.Count == 0)
            {
                 GrayImage = null;
                HeightDisplayResult = null;
                return;
            }

            List<float[]> grayRows = new List<float[]>(measureData.Count);
            List<float[]> heightRows = new List<float[]>(measureData.Count);
            foreach (MeasureData item  in measureData)
            {
                if (item?.AreaData == null || item.AreaData.Count == 0)
                {
                    continue;
                }

                if (item.AreaData.Count > 1)
                {
                    heightRows.Add(item.AreaData[0]);
                    grayRows.Add(item.AreaData[1]);
                    continue;
                }

                grayRows.Add(item.AreaData[0]);
            }

            if (grayRows.Count > 0 && _commonAlgorithm.ConvertListToHObject(grayRows, ReeYin_V.Core.Helper.ImageOP.ImageType.Gray, out HObject grayImage) == 0)
            {
                GrayImage = grayImage;
            }
            else
            {
                GrayImage = null;
            }

            if (heightRows.Count > 0 && Common_Algorithm.ConvertListToMat(heightRows, ReeYin_V.Core.Helper.ImageOP.ImageType.Depth, out Mat heightImage) == 0)
            {
                HeightDisplayResult = new ImageResultsDisplay
                {
                    HeightImage = heightImage
                };
            }
            else
            {
                HeightDisplayResult = null;
            }
        }
        #endregion
    }
}
