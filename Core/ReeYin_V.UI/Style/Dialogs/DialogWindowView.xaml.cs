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
    /// DialogWindowView.xaml 的交互逻辑
    /// </summary>
    public partial class DialogWindowView : Window,IDialogWindow
    {
        private const double WindowCornerRadius = 8d;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;

        private readonly IRegionManager _scopedRegionManager;
        private bool _usesDwmRoundedCorners;

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

        public DialogWindowView(IRegionManager regionManager)
        {
            InitializeComponent();
            _scopedRegionManager = regionManager.CreateRegionManager();
            SourceInitialized += OnSourceInitialized;
            SizeChanged += OnSizeChanged;
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, CloseEvent));
            CommandBindings.Add(new CommandBinding(
                SystemCommands.MinimizeWindowCommand,
                (_, __) => SystemCommands.MinimizeWindow(this)));
        }

        
        public IDialogResult Result { get; set; }

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);

            if (newContent is DependencyObject dep)
            {
                RegionManager.SetRegionManager(dep, _scopedRegionManager);
                RegionManager.UpdateRegions();
            }
        }

        private void CloseEvent(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
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
    }

    public static class GlobalMouseListener
    {
        // Windows API 导入
        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hHook);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hHook, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT pt);

        [DllImport("user32.dll")]
        public static extern bool IsChild(IntPtr hWndParent, IntPtr hWndChild);

        // 鼠标钩子常量和委托
        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_RBUTTONDOWN = 0x0204;

        private static IntPtr _hookId = IntPtr.Zero;
        private static HookProc _hookProc;

        // 监听状态属性
        public static bool IsListening => _hookId != IntPtr.Zero;

        // 鼠标点击事件
        public static event EventHandler<GlobalMouseEventArgs> MouseButtonDown;

        // 启动监听
        public static void Start()
        {
            if (_hookId == IntPtr.Zero)
            {
                _hookProc = HookCallback;
                _hookId = SetWindowsHookEx(WH_MOUSE_LL, _hookProc, GetModuleHandle(null), 0);
            }
        }

        // 停止监听
        public static void Stop()
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
        }

        // 钩子回调函数
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode >= 0 && (wParam == (IntPtr)WM_LBUTTONDOWN || wParam == (IntPtr)WM_RBUTTONDOWN))
                {
                    MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                    POINT screenPoint = hookStruct.pt;

                    // 获取鼠标点击位置的窗口句柄
                    IntPtr hWnd = WindowFromPoint(screenPoint);

                    // 触发事件
                    MouseButtonDown?.Invoke(null, new GlobalMouseEventArgs
                    {
                        ScreenPoint = new Point(screenPoint.x, screenPoint.y),
                        IsLeftButton = wParam == (IntPtr)WM_LBUTTONDOWN,
                        WindowHandle = hWnd
                    });
                }

                return CallNextHookEx(_hookId, nCode, wParam, lParam);
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.StackTrace.ToString());
                return IntPtr.Zero;
            }

        }

        // 鼠标事件参数类
        public class GlobalMouseEventArgs : EventArgs
        {
            public Point ScreenPoint { get; set; }
            public bool IsLeftButton { get; set; }
            public IntPtr WindowHandle { get; set; }
        }

        // 结构体定义
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);
    }

    public static class MaximizeOnClickOutsideBehavior
    {
        // 附加属性：是否启用点击外部最大化功能
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(MaximizeOnClickOutsideBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Window window)
            {
                if ((bool)e.NewValue)
                {
                    // 窗口加载时开始监听
                    window.Loaded += Window_Loaded;
                    window.Unloaded += Window_Unloaded;

                    // 如果窗口已加载，立即开始监听
                    if (window.IsLoaded)
                    {
                        StartListening(window);
                    }
                }
                else
                {
                    // 停止监听
                    window.Loaded -= Window_Loaded;
                    window.Unloaded -= Window_Unloaded;
                    StopListening(window);
                }
            }
        }

        private static void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Window window && GetIsEnabled(window))
            {
                StartListening(window);
            }
        }

        private static void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            if (sender is Window window)
            {
                StopListening(window);
            }
        }

        private static void StartListening(Window window)
        {
            // 首次使用时启动全局鼠标监听
            if (!GlobalMouseListener.IsListening)
            {
                GlobalMouseListener.Start();
            }

            // 注册窗口特定的事件处理
            GlobalMouseListener.MouseButtonDown += Window_MouseButtonDown;
        }

        private static void StopListening(Window window)
        {
            // 取消注册窗口特定的事件处理
            GlobalMouseListener.MouseButtonDown -= Window_MouseButtonDown;

            // 如果没有窗口使用监听，停止全局监听
            if (!IsAnyWindowUsingListener())
            {
                GlobalMouseListener.Stop();
            }
        }

        private static bool IsAnyWindowUsingListener()
        {
            return Application.Current.Windows
                .OfType<Window>()
                .Any(w => GetIsEnabled(w));
        }

        private static void Window_MouseButtonDown(object sender, GlobalMouseEventArgs e)
        {
            try
            {
                // 获取所有启用了该行为的窗口
                var windows = Application.Current.Windows
                    .OfType<Window>()
                    .Where(w => GetIsEnabled(w));

                foreach (var window in windows)
                {
                    if (IsClickOutsideWindow(window, e))
                    {
                        //如果点击在窗口外部，弹出此窗体
                        window.WindowState = WindowState.Normal;
                    }
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.StackTrace.ToString());
            }

        }

        private static bool IsClickOutsideWindow(Window window, GlobalMouseEventArgs e)
        {
            // 获取窗口句柄
            IntPtr windowHandle = new WindowInteropHelper(window).Handle;

            // 检查点击是否在窗口外部
            return e.WindowHandle != windowHandle && !IsChild(windowHandle, e.WindowHandle);
        }
    }
}
