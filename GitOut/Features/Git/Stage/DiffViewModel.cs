using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace GitOut.Features.Git.Stage
{
    public class DiffViewModel
    {
        private static readonly Brush RemovedLineBackground = new SolidColorBrush(Color.FromArgb(128, 200, 90, 90));
        private static readonly Brush AddedLineBackground = new SolidColorBrush(Color.FromArgb(128, 90, 200, 90));

        private readonly GitStatusChangeType changeType;
        private readonly string path;
        private readonly List<(Run, HunkLine)> diffContexts;

        public DiffViewModel(GitStatusChangeType changeType, string path, FlowDocument document, IEnumerable<LineNumberViewModel> lineNumbers, List<(Run, HunkLine)> diffContexts)
        {
            this.path = path;
            this.diffContexts = diffContexts;
            Document = document;
            LineNumbers = lineNumbers;
            this.changeType = changeType;
        }

        public FlowDocument Document { get; }
        public IEnumerable<LineNumberViewModel> LineNumbers { get; }

        public GitPatch CreatePatch(TextSelection selection)
        {
            IGitPatchBuilder builder = GitPatch.Builder();
            builder.CreateHeader(path, changeType);

            TextPointer start = selection.Start;
            TextPointer end = selection.End;

            int contextOffset = diffContexts.FindIndex(context => context.Item1 == start.Parent);
            if (contextOffset == -1)
            {
                throw new InvalidOperationException("Invalid state, could not find matching paragraph");
            }
            // find closest unedited line from selected paragraph, we need that text and line number
            var lines = new List<PatchLine>();
            int fromRangeIndex = 0, startOffset;
            for (startOffset = contextOffset - 1; startOffset >= 0; --startOffset)
            {
                HunkLine line = diffContexts[startOffset].Item2;
                if (line.Type == DiffLineType.Header)
                {
                    // since a header is only found if line is on first line number, set fromRange to 0
                    fromRangeIndex = 0;
                    break;
                }
                if (line.Type == DiffLineType.None || line.Type == DiffLineType.Removed)
                {
                    lines.Insert(0, PatchLine.CreateLine(DiffLineType.None, line.StrippedLine));
                    fromRangeIndex = line.FromIndex!.Value;
                    break;
                }
            }

            // start is prepared, now we need to add text up to selectedEnd
            int endOffset = contextOffset;
            for (; endOffset < diffContexts.Count; ++endOffset)
            {
                (Run run, HunkLine line) = diffContexts[endOffset];
                if (line.Type == DiffLineType.Header)
                {
                    if (lines.Count > 0)
                    {
                        // user crossed hunk boundaries; create a new hunk here, offset toRange with added - removed lines
                        builder.CreateHunk(fromRangeIndex, lines);
                        lines.Clear();
                        (Run nextpara, HunkLine nextline) = diffContexts[endOffset + 1];
                        fromRangeIndex = nextline.FromIndex!.Value;
                    }
                    continue;
                }
                lines.Add(PatchLine.CreateLine(line.Type, line.StrippedLine));
                if (run == end.Parent)
                {
                    break;
                }
            }

            // unless last line is unedited, add next unedited line
            if (lines[^1].Type != DiffLineType.None)
            {
                for (++endOffset; endOffset < diffContexts.Count; ++endOffset)
                {
                    HunkLine line = diffContexts[endOffset].Item2;
                    if (line.Type == DiffLineType.None || line.Type == DiffLineType.Removed)
                    {
                        lines.Add(PatchLine.CreateLine(DiffLineType.None, line.StrippedLine));
                        break;
                    }
                }
            }

            builder.CreateHunk(fromRangeIndex, lines);
            Trace.WriteLine(builder.Build().Writer.ToString());
            return builder.Build();
        }

        public static DiffViewModel ParseDiff(GitDiffResult result)
        {
            var document = new FlowDocument
            {
                FontFamily = new FontFamily("Consolas sans-serif"),
                FontSize = 12,
                PagePadding = new Thickness(0)
            };
            var lineNumbers = new List<LineNumberViewModel>();
            double maxWidth = 0;
            double pixelsPerDip = VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip;
            var diffContexts = new List<(Run, HunkLine)>();
            foreach (GitDiffHunk hunk in result.Hunks)
            {
                var section = new Section
                {
                    BorderBrush = (Brush)Application.Current.Resources["MaterialLightDividers"],
                    BorderThickness = new Thickness(0, 0, 0, 1)
                };
                foreach (HunkLine text in hunk.Lines)
                {
                    lineNumbers.Add(new LineNumberViewModel(text.FromIndex, text.ToIndex));
                    Paragraph p = text.Type switch
                    {
                        DiffLineType.Added => CreateAddedParagraph(text.StrippedLine),
                        DiffLineType.Removed => CreateRemovedParagraph(text.StrippedLine),
                        DiffLineType.Header => CreateHeaderParagraph(text.StrippedLine),
                        _ => CreateDefaultParagraph(text.StrippedLine)
                    };
                    double width = new FormattedText(text.StrippedLine, System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Consolas sans-serif"), 12, Brushes.White, pixelsPerDip).Width;
                    maxWidth = Math.Max(maxWidth, width);
                    section.Blocks.Add(p);

                    diffContexts.Add(((Run)p.Inlines.FirstInline, text));
                }
                document.Blocks.Add(section);
            }
            document.PageWidth = maxWidth + 20;
            return new DiffViewModel(GitStatusChangeType.Ordinary, result.Change.Path, document, lineNumbers, diffContexts);

            static Paragraph CreateDefaultParagraph(string text) =>
                new Paragraph(new Run(text))
                {
                    Margin = new Thickness(0)
                };

            static Paragraph CreateHeaderParagraph(string text) => new Paragraph(new Run(text))
            {
                Background = Brushes.Transparent,
                Foreground = (Brush)Application.Current.Resources["MaterialGray400"],
                FontSize = 11,
                Margin = new Thickness(0)
            };

            static Paragraph CreateRemovedParagraph(string text) => new Paragraph(new Run(text))
            {
                Background = RemovedLineBackground,
                Margin = new Thickness(0)
            };

            static Paragraph CreateAddedParagraph(string text) => new Paragraph(new Run(text))
            {
                Background = AddedLineBackground,
                Margin = new Thickness(0)
            };
        }

        public static DiffViewModel? ParseFileContent(GitStatusChange origin, string[] result)
        {
            var document = new FlowDocument
            {
                FontFamily = new FontFamily("Consolas sans-serif"),
                FontSize = 12,
                PagePadding = new Thickness(0)
            };
            var lineNumbers = new List<LineNumberViewModel>();
            double maxWidth = 0;
            double pixelsPerDip = VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip;
            int lineNumber = 0;
            var content = new Paragraph
            {
                Margin = new Thickness(0)
            };
            var diffContexts = new List<(Run, HunkLine)>();
            for (int i = 0; i < result.Length; i++)
            {
                string line = result[i];
                lineNumbers.Add(new LineNumberViewModel(lineNumber++, null));
                double width = new FormattedText(line, System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Consolas sans-serif"), 12, Brushes.White, pixelsPerDip).Width;
                maxWidth = Math.Max(maxWidth, width);
                var run = new Run(line);
                content.Inlines.Add(run);
                content.Inlines.Add(new LineBreak());
                diffContexts.Add((run, HunkLine.AsAdded("+" + line, i + 1)));
            }

            document.Blocks.Add(content);
            document.PageWidth = maxWidth + 20;
            return new DiffViewModel(GitStatusChangeType.Untracked, origin.Path, document, lineNumbers, diffContexts);
        }
    }
}
