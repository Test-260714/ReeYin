using ReeYin.RootManager.Models;
using ReeYin_V.Core;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.IOC;
using System.Collections.ObjectModel;
using ModuleInfo = ReeYin.RootManager.Models.ModuleInfo;

namespace ReeYin.RootManager.ViewModels
{
    public class ComponentPreviewViewModel : BindableBase, INavigationAware
    {
        #region Properties

        public ObservableCollection<ModuleInfo> Categories { get; } = new();

        private ModuleInfo _selectedCategory;
        public ModuleInfo SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                SetProperty(ref _selectedCategory, value);
                RaisePropertyChanged(nameof(ModuleInfos));
            }
        }

        /// <summary>
        /// 当前分类下的模块分组（若无选中则显示全部）
        /// </summary>
        public ObservableCollection<ModuleInfo> ModuleInfos
        {
            get
            {
                if (_selectedCategory == null)
                    return Categories;

                var result = new ObservableCollection<ModuleInfo>();
                foreach (var cat in Categories)
                {
                    if (cat.Header == _selectedCategory.Header)
                        result.Add(cat);
                }
                return result;
            }
        }

        #endregion

        #region Commands

        private DelegateCommand<string>? _generalCommand;
        public DelegateCommand<string> GeneralCommand => _generalCommand ??= new DelegateCommand<string>(order =>
        {
            switch (order)
            {
                case "全选":
                    foreach (var menu in PrismProvider.NodifyMenuManager.AllMenus)
                        menu.IsUsing = true;
                    break;
                case "全不选":
                    foreach (var menu in PrismProvider.NodifyMenuManager.AllMenus)
                        menu.IsUsing = false;
                    break;
                case "刷新条件":
                    LoadCategories();
                    break;
            }
        });

        #endregion

        #region Methods

        private void LoadCategories()
        {
            Categories.Clear();
            var groups = PrismProvider.NodifyMenuManager.AllMenus.GroupBy(m => m.Type);
            foreach (var group in groups)
            {
                Categories.Add(new ModuleInfo
                {
                    Header = group.Key,
                    Children = group.ToList().ToObservableCollection()
                });
            }
            RaisePropertyChanged(nameof(ModuleInfos));
        }

        #endregion

        #region INavigationAware

        public bool IsNavigationTarget(NavigationContext navigationContext) => true;

        public void OnNavigatedFrom(NavigationContext navigationContext) { }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            LoadCategories();
            if (Categories.Count > 0)
                SelectedCategory = Categories[0];
        }

        #endregion
    }
}
