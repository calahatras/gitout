using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using GitOut.Features.Git.Diff;
using GitOut.Features.Git.Patch;
using GitOut.Features.IO;
using GitOut.Features.Material.Snackbar;
using GitOut.Features.Navigation;
using GitOut.Features.Text;
using GitOut.Features.Wpf;
using Microsoft.Extensions.Options;

namespace GitOut.Features.Git.Stage
{
    public class GitStageViewModel : INotifyPropertyChanged, INavigationListener
    {
        private readonly ISnackbarService snack;
        private readonly IOptionsMonitor<GitStageOptions> stagingOptions;
        private readonly IRepositoryWatcher repositoryWatcher;

        private readonly ObservableCollection<StatusChangeViewModel> workspaceFiles = new();
        private readonly object workspaceFilesLock = new();
        private readonly ObservableCollection<StatusChangeViewModel> indexFiles = new();
        private readonly object indexFilesLock = new();

        private StatusChangeViewModel? selectedChange;
        private GitDiffResult? selectedDiffResult;

        private int selectedWorkspaceIndex;
        private int selectedIndexIndex;

        private bool diffWhitespace;
        private bool amendLastCommit;

        private CancellationTokenSource? previousCancellation;
        private bool hasChanges = false;
        private bool selectedFileHasChanges = false;
        private bool refreshAutomatically = false;

        private string commitMessage = string.Empty;
        private string cachedCommitMessage = string.Empty;
        private EditPatchViewModel? editHunk = null;
        private GitPatch? undoPatch;

        public GitStageViewModel(
            INavigationService navigation,
            ITitleService title,
            IGitRepositoryWatcherProvider watchProvider,
            ISnackbarService snack,
            IOptionsMonitor<GitStageOptions> stagingOptions
        )
        {
            GitStagePageOptions options = navigation.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName!)
                ?? throw new ArgumentNullException(nameof(options), "Options may not be null");
            title.Title = "Stage";
            this.snack = snack;
            this.stagingOptions = stagingOptions;
            Repository = options.Repository;
            ShowSpacesAsDots = stagingOptions.CurrentValue.ShowSpacesAsDots;

            repositoryWatcher = watchProvider.PrepareWatchRepositoryChanges(Repository);
            repositoryWatcher.Events += OnFileSystemChanges;

            BindingOperations.EnableCollectionSynchronization(workspaceFiles, workspaceFilesLock);
            WorkspaceFiles = CollectionViewSource.GetDefaultView(workspaceFiles);
            BindingOperations.EnableCollectionSynchronization(indexFiles, indexFilesLock);
            IndexFiles = CollectionViewSource.GetDefaultView(indexFiles);

            RefreshStatusCommand = new AsyncCallbackCommand(GetRepositoryStatusAsync);
            CommitCommand = new AsyncCallbackCommand(CommitChangesAsync, () => !string.IsNullOrEmpty(CommitMessage) && (indexFiles.Count > 0 || amendLastCommit));
            StageFileCommand = new AsyncCallbackCommand<StatusChangeViewModel>(StageFileAsync);
            StageWorkspaceFilesCommand = new AsyncCallbackCommand(StageWorkspaceFilesAsync);
            ResetWorkspaceFilesCommand = new AsyncCallbackCommand(ResetWorkspaceFilesAsync);
            ResetIndexFilesCommand = new AsyncCallbackCommand(ResetIndexFilesAsync);
            ResetSelectedTextCommand = new AsyncCallbackCommand<IHunkLineVisitorProvider>(ResetSelectionAsync);
            StageSelectedTextCommand = new AsyncCallbackCommand<IHunkLineVisitorProvider>(StageSelectionAsync);
            EditSelectedTextCommand = new CallbackCommand<IHunkLineVisitorProvider>(PrepareEditSelection);
            UndoPatchCommand = new AsyncCallbackCommand(UndoPatchAsync, () => undoPatch is not null);
            AddAllCommand = new AsyncCallbackCommand(StageAllFilesAsync);
            IntentToAddCommand = new AsyncCallbackCommand(IntentToAddAsync);
            ResetHeadCommand = new AsyncCallbackCommand(ResetAllFilesAsync);

            CancelEditTextCommand = new CallbackCommand(() => EditHunk = null);
            PatchEditTextCommand = new AsyncCallbackCommand(PatchEditSelectionAsync, () => editHunk is not null);
        }

        public IGitRepository Repository { get; }

