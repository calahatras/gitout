using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GitOut.Features.Git.Diff;
using GitOut.Features.IO;
using GitOut.Features.Text;

namespace GitOut.Features.Git.Patch
{
    public class GitPatch
    {
        private GitPatch(StringBuilder writer, PatchMode mode)
        {
            Writer = writer;
            Mode = mode;
        }

        public StringBuilder Writer { get; }
        public PatchMode Mode { get; }

        public override string ToString() => Writer.ToString();

        public static GitPatch Create(
            PatchMode mode,
            RelativeDirectoryPath path,
            GitStatusChangeType type,
            IHunkLineVisitor visitor
        ) => Create(mode, path, type, visitor, new PassThroughTransform());

        public static GitPatch Create(
            PatchMode mode,
            RelativeDirectoryPath path,
            GitStatusChangeType type,
            IHunkLineVisitor visitor,
            ITextTransform transform
        )
        {
            var builder = new GitPatchBuilder();
            builder.SetMode(mode);

            builder.CreateHeader(path, type);
            var lines = new List<PatchLine>();
            HunkLine preline = visitor.FindPrepositionHunk();
            int fromRangeIndex;
            if (mode == PatchMode.AddIndex || mode == PatchMode.AddWorkspace)
            {
                switch (preline.Type)
                {
                    case DiffLineType.Header:
                        fromRangeIndex = preline.FromIndex!.Value;
                        break;
                    case DiffLineType.Removed:
                    case DiffLineType.None:
                        fromRangeIndex = preline.FromIndex!.Value;
                        lines.Add(PatchLine.CreateLine(DiffLineType.None, preline.StrippedLine));
                        break;
                    default:
                        throw new InvalidOperationException("Preline is not of expected type");
                }
            }
            else if (mode == PatchMode.ResetIndex || mode == PatchMode.ResetWorkspace)
            {
                switch (preline.Type)
                {
                    case DiffLineType.Header:
                        fromRangeIndex = preline.FromIndex!.Value;
                        break;
                    case DiffLineType.None:
                        fromRangeIndex = preline.FromIndex!.Value;
                        lines.Add(PatchLine.CreateLine(DiffLineType.None, preline.StrippedLine));
                        break;
                    case DiffLineType.Added:
                        fromRangeIndex = preline.ToIndex!.Value;
                        lines.Add(PatchLine.CreateLine(DiffLineType.None, preline.StrippedLine));
                        break;
                    default:
                        throw new InvalidOperationException("Preline is not of expected type");
                }
            }
            else
            {
                throw new ArgumentException($"Unrecognized PatchMode {mode}", nameof(mode));
            }
            do
            {
                lines.AddRange(visitor
                    .TraverseSelectionHunks()
                    .Select(line => PatchLine.CreateLine(
                        line.Type,
                        line.Type == DiffLineType.None || line.Type == DiffLineType.Removed
                            ? line.StrippedLine
                            : transform.Transform(line.StrippedLine)
                    )));
                if (visitor.IsDone)
                {
                    if (lines[^1].Type != DiffLineType.None)
                    {
                        HunkLine? postline = visitor.FindPostpositionHunk();
                        if (!(postline is null))
                        {
                            if (postline.Type == DiffLineType.Control)
                            {
                                lines.Add(PatchLine.CreateLine(DiffLineType.Control, postline.StrippedLine));
                            }
                            else if (postline.Type == DiffLineType.None
                                || ((mode == PatchMode.ResetIndex || mode == PatchMode.ResetWorkspace || mode == PatchMode.AddWorkspace) && postline.Type == DiffLineType.Added)
                                || (mode == PatchMode.AddIndex && postline.Type == DiffLineType.Removed))
                            {
                                lines.Add(PatchLine.CreateLine(DiffLineType.None, postline.StrippedLine));
                            }
                        }
                    }
                }
                builder.CreateHunk(fromRangeIndex, lines);
                lines.Clear();
                if (!visitor.IsDone)
                {
                    fromRangeIndex = visitor.Current.FromIndex!.Value;
                }
            } while (!visitor.IsDone);

            return builder.Build();
        }

        private class GitPatchBuilder
        {
            private readonly StringBuilder patchBuilder = new StringBuilder();
            private int hunkOffset;
            private PatchMode mode;

            public GitPatch Build()
            {
                if (mode == PatchMode.None)
                {
                    throw new InvalidOperationException("Must set patch mode before building");
                }
                return new GitPatch(patchBuilder, mode);
            }

