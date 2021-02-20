using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Input;
using GitOut.Features.Collections;
using GitOut.Features.Git.Log;
using GitOut.Features.Git.Storage;
using GitOut.Features.IO;
using GitOut.Features.Navigation;
using GitOut.Features.Wpf;

namespace GitOut.Features.Git.RepositoryList
{
    public class RepositoryListViewModel : INavigationListener
    {
        private readonly ICollection<IGitRepository> repositories = new SortedObservableCollection<IGitRepository>((a, b) => string.Compare(a.Name, b.Name, true));
        private readonly IDisposable subscription;

        public RepositoryListViewModel(
            INavigationService navigation,
            IGitRepositoryStorage storage,
            IGitRepositoryFactory repositoryFactory
        )
        {
            NavigateToLogCommand = new NavigateLocalCommand<IGitRepository>(
                navigation,
                typeof(GitLogPage).FullName!,
                repository => GitLogPageOptions.OpenRepository(repository),
                repository => repository != null
            );

            AddRepositoryCommand = new CallbackCommand(() =>
            {
                var dialog = new FolderBrowserDialog();
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    storage.Add(repositoryFactory.Create(DirectoryPath.Create(dialog.SelectedPath)));
                }
            });

            RemoveRepositoryCommand = new CallbackCommand<IGitRepository>(repository => storage.Remove(repository));

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
        public ICommand AddRepositoryCommand { get; }
        public ICommand RemoveRepositoryCommand { get; }
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
