namespace GitOut.Features.Git;

public interface IDiffOptionsBuilder
{
    DiffOptions Build();
    IDiffOptionsBuilder Cached();
    IDiffOptionsBuilder IgnoreAllSpace();
    IDiffOptionsBuilder Recursive();
    IDiffOptionsBuilder ContextLines(int lines);
}
