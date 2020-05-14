using System;

namespace GitOut.Features.Git
{
    public class HunkLine
    {
        private HunkLine(DiffLineType type, string line, int? fromIndex, int? toIndex)
        {
            switch (type)
            {
                case DiffLineType.Header:
                    if (!line.StartsWith("@"))
                    {
                        throw new InvalidOperationException($"Invalid start of line for header {line}");
                    }
                    StrippedLine = line.Substring(1);
                    break;
                case DiffLineType.Added:
                    if (!line.StartsWith("+"))
                    {
                        throw new InvalidOperationException($"Invalid start of line for added {line}");
                    }
                    StrippedLine = line.Substring(1);
                    break;
                case DiffLineType.Removed:
                    if (!line.StartsWith("-"))
                    {
                        throw new InvalidOperationException($"Invalid start of line for removed {line}");
                    }
                    StrippedLine = line.Substring(1);
                    break;
                case DiffLineType.None:
                default:
                    StrippedLine = line.Length > 0 ? line.Substring(1) : line;
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

        public static HunkLine AsHead(string line) => new HunkLine(DiffLineType.Header, line, null, null);
        public static HunkLine AsLine(string line, int fromIndex, int toIndex) => new HunkLine(DiffLineType.None, line, fromIndex, toIndex);
        public static HunkLine AsAdded(string line, int toIndex) => new HunkLine(DiffLineType.Added, line, null, toIndex);
        public static HunkLine AsRemoved(string line, int fromIndex) => new HunkLine(DiffLineType.Removed, line, fromIndex, null);
    }
}
