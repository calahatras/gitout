namespace GitOut.Features.Git;

public interface IGitStashBuilder
{
    string Name { get; }

    IGitStashBuilder UseId(GitCommitId id);
    GitStash Build();
}
