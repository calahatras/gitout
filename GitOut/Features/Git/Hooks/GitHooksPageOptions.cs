namespace GitOut.Features.Git.Hooks;

public class GitHooksPageOptions
{
    private GitHooksPageOptions(IGitRepository repository) => Repository = repository;

    public IGitRepository Repository { get; }

    public static GitHooksPageOptions OpenRepository(IGitRepository repository) => new(repository);
}
