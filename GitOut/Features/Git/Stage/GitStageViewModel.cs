#pragma warning disable CA1506
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using GitOut.Features.Collections;
using GitOut.Features.Git.Diff;
using GitOut.Features.Git.Files;
using GitOut.Features.Git.Log;
using GitOut.Features.Git.Patch;
using GitOut.Features.IO;
using GitOut.Features.Material.Snackbar;
using GitOut.Features.Native.Shell32;
using GitOut.Features.Navigation;
using GitOut.Features.Text;
using GitOut.Features.Wpf;
using Microsoft.Extensions.Options;

namespace GitOut.Features.Git.Stage;

public class GitStageViewModel
    : INotifyPropertyChanged,
        INavigationListener,
        INavigationFallback,
        IDisposable
{
    private readonly ISnackbarService snack;
    private readonly IOptionsMonitor<GitStageOptions> stagingOptions;
    private readonly IRepositoryWatcher repositoryWatcher;
    private readonly IDisposable? stagingOptionsHandle;

    private readonly ObservableCollection<StatusChangeViewModel> workspaceFiles = [];
    private readonly object workspaceFilesLock = new();
    private readonly ObservableCollection<StatusChangeViewModel> indexFiles = [];
    private readonly object indexFilesLock = new();

    private StatusChangeViewModel? selectedChange;
    private bool showSpacesAsDots;
    private CancellationTokenSource? cancelRefreshSnack;
    private bool hasChanges;
    private bool selectedFileHasChanges;
    private CancellationTokenSource? refreshContextCancellationTokenSource;
    private string cachedCommitMessage = string.Empty;
    private GitPatch? undoPatch;

    public GitStageViewModel(
        INavigationService navigation,
        ITitleService title,
        IGitRepositoryWatcherProvider watchProvider,
        ISnackbarService snack,
        IOptionsMonitor<GitStageOptions> stagingOptions
    )
    {
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
        GitStagePageOptions options =
            navigation.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName!)
            ?? throw new ArgumentNullException(nameof(options), "Options may not be null");
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
        this.snack = snack;
        this.stagingOptions = stagingOptions;
        Repository = options.Repository;
        title.Title = $"{Repository.Name} (Stage)";
        showSpacesAsDots = stagingOptions.CurrentValue.ShowSpacesAsDots;
        stagingOptionsHandle = stagingOptions.OnChange(options =>
            SetProperty(ref showSpacesAsDots, options.ShowSpacesAsDots, nameof(ShowSpacesAsDots))
        );

        repositoryWatcher = watchProvider.PrepareWatchRepositoryChanges(Repository);
        repositoryWatcher.Events += OnFileSystemChanges;

        BindingOperations.EnableCollectionSynchronization(workspaceFiles, workspaceFilesLock);
        WorkspaceFiles = CollectionViewSource.GetDefaultView(workspaceFiles);
        BindingOperations.EnableCollectionSynchronization(indexFiles, indexFilesLock);
        IndexFiles = CollectionViewSource.GetDefaultView(indexFiles);

        RefreshStatusCommand = new AsyncCallbackCommand(GetRepositoryStatusAsync);

        SetFocusCommand = new NotNullCallbackCommand<UIElement>(control => control.Focus());
        MovePreviousCommand = new NotNullCallbackCommand<ListBox>(list =>
            list.SelectedIndex = Math.Clamp(list.SelectedIndex - 1, 0, list.Items.Count)
        );
        MoveNextCommand = new NotNullCallbackCommand<ListBox>(list =>
            list.SelectedIndex = Math.Clamp(list.SelectedIndex + 1, 0, list.Items.Count)
        );

        StashIndexCommand = new AsyncCallbackCommand(
            StashIndexAsync,
            () => !AmendLastCommit && indexFiles.Count > 0 && !CheckoutBranchBeforeCommit
        );

        CommitCommand = new AsyncCallbackCommand(
            CommitChangesAsync,
            () =>
                !string.IsNullOrEmpty(CommitMessage)
                && (indexFiles.Count > 0 || AmendLastCommit)
                && (!CheckoutBranchBeforeCommit || GitBranchName.IsValid(NewBranchName))
        );
        StageFileCommand = new AsyncCallbackCommand<StatusChangeViewModel>(StageFileAsync);
        StageWorkspaceFilesCommand = new AsyncCallbackCommand(StageWorkspaceFilesAsync);
        ResetWorkspaceFileCommand = new AsyncCallbackCommand<StatusChangeViewModel>(
            ResetWorkspaceFileAsync
        );
        ResetWorkspaceFilesCommand = new AsyncCallbackCommand(ResetWorkspaceFilesAsync);
        ResetIndexFileCommand = new AsyncCallbackCommand<StatusChangeViewModel>(
            ResetIndexFileAsync
        );
        ResetIndexFilesCommand = new AsyncCallbackCommand(ResetIndexFilesAsync);
        ResetSelectedTextCommand = new AsyncCallbackCommand<IHunkLineVisitorProvider>(
            ResetSelectionAsync,
            CanModifySelection
        );
        StageSelectedTextCommand = new AsyncCallbackCommand<IHunkLineVisitorProvider>(
            StageSelectionAsync,
            CanModifySelection
        );
        EditSelectedTextCommand = new CallbackCommand<IHunkLineVisitorProvider>(
            PrepareEditSelection,
            CanModifySelection
        );
        UndoPatchCommand = new AsyncCallbackCommand(UndoPatchAsync, () => undoPatch is not null);
        AddAllCommand = new AsyncCallbackCommand(
            StageAllFilesAsync,
            () => workspaceFiles.Count > 0
        );
        StageUpdatedCommand = new AsyncCallbackCommand(
            StageUpdatedFilesAsync,
            () => workspaceFiles.Count > 0 && indexFiles.Count > 0
        );
        IntentToAddFileCommand = new AsyncCallbackCommand<StatusChangeViewModel>(
            IntentToAddFileAsync,
            CanIntentToAddFile
        );
        IntentToAddCommand = new AsyncCallbackCommand(IntentToAddAsync);
        ResetHeadCommand = new AsyncCallbackCommand(ResetAllFilesAsync, () => indexFiles.Count > 0);

        CancelEditTextCommand = new CallbackCommand(() => EditHunk = null);
        PatchEditTextCommand = new AsyncCallbackCommand(
            PatchEditSelectionAsync,
            () => EditHunk is not null
        );
        DiffSelectedFilesCommand = new AsyncCallbackCommand(
            DiffSelectedFilesAsync,
            () =>
                workspaceFiles.Count(x => x.IsSelected) == 2
                || indexFiles.Count(x => x.IsSelected) == 2
        );
        DecreaseContextLinesCommand = new CallbackCommand(() =>
            ContextLines = Math.Max(0, ContextLines - 1)
        );
        IncreaseContextLinesCommand = new CallbackCommand(() =>
            ContextLines = Math.Min(100, ContextLines + 1)
        );
    }

    public IGitRepository Repository { get; }

    public bool ShowSpacesAsDots => showSpacesAsDots;

    public bool RefreshAutomatically
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool DiffWhitespace
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                _ = ExecuteCurrentDiffAsync();
            }
        }
    }

    public int ContextLines
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                PropertyChanged?.Invoke(
                    this,
                    new PropertyChangedEventArgs(nameof(MaxContextLines))
                );
                refreshContextCancellationTokenSource?.Cancel();
                refreshContextCancellationTokenSource = new CancellationTokenSource();
                CancellationToken token = refreshContextCancellationTokenSource.Token;

                _ = Task.Delay(TimeSpan.FromMilliseconds(300), token)
                    .ContinueWith(
                        t =>
                        {
                            if (!t.IsCanceled)
                            {
                                _ = Application.Current.Dispatcher.Invoke(ExecuteCurrentDiffAsync);
                            }
                        },
                        token
                    );
            }
        }
    } = 3;

    public int MaxContextLines => Math.Max(20, ContextLines);

    public bool ShowWholeFile
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                _ = ExecuteCurrentDiffAsync();
            }
        }
    }

    public bool AmendLastCommit
    {
        get;
        set
        {
            _ = SetProperty(ref field, value);
            if (value)
            {
                cachedCommitMessage = CommitMessage;
                _ = PrepareAmendAsync();
            }
            else
            {
                CommitMessage = cachedCommitMessage;
                AmendFiles = null;
            }
        }
    }

    public bool CheckoutBranchBeforeCommit
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string CommitMessage
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    public string NewBranchName
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    public int SelectedWorkspaceIndex
    {
        get;
        set => SetProperty(ref field, value);
    }

    public int SelectedIndexIndex
    {
        get;
        set => SetProperty(ref field, value);
    }

    public StatusChangeViewModel? SelectedChange
    {
        get => selectedChange;
        set
        {
            if (SetProperty(ref selectedChange, value))
            {
                SelectedDiffResult = null;
                cancelRefreshSnack?.Cancel();
                if (selectedChange is not null)
                {
                    SelectedAmendChange = null;
                    _ = ExecuteDiffAsync();
                }
            }
        }
    }

    public IGitFileEntryViewModel? SelectedAmendChange
    {
        get;
        set
        {
            if (field is INotifyPropertyChanged unsubscribe)
            {
                unsubscribe.PropertyChanged -= NotifyDiffResultPropertyChanged;
            }
            if (SetProperty(ref field, value))
            {
                SelectedDiffResult = null;
                if (field is INotifyPropertyChanged subscribe)
                {
                    subscribe.PropertyChanged += NotifyDiffResultPropertyChanged;
                }
                if (field is not null)
                {
                    SelectedChange = null;
                    ExecuteAmendDiff();
                }
            }

            void NotifyDiffResultPropertyChanged(object? sender, EventArgs e) =>
                SelectedDiffResult = (sender as GitFileViewModel)?.DiffResult;
        }
    }

    public EditPatchViewModel? EditHunk
    {
        get;
        set => SetProperty(ref field, value);
    }

    public DiffContext? SelectedDiffResult
    {
        get;
        set => SetProperty(ref field, value);
    }

    public ICollectionView IndexFiles { get; }
    public ICollectionView WorkspaceFiles { get; }
    public ICollectionView? AmendFiles
    {
        get;
        set => SetProperty(ref field, value);
    }

    public ICommand RefreshStatusCommand { get; }
    public ICommand SetFocusCommand { get; }
    public ICommand MovePreviousCommand { get; }
    public ICommand MoveNextCommand { get; }

    public ICommand AddAllCommand { get; }
    public ICommand StageUpdatedCommand { get; }
    public ICommand IntentToAddFileCommand { get; }
    public ICommand IntentToAddCommand { get; }
    public ICommand StageFileCommand { get; }
    public ICommand StageWorkspaceFilesCommand { get; }
    public ICommand ResetWorkspaceFileCommand { get; }
    public ICommand ResetWorkspaceFilesCommand { get; }
    public ICommand ResetIndexFileCommand { get; }
    public ICommand ResetIndexFilesCommand { get; }
    public ICommand ResetSelectedTextCommand { get; }
    public ICommand StageSelectedTextCommand { get; }
    public ICommand EditSelectedTextCommand { get; }
    public ICommand UndoPatchCommand { get; }
    public ICommand ResetHeadCommand { get; }
    public ICommand StashIndexCommand { get; }
    public ICommand CommitCommand { get; }
    public ICommand CancelEditTextCommand { get; }
    public ICommand PatchEditTextCommand { get; }
    public ICommand DiffSelectedFilesCommand { get; }
    public ICommand DecreaseContextLinesCommand { get; }
    public ICommand IncreaseContextLinesCommand { get; }

    public string FallbackPageName => typeof(GitLogPage).FullName!;
    public object? FallbackOptions => GitLogPageOptions.OpenRepository(Repository);

    public event PropertyChangedEventHandler? PropertyChanged;

    public async void Navigated(NavigationType type)
    {
        switch (type)
        {
            case NavigationType.Initial:
                await GetRepositoryStatusAsync();
                break;
            case NavigationType.NavigatedLeave:
                cancelRefreshSnack?.Cancel();
                repositoryWatcher.Events -= OnFileSystemChanges;
                break;
            case NavigationType.Deactivated:
                repositoryWatcher.EnableRaisingEvents = true;
                break;
            case NavigationType.Activated:
                {
                    repositoryWatcher.EnableRaisingEvents = false;
                    if (hasChanges && !(selectedFileHasChanges && !RefreshAutomatically))
                    {
                        const string RefreshedMessage =
                            "git out detected file changes and refreshed automatically";
                        _ = snack.ShowAsync(
                            Snack
                                .Builder()
                                .WithMessage(RefreshedMessage)
                                .WithDuration(TimeSpan.FromSeconds(4))
                        );
                    }
                    if (hasChanges)
                    {
                        ParseStatus(await Repository.StatusAsync());
                    }

                    if (
                        selectedFileHasChanges
                        && (selectedChange is not null || SelectedAmendChange is not null)
                    )
                    {
                        if (RefreshAutomatically)
                        {
                            if (
                                selectedChange is null
                                || selectedChange.Location == StatusChangeLocation.Workspace
                            )
                            {
                                await ExecuteCurrentDiffAsync();
                            }
                        }
                        else
                        {
                            cancelRefreshSnack?.Cancel();
                            cancelRefreshSnack = new CancellationTokenSource();
                            string refreshText = "REFRESH";
                            _ = snack
                                .ShowAsync(
                                    Snack
                                        .Builder()
                                        .WithMessage(
                                            "git out detected changes to selected file while window was inactive"
                                        )
                                        .WithDuration(TimeSpan.FromMinutes(1))
                                        .WithCancellation(cancelRefreshSnack.Token)
                                        .AddAction(refreshText)
                                )
                                .ContinueWith(
                                    async (task) =>
                                    {
                                        SnackAction? selectedAction = task.Result;
                                        if (selectedAction?.Text == refreshText)
                                        {
                                            if (
                                                selectedChange is null
                                                || selectedChange.Location
                                                    == StatusChangeLocation.Workspace
                                            )
                                            {
                                                await ExecuteCurrentDiffAsync();
                                            }
                                        }
                                    }
                                );
                        }
                    }
                    hasChanges = selectedFileHasChanges = false;
                }
                break;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            cancelRefreshSnack?.Dispose();
            repositoryWatcher.Dispose();
            stagingOptionsHandle?.Dispose();
            refreshContextCancellationTokenSource?.Dispose();
        }
    }

    private void OnFileSystemChanges(object sender, RepositoryWatcherEventArgs args)
    {
        hasChanges = true;
        selectedFileHasChanges |=
            SelectedChange is not null
            && SelectedChange.Location == StatusChangeLocation.Workspace
            && args.RepositoryPath == SelectedChange.Path;
    }

    private bool CanModifySelection(IHunkLineVisitorProvider? viewer) =>
        viewer is not null && selectedChange is not null;

    private bool CanIntentToAddFile(StatusChangeViewModel? model) =>
        model is not null && model.Model.Type == GitStatusChangeType.Untracked;

    private async Task GetRepositoryStatusAsync()
    {
        ParseStatus(await Repository.StatusAsync());
        if (selectedChange is null || selectedChange.Location == StatusChangeLocation.Workspace)
        {
            await ExecuteCurrentDiffAsync();
        }
    }

    private async Task PrepareAmendAsync()
    {
        try
        {
            GitHistoryEvent head = await Repository.GetHeadAsync();
            UpdateCommitMessageFromHead(head);
            RefreshAmendList(head);
        }
        catch (InvalidOperationException) { }
    }

    private void UpdateCommitMessageFromHead(GitHistoryEvent head) =>
        CommitMessage = string.IsNullOrEmpty(head.Body)
            ? head.Subject
            : $"{head.Subject}{Environment.NewLine}{Environment.NewLine}{head.Body}";

    private void RefreshAmendList(GitHistoryEvent head)
    {
        IDiffOptionsBuilder optionsBuilder = DiffOptions
            .Builder()
            .ContextLines(ShowWholeFile ? 999999 : ContextLines);
        if (DiffWhitespace)
        {
            _ = optionsBuilder.IgnoreAllSpace();
        }
        DiffOptions options = optionsBuilder.Build();

        var logFiles = new SortedLazyAsyncCollection<IGitFileEntryViewModel, RelativeDirectoryPath>(
            relativePath =>
                GitFileEntryViewModelFactory.DiffAllAsync(
                    head.ParentId,
                    head.Id,
                    Repository,
                    options
                ),
            IGitDirectoryEntryViewModel.CompareItems
        );

        _ = logFiles.MaterializeAsync(RelativeDirectoryPath.Root).AsTask();
        AmendFiles = CollectionViewSource.GetDefaultView(logFiles);
    }

    private async Task ExecuteCurrentDiffAsync()
    {
        List<StatusChangeViewModel> selectedLocal = [.. workspaceFiles.Where(x => x.IsSelected)];
        if (selectedLocal.Count != 2)
        {
            selectedLocal = [.. indexFiles.Where(x => x.IsSelected)];
        }

        if (selectedLocal.Count == 2)
        {
            await DiffSelectedFilesAsync();
        }
        else if (selectedChange is not null)
        {
            await ExecuteDiffAsync();
        }
        else if (SelectedAmendChange is not null)
        {
            ExecuteAmendDiff();
        }
    }

    private void ExecuteAmendDiff()
    {
        if (SelectedAmendChange is GitFileViewModel viewmodel)
        {
            IDiffOptionsBuilder optionsBuilder = DiffOptions
                .Builder()
                .ContextLines(ShowWholeFile ? 999999 : ContextLines);
            if (DiffWhitespace)
            {
                _ = optionsBuilder.IgnoreAllSpace();
            }
            viewmodel.UpdateOptions(optionsBuilder.Build());
            SelectedDiffResult = viewmodel.DiffResult;
        }
    }

    private async Task ExecuteDiffAsync()
    {
        if (selectedChange is null)
        {
            throw new ArgumentNullException(
                nameof(selectedChange),
                "Cannot perform status on null change"
            );
        }

        GitStatusChange change = selectedChange.Model;
        if (change.Path.IsDirectory)
        {
            return;
        }
        StatusChangeLocation location = selectedChange.Location;
        if (
            location == StatusChangeLocation.Index
            && (Monitor.IsEntered(indexFilesLock) || indexFiles.Count == 0)
        )
        {
            // we end up here if the selected index was changed while we are adding items to the list, so we ignore the request since it will be updated later
            return;
        }
        if (location == StatusChangeLocation.Index && change.SourceId! == change.DestinationId!)
        {
            SelectedDiffResult = null;
            return;
        }
        IDiffOptionsBuilder optionsBuilder = DiffOptions.Builder();
        if (DiffWhitespace)
        {
            _ = optionsBuilder.IgnoreAllSpace();
        }
        if (location == StatusChangeLocation.Index)
        {
            _ = optionsBuilder.Cached();
        }
        _ = optionsBuilder.ContextLines(ShowWholeFile ? 999999 : ContextLines);
        SelectedDiffResult = await DiffContext.DiffAsync(
            Repository,
            change,
            optionsBuilder.Build()
        );
    }

    private async Task StageAllFilesAsync()
    {
        await Repository.AddAllAsync();
        await GetRepositoryStatusAsync();
    }

    private async Task StageUpdatedFilesAsync()
    {
        undoPatch = null;
        int previousIndex = SelectedWorkspaceIndex;
        foreach (StatusChangeViewModel item in workspaceFiles)
        {
            if (indexFiles.Any(indexItem => indexItem.Path == item.Path))
            {
                await Repository.AddAsync(item.Model, AddOptions.None);
            }
        }
        await GetRepositoryStatusAsync();
        SelectedWorkspaceIndex =
            previousIndex >= workspaceFiles.Count ? workspaceFiles.Count - 1 : previousIndex;
    }

    private async Task IntentToAddFileAsync(StatusChangeViewModel? model)
    {
        if (model is null)
        {
            return;
        }

        await Repository.AddAsync(model.Model, AddOptions.Builder().WithIntent().Build());
        await GetRepositoryStatusAsync();
    }

    private async Task IntentToAddAsync()
    {
        foreach (StatusChangeViewModel item in workspaceFiles)
        {
            if (item.IsSelected)
            {
                await Repository.AddAsync(item.Model, AddOptions.Builder().WithIntent().Build());
            }
        }
        await GetRepositoryStatusAsync();
    }

    private async Task ResetAllFilesAsync()
    {
        await Repository.ResetAllAsync();
        await GetRepositoryStatusAsync();
    }

    private async Task StageFileAsync(StatusChangeViewModel? model)
    {
        if (model is null)
        {
            return;
        }

        if (model.Location == StatusChangeLocation.Index)
        {
            int previousIndex = SelectedIndexIndex;
            await Repository.ResetAsync(model.Model);
            await GetRepositoryStatusAsync();
            SelectedIndexIndex =
                previousIndex >= indexFiles.Count ? indexFiles.Count - 1 : previousIndex;
        }
        else
        {
            int previousIndex = SelectedWorkspaceIndex;
            await Repository.AddAsync(model.Model, AddOptions.None);
            await GetRepositoryStatusAsync();
            SelectedWorkspaceIndex =
                previousIndex >= workspaceFiles.Count ? workspaceFiles.Count - 1 : previousIndex;
        }
    }

    private async Task StageWorkspaceFilesAsync()
    {
        undoPatch = null;
        int previousIndex = SelectedWorkspaceIndex;
        foreach (StatusChangeViewModel item in workspaceFiles)
        {
            if (item.IsSelected)
            {
                await Repository.AddAsync(item.Model, AddOptions.None);
            }
        }
        await GetRepositoryStatusAsync();
        SelectedWorkspaceIndex =
            previousIndex >= workspaceFiles.Count ? workspaceFiles.Count - 1 : previousIndex;
    }

    private async Task ResetWorkspaceFileAsync(StatusChangeViewModel? model)
    {
        if (model is null)
        {
            return;
        }

        if (model.Location == StatusChangeLocation.Workspace)
        {
            int previousIndex = SelectedWorkspaceIndex;

            switch (model.Status)
            {
                case GitModifiedStatusType.Added:
                    await Repository.RestoreAsync(model.Model).ConfigureAwait(false);
                    break;
                case GitModifiedStatusType.Untracked:
                    await DeleteFileSnackAsync(model.FullPath).ConfigureAwait(false);
                    break;
                default:
                    await Repository.CheckoutAsync(model.Model).ConfigureAwait(false);
                    break;
            }
            await GetRepositoryStatusAsync().ConfigureAwait(false);
            SelectedWorkspaceIndex =
                previousIndex >= workspaceFiles.Count ? workspaceFiles.Count - 1 : previousIndex;
        }
    }

    private async Task ResetWorkspaceFilesAsync()
    {
        undoPatch = null;
        int previousIndex = SelectedWorkspaceIndex;
        foreach (StatusChangeViewModel model in workspaceFiles)
        {
            if (model.IsSelected)
            {
                switch (model.Status)
                {
                    case GitModifiedStatusType.Added:
                        await Repository.RestoreAsync(model.Model).ConfigureAwait(false);
                        break;
                    case GitModifiedStatusType.Untracked:
                        await DeleteFileSnackAsync(model.FullPath).ConfigureAwait(false);
                        break;
                    default:
                        await Repository.RestoreWorkspaceAsync(model.Model).ConfigureAwait(false);
                        break;
                }
            }
        }
        await GetRepositoryStatusAsync().ConfigureAwait(false);
        SelectedWorkspaceIndex =
            previousIndex >= workspaceFiles.Count ? workspaceFiles.Count - 1 : previousIndex;
    }

    private async Task ResetIndexFileAsync(StatusChangeViewModel? model)
    {
        if (model is null)
        {
            return;
        }

        int previousIndex = SelectedIndexIndex;
        if (model.Location == StatusChangeLocation.Index)
        {
            await Repository.ResetAsync(model.Model);
        }

        await GetRepositoryStatusAsync();
        SelectedIndexIndex =
            previousIndex >= indexFiles.Count ? indexFiles.Count - 1 : previousIndex;
    }

    private async Task ResetIndexFilesAsync()
    {
        undoPatch = null;
        int previousIndex = SelectedIndexIndex;
        foreach (StatusChangeViewModel item in indexFiles)
        {
            if (item.IsSelected)
            {
                await Repository.ResetAsync(item.Model);
            }
        }
        await GetRepositoryStatusAsync();
        SelectedIndexIndex =
            previousIndex >= indexFiles.Count ? indexFiles.Count - 1 : previousIndex;
    }

    private async Task ResetSelectionAsync(IHunkLineVisitorProvider? viewer)
    {
        if (viewer is null)
        {
            return;
        }

        if (selectedChange is null)
        {
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
            throw new ArgumentNullException(nameof(selectedChange), "No change is selected");
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
        }
        if (selectedChange.Location == StatusChangeLocation.Workspace)
        {
            IHunkLineVisitor? visitor = viewer.GetHunkVisitor(PatchMode.AddWorkspace);
            if (visitor is null)
            {
                return;
            }
            undoPatch = GitPatch.Create(
                PatchMode.AddWorkspace,
                selectedChange.Model.Path,
                GitStatusChangeType.Ordinary,
                visitor
            );
            visitor = viewer.GetHunkVisitor(PatchMode.ResetWorkspace);
            if (visitor is null)
            {
                return;
            }
            var patch = GitPatch.Create(
                PatchMode.ResetWorkspace,
                selectedChange.Model.Path,
                selectedChange.Status == GitModifiedStatusType.Added
                    ? GitStatusChangeType.Untracked
                    : GitStatusChangeType.Ordinary,
                visitor
            );
            string filename = Path.GetFileName(selectedChange.Path);
            const string undoText = "UNDO";
            await Repository.ApplyAsync(patch);
            _ = snack
                .ShowAsync(
                    Snack
                        .Builder()
                        .WithMessage($"Changes reset in {filename}")
                        .WithDuration(TimeSpan.FromSeconds(5))
                        .AddAction(undoText)
                )
                .ContinueWith(async task =>
                {
                    SnackAction? action = task.Result;
                    if (action?.Text == undoText)
                    {
                        await UndoPatchAsync();
                    }
                });
        }
        else
        {
            IHunkLineVisitor? visitor = viewer.GetHunkVisitor(PatchMode.ResetIndex);
            if (visitor is null)
            {
                return;
            }
            var patch = GitPatch.Create(
                PatchMode.ResetIndex,
                selectedChange.Model.Path,
                selectedChange.Status == GitModifiedStatusType.Added
                    ? GitStatusChangeType.Untracked
                    : GitStatusChangeType.Ordinary,
                visitor
            );
            await Repository.ApplyAsync(patch);
        }
        await GetRepositoryStatusAsync();
    }

    private async Task StageSelectionAsync(IHunkLineVisitorProvider? viewer)
    {
        if (viewer is null)
        {
            return;
        }

        if (selectedChange is null)
        {
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
            throw new ArgumentNullException(nameof(selectedChange), "No change is selected");
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
        }
        if (selectedChange.Location != StatusChangeLocation.Workspace)
        {
            snack.Show("Sorry, can only stage from workspace");
            return;
        }
        IHunkLineVisitor? hunks = viewer.GetHunkVisitor(PatchMode.AddIndex);
        if (hunks is null)
        {
            return;
        }
        IPatchLineTransformBuilder transformBuilder = PatchLineTransform.Builder();
        if (stagingOptions.CurrentValue.TrimLineEndings)
        {
            _ = transformBuilder.TrimLines();
        }
        if (stagingOptions.CurrentValue.TabTransformText.Length > 0)
        {
            _ = transformBuilder.ConvertTabsToSpaces(stagingOptions.CurrentValue.TabTransformText);
        }
        ITextTransform transform = transformBuilder.Build();

        try
        {
            var patch = GitPatch.Create(
                PatchMode.AddIndex,
                selectedChange.Model.Path,
                selectedChange.Status == GitModifiedStatusType.Untracked
                    ? GitStatusChangeType.Untracked
                    : GitStatusChangeType.Ordinary,
                hunks,
                transform
            );
            int previousIndex = SelectedWorkspaceIndex;
            await Repository.ApplyAsync(patch);
            await GetRepositoryStatusAsync();
            if (selectedChange is null)
            {
                SelectedWorkspaceIndex = previousIndex;
            }
            else if (workspaceFiles.Count > 0)
            {
                int index;
                lock (workspaceFilesLock)
                {
                    index = FindSortedIndex(
                        workspaceFiles,
                        item => selectedChange.Path.CompareTo(item.Path)
                    );
                }
                if (
                    index < workspaceFiles.Count
                    && workspaceFiles[index].Path == selectedChange.Path
                )
                {
                    await ExecuteDiffAsync();
                }
            }
        }
        catch (InvalidOperationException ioe)
        {
            snack.ShowError("Could not create patch from selection", ioe);
        }
    }

    private async Task UndoPatchAsync()
    {
        if (undoPatch is null)
        {
            return;
        }
        await Repository.ApplyAsync(undoPatch);
        await GetRepositoryStatusAsync();
        undoPatch = null;
    }

    private void PrepareEditSelection(IHunkLineVisitorProvider? viewer)
    {
        if (viewer is null)
        {
            return;
        }

        if (selectedChange is null)
        {
            return;
        }
        IHunkLineVisitor? hunks = viewer.GetHunkVisitor(PatchMode.AddIndex);
        if (hunks is null)
        {
            return;
        }
        try
        {
            EditHunk = EditPatchViewModel.StageFrom(hunks);
        }
        catch (InvalidOperationException ioe)
        {
            snack.ShowError("Could not edit selected text", ioe);
        }
    }

    private async Task PatchEditSelectionAsync()
    {
        if (selectedChange is null || EditHunk is null)
        {
            return;
        }
        var patch = GitPatch.Create(
            PatchMode.AddIndex,
            selectedChange.Model.Path,
            GitStatusChangeType.Ordinary,
            EditHunk.GetHunkVisitor(PatchMode.AddIndex)
        );

        await Repository.ApplyAsync(patch);
        EditHunk = null;
        snack.ShowSuccess("Staged edit");
        await GetRepositoryStatusAsync();
    }

    private async Task StashIndexAsync()
    {
        await Repository.StashIndexAsync();
        snack.ShowSuccess("Stashed index successfully");
        await GetRepositoryStatusAsync();
    }

    private async Task CommitChangesAsync()
    {
        if (CheckoutBranchBeforeCommit)
        {
            try
            {
                await Repository.CheckoutBranchAsync(
                    GitBranchName.CreateLocal(NewBranchName),
                    new GitCheckoutBranchOptions(true)
                );
                NewBranchName = string.Empty;
                snack.ShowSuccess("Created new branch");
            }
            catch (InvalidOperationException e)
            {
                snack.ShowError(e.Message, e, TimeSpan.FromSeconds(10));
                return;
            }
        }

        GitCommitOptions options = AmendLastCommit
            ? GitCommitOptions.AmendLatest(CommitMessage)
            : GitCommitOptions.CreateCommit(CommitMessage);
        await Repository.CommitAsync(options);
        snack.ShowSuccess("Commited changes successfully");
        await GetRepositoryStatusAsync();
        if (AmendLastCommit)
        {
            GitHistoryEvent head = await Repository.GetHeadAsync();
            RefreshAmendList(head);
        }
        else
        {
            CommitMessage = string.Empty;
        }
        SelectedChange = null;
    }

    private void ParseStatus(GitStatusResult result)
    {
        foreach (GitStatusChange change in result.Changes)
        {
            if (
                change.IndexStatus.HasValue
                && change.IndexStatus != GitModifiedStatusType.Unmodified
            )
            {
                AddChangeToIndex(change);
            }
            if (
                change.WorkspaceStatus != GitModifiedStatusType.Unmodified
                || change.Type == GitStatusChangeType.Untracked
            )
            {
                AddChangeToWorkspace(change);
            }
        }
        lock (indexFilesLock)
        {
            for (int i = 0; i < indexFiles.Count; ++i)
            {
                StatusChangeViewModel item = indexFiles[i];
                if (
                    result
                        .Changes.Where(res =>
                            res.IndexStatus.HasValue
                            && res.IndexStatus != GitModifiedStatusType.Unmodified
                        )
                        .All(res => res.Path.ToString() != item.Model.Path.ToString())
                )
                {
                    indexFiles.RemoveAt(i--);
                }
            }
        }
        lock (workspaceFilesLock)
        {
            for (int i = 0; i < workspaceFiles.Count; ++i)
            {
                StatusChangeViewModel item = workspaceFiles[i];
                if (
                    result.Changes.Count == 0
                    || result.Changes.All(res =>
                        res.Path.ToString() != item.Model.Path.ToString()
                        || (
                            res.WorkspaceStatus.HasValue
                            && res.WorkspaceStatus == GitModifiedStatusType.Unmodified
                        )
                    )
                )
                {
                    if (selectedChange == item)
                    {
                        SelectedChange = null;
                    }
                    workspaceFiles.RemoveAt(i--);
                }
            }
        }
    }

    private void AddChangeToWorkspace(GitStatusChange change)
    {
        if (workspaceFiles.Count == 0)
        {
            lock (workspaceFilesLock)
            {
                workspaceFiles.Add(StatusChangeViewModel.AsWorkspace(change));
            }
            return;
        }
        int index = FindSortedIndex(
            workspaceFiles,
            item => change.Path.ToString().CompareTo(item.Path.ToString())
        );
        if (index >= workspaceFiles.Count)
        {
            lock (workspaceFilesLock)
            {
                workspaceFiles.Insert(index, StatusChangeViewModel.AsWorkspace(change));
            }
        }
        else if (workspaceFiles[index].Path.ToString() == change.Path.ToString())
        {
            if (workspaceFiles[index].Model.WorkspaceStatus != change.WorkspaceStatus)
            {
                lock (workspaceFilesLock)
                {
                    workspaceFiles.RemoveAt(index);
                    workspaceFiles.Insert(index, StatusChangeViewModel.AsWorkspace(change));
                }
            }
        }
        else
        {
            lock (workspaceFilesLock)
            {
                workspaceFiles.Insert(index, StatusChangeViewModel.AsWorkspace(change));
            }
        }
    }

    private void AddChangeToIndex(GitStatusChange change)
    {
        if (indexFiles.Count == 0)
        {
            lock (indexFilesLock)
            {
                indexFiles.Add(StatusChangeViewModel.AsStaged(change));
            }
            return;
        }
        int index = FindSortedIndex(
            indexFiles,
            item => change.Path.ToString().CompareTo(item.Path.ToString())
        );
        if (index >= indexFiles.Count)
        {
            lock (indexFilesLock)
            {
                indexFiles.Insert(index, StatusChangeViewModel.AsStaged(change));
            }
        }
        else if (indexFiles[index].Path.ToString() == change.Path.ToString())
        {
            if (indexFiles[index].Model.IndexStatus != change.IndexStatus)
            {
                lock (indexFilesLock)
                {
                    indexFiles.RemoveAt(index);
                    indexFiles.Insert(index, StatusChangeViewModel.AsStaged(change));
                }
            }
        }
        else
        {
            lock (indexFilesLock)
            {
                indexFiles.Insert(index, StatusChangeViewModel.AsStaged(change));
            }
        }
    }

    private async Task DiffSelectedFilesAsync()
    {
        List<StatusChangeViewModel> selected = [.. workspaceFiles.Where(x => x.IsSelected)];
        if (selected.Count != 2)
        {
            selected = [.. indexFiles.Where(x => x.IsSelected)];
        }

        IDiffOptionsBuilder optionsBuilder = DiffOptions
            .Builder()
            .ContextLines(ShowWholeFile ? 999999 : ContextLines);
        if (DiffWhitespace)
        {
            _ = optionsBuilder.IgnoreAllSpace();
        }

        SelectedDiffResult = await DiffContext.DiffFilesAsync(
            Repository,
            selected[0].Model.Path,
            selected[1].Model.Path,
            optionsBuilder.Build()
        );
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

    private async Task DeleteFileSnackAsync(string path)
    {
        cancelRefreshSnack?.Cancel();
        cancelRefreshSnack = new CancellationTokenSource();
        ISnackBuilder? builder = Snack.Builder();
        _ = builder.AddAction("DELETE");
        _ = builder.AddAction("RECYCLE");
        _ = builder.WithMessage(
            $"{Path.GetFileName(path)} is untracked. " + $"Do you want to delete the file?"
        );
        _ = builder.WithDuration(Timeout.InfiniteTimeSpan);
        _ = builder.WithCancellation(cancelRefreshSnack.Token);
        SnackAction? action = await snack.ShowAsync(builder);

        if (action?.Text == "DELETE")
        {
            File.Delete(path);
        }
        else if (action?.Text == "RECYCLE")
        {
            FileOperations.MoveFileToRecycleBin(path);
        }
    }

    private static int FindSortedIndex<T>(ObservableCollection<T> items, Func<T, int> compare)
    {
        int start = 0;
        int middle = items.Count / 2;
        int end = items.Count;

        do
        {
            if (middle == start)
            {
                return compare(items[middle]) > 0 ? end : start;
            }
            if (middle == end)
            {
                return end;
            }
            int slice = compare(items[middle]);
            if (slice == 0)
            {
                return middle;
            }

            if (slice > 0)
            {
                start = middle;
                middle = (int)Math.Ceiling(Math.FusedMultiplyAdd(end - start, .5, start));
            }
            else
            {
                end = middle;
                middle = (int)Math.Floor(Math.FusedMultiplyAdd(end - start, .5, start));
            }
        } while (true);
    }
}
