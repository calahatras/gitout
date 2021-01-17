namespace GitOut.Features.Git
{
    public interface IDiffOptionsBuilder
    {
        DiffOptions Build();
        IDiffOptionsBuilder Cached();
        IDiffOptionsBuilder IgnoreAllSpace();
        IDiffOptionsBuilder Recursive();
    }
}
