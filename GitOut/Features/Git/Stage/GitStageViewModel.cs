using System;
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
using System.Windows.Media;
using GitOut.Features.Commands;
using GitOut.Features.Material.Snackbar;
using GitOut.Features.Navigation;
using GitOut.Features.Wpf;
using Microsoft.Extensions.Options;

namespace GitOut.Features.Git.Stage
{
    public class GitStageViewModel : INotifyPropertyChanged, INavigationListener
    {
        private readonly ISnackbarService snack;
        private readonly IOptionsMonitor<GitStageOptions> stagingOptions;

        private readonly ObservableCollection<StatusChangeViewModel> workspaceFiles = new ObservableCollection<StatusChangeViewModel>();
        private readonly object workspaceFilesLock = new object();
        private readonly ObservableCollection<StatusChangeViewModel> indexFiles = new ObservableCollection<StatusChangeViewModel>();
        private readonly object indexFilesLock = new object();

        private StatusChangeViewModel? selectedChange;
        private DiffViewModel? selectedDiff;

        private bool diffWhitespace;
        private bool amendLastCommit;
        private string commitMessage = string.Empty;
        private int selectedWorkspaceIndex;
        private int selectedIndexIndex;

        public GitStageViewModel(
            INavigationService navigation,
            ITitleService title,
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

            BindingOperations.EnableCollectionSynchronization(workspaceFiles, workspaceFilesLock);
            WorkspaceFiles = CollectionViewSource.GetDefaultView(workspaceFiles);
            BindingOperations.EnableCollectionSynchronization(indexFiles, indexFilesLock);
            IndexFiles = CollectionViewSource.GetDefaultView(indexFiles);

            NavigateBackCommand = new CallbackCommand(navigation.Back, navigation.CanGoBack);
            RefreshStatusCommand = new AsyncCallbackCommand(() => GetRepositoryStatusAsync());
            CommitCommand = new AsyncCallbackCommand(CommitChangesAsync, () => !string.IsNullOrEmpty(CommitMessage));
            StageFileCommand = new AsyncCallbackCommand<StatusChangeViewModel>(StageFileAsync);
            ResetSelectedTextCommand = new AsyncCallbackCommand<FlowDocumentScrollViewer>(ResetSelectionAsync);
            StageSelectedTextCommand = new AsyncCallbackCommand<FlowDocumentScrollViewer>(StageSelectionAsync);
            AddAllCommand = new AsyncCallbackCommand(StageAllFilesAsync);
            ResetHeadCommand = new AsyncCallbackCommand(ResetAllFilesAsync);

            Repository = options.Repository;
            if (Repository.CachedStatus != null)
            {
                ParseStatus(Repository.CachedStatus);
            }
        }

        public IGitRepository Repository { get; }

        public bool DiffWhitespace
        {
            get => diffWhitespace;
            set
            {
                if (SetProperty(ref diffWhitespace, value))
                {
                    if (selectedChange != null)
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
                    GitHistoryEvent? head = Repository.Head;
                    if (head != null)
                    {
                        CommitMessage = head.Subject;
                    }
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
                    SelectedDiff = null;
                    if (selectedChange != null)
                    {
                        _ = ExecuteDiffAsync();
                    }
                }
            }
        }

        public DiffViewModel? SelectedDiff
        {
            get => selectedDiff;
            set => SetProperty(ref selectedDiff, value);
        }

        public ICollectionView IndexFiles { get; }
        public ICollectionView WorkspaceFiles { get; }

        public ICommand NavigateBackCommand { get; }
        public ICommand RefreshStatusCommand { get; }
        public ICommand AddAllCommand { get; }
        public ICommand StageFileCommand { get; }
        public ICommand ResetSelectedTextCommand { get; }
        public ICommand StageSelectedTextCommand { get; }
        public ICommand ResetHeadCommand { get; }
        public ICommand CommitCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public async void Navigated(NavigationType type) => await GetRepositoryStatusAsync();

        private async Task GetRepositoryStatusAsync() => ParseStatus(await Repository.ExecuteStatusAsync());

        private async Task ExecuteDiffAsync(SynchronizationContext? syncObject = null)
        {
            if (selectedChange is null)
            {
                throw new ArgumentNullException(nameof(selectedChange), "Cannot perform status on null change");
            }
            syncObject ??= SynchronizationContext.Current!;

            GitStatusChange change = selectedChange.Model;
            StatusChangeLocation location = selectedChange.Location;
            double pixelsPerDip = VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip;
            if (change.Type == GitStatusChangeType.Untracked)
            {
                string[] result = await File.ReadAllLinesAsync(Path.Combine(Repository.WorkingDirectory.Directory, change.Path));
                syncObject.Post(s => SelectedDiff = DiffViewModel.ParseFileContent(change, result, pixelsPerDip), null);
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
                GitDiffResult result = await Repository.ExecuteDiffAsync(change, optionsBuilder.Build());
                syncObject.Post(s => SelectedDiff = DiffViewModel.ParseDiff(
                    result,
                    pixelsPerDip,
                    (Brush)Application.Current.Resources["MaterialLightDividers"],
                    (Brush)Application.Current.Resources["MaterialGray400"]
                ),
                null);
            }
        }

        private async Task StageAllFilesAsync()
        {
            SynchronizationContext? syncObject = SynchronizationContext.Current!;
            await Repository.ExecuteAddAllAsync();
            await GetRepositoryStatusAsync();
        }

        private async Task ResetAllFilesAsync()
        {
            SynchronizationContext? syncObject = SynchronizationContext.Current!;
            await Repository.ExecuteResetAllAsync();
            await GetRepositoryStatusAsync();
        }

