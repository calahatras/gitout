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
using GitOut.Features.Git.Files;
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

        private readonly object rootFilesLock = new object();
        private readonly ObservableCollection<IGitFileEntryViewModel> rootFiles = new ObservableCollection<IGitFileEntryViewModel>();
        private readonly ObservableCollection<GitTreeEvent> selectedLogEntries = new ObservableCollection<GitTreeEvent>();

        private int changesCount;
        private bool includeRemotes = true;
        private bool isStashesVisible = false;
        private LogViewMode viewMode = LogViewMode.None;

        public GitLogViewModel(
            INavigationService navigation,
            ITitleService title,
            ISnackbarService snack
        )
        {
            GitLogPageOptions options = navigation.GetOptions<GitLogPageOptions>(typeof(GitLogPage).FullName!)
                ?? throw new ArgumentNullException(nameof(options), "Options may not be null");
            Repository = options.Repository;
            title.Title = "Log";

            BindingOperations.EnableCollectionSynchronization(activeStashes, activeStashesLock);
            ActiveStashes = CollectionViewSource.GetDefaultView(activeStashes);

            BindingOperations.EnableCollectionSynchronization(entries, entriesLock);
            Entries = CollectionViewSource.GetDefaultView(entries);

            BindingOperations.EnableCollectionSynchronization(rootFiles, rootFilesLock);
            RootFiles = CollectionViewSource.GetDefaultView(rootFiles);

            NavigateToStageAreaCommand = new NavigateLocalCommand<object>(navigation, typeof(GitStagePage).FullName!, e => GitStagePageOptions.Stage(Repository));
            RefreshStatusCommand = new AsyncCallbackCommand(CheckRepositoryStatusAsync);

            CopyCommitHashCommand = new CopyTextToClipBoardCommand<GitTreeEvent?>(
                gte => gte!.Event.Id.Hash,
                gte => !(gte is null),
                TextDataFormat.Text,
                data => snack.ShowSuccess("Copied hash to clipboard")
            );

            selectedLogEntries.CollectionChanged += (sender, args) =>
            {
                if (selectedLogEntries.Count > 0)
                    _ = ListLogFilesAsync(selectedLogEntries.First());
            };
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
        public ICollectionView RootFiles { get; }

        public IGitRepository Repository { get; }
        public IList<GitTreeEvent> SelectedLogEntries => selectedLogEntries;

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

        public ICommand NavigateToStageAreaCommand { get; }
        public ICommand RefreshStatusCommand { get; }
        public ICommand CopyCommitHashCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void Navigated(NavigationType type)
        {
            if (type == NavigationType.Initial || type == NavigationType.NavigatedBack)
            {
                _ = CheckRepositoryStatusAsync();
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

        private async Task ListLogFilesAsync(GitTreeEvent? entry)
        {
            lock (rootFilesLock)
            {
                rootFiles.Clear();
            }
            if (entry is null)
            {
                ViewMode = LogViewMode.None;
            }
            else
            {
                ViewMode = LogViewMode.Files;
                GitHistoryEvent changeset = entry.Event;
                IAsyncEnumerable<IGitFileEntryViewModel> entries = GitFileEntryViewModelFactory.ListIdAsync(changeset.Id, Repository);
                await foreach (IGitFileEntryViewModel viewmodel in entries)
                {
                    lock (rootFilesLock)
                    {
                        rootFiles.Add(viewmodel);
                    }
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
