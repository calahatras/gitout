using System.Windows.Controls;

namespace GitOut.Features.Git.RepositoryList
{
    public partial class RepositoryListPage : UserControl
    {
        public RepositoryListPage(RepositoryListViewModel dataContext)
        {
            InitializeComponent();
            DataContext = dataContext;
        }
    }
}
