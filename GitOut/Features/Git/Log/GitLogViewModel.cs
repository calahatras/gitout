using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
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

            var entries = new ObservableCollection<GitTreeEvent>();
            BindingOperations.EnableCollectionSynchronization(entries, entriesLock);
            Entries = CollectionViewSource.GetDefaultView(entries);

            options.Repository.ExecuteLogAsync()
                .ContinueWith(task => BuildTree(task.Result))
                .ContinueWith(task =>
                {
                    IEnumerable<GitTreeEvent> history = task.Result;
                    lock (entriesLock)
                    {
                        foreach (GitTreeEvent item in history)
                        {
                            entries.Add(item);
                        }
                    }
                });
        }

        public ICollectionView Entries { get; }

        private IEnumerable<GitTreeEvent> BuildTree(IEnumerable<GitHistoryEvent> log)
        {
            var stopwatch = Stopwatch.StartNew();
            var events = new List<GitTreeEvent>();
            IEnumerable<TreeBuildingLeaf> leafs = Enumerable.Empty<TreeBuildingLeaf>();
            GitTreeEvent? previous = null;
            foreach (GitHistoryEvent item in log)
            {
                var node = new GitTreeEvent(item, previous?.ColorIndex ?? 0);
                leafs = node.Process(leafs);
                events.Add(node);
                previous = node;
            }
            Trace.WriteLine($"Built git tree: {stopwatch.Elapsed.TotalMilliseconds}ms");

            return events;
        }
    }
}
