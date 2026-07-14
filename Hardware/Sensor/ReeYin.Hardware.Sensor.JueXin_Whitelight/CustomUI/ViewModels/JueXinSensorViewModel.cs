using Prism.Commands;
using ReeYin.Hardware.Sensor.JueXin;
using ReeYin.Hardware.Sensor.JueXin.API;
using ReeYin.Hardware.Sensor.JueXin.CustomUI.Models;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.IOC;
using ReeYin_V.UI;
using System.Collections.Generic;
using System.Windows.Controls;

namespace ReeYin.Hardware.Sensor.JueXin.CustomUI.ViewModels
{
    public class JueXinSensorViewModel : DialogViewModelBase
    {
        private Grid _singlePointChartGrid = new Grid();
        public Grid SinglePointChartGrid
        {
            get { return _singlePointChartGrid; }
            set { _singlePointChartGrid = value; RaisePropertyChanged(); }
        }

        private Grid _outlineChartGrid = new Grid();
        public Grid OutlineChartGrid
        {
            get { return _outlineChartGrid; }
            set { _outlineChartGrid = value; RaisePropertyChanged(); }
        }

        private JueXinSensorModel _modelParam = new JueXinSensorModel();
        public new JueXinSensorModel ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        public IReadOnlyList<KeyValuePair<int, string>> ExposureModeItems { get; } = new List<KeyValuePair<int, string>>
        {
            new KeyValuePair<int, string>(0, "手动曝光"),
            new KeyValuePair<int, string>(1, "自动曝光")
        };

        public IReadOnlyList<KeyValuePair<int, string>> SpectrumTypeItems { get; } = new List<KeyValuePair<int, string>>
        {
            new KeyValuePair<int, string>(0, "原始光谱"),
            new KeyValuePair<int, string>(1, "校准光谱")
        };

        public IReadOnlyList<KeyValuePair<int, string>> FftProcOptionItems { get; } = new List<KeyValuePair<int, string>>
        {
            new KeyValuePair<int, string>(0, "关闭"),
            new KeyValuePair<int, string>(1, "频谱移动平均"),
            new KeyValuePair<int, string>(2, "频谱累加")
        };

        public IReadOnlyList<KeyValuePair<int, string>> FftWindowItems { get; } = new List<KeyValuePair<int, string>>
        {
            new KeyValuePair<int, string>(0, "2"),
            new KeyValuePair<int, string>(1, "4")
        };

        public IReadOnlyList<KeyValuePair<int, string>> FilterTypeItems { get; } = new List<KeyValuePair<int, string>>
        {
            new KeyValuePair<int, string>(0, "关闭滤波"),
            new KeyValuePair<int, string>(1, "中值滤波"),
            new KeyValuePair<int, string>(2, "平均滤波")
        };

        public IReadOnlyList<KeyValuePair<int, string>> SyncModeItems { get; } = new List<KeyValuePair<int, string>>
        {
            new KeyValuePair<int, string>(0, "主机模式"),
            new KeyValuePair<int, string>(1, "从机模式")
        };

        public IReadOnlyList<KeyValuePair<int, string>> OnOffItems { get; } = new List<KeyValuePair<int, string>>
        {
            new KeyValuePair<int, string>(0, "关闭"),
            new KeyValuePair<int, string>(1, "打开")
        };

        public IReadOnlyList<KeyValuePair<WliSdkCAPI.WliTrigSource, string>> TriggerSourceItems { get; } = new List<KeyValuePair<WliSdkCAPI.WliTrigSource, string>>
        {
            new KeyValuePair<WliSdkCAPI.WliTrigSource, string>(WliSdkCAPI.WliTrigSource.WLI_TRIG_SOURCE_TRIGGER, "触发输入"),
            new KeyValuePair<WliSdkCAPI.WliTrigSource, string>(WliSdkCAPI.WliTrigSource.WLI_TRIG_SOURCE_ENCODER1, "编码器1"),
            new KeyValuePair<WliSdkCAPI.WliTrigSource, string>(WliSdkCAPI.WliTrigSource.WLI_TRIG_SOURCE_ENCODER2, "编码器2"),
            new KeyValuePair<WliSdkCAPI.WliTrigSource, string>(WliSdkCAPI.WliTrigSource.WLI_TRIG_SOURCE_ENCODER3, "编码器3")
        };

