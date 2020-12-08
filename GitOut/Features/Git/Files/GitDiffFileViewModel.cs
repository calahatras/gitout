using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using GitOut.Features.Git.Diff;

namespace GitOut.Features.Git.Files
{
    public class GitDiffFileViewModel : IGitFileEntryViewModel, INotifyPropertyChanged
    {
        private static readonly Brush RemovedLineBackground = new SolidColorBrush(Color.FromArgb(128, 200, 90, 90));
        private static readonly Brush AddedLineBackground = new SolidColorBrush(Color.FromArgb(128, 90, 200, 90));

        private readonly IGitRepository repository;
        private readonly GitDiffFileEntry file;
        private FlowDocument? document;

        private GitDiffFileViewModel(IGitRepository repository, GitDiffFileEntry file)
        {
            if (file.FileType != GitFileType.Blob)
            {
                throw new ArgumentException($"Invalid file type for directory {file.Type}", nameof(file));
            }
            this.repository = repository;
            this.file = file;
        }

        public string FileName => file.SourceFileName.ToString();
        public string IconResourceKey => "File";
        // note: this viewmodel is used in tree view and as such requires IsExpanded property
        public bool IsExpanded { get; set; }

        public FlowDocument? Document
        {
            get
            {
                if (document == null)
                {
                    _ = RefreshDiffAsync();
                }
                return document;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public static GitDiffFileViewModel Wrap(IGitRepository repository, GitDiffFileEntry file) => new GitDiffFileViewModel(repository, file);

        private async Task RefreshDiffAsync()
        {
            double pixelsPerDip = VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip;
            var dividerBrush = (Brush)Application.Current.Resources["MaterialLightDividers"];
            var headerForeground = (Brush)Application.Current.Resources["MaterialGray400"];

            var content = new Paragraph
            {
                Margin = new Thickness(0)
            };
            var document = new FlowDocument
            {
                FontFamily = new FontFamily("Consolas sans-serif"),
                FontSize = 12,
                PagePadding = new Thickness(0)
            };
            var lineNumbers = new List<LineNumberViewModel>();
            double maxWidth = 0;
            var diffContexts = new List<(Run, HunkLine)>();

            if (file.SourceId.IsEmpty)
            {
                string[] lines = await repository.GetFileContentsAsync(file.DestinationId);
                var section = new Section
                {
                    BorderBrush = dividerBrush,
                    BorderThickness = new Thickness(0, 0, 0, 1)
                };
                foreach (HunkLine text in lines.Select((line, index) => HunkLine.AsAdded($"+{line}", index)))
                {
                    lineNumbers.Add(new LineNumberViewModel(text.FromIndex, text.ToIndex));
                    Paragraph p = CreateAddedParagraph(text.StrippedLine);
                    double width = new FormattedText(text.StrippedLine, System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Consolas sans-serif"), 12, Brushes.White, pixelsPerDip).Width;
                    maxWidth = Math.Max(maxWidth, width);
                    section.Blocks.Add(p);

                    diffContexts.Add(((Run)p.Inlines.FirstInline, text));
                }
                document.Blocks.Add(section);
            }
            else if (file.DestinationId.IsEmpty)
            {
                string[] lines = await repository.GetFileContentsAsync(file.SourceId);
                var section = new Section
                {
                    BorderBrush = dividerBrush,
                    BorderThickness = new Thickness(0, 0, 0, 1)
                };
                foreach (HunkLine text in lines.Select((line, index) => HunkLine.AsRemoved($"-{line}", index)))
                {
                    lineNumbers.Add(new LineNumberViewModel(text.FromIndex, text.ToIndex));
                    Paragraph p = CreateRemovedParagraph(text.StrippedLine);
                    double width = new FormattedText(text.StrippedLine, System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Consolas sans-serif"), 12, Brushes.White, pixelsPerDip).Width;
                    maxWidth = Math.Max(maxWidth, width);
                    section.Blocks.Add(p);

                    diffContexts.Add(((Run)p.Inlines.FirstInline, text));
                }
                document.Blocks.Add(section);
            }
            else
            {
                GitDiffResult result = await repository.ExecuteDiffAsync(file.SourceId, file.DestinationId, DiffOptions.Builder().Build());
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
            }
            document.PageWidth = maxWidth + 20;
            this.document = document;

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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Document)));
        }
    }
}
