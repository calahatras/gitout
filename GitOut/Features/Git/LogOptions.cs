namespace GitOut.Features.Git;

public record LogOptions
{
    public bool IncludeRemotes { get; init; }
    public bool IncludeStashes { get; init; }
}
