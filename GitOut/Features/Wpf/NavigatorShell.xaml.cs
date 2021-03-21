using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Microsoft.Windows.Sdk;

namespace GitOut.Features.Wpf
{
    public partial class NavigatorShell : Window
    {
        public NavigatorShell(NavigatorShellViewModel dataContext)
        {
            InitializeComponent();
            DataContext = dataContext;
        }

        public event EventHandler? Resized;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            if (!(PresentationSource.FromVisual(this) is HwndSource source))
            {
                throw new InvalidOperationException("Cannot register hook on window without hwnd");
            }
            source.AddHook(WindowResizedHook);
            GlassHelper.EnableBlur(this);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            DragMove();
        }

        private IntPtr WindowResizedHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_EXITSIZEMOVE = 0x232;
            const int WM_GETMINMAXINFO = 0x0024;

            switch (msg)
            {
                case WM_EXITSIZEMOVE:
                    Resized?.Invoke(this, EventArgs.Empty);
                    break;
                case WM_GETMINMAXINFO:
                    {
                        // from https://stackoverflow.com/a/46465322/238902
                        var minMaxInfo = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO))!;

                        HMONITOR monitor = PInvoke.MonitorFromWindow(
                            new HWND(hwnd),
                            MonitorFrom_dwFlags.MONITOR_DEFAULTTONEAREST
                        );

                        if (monitor != IntPtr.Zero)
                        {
                            var monitorinfo = new MONITORINFO
                            {
                                cbSize = (uint)Marshal.SizeOf(typeof(MONITORINFO))
                            };
                            PInvoke.GetMonitorInfo(monitor, ref monitorinfo);
                            RECT rcWork = monitorinfo.rcWork;
                            RECT rcMonitor = monitorinfo.rcMonitor;

                            minMaxInfo.ptMaxPosition.x = Math.Abs(rcWork.left - rcMonitor.left);
                            minMaxInfo.ptMaxPosition.y = Math.Abs(rcWork.top - rcMonitor.top);
                            minMaxInfo.ptMaxSize.x = Math.Abs(rcWork.right - rcWork.left);
                            minMaxInfo.ptMaxSize.y = Math.Abs(rcWork.bottom - rcWork.top);
                            minMaxInfo.ptMinTrackSize.x = (int)MinWidth;
                            minMaxInfo.ptMinTrackSize.y = (int)MinHeight;
                        }
                        Marshal.StructureToPtr(minMaxInfo, lParam, true);
                    }
                    break;
            }
            return IntPtr.Zero;
        }
    }
}
