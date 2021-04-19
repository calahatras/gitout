using System.IO;
using GitOut.Features.Git;

namespace GitOut.Features.IO
{
    public class GitRepositoryFileSystemWatcherProvider : IGitRepositoryWatcherProvider
    {
        public IRepositoryWatcher PrepareWatchRepositoryChanges(IGitRepository repository)
            => new RepositoryWatcher(new FileSystemWatcher(repository.WorkingDirectory.Directory)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite
                    | NotifyFilters.CreationTime
                    | NotifyFilters.FileName
            });

        private class RepositoryWatcher : IRepositoryWatcher
        {
            private readonly FileSystemWatcher watcher;

            public RepositoryWatcher(FileSystemWatcher watcher)
            {
                this.watcher = watcher;
                watcher.Changed += OnFileSystemChanges;
                watcher.Created += OnFileSystemChanges;
                watcher.Deleted += OnFileSystemChanges;
            }

            public bool EnableRaisingEvents
            {
                get => watcher.EnableRaisingEvents;
                set => watcher.EnableRaisingEvents = value;
            }

            public event RepositoryWatcherEventHandler? Events;

            public void Dispose()
            {
                watcher.Changed -= OnFileSystemChanges;
                watcher.Created -= OnFileSystemChanges;
                watcher.Deleted -= OnFileSystemChanges;
                watcher.Dispose();
            }

            private void OnFileSystemChanges(object sender, FileSystemEventArgs args)
            {
                if (args.Name is not null)
                {
                    Events?.Invoke(this, new RepositoryWatcherEventArgs(args.Name.Replace("\\", "/")));
                }
            }
        }
    }
}
