using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Windows.Input;
using GitOut.Features.Collections;
using GitOut.Features.Git.Log;
using GitOut.Features.Git.Storage;
using GitOut.Features.IO;
using GitOut.Features.Material.Snackbar;
using GitOut.Features.Navigation;
using GitOut.Features.Wpf;

namespace GitOut.Features.Git.RepositoryList
{
    public class RepositoryListViewModel : INavigationListener, INotifyPropertyChanged
    {
        private readonly ICollection<IGitRepository> repositories = new SortedObservableCollection<IGitRepository>((a, b) => string.Compare(a.Name, b.Name, true));
        private readonly IDisposable subscription;
        private string searchQuery = string.Empty;

        public RepositoryListViewModel(
            INavigationService navigation,
            IGitRepositoryStorage storage,
            IGitRepositoryFactory repositoryFactory,
            ISnackbarService snack
        )
        {
            NavigateToLogCommand = new NavigateLocalCommand<IGitRepository>(
                navigation,
                typeof(GitLogPage).FullName!,
                repository => GitLogPageOptions.OpenRepository(repository),
                repository => repository != null
            );

            AddRepositoryCommand = new AsyncCallbackCommand(async () =>
            {
                var dialog = new FolderBrowserDialog();
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    IGitRepository repository = repositoryFactory.Create(DirectoryPath.Create(dialog.SelectedPath));
                    if (!await repository.IsInsideGitFolder())
                    {
                        SnackAction? action = await snack.ShowAsync(Snack.Builder()
                            .WithMessage($"{Path.GetFileName(dialog.SelectedPath)} is not a valid git repository, do you want to add the folder anyway?")
                            .WithDuration(TimeSpan.FromSeconds(30))
                            .AddAction("YES")
                            .AddAction("CANCEL"));
                        if (action?.Text == "YES")
                        {
                            storage.Add(repository);
                            snack.ShowSuccess("Added repository");
                        }
                    }
                    else
                    {
                        storage.Add(repository);
                        snack.ShowSuccess("Added repository");
                    }
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

            ClearCommand = new CallbackCommand(() => SearchQuery = string.Empty);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ICommand NavigateToLogCommand { get; }
        public ICommand AddRepositoryCommand { get; }
        public ICommand RemoveRepositoryCommand { get; }
        public ICommand ClearCommand { get; }
        public IEnumerable<IGitRepository> Repositories => repositories;

        public string SearchQuery
        {
            get => searchQuery;
            set => SetProperty(ref searchQuery, value);
        }

        public void Navigated(NavigationType type)
        {
            if (type == NavigationType.NavigatedLeave)
            {
                subscription.Dispose();
            }
        }

        private void SetProperty<T>(ref T prop, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!ReferenceEquals(prop, value))
            {
                prop = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
