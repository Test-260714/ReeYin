using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Models;
using ReeYin_V.Core.Models.Database.Repository;
using ReeYin_V.Core.Services;
using ReeYin_V.License.Services;
using ReeYin_V.Share.Events;
using ReeYin_V.Share.Prism;
using ReeYin_V.Shell.Views;
using ReeYin_V.UI.Style.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ReeYin_V.Shell.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        #region Fields
        private Window MainWindow { get; set; } = Application.Current.MainWindow;

        public bool IsHardwareInitialized { get; private set; } = false;
        public IModuleManager ModuleManager { get; }
        public IRegionManager RegionManager { get; }
        #endregion

        #region Properties
        private string _windowIconUri;
        public string WindowIconUri
        {
            get { return _windowIconUri; }
            set { _windowIconUri = value; RaisePropertyChanged(); }
        }

        private string title = "ReeYin_V";
        public string Title
        {
            get { return title; }
            set { title = value; RaisePropertyChanged(); }
        }

        private UCMenuView _mainMenu;

        public UCMenuView MainMenu
        {
            get { return _mainMenu; }
            set { _mainMenu = value; RaisePropertyChanged(); }
        }

        private string _modeName = "运行模式";
        /// <summary>
        /// 运行模式
        /// </summary>
        public string ModeName
        {
            get { return _modeName; }
            set { _modeName = value; RaisePropertyChanged(); }
        }

        private Visibility _titleActionButtonsVisibility = Visibility.Collapsed;
        /// <summary>
        /// 登录前隐藏自定义标题按钮，登录成功后显示
        /// </summary>
        public Visibility TitleActionButtonsVisibility
        {
            get { return _titleActionButtonsVisibility; }
            set { _titleActionButtonsVisibility = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor
        public MainWindowViewModel(IModuleManager moduleManager, IRegionManager regionManager)
        {
            ModuleManager = moduleManager;
            RegionManager = regionManager;
            LoadedCommand = new DelegateCommand(Loaded);
            PrismProvider.AppBasePath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

        }
        #endregion

        #region Commands
        public ICommand LoadedCommand { get; }


        #endregion

        #region Metods

        private void Loaded()
        {
            MainWindow.Visibility = Visibility.Hidden;
            IRegion mainRegion = PrismProvider.RegionManager.Regions[RegionNames.MainRegion];
            mainRegion.NavigationService.Navigated += NavigationService_Navigated;
#if !DEBUG
            var licenseService = PrismProvider.Container.Resolve<ILicenseService>();
            var licenseStatus = licenseService.CurrentStatus;

            if (!licenseStatus.IsValid)
            {
                PrismProvider.ModuleManager.LoadModule(ModuleNames.ApplicationLicenseModule);

                PrismProvider.DialogService.Show("LicenseActivationView", new DialogParameters()
                {
                    { "Title", "许可证激活" },
                    { "Icon", "\ue61c" },
                }, result =>
                {
                    if (licenseService.CurrentStatus.IsValid)
                    {
                        MessageBox.Show("许可证激活成功，请重启软件后继续使用。", "激活成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("未完成许可证激活，软件将退出。", "许可证未激活", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }

                    Application.Current.Shutdown();
                }, nameof(DialogWindowView));

                return;
            }
#endif

            MainWindow.Visibility = Visibility.Visible;
            LoadLogin();
        }

        private void LoadLogin()
        {
            //第一步，加载Login模块，导航到Login区域
            PrismProvider.ModuleManager.LoadModule(ModuleNames.ApplicationLoginModule);

            PrismProvider.RegionManager.RequestNavigate(RegionNames.MainRegion, ViewNames.LoginView);

            //订阅用户登录成功事件
            PrismProvider.EventAggregator.GetEvent<LoginSuccessEvent>().Subscribe(OnLogined, ThreadOption.UIThread);

            //登陆成功在显示菜单
            MainMenu = new UCMenuView();
        }


        private void OnLogined(CurrentUser user)
        {
            MainWindow.WindowStyle = WindowStyle.SingleBorderWindow;
            TitleActionButtonsVisibility = Visibility.Visible;

            PrismProvider.EventAggregator.GetEvent<SwitchLanguageEvent>().Publish();
            if (IsHardwareInitialized)
            {

                //加载主界面
                PrismProvider.ModuleManager.LoadModule(ModuleNames.ApplicatoinMainModule);
                //导航到主区域
                PrismProvider.RegionManager.RequestNavigate(RegionNames.MainRegion, "RunMainView");
            }
            else
            {
                //加载硬件初始化模块
                PrismProvider.ModuleManager.LoadModule(ModuleNames.ApplicationInitializeModule);
                MainWindow.Hide();

                //弹窗初始化页面
                PrismProvider.DialogService.Show(ViewNames.InitializeView, new DialogParameters
                {


                }, result =>
                {
                    //初始化成功或跳过，显示界面
                    MainWindow.Show();
                }, nameof(NonTitleDialogWindowView));

                //导航到主区域
                //PrismProvider.RegionManager.RequestNavigate(RegionNames.MainRegion, ViewNames.InitializeView);
            }
        }
        #endregion



        //当主区域导航时
        private void NavigationService_Navigated(object sender, RegionNavigationEventArgs e)
        {
            switch (e.Uri.OriginalString)
            {
                case ViewNames.LoginView:
                    TitleActionButtonsVisibility = Visibility.Collapsed;
                    //MainWindow.ResizeMode = ResizeMode.NoResize;
                    //MainWindow.SizeToContent = SizeToContent.WidthAndHeight;
                    //MainWindow.WindowState = WindowState.Normal;

                    //MainWindow.Width = 600;
                    //MainWindow.Height = 350;
                    //MainWindow.WindowStyle = WindowStyle.None;
                    break;

                case ViewNames.MainView:
                    TitleActionButtonsVisibility = Visibility.Visible;
                    MainWindow.WindowState = WindowState.Maximized;

                    //MainWindow.Width = 1920;
                    //MainWindow.Height = 1080;
                    //MainWindow.ResizeMode = ResizeMode.CanMinimize;
                    //MainWindow.SizeToContent = SizeToContent.Manual;
                    //MainWindow.WindowStyle = WindowStyle.SingleBorderWindow;
                    break;
                default:
                    TitleActionButtonsVisibility = Visibility.Visible;
                    break;
            }
        }
    }
}
