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
        private readonly StatusChangeLocation location;
        private readonly List<(Run, HunkLine)> diffContexts;

        private DiffViewModel(
            GitStatusChangeType changeType,
            string path,
            StatusChangeLocation location,
            FlowDocument document,
            IEnumerable<LineNumberViewModel> lineNumbers,
            List<(Run, HunkLine)> diffContexts
        )
        {
            this.path = path;
            this.location = location;
            this.diffContexts = diffContexts;
            Document = document;
            LineNumbers = lineNumbers;
            this.changeType = changeType;
        }

        public FlowDocument Document { get; }
        public IEnumerable<LineNumberViewModel> LineNumbers { get; }

        public GitPatch CreateResetPatch(TextSelection selection)
            => CreatePatch(selection, false, location);

        public GitPatch CreateUndoPatch(TextSelection selection)
            => CreatePatch(selection, true, StatusChangeLocation.Workspace);

        public GitPatch CreateAddPatch(TextSelection selection)
        {
            if (location == StatusChangeLocation.Index)
            {
                throw new InvalidOperationException("Cannot stage from index");
            }
            return CreatePatch(selection, true, StatusChangeLocation.Index);
        }

        public static DiffViewModel ParseDiff(GitDiffResult result, double pixelsPerDip, Brush dividerBrush, Brush headerForeground)
        {
            var document = new FlowDocument
            {
                FontFamily = new FontFamily("Consolas sans-serif"),
                FontSize = 12,
                PagePadding = new Thickness(0)
            };
            var lineNumbers = new List<LineNumberViewModel>();
            double maxWidth = 0;
            var diffContexts = new List<(Run, HunkLine)>();
            foreach (GitDiffHunk hunk in result.Hunks)
            {
                var section = new Section
                {
                    BorderBrush = dividerBrush,
                    BorderThickness = new Thickness(0, 0, 0, 1)
                };
                foreach (HunkLine text in hunk.Lines)
                {
                    lineNumbers.Add(new LineNumberViewModel(text.FromIndex, text.ToIndex));
                    Paragraph p = text.Type switch
                    {
                        DiffLineType.Added => CreateAddedParagraph(text.StrippedLine),
                        DiffLineType.Removed => CreateRemovedParagraph(text.StrippedLine),
                        DiffLineType.Header => CreateHeaderParagraph(text.StrippedLine, headerForeground),
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
            return new DiffViewModel(GitStatusChangeType.Ordinary, result.Change.Path, result.Options.Cached ? StatusChangeLocation.Index : StatusChangeLocation.Workspace, document, lineNumbers, diffContexts);

            static Paragraph CreateDefaultParagraph(string text) =>
                new Paragraph(new Run(text))
                {
                    Margin = new Thickness(0)
                };

            static Paragraph CreateHeaderParagraph(string text, Brush foreground) => new Paragraph(new Run(text))
            {
                Background = Brushes.Transparent,
                Foreground = foreground,
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

        public static DiffViewModel? ParseFileContent(GitStatusChange origin, string[] result, double pixelsPerDip)
        {
            var document = new FlowDocument
            {
                FontFamily = new FontFamily("Consolas sans-serif"),
                FontSize = 12,
                PagePadding = new Thickness(0)
            };
            var lineNumbers = new List<LineNumberViewModel>();
            double maxWidth = 0;
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
            return new DiffViewModel(GitStatusChangeType.Untracked, origin.Path, StatusChangeLocation.Workspace, document, lineNumbers, diffContexts);
        }

        private GitPatch CreatePatch(TextSelection selection, bool keep, StatusChangeLocation applyLocation)
        {
            IGitPatchBuilder builder = GitPatch.Builder();
            if (applyLocation == StatusChangeLocation.Index)
            {
                builder.SetMode(keep ? PatchMode.AddIndex : PatchMode.ResetIndex);
            }
            else
            {
                builder.SetMode(keep ? PatchMode.AddWorkspace : PatchMode.ResetWorkspace);
            }
            builder.CreateHeader(path, changeType);

            TextPointer start = selection.Start;
            TextPointer end = selection.End;

            int contextOffset = diffContexts.FindIndex(context => context.Item1 == start.Parent);
            if (contextOffset == -1)
            {
                throw new InvalidOperationException("Invalid state, could not find matching paragraph");
            }

            var lines = new List<PatchLine>();
            // find closest unedited line from selected paragraph, we need that text and line number
            int fromRangeIndex = 0, startOffset;
            if (diffContexts[contextOffset].Item2.Type == DiffLineType.Header)
            {
                // user selected a header line; increment offset so that we actually get index from header line and not previous hunk
                ++contextOffset;
            }
            for (startOffset = contextOffset - 1; startOffset >= 0; --startOffset)
            {
                HunkLine line = diffContexts[startOffset].Item2;
                if (line.Type == DiffLineType.Header)
                {
                    // since a header is only found if line is on first line number, set fromRange to 0
                    fromRangeIndex = line.FromIndex!.Value;
                    break;
                }
                if (line.Type == DiffLineType.None || line.Type == DiffLineType.Removed)
                {
                    lines.Insert(0, PatchLine.CreateLine(DiffLineType.None, line.StrippedLine));
                    fromRangeIndex = line.FromIndex!.Value;
                    break;
                }
                if (options.Mode == PatchMode.ResetIndex && line.Type == DiffLineType.Added)
                {
                    lines.Insert(0, PatchLine.CreateLine(DiffLineType.None, line.StrippedLine));
                    fromRangeIndex = line.ToIndex!.Value;
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
                        lines.Add(PatchLine.CreateLine(DiffLineType.None, options.TextTransform.Transform(line.StrippedLine)));
                        break;
                    }
                    if (options.Mode == PatchMode.ResetIndex && line.Type == DiffLineType.Added)
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
    }
}
