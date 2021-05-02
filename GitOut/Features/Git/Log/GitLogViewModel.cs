using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using GitOut.Features.Git.Stage;
using GitOut.Features.Material.Snackbar;
using GitOut.Features.Navigation;
using GitOut.Features.Wpf;
using Microsoft.Extensions.Options;

namespace GitOut.Features.Git.Log
{
    public class GitLogViewModel : INotifyPropertyChanged, INavigationListener
    {
        private readonly object activeStashesLock = new();
        private readonly ObservableCollection<GitStash> activeStashes = new();

        private readonly object entriesLock = new();
        private readonly ObservableCollection<GitTreeEvent> entries = new();

        private readonly object remotesLock = new();
        private readonly ObservableCollection<GitRemoteViewModel> remotes = new();

        private readonly ObservableCollection<GitTreeEvent> selectedLogEntries = new();

        private readonly ISnackbarService snack;

        private int changesCount;
        private bool includeRemotes = true;
        private bool showSpacesAsDots;
        private bool isStashesVisible = false;
        private bool isSearchDisplayed = false;
        private bool isCheckoutBranchVisible = false;
        private LogViewMode viewMode = LogViewMode.None;

        private LogRevisionViewMode revisionViewMode = LogRevisionViewMode.CurrentRevision;
        private LogEntriesViewModel? selectedContext;

        public GitLogViewModel(
            INavigationService navigation,
            ITitleService title,
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
            CheckoutBranchCommand = new AsyncCallbackCommand<TextBox>(
                async input =>
                {
                    IsCheckoutBranchVisible = false;
                    try
                    {
                        string name = input!.Text;
                        var branchName = GitBranchName.CreateLocal(name!); // name is validated by the canExecute callback
                        await Repository.CheckoutBranchAsync(branchName);
                        await CheckRepositoryStatusAsync();
                        snack.ShowSuccess($"Branch {branchName.Name} created");
                        input.Text = string.Empty;
                    }
                    catch (InvalidOperationException e)
                    {
                        snack.ShowError($"Could not create branch", e, TimeSpan.FromSeconds(10));
                    }
                },
                name => name is not null && GitBranchName.IsValid(name.Text)
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
            SelectCommitCommand = new NotNullCallbackCommand<GitHistoryEvent>(commit =>
            {
                foreach (GitTreeEvent entry in entries)
                {
                    entry.IsSelected = false;
                }
                GitTreeEvent gitTreeEvent = entries.First(e => e.Event.Id == commit.Id);
                gitTreeEvent.IsSelected = true;
            });

            AppendSelectCommitCommand = new NotNullCallbackCommand<GitHistoryEvent>(commit => entries.First(e => e.Event.Id == commit.Id).IsSelected = true);
            CloseAutocompleteCommand = new CallbackCommand(() => IsSearchDisplayed = false);
            ShowSearchFilesCommand = new CallbackCommand(() => IsSearchDisplayed = true);
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
                    await Repository.FetchAsync(remote.Model);
                }
            }
            snack.ShowSuccess("Fetched all selected remotes");
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
            IEnumerable<GitHistoryEvent> tree = await Repository.LogAsync(IncludeRemotes ? LogOptions.WithRemoteBranches() : LogOptions.OnlyLocalBranches()).ConfigureAwait(false);
            IEnumerable<GitTreeEvent> history = BuildTree(tree);
            lock (entriesLock)
            {
                entries.Clear();
                foreach (GitTreeEvent item in history)
                {
                    entries.Add(item);
                }
            }
            GitStatusResult status = await Repository.StatusAsync().ConfigureAwait(false);
            ChangesCount = status.Changes.Count;
        }

        private async Task RefreshStashListAsync()
        {
            lock (activeStashesLock)
            {
                activeStashes.Clear();
            }
            await foreach (GitStash stashEntry in Repository.StashListAsync())
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
