using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using GitOut.Features.Collections;
using GitOut.Features.Git.Hooks;
using GitOut.Features.Git.Log;
using GitOut.Features.Git.Storage;
using GitOut.Features.IO;
using GitOut.Features.Material.Snackbar;
using GitOut.Features.Navigation;
using GitOut.Features.Wpf;
using Microsoft.Win32;
using DataObject = System.Windows.DataObject;

namespace GitOut.Features.Git.RepositoryList;

public class RepositoryListViewModel : INavigationListener, INotifyPropertyChanged
{
    private readonly SortedObservableCollection<IGitRepository> repositories = new(
        (a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase)
    );
    private readonly IDisposable subscription;
    private readonly IGitRepositoryStorage storage;
    private readonly IGitRepositoryFactory repositoryFactory;
    private readonly ISnackbarService snack;

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

        NavigateToHooksCommand = new NavigateLocalCommand<IGitRepository>(
            navigation,
            typeof(GitHooksPage).FullName!,
            repository => GitHooksPageOptions.OpenRepository(repository!),
            repository => repository is not null
        );

        AddRepositoryCommand = new AsyncCallbackCommand(async () =>
        {
            var dialog = new OpenFolderDialog();
            if (dialog.ShowDialog() == true)
            {
                IGitRepository? repository = await CreateRepositoryAsync(dialog.FolderName);
                if (repository is not null)
                {
                    storage.Add(repository);
                    snack.ShowSuccess("Added repository");
                }
            }
        });

        RemoveRepositoryCommand = new NotNullCallbackCommand<IGitRepository>(storage.Remove);

        subscription = storage.Repositories.Subscribe(finalList =>
        {
            foreach (IGitRepository repo in finalList)
            {
                if (
                    !repositories.Any(item =>
                        item.WorkingDirectory.Directory.Equals(
                            repo.WorkingDirectory.Directory,
                            StringComparison.Ordinal
                        )
                    )
                )
                {
                    repositories.Add(repo);
                }
            }
            foreach (
                IGitRepository repo in repositories
                    .Where(repo =>
                        finalList.All(item =>
                            !item.WorkingDirectory.Directory.Equals(
                                repo.WorkingDirectory.Directory,
                                StringComparison.Ordinal
                            )
                        )
                    )
                    .ToList()
            )
            {
                _ = repositories.Remove(repo);
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
        SnackAction? action = await snack.ShowAsync(
            Snack
                .Builder()
                .WithMessage(
                    $"{Path.GetFileName(path)} is not a valid git repository, do you want to add the folder anyway?"
                )
                .WithDuration(TimeSpan.FromSeconds(300))
                .AddAction(approveText)
        );

        return action?.Text == approveText ? repository : null;
    }

    private async Task OnDropAsync(DataObject? dataObject)
    {
        if (dataObject is null)
        {
            return;
        }

        List<IGitRepository> repositories = [];
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
    public ICommand NavigateToHooksCommand { get; }
    public ICommand AddRepositoryCommand { get; }
    public ICommand RemoveRepositoryCommand { get; }
    public ICommand ClearCommand { get; }
    public ICommand DropCommand { get; }

    public IEnumerable<IGitRepository> Repositories => repositories;

    public string SearchQuery
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

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
