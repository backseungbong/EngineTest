using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHLib.WPFUtil.D3DHosting
{
    internal sealed class HiddenWindow : IDisposable
    {
        private const int WS_OVERLAPPED = 0x00000000;
        private const int WS_VISIBLE = 0x10000000;   // 넣지 않음(=숨김)
        private const int CS_NOCLOSE = 0x0200;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyWindow(nint hWnd);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern nint DefWindowProc(nint hWnd, uint uMsg, nint wParam, nint lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern ushort RegisterClassEx([In] ref WNDCLASSEX wc);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern nint CreateWindowExW(
            uint dwExStyle,
            string lpClassName,
            string lpWindowName,
            uint dwStyle,
            int x, int y, int nWidth, int nHeight,
            nint hWndParent,
            nint hMenu,
            nint hInstance,
            nint lpParam);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WNDCLASSEX
        {
            public uint cbSize;
            public uint style;
            public nint lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public nint hInstance;
            public nint hIcon;
            public nint hCursor;
            public nint hbrBackground;
            public string lpszMenuName;
            public string lpszClassName;
            public nint hIconSm;
        }

        private delegate nint WndProc(nint hWnd, uint msg, nint wParam, nint lParam);
        private static nint StaticWndProc(nint hwnd, uint msg, nint wParam, nint lParam) => DefWindowProc(hwnd, msg, wParam, lParam);


        private static readonly HiddenWindow _hiddenWindow = new();
        public static nint Handle => _hiddenWindow._hwnd;

        private readonly nint _hwnd;
        private readonly string _className;
        private readonly WndProc _wndProc; // delegate 보관(가비지 컬렉션 방지)
        private bool _disposed;
        ~HiddenWindow() => Dispose();
        public HiddenWindow()
        {
            _wndProc = StaticWndProc;
            _className = "D3D9Dummy_" + Guid.NewGuid().ToString("N");

            var wc = new WNDCLASSEX
            {
                cbSize = (uint)Unsafe.SizeOf<WNDCLASSEX>(),
                style = CS_NOCLOSE, // 또는 0 (스타일 없음)
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
                cbClsExtra = 0,
                cbWndExtra = 0,
                hInstance = Marshal.GetHINSTANCE(typeof(HiddenWindow).Module),
                hIcon = nint.Zero,
                hCursor = nint.Zero,
                hbrBackground = nint.Zero, // 숨은 창은 배경색이 필요 없을 수 있음
                lpszMenuName = null,
                lpszClassName = _className,
                hIconSm = nint.Zero
            };

            if (RegisterClassEx(ref wc) == 0)
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "RegisterClassEx failed");

            // 3) 창 생성 (보이지 않음)
            _hwnd = CreateWindowExW(
                0,
                _className,
                null,
                WS_OVERLAPPED,     // WS_VISIBLE 미지정: 숨은 창
                0, 0, 1, 1,        // 위치/크기
                nint.Zero, nint.Zero,
                wc.hInstance,
                nint.Zero);

            if (_hwnd == nint.Zero)
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "CreateWindowEx failed");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_hwnd != nint.Zero)
                DestroyWindow(_hwnd);

            GC.SuppressFinalize(this);
        }
    }
}