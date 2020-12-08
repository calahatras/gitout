using GitOut.Features.Text;

namespace GitOut.Features.Git.Patch
{
    public interface IPatchLineTransformBuilder
    {
        ITextTransform Build();
        IPatchLineTransformBuilder TrimLines();
        IPatchLineTransformBuilder ConvertTabsToSpaces(string replacement);
    }
}
