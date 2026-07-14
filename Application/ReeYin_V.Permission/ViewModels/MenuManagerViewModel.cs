using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Models.Database.Repository;
using ReeYin_V.Core.Services;
using ReeYin_V.Share.Helper;
using ReeYin_V.Share.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Permission.ViewModels
{
    public class MenuManagerViewModel : DialogViewModelBase
    {
        #region Fields

        #endregion

        #region Properties  
        /// <summary>
        /// 所有菜单
        /// </summary>
        public ObservableCollection<MenuModel> Menus
        {
            get { return ServiceProvider.MenuService.SortMenu([-1,0]); }
            set { RaisePropertyChanged(); }
        }

        private MenuModel _curMenuInfo;
        /// <summary>
        /// 菜单信息
        /// </summary>
        public MenuModel CurMenuInfo
        {
            get { return _curMenuInfo; }
            set { _curMenuInfo = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Constructor
        public MenuManagerViewModel()
        {
            
        }
        #endregion

        #region Commands
        public DelegateCommand<object> RefreshMenu => new DelegateCommand<object>((obj) =>
        {
            var Param = obj as MenuModel;
            if(Param.Icon != "")
                Param.Icon = ChartOperation.ReplaceUnicode(Param.Icon);
            else
                Param.Icon = "null";
            CurMenuInfo = Param;
        });
        #endregion

        #region Methods

        #endregion

    }
}
