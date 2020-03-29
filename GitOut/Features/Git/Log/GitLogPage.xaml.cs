using System.Windows.Controls;

namespace GitOut.Features.Git.Log
{
    public partial class GitLogPage : UserControl
    {
        public GitLogPage(GitLogViewModel dataContext)
        {
            InitializeComponent();
            DataContext = dataContext;
        }
    }
}
