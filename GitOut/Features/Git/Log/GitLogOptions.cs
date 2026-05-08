namespace GitOut.Features.Git.Log;

public sealed record GitLogOptions
{
    public const string SectionKey = "log";

    public LogRevisionViewMode DefaultSingleRevisionViewMode { get; set; } =
        LogRevisionViewMode.DiffInline;

    public LogRevisionViewMode DefaultMultiRevisionViewMode { get; set; } =
        LogRevisionViewMode.DiffInline;
}
