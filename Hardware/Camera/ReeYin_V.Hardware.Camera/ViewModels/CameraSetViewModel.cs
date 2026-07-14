using HalconDotNet;
using ImageTool.Halcon;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Models;
using ReeYin_V.Core.Services.Module;
using ReeYin_V.Hardware.Camera.Models;
using ReeYin_V.UI;
using ReeYin_V.UI.Style.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using ComboBox = System.Windows.Controls.ComboBox;

namespace ReeYin_V.Hardware.Camera.ViewModels
{
    public class CameraSetViewModel : DialogViewModelBase
    {
        #region Fields
        //public static VMHWindowControl mWindowH = new VMHWindowControl();

        private IConfigManager ConfigManager { get; }
        #endregion

        #region Properties
        private CameraSetModel _modelParam = new CameraSetModel();

        public CameraSetModel ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        [NonSerialized]
        public Timer Timer_ContinuousAcq;

        private CameraBase _SelectedCameraModel;

        public CameraBase SelectedCameraModel
        {
            get { return _SelectedCameraModel; }
            set { _SelectedCameraModel = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<string> _CameraTypes = new ObservableCollection<string>();

        public ObservableCollection<string> CameraTypes
        {
            get { return _CameraTypes; }
            set { _CameraTypes = value; RaisePropertyChanged(); }
        }



        private HImage _image;

        public HImage Image
        {
            get { return _image; }
            set { _image = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor
        public CameraSetViewModel(IConfigManager configManager)
        {
            ConfigManager = configManager;
            Timer_ContinuousAcq = new Timer();
            Timer_ContinuousAcq.Interval = 100;
            Timer_ContinuousAcq.Tick += ContinuousAcqMethod;
        }
        #endregion

        #region Override
        public override void InitParam()
        {
            ModelParam = PrismProvider.HardwareModuleManager.Modules[ConfigKey.CamConfig] as CameraSetModel?? new CameraSetModel();
            //默认选中第一个相机
            SelectedCameraModel = ModelParam.CameraModels.Count > 0 ? ModelParam.CameraModels[0] : null;
        }
        #endregion

        #region Command
        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            //等待加载完成赋值
            var Modules = PrismProvider.ModuleManager.Classified["Camera"].ToList();

            CameraTypes.AddRange(Modules);
        });

        /// <summary>
        /// 通用指令
        /// </summary>
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "确认":
                    {
                        if (Timer_ContinuousAcq.Enabled)
                        {
                            SelectedCameraModel.EndCollect();
                            Timer_ContinuousAcq.Stop(); return;
                        }
                        PrismProvider.HardwareModuleManager.Modules[ConfigKey.CamConfig] = ModelParam;
                        ConfigManager.Write(ConfigKey.CamConfig, ModelParam);
                        CloseDialog(ButtonResult.OK, new DialogParameters()
                        {
                            { "Param", ModelParam },
                        });
                    }
                    break;
                case "取消":
                    CloseDialog(ButtonResult.No);

                    break;

                case "配置":
                    if(SelectedCameraModel == null)
                    {
                        MessageBox.Show("请选中一个实例");
                        return;
                    }
                    PrismProvider.DialogService.Show("CamConfigView", new DialogParameters
                    {
                         { "Title", "相机配置" },
                         { "Icon", "\ue6e8" },
                         { "Param", SelectedCameraModel },
                    }, result =>
                    {
                        if (result.Result == ButtonResult.OK)
                        {

                        }
                    }, nameof(DialogWindowView));

                    break;

                default:
                    break;
            }

        });

