using GitOut.Features.Git;

namespace GitOut.Features.Git.Ignore;

public class GitIgnorePageOptions
{
    public GitIgnorePageOptions(IGitRepository repository) => Repository = repository;

    public IGitRepository Repository { get; }

    public static GitIgnorePageOptions Open(IGitRepository repository) => new(repository);
}
