using System.Runtime.InteropServices;

namespace PointCloud.VTKWPF.Native;

internal static class User32Native
{
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct WNDCLASS
    {
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public string? lpszMenuName;
        public string lpszClassName;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct POINT
    {
        public int X;
        public int Y;
    }

    internal const int ERROR_CLASS_ALREADY_EXISTS = 1410;
    internal const int WM_MOUSEMOVE = 0x0200;
    internal const int WM_LBUTTONDOWN = 0x0201;
    internal const int WM_SIZE = 0x0005;
    internal const int WM_PAINT = 0x000F;
    internal const int WM_ERASEBKGND = 0x0014;
    internal const int WM_LBUTTONUP = 0x0202;
    internal const int WM_RBUTTONDOWN = 0x0204;
    internal const int WM_RBUTTONUP = 0x0205;
    internal const int WM_MBUTTONDOWN = 0x0207;
    internal const int WM_MBUTTONUP = 0x0208;
    internal const int WM_MOUSEWHEEL = 0x020A;

    internal const uint CS_VREDRAW = 0x0001;
    internal const uint CS_HREDRAW = 0x0002;
    internal const uint CS_DBLCLKS = 0x0008;
    internal const uint CS_OWNDC = 0x0020;

    internal const uint WS_CHILD = 0x40000000;
    internal const uint WS_VISIBLE = 0x10000000;
    internal const uint WS_CLIPSIBLINGS = 0x04000000;
    internal const uint WS_CLIPCHILDREN = 0x02000000;

    private const string VtkHostWindowClassName = "PointCloud.VTKWPF.VtkHostWindow";
    private static readonly WindowProc DefaultWindowProcDelegate = DefWindowProc;
    private static readonly IntPtr DefaultWindowProcPointer = Marshal.GetFunctionPointerForDelegate(DefaultWindowProcDelegate);

    internal static string EnsureVtkHostWindowClass()
    {
        var wndClass = new WNDCLASS
        {
            style = CS_HREDRAW | CS_VREDRAW | CS_OWNDC | CS_DBLCLKS,
            lpfnWndProc = DefaultWindowProcPointer,
            cbClsExtra = 0,
            cbWndExtra = IntPtr.Size * 2,
            hInstance = GetModuleHandle(null),
            hIcon = IntPtr.Zero,
            hCursor = LoadCursor(IntPtr.Zero, new IntPtr(32512)),
            hbrBackground = IntPtr.Zero,
            lpszMenuName = null,
            lpszClassName = VtkHostWindowClassName,
        };

        ushort atom = RegisterClass(ref wndClass);
        if (atom == 0)
        {
            int error = Marshal.GetLastWin32Error();
            if (error != ERROR_CLASS_ALREADY_EXISTS)
            {
                throw new InvalidOperationException($"无法注册 VTK 宿主窗口类，Win32 错误码: {error}。");
            }
        }

        return VtkHostWindowClassName;
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    internal static extern ushort RegisterClass([In] ref WNDCLASS wndClass);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    internal static extern IntPtr CreateWindowEx(
        int exStyle,
        string className,
        string? windowName,
        uint style,
        int x,
        int y,
        int width,
        int height,
        IntPtr parentHandle,
        IntPtr menuHandle,
        IntPtr instanceHandle,
        IntPtr param);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DestroyWindow(IntPtr hwnd);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    internal static extern IntPtr GetModuleHandle(string? moduleName);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    internal static extern IntPtr LoadCursor(IntPtr instanceHandle, IntPtr cursorName);

    [DllImport("user32.dll")]
    internal static extern IntPtr DefWindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ValidateRect(IntPtr hwnd, IntPtr rect);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr SetFocus(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr SetCapture(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ReleaseCapture();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ScreenToClient(IntPtr hwnd, ref POINT point);
}
