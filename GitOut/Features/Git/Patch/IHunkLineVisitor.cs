using System.Collections.Generic;
using GitOut.Features.Git.Diff;

namespace GitOut.Features.Git.Patch
{
    public interface IHunkLineVisitor
    {
        bool IsDone { get; }
        HunkLine Current { get; }

        HunkLine FindPrepositionHunk();
        IEnumerable<HunkLine> TraverseSelectionHunks();
        HunkLine? FindPostpositionHunk();
    }
}
