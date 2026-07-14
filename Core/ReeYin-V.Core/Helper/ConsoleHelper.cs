using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Helper
{
    public class ConsoleHelper
    {
        #region Console
        // Windows API 导入
        [DllImport("kernel32.dll")]
        public static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        public static extern bool FreeConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int GetConsoleTitle(StringBuilder lpConsoleTitle, int nSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleTitle(string lpConsoleTitle);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool SetConsoleTitleW(string lpConsoleTitle);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        // 设置控制台代码页的API
        [DllImport("kernel32.dll")]
        public static extern bool SetConsoleOutputCP(uint wCodePageID);

        [DllImport("kernel32.dll")]
        public static extern bool SetConsoleCP(uint wCodePageID);

        // 控制台相关变量
        public static IntPtr _consoleHandle;
        public static string _consoleTitle;
        public static bool IsConsoleVisible { get;  set; } = false;

        /// <summary>
        /// 设置包含中文的控制台标题
        /// </summary>
        public static void SetConsoleTitleWithChinese(string title)
        {
            try
            {
                // 优先使用Unicode版本的API设置标题
                if (SetConsoleTitleW(title))
                    return;

                // 备选方案：使用默认API并指定编码
                byte[] titleBytes = Encoding.UTF8.GetBytes(title);
                string titleUtf8 = Encoding.UTF8.GetString(titleBytes);
                SetConsoleTitle(titleUtf8);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"设置控制台标题失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 重定向控制台输出流，确保日志能正常显示
        /// </summary>
        public static void RedirectConsoleOutput()
        {
            try
            {
                // 重定向标准输出
                StreamWriter standardOutput = new StreamWriter(Console.OpenStandardOutput())
                {
                    AutoFlush = true
                };
                Console.SetOut(standardOutput);

                // 重定向标准错误输出
                StreamWriter standardError = new StreamWriter(Console.OpenStandardError())
                {
                    AutoFlush = true
                };
                Console.SetError(standardError);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"控制台输出重定向失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取控制台窗口标题(获取到的是乱码，不用了)
        /// </summary>
        private static string GetConsoleWindowTitle()
        {
            const int bufferSize = 256;
            var buffer = new StringBuilder(bufferSize);

            if (GetConsoleTitle(buffer, bufferSize) > 0)
            {
                return buffer.ToString();
            }

            return "Console输出"; // 返回中文自定义标题作为备选
        }

        /// <summary>
        /// 切换控制台显示状态
        /// </summary>
        public static void ToggleConsole()
        {
            // 如果句柄无效，尝试重新获取
            if (_consoleHandle == IntPtr.Zero)
            {
                _consoleTitle = GetConsoleWindowTitle();
                _consoleHandle = FindWindow(null, _consoleTitle);
            }

            if (IsConsoleVisible)
            {
                // 隐藏控制台
                if (_consoleHandle != IntPtr.Zero)
                    ShowWindow(_consoleHandle, 0); // 0 = SW_HIDE
                LogToConsole($"[{DateTime.Now:HH:mm:ss}] 控制台已隐藏");
            }
            else
            {
                // 显示控制台
                if (_consoleHandle != IntPtr.Zero)
                    ShowWindow(_consoleHandle, 5); // 5 = SW_SHOW
                LogToConsole($"[{DateTime.Now:HH:mm:ss}] 控制台已显示");
            }

            IsConsoleVisible = !IsConsoleVisible;
        }

        /// <summary>
        /// 确保日志能输出到控制台的封装方法
        /// </summary>
        public static void LogToConsole(string message)
        {
            try
            {
                Console.WriteLine(message);
                // 强制刷新输出缓冲区
                Console.Out.Flush();
            }
            catch (Exception ex)
            {
                // 输出到调试窗口作为备选
                System.Diagnostics.Debug.WriteLine($"日志输出失败: {ex.Message} | 原消息: {message}");
            }
        }

        #endregion

    }
}



