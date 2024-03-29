using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using GitOut.Features.Collections;
using GitOut.Features.Git.Files;
using GitOut.Features.IO;
using GitOut.Features.Material.Snackbar;
using GitOut.Features.Wpf;

namespace GitOut.Features.Git.Log
{
    public class LogEntriesViewModel : INotifyPropertyChanged
    {
        private readonly GitHistoryEvent? diff;
        private readonly IGitRepository repository;
        private readonly IGitRepositoryNotifier notifier;
        private readonly ISnackbarService snack;

        private readonly CollectionViewSource currentSource;

        private IEnumerable<IGitFileEntryViewModel> logFiles;
        private readonly ILazyAsyncEnumerable<IGitFileEntryViewModel, RelativeDirectoryPath> allFiles;
        private readonly IEnumerable<IGitFileEntryViewModel> diffFiles;
        private readonly IEnumerable<IGitFileEntryViewModel> flattenedDiffFiles;

        private ICollectionView rootView;
        private IGitFileEntryViewModel? selectedItem;
        private LogRevisionViewMode viewMode = LogRevisionViewMode.CurrentRevision;

        private LogEntriesViewModel(
            GitHistoryEvent root,
            IGitRepository repository,
            IGitRepositoryNotifier notifier,
            ISnackbarService snack
        )
        {
            Root = root;
            this.repository = repository;
            this.notifier = notifier;
            this.snack = snack;
            allFiles = new SortedLazyAsyncCollection<IGitFileEntryViewModel, RelativeDirectoryPath>(
                _ => ListAllFilesAsync(),
                IGitDirectoryEntryViewModel.CompareItems
            );
            var logFiles = new SortedLazyAsyncCollection<IGitFileEntryViewModel, RelativeDirectoryPath>(
                relativePath => GitFileEntryViewModelFactory.ListIdAsync(root.Id, repository, relativePath),
                IGitDirectoryEntryViewModel.CompareItems
            );

            _ = logFiles.MaterializeAsync(RelativeDirectoryPath.Root).AsTask();
            this.logFiles = logFiles;

            diffFiles = new SortedLazyAsyncCollection<IGitFileEntryViewModel, RelativeDirectoryPath>(
                relativePath => GitFileEntryViewModelFactory.DiffIdAsync(diff?.Id ?? root.ParentId, root.Id, repository, RelativeDirectoryPath.Root),
                IGitDirectoryEntryViewModel.CompareItems
            );

            flattenedDiffFiles = new SortedLazyAsyncCollection<IGitFileEntryViewModel, RelativeDirectoryPath>(
                relativePath => GitFileEntryViewModelFactory.DiffAllAsync(diff?.Id ?? root.ParentId, root.Id, repository),
                IGitDirectoryEntryViewModel.CompareItems
            );

            currentSource = new CollectionViewSource
            {
                Source = logFiles
            };
            rootView = currentSource.View;

            Branches = root
                .Branches
                .Select(branch => new BranchNameViewModel(
                    branch,
                    repository,
                    notifier,
                    snack
                ))
                .ToList()
                .AsReadOnly();
            HasBranches = Branches.Count > 0;

            SelectFileCommand = new CallbackCommand<IGitFileEntryViewModel>(SelectItem);
        }

        private LogEntriesViewModel(
            GitHistoryEvent root,
            GitHistoryEvent diff,
            IGitRepository repository,
            IGitRepositoryNotifier notifier,
            ISnackbarService snack
        ) : this(root, repository, notifier, snack) => this.diff = diff;

        public GitHistoryEvent Root { get; }

        public string Subject => Root.Subject;

        public ILazyAsyncEnumerable<IGitFileEntryViewModel, RelativeDirectoryPath> AllFiles => allFiles;

        public ICollectionView RootFiles
        {
            get => rootView;
            private set => SetProperty(ref rootView, value);
        }

        public bool HasBranches { get; }
        public bool IsSingleSelection => diff is null;

        public IReadOnlyCollection<BranchNameViewModel> Branches { get; }

        public IGitFileEntryViewModel? SelectedItem
        {
            get => selectedItem;
            set => SetProperty(ref selectedItem, value);
        }
        public ICommand SelectFileCommand { get; }

