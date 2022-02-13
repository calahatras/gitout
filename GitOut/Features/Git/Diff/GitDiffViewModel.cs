using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using GitOut.Features.Text;

namespace GitOut.Features.Git.Diff
{
    public class GitDiffViewModel : IDocumentSelectionViewModel
    {
        private const string FontFamilyName = "Consolas sans-serif";

        private GitDiffViewModel(
            FlowDocument document,
            IEnumerable<LineNumberViewModel> lineNumbers,
            List<(Paragraph, HunkLine)> diffContexts
        )
        {
            DiffContexts = diffContexts;
            Document = document;
            LineNumbers = lineNumbers;
        }

        public FlowDocument Document { get; }
        public IEnumerable<LineNumberViewModel> LineNumbers { get; }
        public TextRange? Selection { get; set; }

        public IReadOnlyCollection<(Paragraph, HunkLine)> DiffContexts { get; }

        public static GitDiffViewModel ParseDiff(IEnumerable<GitDiffHunk> result, DiffDisplayOptions display)
        {
            ISyntaxHighlighter highlighter = new CSharpSyntaxHighlighter();

            var document = new FlowDocument
            {
                FontFamily = new FontFamily(FontFamilyName),
                FontSize = 12,
                PagePadding = new Thickness(0),
            };
            var lineNumbers = new List<LineNumberViewModel>();
            double maxWidth = 0;
            var diffContexts = new List<(Paragraph, HunkLine)>();
            foreach (GitDiffHunk hunk in result)
            {
                var section = new Section
                {
                    BorderBrush = display.DividerBrush,
                    BorderThickness = new Thickness(0, 0, 0, 1)
                };
                Paragraph header = CreateHeaderParagraph(hunk.Header.StrippedLine, display.HeaderForeground);
                section.Blocks.Add(header);
                diffContexts.Add((header, hunk.Header));
                double width = CalculateLineWidth(hunk.Header.StrippedLine);
                maxWidth = Math.Max(maxWidth, width);
                lineNumbers.Add(new LineNumberViewModel(null, null));
                IEnumerable<HunkLine> lines = hunk.Lines;
                IEnumerable<Paragraph> highlighted = highlighter.Highlight(lines.Select(line => line.StrippedLine), new DiffLineHighlighter(lines));
                foreach ((Paragraph line, HunkLine text) in highlighted.Zip(lines))
                {
                    lineNumbers.Add(new LineNumberViewModel(text.FromIndex, text.ToIndex));
                    width = CalculateLineWidth(text.StrippedLine);
                    maxWidth = Math.Max(maxWidth, width);
                    foreach (Run subitem in line.Inlines.OfType<Run>().ToList())
                    {
                        subitem.Text = display.Transform.Transform(subitem.Text);
                    }
                    section.Blocks.Add(line);
                    diffContexts.Add((line, text));
                }
                document.Blocks.Add(section);
            }
            document.PageWidth = maxWidth + 20;
            return new GitDiffViewModel(document, lineNumbers, diffContexts);

            double CalculateLineWidth(string text) =>
                new FormattedText(
                display.Transform.Transform(text),
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamilyName),
                12,
                Brushes.White,
                display.PixelsPerDip
            ).Width;

            static Paragraph CreateHeaderParagraph(string text, Brush foreground) => new(new Run(text))
            {
                Background = Brushes.Transparent,
                Foreground = foreground,
                FontSize = 11,
                Margin = new Thickness(0)
            };
        }

        private class DiffLineHighlighter : ILineDecorator
        {
            private static readonly Brush RemovedLineBackground = new SolidColorBrush(Color.FromArgb(128, 200, 90, 90));
            private static readonly Brush AddedLineBackground = new SolidColorBrush(Color.FromArgb(90, 90, 200, 90));

            private readonly IReadOnlyList<HunkLine> lines;

            public DiffLineHighlighter(IEnumerable<HunkLine> lines) => this.lines = lines.ToList().AsReadOnly();

            public void Decorate(TextElement paragraph, int lineNumber) => paragraph.Background = lines[lineNumber].Type switch
            {
                DiffLineType.Added => AddedLineBackground,
                DiffLineType.Removed => RemovedLineBackground,
                _ => Brushes.Transparent
            };
        }
    }
}
