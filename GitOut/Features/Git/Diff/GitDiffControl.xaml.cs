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
using GitOut.Features.Collections;
using GitOut.Features.Git.Patch;
using GitOut.Features.Text;

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
                Paragraph start = selection.Start.Paragraph;
                Paragraph end = selection.End.Paragraph;
                IReadOnlyCollection<(Paragraph, HunkLine)> diffContexts = document.DiffContexts;

                int contextOffset = diffContexts.FindIndex(context => context.Item1 == start);
                if (contextOffset == -1)
                {
                    throw new InvalidOperationException("Invalid state, could not find matching paragraph");
                }
                int endOffset = diffContexts.FindIndex(contextOffset, context => context.Item1 == end);
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
                    var vm = GitDiffViewModel.ParseDiff(result.Hunks, new CSharpSyntaxHighlighter(), display);
                    control.Document = vm.Document;
                    control.LineNumbers = vm.LineNumbers;
                    control.viewModel = vm;
                }
            }
        }

        private class GitDiffViewModel
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

            public List<(Paragraph, HunkLine)> DiffContexts { get; }

            public static GitDiffViewModel ParseDiff(IEnumerable<GitDiffHunk> result, ISyntaxHighlighter parser, DiffDisplayOptions display)
            {
                var document = new FlowDocument
                {
                    FontFamily = new FontFamily(FontFamilyName),
                    FontSize = 12,
                    PagePadding = new Thickness(0)
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
                    IEnumerable<Paragraph> highlighted = parser.Highlight(lines.Select(line => line.StrippedLine), new DiffLineHighlighter(lines));
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
        }

        private class DiffLineHighlighter : ILineDecorator
        {
            private static readonly Brush RemovedLineBackground = new SolidColorBrush(Color.FromArgb(128, 200, 90, 90));
            private static readonly Brush AddedLineBackground = new SolidColorBrush(Color.FromArgb(90, 90, 200, 90));

            private readonly IReadOnlyList<HunkLine> lines;

            public DiffLineHighlighter(IEnumerable<HunkLine> lines) => this.lines = lines.ToList().AsReadOnly();

            public void Decorate(Paragraph paragraph, int lineNumber) => paragraph.Background = lines[lineNumber].Type switch
            {
                DiffLineType.Added => AddedLineBackground,
                DiffLineType.Removed => RemovedLineBackground,
                _ => Brushes.Transparent
            };
        }
    }
}
