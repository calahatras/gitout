namespace GitOut.Features.Settings;

public class WorktreeOptions
{
    public const string SectionKey = "Worktrees";

    public string DefaultPrefixPath { get; set; } = ".gitout/worktrees/<name>";
}
