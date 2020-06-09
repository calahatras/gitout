namespace GitOut.Features.Git.Stage
{
    public interface IPatchLineTransformBuilder
    {
        PatchLineTransform Build();
        IPatchLineTransformBuilder TrimLines();
        IPatchLineTransformBuilder ConvertTabsToSpaces();
    }
}
