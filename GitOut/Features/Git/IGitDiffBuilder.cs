namespace GitOut.Features.Git
{
    public interface IGitDiffBuilder
    {
        GitDiffResult Build(DiffOptions options);
        void Feed(string line);
    }
}
