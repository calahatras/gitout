namespace GitOut.Features.Git.Diff;

public interface IGitDiffFileEntryBuilder
{
    GitDiffType Type { get; }
    GitDiffFileEntry Build(string sourcePath);
    GitDiffFileEntry Build(string sourcePath, string destinationPath);
}
