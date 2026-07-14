using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using ReeYin_V.Core;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Module;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Config.ViewModels
{
    public class HardwareConfigViewModel : DialogViewModelBase
    {
        #region Fields

        #endregion

        #region Properties
        private ObservableCollection<HardwareConfigItem> _configItems = new ObservableCollection<HardwareConfigItem>();
        /// <summary>
        /// 硬件配置项集合
        /// </summary>
        public ObservableCollection<HardwareConfigItem> ConfigItems
        {
            get { return _configItems; }
            set { _configItems = value; RaisePropertyChanged(); }
        }

        private HardwareConfigItem _curSltHardware;
        /// <summary>
        /// 当前选中的硬件配置项
        /// </summary>
        public HardwareConfigItem CurSltHardware
        {
            get { return _curSltHardware; }
            set { _curSltHardware = value; RaisePropertyChanged();}
        }

        #endregion

        #region Constructor
        public HardwareConfigViewModel()
        {
            ConfigItems = PrismProvider.HardwareModuleManager.ConfigItems;

            //PrismProvider.Dispatcher.Invoke(() =>
            //{
            //    ////加载主界面
            //    PrismProvider.ModuleManager.LoadModule("ControlCardBaseModule");
            //    //导航到主区域
            //    PrismProvider.RegionManager.RequestNavigate("HardwareRegion", "ControlCardConfigView");
            //});
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
                case "切换页面":
                    {
                        CurSltHardware.Navigation.Invoke();
                    }
                    break;

                case "取消":
                    CloseDialog(ButtonResult.No);
                    break;
                case "返回主页面":
                    {
                        PrismProvider.Dispatcher.Invoke(() =>
                        {
                            //加载主界面
                            PrismProvider.ModuleManager.LoadModule("ApplicatoinMainModule");
                            //导航到主区域
                            PrismProvider.RegionManager.RequestNavigate("MainRegion", "MainView");
                        });

                    }
                    break;
                case "确认":
                    CloseDialog(ButtonResult.OK, new DialogParameters()
                    {
                        
                    });
                    break;
                default:
                    break;
            }

        });
        #endregion

        #region Methods
        public override void InitParam()
        {
            PrismProvider.Dispatcher.Invoke(() =>
            {
                //////加载主界面
                //PrismProvider.ModuleManager.LoadModule("ControlCardBaseModule");
                ////导航到主区域
                //PrismProvider.RegionManager.RequestNavigate("HardwareRegion", "ControlCardConfigView");
            });
        }
        #endregion

    }
}
