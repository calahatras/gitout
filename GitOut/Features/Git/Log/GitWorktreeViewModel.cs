using GitOut.Features.Git.Worktree;

namespace GitOut.Features.Git.Log;

public class GitWorktreeViewModel
{
    public GitWorktreeViewModel(GitWorktree model, IGitRepository repository)
    {
        Model = model;
        Repository = repository;
    }

    public GitWorktree Model { get; }
    public IGitRepository Repository { get; }
}
