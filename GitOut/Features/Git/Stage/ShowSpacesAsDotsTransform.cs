namespace GitOut.Features.Git.Stage
{
    public class ShowSpacesAsDotsTransform : ITextTransform
    {
        public string Transform(string input) => input.Replace(' ', '\u00B7');
    }
}
