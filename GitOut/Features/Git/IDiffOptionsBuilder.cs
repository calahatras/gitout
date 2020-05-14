namespace GitOut.Features.Git
{
    public interface IDiffOptionsBuilder
    {
        DiffOptions Build();
        IDiffOptionsBuilder IgnoreAllSpace();
        IDiffOptionsBuilder Cached();
    }
}
