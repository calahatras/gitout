using System.Windows.Controls;

namespace GitOut.Features.Git.Stage
{
    public partial class GitStagePage : UserControl
    {
        public GitStagePage(GitStageViewModel dataContext)
        {
            InitializeComponent();
            DataContext = dataContext;
        }
    }
}
