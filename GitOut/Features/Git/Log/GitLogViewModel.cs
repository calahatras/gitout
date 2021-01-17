using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using GitOut.Features.Git.Stage;
using GitOut.Features.Material.Snackbar;
using GitOut.Features.Navigation;
using GitOut.Features.Wpf;

namespace GitOut.Features.Git.Log
{
    public class GitLogViewModel : INotifyPropertyChanged, INavigationListener
    {
        private readonly object activeStashesLock = new object();
        private readonly ObservableCollection<GitStash> activeStashes = new ObservableCollection<GitStash>();

        private readonly object entriesLock = new object();
        private readonly ObservableCollection<GitTreeEvent> entries = new ObservableCollection<GitTreeEvent>();

        private readonly object remotesLock = new object();
        private readonly ObservableCollection<GitRemoteViewModel> remotes = new ObservableCollection<GitRemoteViewModel>();

        private readonly ObservableCollection<GitTreeEvent> selectedLogEntries = new ObservableCollection<GitTreeEvent>();

        private readonly ISnackbarService snack;

        private int changesCount;
        private bool includeRemotes = true;
        private bool isStashesVisible = false;
        private LogViewMode viewMode = LogViewMode.None;

        private LogRevisionViewMode revisionViewMode = LogRevisionViewMode.CurrentRevision;
        private LogEntriesViewModel? selectedContext;

        public GitLogViewModel(
            INavigationService navigation,
            ITitleService title,
            ISnackbarService snack
        )
        {
            this.snack = snack;
            GitLogPageOptions options = navigation.GetOptions<GitLogPageOptions>(typeof(GitLogPage).FullName!)
                ?? throw new ArgumentNullException(nameof(options), "Options may not be null");
            Repository = options.Repository;
            title.Title = "Log";
            Repository = options.Repository;

            BindingOperations.EnableCollectionSynchronization(activeStashes, activeStashesLock);
            ActiveStashes = CollectionViewSource.GetDefaultView(activeStashes);

            BindingOperations.EnableCollectionSynchronization(entries, entriesLock);
            Entries = CollectionViewSource.GetDefaultView(entries);

            BindingOperations.EnableCollectionSynchronization(remotes, remotesLock);
            Remotes = CollectionViewSource.GetDefaultView(remotes);

            NavigateToStageAreaCommand = new NavigateLocalCommand<object>(navigation, typeof(GitStagePage).FullName!, e => GitStagePageOptions.Stage(Repository));
            RefreshStatusCommand = new AsyncCallbackCommand(CheckRepositoryStatusAsync);

            selectedLogEntries.CollectionChanged += (sender, args) =>
            {
                SelectedContext = LogEntriesViewModel.CreateContext(selectedLogEntries, Repository, RevisionViewMode);
                ViewMode = SelectedContext is null
                    ? LogViewMode.None
                    : LogViewMode.Files;
                if (selectedLogEntries.Count >= 2)
                {
                    RevisionViewMode = LogRevisionViewMode.Diff;
                }
            };

            FetchRemotesCommand = new AsyncCallbackCommand(FetchRemotesAsync);

            CopyContentCommand = new CopyTextToClipBoardCommand<object>(
                d => Repository.WorkingDirectory.Directory,
                d => true,
                text => snack.ShowSuccess("Copied path to clipboard")
            );

            CopyCommitHashCommand = new CopyTextToClipBoardCommand<LogEntriesViewModel?>(
                gte => gte!.Root.Event.Id.Hash,
                gte => !(gte is null),
                TextDataFormat.Text,
                data => snack.ShowSuccess("Copied hash to clipboard")
            );

            CopySubjectCommand = new CopyTextToClipBoardCommand<LogEntriesViewModel?>(
                gte => gte!.Subject,
                gte => !(gte is null),
                TextDataFormat.Text,
                data => snack.ShowSuccess("Copied subject to clipboard")
            );
        }

        public bool IncludeRemotes
        {
            get => includeRemotes;
            set
            {
                if (SetProperty(ref includeRemotes, value))
                {
                    _ = CheckRepositoryStatusAsync();
                }
            }
        }

        public int ChangesCount
        {
            get => changesCount;
            private set => SetProperty(ref changesCount, value);
        }

        public ICollectionView ActiveStashes { get; }
        public ICollectionView Entries { get; }
        public ICollectionView Remotes { get; }

        public IGitRepository Repository { get; }
        public IList<GitTreeEvent> SelectedLogEntries => selectedLogEntries;

        public LogEntriesViewModel? SelectedContext
        {
            get => selectedContext;
            set => SetProperty(ref selectedContext, value);
        }