        public IReadOnlyList<KeyValuePair<WliSdkCAPI.WliTrigMode, string>> TriggerModeItems { get; } = new List<KeyValuePair<WliSdkCAPI.WliTrigMode, string>>
        {
            new KeyValuePair<WliSdkCAPI.WliTrigMode, string>(WliSdkCAPI.WliTrigMode.WLI_TRIG_MODE_EDGE, "边沿触发"),
            new KeyValuePair<WliSdkCAPI.WliTrigMode, string>(WliSdkCAPI.WliTrigMode.WLI_TRIG_MODE_LEVEL, "电平触发"),
            new KeyValuePair<WliSdkCAPI.WliTrigMode, string>(WliSdkCAPI.WliTrigMode.WLI_TRIG_MODE_COUNT, "计数触发"),
            new KeyValuePair<WliSdkCAPI.WliTrigMode, string>(WliSdkCAPI.WliTrigMode.WLI_TRIG_MODE_LOCATION, "位置触发")
        };

        public IReadOnlyList<KeyValuePair<WliSdkCAPI.WliTrigLevel, string>> TriggerLevelItems { get; } = new List<KeyValuePair<WliSdkCAPI.WliTrigLevel, string>>
        {
            new KeyValuePair<WliSdkCAPI.WliTrigLevel, string>(WliSdkCAPI.WliTrigLevel.WLI_TRIG_LEVEL_RISE_EDGE, "上升沿"),
            new KeyValuePair<WliSdkCAPI.WliTrigLevel, string>(WliSdkCAPI.WliTrigLevel.WLI_TRIG_LEVEL_FALL_EDGE, "下降沿"),
            new KeyValuePair<WliSdkCAPI.WliTrigLevel, string>(WliSdkCAPI.WliTrigLevel.WLI_TRIG_LEVEL_HIGH_LEVEL, "高电平"),
            new KeyValuePair<WliSdkCAPI.WliTrigLevel, string>(WliSdkCAPI.WliTrigLevel.WLI_TRIG_LEVEL_LOW_LEVEL, "低电平")
        };

        public IReadOnlyList<KeyValuePair<WliSdkCAPI.WliEncoderInputMode, string>> EncoderInputModeItems { get; } = new List<KeyValuePair<WliSdkCAPI.WliEncoderInputMode, string>>
        {
            new KeyValuePair<WliSdkCAPI.WliEncoderInputMode, string>(WliSdkCAPI.WliEncoderInputMode.WLI_ENCODER_INPUT_MODE_SINGLE_PHASE, "单相"),
            new KeyValuePair<WliSdkCAPI.WliEncoderInputMode, string>(WliSdkCAPI.WliEncoderInputMode.WLI_ENCODER_INPUT_MODE_TWO_PHASE, "双相")
        };

        public IReadOnlyList<KeyValuePair<WliSdkCAPI.WliTrigDirection, string>> EncoderDirectionItems { get; } = new List<KeyValuePair<WliSdkCAPI.WliTrigDirection, string>>
        {
            new KeyValuePair<WliSdkCAPI.WliTrigDirection, string>(WliSdkCAPI.WliTrigDirection.WLI_TRIG_DIRECTION_POSITIVE, "正向"),
            new KeyValuePair<WliSdkCAPI.WliTrigDirection, string>(WliSdkCAPI.WliTrigDirection.WLI_TRIG_DIRECTION_NEGATIVE, "反向"),
            new KeyValuePair<WliSdkCAPI.WliTrigDirection, string>(WliSdkCAPI.WliTrigDirection.WLI_TRIG_DIRECTION_BIDIRECTION, "双向")
        };

