using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Login.Model
{
    public class BarCodeHook
    {
        public delegate void BarCodeDelegate(BarCodes barCode);

        public event BarCodeDelegate BarCodeEvent;

        //定义成静态，这样不会抛出回收异常
        private static HookProc _hookproc;


        public struct BarCodes
        {
            public int VirtKey;
            public int ScanCode;
            public string KeyName;
            public uint Ascll;
            public char Chr;

            public string OriginalChrs;
            public string OriginalAsciis;

            public string OriginalBarCode;

            public bool IsValid;
            public DateTime Time;

            public string BarCode;
        }

        private struct EventMsg
        {
            public int Message;
            public int ParamL;
            public int ParamH;
            public int Time;
            public int hwnd;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern int SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hInstance, int threadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern bool UnhookWindowsHookEx(int idHook);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern int CallNextHookEx(int idHook, int nCode, Int32 wParam, IntPtr lParam);

        [DllImport("user32", EntryPoint = "GetKeyNameText")]
        private static extern int GetKeyNameText(int IParam, StringBuilder lpBuffer, int nSize);

        [DllImport("user32", EntryPoint = "GetKeyboardState")]
        private static extern int GetKeyboardState(byte[] pbKeyState);

        [DllImport("user32", EntryPoint = "ToAscii")]
        private static extern bool ToAscii(int VirtualKey, int ScanCode, byte[] lpKeySate, ref uint lpChar, int uFlags);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string name);


        private delegate int HookProc(int nCode, Int32 wParam, IntPtr lParam);

        BarCodes _barCode = new BarCodes();

        int _hKeyboardHook = 0;

        readonly StringBuilder sbBarCode = new StringBuilder();

        public BarCodeHook()
        {
            _hKeyboardHook = 0;
        }

        private int KeyboardHookProc(int nCode, Int32 wParam, IntPtr lParam)
        {
            var iCalledNext = -10;
            if (nCode == 0)
            {
                EventMsg msg = (EventMsg)Marshal.PtrToStructure(lParam, typeof(EventMsg));

                if (wParam == 0x100)
                {
                    _barCode.VirtKey = msg.Message & 0xff;
                    _barCode.ScanCode = msg.ParamL & 0xff;
                    var strKeyName = new StringBuilder(225);
                    if (GetKeyNameText(_barCode.ScanCode * 65536, strKeyName, 255) > 0)
                    {
                        _barCode.KeyName = strKeyName.ToString().Trim(new char[] { ' ', '\0' });
                    }
                    else
                    {
                        _barCode.KeyName = "";
                    }

                    var kbArray = new byte[256];
                    uint uKey = 0;
                    GetKeyboardState(kbArray);

                    if (ToAscii(_barCode.VirtKey, _barCode.ScanCode, kbArray, ref uKey, 0))
                    {
                        _barCode.Ascll = uKey;
                        _barCode.Chr = Convert.ToChar(uKey);
                        _barCode.OriginalChrs += " " + Convert.ToString(_barCode.Chr);
                        _barCode.OriginalAsciis += " " + Convert.ToString(_barCode.Ascll);
                        _barCode.OriginalBarCode += Convert.ToString(_barCode.Chr);
                    }

                    var ts = DateTime.Now.Subtract(_barCode.Time);

                    if (ts.TotalMilliseconds > 30)
                    {
                        //时间戳，大于50 毫秒表示手动输入
                        sbBarCode.Remove(0, sbBarCode.Length);
                        sbBarCode.Append(_barCode.Chr.ToString());
                        _barCode.OriginalChrs = " " + Convert.ToString(_barCode.Chr);
                        _barCode.OriginalAsciis = " " + Convert.ToString(_barCode.Ascll);
                        _barCode.OriginalBarCode = Convert.ToString(_barCode.Chr);
                    }
                    else
                    {
                        if ((msg.Message & 0xff) == 13 && sbBarCode.Length > 3)
                        {
                            _barCode.BarCode = sbBarCode.ToString();

                            _barCode.IsValid = true;

                            sbBarCode.Remove(0, sbBarCode.Length);
                        }

                        sbBarCode.Append(_barCode.Chr.ToString());
                    }


                    try
                    {
                        if (BarCodeEvent != null && _barCode.IsValid)
                        {
                            //barCode.BarCode = barCode.BarCode.Replace("\b", "").Replace("\0","");  可以不需要 因为大于50毫秒已经处理
                            //先进行 WINDOWS事件往下传
                            iCalledNext = CallNextHookEx(_hKeyboardHook, nCode, wParam, lParam);


                            BarCodeEvent(_barCode); //触发事件

                            _barCode.BarCode = "";
                            _barCode.OriginalChrs = "";
                            _barCode.OriginalAsciis = "";
                            _barCode.OriginalBarCode = "";
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }
                    finally
                    {
                        _barCode.IsValid = false; //最后一定要 设置barCode无效
                        _barCode.Time = DateTime.Now;
                    }
                }
            }

            if (iCalledNext == -10)
            {
                iCalledNext = CallNextHookEx(_hKeyboardHook, nCode, wParam, lParam);
            }

            return iCalledNext;
        }

        //安装钩子
        public bool Start()
        {
            if (_hKeyboardHook != 0) return (_hKeyboardHook != 0);
            _hookproc = new HookProc(KeyboardHookProc);

            //GetModuleHandle 函数 替代 Marshal.GetHINSTANCE
            //防止在 framework4.0中 注册钩子不成功
            var modulePtr = GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName);

            _hKeyboardHook = SetWindowsHookEx(13, _hookproc, modulePtr, 0);

            return (_hKeyboardHook != 0);
        }

        //卸载钩子
        public bool Stop()
        {
            return _hKeyboardHook == 0 || UnhookWindowsHookEx(_hKeyboardHook);
        }
    }
}
