using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using GitOut.Features.Collections;
using GitOut.Features.Git.Log;
using GitOut.Features.Git.Storage;
using GitOut.Features.IO;
using GitOut.Features.Material.Snackbar;
using GitOut.Features.Navigation;
using GitOut.Features.Wpf;

using DataObject = System.Windows.DataObject;

namespace GitOut.Features.Git.RepositoryList
{
    public class RepositoryListViewModel : INavigationListener, INotifyPropertyChanged
    {
        private readonly SortedObservableCollection<IGitRepository> repositories = new((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        private readonly IDisposable subscription;
        private readonly IGitRepositoryStorage storage;
        private readonly IGitRepositoryFactory repositoryFactory;
        private readonly ISnackbarService snack;
        private string searchQuery = string.Empty;

        public RepositoryListViewModel(
            INavigationService navigation,
            IGitRepositoryStorage storage,
            IGitRepositoryFactory repositoryFactory,
            ISnackbarService snack
        )
        {
            this.storage = storage;
            this.repositoryFactory = repositoryFactory;
            this.snack = snack;

            NavigateToLogCommand = new NavigateLocalCommand<IGitRepository>(
                navigation,
                typeof(GitLogPage).FullName!,
                repository => GitLogPageOptions.OpenRepository(repository!),
                repository => repository is not null
            );

            AddRepositoryCommand = new AsyncCallbackCommand(async () =>
            {
                var dialog = new FolderBrowserDialog();
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    IGitRepository? repository = await CreateRepositoryAsync(dialog.SelectedPath);
                    if (repository is not null)
                    {
                        storage.Add(repository);
                        snack.ShowSuccess("Added repository");
                    }
                }
            });

            RemoveRepositoryCommand = new NotNullCallbackCommand<IGitRepository>(repository => storage.Remove(repository));

            subscription = storage.Repositories.Subscribe(finalList =>
            {
                foreach (IGitRepository repo in finalList)
                {
                    if (!repositories.Any(item => item.WorkingDirectory.Directory.Equals(repo.WorkingDirectory.Directory, StringComparison.Ordinal)))
                    {
                        repositories.Add(repo);
                    }
                }
                foreach (IGitRepository repo in repositories.Where(repo => finalList.All(item => !item.WorkingDirectory.Directory.Equals(repo.WorkingDirectory.Directory, StringComparison.Ordinal))).ToList())
                {
                    repositories.Remove(repo);
                }
            });

            ClearCommand = new CallbackCommand(() => SearchQuery = string.Empty);

            DropCommand = new AsyncCallbackCommand<DataObject>(OnDropAsync);
        }

        private async Task<IGitRepository?> CreateRepositoryAsync(string path)
        {
            IGitRepository repository = repositoryFactory.Create(DirectoryPath.Create(path));
            if (await repository.IsInsideWorkTree())
            {
                return repository;
            }

            const string approveText = "YES";
            SnackAction? action = await snack.ShowAsync(Snack.Builder()
                .WithMessage($"{Path.GetFileName(path)} is not a valid git repository, do you want to add the folder anyway?")
                .WithDuration(TimeSpan.FromSeconds(300))
                .AddAction(approveText));

            return action?.Text == approveText ? repository : null;
        }

        private async Task OnDropAsync(DataObject? dataObject)
        {
            if (dataObject is null)
            {
                return;
            }

            List<IGitRepository> repositories = new();
            foreach (string? path in dataObject.GetFileDropList())
            {
                if (path is null)
                {
                    continue;
                }
                if (!Directory.Exists(path))
                {
                    continue;
                }
                IGitRepository? repository = await CreateRepositoryAsync(path);
                if (repository is not null)
                {
                    repositories.Add(repository);
                }
            }

            if (repositories.Count == 0)
            {
                return;
            }

            storage.AddRange(repositories);
            snack.ShowSuccess($"Added {(repositories.Count == 1 ? "repository" : "repositories")}");
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ICommand NavigateToLogCommand { get; }
        public ICommand AddRepositoryCommand { get; }
        public ICommand RemoveRepositoryCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand DropCommand { get; }

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
