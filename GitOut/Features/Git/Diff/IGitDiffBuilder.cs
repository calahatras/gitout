namespace GitOut.Features.Git.Diff
{
    public interface IGitDiffBuilder
    {
        GitDiffResult Build(DiffOptions options);
        void Feed(string line);
    }
}
