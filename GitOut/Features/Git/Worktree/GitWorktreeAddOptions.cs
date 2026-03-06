using GitOut.Features.IO;

namespace GitOut.Features.Git.Worktree;

public class GitWorktreeAddOptions
{
    public GitWorktreeAddOptions(DirectoryPath path) => Path = path;

    public DirectoryPath Path { get; }
    public GitBranchName? Branch { get; init; }
    public GitObjectId? Commit { get; init; }
    public bool CreateBranch { get; init; }

    public static GitWorktreeAddOptions Builder(DirectoryPath path) => new(path);
}
