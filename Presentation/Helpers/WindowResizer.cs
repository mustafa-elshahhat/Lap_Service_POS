using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace CarPartsShopWPF.Presentation.Helpers
{
    public class WindowResizer
    {
        private Window _window;
        private bool _isFullScreen;

        public bool IsFullScreen
        {
            get => _isFullScreen;
            set
            {
                _isFullScreen = value;
                ApplyFullScreen();
            }
        }

        private void ApplyFullScreen()
        {
            if (_window == null) return;

            _window.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, new Action(() =>
            {
                var handle = (new WindowInteropHelper(_window)).Handle;
                if (handle == IntPtr.Zero) return;

                IntPtr monitor = MonitorFromWindow(handle, MONITOR_DEFAULTTONEAREST);
                if (monitor == IntPtr.Zero) return;

                MONITORINFO monitorInfo = new MONITORINFO();
                monitorInfo.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
                if (!GetMonitorInfo(monitor, monitorInfo)) return;

                RECT rcMonitorArea = monitorInfo.rcMonitor;

                if (_isFullScreen)
                {

                    SetWindowPos(handle, HWND_TOPMOST,
                        rcMonitorArea.left,
                        rcMonitorArea.top,
                        rcMonitorArea.right - rcMonitorArea.left,
                        rcMonitorArea.bottom - rcMonitorArea.top,
                        SWP_SHOWWINDOW | SWP_FRAMECHANGED);
                }
                else
                {

                    SetWindowPos(handle, HWND_NOTOPMOST, 0, 0, 0, 0,
                        SWP_NOSIZE | SWP_NOMOVE | SWP_FRAMECHANGED);
                }
            }));
        }

        public WindowResizer(Window window)
        {
            _window = window;
        }

        public void Register()
        {
            _window.SourceInitialized += Window_SourceInitialized;
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            var handle = (new WindowInteropHelper(_window)).Handle;
            HwndSource.FromHwnd(handle)?.AddHook(WindowProc);
        }

        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x0024)
            {
                WmGetMinMaxInfo(hwnd, lParam);
                handled = true;
            }

            return IntPtr.Zero;
        }

        private void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
        {
            var mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));

            IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

            if (monitor != IntPtr.Zero)
            {
                MONITORINFO monitorInfo = new MONITORINFO();
                monitorInfo.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
                GetMonitorInfo(monitor, monitorInfo);

                RECT rcWorkArea = monitorInfo.rcWork;
                RECT rcMonitorArea = monitorInfo.rcMonitor;

                if (_isFullScreen)
                {




                    mmi.ptMaxPosition.x = 0;
                    mmi.ptMaxPosition.y = 0;

                    mmi.ptMaxSize.x = rcMonitorArea.right - rcMonitorArea.left;
                    mmi.ptMaxSize.y = rcMonitorArea.bottom - rcMonitorArea.top;
                }
                else
                {



                    mmi.ptMaxPosition.x = rcWorkArea.left - rcMonitorArea.left;
                    mmi.ptMaxPosition.y = rcWorkArea.top - rcMonitorArea.top;

                    mmi.ptMaxSize.x = rcWorkArea.right - rcWorkArea.left;
                    mmi.ptMaxSize.y = rcWorkArea.bottom - rcWorkArea.top;
                }

                mmi.ptMaxTrackSize.x = mmi.ptMaxSize.x;
                mmi.ptMaxTrackSize.y = mmi.ptMaxSize.y;
            }

            Marshal.StructureToPtr(mmi, lParam, true);
        }

        #region Native Constants and Structs

        private const int MONITOR_DEFAULTTONEAREST = 0x00000002;
        private static readonly IntPtr HWND_TOP = new IntPtr(0);
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SWP_FRAMECHANGED = 0x0020;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, [In, Out] MONITORINFO lpmi);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MONITORINFO
        {
            public int cbSize = Marshal.SizeOf(typeof(MONITORINFO));
            public RECT rcMonitor = new RECT();
            public RECT rcWork = new RECT();
            public int dwFlags = 0;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        #endregion
    }
}
