using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Data;
using GitOut.Features.Git.Files;

namespace GitOut.Features.Git.Log
{
    public class LogEntriesViewModel
    {
        private readonly GitTreeEvent? diff;
        private readonly IGitRepository repository;

        private readonly object rootFilesLock = new object();
        private readonly ObservableCollection<IGitFileEntryViewModel> rootFiles = new ObservableCollection<IGitFileEntryViewModel>();

        public LogEntriesViewModel(GitTreeEvent root, IGitRepository repository)
        {
            Root = root;
            this.repository = repository;
            BindingOperations.EnableCollectionSynchronization(rootFiles, rootFilesLock);
            RootFiles = CollectionViewSource.GetDefaultView(rootFiles);
        }

        public LogEntriesViewModel(GitTreeEvent root, GitTreeEvent diff, IGitRepository repository)
            : this(root, repository) => this.diff = diff;

        public GitTreeEvent Root { get; }
        public string Subject => Root.Event.Subject;
        public ICollectionView RootFiles { get; }

        public Task SwitchViewAsync(LogRevisionViewMode mode) => mode switch
        {
            LogRevisionViewMode.CurrentRevision => ListLogFilesAsync(),
            LogRevisionViewMode.Diff => ListDiffFilesAsync(),
            LogRevisionViewMode.DiffInline => ListDiffInlineFilesAsync(),
            _ => Task.CompletedTask,
        };

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
            context.SwitchViewAsync(mode);
            return context;
        }

        private async Task ListLogFilesAsync()
        {
            lock (rootFilesLock)
            {
                rootFiles.Clear();
            }
            IAsyncEnumerable<IGitFileEntryViewModel> entries = GitFileEntryViewModelFactory.ListIdAsync(Root.Event.Id, repository);
            await foreach (IGitFileEntryViewModel viewmodel in entries)
            {
                lock (rootFilesLock)
                {
                    rootFiles.Add(viewmodel);
                }
            }
        }

        private async Task ListDiffFilesAsync()
        {
            lock (rootFilesLock)
            {
                rootFiles.Clear();
            }
            IAsyncEnumerable<IGitFileEntryViewModel> entries = GitFileEntryViewModelFactory.DiffIdAsync(diff?.Event.Id ?? Root.Event.Parent?.Id, Root.Event.Id, repository);
            await foreach (IGitFileEntryViewModel viewmodel in entries)
            {
                lock (rootFilesLock)
                {
                    rootFiles.Add(viewmodel);
                }
            }
        }

        private async Task ListDiffInlineFilesAsync()
        {
            lock (rootFilesLock)
            {
                rootFiles.Clear();
            }
            IAsyncEnumerable<IGitFileEntryViewModel> entries = GitFileEntryViewModelFactory.DiffAllAsync(diff?.Event.Id ?? Root.Event.Parent?.Id, Root.Event.Id, repository);
            await foreach (IGitFileEntryViewModel viewmodel in entries)
            {
                lock (rootFilesLock)
                {
                    rootFiles.Add(viewmodel);
                }
            }
        }
    }
}
