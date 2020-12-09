namespace GitOut.Features.Git.Diff
{
    public interface IGitDiffBuilder
    {
        GitDiffResult Build();
        void Feed(string line);
    }
}
