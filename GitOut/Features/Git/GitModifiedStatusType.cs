namespace GitOut.Features.Git
{
    public enum GitModifiedStatusType
    {
        None,
        Unmodified,
        Modified,
        Added,
        Deleted,
        Renamed,
        Copied,
        UpdatedButUnmerged,
        Untracked
    }
}
