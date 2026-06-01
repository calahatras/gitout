using GitOut.Features.IO;

namespace GitOut.Features.Git.Worktree;

public sealed record GitWorktree(DirectoryPath Path, GitObjectId Hash, GitBranchName Branch);
