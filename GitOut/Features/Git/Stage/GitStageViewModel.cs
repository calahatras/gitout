using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using GitOut.Features.Commands;
using GitOut.Features.Material.Snackbar;
using GitOut.Features.Navigation;
using GitOut.Features.Wpf;

namespace GitOut.Features.Git.Stage
{
    public class GitStageViewModel : INotifyPropertyChanged, INavigationListener
    {
        private readonly ISnackbarService snack;

        private readonly ObservableCollection<StatusChangeViewModel> workspaceFiles = new ObservableCollection<StatusChangeViewModel>();
        private readonly object workspaceFilesLock = new object();
        private readonly ObservableCollection<StatusChangeViewModel> indexFiles = new ObservableCollection<StatusChangeViewModel>();
        private readonly object indexFilesLock = new object();

        private StatusChangeViewModel? selectedChange;
        private DiffViewModel? selectedDiff;
        private bool diffWhitespace;
        private string commitMessage = string.Empty;

        public GitStageViewModel(
            INavigationService navigation,
            ITitleService title,
            ISnackbarService snack
        )
        {
            GitStagePageOptions options = navigation.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName!)
                ?? throw new ArgumentNullException(nameof(options), "Options may not be null");
            title.Title = "Stage";

            Repository = options.Repository;

            BindingOperations.EnableCollectionSynchronization(workspaceFiles, workspaceFilesLock);
            WorkspaceFiles = CollectionViewSource.GetDefaultView(workspaceFiles);
            BindingOperations.EnableCollectionSynchronization(indexFiles, indexFilesLock);
            IndexFiles = CollectionViewSource.GetDefaultView(indexFiles);

            NavigateBackCommand = new CallbackCommand(navigation.Back, navigation.CanGoBack);
            RefreshStatusCommand = new AsyncCallbackCommand(() => GetRepositoryStatusAsync());
            CommitCommand = new AsyncCallbackCommand(() => CommitChanges(CommitMessage), () => !string.IsNullOrEmpty(CommitMessage));
            StageFileCommand = new AsyncCallbackCommand<StatusChangeViewModel>(StageFileAsync);
            StageSelectedTextCommand = new AsyncCallbackCommand<FlowDocumentScrollViewer>(StageSelectionAsync);
            AddAllCommand = new AsyncCallbackCommand(() => Repository.ExecuteAddAllAsync().ContinueWith(_ => GetRepositoryStatusAsync()));
            ResetHeadCommand = new AsyncCallbackCommand(() => Repository.ExecuteResetAllAsync().ContinueWith(_ => GetRepositoryStatusAsync()));

            Repository = options.Repository;
            if (Repository.CachedStatus != null)
            {
                ParseStatus(Repository.CachedStatus);
            }

            this.snack = snack;
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
                        ExecuteDiffAsync();
                    }
                }
            }
        }

        public string CommitMessage
        {
            get => commitMessage;
            set => SetProperty(ref commitMessage, value);
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
                        ExecuteDiffAsync();
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
        public ICommand StageSelectedTextCommand { get; }
        public ICommand ResetHeadCommand { get; }
        public ICommand CommitCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public async void Navigated(NavigationType type) => await GetRepositoryStatusAsync();

        private async Task<GitStatusResult> GetRepositoryStatusAsync()
        {
            GitStatusResult result = await Repository.ExecuteStatusAsync();
            ParseStatus(result);
            return result;
        }

        private async Task ExecuteDiffAsync(SynchronizationContext? syncObject = null)
        {
            if (selectedChange == null)
            {
                throw new ArgumentNullException(nameof(selectedChange), "Cannot perform status on null change");
            }
            syncObject ??= SynchronizationContext.Current!;

            if (selectedChange.Location == StatusChangeLocation.Workspace && selectedChange.Model.Type == GitStatusChangeType.Untracked)
            {
                string[] result = await File.ReadAllLinesAsync(Path.Combine(Repository.WorkingDirectory.Directory, selectedChange.Path));
                syncObject.Post(s => SelectedDiff = DiffViewModel.ParseFileContent(selectedChange.Model, result), null);
            }
            else
            {
                IDiffOptionsBuilder optionsBuilder = DiffOptions.Builder();
                if (diffWhitespace)
                {
                    optionsBuilder.IgnoreAllSpace();
                }
                if (selectedChange.Location == StatusChangeLocation.Index)
                {
                    optionsBuilder.Cached();
                }
                GitDiffResult result = await Repository.ExecuteDiffAsync(selectedChange.Model, optionsBuilder.Build());
                syncObject.Post(s => SelectedDiff = DiffViewModel.ParseDiff(result), null);
            }
        }

        private async Task StageFileAsync(StatusChangeViewModel model)
        {
            if (model.Location == StatusChangeLocation.Index)
            {
                await Repository.ExecuteResetAsync(model.Model);
            }
            else
            {
                await Repository.ExecuteAddAsync(model.Model);
            }
            await GetRepositoryStatusAsync();
        }

        private async Task StageSelectionAsync(FlowDocumentScrollViewer viewer)
        {
            if (selectedDiff is null)
            {
                throw new ArgumentNullException(nameof(selectedDiff), "No diff is selected");
            }
            if (selectedChange == null)
            {
                throw new ArgumentNullException(nameof(selectedChange), "No change is selected");
            }
            if (selectedChange.Location == StatusChangeLocation.Index)
            {
                snack.Show("Sorry, can not reset from index yet");
                return;
            }
            GitPatch patch = selectedDiff.CreatePatch(viewer.Selection);
            SynchronizationContext? syncObject = SynchronizationContext.Current!;
            await Repository.ExecuteApplyAsync(patch);
            await GetRepositoryStatusAsync();
            if (selectedChange != null)
            {
                int index = FindSortedIndex(workspaceFiles, item => selectedChange.Path.CompareTo(item.Path));
                if (workspaceFiles[index].Path == selectedChange.Path)
                {
                    await ExecuteDiffAsync(syncObject);
                }
            }
        }

        private async Task CommitChanges(string message)
        {
            await Repository.ExecuteCommitAsync(message);
            await GetRepositoryStatusAsync();
            snack.ShowSuccess("Commited changes successfully");
            CommitMessage = string.Empty;
            SelectedChange = null;
        }

        private void ParseStatus(GitStatusResult result)
        {
            foreach (GitStatusChange change in result.Changes)
            {
                if (change.IndexStatus != GitModifiedStatusType.Unmodified)
                {
                    if (indexFiles.Count == 0)
                    {
                        lock (indexFilesLock)
                        {
                            indexFiles.Insert(0, StatusChangeViewModel.AsStaged(change));
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
                            workspaceFiles.Insert(0, StatusChangeViewModel.AsWorkspace(change));
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
                    if (result.Changes.Where(res => res.Type == GitStatusChangeType.Untracked || (res.Type == GitStatusChangeType.Ordinary && res.WorkspaceStatus.HasValue && res.WorkspaceStatus.Value != GitModifiedStatusType.Unmodified)).All(res => res.Path != item.Model.Path))
                    {
                        workspaceFiles.RemoveAt(i--);
                    }
                }
            }
        }

        private bool SetProperty<T>(ref T prop, T value, [CallerMemberName]string? propertyName = null)
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
