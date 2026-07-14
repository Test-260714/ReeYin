using HalconDotNet;
using MPSizectorS_DotNet;
using OpenCvSharp;
using ReeYin.Hardware.Sensor.MEGAPHASE.Models;
using ReeYin_V.Core;
using ReeYin_V.Core.Helper.ImageOP;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.ResultsDisplay;
using ReeYin_V.Core.Services.DataCollectRelated;
using System.Windows.Threading;

namespace ReeYin.Hardware.Sensor.MEGAPHASE.ViewModels
{
    public class MegAphaseSensorViewModel : DialogViewModelBase
    {
        #region Fields
        private readonly Common_Algorithm _commonAlgorithm;
        private readonly DispatcherTimer _renderTimer;
        private bool _isCollectOnceRunning;
        private MegAphaseSensorModel _modelParam = new MegAphaseSensorModel();
        private HObject _disposeImage;
        #endregion

        #region Constructor
        public MegAphaseSensorViewModel()
        {
            _commonAlgorithm = PrismProvider.Container.Resolve(typeof(Common_Algorithm)) as Common_Algorithm ?? new Common_Algorithm();
            _renderTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _renderTimer.Tick += RenderTimer_Tick;
        }
        #endregion

        #region Properties
        public MegAphaseSensorModel ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        public IReadOnlyList<DisplayOption<WorkingModeType>> WorkingModeItems { get; } =
        [
            new DisplayOption<WorkingModeType>(WorkingModeType.Fast3D, "快速3D"),
            new DisplayOption<WorkingModeType>(WorkingModeType.Standard3D, "标准3D"),
            new DisplayOption<WorkingModeType>(WorkingModeType.Precise3D, "精密3D"),
            new DisplayOption<WorkingModeType>(WorkingModeType.SuperPrecise3D, "超精密3D"),
            new DisplayOption<WorkingModeType>(WorkingModeType.White2D, "白底2D"),
            new DisplayOption<WorkingModeType>(WorkingModeType.Black2D, "黑底2D"),
            new DisplayOption<WorkingModeType>(WorkingModeType.Grid2D, "网格2D"),
            new DisplayOption<WorkingModeType>(WorkingModeType.ExposurePrediction2D, "曝光预测2D"),
            new DisplayOption<WorkingModeType>(WorkingModeType.SuperFast3D, "超快速3D"),
            new DisplayOption<WorkingModeType>(WorkingModeType.FastPrecision3D, "快速精密3D"),
            new DisplayOption<WorkingModeType>(WorkingModeType.SuperDynamic3D, "超动态3D"),
        ];

        public IReadOnlyList<DisplayOption<TriggerSourceType>> TriggerSourceItems { get; } =
        [
            new DisplayOption<TriggerSourceType>(TriggerSourceType.SoftTriggerOnly, "软件触发"),
            new DisplayOption<TriggerSourceType>(TriggerSourceType.KeepRunning, "连续采集"),
            new DisplayOption<TriggerSourceType>(TriggerSourceType.HardTriggerI0, "硬触发I0"),
            new DisplayOption<TriggerSourceType>(TriggerSourceType.HardTriggerI1, "硬触发I1"),
        ];

        public IReadOnlyList<DisplayOption<SoftwarePreprocessModeType>> SoftwarePreprocessModeItems { get; } =
        [
            new DisplayOption<SoftwarePreprocessModeType>(SoftwarePreprocessModeType.SoftwarePreprocessModeType_Off, "关闭"),
            new DisplayOption<SoftwarePreprocessModeType>(SoftwarePreprocessModeType.SoftwarePreprocessModeType_Weak, "弱"),
            new DisplayOption<SoftwarePreprocessModeType>(SoftwarePreprocessModeType.SoftwarePreprocessModeType_Normal, "标准"),
            new DisplayOption<SoftwarePreprocessModeType>(SoftwarePreprocessModeType.SoftwarePreprocessModeType_Strong, "强"),
            new DisplayOption<SoftwarePreprocessModeType>(SoftwarePreprocessModeType.SoftwarePreprocessModeType_Custom, "自定义"),
        ];

