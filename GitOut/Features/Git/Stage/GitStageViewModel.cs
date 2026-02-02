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

namespace GitOut.Features.Git.Stage
{
    public class GitStageViewModel
        : INotifyPropertyChanged,
            INavigationListener,
            INavigationFallback,
            IDisposable
    {
        private readonly ISnackbarService snack;
        private readonly IOptionsMonitor<GitStageOptions> stagingOptions;
        private readonly IRepositoryWatcher repositoryWatcher;
        private readonly IDisposable stagingOptionsHandle;

        private readonly ObservableCollection<StatusChangeViewModel> workspaceFiles = new();
        private readonly object workspaceFilesLock = new();
        private readonly ObservableCollection<StatusChangeViewModel> indexFiles = new();
        private readonly object indexFilesLock = new();

        private StatusChangeViewModel? selectedChange;
        private DiffContext? selectedDiffResult;

        private ICollectionView? amendFilesView;
        private IGitFileEntryViewModel? selectedAmendChange;

        private int selectedWorkspaceIndex;
        private int selectedIndexIndex;

        private bool showSpacesAsDots;
        private bool diffWhitespace;
        private bool amendLastCommit;
        private bool checkoutBranchBeforeCommit;

        private CancellationTokenSource? cancelRefreshSnack;
        private bool hasChanges;
        private bool selectedFileHasChanges;
        private bool refreshAutomatically;

        private string commitMessage = string.Empty;
        private string newBranchName = string.Empty;
        private string cachedCommitMessage = string.Empty;
        private EditPatchViewModel? editHunk;
        private GitPatch? undoPatch;

        public GitStageViewModel(
            INavigationService navigation,
            ITitleService title,
            IGitRepositoryWatcherProvider watchProvider,
            ISnackbarService snack,
            IOptionsMonitor<GitStageOptions> stagingOptions
        )
        {
            GitStagePageOptions options =
                navigation.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName!)
                ?? throw new ArgumentNullException(nameof(options), "Options may not be null");
            this.snack = snack;
            this.stagingOptions = stagingOptions;
            Repository = options.Repository;
            title.Title = $"{Repository.Name} (Stage)";
            showSpacesAsDots = stagingOptions.CurrentValue.ShowSpacesAsDots;
            stagingOptionsHandle = stagingOptions.OnChange(options =>
                SetProperty(
                    ref showSpacesAsDots,
                    options.ShowSpacesAsDots,
                    nameof(ShowSpacesAsDots)
                )
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
                () => !amendLastCommit && indexFiles.Count > 0 && !checkoutBranchBeforeCommit
            );

            CommitCommand = new AsyncCallbackCommand(
                CommitChangesAsync,
                () =>
                    !string.IsNullOrEmpty(CommitMessage)
                    && (indexFiles.Count > 0 || amendLastCommit)
                    && (!checkoutBranchBeforeCommit || GitBranchName.IsValid(newBranchName))
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
            UndoPatchCommand = new AsyncCallbackCommand(
                UndoPatchAsync,
                () => undoPatch is not null
            );
            AddAllCommand = new AsyncCallbackCommand(
                StageAllFilesAsync,
                () => workspaceFiles.Count > 0
            );
            IntentToAddFileCommand = new AsyncCallbackCommand<StatusChangeViewModel>(
                IntentToAddFileAsync,
                CanIntentToAddFile
            );
            IntentToAddCommand = new AsyncCallbackCommand(IntentToAddAsync);
            ResetHeadCommand = new AsyncCallbackCommand(
                ResetAllFilesAsync,
                () => indexFiles.Count > 0
            );

            CancelEditTextCommand = new CallbackCommand(() => EditHunk = null);
            PatchEditTextCommand = new AsyncCallbackCommand(
                PatchEditSelectionAsync,
                () => editHunk is not null
            );
        }

        public IGitRepository Repository { get; }

        public bool ShowSpacesAsDots => showSpacesAsDots;

        public bool RefreshAutomatically
        {
            get => refreshAutomatically;
            set => SetProperty(ref refreshAutomatically, value);
        }

        public bool DiffWhitespace
        {
            get => diffWhitespace;
            set
            {
                if (SetProperty(ref diffWhitespace, value))
                {
                    if (selectedChange is not null)
                    {
                        _ = ExecuteDiffAsync();
                    }
                }
            }
        }

        public bool AmendLastCommit
        {
            get => amendLastCommit;
            set
            {
                SetProperty(ref amendLastCommit, value);
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
            get => checkoutBranchBeforeCommit;
            set => SetProperty(ref checkoutBranchBeforeCommit, value);
        }

        public string CommitMessage
        {
            get => commitMessage;
            set => SetProperty(ref commitMessage, value);
        }

        public string NewBranchName
        {
            get => newBranchName;
            set => SetProperty(ref newBranchName, value);
        }

        public int SelectedWorkspaceIndex
        {
            get => selectedWorkspaceIndex;
            set => SetProperty(ref selectedWorkspaceIndex, value);
        }

        public int SelectedIndexIndex
        {
            get => selectedIndexIndex;
            set => SetProperty(ref selectedIndexIndex, value);
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
            get => selectedAmendChange;
            set
            {
                if (selectedAmendChange is INotifyPropertyChanged unsubscribe)
                {
                    unsubscribe.PropertyChanged -= NotifyDiffResultPropertyChanged;
                }
                if (SetProperty(ref selectedAmendChange, value))
                {
                    SelectedDiffResult = null;
                    if (selectedAmendChange is INotifyPropertyChanged subscribe)
                    {
                        subscribe.PropertyChanged += NotifyDiffResultPropertyChanged;
                    }
                    if (selectedAmendChange is not null)
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
            get => editHunk;
            set => SetProperty(ref editHunk, value);
        }

        public DiffContext? SelectedDiffResult
        {
            get => selectedDiffResult;
            set => SetProperty(ref selectedDiffResult, value);
        }

        public ICollectionView IndexFiles { get; }
        public ICollectionView WorkspaceFiles { get; }
        public ICollectionView? AmendFiles
        {
            get => amendFilesView;
            set => SetProperty(ref amendFilesView, value);
        }

        public ICommand RefreshStatusCommand { get; }
        public ICommand SetFocusCommand { get; }
        public ICommand MovePreviousCommand { get; }
        public ICommand MoveNextCommand { get; }

        public ICommand AddAllCommand { get; }
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
                        if (hasChanges && !(selectedFileHasChanges && !refreshAutomatically))
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

                        if (selectedFileHasChanges && selectedChange is not null)
                        {
                            if (refreshAutomatically)
                            {
                                await ExecuteDiffAsync();
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
                                                await ExecuteDiffAsync();
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
                stagingOptionsHandle.Dispose();
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
            if (selectedChange is not null)
            {
                await ExecuteDiffAsync();
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
            var logFiles = new SortedLazyAsyncCollection<
                IGitFileEntryViewModel,
                RelativeDirectoryPath
            >(
                relativePath =>
                    GitFileEntryViewModelFactory.DiffAllAsync(head.ParentId, head.Id, Repository),
                IGitDirectoryEntryViewModel.CompareItems
            );

            _ = logFiles.MaterializeAsync(RelativeDirectoryPath.Root).AsTask();
            AmendFiles = CollectionViewSource.GetDefaultView(logFiles);
        }

        private void ExecuteAmendDiff()
        {
            if (selectedAmendChange is GitFileViewModel viewmodel)
            {
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
            if (diffWhitespace)
            {
                optionsBuilder.IgnoreAllSpace();
            }
            if (location == StatusChangeLocation.Index)
            {
                optionsBuilder.Cached();
            }
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
                    await Repository.AddAsync(
                        item.Model,
                        AddOptions.Builder().WithIntent().Build()
                    );
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
                    previousIndex >= workspaceFiles.Count
                        ? workspaceFiles.Count - 1
                        : previousIndex;
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
                    previousIndex >= workspaceFiles.Count
                        ? workspaceFiles.Count - 1
                        : previousIndex;
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
                            await Repository
                                .RestoreWorkspaceAsync(model.Model)
                                .ConfigureAwait(false);
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
                throw new ArgumentNullException(nameof(selectedChange), "No change is selected");
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
                throw new ArgumentNullException(nameof(selectedChange), "No change is selected");
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
                transformBuilder.TrimLines();
            }
            if (stagingOptions.CurrentValue.TabTransformText.Length > 0)
            {
                transformBuilder.ConvertTabsToSpaces(stagingOptions.CurrentValue.TabTransformText);
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
                int previousIndex = selectedWorkspaceIndex;
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
            if (selectedChange is null || editHunk is null)
            {
                return;
            }
            var patch = GitPatch.Create(
                PatchMode.AddIndex,
                selectedChange.Model.Path,
                GitStatusChangeType.Ordinary,
                editHunk.GetHunkVisitor(PatchMode.AddIndex)
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

            GitCommitOptions options = amendLastCommit
                ? GitCommitOptions.AmendLatest(commitMessage)
                : GitCommitOptions.CreateCommit(commitMessage);
            await Repository.CommitAsync(options);
            snack.ShowSuccess("Commited changes successfully");
            await GetRepositoryStatusAsync();
            if (amendLastCommit)
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

        private bool SetProperty<T>(
            ref T prop,
            T value,
            [CallerMemberName] string? propertyName = null
        )
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
            builder.AddAction("DELETE");
            builder.AddAction("RECYCLE");
            builder.WithMessage(
                $"{Path.GetFileName(path)} is untracked. " + $"Do you want to delete the file?"
            );
            builder.WithDuration(Timeout.InfiniteTimeSpan);
            builder.WithCancellation(cancelRefreshSnack.Token);
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

        private static int FindSortedIndex<T>(IList<T> items, Func<T, int> compare)
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
}
