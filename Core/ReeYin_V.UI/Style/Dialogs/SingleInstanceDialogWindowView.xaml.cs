using Prism.Dialogs;
using Prism.Events;
using Prism.Navigation.Regions;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.IOC;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static ReeYin_V.UI.Style.Dialogs.GlobalMouseListener;
using Point = System.Windows.Point;

namespace ReeYin_V.UI.Style.Dialogs
{
    /// <summary>
    /// SingleInstanceDialogWindowView.xaml 的交互逻辑
    /// 注册为非阻塞，但只需要一个窗体
    /// </summary>
    public partial class SingleInstanceDialogWindowView : Window, IDialogWindow
    {
        private const double WindowCornerRadius = 8d;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;

        private static readonly Dictionary<string, SingleInstanceDialogWindowView> _opened
        = new();

        private readonly IRegionManager _scopedRegionManager;
        private string _dialogKey;
        private bool _usesDwmRoundedCorners;

        public IDialogResult Result { get; set; }

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect,
            int nTopRect,
            int nRightRect,
            int nBottomRect,
            int nWidthEllipse,
            int nHeightEllipse);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);

        public SingleInstanceDialogWindowView(IRegionManager regionManager)
        {
            InitializeComponent();

            // 给对话框窗口创建独立作用域的 RegionManager
            // 给这个弹窗创建自己的 RegionManager 作用域

            _scopedRegionManager = regionManager.CreateRegionManager();
            SourceInitialized += OnSourceInitialized;
            SizeChanged += OnSizeChanged;
            Loaded += OnLoaded;
            Closed += OnClosed;

            CommandBindings.Add(new CommandBinding(
                ApplicationCommands.Close, (_, __) => Close()));

            CommandBindings.Add(new CommandBinding(
                SystemCommands.MinimizeWindowCommand,
                (_, __) => SystemCommands.MinimizeWindow(this)));

        }

        #region Methods
        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);

            if (newContent is DependencyObject dep)
            {
                // 关键：把 RM 挂到对话框内容根（你的 ConditionView）上
                RegionManager.SetRegionManager(dep, _scopedRegionManager);

                // 让 Region 立即注册（保险）
                RegionManager.UpdateRegions();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            NormalizeTitle();

            Owner = null;
            ShowInTaskbar = true;

            if (!TryGetDialogAware(out var dialogAware))
                return;

            string dialogKey = GetDialogKey(dialogAware);
            _dialogKey = dialogKey;

            if (_opened.TryGetValue(dialogKey, out var existing) && !ReferenceEquals(existing, this))
            {
                if (TryActivateExistingWindow(existing))
                {
                    Dispatcher.BeginInvoke(new Action(Close));
                    return;
                }

                _opened.Remove(dialogKey);
            }

            _opened[dialogKey] = this;
        }

        private void OnClosed(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_dialogKey)
                && _opened.TryGetValue(_dialogKey, out var existing)
                && ReferenceEquals(existing, this))
            {
                _opened.Remove(_dialogKey);
            }
        }

        private static string GetDialogKey(IDialogAware dialogAware)
        {
            string typeKey = dialogAware.GetType().FullName ?? dialogAware.GetType().Name;

            // Scope node configuration dialogs by Guid; global dialogs without Guid stay single-instance by type.
            if (dialogAware is DialogViewModelBase { Guid: var guid } && guid != Guid.Empty)
            {
                return $"{typeKey}:{guid:D}";
            }

            return typeKey;
        }

        private bool TryGetDialogAware(out IDialogAware dialogAware)
        {
            if (DataContext is IDialogAware dataContextDialogAware)
            {
                dialogAware = dataContextDialogAware;
                return true;
            }

            if (Content is IDialogAware contentDialogAware)
            {
                dialogAware = contentDialogAware;
                return true;
            }

            dialogAware = null;
            return false;
        }

        private static bool TryActivateExistingWindow(Window existing)
        {
            try
            {
                if (PresentationSource.FromVisual(existing) is null)
                {
                    return false;
                }

                if (existing.WindowState == WindowState.Minimized)
                {
                    existing.WindowState = WindowState.Normal;
                }

                existing.Activate();
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private void NormalizeTitle()
        {
            if (string.IsNullOrEmpty(Title) || !Title.Contains("_"))
            {
                return;
            }

            string[] partsWithoutEmpty = Title.Split('_', StringSplitOptions.RemoveEmptyEntries);
            if (partsWithoutEmpty.Length < 2)
            {
                return;
            }

            var resourceTitle = PrismProvider.LanguageManager.GetStringResource(partsWithoutEmpty[1]);
            if (!string.IsNullOrEmpty(resourceTitle))
            {
                Title = partsWithoutEmpty[0] + "-" + resourceTitle;
            }
        }

        private void OnSourceInitialized(object sender, EventArgs e)
        {
            ApplyWindowCorners();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!_usesDwmRoundedCorners)
            {
                ApplyRoundedRegion();
            }
        }

        private void ApplyWindowCorners()
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            _usesDwmRoundedCorners = TryApplyDwmRoundedCorners(hwnd);
            if (!_usesDwmRoundedCorners)
            {
                ApplyRoundedRegion(hwnd);
            }
        }

        private static bool TryApplyDwmRoundedCorners(IntPtr hwnd)
        {
            int preference = DWMWCP_ROUND;
            try
            {
                return DwmSetWindowAttribute(
                    hwnd,
                    DWMWA_WINDOW_CORNER_PREFERENCE,
                    ref preference,
                    sizeof(int)) == 0;
            }
            catch (DllNotFoundException)
            {
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
        }

        private void ApplyRoundedRegion()
        {
            ApplyRoundedRegion(new WindowInteropHelper(this).Handle);
        }

        private void ApplyRoundedRegion(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || ActualWidth <= 0 || ActualHeight <= 0)
            {
                return;
            }

            if (WindowState == WindowState.Maximized)
            {
                SetWindowRgn(hwnd, IntPtr.Zero, true);
                return;
            }

            var transformToDevice = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice;
            double dpiX = transformToDevice?.M11 ?? 1d;
            double dpiY = transformToDevice?.M22 ?? 1d;

            int width = Math.Max(1, (int)Math.Ceiling(ActualWidth * dpiX));
            int height = Math.Max(1, (int)Math.Ceiling(ActualHeight * dpiY));
            int cornerWidth = Math.Max(1, (int)Math.Ceiling(WindowCornerRadius * 2 * dpiX));
            int cornerHeight = Math.Max(1, (int)Math.Ceiling(WindowCornerRadius * 2 * dpiY));

            IntPtr region = CreateRoundRectRgn(0, 0, width + 1, height + 1, cornerWidth, cornerHeight);
            if (region == IntPtr.Zero)
            {
                return;
            }

            if (SetWindowRgn(hwnd, region, true) == 0)
            {
                DeleteObject(region);
            }
        }

        #endregion
    }

}
