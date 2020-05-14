using System;
using System.Windows;
using System.Windows.Input;

namespace GitOut.Features.Wpf
{
    public partial class NavigatorShell : Window
    {
        public NavigatorShell(NavigatorShellViewModel dataContext)
        {
            InitializeComponent();
            DataContext = dataContext;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            GlassHelper.EnableBlur(this);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            DragMove();
        }
    }
}
