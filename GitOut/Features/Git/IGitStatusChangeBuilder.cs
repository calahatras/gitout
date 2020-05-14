namespace GitOut.Features.Git
{
    public interface IGitStatusChangeBuilder
    {
        GitStatusChangeType Type { get; }
        IGitStatusChangeBuilder MergedFrom(string path);
        GitStatusChange Build();
    }
}
