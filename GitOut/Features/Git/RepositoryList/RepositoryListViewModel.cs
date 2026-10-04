using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using GitOut.Features.Collections;
using GitOut.Features.Diagnostics;
using GitOut.Features.Git.Diagnostics;
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
    private readonly IProcessFactory<IGitProcess>? processFactory;

    public RepositoryListViewModel(
        INavigationService navigation,
        IGitRepositoryStorage storage,
        IGitRepositoryFactory repositoryFactory,
        ISnackbarService snack,
        IProcessFactory<IGitProcess>? processFactory = null
    )
    {
        this.storage = storage;
        this.repositoryFactory = repositoryFactory;
        this.snack = snack;
        this.processFactory = processFactory;

        CloneCommand = new CallbackCommand<object?>(parameter =>
        {
            string? path = parameter switch
            {
                CollectionViewGroup group => group.Name?.ToString(),
                string s => s,
                _ => parameter?.ToString()
            };
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            if (string.Equals(ActiveCloneDirectory, path, StringComparison.Ordinal))
            {
                ActiveCloneDirectory = null;
            }
            else
            {
                ActiveCloneDirectory = path;
                CloneUrl = string.Empty;
                CloneBranch = string.Empty;
            }
        });

        CancelCloneCommand = new CallbackCommand(() => ActiveCloneDirectory = null);

        ExecuteCloneCommand = new AsyncCallbackCommand(async () =>
        {
            if (string.IsNullOrWhiteSpace(ActiveCloneDirectory) || string.IsNullOrWhiteSpace(CloneUrl))
            {
                return;
            }

            string cloneDirectory = ActiveCloneDirectory;
            string url = CloneUrl.Trim();
            string branch = CloneBranch.Trim();
            string? repoName = GetRepositoryNameFromUrl(url);

            if (repoName is not null)
            {
                string expectedPath = Path.Combine(cloneDirectory, repoName);
                if (Directory.Exists(expectedPath))
                {
                    snack.ShowError("Could not clone repository", new InvalidOperationException($"Target directory '{expectedPath}' already exists"));
                    return;
                }
            }

            ActiveCloneDirectory = null;

            if (processFactory is null)
            {
                snack.ShowSuccess($"Cloning repository {url}...");
                return;
            }

            try
            {
                var arguments = new StringBuilder("clone ");
                if (!string.IsNullOrEmpty(branch))
                {
                    arguments.Append($"--branch \"{branch}\" ");
                }
                arguments.Append($"\"{url}\"");

                IGitProcess process = processFactory.Create(
                    DirectoryPath.Create(cloneDirectory),
                    ProcessOptions.FromArguments(arguments.ToString())
                );
                _ = await process.ExecuteAsync();

                string? targetPath = repoName is not null ? Path.Combine(cloneDirectory, repoName) : null;
                if (targetPath is not null && Directory.Exists(targetPath))
                {
                    IGitRepository? repository = await CreateRepositoryAsync(targetPath);
                    if (repository is not null)
                    {
                        storage.Add(repository);
                        snack.ShowSuccess($"Cloned {repository.Name}");
                    }
                }
                else
                {
                    snack.ShowSuccess("Cloned repository");
                }
            }
            catch (Exception e) when (e is InvalidOperationException or ArgumentException or IOException)
            {
                snack.ShowError("Could not clone repository", e, TimeSpan.FromSeconds(10));
            }
        }, () => !string.IsNullOrWhiteSpace(CloneUrl) && !string.IsNullOrWhiteSpace(ActiveCloneDirectory));

        NavigateToLogCommand = new NavigateLocalCommand<IGitRepository>(
            navigation,
            typeof(GitLogPage).FullName!,
            repository => GitLogPageOptions.OpenRepository(repository!),
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

    public RepositoryGroupMode GroupByMode
    {
        get; set => SetProperty(ref field, value);
    } = RepositoryGroupMode.ParentFolder;

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
    public ICommand AddRepositoryCommand { get; }
    public ICommand RemoveRepositoryCommand { get; }
    public ICommand ClearCommand { get; }
    public ICommand DropCommand { get; }
    public ICommand CloneCommand { get; }
    public ICommand EditGroupCommand => CloneCommand;
    public ICommand CancelCloneCommand { get; }
    public ICommand ExecuteCloneCommand { get; }

    public string? ActiveCloneDirectory
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CloneDestinationHint)));
            }
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1056:URI-like properties should not be strings", Justification = "Git repository URLs can be SSH URLs, paths, or arbitrary string remotes.")]
    public string CloneUrl
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CloneDestinationHint)));
            }
        }
    } = string.Empty;

    public string CloneBranch
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    public string CloneDestinationHint
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ActiveCloneDirectory))
            {
                return string.Empty;
            }
            string? name = GetRepositoryNameFromUrl(CloneUrl);
            return string.IsNullOrEmpty(name)
                ? $"Clone to {ActiveCloneDirectory}"
                : $"Clone to {Path.Combine(ActiveCloneDirectory, name)}";
        }
    }

    private static string? GetRepositoryNameFromUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }
        string trimmed = url.TrimEnd('/', '\\');
        int slashIndex = Math.Max(trimmed.LastIndexOf('/'), trimmed.LastIndexOf('\\'));
        string name = slashIndex >= 0 ? trimmed[(slashIndex + 1)..] : trimmed;
        if (name.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
        {
            name = name[..^4];
        }
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

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

    private bool SetProperty<T>(ref T prop, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!ReferenceEquals(prop, value))
        {
            prop = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
        return false;
    }
}

public enum RepositoryGroupMode
{
    None,
    Single,
    ParentFolder,
}
