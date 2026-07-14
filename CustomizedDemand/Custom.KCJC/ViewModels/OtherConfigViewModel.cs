using Custom.KCJC.Models;
using OpenCvSharp;
using LogicalTool.Loop.Models;
using ReeYin.Hardware.Sensor.Hyperson;
using ReeYin.Hardware.Sensor.Hyperson.API;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.DataCollectRelated;
using ReeYin_V.Logger;
using ReeYin_V.Share;
using ReeYin_V.UI.Controls.Loading;
using ReeYin_V.UI.Style.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using ReeYin.Hardware.Sensor.Models;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Interfaces;

using static Custom.KCJC.Models.KCJC0_Algorithm;
using ReeYin_V.Share.Events;
using ReeYin_V.Core.Enums;

namespace Custom.KCJC.ViewModels
{
    public class OtherConfigViewModel : DialogViewModelBase, INavigationAware
    {
        #region Fields
        //KBTDispensing_Algorithm KBTCustomAlgo;
        //Drawing drawing = new Drawing();
        private Prism.Events.SubscriptionToken? _recipeSwitchedToken;
        #endregion

        #region Properties
        private OtherConfigModel _model = new OtherConfigModel();

        public OtherConfigModel Model
        {
            get { return _model; }
            set { _model = value; RaisePropertyChanged(); }
        }

        private string _filePath;
        /// <summary>
        /// 从文件中加载图片显示
        /// </summary>
        public string FilePath
        {
            get { return _filePath; }
            set { _filePath = value; RaisePropertyChanged(); }
        }

        private IConfigManager ConfigManager { get; }
        /// <summary>
        /// 保存按钮（传感器和解决方案）
        /// </summary>
        /// <param name="configManager"></param>
        public OtherConfigViewModel(IConfigManager configManager)
        {
            ConfigManager = configManager;
        }


        private KCJC0_MeasureParam _measureParam = new KCJC0_MeasureParam();
        public KCJC0_MeasureParam MeasureParam
        {
            get { return _measureParam; }
            set { _measureParam = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 当前绑定的海伯森传感器，用于同步曝光时间。
        /// </summary>
        private HypersenSensor _hypersenSensor;
        public HypersenSensor HypersenSensor
        {
            get { return _hypersenSensor; }
            set
            {
                if (_hypersenSensor?.CmosConfig != null)
                    _hypersenSensor.CmosConfig.PropertyChanged -= HypersenCmosConfig_PropertyChanged;

                _hypersenSensor = value;

                if (_hypersenSensor?.CmosConfig != null)
                    _hypersenSensor.CmosConfig.PropertyChanged += HypersenCmosConfig_PropertyChanged;

                RaisePropertyChanged();
            }
        }

        private LoopModel? _startTheCycleModel;
        /// <summary>
        /// 当前方案中的开始循环模块，用于同步循环次数。
        /// </summary>
        public LoopModel? StartTheCycleModel
        {
            get { return _startTheCycleModel; }
            set
            {
                if (_startTheCycleModel != null)
                    _startTheCycleModel.PropertyChanged -= StartTheCycleModel_PropertyChanged;

                _startTheCycleModel = value;

                if (_startTheCycleModel != null)
                    _startTheCycleModel.PropertyChanged += StartTheCycleModel_PropertyChanged;

                RaisePropertyChanged();
                RaisePropertyChanged(nameof(StartTheCycleLoopNum));
                RaisePropertyChanged(nameof(HasStartTheCycleModel));
            }
        }

        /// <summary>
        /// 是否存在开始循环模块。
        /// </summary>
        public bool HasStartTheCycleModel => StartTheCycleModel != null;

        /// <summary>
        /// 桥接开始循环模块的循环次数，只修改配置值，避免影响运行中的剩余次数。
        /// </summary>
        public int StartTheCycleLoopNum
        {
            get { return StartTheCycleModel?.LoopNum ?? 0; }
            set
            {
                if (StartTheCycleModel == null)
                    return;

                if (StartTheCycleModel.LoopNum == value)
                    return;

                StartTheCycleModel.LoopNum = value;
                RaisePropertyChanged();
            }
        }

        private void StartTheCycleModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LoopModel.LoopNum))
                RaisePropertyChanged(nameof(StartTheCycleLoopNum));
        }

