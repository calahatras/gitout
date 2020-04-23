using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using GitOut.Features.Git.Stage;
using GitOut.Features.Navigation;
using GitOut.Features.Wpf;

namespace GitOut.Features.Git.Log
{
    public class GitLogViewModel : INotifyPropertyChanged, INavigationListener
    {
        private readonly object entriesLock = new object();
        private readonly ObservableCollection<GitTreeEvent> entries = new ObservableCollection<GitTreeEvent>();

        private int changesCount;

        public GitLogViewModel(
            INavigationService navigation,
            ITitleService title
        )
        {
            GitLogPageOptions options = navigation.GetOptions<GitLogPageOptions>(typeof(GitLogPage).FullName!)
                ?? throw new ArgumentNullException(nameof(options), "Options may not be null");
            title.Title = "Log";

            BindingOperations.EnableCollectionSynchronization(entries, entriesLock);
            Entries = CollectionViewSource.GetDefaultView(entries);

            NavigateToStageAreaCommand = new NavigateLocalCommand<object>(navigation, typeof(GitStagePage).FullName!, e => GitStagePageOptions.Stage(Repository));

            Repository = options.Repository;
        }

        public int ChangesCount
        {
            get => changesCount;
            private set => SetProperty(ref changesCount, value);
        }

        public ICollectionView Entries { get; }

        public IGitRepository Repository { get; }

        public ICommand NavigateToStageAreaCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public async void Navigated(NavigationType type)
        {
            if (type == NavigationType.Initial || type == NavigationType.NavigatedBack)
            {
                await CheckRepositoryStatusAsync();
            }
        }

        private async Task CheckRepositoryStatusAsync()
        {
            IEnumerable<GitHistoryEvent> tree = await Repository.ExecuteLogAsync();
            IEnumerable<GitTreeEvent> history = BuildTree(tree);
            lock (entriesLock)
            {
                entries.Clear();
                foreach (GitTreeEvent item in history)
                {
                    entries.Add(item);
                }
            }
            GitStatusResult status = await Repository.ExecuteStatusAsync();
            ChangesCount = status.Changes.Count;
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

        private void SetProperty<T>(ref T prop, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!ReferenceEquals(prop, value))
            {
                prop = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
