using System.Windows;

namespace GitOut.Features.Navigation;

public class NavigationWindowOptions
{
    public const string SectionKey = "window";

    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? Top { get; set; }
    public int? Left { get; set; }

    public static NavigationWindowOptions FromPosition(Point location, Size windowSize) =>
        new()
        {
            Width = (int)windowSize.Width,
            Height = (int)windowSize.Height,
            Left = (int)location.X,
            Top = (int)location.Y,
        };
}
