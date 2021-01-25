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
using GitOut.Features.Wpf;

namespace GitOut.Features.Git.Log
{
    public class LogEntriesViewModel : INotifyPropertyChanged
    {
        private readonly GitTreeEvent? diff;
        private readonly IGitRepository repository;

        private readonly CollectionViewSource currentSource;

        private IEnumerable<IGitFileEntryViewModel> logFiles;
        private readonly ILazyAsyncEnumerable<IGitFileEntryViewModel> allFiles;
        private readonly IEnumerable<IGitFileEntryViewModel> diffFiles;
        private readonly IEnumerable<IGitFileEntryViewModel> flattenedDiffFiles;

        private ICollectionView rootView;
        private IGitFileEntryViewModel? selectedItem;
        private LogRevisionViewMode viewMode = LogRevisionViewMode.CurrentRevision;

        public LogEntriesViewModel(GitTreeEvent root, IGitRepository repository)
        {
            Root = root;
            this.repository = repository;

            allFiles = new SortedLazyAsyncCollection<IGitFileEntryViewModel>(() => ListAllFilesAsync(), IGitDirectoryEntryViewModel.CompareItems);
            var logFiles = new SortedLazyAsyncCollection<IGitFileEntryViewModel>(() => GitFileEntryViewModelFactory.ListIdAsync(root.Event.Id, repository), IGitDirectoryEntryViewModel.CompareItems);
            _ = logFiles.MaterializeAsync();
            this.logFiles = logFiles;
            diffFiles = new SortedLazyAsyncCollection<IGitFileEntryViewModel>(() => GitFileEntryViewModelFactory.DiffIdAsync(diff?.Event.Id ?? root.Event.Parent?.Id, root.Event.Id, repository), IGitDirectoryEntryViewModel.CompareItems);
            flattenedDiffFiles = new SortedLazyAsyncCollection<IGitFileEntryViewModel>(() => GitFileEntryViewModelFactory.DiffAllAsync(diff?.Event.Id ?? root.Event.Parent?.Id, root.Event.Id, repository), IGitDirectoryEntryViewModel.CompareItems);

            currentSource = new CollectionViewSource
            {
                Source = logFiles
            };
            rootView = currentSource.View;

            SelectFileCommand = new CallbackCommand<IGitFileEntryViewModel>(SelectItem);
        }

        public LogEntriesViewModel(GitTreeEvent root, GitTreeEvent diff, IGitRepository repository)
            : this(root, repository) => this.diff = diff;

        public GitTreeEvent Root { get; }

        public string Subject => Root.Event.Subject;

        public ILazyAsyncEnumerable<IGitFileEntryViewModel> AllFiles => allFiles;

        public ICollectionView RootFiles
        {
            get => rootView;
            private set => SetProperty(ref rootView, value);
        }

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
                    if (source is ILazyAsyncEnumerable<object> lazy)
                    {
                        SynchronizationContext? context = SynchronizationContext.Current;
                        ValueTask t = lazy.MaterializeAsync();
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

        public static LogEntriesViewModel? CreateContext(IList<GitTreeEvent> entries, IGitRepository repository, LogRevisionViewMode mode)
        {
            if (entries.Count == 0 || entries.Count >= 3)
            {
                return null;
            }
            LogEntriesViewModel context;
            if (entries.Count == 1)
            {
                // single item, show file content and diff against parent
                context = new LogEntriesViewModel(entries[0], repository);
            }
            else //if (entries.Count == 2)
            {
                context = new LogEntriesViewModel(entries[0], entries[1], repository);
            }
            context.ViewMode = mode;
            return context;
        }

        private async IAsyncEnumerable<IGitFileEntryViewModel> ListAllFilesAsync()
        {
            IGitFileEntryViewModel? currentSelection = selectedItem;
            IDictionary<string, DirectoryScaffold> tree = new Dictionary<string, DirectoryScaffold>();
            int max = 0;
            await foreach (GitFileEntry item in repository.ExecuteListTreeAsync(Root.Event.Id, DiffOptions.Builder().Recursive().Build()))
            {
                var viewModel = GitFileViewModel.Wrap(repository, item);
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

            static IGitDirectoryEntryViewModel NormalizeScaffold(DirectoryScaffold scaffold) => new GitDirectoryViewModel(
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
            if (entry is null || !(currentSource.Source is IEnumerable<IGitFileEntryViewModel> items))
            {
                return;
            }
            IEnumerable<IGitDirectoryEntryViewModel> current = items.OfType<IGitDirectoryEntryViewModel>();
            foreach (string segment in entry.Path.Segments)
            {
                IGitDirectoryEntryViewModel? child = current.FirstOrDefault(directory => directory.FileName == segment);
                if (!(child is null))
                {
                    child.IsExpanded = true;
                    current = child.OfType<IGitDirectoryEntryViewModel>();
                }
            }
            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() => SelectedItem = entry));
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
            }

            public RelativeDirectoryPath Path { get; }

            public bool IsRoot { get; }

            public int Count { get; }

            public bool IsExpanded { get; set; }

            public string FileName { get; }

            public string IconResourceKey => throw new InvalidOperationException("Scaffold does not hold an icon");

            public IEnumerator<IGitFileEntryViewModel> GetEnumerator() => entries.GetEnumerator();
            internal void Add(IGitFileEntryViewModel entry) => entries.Add(entry);
            IEnumerator IEnumerable.GetEnumerator() => entries.GetEnumerator();
        }
    }
}
