using System.Windows;
using System.Windows.Controls;

namespace GitOut.Features.Git.Stage;

public partial class GitStagePage : UserControl
{
    public GitStagePage(GitStageViewModel dataContext)
    {
        InitializeComponent();
        DataContext = dataContext;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => Focus();
}
