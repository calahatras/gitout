using System.Windows.Media;

namespace GitOut.Features.Text
{
    public static class CSharpSyntaxHighlighterOptions
    {
        public static readonly Brush KeywordForegroundColor = new SolidColorBrush(Color.FromRgb(86, 156, 214));
        public static readonly Brush ControlKeywordForegroundColor = new SolidColorBrush(Color.FromRgb(216, 160, 223));
        public static readonly Brush StringForegroundColor = Brushes.PeachPuff;
    }
}
