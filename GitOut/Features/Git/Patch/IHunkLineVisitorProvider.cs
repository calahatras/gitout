namespace GitOut.Features.Git.Patch;

public interface IHunkLineVisitorProvider
{
    IHunkLineVisitor? GetHunkVisitor(PatchMode mode);
}
