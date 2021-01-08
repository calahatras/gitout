using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using GitOut.Features.Git.Patch;

namespace GitOut.Features.Git.Diff
{
    public partial class GitDiffControl : UserControl, INotifyPropertyChanged, IHunkLineVisitorProvider
    {
        public static readonly DependencyProperty DiffProperty =
            DependencyProperty.Register(nameof(Diff), typeof(GitDiffResult), typeof(GitDiffControl), new PropertyMetadata(OnDiffChanges));

        public static readonly DependencyProperty ShowSpacesAsDotsProperty =
            DependencyProperty.Register(nameof(ShowSpacesAsDots), typeof(bool), typeof(GitDiffControl), new PropertyMetadata(false));

        public static readonly DependencyProperty DocumentProperty =
            DependencyProperty.Register(nameof(Document), typeof(FlowDocument), typeof(GitDiffControl), new PropertyMetadata(null));

        public static readonly DependencyProperty LineNumbersProperty =
            DependencyProperty.Register(nameof(LineNumbers), typeof(IEnumerable<LineNumberViewModel>), typeof(GitDiffControl), new PropertyMetadata(null));

        private GitDiffViewModel? viewModel;

        public GitDiffControl() => InitializeComponent();

        public GitDiffResult Diff
        {
            get => (GitDiffResult)GetValue(DiffProperty);
            set => SetValue(DiffProperty, value);
        }

        public bool ShowSpacesAsDots
        {
            get => (bool)GetValue(ShowSpacesAsDotsProperty);
            set => SetValue(ShowSpacesAsDotsProperty, value);
        }

        public IEnumerable<LineNumberViewModel>? LineNumbers
        {
            get => (IEnumerable<LineNumberViewModel>)GetValue(LineNumbersProperty);
            set => SetValue(LineNumbersProperty, value);
        }

        public FlowDocument? Document
        {
            get => (FlowDocument)GetValue(DocumentProperty);
            set => SetValue(DocumentProperty, value);
        }

        public InputBindingCollection DocumentInputBindings => HunksViewer.InputBindings;

        public Rect SelectionPosition => HunksViewer.Selection is null
            ? Rect.Empty
            : new Rect(HunksViewer.Selection.Start.GetCharacterRect(LogicalDirection.Forward).TopLeft, HunksViewer.Selection.End.GetCharacterRect(LogicalDirection.Forward).BottomRight);

        public event PropertyChangedEventHandler? PropertyChanged;

        public IHunkLineVisitor? GetHunkVisitor(PatchMode mode)
        {
            if (viewModel is GitDiffViewModel document)
            {
                TextRange selection = HunksViewer.Selection;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectionPosition)));
                TextPointer start = selection.Start;
                TextPointer end = selection.End;
                List<(Run, HunkLine)>? diffContexts = document.DiffContexts;

