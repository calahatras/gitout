namespace GitOut.Features.Git;

public enum GitStatusChangeType
{
    None,
    Ordinary,
    RenamedOrCopied,
    Unmerged,
    Untracked,
    Ignored,
}
