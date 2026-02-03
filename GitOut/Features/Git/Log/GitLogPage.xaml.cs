using System.Windows;
using System.Windows.Controls;

namespace GitOut.Features.Git.Log;

public partial class GitLogPage : UserControl
{
    public GitLogPage(GitLogViewModel dataContext)
    {
        InitializeComponent();
        DataContext = dataContext;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => Root.Focus();
}