        public DelegateCommand<object> DataOperateCommand => new DelegateCommand<object>((obj) =>
        {
            switch (obj?.ToString())
            {
                case "Add":
                    try
                    {
                        if (ModelParam.CameraNo == null) return;
                        int index = ModelParam.CameraModels.ToList().FindIndex(c => c.SerialNo == ModelParam.CameraNo.CamName);
                        if (index >= 0)
                        {
                            MessageView.Ins.MessageBoxShow("该设备已经添加列表!");
                            return;
                        }
                        ////根据选中的插件 new一个 模块
                        //PluginsInfo m_PluginsInfo = PluginService.PluginDic_Camera[SelectedCameraType];
                        //CameraBase module = (CameraBase)Activator.CreateInstance(m_PluginsInfo.ModuleType);
                        //根据选中名称创建新实例
                        var module = PrismProvider.Container.Resolve<ICamera>(ModelParam.SelectedCameraType) as CameraBase;

                        //确定新模块的不重名名称
                        if (ModelParam.CameraModels != null)
                        {
                            if (ModelParam.CameraModels.Count > 0)
                            {
                                List<string> nameList = ModelParam.CameraModels.Select(c => c.CameraNo).ToList();
                                while (true)
                                {
                                    if (!nameList.Contains("Dev" + CameraBase.LastNo))
                                    {
                                        break;//没有重名就跳出循环
                                    }
                                    CameraBase.LastNo++;
                                }
                            }
                            else
                            {
                                CameraBase.LastNo++;
                            }
                        }
                        module.CameraNo = "Dev" + CameraBase.LastNo;
                        //PluginsInfo m_PluginsInfo = PluginService.PluginDic_Camera[SelectedCameraType];
                        //var cameraInfo = CameraSetView.Ins.cmbCameraNo.SelectedItem as CameraInfoModel;
                        if (ModelParam.CameraNo != null)
                        {
                            module.SerialNo = ModelParam.CameraNo.SerialNO;
                            module.CameraType = ModelParam.SelectedCameraType;
                            module.Remarks = $"";
                            module.ExtInfo = ModelParam.CameraNo.ExtInfo;
                            ModelParam.CameraModels.Add(module);

                            //PrismProvider.EventAggregator.GetEvent<AddCameraEvent>().Publish(new AddCameraEventParamModel() { Camera = module, OperateType = eOperateType.Add });
                        }
                    }
                    catch (Exception ex)
                    {
                        //Logger.GetExceptionMsg(ex);
                    }
                    break;
                case "Delete":
                    if (SelectedCameraModel == null) return;
                    ModelParam.CameraModels.Remove(SelectedCameraModel);
                    break;
                case "Modify":
                    break;

                default:
                    break;
            }
            ModelParam.SelectedIndex = ModelParam.CameraModels.Count - 1;
            PrismProvider.EventAggregator.GetEvent<HardwareChangedEvent>().Publish();
        });
        
        public DelegateCommand<object> ButtonOperateCommand=> new DelegateCommand<object>((obj) =>
        {
            switch (obj?.ToString())
            {
                case "Connect":
                    if (SelectedCameraModel == null) return;
                    SelectedCameraModel.ConnectDev();
                    // 连接成功后，读取相机当前曝光/增益到 Config（仅当值仍为默认 0 时回填）
                    if (SelectedCameraModel.Connected)
                    {
                        var cfg = SelectedCameraModel.Config; // getter 自动创建
                        try
                        {
                            object val = 0f;
                            if (cfg.ExposeTime == 0 && SelectedCameraModel.GetSpecifiedParam("Float", "ExposureTime", ref val))
                                cfg.ExposeTime = Convert.ToSingle(val);
                            val = 0f;
                            if (cfg.Gain == 0 && SelectedCameraModel.GetSpecifiedParam("Float", "Gain", ref val))
                                cfg.Gain = Convert.ToSingle(val);
                        }
                        catch { /* 读取失败不影响连接 */ }
                    }
                    break;
                case "Disconnect":
                    if (SelectedCameraModel == null) return;
                    SelectedCameraModel.DisConnectDev();
                    break;
                case "SingleAcq":
                    if (SelectedCameraModel == null || !SelectedCameraModel.Connected) return;
                        Task.Run(() =>
                        {
                            //设为软件触发，采集一张

                            SelectedCameraModel.EventWait.Reset();
                            SelectedCameraModel.StartCollect();
                            //SelectedCameraModel.CaptureImage(true);
                            SelectedCameraModel.EventWait.WaitOne();
                            SelectedCameraModel.EndCollect();
                            lock (SelectedCameraModel._frameLock)
                            {
                                SelectedCameraModel.Image = SelectedCameraModel._frameBuffer.GetHalconImage_Mono8(); 
                            }

                            if (SelectedCameraModel.Image != null && SelectedCameraModel.Image.IsInitialized())
                            {
                                try
                                {
                                    lock (SelectedCameraModel._frameLock)
                                    {
                                        Image = new HImage(SelectedCameraModel.Image);
                                    }
                                }
                                catch { }
                            }
                        });
                    break;
                case "ContinuousAcq":
                    {
                        if (SelectedCameraModel == null) return;

                        if (Timer_ContinuousAcq.Enabled)
                        {
                            SelectedCameraModel.EndCollect();
                            Timer_ContinuousAcq.Stop(); return;
                        }
                        else
                        {
                            SelectedCameraModel.StartCollect();
                            Timer_ContinuousAcq.Start(); return;
                        }
                    }
                default:
                    break;
            }
        });
        