            public GitPatchBuilder CreateHunk(int fromFileRange, IEnumerable<PatchLine> lines)
            {
                if (mode == PatchMode.None)
                {
                    throw new InvalidOperationException("Must set patch mode before hunking");
                }
                var edits = lines.ToList();
                if (edits.Count == 0)
                {
                    return this;
                }
                if (fromFileRange > 1 && edits[0].Type != DiffLineType.None)
                {
                    throw new InvalidOperationException("Cannot create patch hunk if first line is not unmodified");
                }
                int uneditedLines = lines.Count(l => l.Type == DiffLineType.None);
                int addedLines = lines.Count(l => l.Type == DiffLineType.Added);
                int removedLines = lines.Count(l => l.Type == DiffLineType.Removed);

                if (addedLines == 0 && removedLines == 0)
                {
                    // user most likely selected unmodified lines or header, ignore
                    return this;
                }
                if (mode == PatchMode.AddIndex || mode == PatchMode.AddWorkspace)
                {
                    patchBuilder.AppendLine($"@@ -{fromFileRange},{uneditedLines + removedLines} +{fromFileRange + hunkOffset},{uneditedLines + addedLines} @@");

                    int deltaLines = addedLines - removedLines;
                    hunkOffset += deltaLines;
                    for (int i = 0; i < edits.Count; ++i)
                    {
                        PatchLine line = edits[i];
                        if (line.Type == DiffLineType.Control)
                        {
                            continue;
                        }
                        char editType = line.Type switch
                        {
                            DiffLineType.None => ' ',
                            DiffLineType.Added => '+',
                            DiffLineType.Removed => '-',
                            _ => throw new ArgumentOutOfRangeException($"Invalid patch type for hunk {line.Type}"),
                        };
                        patchBuilder.Append(editType);
                        patchBuilder.Append(line.Line);
                        if (edits.Count <= i + 1 || edits[i + 1].Type != DiffLineType.Control)
                        {
                            patchBuilder.Append('\n');
                        }
                    }
                }
                else
                {
                    patchBuilder.AppendLine($"@@ -{fromFileRange},{uneditedLines + addedLines} +{fromFileRange + hunkOffset},{uneditedLines + removedLines} @@");

                    int deltaLines = removedLines - addedLines;
                    hunkOffset += deltaLines;
                    for (int i = 0; i < edits.Count; ++i)
                    {
                        PatchLine line = edits[i];
                        if (line.Type == DiffLineType.Control)
                        {
                            continue;
                        }
                        char editType = line.Type switch
                        {
                            DiffLineType.None => ' ',
                            DiffLineType.Added => '-',
                            DiffLineType.Removed => '+',
                            _ => throw new ArgumentOutOfRangeException($"Invalid patch type for hunk {line.Type}"),
                        };
                        patchBuilder.Append(editType);
                        patchBuilder.Append(line.Line);
                        if (edits.Count <= i + 1 || edits[i + 1].Type != DiffLineType.Control)
                        {
                            patchBuilder.Append('\n');
                        }
                    }
                }
                return this;
            }

            public GitPatchBuilder CreateHeader(RelativeDirectoryPath path, GitStatusChangeType type)
            {
                patchBuilder.AppendLine($"diff --git a/{path} b/{path}");
                switch (type)
                {
                    case GitStatusChangeType.Ordinary:
                        patchBuilder.AppendLine($"--- a/{path}");
                        break;
                    case GitStatusChangeType.Untracked:
                        patchBuilder.AppendLine("new file mode 10644");
                        patchBuilder.AppendLine("--- /dev/null");
                        break;
                    case GitStatusChangeType.RenamedOrCopied:
                    case GitStatusChangeType.Unmerged:
                    case GitStatusChangeType.None:
                    case GitStatusChangeType.Ignored:
                        throw new InvalidOperationException($"Cannot create diff for change type {type}");
                }
                patchBuilder.AppendLine($"+++ b/{path}");
                return this;
            }

            public GitPatchBuilder SetMode(PatchMode mode)
            {
                if (patchBuilder.Length > 0)
                {
                    throw new InvalidOperationException("Cannot set mode after changes have been written");
                }
                this.mode = mode;
                return this;
            }
        }

        private class PassThroughTransform : ITextTransform
        {
            public string Transform(string input) => input;
        }
    }
}
