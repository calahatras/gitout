using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using GitOut.Features.Git.Log;
using GitOut.Features.Git.Storage;
using GitOut.Features.Navigation;

namespace GitOut.Features.Git.RepositoryList
{
    public class RepositoryListViewModel
    {
        public RepositoryListViewModel(
            INavigationService navigation,
            IGitRepositoryStorage storage
        )
        {
            var repositories = new ObservableCollection<IGitRepository>();
            Repositories = CollectionViewSource.GetDefaultView(repositories);
            Repositories.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

            NavigateToLogCommand = new NavigateLocalCommand<IGitRepository>(
                navigation,
                typeof(GitLogPage).FullName!,
                repository => GitLogPageOptions.OpenRepository(repository),
                repository => repository != null
            );

            SynchronizationContext sync = SynchronizationContext.Current!;
            Task.Run(() =>
            {
                IEnumerable<IGitRepository> repos = storage.GetAll();
                sync.Post(s =>
                {
                    foreach (IGitRepository repo in repos)
                    {
                        repositories.Add(repo);
                    }
                }, null);
            });
        }

        public ICollectionView Repositories { get; }
        public ICommand NavigateToLogCommand { get; }
    }
}
