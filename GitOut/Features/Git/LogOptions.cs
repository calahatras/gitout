namespace GitOut.Features.Git;

public record LogOptions
{
    public bool IncludeRemotes { get; init; }
    public bool IncludeStashes { get; init; }
    public GitOut.Features.IO.RelativeDirectoryPath? Path { get; init; }
    public GitOut.Features.IO.FileName? FileName { get; init; }
}
