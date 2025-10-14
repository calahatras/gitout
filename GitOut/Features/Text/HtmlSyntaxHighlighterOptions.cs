using System.Windows.Media;

namespace GitOut.Features.Text;

public static class HtmlSyntaxHighlighterOptions
{
    public static readonly Brush ElementForegroundColor = new SolidColorBrush(Color.FromRgb(86, 156, 214));
    public static readonly Brush AttributeForegroundColor = new SolidColorBrush(Color.FromRgb(216, 160, 223));
    public static readonly Brush StringForegroundColor = Brushes.PeachPuff;
    public static readonly Brush BoundStringForegroundColor = new SolidColorBrush(Color.FromRgb(255, 223, 127)); // Yellowish color
}