        public IReadOnlyList<KeyValuePair<WliSdkCAPI.WliDecodeMode, string>> EncoderDecodeModeItems { get; } = new List<KeyValuePair<WliSdkCAPI.WliDecodeMode, string>>
        {
            new KeyValuePair<WliSdkCAPI.WliDecodeMode, string>(WliSdkCAPI.WliDecodeMode.WLI_DECODE_MODE_X1, "x1"),
            new KeyValuePair<WliSdkCAPI.WliDecodeMode, string>(WliSdkCAPI.WliDecodeMode.WLI_DECODE_MODE_X2, "x2"),
            new KeyValuePair<WliSdkCAPI.WliDecodeMode, string>(WliSdkCAPI.WliDecodeMode.WLI_DECODE_MODE_X4, "x4")
        };

        public override void InitParam()
        {
            if (Param != null && (Param is JueXinSensor))
                ModelParam.JueXinSensor = Param as JueXinSensor ?? new JueXinSensor();
            else
                ModelParam.JueXinSensor = new JueXinSensor();
        }

        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "Cancel":
                    CloseDialog(ButtonResult.No);
                    break;

                case "Confirm":
                    CloseDialog(ButtonResult.OK, new DialogParameters()
                    {
                        { "Param", ModelParam.JueXinSensor },
                    });
                    break;

                default:
                    break;
            }
        });

        // 这里只处理已连接后的 SDK 参数和采集动作；连接/断开入口在 SensorSetView。
        public DelegateCommand<string> SensorCtrlCommand => new DelegateCommand<string>((order) =>
        {
            bool success = true;

            switch (order)
            {
                case "LoadParams":
                    success = ModelParam.JueXinSensor.LoadDeviceParams();
                    break;

                case "ApplyBasic":
                    success = ModelParam.JueXinSensor.ApplyBasicParams();
                    break;

                case "ApplySpectrum":
                    success = ModelParam.JueXinSensor.ApplySpectrumParams();
                    break;

                case "ApplyThickness":
                    success = ModelParam.JueXinSensor.ApplyThicknessParams();
                    break;

                case "ApplyTrigger":
                    success = ModelParam.JueXinSensor.ApplyTriggerConfig();
                    break;

                case "ApplySync":
                    success = ModelParam.JueXinSensor.ApplySyncConfig();
                    break;

                case "ApplyEncoder":
                    success = ModelParam.JueXinSensor.ApplyEncoder1Config();
                    break;

                case "ApplyEncoder1":
                    success = ModelParam.JueXinSensor.ApplyEncoderConfig(1);
                    break;

                case "ApplyEncoder2":
                    success = ModelParam.JueXinSensor.ApplyEncoderConfig(2);
                    break;

                case "ApplyEncoder3":
                    success = ModelParam.JueXinSensor.ApplyEncoderConfig(3);
                    break;

                case "SetEncoderPosition":
                    success = ModelParam.JueXinSensor.SetEncoder1Position();
                    break;

                case "SetEncoderPosition1":
                    success = ModelParam.JueXinSensor.SetEncoderPosition(1);
                    break;

                case "SetEncoderPosition2":
                    success = ModelParam.JueXinSensor.SetEncoderPosition(2);
                    break;

                case "SetEncoderPosition3":
                    success = ModelParam.JueXinSensor.SetEncoderPosition(3);
                    break;

                case "SampleDarkSignal":
                    success = ModelParam.JueXinSensor.ExecuteSdkCommand("SampleDarkSignal", "暗校准");
                    break;

                case "SampleLightSource":
                    success = ModelParam.JueXinSensor.ExecuteSdkCommand("SampleLightSource", "采集光源");
                    break;

                case "StartCollect":
                    ModelParam.JueXinSensor.StartCollect();
                    break;

                case "StopCollect":
                    ModelParam.JueXinSensor.StopCollect();
                    break;

                case "ClearCache":
                    ModelParam.JueXinSensor.ClearDataQueue();
                    break;

                default:
                    return;
            }

            if (!success)
            {
                MessageView.Ins.MessageBoxShow(ModelParam.JueXinSensor.LastSdkMessage, eMsgType.Warn);
            }
        });
    }
}
