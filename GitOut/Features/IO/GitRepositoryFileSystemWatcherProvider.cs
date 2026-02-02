using System;
using System.IO;
using GitOut.Features.Git;

namespace GitOut.Features.IO;

public class GitRepositoryFileSystemWatcherProvider : IGitRepositoryWatcherProvider
{
    public IRepositoryWatcher PrepareWatchRepositoryChanges(
        IGitRepository repository,
        RepositoryWatcherOptions options
    ) =>
        new RepositoryWatcher(
            new FileSystemWatcher(repository.WorkingDirectory.Directory)
            {
                IncludeSubdirectories = true,
                NotifyFilter =
                    NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.FileName,
            },
            options
        );

    private class RepositoryWatcher : IRepositoryWatcher
    {
        private readonly FileSystemWatcher watcher;
        private readonly RepositoryWatcherOptions options;

        public RepositoryWatcher(FileSystemWatcher watcher, RepositoryWatcherOptions options)
        {
            this.watcher = watcher;
            this.options = options;
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
            if (args.Name is null)
            {
                return;
            }

            bool isGitFolder = args.Name.StartsWith(".git");
            if (
                (isGitFolder && options.HasFlag(RepositoryWatcherOptions.GitFolder))
                || (!isGitFolder && options.HasFlag(RepositoryWatcherOptions.Workspace))
            )
            {
                Events?.Invoke(
                    this,
                    new RepositoryWatcherEventArgs(
                        args.Name.Replace("\\", "/", StringComparison.Ordinal)
                    )
                );
            }
        }
    }
}
