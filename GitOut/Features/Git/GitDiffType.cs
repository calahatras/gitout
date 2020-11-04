namespace GitOut.Features.Git
{
    public enum GitDiffType
    {
        None,
        InPlaceEdit, // M
        CopyEdit,    // C
        RenameEdit,  // R
        Create,      // A
        Delete,      // D
        Unmerged,    // U
    }
}
