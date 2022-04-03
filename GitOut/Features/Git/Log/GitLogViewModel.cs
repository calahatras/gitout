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
using GitOut.Features.Collections;
using GitOut.Features.Git.RepositoryList;
using GitOut.Features.Git.Stage;
using GitOut.Features.IO;
using GitOut.Features.Material.Snackbar;
using GitOut.Features.Navigation;
using GitOut.Features.Wpf;
using Microsoft.Extensions.Options;

namespace GitOut.Features.Git.Log
{
    public class GitLogViewModel : INotifyPropertyChanged, INavigationListener, INavigationFallback
    {
        private readonly object activeStashesLock = new();
        private readonly ObservableCollection<GitTreeEvent> activeStashes = new();

        private readonly object entriesLock = new();
        private readonly RangeObservableCollection<GitTreeEvent> entries = new();

        private readonly object remotesLock = new();
        private readonly ObservableCollection<GitRemoteViewModel> remotes = new();

        private readonly ObservableCollection<GitTreeEvent> selectedLogEntries = new();

        private readonly ISnackbarService snack;
        private readonly IRepositoryWatcher repositoryWatcher;

        private int changesCount;
        private bool includeStashes = true;
        private bool includeRemotes = true;
        private bool suppressSelectedLogEntriesCollectionChanged;
        private bool showSpacesAsDots;
        private bool isStashesVisible;
        private bool isSearchDisplayed;
        private bool isCheckoutBranchVisible;
        private LogViewMode viewMode = LogViewMode.None;

        private LogRevisionViewMode revisionViewMode = LogRevisionViewMode.CurrentRevision;
        private LogEntriesViewModel? selectedContext;

        private string? checkoutBranchName;
        private GitTreeEvent? entryInView;
        private bool hasChanges;

        private bool isWorking;

        public GitLogViewModel(
            INavigationService navigation,
            ITitleService title,
            IGitRepositoryWatcherProvider watchProvider,
            ISnackbarService snack,
            IOptionsMonitor<GitStageOptions> stagingOptions
        )
        {
            this.snack = snack;
            showSpacesAsDots = stagingOptions.CurrentValue.ShowSpacesAsDots;
            GitLogPageOptions options = navigation.GetOptions<GitLogPageOptions>(typeof(GitLogPage).FullName!)
                ?? throw new ArgumentNullException(nameof(options), "Options may not be null");
            Repository = options.Repository;
            title.Title = $"{Repository.Name} (Log)";

            repositoryWatcher = watchProvider.PrepareWatchRepositoryChanges(Repository, RepositoryWatcherOptions.Workspace);
            repositoryWatcher.Events += OnFileSystemChanges;

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
                if (suppressSelectedLogEntriesCollectionChanged)
                {
                    return;
                }

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
            CheckoutBranchCommand = new AsyncCallbackCommand(
                async () =>
                {
                    IsCheckoutBranchVisible = false;
                    try
                    {
                        var branchName = GitBranchName.CreateLocal(checkoutBranchName!); // name is validated by the canExecute callback
                        await Repository.CheckoutBranchAsync(branchName);
                        await CheckRepositoryStatusAsync();
                        snack.ShowSuccess($"Branch {branchName.Name} created");
                        checkoutBranchName = string.Empty;
                    }
                    catch (InvalidOperationException e)
                    {
                        snack.ShowError($"Could not create branch", e, TimeSpan.FromSeconds(10));
                    }
                },
                () => checkoutBranchName is not null && GitBranchName.IsValid(checkoutBranchName)
            );

            RevealInExplorerCommand = new CallbackCommand(() => Process.Start("explorer.exe", $"/s,{Repository.WorkingDirectory}").Dispose());
            CopyContentCommand = new CopyTextToClipBoardCommand<object>(
                d => Repository.WorkingDirectory.Directory,
                d => true,
                text => snack.ShowSuccess("Copied path to clipboard")
            );

            CopyCommitHashCommand = new CopyTextToClipBoardCommand<GitHistoryEvent?>(
                ghe => ghe!.Id.Hash,
                ghe => ghe is not null,
                TextDataFormat.UnicodeText,
                data => snack.ShowSuccess("Copied hash to clipboard"),
                data => snack.ShowError(data.Message, data)
            );

            CopySubjectCommand = new CopyTextToClipBoardCommand<LogEntriesViewModel?>(
                gte => gte!.Subject,
                gte => gte is not null,
                TextDataFormat.UnicodeText,
                data => snack.ShowSuccess("Copied subject to clipboard"),
                data => snack.ShowError(data.Message, data)
            );

            CloseDetailsCommand = new CallbackCommand(() =>
            {
                foreach (GitTreeEvent entry in entries)
                {
                    entry.IsSelected = false;
                }
            });
            SwapCommitsCommand = new CallbackCommand(
                () =>
                {
                    suppressSelectedLogEntriesCollectionChanged = true;
                    (selectedLogEntries[0], selectedLogEntries[1]) = (selectedLogEntries[1], selectedLogEntries[0]);

                    SelectedContext = selectedContext!.CopyContext(selectedLogEntries, Repository, RevisionViewMode);

                    suppressSelectedLogEntriesCollectionChanged = false;
                },
                () => selectedLogEntries.Count == 2
            );
            SelectCommitCommand = new NotNullCallbackCommand<GitHistoryEvent>(commit =>
            {
                EntryInView = null;
                foreach (GitTreeEvent entry in entries)
                {
                    entry.IsSelected = false;
                }
                GitTreeEvent gitTreeEvent = entries.First(e => e.Event.Id == commit.Id);
                EntryInView = gitTreeEvent;
                gitTreeEvent.IsSelected = true;
            });

            AppendSelectCommitCommand = new NotNullCallbackCommand<GitHistoryEvent>(commit => entries.First(e => e.Event.Id == commit.Id).IsSelected = true);
            CloseAutocompleteCommand = new CallbackCommand(() => IsSearchDisplayed = false);
            ShowSearchFilesCommand = new CallbackCommand(() => IsSearchDisplayed = true);
        }

