using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace GitOut.Features.Wpf
{
    public partial class NavigatorShell : Window
    {
        public NavigatorShell(NavigatorShellViewModel dataContext)
        {
            InitializeComponent();
            MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
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
            if (msg == WM_EXITSIZEMOVE)
            {
                Resized?.Invoke(this, EventArgs.Empty);
            }
            return IntPtr.Zero;
        }
    }
}
