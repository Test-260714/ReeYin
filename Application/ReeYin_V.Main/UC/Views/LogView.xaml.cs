using ReeYin_V.Logger;
using ReeYin_V.Logger.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;

namespace ReeYin_V.Main.UC.Views
{
    /// <summary>
    /// LogView.xaml 的交互逻辑
    /// </summary>
    public partial class LogView : UserControl
    {
        private bool _autoScroll = true;
        private const int MaxLogLines = 1000; // 最大日志行数，防止内存溢出
        private int _currentLogCount = 0;
        private NLogLevel _currentFilter = NLogLevel.Trace; // 当前过滤级别

        // NLog 日志级别颜色配置（与 NLog 级别对应）
        private readonly Dictionary<NLogLevel, Brush> _logColors = new Dictionary<NLogLevel, Brush>
        {
            { NLogLevel.Trace, new SolidColorBrush(Color.FromRgb(180, 180, 180)) },   // 浅灰色
            { NLogLevel.Debug, new SolidColorBrush(Color.FromRgb(128, 128, 128)) },   // 灰色
            { NLogLevel.Info, new SolidColorBrush(Color.FromRgb(0, 150, 255)) },      // 蓝色
            { NLogLevel.Warn, new SolidColorBrush(Color.FromRgb(255, 165, 0)) },      // 橙色
            { NLogLevel.Error, new SolidColorBrush(Color.FromRgb(255, 50, 50)) },     // 红色
            { NLogLevel.Fatal, new SolidColorBrush(Color.FromRgb(200, 0, 0)) }        // 深红色
        };

        // NLog 日志级别图标
        private readonly Dictionary<NLogLevel, string> _logIcons = new Dictionary<NLogLevel, string>
        {
            { NLogLevel.Trace, "🔍" },
            { NLogLevel.Debug, "🐛" },
            { NLogLevel.Info, "ℹ️" },
            { NLogLevel.Warn, "⚠️" },
            { NLogLevel.Error, "❌" },
            { NLogLevel.Fatal, "💀" }
        };

        public LogView()
        {
            InitializeComponent();

            // 设置旧的 TextBox 目标（保持兼容性）
            WpfTextBoxTarget.LogTextBox = this.LogTextBox;

            // 订阅 NLog 的彩色日志事件
            WpfTextBoxTarget.ColoredLogReceived += OnColoredLogReceived;

            // 初始化自动滚动
            AutoScrollCheckBox.Checked += (s, e) => _autoScroll = true;
            AutoScrollCheckBox.Unchecked += (s, e) => _autoScroll = false;
        }

        /// <summary>
        /// 处理 NLog 彩色日志事件
        /// </summary>
        private void OnColoredLogReceived(object sender, ColoredLogEventArgs e)
        {
            // 检查是否符合过滤条件
            if ((int)e.Level < (int)_currentFilter)
                return;

            AddColoredLogFromNLog(e.Message, e.Level, e.Timestamp);
        }