        private void HypersenCmosConfig_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(CMOSConfig.ExposureTime) || HypersenSensor == null)
                return;

            PrismProvider.Dispatcher.BeginInvoke(() =>
            {
                Model.ExposureTime = HypersenSensor.CmosConfig.ExposureTime;
            });
        }

        #endregion

        #region Constructor
        public OtherConfigViewModel()
        {
            //KBTCustomAlgo = PrismProvider.Container.Resolve(typeof(KBTDispensing_Algorithm)) as KBTDispensing_Algorithm;
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
                case "执行标定":
                    {
                        PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Publish($"ClearStandardFilmPointCache@{Model.CalibrationNodeSerial}");
                        PrismProvider.EventAggregator.GetEvent<SwitchWorkStatusEvent>().Publish((eRunStatus.Running, Model.CalibrationNodeSerial));
                    }
                    break;
                case "打开标定页面":
                    PrismProvider.DialogService.Show("CalibrationView", new DialogParameters
                    {
                        { "Title", "标定页面" },
                        { "Serial", Serial },
                        { "Icon", "\ue673" },
                    }, result => { }, nameof(DialogWindowView));
                    break;
                case "信息配置":
                    PrismProvider.DialogService.Show("InfoConfigView", new DialogParameters
                    {
                        { "Title", "信息配置" },
                        { "Serial", Serial },
                        { "Icon", "\ue673" },
                    }, result => { }, nameof(DialogWindowView));
                    break;
                case "保存":
                    {
                        MessageBoxResult result = System.Windows.MessageBox.Show(
                            "确定要保存吗?",
                            "操作确认",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (result != MessageBoxResult.Yes)
                            return;
                        //1. 写入传感器配置
                        if (HypersenSensor != null)
                            HypersenSensor.SaveToSensor();

                        // 2. 保存解决方案
                        PrismProvider.EventAggregator.GetEvent<SolutionOperationEvent>().Publish("保存");

                        System.Windows.MessageBox.Show("保存成功！");
                    }
                    break;

                case "打开":
                    {
                        PrismProvider.Dispatcher.Invoke(() =>
                        {
                            using (var folderDialog = new FolderBrowserDialog())
                            {
                                folderDialog.Description = "请选择文件夹";
                                folderDialog.ShowNewFolderButton = true;

                                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                                {
                                    FilePath = folderDialog.SelectedPath;
                                }
                            }
                        });
                        Logs.LogInfo($"文件路径为：{FilePath}");
                        string[] subdirectories = Directory.GetDirectories(FilePath);
                        //从文件中加载图片执行算法流程
                        var loading = new LoadingWindow()
                        {
                            Topmost = true
                        };

                        //Task.Run(() =>
                        //{
                        //    PrismProvider.Dispatcher.BeginInvoke(() =>
                        //    {
                        //        loading.Show();
                        //    });
                        //    //SltModel.StopCollect();
                        //    string[] folders = Directory.GetDirectories(FilePath);

                        //    for (int i = 0; i < folders.Length; i++)
                        //    {
                        //        string folder = folders[i];

                        //        string grayImageDir = folder + "/gray";
                        //        string heightImageDir = folder + "/depth";

                        //        string grayImagePath = grayImageDir + $"/1_tif.tif";
                        //        string heightImageL0Path = heightImageDir + $"/1_tif_0.tif";
                        //        string heightImageL1Path = heightImageDir + $"/1_tif_1.tif";

                        //        Mat grayImage = Cv2.ImRead(grayImagePath, ImreadModes.Grayscale);
                        //        Mat heightImage = Cv2.ImRead(heightImageL0Path, ImreadModes.Unchanged);
                        //        Mat height1Image = Cv2.ImRead(heightImageL1Path, ImreadModes.Unchanged);
                        //        List<float[]> grayDate = KBTCustomAlgo.ConvertMatToList(grayImage);
                        //        List<float[]> heightDate = KBTCustomAlgo.ConvertMatToList(heightImage);
                        //        List<float[]> height1Date = KBTCustomAlgo.ConvertMatToList(height1Image);

                        //        Stopwatch stopwatch0 = Stopwatch.StartNew();

                        //        KBTCustomAlgo.Process(grayDate, heightDate, height1Date, ConvertMeasureParam(Algorithm));

                        //        stopwatch0.Stop();
                        //        Console.WriteLine($"Process运行时间：{stopwatch0.Elapsed.TotalMilliseconds} 毫秒");
                        //    }

                        //    Stopwatch stopwatch1 = Stopwatch.StartNew();
                        //    KBTDispensing_MeasureResult _measureResult = KBTCustomAlgo.GetMeasureResult();
                        //    Mat gray, depth;

                        //    DateTime dateTime = DateTime.Now;
                        //    drawing.CvDrawResult(_measureResult, out gray, out depth);

                        //    Console.WriteLine("绘制图形耗时：" + DateTime.Now.Subtract(dateTime).TotalMilliseconds);
                        //    //推送检测结果
                        //    PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Publish(("KBTDispensing_MeasureResult", _measureResult));

                        //    stopwatch1.Stop();
                        //    Console.WriteLine($"GetMeasureResult运行时间：{stopwatch1.Elapsed.TotalMilliseconds} 毫秒");
                        //    ProcessedData processedData = new ProcessedData();
                        //    processedData.SetMemoryPara("MFDJC0_MeasureResult", _measureResult);
                        //    Stopwatch stopwatch2 = Stopwatch.StartNew();
                        //    processedData.Gray = gray.Clone();
                        //    stopwatch2.Stop();
                        //    Console.WriteLine($"CvDrawResult运行时间：{stopwatch2.Elapsed.TotalMilliseconds} 毫秒");

                        //    //通知数据更新
                        //    PrismProvider.EventAggregator.GetEvent<SensorTransferData>().Publish(processedData);

                        //    PrismProvider.Dispatcher.BeginInvoke(() =>
                        //    {
                        //        loading.Close();
                        //    });
                        //});


                        //KBTDispensing_MeasureResult measureResult = KBTCustomAlgo.GetMeasureResult();
                        //ProcessedData processedData = new ProcessedData();
                        //processedData.SetMemoryPara("MFDJC0_MeasureResult", measureResult);

                        ////KBTCustomAlgo.CvDrawResult(measureResult, true);

                        ////通知数据更新
                        //PrismProvider.EventAggregator.GetEvent<SensorTransferData>().Publish(processedData);

                        //KBTCustomAlgo.Dispose();
                    }
                    break;

                default:
                    break;
            }
        });

        /// <summary>
        /// 数值变更指令，保持和海伯森页面一致的曝光写入逻辑。
        /// </summary>
        public DelegateCommand<string> ValueChangedCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "曝光时间":
                    {
                        if (HypersenSensor == null)
                            return;

                        Console.WriteLine("设置曝光时间为:" + Model.ExposureTime.ToString());
                        if (!HypersenSensor.SetSingleParam("int", LCF_ParameterDefine.PARAM_EXPOSURE_TIME, Model.ExposureTime))
                            return;

                        HypersenSensor.CmosConfig.ExposureTime = Model.ExposureTime;
                        HypersenSensor.Config.ExposureTime = Model.ExposureTime;
                    }
                    break;
                case "循环次数":
                    {
                        if (StartTheCycleModel == null)
                            return;
                    }
                    break;
                default:
                    break;
            }
        });

        public DelegateCommand LimitValueChangedCommand => new DelegateCommand(() =>
        {
            if (PrismProvider.ProjectManager?.SltCurSolutionItem?.NodeParamCaches == null)
                return;

            string cacheKey = Serial.ToString("D3");
            if (PrismProvider.ProjectManager.SltCurSolutionItem.NodeParamCaches.TryGetValue(cacheKey, out var param)
                && param is SensorDataCollectionModel sensorModel)
            {
                sensorModel.SyncMeasureParamByLimit();
            }
        });

        /// <summary>
        /// 从当前方案里找到 StartTheCycle 节点对应的循环模型。
        /// </summary>
        private LoopModel? ResolveStartTheCycleModel()
        {
            var solutionItem = PrismProvider.ProjectManager?.SltCurSolutionItem;
            if (solutionItem == null)
                return null;

            // 优先通过节点标题精确定位，避免方案里有多个循环模块时拿错对象。
            if (solutionItem.NodeCaches is IEnumerable nodeCaches)
            {
                foreach (var node in nodeCaches)
                {
                    object menuInfo = node?.GetType().GetProperty("MenuInfo")?.GetValue(node);
                    if (menuInfo == null)
                        continue;

                    string title = menuInfo.GetType().GetProperty("Title")?.GetValue(menuInfo) as string ?? string.Empty;
                    if (!string.Equals(title, "StartTheCycle", StringComparison.Ordinal))
                        continue;

                    object serialValue = menuInfo.GetType().GetProperty("Serial")?.GetValue(menuInfo);
                    if (serialValue is not int serial)
                        continue;

                    string cacheKey = serial.ToString("D3");
                    if (solutionItem.NodeParamCaches.TryGetValue(cacheKey, out var param) && param is LoopModel loopModel)
                        return loopModel;
                }
            }

            var fallbackLoopModel = solutionItem.NodeParamCaches.Values.OfType<LoopModel>().FirstOrDefault();
            if (fallbackLoopModel != null)
                return fallbackLoopModel;
            return null;
        }

        /// <summary>
        /// 配方切换后刷新当前页面绑定的模型，保证检测方式等嵌套参数同步显示。
        /// </summary>
        private void RefreshCurrentNodeParam()
        {
            string cacheKey = Serial.ToString("D3");
            if (PrismProvider.ProjectManager?.SltCurSolutionItem?.NodeParamCaches?.TryGetValue(cacheKey, out var param) != true ||
                param is not SensorDataCollectionModel temp)
                return;

            temp.LoadKeyParam(); // 页面显示前先按当前配方刷新模型参数
            Model = temp.OtherConfig;
            MeasureParam = temp.MeasureParam;
        }

        private void RecipeSwitchedRefreshOtherConfig(string order)
        {
            if (order != "RecipeSwitched") return;

            PrismProvider.Dispatcher.BeginInvoke(() =>
            {
                RefreshCurrentNodeParam();
            });
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            var parameters = navigationContext.Parameters;

            Serial = -999;
            if (parameters.TryGetValue<int>("Serial", out var id))
                Serial = id;

            if (_recipeSwitchedToken == null)
            {
                _recipeSwitchedToken = PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>()
                    .Subscribe(RecipeSwitchedRefreshOtherConfig, Prism.Events.ThreadOption.PublisherThread);
            }

            var temp = PrismProvider.ProjectManager.SltCurSolutionItem.NodeParamCaches[$"{Serial.ToString("D3")}"] as SensorDataCollectionModel;
            if (temp == null)
                return;

            Model = temp.OtherConfig;
            MeasureParam = temp.MeasureParam;
            HypersenSensor = (HypersenSensor)temp.Models.Where(c => c.NickName == temp.SltModelName).FirstOrDefault();
            if (HypersenSensor != null)
            {
                Model.ExposureTime = HypersenSensor.CmosConfig.ExposureTime;
            }
            StartTheCycleModel = ResolveStartTheCycleModel();
            //HypersenSensor = temp.SltModel as HypersenSensor;
            //PrismProvider.EventAggregator?.GetEvent<UpdateMessageEvent>()?.Publish("RecipeSwitched");
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return false;

        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            if (HypersenSensor?.CmosConfig != null)
                HypersenSensor.CmosConfig.PropertyChanged -= HypersenCmosConfig_PropertyChanged;

            if (_recipeSwitchedToken != null)
            {
                PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Unsubscribe(_recipeSwitchedToken);
                _recipeSwitchedToken = null;
            }
        }
        #endregion
    }
}
