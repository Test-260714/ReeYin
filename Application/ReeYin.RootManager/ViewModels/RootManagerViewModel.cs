using Newtonsoft.Json;
using ReeYin.RootManager.Models;
using ReeYin.RootManager.Views;
using ReeYin_V.Core;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.IOC;
using ReeYin_V.UI;
using System.Collections.ObjectModel;

namespace ReeYin.RootManager.ViewModels
{
    public class RootManagerViewModel : DialogViewModelBase
    {
        #region Fields

        private readonly IRegionManager _regionManager;

        #endregion

        #region Properties

        public ObservableCollection<NavItemModel> NavItems { get; } = new ObservableCollection<NavItemModel>
        {
            new NavItemModel { Header = "组件管理",     ViewName = nameof(ComponentPreviewView) },
            new NavItemModel { Header = "硬件管理",     ViewName = nameof(HardwareManageView) },
            new NavItemModel { Header = "模块加载配置", ViewName = nameof(ModuleLoadConfigView) },
            new NavItemModel { Header = "软件更新",     ViewName = nameof(UpdateView) },
        };

        private NavItemModel _selectedNavItem;
        public NavItemModel SelectedNavItem
        {
            get => _selectedNavItem;
            set
            {
                SetProperty(ref _selectedNavItem, value);
                if (value != null)
                    _regionManager.RequestNavigate("RootManagerContentRegion", value.ViewName);
            }
        }

        #endregion

        #region Constructor

        public RootManagerViewModel(IDialogService dialogService, IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }

        #endregion

        #region Commands

        private DelegateCommand? _loadCommand;
        public DelegateCommand LoadCommand => _loadCommand ??= new DelegateCommand(() =>
        {
            SelectedNavItem = NavItems[0];
        });

        private DelegateCommand<string>? _generalCommand;
        public DelegateCommand<string> GeneralCommand => _generalCommand ??= new DelegateCommand<string>(order =>
        {
            switch (order)
            {
                case "保存":
                    JsonHelper.JsonObjectSerialize(PrismProvider.NodifyMenuManager.AllMenus,
                        FileHelper.AppHiddenPath + $"\\LocalMenus.json", TypeNameHandling.Auto);

                    PrismProvider.NodifyMenuManager.AvailableMenus.Clear();
                    foreach (var menu in PrismProvider.NodifyMenuManager.AllMenus)
                    {
                        if (menu.IsUsing)
                            PrismProvider.NodifyMenuManager.AvailableMenus.Add(menu);
                    }
                    break;
                case "取消":
                    break;
            }
        });

        #endregion
    }
}