        public DelegateCommand<object> CameraTypesChangedCommand => new DelegateCommand<object>((obj) =>
        {
            var cmbCameraType = obj as ComboBox;

            if (cmbCameraType.SelectedIndex < 0 || cmbCameraType.SelectedItem == null) return;
            //PluginsInfo pluginsInfo = PluginService.PluginDic_Camera[cmbCameraType.SelectedItem.ToString()];
            //根据选中名称创建新实例
            var module = PrismProvider.Container.Resolve<ICamera>(cmbCameraType.SelectedItem.ToString()) as CameraBase;
            //CameraBase module = (CameraBase)Activator.CreateInstance(pluginsInfo.ModuleType);
            ModelParam.CameraNos = module.SearchCameras().ToObservableCollection();
        });

        public DelegateCommand ApplyParamsCommand => new DelegateCommand(() =>
        {
            if (SelectedCameraModel == null) return;
            if (!SelectedCameraModel.Connected)
            {
                MessageView.Ins.MessageBoxShow("请先连接相机再应用参数。");
                return;
            }

            var cfg = SelectedCameraModel.Config;
            var results = new List<string>();

            // 应用曝光
            if (cfg.ExposeTime > 0)
            {
                bool ok = SelectedCameraModel.SetSpecifiedParam("Float", "ExposureTime", cfg.ExposeTime);
                results.Add($"曝光 = {cfg.ExposeTime}  {(ok ? "✓ 成功" : "✗ 失败")}");
            }
            else
            {
                // 值为0，尝试从相机读取当前值
                object val = 0f;
                if (SelectedCameraModel.GetSpecifiedParam("Float", "ExposureTime", ref val))
                {
                    cfg.ExposeTime = Convert.ToSingle(val);
                    results.Add($"曝光 = {cfg.ExposeTime}  ✓ 已从相机读取当前值");
                }
                else
                {
                    results.Add("曝光 = 0  ⚠ 请先设置有效曝光值（>0）");
                }
            }

            // 应用增益
            if (cfg.Gain >= 0 && cfg.Gain != 0)
            {
                bool ok = SelectedCameraModel.SetSpecifiedParam("Float", "Gain", cfg.Gain);
                results.Add($"增益 = {cfg.Gain}  {(ok ? "✓ 成功" : "✗ 失败")}");
            }
            else
            {
                // 值为0，尝试从相机读取当前值
                object val = 0f;
                if (SelectedCameraModel.GetSpecifiedParam("Float", "Gain", ref val))
                {
                    cfg.Gain = Convert.ToSingle(val);
                    results.Add($"增益 = {cfg.Gain}  ✓ 已从相机读取当前值");
                }
                else
                {
                    results.Add("增益 = 0  ⚠ 请先设置有效增益值");
                }
            }

            // 线扫：应用拼帧行数（仅保存到 Config，下次拼帧生效）
            if (SelectedCameraModel.IsLineScan)
            {
                results.Add($"拼帧行数 = {cfg.LineScanFrameHeight}  ✓ 已保存（下次拼帧生效）");
            }

            MessageView.Ins.MessageBoxShow(string.Join("\n", results));
        });

        public DelegateCommand<object> SelectedCellsChanged => new DelegateCommand<object>((obj) =>
        {
            if (SelectedCameraModel == null) return;
        });

        #endregion

        #region Method
        private void ContinuousAcqMethod(object sender, EventArgs e)
        {
            if (SelectedCameraModel == null) return;

            SelectedCameraModel.CaptureImage(false);
            
            try
            {
                lock (SelectedCameraModel._frameLock)
                {
                    if (SelectedCameraModel.Image != null && SelectedCameraModel.Image.IsInitialized())
                    {
                        Image = new HImage(SelectedCameraModel.Image);
                    }
                }
            }
            catch { }
        }

        [OnDeserialized()]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            Timer_ContinuousAcq = new Timer();
            Timer_ContinuousAcq.Interval = 100;
            Timer_ContinuousAcq.Tick += ContinuousAcqMethod;
        }
        #endregion

    }
}
