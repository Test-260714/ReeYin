using ReeYin_V.Share.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Services.Menu
{
    public interface IMenuService
    {
        ObservableCollection<MenuModel> _Menus { get; set; }

        /// <summary>
        /// 刷新/加载菜单
        /// </summary>
        void OnLoadMenu();
        /// <summary>
        /// 排序菜单
        /// </summary>
        /// <param name="TopID"> 对哪些ID设置为顶级项</param>
        /// <returns></returns>
        ObservableCollection<MenuModel> SortMenu(int[] TopID);
    }
}
