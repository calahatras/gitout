using System.Windows.Media;
using GitOut.Features.Text;

namespace GitOut.Features.Git.Diff
{
    public class DiffDisplayOptions
    {
        public DiffDisplayOptions(
            double pixelsPerDip,
            Brush dividerBrush,
            Brush headerForeground,
            ITextTransform transform
        )
        {
            PixelsPerDip = pixelsPerDip;
            DividerBrush = dividerBrush;
            HeaderForeground = headerForeground;
            Transform = transform;
        }

        public DiffDisplayOptions(double pixelsPerDip, Brush dividerBrush, Brush headerForeground)
            : this(pixelsPerDip, dividerBrush, headerForeground, new PassThroughTransform()) { }

        public double PixelsPerDip { get; }
        public Brush DividerBrush { get; }
        public Brush HeaderForeground { get; }
        public ITextTransform Transform { get; }

        private class PassThroughTransform : ITextTransform
        {
            public string Transform(string input) => input;
        }
    }
}
