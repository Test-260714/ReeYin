using Prism.Events;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Models.Database.Repository;
using ReeYin_V.Core.Services.Language;
using ReeYin_V.Core.Services.User;
using ReeYin_V.Share.Events;
using ReeYin_V.Share.Helper;
using ReeYin_V.Share.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Services.Menu
{
    [ExposedService(Lifetime.Singleton, 4, typeof(IMenuService))]
    public class MenuService : IMenuService
    {
        #region Fields      
        private MenuRepository _menuRepository;
        private PermMenuRelationRepository _permMenuRelationRepository;
        private ILanguageManager _languageManager;
        private IUserService _userService;
        public ObservableCollection<MenuModel> _Menus { get; set; } = new ObservableCollection<MenuModel>();
        #endregion

        #region Constructor

        public MenuService(MenuRepository menuRepository,
            PermMenuRelationRepository permMenuRelationRepository,
            ILanguageManager languageManager,
            IUserService userService
            )
        {
            _languageManager = languageManager;
            _permMenuRelationRepository = permMenuRelationRepository;
            _menuRepository = menuRepository;
            _userService = userService;

            //初始化菜单
            OnLoadMenu();
            //注册语言切换事件
            PrismProvider.EventAggregator.GetEvent<SwitchLanguageEvent>().Subscribe(OnLoadMenu, ThreadOption.UIThread);
        }
        #endregion

        #region Methods
        /// <summary>
        /// 载入菜单
        /// </summary>
        public void OnLoadMenu()
        {
            _Menus.Clear();

            #region 获取用户权限
            var allValidMenus = _permMenuRelationRepository.GetList(p => p.PermId == _userService.CurUser.PermissionID);

            #endregion

            // 获取所有菜单数据
            var allMenus = new List<MenuModel>();

            foreach (var item in _menuRepository.GetList())
            {
                //当前菜单权限
                var curMenuPerm = allValidMenus.FirstOrDefault(p => p.MenuId == item.MenuId);

                // 筛选出当前用户有权限访问的菜单（超级管理员直接显示所有菜单）
                if(allValidMenus.Any(p => p.MenuId == item.MenuId && p.IsVisible == true))
                {
                    allMenus.Add(new MenuModel()
                    {
                        IsEnabled = curMenuPerm.IsEnabled,
                        Name = _languageManager.GetStringResource(item.MenuName),
                        Icon = ChartOperation.ReplaceUnicode(item.Icon),
                        Description = item.Description,
                        Event = item.Event,
                        Type = item.Type,
                        ID = item.MenuId,
                        ParentID = item.ParentId,
                        IsVisiable = curMenuPerm.IsVisible,
                        CreateTime = item.CreateTime,
                        UpdateTime = item.UpdateTime,
                    });
                }
            }
            _Menus = allMenus.ToObservableCollection();
            //发布菜单发生改变
            PrismProvider.EventAggregator.GetEvent<PermissionChangedEvent>().Publish();
        }

        public ObservableCollection<MenuModel> SortMenu(int[] TopID)
        {
            // 获取所有菜单数据(使用深拷贝避免对原始菜单数据进行修改)
            var allMenus = _Menus.DeepCopy();

            ObservableCollection<MenuModel> sortedMenus = new ObservableCollection<MenuModel>();

            // 筛选条件：ParentID在SortID数组中且菜单启用
            var topLevelMenus = allMenus
                .Where(m => TopID.Contains(m.ParentID))
                .ToList();

            // 按SortID数组中的顺序排序
            topLevelMenus = topLevelMenus
                .OrderBy(m => Array.IndexOf(TopID, m.ParentID))
                .ToList();

            // 添加顶级菜单并递归处理子菜单
            foreach (var menu in topLevelMenus)
            {
                sortedMenus.Add(menu);
                AddChildMenus(menu, allMenus);
            }

            return sortedMenus;
        }

        /// <summary>
        /// 递归添加子菜单
        /// </summary>
        /// <param name="parentMenu"></param>
        /// <param name="allMenus"></param>
        public void AddChildMenus(MenuModel parentMenu, ObservableCollection<MenuModel> allMenus)
        {
            // 查找当前菜单的所有子菜单
            var children = allMenus.Where(m => m.ParentID == parentMenu.ID).ToList();

            foreach (var child in children)
            {
                // 添加子菜单到父菜单的 Children 集合
                parentMenu.Children.Add(child);
                // 递归处理子菜单的子菜单
                AddChildMenus(child, allMenus);
            }
        }
        #endregion

    }
}
