using System;

namespace GitOut.Features.Git.Diff
{
    public class HunkLine
    {
        public static readonly HunkLine Empty = new HunkLine(DiffLineType.None, string.Empty, null, null);

        private HunkLine(DiffLineType type, string line, int? fromIndex, int? toIndex)
        {
            switch (type)
            {
                case DiffLineType.Header:
                    if (line[0] != '@')
                    {
                        throw new InvalidOperationException($"Invalid start of line for header {line}");
                    }
                    StrippedLine = line[1..];
                    break;
                case DiffLineType.Added:
                    if (line[0] != '+')
                    {
                        throw new InvalidOperationException($"Invalid start of line for added {line}");
                    }
                    StrippedLine = line[1..];
                    break;
                case DiffLineType.Removed:
                    if (line[0] != '-')
                    {
                        throw new InvalidOperationException($"Invalid start of line for removed {line}");
                    }
                    StrippedLine = line[1..];
                    break;
                case DiffLineType.Control:
                    if (line[0] != '\\')
                    {
                        throw new InvalidOperationException($"Invalid start of line for control {line}");
                    }
                    StrippedLine = line;
                    break;
                case DiffLineType.None:
                default:
                    if (line.Length > 0 && line[0] != ' ')
                    {
                        throw new InvalidOperationException($"Invalid start of line for ordinal {line}");
                    }
                    StrippedLine = line.Length > 0 ? line[1..] : line;
                    break;
            }
            Type = type;
            FromIndex = fromIndex;
            ToIndex = toIndex;
        }

        public int? FromIndex { get; }
        public int? ToIndex { get; }
        public string StrippedLine { get; }
        public DiffLineType Type { get; }

        public static HunkLine AsHead(string line, int fromIndex, int toIndex) => new HunkLine(DiffLineType.Header, line, fromIndex, toIndex);
        public static HunkLine AsLine(string line, int fromIndex, int toIndex) => new HunkLine(DiffLineType.None, line, fromIndex, toIndex);
        public static HunkLine AsControl(string line, int fromIndex, int toIndex) => new HunkLine(DiffLineType.Control, line, fromIndex, toIndex);
        public static HunkLine AsAdded(string line, int toIndex) => new HunkLine(DiffLineType.Added, line, null, toIndex);
        public static HunkLine AsRemoved(string line, int fromIndex) => new HunkLine(DiffLineType.Removed, line, fromIndex, null);
    }
}