        private async Task StageFileAsync(StatusChangeViewModel model)
        {
            if (model.Location == StatusChangeLocation.Index)
            {
                int previousIndex = SelectedIndexIndex;
                await Repository.ExecuteResetAsync(model.Model);
                await GetRepositoryStatusAsync();
                SelectedIndexIndex = previousIndex >= indexFiles.Count ? indexFiles.Count - 1 : previousIndex;
            }
            else
            {
                int previousIndex = SelectedWorkspaceIndex;
                await Repository.ExecuteAddAsync(model.Model);
                await GetRepositoryStatusAsync();
                SelectedWorkspaceIndex = previousIndex >= workspaceFiles.Count ? workspaceFiles.Count - 1 : previousIndex;
            }
        }

        private async Task ResetSelectionAsync(FlowDocumentScrollViewer viewer)
        {
            if (selectedDiff is null)
            {
                throw new ArgumentNullException(nameof(selectedDiff), "No diff is selected");
            }
            if (selectedChange is null)
            {
                throw new ArgumentNullException(nameof(selectedChange), "No change is selected");
            }
            SynchronizationContext? syncObject = SynchronizationContext.Current!;
            string filename = Path.GetFileName(selectedChange.Path);
            GitPatch? undoPatch = selectedChange.Location == StatusChangeLocation.Workspace
                ? selectedDiff.CreateUndoPatch(viewer.Selection)
                : null;
            GitPatch patch = selectedDiff.CreateResetPatch(viewer.Selection);
            await Repository.ExecuteApplyAsync(patch);
            await GetRepositoryStatusAsync();

            if (undoPatch != null)
            {
                snack.ShowSuccess("Changes reset in " + filename, 8000, "UNDO", async () =>
                {
                    await Repository.ExecuteApplyAsync(undoPatch);
                    await GetRepositoryStatusAsync();
                });
            }
        }

        private async Task StageSelectionAsync(FlowDocumentScrollViewer viewer)
        {
            if (selectedDiff is null)
            {
                throw new ArgumentNullException(nameof(selectedDiff), "No diff is selected");
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
            IPatchLineTransformBuilder builder = PatchLineTransform.Builder();
            if (stagingOptions.CurrentValue.TrimLineEndings)
            {
                builder.TrimLines();
            }
            if (!string.IsNullOrEmpty(stagingOptions.CurrentValue.TabTransformText))
            {
                builder.ConvertTabsToSpaces(stagingOptions.CurrentValue.TabTransformText);
            }
            GitPatch patch = selectedDiff.CreateAddPatch(viewer.Selection, builder.Build());
            SynchronizationContext? syncObject = SynchronizationContext.Current!;
            int previousIndex = selectedWorkspaceIndex;
            await Repository.ExecuteApplyAsync(patch);
            await GetRepositoryStatusAsync();
            if (selectedChange is null)
            {
                SelectedWorkspaceIndex = previousIndex;
            }
            else
            {
                int index = FindSortedIndex(workspaceFiles, item => selectedChange.Path.CompareTo(item.Path));
                if (workspaceFiles[index].Path == selectedChange.Path)
                {
                    await ExecuteDiffAsync(syncObject);
                }
            }
        }

        private async Task CommitChangesAsync()
        {
            GitCommitOptions options = amendLastCommit
                ? GitCommitOptions.AmendLatest(commitMessage)
                : GitCommitOptions.CreateCommit(commitMessage);
            await Repository.ExecuteCommitAsync(options);
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
                    if (indexFiles.Count == 0)
                    {
                        lock (indexFilesLock)
                        {
                            indexFiles.Add(StatusChangeViewModel.AsStaged(change));
                        }
                        continue;
                    }
                    int index = FindSortedIndex(indexFiles, item => change.Path.CompareTo(item.Path));
                    if (index >= indexFiles.Count)
                    {
                        lock (indexFilesLock)
                        {
                            indexFiles.Insert(index, StatusChangeViewModel.AsStaged(change));
                        }
                    }
                    else if (indexFiles[index].Path == change.Path)
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
                if (change.WorkspaceStatus != GitModifiedStatusType.Unmodified || change.Type == GitStatusChangeType.Untracked)
                {
                    if (workspaceFiles.Count == 0)
                    {
                        lock (workspaceFilesLock)
                        {
                            workspaceFiles.Add(StatusChangeViewModel.AsWorkspace(change));
                        }
                        continue;
                    }
                    int index = FindSortedIndex(workspaceFiles, item => change.Path.CompareTo(item.Path));
                    if (index >= workspaceFiles.Count)
                    {
                        lock (workspaceFilesLock)
                        {
                            workspaceFiles.Insert(index, StatusChangeViewModel.AsWorkspace(change));
                        }
                    }
                    else if (workspaceFiles[index].Path == change.Path)
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
            }
            lock (indexFilesLock)
            {
                for (int i = 0; i < indexFiles.Count; ++i)
                {
                    StatusChangeViewModel item = indexFiles[i];
                    if (result.Changes.Where(res => res.IndexStatus.HasValue && res.IndexStatus != GitModifiedStatusType.Unmodified).All(res => res.Path != item.Model.Path))
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
                    if (result.Changes.Where(res => res.Type == GitStatusChangeType.Untracked
                        || (res.Type == GitStatusChangeType.Ordinary
                            && res.WorkspaceStatus.HasValue
                            && res.WorkspaceStatus.Value != GitModifiedStatusType.Unmodified)).All(res => res.Path != item.Model.Path))
                    {
                        workspaceFiles.RemoveAt(i--);
                    }
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
}
