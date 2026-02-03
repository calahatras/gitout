using System;
using System.Windows.Media;

namespace GitOut.Features.Git.Log;

public class GitTreeNode
{
    private const string LinesDoNotMeetError = "lines do not meet";

    private GitTreeNode(Line? top, Line? bottom, Color color, bool commit, LineType lineType)
    {
        Top = top;
        Bottom = bottom;
        Color = color;
        IsCommit = commit;
        if (bottom is not null)
        {
            BottomLineType = lineType;
        }
        if (top is not null)
        {
            TopLineType = lineType;
        }
    }

    public bool IsCommit { get; }
    public LineType TopLineType { get; }
    public LineType BottomLineType { get; private set; }
    public Color Color { get; }

    public Line? Top { get; }
    public Line? Bottom { get; private set; }

    public void AttachBottom(Line value, LineType type)
    {
        if (value is Line bottom && Top is Line top && top.Down != bottom.Up)
        {
            throw new ArgumentException(LinesDoNotMeetError, nameof(value));
        }

        Bottom = value;
        BottomLineType = type;
    }

    public static GitTreeNode WithTopLine(Line top, Color color, bool commit, LineType lineType) =>
        new(top, null, color, commit, lineType);

    public static GitTreeNode WithBottomLine(
        Line bottom,
        Color color,
        bool commit,
        LineType lineType
    ) => new(null, bottom, color, commit, lineType);
}
