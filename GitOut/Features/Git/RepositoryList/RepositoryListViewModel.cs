using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        public readonly object repositoriesLock = new object();

        public RepositoryListViewModel(
            INavigationService navigation,
            IGitRepositoryStorage storage
        )
        {
            var repositories = new ObservableCollection<IGitRepository>();
            BindingOperations.EnableCollectionSynchronization(repositories, repositoriesLock);
            Repositories = CollectionViewSource.GetDefaultView(repositories);
            Repositories.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

            NavigateToLogCommand = new NavigateLocalCommand<IGitRepository>(
                navigation,
                typeof(GitLogPage).FullName!,
                repository => GitLogPageOptions.OpenRepository(repository),
                repository => repository != null
            );

            Task.Run(() =>
            {
                IEnumerable<IGitRepository> repos = storage.GetAll();
                lock (repositoriesLock)
                {
                    foreach (IGitRepository repo in repos)
                    {
                        repositories.Add(repo);
                    }
                };
            });
        }

        public ICollectionView Repositories { get; }
        public ICommand NavigateToLogCommand { get; }
    }
}
