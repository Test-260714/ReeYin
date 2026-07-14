using ReeYin_V.Core.Config;
using Newtonsoft.Json;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Module;
using ReeYin_V.Hardware.PLC.Interface;
using ReeYin_V.Hardware.PLC.Models;
using ReeYin_V.UI;
using ReeYin_V.UI.Style.Dialogs;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using PLCType = ReeYin_V.Hardware.PLC.Models.PLCType;

namespace ReeYin_V.Hardware.PLC.ViewModels
{
    public class PLCSetViewModel : DialogViewModelBase
    {
        #region Fields
        private IConfigManager ConfigManager { get; }
        #endregion

        #region Properties
        private PLCSetModel _modelParam;
        /// <summary>
        /// 参数
        /// </summary>
        public new PLCSetModel ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Constructor
        public PLCSetViewModel(IConfigManager configManager)
        {
            ConfigManager = configManager;
            InitParam();
        }
        #endregion

        #region Overrides
        public override void InitParam()
        {
            //ModelParam = ConfigManager.Read<PLCSetModel>(ConfigKey.PLCConfig) ?? new PLCSetModel();
            ModelParam = PrismProvider.HardwareModuleManager.Modules[ConfigKey.PLCConfig] as PLCSetModel ?? new PLCSetModel();
            ModelParam.NormalizeSelections();
        }
        #endregion

        #region Commands
        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            //等待加载完成赋值

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

                        ModelParam.NormalizeSelections();
                        JsonHelper.JsonObjectSerialize(ModelParam, GetDefaultConfigPath(), TypeNameHandling.All);
                    }
                    break;
                case "ImportConfig":
                    ImportConfig();
                    break;
                case "ExportConfig":
                    ExportConfig();
                    break;
                case "取消":

                    break;

