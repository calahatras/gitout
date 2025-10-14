using GitOut.Features.Git;

namespace GitOut.Features.IO
{
    public interface IGitRepositoryWatcherProvider
    {
        IRepositoryWatcher PrepareWatchRepositoryChanges(
            IGitRepository repository,
            RepositoryWatcherOptions options = RepositoryWatcherOptions.All
        );
    }
}