        /// <summary>
        /// 添加来自 NLog 的彩色日志
        /// </summary>
        private void AddColoredLogFromNLog(string message, NLogLevel level, DateTime timestamp)
        {
            // 日志 UI 只做异步投递，避免业务线程打日志时被界面刷新反向卡死。
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    // 检查日志数量，超过限制则删除旧日志
                    if (_currentLogCount >= MaxLogLines)
                    {
                        RemoveOldestLog();
                    }

                    // 解析 NLog 格式的消息：2026-02-10 14:23:45.1234 | INFO | 消息内容
                    var parts = message.Split(new[] { " | " }, StringSplitOptions.None);
                    string displayMessage = message;
                    string timeStr = timestamp.ToString("HH:mm:ss.fff");

                    if (parts.Length >= 3)
                    {
                        // 提取时间、级别和消息
                        timeStr = parts[0].Split(' ').LastOrDefault()?.Substring(0, 12) ?? timeStr;
                        displayMessage = string.Join(" | ", parts.Skip(2));
                    }

                    // 创建新的段落 - 所有内容在一行显示
                    var paragraph = new Paragraph
                    {
                        Margin = new Thickness(0),
                        Padding = new Thickness(2, 1, 2, 1),
                        LineHeight = 1  // 确保单行显示
                    };

                    // 添加图标（内联显示）
                    var iconRun = new Run(_logIcons[level])
                    {
                        FontSize = 14,
                        BaselineAlignment = BaselineAlignment.Center
                    };
                    paragraph.Inlines.Add(iconRun);

                    // 添加时间戳（内联显示）
                    var timeRun = new Run($" [{timeStr}]")
                    {
                        Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                        FontWeight = FontWeights.Normal,
                        FontSize = 11
                    };
                    paragraph.Inlines.Add(timeRun);

                    // 添加日志级别标签（内联显示）
                    var levelTag = GetLevelTag(level);
                    var levelRun = new Run($" {levelTag}")
                    {
                        Foreground = _logColors[level],
                        FontWeight = FontWeights.Bold,
                        FontSize = 11
                    };
                    paragraph.Inlines.Add(levelRun);

                    // 添加日志内容（内联显示）
                    var messageRun = new Run($" {displayMessage}")
                    {
                        Foreground = _logColors[level],
                        FontSize = 12
                    };
                    paragraph.Inlines.Add(messageRun);

                    // 添加到文档
                    LogDocument.Blocks.Add(paragraph);
                    _currentLogCount++;

                    // 自动滚动到底部
                    if (_autoScroll)
                    {
                        LogScrollViewer.ScrollToEnd();
                    }
                }
                catch (Exception ex)
                {
                    // 防止日志系统本身出错
                    System.Diagnostics.Debug.WriteLine($"日志添加失败: {ex.Message}");
                }
            }));
        }

        /// <summary>
        /// 获取日志级别标签
        /// </summary>
        private string GetLevelTag(NLogLevel level)
        {
            return level switch
            {
                NLogLevel.Trace => "[跟踪]",
                NLogLevel.Debug => "[调试]",
                NLogLevel.Info => "[信息]",
                NLogLevel.Warn => "[警告]",
                NLogLevel.Error => "[错误]",
                NLogLevel.Fatal => "[致命]",
                _ => "[日志]"
            };
        }

        /// <summary>
        /// 删除最旧的日志
        /// </summary>
        private void RemoveOldestLog()
        {
            if (LogDocument.Blocks.Count > 0)
            {
                LogDocument.Blocks.Remove(LogDocument.Blocks.FirstBlock);
                _currentLogCount--;
            }
        }

        /// <summary>
        /// 清空日志按钮点击事件
        /// </summary>
        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            LogDocument.Blocks.Clear();
            _currentLogCount = 0;
        }

        /// <summary>
        /// 日志级别过滤器变化事件
        /// </summary>
        private void LogLevelFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LogLevelFilter.SelectedIndex < 0)
                return;

            // 设置过滤级别
            _currentFilter = LogLevelFilter.SelectedIndex switch
            {
                0 => NLogLevel.Trace,  // 全部
                1 => NLogLevel.Debug,  // 调试
                2 => NLogLevel.Info,   // 信息
                3 => NLogLevel.Warn,   // 警告
                4 => NLogLevel.Error,  // 错误
                _ => NLogLevel.Trace
            };

            // 可选：重新过滤现有日志
            // FilterExistingLogs();
        }

        /// <summary>
        /// 滚动条变化事件
        /// </summary>
        private void LogScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // 如果用户手动向上滚动，暂停自动滚动
            if (e.ExtentHeightChange == 0)
            {
                // 用户手动滚动
                if (LogScrollViewer.VerticalOffset < LogScrollViewer.ScrollableHeight - 10)
                {
                    _autoScroll = false;
                    AutoScrollCheckBox.IsChecked = false;
                }
                else
                {
                    _autoScroll = true;
                    AutoScrollCheckBox.IsChecked = true;
                }
            }
        }

        /// <summary>
        /// 兼容旧代码的 TextChanged 事件
        /// </summary>
        private void LogTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            // NLog 事件已经处理，这里不需要额外处理
            // 清空 TextBox 避免占用内存
            if (LogTextBox.Text.Length > 1000)
            {
                LogTextBox.Clear();
            }
        }

        /// <summary>
        /// 公共方法：手动添加不同级别的日志（用于非 NLog 场景）
        /// </summary>
        public void AddTraceLog(string message) => AddColoredLogFromNLog(message, NLogLevel.Trace, DateTime.Now);
        public void AddDebugLog(string message) => AddColoredLogFromNLog(message, NLogLevel.Debug, DateTime.Now);
        public void AddInfoLog(string message) => AddColoredLogFromNLog(message, NLogLevel.Info, DateTime.Now);
        public void AddWarningLog(string message) => AddColoredLogFromNLog(message, NLogLevel.Warn, DateTime.Now);
        public void AddErrorLog(string message) => AddColoredLogFromNLog(message, NLogLevel.Error, DateTime.Now);
        public void AddFatalLog(string message) => AddColoredLogFromNLog(message, NLogLevel.Fatal, DateTime.Now);
    }
}