                int contextOffset = diffContexts.FindIndex(context => context.Item1 == start.Parent);
                if (contextOffset == -1)
                {
                    throw new InvalidOperationException("Invalid state, could not find matching paragraph");
                }
                int endOffset = diffContexts.FindIndex(contextOffset, context => context.Item1 == end.Parent);
                if (endOffset == -1)
                {
                    endOffset = diffContexts.Count - 1;
                }
                return new DiffHunkLineVisitor(mode, diffContexts.Select(item => item.Item2), contextOffset, endOffset);
            }
            return null;
        }

        private void TunnelEventToParentScroll(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                DocumentScroll.ScrollToHorizontalOffset(DocumentScroll.HorizontalOffset - e.Delta);
            }
            else
            {
                DocumentScroll.ScrollToVerticalOffset(DocumentScroll.VerticalOffset - e.Delta);
            }
        }

        private void CopySelectedText(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Clipboard.SetText(HunksViewer.Selection.Text.Replace('\u00B7', ' '), TextDataFormat.Text);
        }

        private static void OnDiffChanges(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GitDiffControl control)
            {
                if (e.NewValue is null)
                {
                    control.Document = null;
                    control.LineNumbers = null;
                    control.viewModel = null;
                }
                else if (e.NewValue is GitDiffResult result)
                {
                    double pixelsPerDip = VisualTreeHelper.GetDpi(control).PixelsPerDip;
                    DiffDisplayOptions display = control.ShowSpacesAsDots
                        ? new DiffDisplayOptions(
                            pixelsPerDip,
                            (Brush)Application.Current.Resources["MaterialLightDividers"],
                            (Brush)Application.Current.Resources["MaterialGray400"],
                            new ShowSpacesAsDotsTransform()
                        )
                        : new DiffDisplayOptions(
                            pixelsPerDip,
                            (Brush)Application.Current.Resources["MaterialLightDividers"],
                            (Brush)Application.Current.Resources["MaterialGray400"]
                        );
                    var vm = GitDiffViewModel.ParseDiff(result.Hunks, display);
                    control.Document = vm.Document;
                    control.LineNumbers = vm.LineNumbers;
                    control.viewModel = vm;
                }
            }
        }

        private class GitDiffViewModel
        {
            private static readonly Brush RemovedLineBackground = new SolidColorBrush(Color.FromArgb(128, 200, 90, 90));
            private static readonly Brush AddedLineBackground = new SolidColorBrush(Color.FromArgb(128, 90, 200, 90));

            private GitDiffViewModel(
                FlowDocument document,
                IEnumerable<LineNumberViewModel> lineNumbers,
                List<(Run, HunkLine)> diffContexts
            )
            {
                DiffContexts = diffContexts;
                Document = document;
                LineNumbers = lineNumbers;
            }

            public FlowDocument Document { get; }
            public IEnumerable<LineNumberViewModel> LineNumbers { get; }

            public List<(Run, HunkLine)> DiffContexts { get; }

            public static GitDiffViewModel ParseDiff(IEnumerable<GitDiffHunk> result, DiffDisplayOptions display)
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
                foreach (GitDiffHunk hunk in result)
                {
                    var section = new Section
                    {
                        BorderBrush = display.DividerBrush,
                        BorderThickness = new Thickness(0, 0, 0, 1)
                    };
                    foreach (HunkLine text in hunk.Lines)
                    {
                        lineNumbers.Add(new LineNumberViewModel(text.FromIndex, text.ToIndex));
                        Paragraph p = text.Type switch
                        {
                            DiffLineType.Added => CreateAddedParagraph(display.Transform.Transform(text.StrippedLine)),
                            DiffLineType.Removed => CreateRemovedParagraph(display.Transform.Transform(text.StrippedLine)),
                            DiffLineType.Header => CreateHeaderParagraph(text.StrippedLine, display.HeaderForeground),
                            DiffLineType.Control => CreateHeaderParagraph(text.StrippedLine, display.HeaderForeground),
                            _ => CreateDefaultParagraph(display.Transform.Transform(text.StrippedLine))
                        };
                        double width = new FormattedText(
                            display.Transform.Transform(text.StrippedLine), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Consolas sans-serif"), 12, Brushes.White, display.PixelsPerDip).Width;
                        maxWidth = Math.Max(maxWidth, width);
                        section.Blocks.Add(p);

                        diffContexts.Add(((Run)p.Inlines.FirstInline, text));
                    }
                    document.Blocks.Add(section);
                }
                document.PageWidth = maxWidth + 20;
                return new GitDiffViewModel(document, lineNumbers, diffContexts);

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

            public static GitDiffViewModel ParseFileContent(string[] result, double pixelsPerDip)
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
                    double width = new FormattedText(line, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Consolas sans-serif"), 12, Brushes.White, pixelsPerDip).Width;
                    maxWidth = Math.Max(maxWidth, width);
                    var run = new Run(line);
                    content.Inlines.Add(run);
                    content.Inlines.Add(new LineBreak());
                    diffContexts.Add((run, HunkLine.AsAdded("+" + line, i + 1)));
                }

                document.Blocks.Add(content);
                document.PageWidth = maxWidth + 20;
                return new GitDiffViewModel(document, lineNumbers, diffContexts);
            }
        }
    }
}
