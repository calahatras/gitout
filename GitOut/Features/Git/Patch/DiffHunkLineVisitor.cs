using System;
using System.Collections.Generic;
using System.Linq;
using GitOut.Features.Git.Diff;

namespace GitOut.Features.Git.Patch;

public class DiffHunkLineVisitor : IHunkLineVisitor
{
    private readonly PatchMode mode;
    private readonly IList<HunkLine> diffContexts;
    private readonly int endOffset;
    private int currentOffset;

    public DiffHunkLineVisitor(
        PatchMode mode,
        IEnumerable<HunkLine> diffContexts,
        int startOffset,
        int endOffset
    )
    {
        this.mode = mode;
        this.diffContexts = diffContexts.ToList();
        this.endOffset = endOffset;
        currentOffset = startOffset;
    }

    public bool IsDone => currentOffset >= endOffset;
    public HunkLine Current => diffContexts[currentOffset];

    public HunkLine FindPrepositionHunk()
    {
        if (diffContexts[currentOffset].Type == DiffLineType.Header)
        {
            // user selected a header line; increment offset so that we actually get index from header line and not previous hunk
            ++currentOffset;
        }
        for (int startOffset = currentOffset - 1; startOffset >= 0; --startOffset)
        {
            HunkLine line = diffContexts[startOffset];
            if (
                line.Type == DiffLineType.Header
                || line.Type == DiffLineType.None
                || (line.Type == DiffLineType.Removed && mode == PatchMode.AddIndex)
                || (
                    line.Type == DiffLineType.Added
                    && (
                        mode == PatchMode.ResetIndex
                        || mode == PatchMode.ResetWorkspace
                        || mode == PatchMode.AddWorkspace
                    )
                )
            )
            {
                return line;
            }
        }
        throw new InvalidOperationException("Could not find start hunk");
    }

    public IEnumerable<HunkLine> TraverseSelectionHunks()
    {
        for (; currentOffset <= endOffset; ++currentOffset)
        {
            HunkLine line = diffContexts[currentOffset];
            if (line.Type == DiffLineType.Header)
            {
                ++currentOffset;
                yield break;
            }
            yield return line;
        }
    }

    public HunkLine? FindPostpositionHunk()
    {
        for (; currentOffset < diffContexts.Count; ++currentOffset)
        {
            HunkLine line = diffContexts[currentOffset];
            if (
                line.Type == DiffLineType.None
                || line.Type == DiffLineType.Control
                || (line.Type == DiffLineType.Removed && mode == PatchMode.AddIndex)
                || (
                    line.Type == DiffLineType.Added
                    && (
                        mode == PatchMode.ResetIndex
                        || mode == PatchMode.ResetWorkspace
                        || mode == PatchMode.AddWorkspace
                    )
                )
            )
            {
                return line;
            }
        }
        return null;
    }
}