        public bool IncludeStashes
        {
            get => includeStashes;
            set
            {
                if (SetProperty(ref includeStashes, value))
                {
                    _ = CheckRepositoryStatusAsync();
                }
            }
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

        public bool ShowSpacesAsDots
        {
            get => showSpacesAsDots;
            set => SetProperty(ref showSpacesAsDots, value);
        }

        public bool IsSearchDisplayed
        {
            get => isSearchDisplayed;
            set => SetProperty(ref isSearchDisplayed, value);
        }

        public bool IsCheckoutBranchVisible
        {
            get => isCheckoutBranchVisible;
            set => SetProperty(ref isCheckoutBranchVisible, value);
        }

        public bool IsWorking
        {
            get => isWorking;
            set => SetProperty(ref isWorking, value);
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

        public GitTreeEvent? EntryInView
        {
            get => entryInView;
            private set => SetProperty(ref entryInView, value);
        }

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
                    if (selectedContext is not null)
                    {
                        selectedContext.ViewMode = revisionViewMode;
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

        public string? CheckoutBranchName
        {
            get => checkoutBranchName;
            set => SetProperty(ref checkoutBranchName, value);
        }

        public ICommand NavigateToStageAreaCommand { get; }
        public ICommand RefreshStatusCommand { get; }
        public ICommand FetchRemotesCommand { get; }
        public ICommand CheckoutBranchCommand { get; }
        public ICommand RevealInExplorerCommand { get; }
        public ICommand CopyContentCommand { get; }
        public ICommand CopyCommitHashCommand { get; }
        public ICommand CopySubjectCommand { get; }
        public ICommand SelectCommitCommand { get; }
        public ICommand AppendSelectCommitCommand { get; }
        public ICommand CloseAutocompleteCommand { get; }
        public ICommand ShowSearchFilesCommand { get; }
        public ICommand CloseDetailsCommand { get; }
        public ICommand SwapCommitsCommand { get; }

        public string FallbackPageName => typeof(RepositoryListPage).FullName!;
        public object? FallbackOptions => null;

        public event PropertyChangedEventHandler? PropertyChanged;

        public void Navigated(NavigationType type)
        {
            switch (type)
            {
                case NavigationType.Initial:
                    _ = CheckRepositoryStatusAsync();
                    _ = PopulateRemotesAsync();
                    break;
                case NavigationType.NavigatedBack:
                    _ = CheckRepositoryStatusAsync();
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
                        if (hasChanges)
                        {
                            _ = CheckRepositoryStatusAsync();
                        }
                        hasChanges = false;
                    }
                    break;
            }
        }

        private void OnFileSystemChanges(object sender, RepositoryWatcherEventArgs args) => hasChanges = true;

        private async Task FetchRemotesAsync()
        {
            IsWorking = true;
            foreach (GitRemoteViewModel remote in remotes)
            {
                if (remote.IsSelected)
                {
                    await Repository.FetchAsync(remote.Model);
                }
            }
            snack.ShowSuccess("Fetched all selected remotes");
            await CheckRepositoryStatusAsync();
            IsWorking = false;
        }

        private async Task PopulateRemotesAsync()
        {
            IsWorking = true;
            await foreach (GitRemote remote in Repository.GetRemotesAsync())
            {
                lock (remotesLock)
                {
                    remotes.Add(GitRemoteViewModel.From(remote));
                }
            }
            IsWorking = false;
        }

        private async Task CheckRepositoryStatusAsync()
        {
            IsWorking = true;
            IEnumerable<GitTreeEvent>? history = await Task.Run(async () =>
            {
                IEnumerable<GitHistoryEvent> tree = await Repository.LogAsync(new LogOptions
                {
                    IncludeRemotes = includeRemotes,
                    IncludeStashes = includeStashes
                }).ConfigureAwait(false);

                IEnumerable<GitTreeEvent>? result = BuildTree(tree);
                var selected = entries
                    .Where(e => e.IsSelected)
                    .Select(e => e.Event.Id)
                    .ToList();

                foreach (GitTreeEvent item in result)
                {
                    if (selected.Contains(item.Event.Id))
                    {
                        item.IsSelected = true;
                    }
                }
                return result;
            }).ConfigureAwait(false);

            lock (entriesLock)
            {
                entries.Clear();
                entries.AddRange(history);
            }
            GitStatusResult status = await Repository.StatusAsync().ConfigureAwait(false);
            ChangesCount = status.Changes.Count;
            IsWorking = false;
        }

        private async Task RefreshStashListAsync()
        {
            lock (activeStashesLock)
            {
                activeStashes.Clear();
            }
            await foreach (GitHistoryEvent stashEntry in Repository.StashListAsync())
            {
                lock (activeStashesLock)
                {
                    activeStashes.Add(new GitTreeEvent(stashEntry));
                }
            }
        }

        private static IEnumerable<GitTreeEvent> BuildTree(IEnumerable<GitHistoryEvent> log)
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
