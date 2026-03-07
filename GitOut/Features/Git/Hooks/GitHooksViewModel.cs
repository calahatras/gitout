using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using GitOut.Features.Git.Log;
using GitOut.Features.Git.Storage;
using GitOut.Features.Material.Snackbar;
using GitOut.Features.Navigation;
using GitOut.Features.Wpf;

namespace GitOut.Features.Git.Hooks;

public class GitHooksViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IGitRepository repository;
    private readonly IGitRepositoryStorage storage;
    private readonly IGitHookManager hookManager;
    private readonly ISnackbarService snacks;
    private readonly IDisposable subscription;
    private GitHookType selectedHookType;

    public GitHooksViewModel(
        ITitleService title,
        ISnackbarService snacks,
        IGitRepositoryStorage storage,
        INavigationService navigation,
        IGitHookManager hookManager
    )
    {
        this.snacks = snacks;
        this.storage = storage;
        this.hookManager = hookManager;
        GitHooksPageOptions options =
            navigation.GetOptions<GitHooksPageOptions>(typeof(GitHooksPage).FullName!)
            ?? throw new InvalidOperationException("Options may not be null");
        repository = options.Repository;

        title.Title = $"Hooks - {repository.Name}";

        AvailableHooks = Enum.GetValues(typeof(GitHookType)).Cast<GitHookType>();
        selectedHookType = AvailableHooks.First();
        UpdateScriptContentAsync();

        SaveCommand = new AsyncCallbackCommand(async () =>
        {
            await hookManager.SaveHookAsync(
                repository,
                new GitHook(SelectedHookType, ScriptContent)
            );
            snacks.ShowSuccess($"Saved {SelectedHookType} hook");
        });

        CopyToCommand = new AsyncCallbackCommand(
            async () =>
            {
                if (SelectedTargetRepository is not null)
                {
                    await hookManager.CopyHookAsync(
                        repository,
                        SelectedTargetRepository,
                        SelectedHookType
                    );
                    snacks.ShowSuccess(
                        $"Copied {SelectedHookType} hook to {SelectedTargetRepository.Name}"
                    );
                }
            },
            () => SelectedTargetRepository is not null
        );

        subscription = storage.Repositories.Subscribe(repos => TargetRepositories = repos
                .Where(r => r.WorkingDirectory.Directory != repository.WorkingDirectory.Directory)
                .ToList());
    }

    public void Dispose() => subscription.Dispose();

    public IEnumerable<GitHookType> AvailableHooks { get; }

    public GitHookType SelectedHookType
    {
        get => selectedHookType;
        set
        {
            if (SetProperty(ref selectedHookType, value))
            {
                UpdateScriptContentAsync();
            }
        }
    }

    public string ScriptContent
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    public IEnumerable<IGitRepository> TargetRepositories
    {
        get;
        set => SetProperty(ref field, value);
    } = Array.Empty<IGitRepository>();

    public IGitRepository? SelectedTargetRepository
    {
        get;
        set => SetProperty(ref field, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand CopyToCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

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

    private async void UpdateScriptContentAsync()
    {
        GitHook? hook = await hookManager.GetHookAsync(repository, selectedHookType);
        ScriptContent = hook?.Content ?? string.Empty;
    }
}
