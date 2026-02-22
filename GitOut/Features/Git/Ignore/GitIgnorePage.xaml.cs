using System.Windows.Controls;

namespace GitOut.Features.Git.Ignore;

public partial class GitIgnorePage : UserControl
{
    public GitIgnorePage(GitIgnoreViewModel dataContext)
    {
        InitializeComponent();
        DataContext = dataContext;
    }
}
