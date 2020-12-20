using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using GitOut.Features.Git.Log;
using GitOut.Features.Git.Storage;
using GitOut.Features.Material.Snackbar;
using GitOut.Features.Navigation;

namespace GitOut.Features.Git.RepositoryList
{
    public class RepositoryListViewModel : INavigationListener
    {
        private readonly object repositoriesLock = new object();
        private readonly ObservableCollection<IGitRepository> repositories = new ObservableCollection<IGitRepository>();

        private readonly IDisposable subscription;

        public RepositoryListViewModel(
            INavigationService navigation,
            IGitRepositoryStorage storage,
            ISnackbarService snacks
        )
        {
            BindingOperations.EnableCollectionSynchronization(repositories, repositoriesLock);
            Repositories = CollectionViewSource.GetDefaultView(repositories);
            Repositories.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

            NavigateToLogCommand = new NavigateLocalCommand<IGitRepository>(
                navigation,
                typeof(GitLogPage).FullName!,
                repository => GitLogPageOptions.OpenRepository(repository),
                repository => repository != null
            );

            subscription = storage.Repositories.Subscribe(finalList =>
            {
                lock (repositoriesLock)
                {
                    foreach (IGitRepository repo in finalList)
                    {
                        if (!repositories.Any(item => item.WorkingDirectory.Directory.Equals(repo.WorkingDirectory.Directory)))
                        {
                            repositories.Add(repo);
                        }
                    }
                    for (int i = 0; i < repositories.Count; ++i)
                    {
                        if (finalList.All(item => !item.WorkingDirectory.Directory.Equals(repositories[i].WorkingDirectory.Directory)))
                        {
                            repositories.RemoveAt(i--);
                        }
                    }
                };
            });
        }

        public ICollectionView Repositories { get; }
        public ICommand NavigateToLogCommand { get; }

        public void Navigated(NavigationType type)
        {
            if (type == NavigationType.NavigatedLeave)
            {
                subscription.Dispose();
            }
        }
    }
}
