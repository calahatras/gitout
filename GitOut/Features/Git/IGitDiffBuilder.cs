namespace GitOut.Features.Git
{
    public interface IGitDiffBuilder
    {
        GitDiffResult Build();
        void Feed(string line);
    }
}
