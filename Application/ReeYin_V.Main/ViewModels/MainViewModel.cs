using HandyControl.Controls;
using NetTaste;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Events.Hardware;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Models;
using ReeYin_V.Core.Services;
using ReeYin_V.Core.Services.WorkStatus;
using ReeYin_V.Main.Services;
using ReeYin_V.Share.Events;
using ReeYin_V.Share.Helper;
using ReeYin_V.Share.Models;
using ReeYin_V.Share.Prism;
using ReeYin_V.UI.Style.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ReeYin_V.Main.ViewModels
{
    public class MainViewModel : BindableBase, INavigationAware, IRegionMemberLifetime
    {
        #region Fields
        public ICommand LoadedCommand { get; }
        private CancellationTokenSource _controlCardResetOverlayTimeoutCts;
        private Guid _controlCardResetOverlayOperationId;
        private System.Windows.Window _controlCardResetOverlayWindow;
        private TextBlock _controlCardResetOverlayWindowMessage;
        #endregion

        #region Properties
        public CurrentUser CurUser
        {
            get { return PrismProvider.User.CurUser; }
            set { RaisePropertyChanged(); }
        }

        public WorkStatus CurStatus
        {
            get { return PrismProvider.WorkStatusManager.CurStatus; }
            set { RaisePropertyChanged(); }
        }

        public AlarmNotificationService AlarmNotifications { get; }

        private bool _isControlCardResetOverlayVisible;
        public bool IsControlCardResetOverlayVisible
        {
            get { return _isControlCardResetOverlayVisible; }
            set { SetProperty(ref _isControlCardResetOverlayVisible, value); }
        }

        private string _controlCardResetOverlayMessage = "复位中，不允许操作";
        public string ControlCardResetOverlayMessage
        {
            get { return _controlCardResetOverlayMessage; }
            set
            {
                if (SetProperty(ref _controlCardResetOverlayMessage, value) &&
                    _controlCardResetOverlayWindowMessage != null)
                {
                    _controlCardResetOverlayWindowMessage.Text = value;
                }
            }
        }

        public ObservableCollection<MenuModel> MainBtnList
        {
            get { return ServiceProvider.MenuService._Menus.Where(m => m.Type == "main").ToList().ToObservableCollection(); }
            set {  RaisePropertyChanged(); }
        }

        public ObservableCollection<MenuModel> RunBtnList
        {
            get { return ServiceProvider.MenuService._Menus.Where(m => m.Type == "run").ToList().ToObservableCollection(); }
            set { RaisePropertyChanged(); }
        }

        public bool KeepAlive => true;
        #endregion

        #region Constructor

        public MainViewModel()
        {
            AlarmNotifications = PrismProvider.Container.Resolve<AlarmNotificationService>();
            //订阅权限变化事件
            PrismProvider.EventAggregator.GetEvent<PermissionChangedEvent>().Subscribe(RefreshMenu, ThreadOption.UIThread);
            PrismProvider.EventAggregator.GetEvent<WorkStatusChangeEvent>().Subscribe((obj) =>
            {
                CurStatus = obj;
            }, ThreadOption.UIThread);
            PrismProvider.EventAggregator.GetEvent<ControlCardResetOverlayEvent>().Subscribe(OnControlCardResetOverlay, ThreadOption.UIThread);
            LoadedCommand = new DelegateCommand(Loaded);
        }

        #endregion

        #region Override
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            // 页面被激活
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            // 控制是否复用已有实例
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // 页面切出时执行（但不会销毁）
        }
        #endregion

        #region Methods
        public void RefreshMenu()
        {
            MainBtnList = ServiceProvider.MenuService._Menus.Where(m => m.Type == "main").ToList().ToObservableCollection();
            RunBtnList = ServiceProvider.MenuService._Menus.Where(m => m.Type == "run").ToList().ToObservableCollection();
        }

        private void OnControlCardResetOverlay(ControlCardResetOverlayPayload payload)
        {
            if (payload == null)
            {
                return;
            }

            if (payload.IsRunning)
            {
                var timeoutSeconds = payload.TimeoutSeconds > 0 ? payload.TimeoutSeconds : 60;
                _controlCardResetOverlayOperationId = payload.OperationId;
                ControlCardResetOverlayMessage = string.IsNullOrWhiteSpace(payload.Message)
                    ? $"复位中，不允许操作。超时时间：{timeoutSeconds}秒"
                    : payload.Message;
                IsControlCardResetOverlayVisible = true;
                ShowControlCardResetOverlayWindow();
                StartControlCardResetOverlayTimeout(payload.OperationId, timeoutSeconds);
                return;
            }

            if (payload.OperationId != _controlCardResetOverlayOperationId)
            {
                return;
            }

            _controlCardResetOverlayTimeoutCts?.Cancel();
            ControlCardResetOverlayMessage = string.IsNullOrWhiteSpace(payload.Message)
                ? "复位完成"
                : payload.Message;
            IsControlCardResetOverlayVisible = false;
            HideControlCardResetOverlayWindow();
        }

        private void StartControlCardResetOverlayTimeout(Guid operationId, int timeoutSeconds)
        {
            _controlCardResetOverlayTimeoutCts?.Cancel();
            _controlCardResetOverlayTimeoutCts = new CancellationTokenSource();
            _ = HideControlCardResetOverlayOnTimeoutAsync(operationId, timeoutSeconds, _controlCardResetOverlayTimeoutCts.Token);
        }

        private async Task HideControlCardResetOverlayOnTimeoutAsync(Guid operationId, int timeoutSeconds, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), cancellationToken);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            if (cancellationToken.IsCancellationRequested || operationId != _controlCardResetOverlayOperationId)
            {
                return;
            }

            ControlCardResetOverlayMessage = $"复位超时（{timeoutSeconds}秒），已解除页面锁定。";
            IsControlCardResetOverlayVisible = false;
            HideControlCardResetOverlayWindow();
        }

        private void ShowControlCardResetOverlayWindow()
        {
            if (_controlCardResetOverlayWindow == null)
            {
                _controlCardResetOverlayWindow = CreateControlCardResetOverlayWindow();
            }

            if (_controlCardResetOverlayWindowMessage != null)
            {
                _controlCardResetOverlayWindowMessage.Text = ControlCardResetOverlayMessage;
            }

            if (_controlCardResetOverlayWindow.IsVisible)
            {
                return;
            }

            _controlCardResetOverlayWindow.Left = SystemParameters.VirtualScreenLeft;
            _controlCardResetOverlayWindow.Top = SystemParameters.VirtualScreenTop;
            _controlCardResetOverlayWindow.Width = SystemParameters.VirtualScreenWidth;
            _controlCardResetOverlayWindow.Height = SystemParameters.VirtualScreenHeight;
            _controlCardResetOverlayWindow.Show();
        }

        private void HideControlCardResetOverlayWindow()
        {
            if (_controlCardResetOverlayWindow?.IsVisible == true)
            {
                _controlCardResetOverlayWindow.Hide();
            }
        }

        private System.Windows.Window CreateControlCardResetOverlayWindow()
        {
            var root = new Grid
            {
                Background = new SolidColorBrush(Color.FromArgb(0xAA, 0x00, 0x00, 0x00))
            };

            var card = new Border
            {
                Width = 520,
                Padding = new Thickness(42, 36, 42, 36),
                CornerRadius = new CornerRadius(24),
                Background = new SolidColorBrush(Color.FromArgb(0xF5, 0xFF, 0xFF, 0xFF)),
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var content = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center
            };

            content.Children.Add(new ProgressBar
            {
                Width = 320,
                Height = 8,
                IsIndeterminate = true,
                Foreground = new SolidColorBrush(Color.FromRgb(0x21, 0x96, 0xF3)),
                Margin = new Thickness(0, 0, 0, 26)
            });

            content.Children.Add(new TextBlock
            {
                Text = "复位中，不允许操作",
                FontSize = 30,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x22, 0x22, 0x22)),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            });

            _controlCardResetOverlayWindowMessage = new TextBlock
            {
                Text = ControlCardResetOverlayMessage,
                FontSize = 18,
                Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 18, 0, 0)
            };
            content.Children.Add(_controlCardResetOverlayWindowMessage);

            card.Child = content;
            root.Children.Add(card);

            return new System.Windows.Window
            {
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                Topmost = true,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                Left = SystemParameters.VirtualScreenLeft,
                Top = SystemParameters.VirtualScreenTop,
                Width = SystemParameters.VirtualScreenWidth,
                Height = SystemParameters.VirtualScreenHeight,
                Content = root
            };
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
