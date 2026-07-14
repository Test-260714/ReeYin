using ImageTool.Halcon;
using Newtonsoft.Json;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Models;
using ReeYin_V.Core.Services.Module;
using ReeYin_V.Hardware.ControlCard.Models;
using ReeYin_V.Hardware.ControlCard.Views;
using ReeYin_V.UI;
using ReeYin_V.UI.Style.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ReeYin_V.Core.Services.Project;

namespace ReeYin_V.Hardware.ControlCard.ViewModels
{
    public class ControlCardConfigViewModel : DialogViewModelBase
    {
        #region Fields
        private IConfigManager ConfigManager { get; }
        #endregion

        #region Properties
        private ObservableCollection<string> _venderTypes = new ObservableCollection<string>();
        /// <summary>
        /// 所有轴卡厂家
        /// </summary>
        public ObservableCollection<string> VenderTypes
        {
            get { return _venderTypes; }
            set { _venderTypes = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<string> _cardTypes = new ObservableCollection<string>() 
        { 
            "None",
            "GXN",
            "SPiiPlus"
        };
        /// <summary>
        /// 所有轴卡类型
        /// </summary>
        public ObservableCollection<string> CardTypes
        {
            get { return _cardTypes; }
            set { _cardTypes = value; RaisePropertyChanged(); }
        }

        private ControlCardConfigModel _modelParam;
        /// <summary>
        /// 参数
        /// </summary>
        public ControlCardConfigModel ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 存数据的中间变量
        /// </summary>
        private SingleAxisParam tempSltAxis {  get; set; } = new SingleAxisParam();
        #endregion

        #region Constructor
        public ControlCardConfigViewModel(IConfigManager configManager)
        {
            ConfigManager = configManager;
            InitParam();

        }
        #endregion

        #region Override
        public override void InitParam()
        {
            ModelParam = PrismProvider.HardwareModuleManager.Modules[ConfigKey.ControlCard] as ControlCardConfigModel ?? new ControlCardConfigModel();
            foreach (var card in ModelParam.CardModels.Where(card => card?.Config != null))
            {
                card.Config.EnsureInterpolationCoordinateSystems();
                EnsureVendorSpecificOptions(card);
            }
            if (ModelParam.CardModels.Count > 0)
                ModelParam.CurSltCard = ModelParam.CardModels[0];
        }
        #endregion

        #region Command
        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            //等待加载完成赋值
            VenderTypes = PrismProvider.ModuleManager.Classified["ControlCard"].ToList().ToObservableCollection();

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
                        if (!TryValidateInterpolationCoordinateSystems(out var interpolationMessage))
                        {
                            MessageBox.Show(interpolationMessage, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        if (!TryValidateLimitConfig(out var limitMessage))
                        {
                            MessageBox.Show(limitMessage, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        MessageBoxResult result = MessageBox.Show("确定要保存当前参数吗?", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.No)
                        {
                            return;
                        }

                        ConfigManager.Write(ConfigKey.ControlCard, ModelParam);
                    }
                    break;
                case "取消":

                    break;

                case "添加新项":
                    {
                        ModelParam.CurSltCard.Config.AllAxis.Add(new SingleAxisParam()
                        {
                            SpeedDict1 = SingleAxisParam.CreateDefaultSpeedSettings()
                        });
                    }
                    break;
                case "删除选中项":
                    {
                        ModelParam.CurSltCard.Config.AllAxis.Remove(ModelParam.SltAxis);
                        ModelParam.SltAxis = null;
                    }
                    break;

                case "添加限位":
                    {
                        ModelParam.AllLimitPos.Add(new LimitPos
                        {
                            LimitAxisNum = En_AxisNum.X,
                            ByLimitAxisNum = En_AxisNum.Z,
                            TriggerCondition = LimitTriggerCondition.大于,
                            LimitValue = 70,
                            MinLimitValue = 20,
                            MaxLimitValue = 520,
                            IsUsing = true
                        });
                    }
                    break;

                case "删除选中限位":
                    {
                        ModelParam.AllLimitPos.Remove(ModelParam.SltLimitPos);
                        ModelParam.SltLimitPos = null;
                    }
                    break;

                case "添加插补坐标系":
                    {
                        if (ModelParam?.CurSltCard?.Config == null)
                        {
                            return;
                        }

                        var config = ModelParam.CurSltCard.Config;
                        config.EnsureInterpolationCoordinateSystems();
                        var coordinateSystem = config.CreateDefaultInterpolationCoordinateSystem(false);
                        config.InterpolationCoordinateSystems.Add(coordinateSystem);
                        ModelParam.SltInterPoCoordinateSystem = coordinateSystem;
                    }
                    break;

                case "删除选中插补坐标系":
                    {
                        if (ModelParam?.CurSltCard?.Config?.InterpolationCoordinateSystems == null ||
                            ModelParam.SltInterPoCoordinateSystem == null)
                        {
                            return;
                        }

                        var coordinateSystems = ModelParam.CurSltCard.Config.InterpolationCoordinateSystems;
                        if (coordinateSystems.Count <= 1)
                        {
                            MessageBox.Show("至少需要保留一个插补坐标系。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        coordinateSystems.Remove(ModelParam.SltInterPoCoordinateSystem);
                        ModelParam.SltInterPoCoordinateSystem = coordinateSystems.FirstOrDefault();
                    }
                    break;

                case "单轴参数设置关闭":
                    {
                        //MessageBoxResult result = MessageBox.Show("点击是保存参数?", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        //if (result == MessageBoxResult.No)
                        //{
                        //    ModelParam.SltAxis = tempSltAxis;
                        //    return;
                        //}
                    }
                    break;
                case "单轴参数设置打开":
                    {
                        //tempSltAxis = ModelParam.SltAxis.DeepCopy();
                    }
                    break;

                case "运动轴操作面板":
                    {
                        PrismProvider.EventAggregator.GetEvent<OpenSingleWindowEvent>().Publish($"AxisView");
                    }
                    break;

                case "IO操作面板":
                    {
                        PrismProvider.EventAggregator.GetEvent<OpenSingleWindowEvent>().Publish($"IOManagerView");
                    }
                    break;

                case "打开调试面板":
                    {
                        if (ModelParam?.CurSltCard == null)
                        {
                            return;
                        }

                        var venderName = ModelParam.CurSltCard.VenderName ?? string.Empty;

                        if (venderName == "Googol")
                        {
                            //弹窗初始化页面
                            PrismProvider.DialogService.Show("GoogolCustomView", new DialogParameters
                            {
                                { "Title", "固高轴卡调试" },
                                { "Icon", "\ue673" },
                            }, result =>
                            {
                            }, nameof(DialogWindowView));
                        }
                        else if (venderName == "ZMotion")
                        {
                            var card = ModelParam.CurSltCard;
                            var handle = IntPtr.Zero;
                            var prop = card.GetType().GetProperty("Handle", BindingFlags.Public | BindingFlags.Instance);
                            if (prop != null && prop.PropertyType == typeof(IntPtr))
                            {
                                handle = (IntPtr)prop.GetValue(card);
                            }
                            else
                            {
                                var field = card.GetType().GetField("g_handle", BindingFlags.NonPublic | BindingFlags.Instance);
                                if (field != null && field.FieldType == typeof(IntPtr))
                                {
                                    handle = (IntPtr)field.GetValue(card);
                                }
                            }

                            var viewType = Type.GetType("ReeYin_V.Hardware.ControlCard.ZMotion.CustomUI.Views.MotionCardSettingsView, ReeYin_V.Hardware.ControlCard.ZMotion");
                            if (viewType == null)
                            {
                                MessageBox.Show("未找到正运动参数页面");
                                return;
                            }

                            var window = Activator.CreateInstance(viewType, handle) as Window;
                            if (window == null)
                            {
                                MessageBox.Show("正运动参数页面创建失败");
                                return;
                            }

                            window.Owner = Application.Current.MainWindow;
                            window.ShowDialog();
                        }
                        else if (IsAcsControlCard(ModelParam.CurSltCard))
                        {
                            OpenAcsConfigWindow(ModelParam.CurSltCard);
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

                        if (ModelParam.SltCardName == null || ModelParam.SltCardName == "") return;

                        //if (ModelParam.CardModels.Contains(ModelParam.CurSltCard))
                        //{
                        //    MessageView.Ins.MessageBoxShow("该设备已经添加列表!");
                        //    return;
                        //}
                        //根据选中名称创建新实例

                        ControlCardBase module = null;

                        if (ModelParam.SltVendorType == "ZMotion" || ModelParam.SltVendorType == "正运动")
                        {
                            module = PrismProvider.Container.Resolve<ControlCardBase>("ZMotionControlCard");
                        }


                        if (ModelParam.SltVendorType == "Googol")
                        { 
                            module = PrismProvider.Container.Resolve<IControlCard>("GoogolControlCard") as ControlCardBase;
                        }

                        if (ModelParam.SltVendorType == "ACS")
                        {
                            module = PrismProvider.Container.Resolve<IControlCard>("ACSControlCard") as ControlCardBase;
                        }

                        if (module == null)
                        {
                            MessageBox.Show($"未找到控制卡模块：{ModelParam.SltVendorType}/{ModelParam.SltCardName}");
                            return;
                        }


                        // var module = PrismProvider.Container.Resolve<IControlCard>("GoogolControlCard") as ControlCardBase;
                        module.VenderName = ModelParam.SltVendorType;
                        module.CardType = ModelParam.SltCardName;
                        module.Config?.EnsureInterpolationCoordinateSystems();
                        EnsureVendorSpecificOptions(module);

                        ModelParam.CardModels.Add(module);
                        ModelParam.CurSltCard = module;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ControlCardConfigViewModel_DataOperateCommand错误：{ex.StackTrace}");
                    }
                    break;
                case "Del":
                    {
                        ModelParam.CurSltCard.Close();
                        ModelParam.CardModels.Remove(ModelParam.CurSltCard);
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
        private static void EnsureVendorSpecificOptions(ControlCardBase card)
        {
            if (card == null)
            {
                return;
            }

            var method = card.GetType().GetMethod("EnsureOptions", BindingFlags.Public | BindingFlags.Instance);
            method?.Invoke(card, null);
        }

        private static bool IsAcsControlCard(ControlCardBase card)
        {
            return card != null &&
                   (string.Equals(card.VenderName, "ACS", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(card.GetType().FullName, "ReeYin_V.Hardware.ControlCard.ACS.App.AcsControlCard", StringComparison.Ordinal));
        }

        private static void OpenAcsConfigWindow(ControlCardBase card)
        {
            EnsureVendorSpecificOptions(card);

            // 通过反射打开 ACS 插件页面，避免基础控制卡项目反向引用 ACS 项目。
            var viewType = card.GetType().Assembly.GetType("ReeYin_V.Hardware.ControlCard.ACS.Views.AcsControlCardConfigView")
                           ?? Type.GetType("ReeYin_V.Hardware.ControlCard.ACS.Views.AcsControlCardConfigView, ReeYin_V.Hardware.ControlCard.ACS");
            if (viewType == null)
            {
                MessageBox.Show("未找到ACS配置页面");
                return;
            }

            Window? window;
            try
            {
                window = Activator.CreateInstance(viewType, card) as Window;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ACS配置页面创建失败：{ex.Message}");
                return;
            }

            if (window == null)
            {
                MessageBox.Show("ACS配置页面创建失败");
                return;
            }

            if (Application.Current?.MainWindow != null)
            {
                window.Owner = Application.Current.MainWindow;
            }

            window.ShowDialog();
        }

        private bool TryValidateInterpolationCoordinateSystems(out string message)
        {
            message = string.Empty;

            if (ModelParam?.CardModels == null || ModelParam.CardModels.Count == 0)
            {
                return true;
            }

            for (int cardIndex = 0; cardIndex < ModelParam.CardModels.Count; cardIndex++)
            {
                var card = ModelParam.CardModels[cardIndex];
                var coordinateSystems = card?.Config?.InterpolationCoordinateSystems;
                if (coordinateSystems == null || coordinateSystems.Count == 0)
                {
                    message = $"第{cardIndex + 1}张控制卡请至少配置一个插补坐标系。";
                    return false;
                }

                var enabledSystems = coordinateSystems.Where(item => item != null && item.IsUsing).ToList();
                if (enabledSystems.Count != 1)
                {
                    message = $"第{cardIndex + 1}张控制卡的插补坐标系必须且只能启用一个。";
                    return false;
                }

                for (int i = 0; i < coordinateSystems.Count; i++)
                {
                    var coordinateSystem = coordinateSystems[i];
                    if (coordinateSystem == null)
                    {
                        message = $"第{cardIndex + 1}张控制卡的第{i + 1}个插补坐标系为空。";
                        return false;
                    }

                    if (!coordinateSystem.TryValidate(out var validateMessage))
                    {
                        message = $"第{cardIndex + 1}张控制卡的第{i + 1}个插补坐标系无效：{validateMessage}";
                        return false;
                    }
                }
            }

            return true;
        }

        private bool TryValidateLimitConfig(out string message)
        {
            message = string.Empty;

            if (ModelParam?.AllLimitPos == null || ModelParam.AllLimitPos.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < ModelParam.AllLimitPos.Count; i++)
            {
                var limit = ModelParam.AllLimitPos[i];
                if (limit == null)
                {
                    continue;
                }

                if (!limit.TryValidate(out var limitMessage))
                {
                    message = $"第{i + 1}条限位配置无效：{limitMessage}";
                    return false;
                }
            }

            return true;
        }

        #endregion

    }
}
