using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace GitOut
{
    public class GitHistory
    {
        private readonly CollectionViewSource gitHistoryEntries;
        public GitHistory()
        {
            ObservableCollection<GitHistoryEvent> entries = new ObservableCollection<GitHistoryEvent>();
            CollectionViewSource entrySource = new CollectionViewSource
            {
                Source = entries
            };
            entrySource.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Descending));
            gitHistoryEntries = entrySource;

            GitCommitId initialId = GitCommitId.FromHash("f5105fe06dbb07b11482c5fe55cf22165157a700");
            entries.Add(GitHistoryEvent.FromHistory(initialId, DateTimeOffset.UtcNow.AddMinutes(-10), "epic(app): initial thing", null));
            entries.Add(GitHistoryEvent.FromHistory(GitCommitId.FromHash("f5105fe06dbb07b11482c5fe55cf22165157a700"), DateTimeOffset.UtcNow.AddMinutes(-10), "wip(component): that thing over there", initialId));
            entries.Add(GitHistoryEvent.FromHistory(GitCommitId.FromHash("453a5486baa8d77ccffb85f043a49648a613d12d"), DateTimeOffset.UtcNow.AddMinutes(-10), "enhancement(component): that thing over there", GitCommitId.FromHash("f5105fe06dbb07b11482c5fe55cf22165157a700")));
            entries.Add(GitHistoryEvent.FromHistory(GitCommitId.FromHash("effe31c68490a94895013ae1e27dfaf885e9520a"), DateTimeOffset.UtcNow.AddMinutes(-10), "feature(component): that thing over there", initialId));
        }

        public ICollectionView Entries => gitHistoryEntries.View;
    }

    public class GitHistoryEvent
    {
        public GitHistoryEvent(GitCommitId id, DateTimeOffset offset, string message, GitCommitId parent)
        {
            Id = id;
            Date = offset;
            CommitMessage = message;
            Parent = parent;
        }

        public GitCommitId Id { get; }
        public string CommitMessage { get; }
        public GitCommitId Parent { get; }
        public DateTimeOffset Date { get; }

        public static GitHistoryEvent FromHistory(GitCommitId id, DateTimeOffset offset, string message, GitCommitId parent)
        {
            return new GitHistoryEvent(id, offset, message, parent);
        }
    }

    public class GitCommitId
    {
        private GitCommitId(string hash)
        {
            Hash = hash;
        }

        public string Hash { get; }

        public static GitCommitId FromHash(string hash)
        {
            return new GitCommitId(hash);
        }
    }
}