        public LogRevisionViewMode ViewMode
        {
            get => viewMode;
            set
            {
                if (SetProperty(ref viewMode, value))
                {
                    IGitFileEntryViewModel? previousSelection = selectedItem;
                    IEnumerable<IGitFileEntryViewModel> source = value switch
                    {
                        LogRevisionViewMode.CurrentRevision => logFiles,
                        LogRevisionViewMode.Diff => diffFiles,
                        LogRevisionViewMode.DiffInline => flattenedDiffFiles,
                        _ => throw new InvalidOperationException($"Invalid view mode: {value}")
                    };
                    if (source is ILazyAsyncEnumerable<object, RelativeDirectoryPath> lazy)
                    {
                        SynchronizationContext? context = SynchronizationContext.Current;
                        ValueTask t = lazy.MaterializeAsync(RelativeDirectoryPath.Root);
                        t.AsTask().ContinueWith(task => context!.Post(d =>
                        {
                            currentSource.Source = source;
                            RootFiles = currentSource.View;
                            SelectItem(previousSelection);
                        }, null));
                    }
                    else
                    {
                        currentSource.Source = source;
                        RootFiles = currentSource.View;
                        SelectItem(previousSelection);
                    }
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public static LogEntriesViewModel? CreateContext<T>(
            IList<T> entries,
            IGitRepository repository,
            IGitRepositoryNotifier notifier,
            ISnackbarService snack,
            LogRevisionViewMode mode
        ) where T : GitHistoryEvent
        {
            if (entries.Count is 0 or >= 3)
            {
                return null;
            }
            LogEntriesViewModel context;
            if (entries.Count == 1)
            {
                // single item, show file content and diff against parent
                context = new LogEntriesViewModel(entries[0], repository, notifier, snack);
            }
            else //if (entries.Count == 2)
            {
                context = new LogEntriesViewModel(entries[0], entries[1], repository, notifier, snack);
            }
            context.ViewMode = mode;
            return context;
        }

        public LogEntriesViewModel SwapEntries() => diff is null
            ? throw new InvalidOperationException("Cannot swap entries when diff is not set")
            : new LogEntriesViewModel(diff, Root, repository, notifier, snack)
            {
                selectedItem = selectedItem,
                ViewMode = ViewMode
            };

        private async IAsyncEnumerable<IGitFileEntryViewModel> ListAllFilesAsync()
        {
            IGitFileEntryViewModel? currentSelection = selectedItem;
            IDictionary<string, DirectoryScaffold> tree = new Dictionary<string, DirectoryScaffold>();
            int max = 0;
            await foreach (GitFileEntry item in repository.ListTreeAsync(Root.Id, DiffOptions.Builder().Recursive().Build()))
            {
                var viewModel = GitFileViewModel.Snapshot(repository, item, RelativeDirectoryPath.Root);
                if (!tree.TryGetValue(item.Directory.Directory, out DirectoryScaffold? directory))
                {
                    directory = new DirectoryScaffold(item.Directory);
                    tree.Add(item.Directory.Directory, directory);
                    DirectoryScaffold current = directory;
                    while (!current.IsRoot)
                    {
                        RelativeDirectoryPath parent = current.Path.Parent;
                        if (tree.TryGetValue(parent.Directory, out DirectoryScaffold? parentDirectory))
                        {
                            parentDirectory.Add(current);
                            break;
                        }

                        parentDirectory = new DirectoryScaffold(parent) { current };
                        tree.Add(parent.Directory, parentDirectory);
                        current = parentDirectory;
                    }
                    max = Math.Max(max, item.Directory.Segments.Count);
                }
                directory.Add(viewModel);
                yield return viewModel;
            }

            // create directories
            IEnumerable<DirectoryScaffold> available = tree.Values;
            IGitDirectoryEntryViewModel root = NormalizeScaffold(available.Single(directory => directory.IsRoot));

            currentSource.Source = logFiles = root;
            RootFiles = currentSource.View;
            SelectItem(currentSelection);

            IGitDirectoryEntryViewModel NormalizeScaffold(DirectoryScaffold scaffold) => new GitDirectoryViewModel(
                repository,
                scaffold.FileName,
                scaffold.Path,
                scaffold
                    .OfType<DirectoryScaffold>()
                    .Select(NormalizeScaffold)
                    .Concat(scaffold
                        .OfType<GitFileViewModel>()
                        .Cast<IGitFileEntryViewModel>()
                    )
            );
        }

        private void SelectItem(IGitFileEntryViewModel? entry)
        {
            if (entry is null || currentSource.Source is not IEnumerable<IGitFileEntryViewModel> items)
            {
                return;
            }
            IEnumerable<IGitDirectoryEntryViewModel> current = items.OfType<IGitDirectoryEntryViewModel>();
            IGitFileEntryViewModel? selectedItem = null;
            foreach (string segment in entry.Path.Segments)
            {
                IGitDirectoryEntryViewModel? child = current.FirstOrDefault(directory => directory.FileName.ToString() == segment);
                if (child is not null)
                {
                    child.IsExpanded = true;
                    current = child.OfType<IGitDirectoryEntryViewModel>();
                    if (selectedItem is null)
                    {
                        selectedItem = child.FirstOrDefault(f => f.FullPath == entry.FullPath);
                    }
                }
            }
            if (selectedItem is null)
            {
                selectedItem = items.FirstOrDefault(f => f.FullPath == entry.FullPath);
            }
            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() => SelectedItem = selectedItem));
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

        private class DirectoryScaffold : IGitDirectoryEntryViewModel
        {
            private readonly ICollection<IGitFileEntryViewModel> entries = new List<IGitFileEntryViewModel>();

            public DirectoryScaffold(RelativeDirectoryPath directory)
            {
                Path = directory;
                IsRoot = directory == RelativeDirectoryPath.Root;
                Count = directory.Segments.Count;
                FileName = directory.Name;
                FullPath = directory.ToString();
            }

            public RelativeDirectoryPath Path { get; }

            public bool IsRoot { get; }

            public int Count { get; }

            public bool IsExpanded { get; set; }

            public FileName FileName { get; }
            public string FullPath { get; }

            public string IconResourceKey => throw new InvalidOperationException("Scaffold does not hold an icon");

            public IEnumerator<IGitFileEntryViewModel> GetEnumerator() => entries.GetEnumerator();
            internal void Add(IGitFileEntryViewModel entry) => entries.Add(entry);
            IEnumerator IEnumerable.GetEnumerator() => entries.GetEnumerator();
        }
    }
}
