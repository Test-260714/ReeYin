using Prism.Dialogs;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Dm.FldrStatement;

namespace ReeYin_V.Core.Extension
{
    /// <summary>
    /// 扩展一个阻塞窗体的方法
    /// </summary>
    
    public static class IDialogServiceCompatExtensions
    {
        [SupportedOSPlatform("windows7.0")]
        public static void ShowDialog(this IDialogService dialogService, string name, IDialogParameters parameters, Action<IDialogResult> callback, string windowName)
        {
            //EnsureShowNonModalParameter(parameters);
            if (!string.IsNullOrEmpty(windowName))
                parameters.Add(KnownDialogParameters.WindowName, windowName);

            dialogService.ShowDialog(name, parameters, new DialogCallback().OnClose(callback));
        }



        // 原始方法中的辅助方法
        [SupportedOSPlatform("windows7.0")]
        private static IDialogParameters EnsureShowNonModalParameter(IDialogParameters parameters)
        {
            parameters ??= new DialogParameters();
            parameters.Add(KnownDialogParameters.ShowNonModal, true);
            return parameters;
        }
    }

    public class WindowManager
    {
        // 存储已打开的窗口
        private static readonly Dictionary<string, Window> _openWindows = new Dictionary<string, Window>();

        /// <summary>
        /// 打开单实例独立窗口，支持自定义窗口属性
        /// </summary>
        /// <param name="windowType">窗口类型</param>
        /// <param name="windowName">窗口唯一标识名称</param>
        /// <param name="configureWindow">窗口属性配置委托</param>
        public static void OpenSingleInstanceWindow(
            Type windowType,
            string windowName,
            Action<Window> configureWindow = null)
        {
            // 检查窗口是否已打开
            if (_openWindows.TryGetValue(windowName, out var existingWindow) && existingWindow != null)
            {
                // 如果已打开则激活窗口
                if (existingWindow.WindowState == WindowState.Minimized)
                    existingWindow.WindowState = WindowState.Normal;

                existingWindow.Activate();
                return;
            }

            // 创建新窗口实例
            if (Activator.CreateInstance(windowType) is not Window newWindow)
                return;

            // 设置默认独立窗口属性
            newWindow.Owner = null;          // 无所有者
            newWindow.ShowInTaskbar = true;  // 在任务栏显示
            newWindow.Topmost = false;       // 默认不置顶

            // 应用自定义配置（会覆盖默认值）
            configureWindow?.Invoke(newWindow);

            // 窗口关闭时从字典移除
            newWindow.Closed += (s, e) =>
            {
                if (_openWindows.ContainsKey(windowName))
                    _openWindows.Remove(windowName);
            };

            // 添加到已打开窗口字典
            _openWindows[windowName] = newWindow;

            // 显示窗口
            newWindow.Show();
        }

        /// <summary>
        /// 关闭指定窗口
        /// </summary>
        public static void CloseWindow(string windowName)
        {
            if (_openWindows.TryGetValue(windowName, out var window) && window != null)
            {
                window.Close();
            }
        }
    }

    public class UserControlManager
    {
        private static readonly Dictionary<string, Window> _openWindows = new();

        /// <summary>
        /// 打开单实例窗口（以 UserControl 作为内容）
        /// </summary>
        public static void OpenSingleInstanceWindow(
            UserControl content,
            string windowName,
            Action<Window> configureWindow = null)
        {
            // 已存在 → 激活
            if (_openWindows.TryGetValue(windowName, out var existing) && existing != null)
            {
                if (existing.WindowState == WindowState.Minimized)
                    existing.WindowState = WindowState.Normal;

                existing.Activate();
                return;
            }

            // 创建宿主 Window
            var window = new Window
            {
                Content = content,
                Owner = null,
                ShowInTaskbar = true,
                Topmost = false,
                SizeToContent = SizeToContent.WidthAndHeight
            };

            // 让 UserControl 能拿到宿主 Window（可选）
            content.Loaded += (_, _) =>
            {
                Window.GetWindow(content);
            };

            // 应用外部配置
            configureWindow?.Invoke(window);

            // 关闭后移除
            window.Closed += (_, _) =>
            {
                _openWindows.Remove(windowName);
            };

            _openWindows[windowName] = window;

            window.Show();
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        public static void CloseWindow(string windowName)
        {
            if (_openWindows.TryGetValue(windowName, out var window))
            {
                window.Close();
            }
        }
    }

}
