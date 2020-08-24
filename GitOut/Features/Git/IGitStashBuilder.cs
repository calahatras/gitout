namespace GitOut.Features.Git
{
    public interface IGitStashBuilder
    {
        string Name { get; }

        IGitStashBuilder UseParent(string parentId);
        GitStash Build();
    }
}
