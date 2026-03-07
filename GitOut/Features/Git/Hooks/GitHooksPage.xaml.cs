using System.Windows.Controls;

namespace GitOut.Features.Git.Hooks;

public partial class GitHooksPage : UserControl
{
    public GitHooksPage(GitHooksViewModel dataContext)
    {
        InitializeComponent();
        DataContext = dataContext;
    }
}
