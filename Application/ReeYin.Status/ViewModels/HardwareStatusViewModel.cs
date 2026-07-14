using Microsoft.VisualBasic;
using ReeYin.Status.Models;
using ReeYin_V.Core;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Events.Hardware;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Module;
using ReeYin_V.Core.Services.Project;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ReeYin.Status.ViewModels
{
    public class HardwareStatusViewModel : DialogViewModelBase, INavigationAware
    {
        #region Fields
        public SubscriptionToken subscriptionToken;
        #endregion

        #region Properties
        private ObservableCollection<HardwareStatus> _allStatus = new ObservableCollection<HardwareStatus>();

        public ObservableCollection<HardwareStatus> AllStatus
        {
            get { return _allStatus; }
            set { _allStatus = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor
        public HardwareStatusViewModel()
        {
            subscriptionToken = PrismProvider.EventAggregator.GetEvent<HardwareStatusChangedEvent>().Subscribe(action: (statusItem) =>
            {
                // 注意：这里假设事件处理程序在非UI线程执行，所以需要 BeginInvoke
                PrismProvider.Dispatcher.BeginInvoke(() =>
                {
                    // 在UI线程上操作集合
                    var existingItem = AllStatus.FirstOrDefault(p => p.Name == statusItem.Name);
                    if (existingItem != null)
                    {
                        // 更新现有项的状态
                        existingItem.Status = statusItem.Status;
                        existingItem.IsConnect = statusItem.IsConnect;
                        // 如果 StatusItem 有其他属性也需要更新，可以一起更新
                        // existingItem.SomeOtherProperty = statusItem.SomeOtherProperty;
                    }
                    else
                    {
                        // 添加新项
                        AllStatus.Add(statusItem);
                    }
                });
            });

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
            subscriptionToken?.Dispose();

        });
        #endregion

    }
}
