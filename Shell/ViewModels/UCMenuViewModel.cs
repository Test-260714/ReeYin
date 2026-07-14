using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Mvvm;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Models.Database.Repository;
using ReeYin_V.Core.Models.Database.Tables;
using ReeYin_V.Core.Services;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Logger;
using ReeYin_V.Share.Events;
using ReeYin_V.Share.Helper;
using ReeYin_V.Share.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ReeYin_V.Shell.ViewModels
{
    public class UCMenuViewModel : BindableBase
    {
        #region Fields

        #endregion

        #region Properties
        private string _loginIconPath;

        public string LoginIconPath
        {
            get { return _loginIconPath; }
            set { _loginIconPath = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<MenuModel> Menus
        {
            get { return ServiceProvider.MenuService.SortMenu([-1]); }
            set { RaisePropertyChanged(); }
        }

        private string _curSolutionPath;
        /// <summary>
        /// 当前项目路径
        /// </summary>
        public string CurSolutionPath
        {
            get { return _curSolutionPath; }
            set { _curSolutionPath = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor
        public UCMenuViewModel()
        {
            //订阅权限变化事件
            PrismProvider.EventAggregator.GetEvent<PermissionChangedEvent>().Subscribe(RefreshMenu, ThreadOption.UIThread);

            PrismProvider.EventAggregator.GetEvent<NodifyRalatedEvent>().Subscribe((obj) =>
            {

                CurSolutionPath = PrismProvider.ProjectManager.SltCurSolutionItem.FilePath +
                  PrismProvider.ProjectManager.SltCurSolutionItem.Name;

            }, ThreadOption.UIThread);

            LoginIconPath = PrismProvider.AppBasePath + "\\Resource\\Icon\\login_logo.png";

            if (!File.Exists(LoginIconPath))
            {
                LoginIconPath = PrismProvider.AppBasePath + "\\Resource\\Icon\\Default.png";
            }
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
                case "打开":
                    {
                        // 打开文件夹路径
                        Process.Start("explorer.exe", PrismProvider.ProjectManager.SltCurSolutionItem.FilePath);
                    }
                    break;

                default:
                    break;
            }

        });
        #endregion

        #region Methods
        public void RefreshMenu()
        {
            Menus = ServiceProvider.MenuService.SortMenu([-1]);
            CurSolutionPath = PrismProvider.ProjectManager.SltCurSolutionItem.FilePath +
                              PrismProvider.ProjectManager.SltCurSolutionItem.Name;
        }
        #endregion
    }
}