        /// <summary>
        /// 灰度数据
        /// </summary>
        public HObject DisposeImage
        {
            get { return _disposeImage; }
            set
            {
                if (ReferenceEquals(_disposeImage, value))
                {
                    return;
                }

                _disposeImage?.Dispose();
                _disposeImage = value;
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
        #endregion

        #region Commands
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>(async (order) =>
        {
            switch (order)
            {
                case "连接":
                    if (ModelParam.MegAphaseSensor.Init())
                    {
                        ModelParam.MegAphaseSensor.RefreshSettingsFromDevice();
                    }
                    break;
                case "断开连接":
                    _renderTimer.Stop();
                    ModelParam.MegAphaseSensor.Close();
                    break;
                case "开始采集":
                    ModelParam.MegAphaseSensor.StartCollect();
                    if (ModelParam.MegAphaseSensor.State == HardwareState.Running)
                    {
                        _renderTimer.Start();
                    }
                    break;
                case "结束采集":
                    _renderTimer.Stop();
                    ModelParam.MegAphaseSensor.StopCollect();
                    break;
                case "切换Hold状态":
                    ModelParam.MegAphaseSensor.Settings.HoldState = ModelParam.MegAphaseSensor.Settings.HoldState == 0 ? (byte)1 : (byte)0;
                    break;
                case "发送软件触发":
                    if (_isCollectOnceRunning)
                    {
                        return;
                    }

                    try
                    {
                        _isCollectOnceRunning = true;
                        await ModelParam.MegAphaseSensor.CollectOnceAsync();
                        RenderCollectedData();
                    }
                    finally
                    {
                        _isCollectOnceRunning = false;
                    }
                    break;
                case "确认":
                    CloseDialog(ButtonResult.OK, new DialogParameters()
                    {
                        { "Param", ModelParam.MegAphaseSensor },
                    });
                    break;
                case "取消":
                    CloseDialog(ButtonResult.No);
                    break;
                default:
                    break;
            }
        });
        #endregion

        #region Methods
        public override void InitParam()
        {
            if (Param is MegAphaseSensor sensor)
                ModelParam.MegAphaseSensor = sensor;
            else
                ModelParam.MegAphaseSensor = new MegAphaseSensor();

            ModelParam.MegAphaseSensor.RefreshSettingsFromDevice();
        }

        public override void OnDialogClosed()
        {
            _renderTimer.Stop();
            base.OnDialogClosed();
        }

        private void RenderTimer_Tick(object? sender, EventArgs e)
        {
            RenderCollectedData(false);
        }

        /// <summary>
        /// 接收数据并渲染
        /// </summary>
        private void RenderCollectedData(bool clearWhenNoData = true)
        {
            List<MeasureData> measureData = ModelParam.MegAphaseSensor.ReceiveSensorData();
            if (measureData.Count == 0)
            {
                if (clearWhenNoData)
                {
                    DisposeImage = null;
                    HeightDisplayResult = null;
                }

                return;
            }

            List<float[]> grayRows = new List<float[]>(measureData.Count);
            List<float[]> zRows = new List<float[]>(measureData.Count);
            foreach (MeasureData item in measureData)
            {
                if (item?.AreaData == null || item.AreaData.Count == 0)
                {
                    continue;
                }

                if (item.AreaData.Count > 1)
                {
                    zRows.Add(item.AreaData[0]);
                    grayRows.Add(item.AreaData[1]);
                    continue;
                }

                grayRows.Add(item.AreaData[0]);
            }

            if (grayRows.Count > 0 && _commonAlgorithm.ConvertListToHObject(grayRows, ReeYin_V.Core.Helper.ImageOP.ImageType.Gray, out HObject grayImage) == 0)
            {
                DisposeImage = grayImage;
            }


            if (zRows.Count > 0 && Common_Algorithm.ConvertListToMat(zRows, ReeYin_V.Core.Helper.ImageOP.ImageType.Depth, out Mat heightImage) == 0)
            {
                HeightDisplayResult = new ImageResultsDisplay
                {
                    HeightImage = heightImage
                };
            }
        }
        #endregion
    }

    public class DisplayOption<T>
    {
        public DisplayOption(T value, string displayName)
        {
            Value = value;
            DisplayName = displayName;
        }

        public T Value { get; }

        public string DisplayName { get; }
    }
}
