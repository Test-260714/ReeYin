using Prism.Events;
using Prism.Mvvm;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services;
using ReeYin_V.Share.Events;
using ReeYin_V.Share.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Main.ViewModels
{
    public class PrimaryViewModel : BindableBase
    {
        #region Field

        #endregion

        #region Properties
        public ObservableCollection<MenuModel> MainBtnList
        {
            get { return ServiceProvider.MenuService._Menus.Where(m => m.Type == "main").ToList().ToObservableCollection(); }
            set { RaisePropertyChanged(); }
        }
        #endregion

        #region Construtor
        public PrimaryViewModel()
        {
            //订阅权限变化事件
            PrismProvider.EventAggregator.GetEvent<PermissionChangedEvent>().Subscribe(RefreshMenu, ThreadOption.UIThread);
        }
        #endregion

        #region Method
        public void RefreshMenu()
        {
            MainBtnList = ServiceProvider.MenuService._Menus.Where(m => m.Type == "main").ToList().ToObservableCollection();

        }
        #endregion

        #region Command

        #endregion
    }
}
