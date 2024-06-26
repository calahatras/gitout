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
using GitOut.Features.Options;
using GitOut.Features.Text;
using GitOut.Features.Wpf;
using Microsoft.Extensions.Options;

namespace GitOut.Features.Git.Log
{
    public class GitLogViewModel : INotifyPropertyChanged, INavigationListener, INavigationFallback
    {
        private static readonly Size PromptSize = new(400, 150);
        private static readonly Point PromptOffset = new(80, 60);

        private readonly object activeStashesLock = new();
        private readonly ObservableCollection<GitStashEventViewModel> activeStashes = new();

        private readonly object entriesLock = new();
        private readonly RangeObservableCollection<GitTreeEvent> entries = new();

        private readonly object remotesLock = new();
        private readonly ObservableCollection<GitRemoteViewModel> remotes = new();

        private readonly ObservableCollection<GitTreeEvent> selectedLogEntries = new();
        private readonly ObservableCollection<GitStashEventViewModel> selectedStashEntries = new();
        private readonly ISnackbarService snack;
        private readonly IOptionsWriter<GitStageOptions> updateStageOptions;
        private readonly IRepositoryWatcher repositoryWatcher;
        private readonly GitRepositoryMonitor monitor;
        private readonly IDisposable settingsMonitorHandle;

        private readonly ICommand createStashBranchCommand;

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
            IOptionsMonitor<GitStageOptions> stagingOptions,
            IOptionsWriter<GitStageOptions> updateStageOptions
        )
        {
            this.snack = snack;
            this.updateStageOptions = updateStageOptions;
            showSpacesAsDots = stagingOptions.CurrentValue.ShowSpacesAsDots;
            settingsMonitorHandle = stagingOptions.OnChange(options => SetProperty(ref showSpacesAsDots, options.ShowSpacesAsDots, nameof(ShowSpacesAsDots)));
            GitLogPageOptions options = navigation.GetOptions<GitLogPageOptions>(typeof(GitLogPage).FullName!)
                ?? throw new InvalidOperationException("Options may not be null");
            Repository = options.Repository;
            monitor = new GitRepositoryMonitor();
            monitor.LogChanged += OnLogChanged;
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

                SelectedContext = LogEntriesViewModel.CreateContext(
                    selectedLogEntries.Select(vm => vm.Event).ToList(),
                    Repository,
                    monitor.CreateCallback(),
                    snack,
                    RevisionViewMode
                );
                ViewMode = SelectedContext is null
                    ? LogViewMode.None
                    : LogViewMode.Files;
                if (selectedLogEntries.Count >= 2)
                {
                    RevisionViewMode = LogRevisionViewMode.Diff;
                }
            };
            selectedStashEntries.CollectionChanged += (sender, args) =>
            {
                SelectedContext = LogEntriesViewModel.CreateContext(
                    selectedStashEntries.Select(vm => vm.Event).ToList(),
                    Repository,
                    monitor.CreateCallback(),
                    snack,
                    RevisionViewMode
                );
                ViewMode = SelectedContext is null
                    ? LogViewMode.None
                    : LogViewMode.Files;
            };

            createStashBranchCommand = new NotNullCallbackCommand<GitStashEventViewModel>(
                model =>
                {
                    INavigationService child = navigation.NavigateNewWindow(
                        typeof(TextPromptPage).FullName!,
                        new TextPromptOptions(
                            $"stash-{model.StashIndex}",
                            "Branch name",
                            GitBranchName.IsValid,
                            GitBranchName.CreateLocal
                        ),
                        new NavigationOverrideOptions(
                            PromptSize,
                            PromptOffset,
                            IsModal: true,
                            IsStatusBarVisible: false
                        )
                    );
                    child.Closed += async (sender, args) =>
                    {
                        GitBranchName? branchName = child.GetDialogResult<GitBranchName>();
                        if (branchName is not null)
                        {
                            try
                            {
                                await Repository.CreateBranchAsync(branchName, new GitCreateBranchOptions(model.Event.Id));
                                await CheckRepositoryStatusAsync();
                            }
                            catch (InvalidOperationException ioe)
                            {
                                snack.ShowError(ioe.Message, ioe, TimeSpan.FromSeconds(4));
                            }
                        }
                    };
                }
            );

