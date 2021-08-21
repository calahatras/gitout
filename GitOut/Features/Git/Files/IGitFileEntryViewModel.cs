using GitOut.Features.IO;

namespace GitOut.Features.Git.Files
{
    public interface IGitFileEntryViewModel
    {
        RelativeDirectoryPath Path { get; }
        FileName FileName { get; }
        string IconResourceKey { get; }
    }
}
