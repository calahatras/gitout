using System;
using System.Collections.Generic;

namespace GitOut.Features.Git;

public class GitBlameLine
{
    public int FinalLineNumber { get; init; }
    public string Content { get; init; } = string.Empty;
}

public class GitBlameHunk
{
    public GitCommitId CommitId { get; init; } = GitCommitId.Empty;
    public string Author { get; init; } = string.Empty;
    public string AuthorEmail { get; init; } = string.Empty;
    public DateTimeOffset AuthorDate { get; init; }
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyList<GitBlameLine> Lines { get; init; } = Array.Empty<GitBlameLine>();
}

public class GitBlameResult
{
    public GitBlameResult(IReadOnlyList<GitBlameHunk> hunks)
    {
        Hunks = hunks;
    }

    public IReadOnlyList<GitBlameHunk> Hunks { get; }
}