            FetchRemotesCommand = new AsyncCallbackCommand(FetchRemotesAsync);
            CheckoutCommitCommand = new AsyncCallbackCommand<GitCommitId>(
                async id =>
                {
                    if (id is null)
                    {
                        return;
                    }
                    try
                    {
                        await Repository.CheckoutCommitDetachedAsync(id);
                        await CheckRepositoryStatusAsync();
                        snack.ShowSuccess($"Checked out commit with id '{id.Hash[0..7]}'");
                    }
                    catch (InvalidOperationException e)
                    {
                        snack.ShowError(e.Message, e, TimeSpan.FromSeconds(10));
                    }
                },
                id => id is not null
            );
            CheckoutBranchCommand = new AsyncCallbackCommand(
                async () =>
                {
                    IsCheckoutBranchVisible = false;
                    try
                    {
                        var branchName = GitBranchName.CreateLocal(checkoutBranchName!); // name is validated by the canExecute callback
                        await Repository.CheckoutBranchAsync(branchName, new GitCheckoutBranchOptions(true));
                        await CheckRepositoryStatusAsync();
                        snack.ShowSuccess($"Branch {branchName.Name} created");
                        checkoutBranchName = string.Empty;
                    }
                    catch (InvalidOperationException e)
                    {
                        snack.ShowError("Could not create branch", e, TimeSpan.FromSeconds(10));
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

            SquashCommitsCommand = new AsyncCallbackCommand<LogEntriesViewModel?>(
                SquashCommitAsync,
                gte => gte is not null && changesCount == 0
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
                    SelectedContext = selectedContext!.SwapEntries();
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
            set
            {
                if (SetProperty(ref showSpacesAsDots, value))
                {
                    updateStageOptions.Update(snap => snap.ShowSpacesAsDots = value);
                }
            }
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
        public IList<GitStashEventViewModel> SelectedStashEntries => selectedStashEntries;

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
                if (SetProperty(ref revisionViewMode, value) && selectedContext is not null)
                {
                    selectedContext.ViewMode = revisionViewMode;
                }
            }
        }

        public string? CheckoutBranchName
        {
            get => checkoutBranchName;
            set => SetProperty(ref checkoutBranchName, value);
        }

        public ICommand NavigateToStageAreaCommand { get; }
        public ICommand RefreshStatusCommand { get; }
        public ICommand FetchRemotesCommand { get; }
        public ICommand CheckoutCommitCommand { get; }
        public ICommand CheckoutBranchCommand { get; }
        public ICommand RevealInExplorerCommand { get; }
        public ICommand CopyContentCommand { get; }
        public ICommand CopyCommitHashCommand { get; }
        public ICommand CopySubjectCommand { get; }
        public ICommand SquashCommitsCommand { get; }
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
                    monitor.LogChanged -= OnLogChanged;
                    settingsMonitorHandle.Dispose();
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

        private void OnLogChanged(object? sender, EventArgs args) => _ = CheckRepositoryStatusAsync();

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
            await foreach (GitStash stashEntry in Repository.StashListAsync())
            {
                lock (activeStashesLock)
                {
                    activeStashes.Add(new GitStashEventViewModel(stashEntry, createStashBranchCommand));
                }
            }
        }

        private async Task SquashCommitAsync(LogEntriesViewModel? gte)
        {
            IsWorking = true;
            string subject = gte!.Root.Subject;
            string body = gte.Root.Body;
            var branch = GitBranchName.CreateLocal($"gitout-bkp/{Guid.NewGuid():N}");
            await Repository.CreateBranchAsync(branch);
            await Repository.ResetToCommitAsync(gte!.Root.Id);
            await Repository.AddAllAsync();
            await Repository.CommitAsync(GitCommitOptions.AmendLatest(body.Length > 0 ? $"{subject}{Environment.NewLine}{Environment.NewLine}{body}" : subject));
            snack.ShowSuccess("Successfully reset to previous commit");
            await CheckRepositoryStatusAsync();
            IsWorking = false;
            const string approveActionText = "YES";
            SnackAction? result = await snack.ShowAsync(
                Snack.Builder()
                    .WithMessage("Remove temporary branch?")
                    .WithDuration(TimeSpan.FromMinutes(1))
                    .AddAction(approveActionText)
            );
            if (result?.Text == approveActionText)
            {
                IsWorking = true;
                await Repository.DeleteBranchAsync(branch, new GitDeleteBranchOptions(ForceDelete: true));
                await CheckRepositoryStatusAsync();
                IsWorking = false;
            }
        }

        private static List<GitTreeEvent> BuildTree(IEnumerable<GitHistoryEvent> log)
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
