using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using GitOut.Features.Collections;
using GitOut.Features.Git.Log;
using GitOut.Features.Git.Storage;
using GitOut.Features.Navigation;

namespace GitOut.Features.Git.RepositoryList
{
    public class RepositoryListViewModel : INavigationListener
    {
        private readonly ICollection<IGitRepository> repositories = new SortedObservableCollection<IGitRepository>((a, b) => string.Compare(a.Name, b.Name, true));
        private readonly IDisposable subscription;

        public RepositoryListViewModel(
            INavigationService navigation,
            IGitRepositoryStorage storage
        )
        {
            NavigateToLogCommand = new NavigateLocalCommand<IGitRepository>(
                navigation,
                typeof(GitLogPage).FullName!,
                repository => GitLogPageOptions.OpenRepository(repository),
                repository => repository != null
            );

            subscription = storage.Repositories.Subscribe(finalList =>
            {
                foreach (IGitRepository repo in finalList)
                {
                    if (!repositories.Any(item => item.WorkingDirectory.Directory.Equals(repo.WorkingDirectory.Directory)))
                    {
                        repositories.Add(repo);
                    }
                }
                foreach (IGitRepository repo in repositories.Where(repo => finalList.All(item => !item.WorkingDirectory.Directory.Equals(repo.WorkingDirectory.Directory))).ToList())
                {
                    repositories.Remove(repo);
                }
            });
        }

        public ICommand NavigateToLogCommand { get; }
        public IEnumerable<IGitRepository> Repositories => repositories;

        public void Navigated(NavigationType type)
        {
            if (type == NavigationType.NavigatedLeave)
            {
                subscription.Dispose();
            }
        }
    }
}