                case "连接":
                    {
                        if (ModelParam.CurSlt == null)
                        {
                            MessageView.Ins.MessageBoxShow("请先选择一个PLC设备！", eMsgType.Error);
                            return;
                        }

                        try
                        {
                            //先断开
                            ModelParam.CurSlt.Close();

                            if (!ModelParam.CurSlt.Init())
                            {
                                MessageView.Ins.MessageBoxShow($"尝试连接{ModelParam.CurSlt.Config.DisplayName}失败!!!", eMsgType.Error);
                            }
                            else
                            {
                                MessageView.Ins.MessageBoxShow($"{ModelParam.CurSlt.Config.DisplayName}连接成功!", eMsgType.Success);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageView.Ins.MessageBoxShow($"连接{ModelParam.CurSlt.Config.DisplayName}时发生异常: {ex.Message}", eMsgType.Error);
                        }
                    }
                    break;

                case "断开":
                    {
                        if (ModelParam.CurSlt == null)
                        {
                            MessageView.Ins.MessageBoxShow("请先选择一个PLC设备！", eMsgType.Error);
                            return;
                        }

                        try
                        {
                            if (ModelParam.CurSlt.Close())
                            {
                                MessageView.Ins.MessageBoxShow($"{ModelParam.CurSlt.Config.DisplayName}已断开连接", eMsgType.Success);
                            }
                            else
                            {
                                MessageView.Ins.MessageBoxShow($"断开{ModelParam.CurSlt.Config.DisplayName}失败", eMsgType.Error);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageView.Ins.MessageBoxShow($"断开{ModelParam.CurSlt.Config.DisplayName}时发生异常: {ex.Message}", eMsgType.Error);
                        }
                    }
                    break;

                case "测试断电":
                    {
                        MessageBoxResult result = MessageBox.Show("确定要执行此操作吗? 确认后设备将断电", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.No)
                        {
                            return;
                        }

                        var Param = new PLCParaInfoModel
                        {
                            PLCAddress = ModelParam.PowerOff.Addr,
                            ParaType = ModelParam.PowerOff.ParamType,
                            ParaValue = ModelParam.PowerOff.Value
                        };
                        ModelParam.CurSlt.WritePLCPara(Param);

                    }
                    break;

                case "写入延时断电":
                    {
                        var Param = new PLCParaInfoModel
                        {
                            PLCAddress = ModelParam.DelayPowerOff.Addr,
                            ParaType = ModelParam.DelayPowerOff.ParamType,
                            ParaValue = ModelParam.DelayPowerOff.Value
                        };
                        ModelParam.CurSlt.WritePLCPara(Param);

                    }
                    break;

                case "添加新项":
                    AddAddressItem();
                    break;
                case "删除选中项":
                    RemoveAddressItem();
                    break;


                case "运动轴操作面板":
                case "PLC运动调试":
                    {
                        PrismProvider.EventAggregator.GetEvent<OpenSingleWindowEvent>().Publish("PLCAxisGroupMoveView");
                    }
                    break;

                case "写入速度档":
                    {
                        if (ModelParam.CurSlt == null || !ModelParam.CurSlt.Config.IsConnected)
                        {
                            MessageView.Ins.MessageBoxShow("请先连接PLC！", eMsgType.Error);
                            return;
                        }
                        if (ModelParam.SltAxisItem == null)
                        {
                            MessageView.Ins.MessageBoxShow("请先选择一个轴！", eMsgType.Error);
                            return;
                        }
                        if (ModelParam.SltSpeedProfile == null)
                        {
                            MessageView.Ins.MessageBoxShow("请先选择一个速度档！", eMsgType.Error);
                            return;
                        }
                        bool writeResult = ModelParam.CurSlt.MotionService.WriteSpeedProfile(ModelParam.SltAxisItem, ModelParam.SltSpeedProfile);
                        MessageView.Ins.MessageBoxShow(writeResult ? "速度档写入成功！" : "速度档写入失败！", writeResult ? eMsgType.Success : eMsgType.Error);
                    }
                    break;

                case "IO操作面板":
                    {
                        PrismProvider.EventAggregator.GetEvent<OpenSingleWindowEvent>().Publish($"IOManagerView");
                    }
                    break;

                case "添加轴项":
                    {
                        if (ModelParam.CurSlt == null || ModelParam.SltAxisGroup == null)
                        {
                            MessageView.Ins.MessageBoxShow("请先选择一个PLC设备！", eMsgType.Error);
                            return;
                        }
                        ModelParam.SltAxisGroup.AxisItems.Add(new PLCAxisItem());
                    }
                    break;

                case "删除轴项":
                    {
                        if (ModelParam.SltAxisItem == null)
                        {
                            MessageView.Ins.MessageBoxShow("请先选择要删除的轴项！", eMsgType.Error);
                            return;
                        }
                        ModelParam.SltAxisGroup.AxisItems.Remove(ModelParam.SltAxisItem);
                        ModelParam.SltAxisItem = null;
                    }
                    break;

                case "添加轴组":
                    {
                        if (ModelParam.CurSlt == null)
                        {
                            MessageView.Ins.MessageBoxShow("请先选择一个PLC设备！", eMsgType.Error);
                            return;
                        }

                        var group = new PLCAxisGroup
                        {
                            GroupName = $"轴组{ModelParam.CurSlt.AxisGroups.Count + 1}"
                        };
                        ModelParam.CurSlt.AxisGroups.Add(group);
                        ModelParam.SltAxisGroup = group;
                    }
                    break;

                case "删除轴组":
                    {
                        if (ModelParam.CurSlt == null || ModelParam.SltAxisGroup == null)
                        {
                            MessageView.Ins.MessageBoxShow("请先选择要删除的轴组！", eMsgType.Error);
                            return;
                        }

                        if (ModelParam.CurSlt.AxisGroups.Count == 1)
                        {
                            MessageView.Ins.MessageBoxShow("至少保留一个轴组，别一把全薅没了。", eMsgType.Warn);
                            return;
                        }

                        ModelParam.CurSlt.AxisGroups.Remove(ModelParam.SltAxisGroup);
                        ModelParam.SltAxisGroup = ModelParam.CurSlt.AxisGroups.FirstOrDefault();
                        ModelParam.SltAxisItem = null;
                    }
                    break;

                case "启动心跳监控":
                    {
                        if (ModelParam.CurSlt == null)
                        {
                            MessageView.Ins.MessageBoxShow("请先选择一个PLC设备！", eMsgType.Error);
                            return;
                        }
                        if (string.IsNullOrWhiteSpace(ModelParam.CurSlt.Heartbeat.Address))
                        {
                            MessageView.Ins.MessageBoxShow("请先配置心跳地址！", eMsgType.Error);
                            return;
                        }
                        if (!ModelParam.CurSlt.Config.IsConnected)
                        {
                            MessageView.Ins.MessageBoxShow("请先连接PLC！", eMsgType.Error);
                            return;
                        }
                        ModelParam.CurSlt.StartHeartbeat();
                        MessageView.Ins.MessageBoxShow("心跳监控已启动", eMsgType.Success);
                    }
                    break;

                case "停止心跳监控":
                    {
                        if (ModelParam.CurSlt == null)
                        {
                            MessageView.Ins.MessageBoxShow("请先选择一个PLC设备！", eMsgType.Error);
                            return;
                        }
                        ModelParam.CurSlt.StopHeartbeat();
                        MessageView.Ins.MessageBoxShow("心跳监控已停止", eMsgType.Success);
                    }
                    break;

                case "启动状态监听":
                    {
                        if (ModelParam.CurSlt == null || !ModelParam.CurSlt.Config.IsConnected)
                        {
                            MessageView.Ins.MessageBoxShow("请先连接PLC！", eMsgType.Error);
                            return;
                        }

                        ModelParam.CurSlt.StartMotionMonitor();
                        MessageView.Ins.MessageBoxShow("PLC状态监听已启动", eMsgType.Success);
                    }
                    break;

                case "停止状态监听":
                    {
                        if (ModelParam.CurSlt == null)
                        {
                            MessageView.Ins.MessageBoxShow("请先选择一个PLC设备！", eMsgType.Error);
                            return;
                        }

                        ModelParam.CurSlt.StopMotionMonitor();
                        MessageView.Ins.MessageBoxShow("PLC状态监听已停止", eMsgType.Success);
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
                        if (ModelParam.SltType == PLCType.None)
                        {
                            MessageView.Ins.MessageBoxShow("请选择有效PLC类型！", eMsgType.Error);
                            return;
                        }
                        //根据选中名称创建新实例
                        var module = new PLCBase();
                        module.Config.Ip = ModelParam.IP;
                        module.Config.Port = ModelParam.Port;
                        module.Config.PlcType = ModelParam.SltType;
                        module.Config.DisplayName = $"PLC{ModelParam.Models.Count + 1}";

                        ModelParam.Models.Add(module);
                        if (ModelParam.Models.Count > 0)
                            ModelParam.CurSlt = module;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ControlCardConfigViewModel_DataOperateCommand错误：{ex.StackTrace}");
                    }
                    break;
                case "Del":
                    {
                        if (ModelParam.CurSlt == null)
                        {
                            return;
                        }

                        ModelParam.CurSlt.Close();
                        ModelParam.Models.Remove(ModelParam.CurSlt);
                        ModelParam.CurSlt = ModelParam.Models.FirstOrDefault();
                    }
                    break;
                case "Modify":


                    break;
                default:
                    break;
            }
        });
        #endregion

        #region Methods
        private void AddAddressItem()
        {
            ModelParam.AllAddr ??= new System.Collections.ObjectModel.ObservableCollection<AddressMappingItem>();
            var item = new AddressMappingItem
            {
                Description = "新地址",
                DataType = EnumParaInfoModelParaType.Bool,
                Address = ""
            };
            ModelParam.AllAddr.Add(item);
            ModelParam.SltAddrItem = item;
        }

        private void RemoveAddressItem()
        {
            if (ModelParam.SltAddrItem == null)
            {
                MessageView.Ins.MessageBoxShow("请先选择要删除的地址项！", eMsgType.Error);
                return;
            }

            ModelParam.AllAddr.Remove(ModelParam.SltAddrItem);
            ModelParam.SltAddrItem = null;
        }

        private void ImportConfig()
        {
            var dialog = new OpenFileDialog
            {
                Title = "导入PLC配置",
                Filter = "PLC配置文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                Multiselect = false
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            MessageBoxResult result = MessageBox.Show("导入后会覆盖当前PLC全部参数配置，是否继续?", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.No)
            {
                return;
            }

            try
            {
                var imported = JsonHelper.JsonDisObjectSerialize<PLCSetModel>(dialog.FileName, out _, TypeNameHandling.All);
                if (imported == null)
                {
                    MessageView.Ins.MessageBoxShow("导入失败：配置文件内容为空或格式不正确。", eMsgType.Error);
                    return;
                }

                ModelParam?.Models?.ToList().ForEach(model => model?.Close());
                imported.NormalizeSelections();
                ModelParam = imported;
                PrismProvider.HardwareModuleManager.Modules[ConfigKey.PLCConfig] = ModelParam;
                JsonHelper.JsonObjectSerialize(ModelParam, GetDefaultConfigPath(), TypeNameHandling.All);

                MessageView.Ins.MessageBoxShow("PLC配置导入成功。", eMsgType.Success);
            }
            catch (Exception ex)
            {
                MessageView.Ins.MessageBoxShow($"PLC配置导入失败：{ex.Message}", eMsgType.Error);
            }
        }

        private void ExportConfig()
        {
            var dialog = new SaveFileDialog
            {
                Title = "导出PLC配置",
                Filter = "PLC配置文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                FileName = $"PLCConfig_{DateTime.Now:yyyyMMdd_HHmmss}.json",
                AddExtension = true,
                DefaultExt = ".json"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                ModelParam.NormalizeSelections();
                JsonHelper.JsonObjectSerialize(ModelParam, dialog.FileName, TypeNameHandling.All);
                MessageView.Ins.MessageBoxShow("PLC配置导出成功。", eMsgType.Success);
            }
            catch (Exception ex)
            {
                MessageView.Ins.MessageBoxShow($"PLC配置导出失败：{ex.Message}", eMsgType.Error);
            }
        }

        private static string GetDefaultConfigPath()
        {
            return Path.Combine("Config", $"{ConfigKey.PLCConfig.GetType().FullName}.{ConfigKey.PLCConfig}.json");
        }

        #endregion
    }
}
