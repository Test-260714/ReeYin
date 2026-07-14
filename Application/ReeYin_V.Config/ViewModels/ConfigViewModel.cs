using Prism.Commands;
using Prism.Dialogs;
using Prism.Navigation.Regions;
using ReeYin_V.Config.Services;
using ReeYin_V.Config.Views;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.IOC;
using ReeYin_V.Share.Prism;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ReeYin_V.Config.ViewModels
{
    public class ConfigViewModel : DialogViewModelBase
    {
        #region Fields
        public SystemConfigManager SystemConfigManager { get; }
        public SystemConfigProvider SystemConfigProvider { get; }
        public ICommand OKCommand { get; }
        #endregion

        #region Properties
        private ObservableCollection<string> items = new ObservableCollection<string>();

        public ObservableCollection<string> Items
        {
            get { return items; }
            set { items = value; RaisePropertyChanged(); }
        }

        private LanguageType sltLanguage;

        public LanguageType SltLanguage
        {
            get { return sltLanguage; }
            set { sltLanguage = value; RaisePropertyChanged(); }
        }

        private ThemeType sltTheme;

        public ThemeType SltTheme
        {
            get { return sltTheme; }
            set { sltTheme = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Constructor
        public ConfigViewModel(SystemConfigManager systemConfigManager,
            SystemConfigProvider systemConfigProvider)
        {
            SystemConfigManager = systemConfigManager;
            SystemConfigProvider = systemConfigProvider;
            OKCommand = new DelegateCommand(Submit);
            Items = new ObservableCollection<string>() { "语言配置", "样式配置", "其他" };
        }
        #endregion

        #region Commands
        public DelegateCommand SetLanguageCommand => new DelegateCommand(() =>
        {
            NavigateToConfigView(nameof(LanguageConfigView));
        });

        public DelegateCommand SetThemeCommand => new DelegateCommand(() =>
        {
            NavigateToConfigView(nameof(StyleConfigView));
        });

        private void Submit()
        {
            SystemConfigManager.Save();
            SystemConfigProvider.Invoke();
            CloseDialog(ButtonResult.OK);
        }
        #endregion

        #region Methods
        private static void NavigateToConfigView(string viewName)
        {
            PrismProvider.Dispatcher.Invoke(() =>
            {
                PrismProvider.ModuleManager.LoadModule(ModuleNames.ApplicatoinMainModule);
                PrismProvider.RegionManager.RequestNavigate("MainRegion", viewName);
            });
        }
        #endregion
    }
}
