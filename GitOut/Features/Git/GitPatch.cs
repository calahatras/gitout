using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitOut.Features.Git
{
    public class GitPatch
    {
        private GitPatch(StringBuilder writer) => Writer = writer;

        public StringBuilder Writer { get; }

        public static IGitPatchBuilder Builder() => new GitPatchBuilder();

        private class GitPatchBuilder : IGitPatchBuilder
        {
            private readonly StringBuilder patchBuilder = new StringBuilder();
            private int hunkOffset;

            public GitPatch Build()
            {
                patchBuilder.Append('\n');
                return new GitPatch(patchBuilder);
            }

            public IGitPatchBuilder CreateHunk(int fromFileRange, IEnumerable<PatchLine> lines)
            {
                var edits = lines.ToList();
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
                patchBuilder.AppendLine($"@@ -{fromFileRange},{uneditedLines + removedLines} +{fromFileRange + hunkOffset},{uneditedLines + addedLines} @@");

                int deltaLines = addedLines - removedLines;
                hunkOffset += deltaLines;
                foreach (PatchLine l in edits)
                {
                    char editType = l.Type switch
                    {
                        DiffLineType.None => ' ',
                        DiffLineType.Added => '+',
                        DiffLineType.Removed => '-',
                        _ => throw new ArgumentOutOfRangeException($"Invalid patch type for hunk {l.Type}"),
                    };
                    patchBuilder.Append(editType);
                    patchBuilder.Append(l.Line);
                    patchBuilder.Append('\n');
                }
                return this;
            }

            public IGitPatchBuilder CreateHeader(string path, GitStatusChangeType type)
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
        }
    }
}
