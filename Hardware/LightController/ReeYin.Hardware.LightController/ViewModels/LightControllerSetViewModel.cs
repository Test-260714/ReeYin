using ReeYin.Hardware.LightController.Models;
using ReeYin.Hardware.LightController.Views;
using ReeYin_V.Core.Base;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Models;
using ReeYin_V.Core.Services.Module;
using ReeYin_V.UI;
using ReeYin_V.UI.Style.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ReeYin.Hardware.LightController.ViewModels
{
    /// <summary>
    /// 通信类型
    /// </summary>
    public enum LightComType
    {
        None,
        网口,
        串口,
    }

    public class LightControllerSetViewModel : DialogViewModelBase
    {
        #region Fields
        private IConfigManager ConfigManager { get; }
        #endregion

        #region Properties
        private LightComType _comType = LightComType.网口;
        /// <summary>
        /// 通信类型
        /// </summary>
        public LightComType ComType
        {
            get { return _comType; }
            set { _comType = value; RaisePropertyChanged(); }
        }

        private LightControllerSetModel _modelParam = new LightControllerSetModel();

        public LightControllerSetModel ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<string> _lightControllerTypes = new ObservableCollection<string>();
        /// <summary>
        /// 所有光源控制器类型
        /// </summary>
        public ObservableCollection<string> LightControllerTypes
        {
            get { return _lightControllerTypes; }
            set { _lightControllerTypes = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Constructor
        public LightControllerSetViewModel(IConfigManager configManager)
        {
            ConfigManager = configManager;
            var Modules = PrismProvider.ModuleManager.Classified["LightController"].ToList();

            LightControllerTypes.AddRange(Modules);
            InitParam();
        }
        #endregion

        #region Override
        public override void InitParam()
        {
            ModelParam = PrismProvider.HardwareModuleManager.Modules[ConfigKey.LightControllerConfig] as LightControllerSetModel ?? new LightControllerSetModel();
            if (ModelParam.Models.Count > 0)
                ModelParam.CurSlt = ModelParam.Models[0];
        }
        #endregion

        #region Command
        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
        });

        /// <summary>
        /// 通用指令
        /// </summary>
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "保存":
                    {
                        MessageBoxResult result = MessageBox.Show("确定要保存当前参数吗?", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.No)
                        {
                            return;
                        }
                        ConfigManager.Write(ConfigKey.LightControllerConfig, ModelParam);
                    }
                    break;

                case "确认":
                    {
                        ConfigManager.Write(ConfigKey.LightControllerConfig, ModelParam);
                        CloseDialog(ButtonResult.OK, new DialogParameters()
                        {
                            { "Param", ModelParam },
                        });
                    }
                    break;
                case "取消":
                    CloseDialog(ButtonResult.No);
                    break;
                case "连接":
                    {
                        if (ModelParam.CurSlt == null) return;
                        // 设置连接类型
                        ModelParam.CurSlt.ConnectionType = ComType == LightComType.网口 ? 0 : 1;
                        if (!ModelParam.CurSlt.Init())
                        {
                            MessageView.Ins.MessageBoxShow("尝试连接光源控制器失败，请检查硬件连接！", eMsgType.Warn);
                        }
                        else
                        {
                            MessageView.Ins.MessageBoxShow("光源控制器连接成功！", eMsgType.Info);
                        }
                    }
                    break;
                case "断开":
                    {
                        if (ModelParam.CurSlt == null) return;
                        ModelParam.CurSlt.Close();
                    }
                    break;
                case "打开调试面板":
                    {
                        if (ModelParam.CurSlt == null) return;
                        // 根据厂家类型打开对应的调试面板
                        string dialogName = GetDialogName(ModelParam.CurSlt.VenderName);
                        if (!string.IsNullOrEmpty(dialogName))
                        {
                            PrismProvider.DialogService.ShowDialog(dialogName, new DialogParameters
                            {
                                { "Title", "光源控制器设置" },
                                { "Icon", "\ue6a2" },
                                { "Param", ModelParam.CurSlt },
                            }, result =>
                            {
                                if (result.Result == ButtonResult.OK)
                                {
                                    ModelParam.CurSlt = result.Parameters.GetValue<object>("Param") as LightControllerBase;
                                }
                            }, nameof(DialogWindowView));
                        }
                    }
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
                        if (ModelParam.SltVendorType == null || ModelParam.SltVendorType == "") return;

                        string registrationName = GetLightControllerRegistrationName(ModelParam.SltVendorType);
                        if (string.IsNullOrEmpty(registrationName))
                        {
                            MessageView.Ins.MessageBoxShow($"未知的光源控制器类型: {ModelParam.SltVendorType}", eMsgType.Warn);
                            return;
                        }

                        var module = PrismProvider.Container.Resolve<ILightController>(registrationName) as LightControllerBase;
                        module.VenderName = ModelParam.SltVendorType;
                        module.VenderType = "";
                        ModelParam.Models.Add(module);
                        if (ModelParam.Models.Count > 0)
                            ModelParam.CurSlt = ModelParam.Models[ModelParam.Models.Count - 1];
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"LightControllerSetViewModel_DataOperateCommand错误：{ex.StackTrace}");
                    }
                    break;
                case "Del":
                    {
                        if (ModelParam.CurSlt == null) return;
                        ModelParam.CurSlt.Close();
                        ModelParam.Models.Remove(ModelParam.CurSlt);
                    }
                    break;
                case "Modify":
                    break;
                default:
                    break;
            }
        });
        #endregion

        #region Method
        /// <summary>
        /// 根据VendorType获取对应的光源控制器注册名
        /// </summary>
        private string GetLightControllerRegistrationName(string vendorType)
        {
            return vendorType switch
            {
                "CST" => "CSTLightController",  // CST光源控制器
                "Rsee" => "RseeLightController",
                _ => null
            };
        }

        /// <summary>
        /// 根据厂家名称获取对话框名称
        /// </summary>
        private string GetDialogName(string venderName)
        {
            return venderName switch
            {
                "CST" => "CSTLightControllerView",
                "Rsee" => "RseeLightControllerView",
                _ => null
            };
        }
        #endregion
    }
}
