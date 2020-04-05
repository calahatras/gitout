using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using GitOut.Features.Navigation;
using GitOut.Features.Wpf;

namespace GitOut.Features.Git.Log
{
    public class GitLogViewModel
    {
        private readonly object entriesLock = new object();

        public GitLogViewModel(
            INavigationService navigation,
            ITitleService title
        )
        {
            GitLogPageOptions? options = navigation.GetOptions<GitLogPageOptions>(typeof(GitLogPage).FullName!);
            _ = options ?? throw new ArgumentNullException(nameof(options), "Options may not be null");
            title.Title = "Log";

            var entries = new ObservableCollection<GitHistoryEvent>();
            BindingOperations.EnableCollectionSynchronization(entries, entriesLock);
            Entries = CollectionViewSource.GetDefaultView(entries);
            Entries.SortDescriptions.Add(new SortDescription("AuthorDate", ListSortDirection.Descending));

            options.Repository.ExecuteLogAsync()
                .ContinueWith(task =>
                {
                    IEnumerable<GitHistoryEvent> history = task.Result;
                    lock (entriesLock)
                    {
                        foreach (GitHistoryEvent item in history)
                        {
                            entries.Add(item);
                        }
                    }
                });
        }

        public ICollectionView Entries { get; }
    }
}
