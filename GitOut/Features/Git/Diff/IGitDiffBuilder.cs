using System.IO;

namespace GitOut.Features.Git.Diff
{
    public interface IGitDiffBuilder
    {
        bool IsBinaryFile { get; }

        GitDiffResult Build();
        IGitDiffBuilder Feed(Stream stream, GitStatusChangeType type = GitStatusChangeType.None);
        IGitDiffBuilder Feed(string line);
    }
}
