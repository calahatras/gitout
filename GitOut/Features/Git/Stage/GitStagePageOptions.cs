namespace GitOut.Features.Git.Stage;

public class GitStagePageOptions
{
    public GitStagePageOptions(IGitRepository repository) => Repository = repository;

    public IGitRepository Repository { get; }

    public static GitStagePageOptions Stage(IGitRepository repository) => new(repository);
}
