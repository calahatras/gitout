using System;
using System.Windows.Media;

namespace GitOut.Features.Git.Log
{
    public class GitTreeNode
    {
        private const string LinesDoNotMeetError = "lines do not meet";
        private Line? bottomLayer;

        public GitTreeNode(Line? top, Line? bottom, Color color, bool commit)
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
                    throw new ArgumentException(LinesDoNotMeetError);
                }

                bottomLayer = value;
            }
        }
    }
}
