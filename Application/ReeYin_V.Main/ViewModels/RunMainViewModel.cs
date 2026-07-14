using HandyControl.Controls;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Models;
using ReeYin_V.Core.Services;
using ReeYin_V.Core.Services.WorkStatus;
using ReeYin_V.Share.Events;
using ReeYin_V.Share.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ReeYin_V.Main.ViewModels
{
    public class RunMainViewModel : BindableBase, INavigationAware, IRegionMemberLifetime
    {
        #region Fields
        public ICommand LoadedCommand { get; }
        #endregion

        #region Properties
        private bool _isEnable = true;
        /// <summary>
        /// 使能页面
        /// </summary>
        public bool IsEnable
        {
            get { return _isEnable; }
            set { _isEnable = value; RaisePropertyChanged(); }
        }

        public CurrentUser CurUser
        {
            get { return PrismProvider.User.CurUser; }
            set { RaisePropertyChanged(); }
        }

        public ObservableCollection<MenuModel> MainBtnList
        {
            get { return ServiceProvider.MenuService._Menus.Where(m => m.Type == "main").ToList().ToObservableCollection(); }
            set { RaisePropertyChanged(); }
        }

        public ObservableCollection<MenuModel> RunBtnList
        {
            get { return ServiceProvider.MenuService._Menus.Where(m => m.Type == "run").ToList().ToObservableCollection(); }
            set { RaisePropertyChanged(); }
        }

        public bool KeepAlive => true;
        #endregion

        #region Constructor

        public RunMainViewModel()
        {
            //订阅权限变化事件
            PrismProvider.EventAggregator.GetEvent<PermissionChangedEvent>().Subscribe(RefreshMenu, ThreadOption.UIThread);

            LoadedCommand = new DelegateCommand(Loaded);

            PrismProvider.EventAggregator.GetEvent<WorkStatusChangeEvent>().Subscribe((obj) =>
            {
                //如果是运行状态，失能整个页面，不允许操作
                if (obj == WorkStatus.Running)
                {
                    IsEnable = false;
                }
                else
                {
                    IsEnable = true;
                }
            }, ThreadOption.UIThread);
        }

        #endregion

        #region Override
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {



            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            
        }
        #endregion

        #region Methods
        public void RefreshMenu()
        {
            MainBtnList = ServiceProvider.MenuService._Menus.Where(m => m.Type == "main").ToList().ToObservableCollection();
            RunBtnList = ServiceProvider.MenuService._Menus.Where(m => m.Type == "run").ToList().ToObservableCollection();
        }

        #endregion

        #region Commands
        private void Loaded()
        {
            Growl.Success("文件保存成功！", "SuccessMsg");
        }

        #endregion
    }
}
