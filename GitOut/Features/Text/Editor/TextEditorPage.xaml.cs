using System.Windows;
using System.Windows.Controls;

namespace GitOut.Features.Text.Editor;

public partial class TextEditorPage : UserControl
{
    public TextEditorPage(TextEditorViewModel dataContext)
    {
        InitializeComponent();
        DataContext = dataContext;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => Root.Focus();
}
