using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using GitOut.Features.Navigation;
using GitOut.Features.Options;
using Microsoft.Extensions.Options;

namespace GitOut.Features.Wpf
{
    public partial class NavigatorShell : Window
    {
        private readonly IOptionsWriter<NavigationWindowOptions>? storage;
        private readonly IOptions<NavigationWindowOptions> windowOptions;

        public NavigatorShell(
            NavigatorShellViewModel dataContext,
            IOptionsWriter<NavigationWindowOptions>? storage,
            IOptions<NavigationWindowOptions> windowOptions
        )
        {
            this.storage = storage;
            this.windowOptions = windowOptions;
            DataContext = dataContext;
            InitializeComponent();
        }

        public event EventHandler? Resized;

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            NavigationWindowOptions cachedValues = windowOptions.Value;
            if (cachedValues.Width.HasValue)
            {
                Width = cachedValues.Width.Value;
            }
            if (cachedValues.Height.HasValue)
            {
                Height = cachedValues.Height.Value;
            }
            if (cachedValues.Top.HasValue)
            {
                Top = cachedValues.Top.Value;
            }
            if (cachedValues.Left.HasValue)
            {
                Left = cachedValues.Left.Value;
            }
            EnsureWithinBounds();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (WindowState == WindowState.Normal && storage is not null)
            {
                storage.Update(snap =>
                {
                    snap.Width = (int)ActualWidth;
                    snap.Height = (int)ActualHeight;
                    snap.Left = (int)Left;
                    snap.Top = (int)Top;
                });
            }
            if (DataContext is NavigatorShellViewModel viewModel)
            {
                viewModel.Dispose();
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            if (PresentationSource.FromVisual(this) is not HwndSource source)
            {
                throw new InvalidOperationException("Cannot register hook on window without hwnd");
            }
            source.AddHook(WindowResizedHook);
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

        private void EnsureWithinBounds()
        {
            double left = SystemParameters.VirtualScreenLeft;
            double right = SystemParameters.VirtualScreenWidth;
            if (Left > right
                || Left + ActualWidth < left)
            {
                Left = 10;
                Top = 10;
                Height = SystemParameters.WorkArea.Height - 20;
            }
        }
    }
}