        public LogViewMode ViewMode
        {
            get => viewMode;
            set
            {
                if (SetProperty(ref viewMode, value))
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FileViewVisible)));
                }
            }
        }
        public bool FileViewVisible => (ViewMode & LogViewMode.Files) == LogViewMode.Files;
        public bool IsStashesVisible
        {
            get => isStashesVisible;
            set
            {
                if (SetProperty(ref isStashesVisible, value) && value)
                {
                    _ = RefreshStashListAsync();
                }
            }
        }

        public LogRevisionViewMode RevisionViewMode
        {
            get => revisionViewMode;
            set
            {
                if (SetProperty(ref revisionViewMode, value))
                {
                    if (!(selectedContext is null))
                    {
                        selectedContext.SwitchViewAsync(revisionViewMode);
                    }
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowRevisionAtCurrent)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowRevisionDiff)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowRevisionDiffInline)));
                }
            }
        }
        public bool ShowRevisionAtCurrent
        {
            get => RevisionViewMode == LogRevisionViewMode.CurrentRevision;
            set { if (value) { RevisionViewMode = LogRevisionViewMode.CurrentRevision; } }
        }
        public bool ShowRevisionDiff
        {
            get => RevisionViewMode == LogRevisionViewMode.Diff;
            set { if (value) { RevisionViewMode = LogRevisionViewMode.Diff; } }
        }
        public bool ShowRevisionDiffInline
        {
            get => RevisionViewMode == LogRevisionViewMode.DiffInline;
            set { if (value) { RevisionViewMode = LogRevisionViewMode.DiffInline; } }
        }

        public ICommand NavigateToStageAreaCommand { get; }
        public ICommand RefreshStatusCommand { get; }
        public ICommand FetchRemotesCommand { get; }
        public ICommand CopyContentCommand { get; }
        public ICommand CopyCommitHashCommand { get; }
        public ICommand CopySubjectCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void Navigated(NavigationType type)
        {
            if (type == NavigationType.Initial || type == NavigationType.NavigatedBack)
            {
                _ = CheckRepositoryStatusAsync();
            }
            if (type == NavigationType.Initial)
            {
                _ = PopulateRemotesAsync();
            }
        }

        private async Task FetchRemotesAsync()
        {
            foreach (GitRemoteViewModel remote in remotes)
            {
                if (remote.IsSelected)
                {
                    await Repository.ExecuteFetchAsync(remote.Model);
                }
            }
            snack.ShowSuccess("Fetched all selected remotes", 8000);
            await CheckRepositoryStatusAsync();
        }

        private async Task PopulateRemotesAsync()
        {
            await foreach (GitRemote remote in Repository.GetRemotesAsync())
            {
                lock (remotesLock)
                {
                    remotes.Add(GitRemoteViewModel.From(remote));
                }
            }
        }

        private async Task CheckRepositoryStatusAsync()
        {
            IEnumerable<GitHistoryEvent> tree = await Repository.ExecuteLogAsync(IncludeRemotes ? LogOptions.WithRemoteBranches() : LogOptions.OnlyLocalBranches()).ConfigureAwait(false);
            IEnumerable<GitTreeEvent> history = BuildTree(tree);
            lock (entriesLock)
            {
                entries.Clear();
                foreach (GitTreeEvent item in history)
                {
                    entries.Add(item);
                }
            }
            GitStatusResult status = await Repository.ExecuteStatusAsync().ConfigureAwait(false);
            ChangesCount = status.Changes.Count;
        }

        private async Task RefreshStashListAsync()
        {
            lock (activeStashesLock)
            {
                activeStashes.Clear();
            }
            await foreach (GitStash stashEntry in Repository.ExecuteStashListAsync())
            {
                for (int i = 0; i < entries.Count; ++i)
                {
                    if (entries[i].Event.Id == stashEntry.ParentId)
                    {
                        // todo: convert stash to githistoryevent, or wrap history event in viewmodel? :thinking:
                        //entries.Insert(i, new GitTreeEvent());
                    }
                }

                lock (activeStashesLock)
                {
                    activeStashes.Add(stashEntry);
                }
            }
        }

        private IEnumerable<GitTreeEvent> BuildTree(IEnumerable<GitHistoryEvent> log)
        {
            var stopwatch = Stopwatch.StartNew();
            var events = new List<GitTreeEvent>();
            IEnumerable<TreeBuildingLeaf> leafs = Enumerable.Empty<TreeBuildingLeaf>();
            GitTreeEvent.ResetColors();
            foreach (GitHistoryEvent item in log)
            {
                var node = new GitTreeEvent(item);
                leafs = node.Process(leafs);
                events.Add(node);
            }
            Trace.WriteLine($"Built git tree: {stopwatch.Elapsed.TotalMilliseconds}ms");

            return events;
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
}