        public bool ShowSpacesAsDots { get; }

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
                    _ = SetAppendCommitMessageAsync();
                }
                else
                {
                    CommitMessage = cachedCommitMessage;
                }
            }
        }

        public string CommitMessage
        {
            get => commitMessage;
            set => SetProperty(ref commitMessage, value);
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
                    if (selectedChange is not null)
                    {
                        _ = ExecuteDiffAsync();
                    }
                }
            }
        }

        public EditPatchViewModel? EditHunk
        {
            get => editHunk;
            set => SetProperty(ref editHunk, value);
        }

        public GitDiffResult? SelectedDiffResult
        {
            get => selectedDiffResult;
            set => SetProperty(ref selectedDiffResult, value);
        }

        public ICollectionView IndexFiles { get; }
        public ICollectionView WorkspaceFiles { get; }

        public ICommand RefreshStatusCommand { get; }
        public ICommand AddAllCommand { get; }
        public ICommand IntentToAddCommand { get; }
        public ICommand StageFileCommand { get; }
        public ICommand StageWorkspaceFilesCommand { get; }
        public ICommand ResetWorkspaceFilesCommand { get; }
        public ICommand ResetIndexFilesCommand { get; }
        public ICommand ResetSelectedTextCommand { get; }
        public ICommand StageSelectedTextCommand { get; }
        public ICommand EditSelectedTextCommand { get; }
        public ICommand UndoPatchCommand { get; }
        public ICommand ResetHeadCommand { get; }
        public ICommand CommitCommand { get; }
        public ICommand CancelEditTextCommand { get; }
        public ICommand PatchEditTextCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public async void Navigated(NavigationType type)
        {
            switch (type)
            {
                case NavigationType.Initial:
                    await GetRepositoryStatusAsync();
                    break;
                case NavigationType.NavigatedLeave:
                    repositoryWatcher.Events -= OnFileSystemChanges;
                    repositoryWatcher.Dispose();
                    break;
                case NavigationType.Deactivated:
                    repositoryWatcher.EnableRaisingEvents = true;
                    break;
                case NavigationType.Activated:
                    {
                        repositoryWatcher.EnableRaisingEvents = false;
                        if (hasChanges && !(selectedFileHasChanges && !refreshAutomatically))
                        {
                            const string RefreshedMessage = "git out detected file changes and refreshed automatically";
                            _ = snack.ShowAsync(Snack.Builder()
                                .WithMessage(RefreshedMessage)
                                .WithDuration(TimeSpan.FromSeconds(4))
                            );
                        }
                        if (hasChanges)
                        {
                            ParseStatus(await Repository.StatusAsync());
                        }

                        if (selectedFileHasChanges)
                        {
                            if (refreshAutomatically)
                            {
                                await ExecuteDiffAsync();
                            }
                            else
                            {
                                if (previousCancellation is not null)
                                {
                                    previousCancellation.Cancel();
                                }
                                previousCancellation = new CancellationTokenSource();
                                string refreshText = "REFRESH";
                                _ = snack.ShowAsync(Snack.Builder()
                                    .WithMessage("git out detected changes to selected file while window was inactive")
                                    .WithDuration(TimeSpan.FromMinutes(1))
                                    .WithCancellation(previousCancellation.Token)
                                    .AddAction(refreshText)
                                ).ContinueWith(async (task) =>
                                {
                                    SnackAction? selectedAction = task.Result;
                                    if (selectedAction?.Text == refreshText)
                                    {
                                        await ExecuteDiffAsync();
                                    }
                                });
                            }
                        }
                        hasChanges = selectedFileHasChanges = false;
                    }
                    break;
            }
        }

        private void OnFileSystemChanges(object sender, RepositoryWatcherEventArgs args)
        {
            hasChanges = true;
            selectedFileHasChanges |= !(SelectedChange is null)
                && SelectedChange.Location == StatusChangeLocation.Workspace
                && args.RepositoryPath == SelectedChange.Path;
        }

        private async Task GetRepositoryStatusAsync()
        {
            ParseStatus(await Repository.StatusAsync());
            if (selectedChange is not null)
            {
                await ExecuteDiffAsync();
            }
        }

        private async Task SetAppendCommitMessageAsync()
        {
            try
            {
                GitHistoryEvent head = await Repository.GetHeadAsync();
                CommitMessage = string.IsNullOrEmpty(head.Body)
                    ? head.Subject
                    : $"{head.Subject}{Environment.NewLine}{Environment.NewLine}{head.Body}";
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (InvalidOperationException) { }
#pragma warning restore CA1031 // Do not catch general exception types
        }

        private async Task ExecuteDiffAsync()
        {
            if (selectedChange is null)
            {
                throw new ArgumentNullException(nameof(selectedChange), "Cannot perform status on null change");
            }

            GitStatusChange change = selectedChange.Model;
            StatusChangeLocation location = selectedChange.Location;
            if (change.Type == GitStatusChangeType.Untracked)
            {
                GitDiffResult result = await Repository.UntrackedDiffAsync(change.Path);
                SelectedDiffResult = result;
            }
            else if (location == StatusChangeLocation.Index && (Monitor.IsEntered(indexFilesLock) || indexFiles.Count == 0))
            {
                // we end up here if the selected index was changed while we are adding items to the list, so we ignore the request since it will be updated later
                return;
            }
            else if (location == StatusChangeLocation.Index && change.SourceId! == change.DestinationId!)
            {
                SelectedDiffResult = null;
            }
            else
            {
                IDiffOptionsBuilder optionsBuilder = DiffOptions.Builder();
                if (diffWhitespace)
                {
                    optionsBuilder.IgnoreAllSpace();
                }
                if (location == StatusChangeLocation.Index)
                {
                    optionsBuilder.Cached();
                }
                GitDiffResult result = change.Type == GitStatusChangeType.RenamedOrCopied && change.SourceId! != change.DestinationId!
                    ? await Repository.DiffAsync(change.SourceId!, change.DestinationId!, optionsBuilder.Build())
                    : await Repository.DiffAsync(change.Path, optionsBuilder.Build());
                SelectedDiffResult = result;
            }
        }

        private async Task StageAllFilesAsync()
        {
            await Repository.AddAllAsync();
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
            if (model is null) return;

            if (model.Location == StatusChangeLocation.Index)
            {
                int previousIndex = SelectedIndexIndex;
                await Repository.ResetAsync(model.Model);
                await GetRepositoryStatusAsync();
                SelectedIndexIndex = previousIndex >= indexFiles.Count ? indexFiles.Count - 1 : previousIndex;
            }
            else
            {
                int previousIndex = SelectedWorkspaceIndex;
                await Repository.AddAsync(model.Model, AddOptions.None);
                await GetRepositoryStatusAsync();
                SelectedWorkspaceIndex = previousIndex >= workspaceFiles.Count ? workspaceFiles.Count - 1 : previousIndex;
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
            SelectedWorkspaceIndex = previousIndex >= workspaceFiles.Count ? workspaceFiles.Count - 1 : previousIndex;
        }

        private async Task ResetWorkspaceFilesAsync()
        {
            undoPatch = null;
            int previousIndex = SelectedWorkspaceIndex;
            foreach (StatusChangeViewModel item in workspaceFiles)
            {
                if (item.IsSelected)
                {
                    await Repository.CheckoutAsync(item.Model);
                }
            }
            await GetRepositoryStatusAsync();
            SelectedWorkspaceIndex = previousIndex >= workspaceFiles.Count ? workspaceFiles.Count - 1 : previousIndex;
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
            SelectedIndexIndex = previousIndex >= indexFiles.Count ? indexFiles.Count - 1 : previousIndex;
        }

        private async Task ResetSelectionAsync(IHunkLineVisitorProvider? viewer)
        {
            if (viewer is null) return;
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
                _ = snack.ShowAsync(Snack.Builder()
                    .WithMessage($"Changes reset in {filename}")
                    .WithDuration(TimeSpan.FromSeconds(5))
                    .AddAction(undoText))
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
            if (viewer is null) return;

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

            var patch = GitPatch.Create(
                PatchMode.AddIndex,
                selectedChange.Model.Path,
                selectedChange.Status == GitModifiedStatusType.Added
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
                    index = FindSortedIndex(workspaceFiles, item => selectedChange.Path.CompareTo(item.Path));
                }
                if (index < workspaceFiles.Count && workspaceFiles[index].Path == selectedChange.Path)
                {
                    await ExecuteDiffAsync();
                }
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
            if (viewer is null) return;

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

        private async Task CommitChangesAsync()
        {
            GitCommitOptions options = amendLastCommit
                ? GitCommitOptions.AmendLatest(commitMessage)
                : GitCommitOptions.CreateCommit(commitMessage);
            await Repository.CommitAsync(options);
            snack.ShowSuccess("Commited changes successfully");
            await GetRepositoryStatusAsync();
            if (!amendLastCommit)
            {
                CommitMessage = string.Empty;
            }
            SelectedChange = null;
        }

        private void ParseStatus(GitStatusResult result)
        {
            foreach (GitStatusChange change in result.Changes)
            {
                if (change.IndexStatus.HasValue && change.IndexStatus != GitModifiedStatusType.Unmodified)
                {
                    AddChangeToIndex(change);
                }
                if (change.WorkspaceStatus != GitModifiedStatusType.Unmodified || change.Type == GitStatusChangeType.Untracked)
                {
                    AddChangeToWorkspace(change);
                }
            }
            lock (indexFilesLock)
            {
                for (int i = 0; i < indexFiles.Count; ++i)
                {
                    StatusChangeViewModel item = indexFiles[i];
                    if (result.Changes.Where(res => res.IndexStatus.HasValue && res.IndexStatus != GitModifiedStatusType.Unmodified).All(res => res.Path.ToString() != item.Model.Path.ToString()))
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
                    if (result.Changes.Count == 0 || result.Changes.All(res => res.Path.ToString() != item.Model.Path.ToString()
                        || (res.WorkspaceStatus.HasValue && res.WorkspaceStatus == GitModifiedStatusType.Unmodified)))
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
            int index = FindSortedIndex(workspaceFiles, item => change.Path.ToString().CompareTo(item.Path.ToString()));
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
            int index = FindSortedIndex(indexFiles, item => change.Path.ToString().CompareTo(item.Path.ToString()));
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
