namespace GitOut.Features.Git.Hooks;

public class GitHooksOptions
{
    public const string SectionKey = "hooks";

    public string PreferredShellPath { get; set; } = string.Empty;
}
