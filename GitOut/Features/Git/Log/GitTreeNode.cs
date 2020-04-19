using System;
using System.Windows.Media;

namespace GitOut.Features.Git.Log
{
    public class GitTreeNode
    {
        private const string LinesDoNotMeetError = "lines do not meet";
        private Line? bottomLayer;

        private GitTreeNode(Line? top, Line? bottom, Color color, bool commit)
        {
            Top = top;
            Bottom = bottom;
            Color = color;
            IsCommit = commit;
        }

        public bool IsCommit { get; }

        public Color Color { get; }

        public Line? Top { get; }
        public Line? Bottom
        {
            get => bottomLayer;
            set
            {
                if (value is Line bottom && Top is Line top && top.Down != bottom.Up)
                {
                    throw new ArgumentException(LinesDoNotMeetError, nameof(value));
                }

                bottomLayer = value;
            }
        }

        public static GitTreeNode WithTopLine(Line top, Color color, bool commit) => new GitTreeNode(top, null, color, commit);
        public static GitTreeNode WithBottomLine(Line bottom, Color color, bool commit) => new GitTreeNode(null, bottom, color, commit);

    }
}
