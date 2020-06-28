namespace GitOut.Features.Git.Files
{
    public interface IGitFileEntryViewModel
    {
        string FileName { get; }
        string IconResourceKey { get; }
    }
}
