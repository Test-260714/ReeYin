using DryIoc.Messages;
using NLog;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ReeYin_V.Logger
{
    //使用说明
    //https://blog.csdn.net/liyou123456789/article/details/125392815
    public static class Logs
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public static void LogTrace(string message)
        {
            logger.Trace(message);
        }

        public static void LogWarning(string message)
        {
            logger.Warn(message);
        }

        public static void LogError(string message)
        {
            logger.Error(message);
        }

        public static void LogFatal(string message)
        {
            logger.Fatal(message);
        }

        public static void LogInfo(string message)
        {
            logger.Info(message);
        }

        public static void LogError(Exception exception)
        {
            logger.Error(exception.Message + exception.StackTrace);
        }
    }

    /// <summary>
    /// NLog 日志级别枚举（与 NLog.LogLevel 对应）
    /// </summary>
    public enum NLogLevel
    {
        Trace,
        Debug,
        Info,
        Warn,
        Error,
        Fatal
    }

    /// <summary>
    /// 彩色日志事件参数
    /// </summary>
    public class ColoredLogEventArgs : EventArgs
    {
        public string Message { get; set; }
        public NLogLevel Level { get; set; }
        public DateTime Timestamp { get; set; }
    }

    [Target("WpfTextBox")]
    public class WpfTextBoxTarget : TargetWithLayout
    {
        public static TextBox LogTextBox;

        /// <summary>
        /// 彩色日志事件，供 LogView 订阅
        /// </summary>
        public static event EventHandler<ColoredLogEventArgs> ColoredLogReceived;

        protected override void Write(LogEventInfo logEvent)
        {
            if (LogTextBox == null)
                return;

            string msg = this.Layout.Render(logEvent);

            // 触发彩色日志事件
            var nlogLevel = ConvertToNLogLevel(logEvent.Level);
            ColoredLogReceived?.Invoke(this, new ColoredLogEventArgs
            {
                Message = msg,
                Level = nlogLevel,
                Timestamp = logEvent.TimeStamp
            });

            // 保持原有的 TextBox 输出（用于触发 LogView 的转换逻辑）
            LogTextBox.Dispatcher.BeginInvoke(new Action(() =>
            {
                LogTextBox.AppendText(msg + Environment.NewLine);
                LogTextBox.ScrollToEnd();
            }));
        }

        /// <summary>
        /// 将 NLog.LogLevel 转换为自定义的 NLogLevel 枚举
        /// </summary>
        private NLogLevel ConvertToNLogLevel(NLog.LogLevel level)
        {
            if (level == NLog.LogLevel.Trace) return NLogLevel.Trace;
            if (level == NLog.LogLevel.Debug) return NLogLevel.Debug;
            if (level == NLog.LogLevel.Info) return NLogLevel.Info;
            if (level == NLog.LogLevel.Warn) return NLogLevel.Warn;
            if (level == NLog.LogLevel.Error) return NLogLevel.Error;
            if (level == NLog.LogLevel.Fatal) return NLogLevel.Fatal;
            return NLogLevel.Info;
        }
    }
}
