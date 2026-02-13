namespace GitOut.Features.Git.Stage;

public class GitStageOptions
{
    public const string SectionKey = "staging";

    public bool TrimLineEndings { get; set; }
    public bool ShowSpacesAsDots { get; set; }
    public string TabTransformText { get; set; } = "  ";
}
