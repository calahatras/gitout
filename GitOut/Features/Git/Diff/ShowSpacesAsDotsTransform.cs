using GitOut.Features.Text;

namespace GitOut.Features.Git.Diff;

public class ShowSpacesAsDotsTransform : ITextTransform
{
    public string Transform(string input) => input.Replace(' ', '\u00B7');
}
