﻿﻿﻿﻿using ImageTool.Halcon;
using ReeYin.Hardware.Sensor.Models;
using ReeYin.Hardware.Sensor.Views;
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
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace ReeYin.Hardware.Sensor.ViewModels
{
    /// <summary>
    /// 通信类型
    /// </summary>
    public enum ComType
    {
        None,
        网口,
        串口,
        自定义,
    }

    public class SensorSetViewModel : DialogViewModelBase
    {
        #region Fields
        private IConfigManager ConfigManager { get; }
        #endregion

        #region Properties
        private ComType _comType = ComType.网口;
        /// <summary>
        /// 通信类型
        /// </summary>
        public ComType ComType
        {
            get { return _comType; }
            set { _comType = value; RaisePropertyChanged(); }
        }

        private SensorSetModel _modelParam = new SensorSetModel();

        public new SensorSetModel ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        [NonSerialized]
        public Timer Timer_ContinuousAcq;

        private ObservableCollection<string> _sensorTypes = new ObservableCollection<string>();
        /// <summary>
        /// 所有传感器类型
        /// </summary>
        public ObservableCollection<string> SensorTypes
        {
            get { return _sensorTypes; }
            set { _sensorTypes = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Constructor
        public SensorSetViewModel(IConfigManager configManager)
        {
            ConfigManager = configManager;
            var Modules = PrismProvider.ModuleManager.Classified["Sensor"].ToList();

            SensorTypes.AddRange(Modules);
            InitParam();
        }
        #endregion

        #region Override
        public override void InitParam()
        {
            ModelParam = PrismProvider.HardwareModuleManager.Modules[ConfigKey.SensorConfig] as SensorSetModel ?? new SensorSetModel();
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

                        ConfigManager.Write(ConfigKey.SensorConfig, ModelParam);
                    }
                    break;

                case "确认":
                    {
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
                case "连接":
                    {
                        if (ModelParam.CurSlt == null) return;
                        if (!ModelParam.CurSlt.Init())
                        {
                            MessageView.Ins.MessageBoxShow("尝试连接传感器失败，请检查硬件连接！", eMsgType.Warn);
                        }
                    }
                    break;
                case "断开":
                    {
                        if (ModelParam.CurSlt == null) return;
                        ModelParam.CurSlt.Close();
                        ModelParam.CurSlt.IsConnected = false;
                    }break;
                case "打开调试面板":
                    {
                        if (ModelParam.CurSlt == null) return;
                        if (ModelParam.CurSlt.VenderName == "HPS")
                        {
                            //弹窗初始化页面
                            PrismProvider.DialogService.ShowDialog("HypersonSensorView", new DialogParameters
                            {
                                { "Title", "3D传感器设置" },
                                { "Icon", "\ue6a2" },
                                { "Param", ModelParam.CurSlt },
                            }, result =>
                            {
                                if (result.Result == ButtonResult.OK)
                                {
                                    ModelParam.CurSlt = result.Parameters.GetValue<object>("Param") as SensorBase;

                                }

                            }, nameof(DialogWindowView));

                        }
                        else if(ModelParam.CurSlt.VenderName == "TS")
                        {
                            //弹窗初始化页面
                            PrismProvider.DialogService.ShowDialog("TronSightSensorView", new DialogParameters
                            {
                                { "Title", "3D传感器设置" },
                                { "Icon", "\ue6a2" },
                                { "Param", ModelParam.CurSlt },
                            }, result =>
                            {
                                if (result.Result == ButtonResult.OK)
                                {
                                    ModelParam.CurSlt = result.Parameters.GetValue<object>("Param") as SensorBase;

                                }

                            }, nameof(DialogWindowView));
                        }
                        else if(ModelParam.CurSlt.VenderName == "SSZN")
                        {
                            //弹窗初始化页面 / Open initialization page
                            PrismProvider.DialogService.ShowDialog("SSZNSensorView", new DialogParameters
                            {
                                { "Title", "SSZN线激光传感器设置" },
                                { "Icon", "\ue6a2" },
                                { "Param", ModelParam.CurSlt },
                            }, result =>
                            {
                                if (result.Result == ButtonResult.OK)
                                {
                                    ModelParam.CurSlt = result.Parameters.GetValue<object>("Param") as SensorBase;

                                }

                            }, nameof(DialogWindowView));
                        }
                        else if (ModelParam.CurSlt.VenderName == "Truelight3D")
                        {
                            PrismProvider.DialogService.ShowDialog("Truelight3DSensorView", new DialogParameters
                            {
                                { "Title", "Truelight3D传感器设置" },
                                { "Icon", "\ue6a2" },
                                { "Param", ModelParam.CurSlt },
                            }, result =>
                            {
                                if (result.Result == ButtonResult.OK)
                                {
                                    ModelParam.CurSlt = result.Parameters.GetValue<object>("Param") as SensorBase;
                                }
                            }, nameof(DialogWindowView));
                        }
                        else if (ModelParam.CurSlt.VenderName == "TSInfrared")
                        {
                            //弹窗初始化页面 / Open initialization page
                            PrismProvider.DialogService.ShowDialog("TronSight2SensorView", new DialogParameters
                            {
                                { "Title", "红外传感器" },
                                { "Icon", "\ue6a2" },
                                { "Param", ModelParam.CurSlt },
                            }, result =>
                            {
                                if (result.Result == ButtonResult.OK)
                                {
                                    ModelParam.CurSlt = result.Parameters.GetValue<object>("Param") as SensorBase;

                                }

                            }, nameof(DialogWindowView));
                        }
                        else if (ModelParam.CurSlt.VenderName == "JueXin_Whitelight")
                        {
                            PrismProvider.DialogService.ShowDialog("JueXinSensorView", new DialogParameters
                            {
                                { "Title", "觉芯白光传感器设置" },
                                { "Icon", "\ue6a2" },
                                { "Param", ModelParam.CurSlt },
                            }, result =>
                            {
                                if (result.Result == ButtonResult.OK)
                                {
                                    ModelParam.CurSlt = result.Parameters.GetValue<object>("Param") as SensorBase;
                                }
                            }, nameof(DialogWindowView));
                        }
                        else if (ModelParam.CurSlt.VenderName == "JueXin_LineSpectrum")
                        {
                            PrismProvider.DialogService.ShowDialog("JueXinLineSpectrumSensorView", new DialogParameters
                            {
                                { "Title", "觉芯线光谱传感器设置" },
                                { "Icon", "\ue6a2" },
                                { "Param", ModelParam.CurSlt },
                            }, result =>
                            {
                                if (result.Result == ButtonResult.OK)
                                {
                                    ModelParam.CurSlt = result.Parameters.GetValue<object>("Param") as SensorBase;
                                }
                            }, nameof(DialogWindowView));
                        }
                        else if (ModelParam.CurSlt.VenderName == "JingCe")
                        {
                            PrismProvider.DialogService.ShowDialog("JingCeSensorView", new DialogParameters
                            {
                                { "Title", "武汉精测传感器设置" },
                                { "Icon", "\ue6a2" },
                                { "Param", ModelParam.CurSlt },
                            }, result =>
                            {
                                if (result.Result == ButtonResult.OK)
                                {
                                    ModelParam.CurSlt = result.Parameters.GetValue<object>("Param") as SensorBase;
                                }
                            }, nameof(DialogWindowView));
                        }
                        else if (ModelParam.CurSlt.VenderName == "IKapSpectralConfocal")
                        {
                            PrismProvider.DialogService.ShowDialog("IKapSpectralConfocalSensorView", new DialogParameters
                            {
                                { "Title", "埃科线光谱共焦传感器设置" },
                                { "Icon", "\ue6a2" },
                                { "Param", ModelParam.CurSlt },
                            }, result =>
                            {
                                if (result.Result == ButtonResult.OK)
                                {
                                    ModelParam.CurSlt = result.Parameters.GetValue<object>("Param") as SensorBase;
                                }
                            }, nameof(DialogWindowView));
                        }
                        else if (ModelParam.CurSlt.VenderName == "PRECITEC")
                        {
                            PrismProvider.DialogService.ShowDialog("ChroCodileSensorView", new DialogParameters
                            {
                                { "Title", "普雷茨特传感器设置" },
                                { "Icon", "\ue6a2" },
                                { "Param", ModelParam.CurSlt },
                            }, result =>
                            {
                                if (result.Result == ButtonResult.OK)
                                {
                                    ModelParam.CurSlt = result.Parameters.GetValue<object>("Param") as SensorBase;
                                }
                            }, nameof(DialogWindowView));
                        }
                        else if (ModelParam.CurSlt.VenderName == "MEGAPHASE")
                        {
                            PrismProvider.DialogService.ShowDialog("MegAphaseSensorView", new DialogParameters
                            {
                                { "Title", "MEGAPHASE传感器设置" },
                                { "Icon", "\ue6a2" },
                                { "Param", ModelParam.CurSlt },
                            }, result =>
                            {
                                if (result.Result == ButtonResult.OK)
                                {
                                    ModelParam.CurSlt = result.Parameters.GetValue<object>("Param") as SensorBase;
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

                        // 根据选中的VendorType映射到对应的注册名
                        string sensorRegistrationName = GetSensorRegistrationName(ModelParam.SltVendorType);
                        if (string.IsNullOrEmpty(sensorRegistrationName))
                        {
                            MessageView.Ins.MessageBoxShow($"未知的传感器类型: {ModelParam.SltVendorType}", eMsgType.Warn);
                            return;
                        }

                        //根据选中名称创建新实例
                        var module = PrismProvider.Container.Resolve<ISensor>(sensorRegistrationName) as SensorBase;
                        module.VenderName = ModelParam.SltVendorType;
                        module.VenderType = "";
                        ModelParam.Models.Add(module);
                        if (ModelParam.Models.Count > 0)
                            ModelParam.CurSlt = ModelParam.Models[ModelParam.Models.Count - 1];
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"SensorSetViewModel_DataOperateCommand错误：{ex.StackTrace}");
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
        /// 根据VendorType获取对应的传感器注册名
        /// </summary>
        /// <param name="vendorType">厂商类型标识（如HPS、TS、SSZN）</param>
        /// <returns>传感器在IOC容器中的注册名</returns>
        private string GetSensorRegistrationName(string vendorType)
        {
            // VendorType到注册名的映射
            // HPS -> HypersenSensor (海伯森传感器)
            // TS -> TronSight (创视传感器)
            // SSZN -> SSZNSensor (SSZN线激光传感器)
            // TSInfrared -> TronSight2Sensor (创视2红外传感器)
            // Truelight3D -> Truelight3D (Truelight3D传感器)
            // JueXin_Whitelight -> JueXin_Whitelight (觉芯白光传感器)
            // JueXin_LineSpectrum -> JueXin_LineSpectrum (觉芯线光谱传感器)
            // JingCe => JingCeSensor (觉芯传感器)
            // PRECITEC => ChroCodileSensor(普雷茨特传感器)
            // MEGAPHASE => MegAphaseSensor(MEGAPHASE传感器)
            // IKapSpectralConfocal => IKapSpectralConfocal(埃科线光谱共焦传感器)
            return vendorType switch
            {
                "HPS" => "HypersenSensor",
                "TS" => "TronSight",
                "SSZN" => "SSZNSensor",
                "Truelight3D" => "Truelight3D",
                "TSInfrared" => "TronSight2Sensor",
                "JueXin_Whitelight" => "JueXin_Whitelight",
                "JueXin_LineSpectrum" => "JueXin_LineSpectrum",
                "JingCe" => "JingCeSensor",
                "PRECITEC" => "ChroCodileSensor",
                "MEGAPHASE" => "MegAphaseSensor",
                "IKapSpectralConfocal" => "IKapSpectralConfocal",
                _ => null
            };
        }
        #endregion

    }
}
