using Prism.Dialogs;
using Prism.Events;
using Prism.Navigation.Regions;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services;
using ReeYin_V.Core.Services.Alarm.Models;
using ReeYin_V.Core.Services.Language;
using ReeYin_V.Core.Services.User;
using ReeYin_V.UI.Style.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace ReeYin_V.UI.Style.CustomWins
{

    public enum DWMWINDOWATTRIBUTE
    {
        DWMWA_NCRENDERING_ENABLED = 1,
        DWMWA_NCRENDERING_POLICY,
        DWMWA_TRANSITIONS_FORCEDISABLED,
        DWMWA_ALLOW_NCPAINT,
        DWMWA_CAPTION_BUTTON_BOUNDS,
        DWMWA_NONCLIENT_RTL_LAYOUT,
        DWMWA_FORCE_ICONIC_REPRESENTATION,
        DWMWA_FLIP3D_POLICY,
        DWMWA_EXTENDED_FRAME_BOUNDS,
        DWMWA_HAS_ICONIC_BITMAP,
        DWMWA_DISALLOW_PEEK,
        DWMWA_EXCLUDED_FROM_PEEK,
        DWMWA_CLOAK,
        DWMWA_CLOAKED,
        DWMWA_FREEZE_REPRESENTATION,
        DWMWA_PASSIVE_UPDATE_MODE,
        DWMWA_USE_HOSTBACKDROPBRUSH,
        DWMWA_USE_IMMERSIVE_DARK_MODE = 20, // Windows 10 版本 1809 和更高版本
        DWMWA_WINDOW_CORNER_PREFERENCE = 33, // Windows 11 和更高版本
        DWMWA_BORDER_COLOR = 34,             // Windows 11 和更高版本
        DWMWA_CAPTION_COLOR = 35,            // Windows 11 和更高版本
        DWMWA_TEXT_COLOR = 36,               // Windows 11 和更高版本
        DWMWA_VISIBLE_FRAME_BORDER_THICKNESS = 37, // Windows 11 和更高版本
        DWMWA_SYSTEMBACKDROP_TYPE = 38,      // Windows 11 和更高版本
        DWMWA_LAST
    }

    /// <summary>
    /// 用于为自定义控件提供注册依赖属性,样式,事件等功能
    /// </summary>
    public class ElementBase
    {
        /// <summary>
        /// 注册属性
        /// public static readonly DependencyProperty Property = Utility.Property<T,T>(string,T);
        /// </summary>
        /// <typeparam name="thisType">this</typeparam>
        /// <typeparam name="propertyType">如：string，bool</typeparam>
        /// <param name="name">属性名</param>
        /// <param name="defaultValue">属性值</param>
        /// <returns></returns>
        public static DependencyProperty Property<thisType, propertyType>(string name, propertyType defaultValue)
        {
            return DependencyProperty.Register(name.Replace("Property", ""), typeof(propertyType), typeof(thisType), new PropertyMetadata(defaultValue));
        }

        /// <summary>
        /// 注册属性
        /// public static readonly DependencyProperty Property = Utility.Property<T,T>(string,T);
        /// </summary>
        /// <typeparam name="thisType"></typeparam>
        /// <typeparam name="propertyType"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public static DependencyProperty Property<thisType, propertyType>(string name)
        {
            return DependencyProperty.Register(name.Replace("Property", ""), typeof(propertyType), typeof(thisType));
        }

        /// <summary>
        /// 注册事件
        /// public static readonly RoutedEvent NameRoutedEvent = Utility.RoutedEvent<T,T>(string,T);
        /// public event EventHandler Name { add { AddHandler(EventHandler, value); } remove { RemoveHandler(EventHandler, value); } }
        /// </summary>
        /// <typeparam name="thisType"></typeparam>
        /// <typeparam name="propertyType"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public static RoutedEvent RoutedEvent<thisType, propertyType>(string name)
        {
            return EventManager.RegisterRoutedEvent(name.Replace("Event", ""), RoutingStrategy.Bubble, typeof(propertyType), typeof(thisType));
        }

        /// <summary>
        /// 默认样式
        /// Utility.DefaultStyle<T>(DefaultStyleKeyProperty);
        /// </summary>
        /// <typeparam name="thisType">this</typeparam>
        /// <param name="dp">DefaultStyleKeyProperty</param>
        public static void DefaultStyle<thisType>(DependencyProperty dp)
        {
            dp.OverrideMetadata(typeof(thisType), new FrameworkPropertyMetadata(typeof(thisType)));
        }

        /// <summary>
        /// 初始化一个 Command
        /// </summary>
        /// <typeparam name="thisType"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public static RoutedUICommand Command<thisType>(string name)
        {
            return new RoutedUICommand(name, name, typeof(thisType));
        }

        /// <summary>
        /// 切换状态
        /// </summary>
        /// <param name="element"></param>
        /// <param name="state"></param>
        public static string GoToState(FrameworkElement element, string state)
        {
            VisualStateManager.GoToState(element, state, false);
            return state;
        }
    }

    [TemplatePart(Name = "PART_MinimizedButton", Type = typeof(Button))]
    [TemplatePart(Name = "PART_ModeSwitch", Type = typeof(Button))]
    [TemplatePart(Name = "PART_AlarmButton", Type = typeof(Button))]
    [TemplatePart(Name = "PART_SettingButton", Type = typeof(Button))]
    [TemplatePart(Name = "PART_MaximizedButton", Type = typeof(Button))]
    [TemplatePart(Name = "PART_NormalButton", Type = typeof(Button))]
    [TemplatePart(Name = "PART_CloseButton", Type = typeof(Button))]
    [TemplatePart(Name = "PART_SettingMenu", Type = typeof(ContextMenu))]
    [TemplatePart(Name = "PART_SwitchUserButton", Type = typeof(Button))]
    public class VisionWindow : Window
    {
        #region Win32 API 声明

        #region user32
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);
        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);
        /// <summary>
        /// 用于显示或隐藏窗口
        /// 控制窗口的显示状态，如最大化，最小化，隐藏等
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="nCmdShow"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        /// <summary>
        /// 用于像窗口发送消息
        /// 向指定窗口的的发送消息用于控制窗口行为或与窗口进行通信
        /// </summary>
        /// <param name="hWnd">窗口的句柄</param>
        /// <param name="Msg">消息标识符</param>
        /// <param name="wParam">附加消息参数</param>
        /// <param name="lParam">附加消息参数</param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        // 引入 user32.dll 中的 GetWindowLong 和 SetWindowLong 函数
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        // 引入 user32.dll 中的 SetWindowPos 函数
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        #endregion

        #region dwmapi
        /// <summary>
        /// 用于设置窗口的属性
        /// 设置窗口的特定属性，例如窗口的额圆角偏好
        /// </summary>
        /// <param name="hwnd">窗口的句柄</param>
        /// <param name="dwAttribute">要设置的属性标志</param>
        /// <param name="pvAttribute">属性的值</param>
        /// <param name="cbAttribute">属性值的大小(以字节单位)</param>
        /// <returns></returns>
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

        /// <summary>
        /// 用于将窗口的玻璃扩展区域到客户区
        /// 将窗口的玻璃边框扩展到客户区，实现无边框或自定义边框效果。
        /// </summary>
        /// <param name="hwnd">窗口的句柄</param>
        /// <param name="pMarInset">指定边框的扩展的宽度</param>
        /// <returns></returns>
        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margins pMarInset);

        /// <summary>
        /// 设置圆角样式
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="dwAttribute">窗口圆角属性</param>
        /// <param name="pvAttribute">圆角样式</param>
        /// <param name="cbAttribute"></param>
        /// <returns></returns>
        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, uint cbAttribute);

        /// <summary>
        /// 用于DWM(桌面窗口管理器)是否启用
        /// 将窗口的便利宽度扩展到客户区，实现无边框自定义边框效果
        /// </summary>
        /// <param name="pfEnabled"></param>
        /// <returns></returns>
        [DllImport("dwmapi.dll")]
        private static extern int DwmIsCompositionEnabled(out bool pfEnabled);
        #endregion

        /// <summary>
        /// 定义一个结构体Margins,用于指定窗口边缘的扩展宽度
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct Margins
        {
            public int cxLeftWidth;//左边宽的宽度
            public int cxRightWidth;//右边框的宽度
            public int cyTopHeight;//上边框的高度
            public int cyBottomHeight;//下边框的高度
        }


        #endregion

        #region 常量

        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;

        private const int SW_MINIMIZE = 6;
        private const int SW_MAXIMIZE = 3;
        private const int SW_NORMAL = 1;

        private const int WM_CLOSE = 0x0010;
        private const int GWL_STYLE = -16;
        private const int GWL_EXSTYLE = -20;
        private const int WS_BORDER = 0x00800000;
        private const int WS_THICKFRAME = 0x00040000;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_FRAMECHANGED = 0x0020;
        private const int WM_NCHITTEST = 0x0084;
        private const int HTCLIENT = 1;
        private const int HTCAPTION = 2;
        #endregion

        #region 公共属性
        public object ReturnValue { get; set; } //= null;
        public bool EscClose { get; set; } //= false;
        private bool _isResizing = false; // 用于标识是否在调整窗口大小中
        private bool isResizing = false;

        /// <summary>
        /// 设计模式
        /// </summary>
        private bool _isDesignMode = false;
        #endregion

        #region 系统按钮
        /// <summary>
        /// 系统控件命名
        /// </summary>
        private const string MinimizedButton = "PART_MinimizedButton";
        private const string SettingButton = "PART_SettingButton";
        private const string ModeSwitchButton = "PART_ModeSwitch";
        private const string AlarmButton = "PART_AlarmButton";
        private const string MaximizedButton = "PART_MaximizedButton";
        private const string NormalButton = "PART_NormalButton";
        private const string CloseButton = "PART_CloseButton";
        private const string SettingMenu = "PART_SettingMenu";
        private const string SwitchUserButton = "PART_SwitchUserButton";
        /// <summary>
        /// 系统按钮
        /// </summary>
        private Button _MinimizedButton;
        private Button _SettingButton;
        private Button _ModeSwitchButton;
        private Button _AlarmButton;
        private Button _MaximizedButton;
        private Button _NormalButton;
        private Button _CloseButton;
        private ContextMenu _SettingMenu;
        private Button _SwitchUserButton;
        private readonly DispatcherTimer _alarmBlinkTimer;
        private bool _alarmBlinkState;
        private bool _alarmStateInitialized;
        private Brush _alarmButtonDefaultForeground;
        private static readonly Brush AlarmButtonAlertBackgroundBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0x2D, 0x20));
        private static readonly Brush AlarmButtonAlertBorderBrush = new SolidColorBrush(Color.FromRgb(0xF9, 0x70, 0x66));
        #endregion

        #region 系统依赖属性
        public static readonly DependencyProperty IsSubWindowShowProperty = ElementBase.Property<VisionWindow, bool>("IsSubWindowShowProperty", false);
        public static readonly DependencyProperty MenuProperty = ElementBase.Property<VisionWindow, object>("MenuProperty", null);
        public static readonly new DependencyProperty BorderBrushProperty = ElementBase.Property<VisionWindow, Brush>("BorderBrushProperty");
        public static readonly DependencyProperty TitleForegroundProperty = ElementBase.Property<VisionWindow, Brush>("TitleForegroundProperty");
        public static readonly DependencyProperty TitleFontSizeProperty = ElementBase.Property<VisionWindow, FontSizeConverter>("TitleFontSizeProperty");
        public static readonly DependencyProperty SysButtonColorProperty = ElementBase.Property<VisionWindow, Brush>("SysButtonColorProperty");
        public static readonly DependencyProperty SysButtonVisibleProperty = ElementBase.Property<VisionWindow, Visibility>("SysButtonVisibleProperty");
        public static readonly DependencyProperty SysButtonMarginProperty = ElementBase.Property<VisionWindow, Thickness>("SysButtonMarginProperty");
        public static readonly DependencyProperty CustomButtonsVisibilityProperty = ElementBase.Property<VisionWindow, Visibility>("CustomButtonsVisibilityProperty", Visibility.Visible);
        public static readonly DependencyProperty WindowHeightProperty = ElementBase.Property<VisionWindow, double>("WindowHeightProperty", 30);
        public static readonly DependencyProperty TitleBrushProperty = ElementBase.Property<VisionWindow, Brush>("TitleBrushProperty");
        public static readonly DependencyProperty DongleProperty = ElementBase.Property<VisionWindow, string>("DongleProperty", "Not");//加密狗开发属性
        public static readonly DependencyProperty FormulationProperty = ElementBase.Property<VisionWindow, string>("FormulationProperty", "");//当前配方属性
        public static readonly DependencyProperty ThisUserProperty = ElementBase.Property<VisionWindow, string>("ThisUserProperty", "No users");//当前配方属性
        public static readonly DependencyProperty ModeNameProperty = ElementBase.Property<VisionWindow, string>("ModeNameProperty", "设计模式");//运行模式名称
        public static readonly DependencyProperty AlarmButtonTextProperty = ElementBase.Property<VisionWindow, string>("AlarmButtonTextProperty", "报警管理");
        public static readonly DependencyProperty AlarmActiveCountProperty = ElementBase.Property<VisionWindow, int>("AlarmActiveCountProperty", 0);
        public static readonly DependencyProperty IsAlarmActiveProperty = ElementBase.Property<VisionWindow, bool>("IsAlarmActiveProperty", false);
        public static readonly DependencyProperty AlarmBlinkIntervalProperty = DependencyProperty.Register(
            nameof(AlarmBlinkInterval),
            typeof(TimeSpan),
            typeof(VisionWindow),
            new PropertyMetadata(TimeSpan.FromMilliseconds(600), OnAlarmBlinkIntervalChanged));
        public static readonly DependencyProperty BorderColorProperty = DependencyProperty.Register(nameof(BorderColor), typeof(Brush), typeof(VisionWindow), new PropertyMetadata(Brushes.Transparent, OnBorderColorChanged));
        #endregion

        #region 系统依赖属性(访问)
        /// <summary>
        /// 标题栏高度
        /// </summary>
        public double WindowHeight
        {
            get { return (double)GetValue(WindowHeightProperty); }
            set { SetValue(WindowHeightProperty, value); }
        }

        /// <summary>
        /// 用户切换子窗体的显示状态
        /// </summary>
        public bool IsSubWindowShow
        {
            get { return (bool)GetValue(IsSubWindowShowProperty); }
            set { SetValue(IsSubWindowShowProperty, value); GoToState(); }
        }

        /// <summary>
        /// 绑定菜单对象
        /// </summary>
        public object Menu
        {
            get { return GetValue(MenuProperty); }
            set { SetValue(MenuProperty, value); }
        }

        /// <summary>
        /// 设置窗口边边框颜色
        /// </summary>
        public new Brush BorderBrush
        {
            get { return (Brush)GetValue(BorderBrushProperty); }
            set { SetValue(BorderBrushProperty, value); }
        }

        /// <summary>
        /// 设置标题栏颜色
        /// </summary>
        public Brush TitleBrush
        {
            get { return (Brush)GetValue(TitleBrushProperty); }
            set { SetValue(TitleBrushProperty, value); }
        }

        /// <summary>
        /// 设置标题栏文字颜色
        /// </summary>
        public Brush TitleForeground
        {
            get { return (Brush)GetValue(TitleForegroundProperty); }
            set { SetValue(TitleForegroundProperty, value); }
        }

        /// <summary>
        /// 设置系统按钮（关闭、最小化等）的颜色。
        /// </summary>
        public Brush SysButtonColor
        {
            get { return (Brush)GetValue(SysButtonColorProperty); }
            set { SetValue(SysButtonColorProperty, value); }
        }

        public Visibility SysButtonVisible
        {
            get { return (Visibility)GetValue(SysButtonVisibleProperty); }
            set { SetValue(SysButtonVisibleProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public Thickness SysButtonMargin
        {
            get { return (Thickness)GetValue(SysButtonMarginProperty); }
            set { SetValue(SysButtonMarginProperty, value); }
        }

        /// <summary>
        /// 自定义标题按钮显示状态
        /// </summary>
        public Visibility CustomButtonsVisibility
        {
            get { return (Visibility)GetValue(CustomButtonsVisibilityProperty); }
            set { SetValue(CustomButtonsVisibilityProperty, value); }
        }

        /// <summary>
        /// 设置标题字体大小
        /// </summary>
        public FontSizeConverter TitleFontSize
        {
            get { return (FontSizeConverter)GetValue(TitleFontSizeProperty); }
            set { SetValue(TitleFontSizeProperty, value); }
        }

        /// <summary>
        /// 加密狗状态
        /// </summary>
        public string Dongle
        {
            get { return (string)GetValue(DongleProperty); }
            set { SetValue(DongleProperty, value); }
        }

        /// <summary>
        /// 模式名称
        /// </summary>
        public string ModeName
        {
            get { return (string)GetValue(ModeNameProperty); }
            set { SetValue(ModeNameProperty, value); }
        }

        /// <summary>
        /// 报警按钮文本
        /// </summary>
        public string AlarmButtonText
        {
            get { return (string)GetValue(AlarmButtonTextProperty); }
            set { SetValue(AlarmButtonTextProperty, value); }
        }

        /// <summary>
        /// 当前活动报警数量
        /// </summary>
        public int AlarmActiveCount
        {
            get { return (int)GetValue(AlarmActiveCountProperty); }
            set { SetValue(AlarmActiveCountProperty, value); }
        }

        /// <summary>
        /// 当前是否存在活动报警
        /// </summary>
        public bool IsAlarmActive
        {
            get { return (bool)GetValue(IsAlarmActiveProperty); }
            set { SetValue(IsAlarmActiveProperty, value); }
        }

        /// <summary>
        /// 报警按钮闪烁间隔
        /// </summary>
        public TimeSpan AlarmBlinkInterval
        {
            get { return (TimeSpan)GetValue(AlarmBlinkIntervalProperty); }
            set { SetValue(AlarmBlinkIntervalProperty, value); }
        }

        /// <summary>
        /// 当前配方
        /// </summary>
        public string Formulation
        {
            get { return (string)GetValue(FormulationProperty); }
            set { SetValue(FormulationProperty, value); }
        }

        /// <summary>
        /// 当前用户
        /// </summary>
        public string ThisUser
        {
            get { return (string)GetValue(ThisUserProperty); }
            set { SetValue(ThisUserProperty, value); }
        }

        /// <summary>
        /// Win11系统专用圆角颜色
        /// </summary>
        public Brush BorderColor
        {
            get => (Brush)GetValue(BorderColorProperty);
            set => SetValue(BorderColorProperty, value);
        }
        #endregion

        #region 构造函数
        static VisionWindow()
        {
            //调用ElementBase.DefaultStyle<T>注册默认样式,使MetroWindow的外观自定义模板关联,样式定义通常定义在XAML文件，语序开发者修改窗口的外观
            ElementBase.DefaultStyle<VisionWindow>(DefaultStyleKeyProperty);

        }

        public VisionWindow()
        {
            _alarmBlinkTimer = new DispatcherTimer
            {
                Interval = NormalizeAlarmBlinkInterval(AlarmBlinkInterval)
            };
            _alarmBlinkTimer.Tick += AlarmBlinkTimer_Tick;

            // 修复WindowChrome导致的窗口大小错误
            var sizeToContent = SizeToContent.Manual;
            // 去除系统默认的标题栏
            Loaded += (ss, ee) =>
            {
                sizeToContent = SizeToContent;
                BorderColor = BorderBrush;
            };
            Loaded += async (ss, ee) =>
            {
                await InitializeAlarmStateAsync();
            };
            ContentRendered += (ss, ee) =>
            {
                SizeToContent = SizeToContent.Manual;
                Width = ActualWidth;
                Height = ActualHeight;
                SizeToContent = sizeToContent;
            };
            //按下ESC关闭窗口
            KeyUp += delegate (object sender, KeyEventArgs e)
            {
                if (e.Key == Key.Escape && EscClose)
                {
                    Close();
                }
            };
            //阻止在默写模式下最大化窗口System.InvalidOperationException:“ClipToBounds 对于 Window 无效。”
            StateChanged += delegate
            {
                if (ResizeMode == ResizeMode.CanMinimize || ResizeMode == ResizeMode.NoResize)
                {
                    if (WindowState == WindowState.Maximized)
                    {
                        WindowState = WindowState.Normal;
                    }
                }
            };

            this.SourceInitialized += VisionWindow_SourceInitialized;
            Closed += VisionWindow_Closed;
            //功能
            //1.修复窗口大小错误:在窗口加载完成后,通过调用SizeToContent确保窗口大小正确
            //按ESC键关闭窗口
            //限制窗口状态
            // 监听窗口大小改变事件
        }

        #endregion

        #region 窗口事件
        private void VisionWindow_SourceInitialized(object? sender, EventArgs e)
        {
            //获取当前窗口的句柄
            IntPtr hwnd = new WindowInteropHelper(this).Handle;

            // 检查 DWM 是否启用
            bool isDwmEnabled;
            DwmIsCompositionEnabled(out isDwmEnabled);
            if (!isDwmEnabled)
            {
                //如果 DWM 未启用，则显示警告
                MessageBox.Show("桌面窗口管理器 (DWM) 未启用，圆角效果可能无法正常显示。");
                return;
            }
            if (isDwmEnabled && IsWindows11OrGreater())
            {
                // 设置窗口圆角偏好
                int preferRound = DWMWCP_ROUND;
                DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref preferRound, sizeof(int));
            }
            else
            {
                // 非 Windows 11 或 DWM 不启用时设置直角
                ApplyRectangleCorners(hwnd);
            }


            // 扩展窗口帧到客户区
            Margins margins = new Margins
            {
                cxLeftWidth = (int)11, // 圆角半径
                cxRightWidth = (int)11,
                cyTopHeight = (int)11,
                cyBottomHeight = (int)11
            };
            DwmExtendFrameIntoClientArea(hwnd, ref margins);
            // 设置边框颜色
            SetDwmBorderColor(hwnd, Colors.Red); // 这里设置为红色，可通过依赖属性动态更改
                                                 //this.StateChanged += VisionWindowTwo_StateChanged;

            //注册语言切换事件
            PrismProvider.EventAggregator.GetEvent<SwitchLanguageEvent>().Subscribe(() =>
            {
                PrismProvider.Dispatcher.BeginInvoke(() =>
                {
                    ModeName = PrismProvider.LanguageManager.GetStringResource("DesignMode");
                });
            }, ThreadOption.UIThread);
        }
        #endregion

        #region 重写函数


        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            //获取模板中定义的按钮
            _MinimizedButton = GetTemplateChild(MinimizedButton) as Button;
            _SettingButton = GetTemplateChild(SettingButton) as Button;
            _ModeSwitchButton = GetTemplateChild(ModeSwitchButton) as Button;
            _AlarmButton = GetTemplateChild(AlarmButton) as Button;
            _MaximizedButton = GetTemplateChild(MaximizedButton) as Button;
            _NormalButton = GetTemplateChild(NormalButton) as Button;
            _CloseButton = GetTemplateChild(CloseButton) as Button;
            _SettingMenu = this.FindResource(SettingMenu) as ContextMenu;
            _SwitchUserButton = GetTemplateChild(SwitchUserButton) as Button;
            if (_AlarmButton != null)
            {
                _alarmButtonDefaultForeground ??= _AlarmButton.Foreground;
                _AlarmButton.Click += delegate
                {
                    OpenAlarmManagement();
                };
                UpdateAlarmButtonVisualState();
            }
            //通用设置按钮对应的下拉菜单
            if ( _SettingMenu != null)
            {
                foreach(MenuItem item in _SettingMenu.Items)
                {
                    switch (item.Tag.ToString())
                    {             
                        case "UserManager":
                            item.Click += delegate
                            {
                                //弹窗用户管理页面
                                PrismProvider.DialogService.Show("UserManagerView", new DialogParameters
                                    {
                                        { "Title", "用户管理" },
                                        { "Icon", "\ue6aa" },
                                    }, result =>
                                    {

                                    }, nameof(DialogWindowView));
                            };
                            break;
                        case "LanguageSwitch":
                            {
                                item.Click += delegate
                                {
                                    PrismProvider.Dispatcher.Invoke(() =>
                                    {
                                        //加载主界面
                                        PrismProvider.ModuleManager.LoadModule("ApplicatoinMainModule");
                                        //导航到主区域
                                        PrismProvider.RegionManager.RequestNavigate("MainRegion", "LanguageConfigView");
                                    });
                                };
                            }
                            break;
                        case "StyleSwitch":
                            {
                                item.Click += delegate
                                {
                                    PrismProvider.Dispatcher.Invoke(() =>
                                    {
                                        //加载主界面
                                        PrismProvider.ModuleManager.LoadModule("ApplicatoinMainModule");
                                        //导航到主区域
                                        PrismProvider.RegionManager.RequestNavigate("MainRegion", "StyleConfigView");
                                    });
                                };
                            }
                            break;
                        case "LicenseActivation":
                            {
                                item.Click += delegate
                                {
                                        //加载授权模块
                                        PrismProvider.ModuleManager.LoadModule("ApplicationLicenseModule");

                                        PrismProvider.DialogService.Show("LicenseActivationView", new DialogParameters
                                        {
                                            { "Title", "许可证激活" },
                                            { "Icon", "\ue61c" },
                                        }, _ =>
                                        {
                                        }, nameof(DialogWindowView));
                                };
                            }
                            break;
                        case "ModuleSwitch":

                            break;
                        default:
                            break;
                    }
                }
            }

            //切换用户按钮
            if (_SwitchUserButton != null)
                _SwitchUserButton.Click += delegate
                {
                    //弹窗用户切换页面
                    PrismProvider.DialogService.Show("UserSwitchView", new DialogParameters
                    {
                        { "Title", "切换用户" },
                        { "Icon", "\ue6c6" },
                    }, result =>
                    {

                    }, nameof(DialogWindowView));
                };


            //通用设置按钮
            if (_SettingButton!= null)
                _SettingButton.Click += delegate
                {
                    if (!PrismProvider.User.VerifyCurUserPermission(UserPermission.SuperAdmin))
                    {
                        MessageView.Ins.MessageBoxShow("您无权限操作!", eMsgType.Info);
                        return;
                    }
                    else
                    {
                        _SettingMenu.PlacementTarget = _SettingButton;
                        _SettingMenu.IsOpen = true;
                    }
                };

            //模式切换按钮
            if (_ModeSwitchButton != null)
                _ModeSwitchButton.Click += delegate
                {
                    if (!PrismProvider.User.VerifyCurUserPermission(UserPermission.SuperAdmin))
                    {
                        MessageView.Ins.MessageBoxShow("您无权限操作!", eMsgType.Info);
                        return;
                    }
                    else
                    {
                        if(_isDesignMode)
                        {
                            _isDesignMode = false;
                            PrismProvider.Dispatcher.Invoke(() =>
                            {
                                //加载主界面
                                PrismProvider.ModuleManager.LoadModule("ApplicatoinMainModule");
                                //导航到主区域
                                PrismProvider.RegionManager.RequestNavigate("MainRegion", "RunMainView");
                            });
                            ModeName = PrismProvider.LanguageManager.GetStringResource("RunningMode");
                            //ModeName = "运行模式";
                        }
                        else
                        {
                            _isDesignMode = true;
                            PrismProvider.Dispatcher.Invoke(() =>
                            {
                                //加载主界面
                                PrismProvider.ModuleManager.LoadModule("ApplicatoinMainModule");
                                //导航到主区域
                                PrismProvider.RegionManager.RequestNavigate("MainRegion", "MainView");
                            });
                            ModeName = PrismProvider.LanguageManager.GetStringResource("DesignMode");
                            //ModeName = "设计模式";
                        }

                        //隐藏设计窗体

                        //显示运行窗体

                    }
                };

            if (_MinimizedButton != null)
                _MinimizedButton.Click += delegate { this.WindowState = WindowState.Minimized; };
            if (_MaximizedButton != null)
                _MaximizedButton.Click += delegate { this.WindowState = WindowState.Maximized; this.Padding = new Thickness(2); };
            if (_NormalButton != null)
                _NormalButton.Click += delegate { this.WindowState = WindowState.Normal; this.Padding = new Thickness(0); };
            if (_CloseButton != null)
                _CloseButton.Click += delegate
                {


                    //弹窗通用设置页面
                    PrismProvider.DialogService.ShowDialog("ExitView", new DialogParameters
                        {
                            { "Title", "退出" },
                            { "Icon", "\ue62a" },
                        }, result =>
                        {
                            if (result.Result != ButtonResult.OK)
                            {
                                return;

                            }
                            this.Close();
                        }, nameof(DialogWindowView));
                };
            /*
             * 功能:
             * 1.；绑定按钮:
             * 使用GetTemplateChild获取模板中i当以的按钮控件
             * 为每个控件绑定点击事件,实现最小化,最大化,还原,关闭窗口的功能
             * 2.扩展性:
             * 用户可以呕吐难过自定义模板重新打i那白衣按钮的外观和布局
             */
        }

        private async Task InitializeAlarmStateAsync()
        {
            if (_alarmStateInitialized || PrismProvider.AlarmService == null)
            {
                return;
            }

            _alarmStateInitialized = true;
            PrismProvider.AlarmService.DataChanged += AlarmService_DataChanged;

            try
            {
                AlarmDashboardSnapshot dashboard = await PrismProvider.AlarmService.GetDashboardAsync();
                PrismProvider.Dispatcher.BeginInvoke(() =>
                {
                    ApplyAlarmState(dashboard.ActiveCount);
                });
            }
            catch
            {
                ApplyAlarmState(0);
            }
        }

        private void AlarmService_DataChanged(object sender, AlarmDataChangedEventArgs e)
        {
            PrismProvider.Dispatcher.BeginInvoke(() =>
            {
                ApplyAlarmState(e?.Dashboard?.ActiveCount ?? 0);
            });
        }

        private void ApplyAlarmState(int activeCount)
        {
            AlarmActiveCount = Math.Max(0, activeCount);
            IsAlarmActive = AlarmActiveCount > 0;
            AlarmButtonText = IsAlarmActive ? $"报警管理({AlarmActiveCount})" : "报警管理";
            UpdateAlarmButtonVisualState();
        }

        private void UpdateAlarmButtonVisualState()
        {
            if (_AlarmButton == null)
            {
                return;
            }

            _alarmButtonDefaultForeground ??= _AlarmButton.Foreground;
            _alarmBlinkTimer.Interval = NormalizeAlarmBlinkInterval(AlarmBlinkInterval);

            if (!IsAlarmActive)
            {
                _alarmBlinkTimer.Stop();
                _alarmBlinkState = false;
                ApplyAlarmButtonAppearance(false);
                return;
            }

            if (!_alarmBlinkTimer.IsEnabled)
            {
                _alarmBlinkState = true;
                ApplyAlarmButtonAppearance(true);
                _alarmBlinkTimer.Start();
            }
        }

        private void AlarmBlinkTimer_Tick(object sender, EventArgs e)
        {
            if (!IsAlarmActive)
            {
                _alarmBlinkTimer.Stop();
                ApplyAlarmButtonAppearance(false);
                return;
            }

            _alarmBlinkState = !_alarmBlinkState;
            ApplyAlarmButtonAppearance(_alarmBlinkState);
        }

        private void ApplyAlarmButtonAppearance(bool highlight)
        {
            if (_AlarmButton == null)
            {
                return;
            }

            _AlarmButton.Background = highlight ? AlarmButtonAlertBackgroundBrush : Brushes.Transparent;
            _AlarmButton.BorderBrush = highlight ? AlarmButtonAlertBorderBrush : Brushes.Transparent;
            _AlarmButton.Foreground = highlight ? Brushes.White : (_alarmButtonDefaultForeground ?? TitleForeground ?? SysButtonColor ?? Brushes.White);
            _AlarmButton.FontWeight = highlight ? FontWeights.SemiBold : FontWeights.Normal;
        }

        private void OpenAlarmManagement()
        {
            PrismProvider.Dispatcher.Invoke(() =>
            {
                PrismProvider.ModuleManager.LoadModule("ApplicationAlarmCenterModule");
                PrismProvider.RegionManager.RequestNavigate("MainRegion", "AlarmWorkbenchShellView");
            });
        }

        private void VisionWindow_Closed(object sender, EventArgs e)
        {
            _alarmBlinkTimer.Stop();

            if (PrismProvider.AlarmService != null)
            {
                PrismProvider.AlarmService.DataChanged -= AlarmService_DataChanged;
            }
        }

        private static TimeSpan NormalizeAlarmBlinkInterval(TimeSpan interval)
        {
            if (interval <= TimeSpan.Zero)
            {
                return TimeSpan.FromMilliseconds(600);
            }

            return interval < TimeSpan.FromMilliseconds(120)
                ? TimeSpan.FromMilliseconds(120)
                : interval;
        }

        private static void OnAlarmBlinkIntervalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VisionWindow window)
            {
                window._alarmBlinkTimer.Interval = NormalizeAlarmBlinkInterval(window.AlarmBlinkInterval);
            }
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            AllowsTransparency = false;
            if (WindowStyle == WindowStyle.None)
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
            }
        }
        #endregion

        #region win11圆角设置
        private void SetDwmBorderColor(IntPtr hwnd, Color color)
        {
            if (!IsWindows11OrGreater())
                return;
            int colorRef = color.R | (color.G << 8) | (color.B << 16); // 转换为 COLORREF 格式
            int result = DwmSetWindowAttribute(hwnd, (int)DWMWINDOWATTRIBUTE.DWMWA_BORDER_COLOR, ref colorRef, sizeof(int));
        }

        // 当 BorderColor 发生改变时，更新窗口边框颜色
        private static void OnBorderColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VisionWindow window && e.NewValue is Brush newColor)
            {
                IntPtr hwnd = new WindowInteropHelper(window).Handle;
                window.SetDwmBorderColor(hwnd, ConvertBrushToColor(newColor));
            }
        }

        public static System.Windows.Media.Color ConvertBrushToColor(System.Windows.Media.Brush brush)
        {
            // 检查是否为 SolidColorBrush 类型
            if (brush is SolidColorBrush solidColorBrush)
            {
                return solidColorBrush.Color;
            }

            // 如果不是 SolidColorBrush，则无法直接转换
            // 根据需求返回默认值（如 Transparent）或抛出异常
            return Colors.Transparent; // 或者根据需求处理
        }
        #endregion

        #region 方法
        /// <summary>
        /// 切换状态
        /// </summary>
        void GoToState()
        {
            /*
             * 功能:
             * 根据IsSubWindwoShow属性值,切换控件的视觉状态
             * 调用ElementBase.GoToState方法,应用定义好的状态样式
             */
            ElementBase.GoToState(this, IsSubWindowShow ? "Enabled" : "Disable");
        }
        /// <summary>
        /// 检查当前是否为 Windows 11 或更高版本
        /// </summary>
        private bool IsWindows11OrGreater()
        {
            // Windows 11 的版本号是 10.0.22000 及以上
            Version win11Version = new Version(10, 0, 22000);
            Version osVersion = Environment.OSVersion.Version;

            return osVersion >= win11Version;
        }

        /// <summary>
        /// 为窗口应用直角
        /// </summary>
        private void ApplyRectangleCorners(IntPtr hwnd)
        {
            IntPtr region = CreateRectRgn(0, 0, (int)this.ActualWidth, (int)this.ActualHeight);
            SetWindowRgn(hwnd, region, true);
        }

        /// <summary>
        /// 为窗口应用圆角
        /// </summary>
        private void ApplyRoundedCorners(IntPtr hwnd, double radius)
        {
            IntPtr region = CreateRoundRectRgn(0, 0, (int)this.ActualWidth, (int)this.ActualHeight, (int)radius, (int)radius);
            SetWindowRgn(hwnd, region, true);
        }
        #endregion
    }

}
