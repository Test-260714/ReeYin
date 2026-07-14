using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Navigation.Regions;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Events.Hardware;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Models;
using ReeYin_V.Core.Services.Module;
using ReeYin_V.Core.Services.WorkStatus;
using ReeYin_V.Share.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace ReeYin_V.Main.ViewModels
{
    public class OpBtnViewModel : DialogViewModelBase, INavigationAware
    {
        #region Fields
        public SubscriptionToken SubscriptionToken { get; private set; }
        #endregion

        #region Properties
        private ObservableCollection<HardwareStatus> _allStatus = new ObservableCollection<HardwareStatus>();

        public ObservableCollection<HardwareStatus> AllStatus
        {
            get { return _allStatus; }
            set { _allStatus = value; RaisePropertyChanged(); }
        }
        public CurrentUser CurUser
        {
            get { return PrismProvider.User.CurUser; }
            set { RaisePropertyChanged(); }
        }

        public WorkStatus CurStatus
        {
            get { return PrismProvider.WorkStatusManager.CurStatus; }
            set { RaisePropertyChanged(); }
        }
        #endregion

        #region Constructor
        public OpBtnViewModel()
        {
            SubscriptionToken = PrismProvider.EventAggregator.GetEvent<WorkStatusChangeEvent>().Subscribe((obj) =>
            {
                CurStatus = obj;
            }, ThreadOption.UIThread);
        }


        #endregion

        #region OverrideMethods
        public bool IsNavigationTarget(NavigationContext navigationContext)
        {

            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {

        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {

        }
        #endregion

        #region Methods

        #endregion

        #region Commands
        /// <summary>
        /// 通用指令
        /// </summary>
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "取消":
                    CloseDialog(ButtonResult.No);

                    break;

                case "关闭":
                    {

                    }
                    break;
                case "运行一次":
                    {
                        // 触发工作状态切换事件
                        PrismProvider.EventAggregator
                            .GetEvent<SwitchWorkStatusEvent>()
                            .Publish((eRunStatus.Running, -1));
                    }
                    break;

                case "连续运行":
                    {

                    }
                    break;

                default:
                    break;
            }

        });

        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            PrismProvider.HardwareModuleManager.RefreshStatus();


        });

        public DelegateCommand UnLoadedCommand => new DelegateCommand(() =>
        {
            SubscriptionToken?.Dispose();

        });
        #endregion

    }
}
