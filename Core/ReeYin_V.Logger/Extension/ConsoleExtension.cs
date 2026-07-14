using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ReeYin_V.Logger.Extension
{
    public static class ConsoleEx
    {
        // 控制是否同步输出到页面日志区域
        public static bool EnablePageLogOutput { get; set; } = false;

        // 页面日志输出委托（UI 层可以绑定这个）
        public static Action<string>? PageLogWriter { get; set; }

        public static void WriteLineEx(string message)
        {
            Console.WriteLine(message);

            if (EnablePageLogOutput && PageLogWriter != null)
            {
                PageLogWriter.Invoke(message);
            }
        }

        public static void WriteLineEx(string format, params object[] args)
        {
            WriteLineEx(string.Format(format, args));
        }
    }


    public class DualOutputWriter : TextWriter
    {
        private readonly TextWriter _originalOut;
        public static bool EnablePageLogOutput { get; set; } = false;
        public static Action<string>? PageLogWriter { get; set; }

        public DualOutputWriter(TextWriter originalOut)
        {
            _originalOut = originalOut;
        }

        public override Encoding Encoding => _originalOut.Encoding;

        public override void WriteLine(string? value)
        {
            _originalOut.WriteLine(value);

            if (EnablePageLogOutput && PageLogWriter != null)
            {
                PageLogWriter(value ?? string.Empty);
            }
        }
    }
}
