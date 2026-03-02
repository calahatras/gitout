using System.Windows.Controls;

namespace GitOut.Features.Git.Log;

public partial class CherryPickOptionsPage : UserControl
{
    public CherryPickOptionsPage(CherryPickOptionsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        Loaded += (s, e) =>
        {
            var window = System.Windows.Window.GetWindow(this);
            if (window != null)
            {
                window.Background = System.Windows.Media.Brushes.Transparent;
                System.Windows.Shell.WindowChrome.SetWindowChrome(window, new System.Windows.Shell.WindowChrome
                {
                    CaptionHeight = 0,
                    CornerRadius = new System.Windows.CornerRadius(8),
                    GlassFrameThickness = new System.Windows.Thickness(-1),
                    ResizeBorderThickness = new System.Windows.Thickness(0)
                });
            }
        };
    }
}
