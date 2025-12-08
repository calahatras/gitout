using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using GitOut.Features.Collections;
using GitOut.Features.Git.Patch;

namespace GitOut.Features.Git.Diff
{
    public partial class GitDiffControl
        : UserControl,
            INotifyPropertyChanged,
            IHunkLineVisitorProvider
    {
        public static readonly DependencyProperty DiffProperty = DependencyProperty.Register(
            nameof(Diff),
            typeof(DiffContext),
            typeof(GitDiffControl),
            new PropertyMetadata(OnDiffChanges)
        );

        public static readonly DependencyProperty ShowSpacesAsDotsProperty =
            DependencyProperty.Register(
                nameof(ShowSpacesAsDots),
                typeof(bool),
                typeof(GitDiffControl),
                new PropertyMetadata(false, OnSpacesViewModeChanged)
            );

        public static readonly DependencyProperty CurrentContentProperty =
            DependencyProperty.Register(
                nameof(CurrentContent),
                typeof(object),
                typeof(GitDiffControl),
                new PropertyMetadata(null)
            );

        public GitDiffControl() => InitializeComponent();

        public object? CurrentContent
        {
            get => GetValue(CurrentContentProperty);
            set => SetValue(CurrentContentProperty, value);
        }

        public DiffContext Diff
        {
            get => (DiffContext)GetValue(DiffProperty);
            set => SetValue(DiffProperty, value);
        }

        public bool ShowSpacesAsDots
        {
            get => (bool)GetValue(ShowSpacesAsDotsProperty);
            set => SetValue(ShowSpacesAsDotsProperty, value);
        }

        public Rect SelectionPosition =>
            CurrentContent is GitDiffViewModel document && document.Selection is TextRange selection
                ? new Rect(
                    selection.Start.GetCharacterRect(LogicalDirection.Forward).TopLeft,
                    selection.End.GetCharacterRect(LogicalDirection.Forward).BottomRight
                )
                : Rect.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        public IHunkLineVisitor? GetHunkVisitor(PatchMode mode)
        {
            if (
                CurrentContent is GitDiffViewModel document
                && document.Selection is TextRange selection
            )
            {
                PropertyChanged?.Invoke(
                    this,
                    new PropertyChangedEventArgs(nameof(SelectionPosition))
                );
                Paragraph start = selection.Start.Paragraph;
                Paragraph end = selection.End.Paragraph;
                IReadOnlyCollection<(Paragraph, HunkLine)> diffContexts = document.DiffContexts;

                int contextOffset = diffContexts.FindIndex(context => context.Item1 == start);
                if (contextOffset == -1)
                {
                    throw new InvalidOperationException(
                        "Invalid state, could not find matching paragraph"
                    );
                }
                int endOffset = diffContexts.FindIndex(
                    contextOffset,
                    context => context.Item1 == end
                );
                if (endOffset == -1)
                {
                    endOffset = diffContexts.Count - 1;
                }
                return new DiffHunkLineVisitor(
                    mode,
                    diffContexts.Select(item => item.Item2),
                    contextOffset,
                    endOffset
                );
            }
            return null;
        }

        private void ParseCurrentContent(IEnumerable<GitDiffHunk> hunks)
        {
            double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            DiffDisplayOptions display = ShowSpacesAsDots
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
            var vm = GitDiffViewModel.ParseDiff(hunks, display);
            CurrentContent = vm;
        }

        private static async void OnDiffChanges(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        )
        {
            if (d is GitDiffControl control)
            {
                if (e.NewValue is null)
                {
                    control.CurrentContent = null;
                }
                else if (e.NewValue is DiffContext context)
                {
                    string extension = context.FileExtension;
                    if (context.Blob is not null)
                    {
                        if (IsImageFile(extension))
                        {
                            if (
                                context.DestinationId is not null
                                && context.SourceId is not null
                                && !context.SourceId.IsEmpty
                            )
                            {
                                Stream sourceImage = await context.Blob.GetSourceStreamAsync();
                                control.CurrentContent = new ImageViewModel(
                                    context.Blob.GetBaseStream(),
                                    sourceImage
                                );
                            }
                            else
                            {
                                control.CurrentContent = new ImageViewModel(
                                    context.Blob.GetBaseStream()
                                );
                            }
                        }
                    }
                    else if (context.Text is not null)
                    {
                        control.ParseCurrentContent(context.Text.Hunks);
                    }
                }
            }

            static bool IsImageFile(string extension) =>
                new HashSet<string>(
                    new[] { ".bmp", ".gif", ".png", ".jpg", ".jpeg", ".tiff", ".webp" }
                ).Contains(extension);
        }

        private static void OnSpacesViewModeChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        )
        {
            if (
                d is GitDiffControl control
                && control.Diff is DiffContext context
                && context.Text is not null
            )
            {
                control.ParseCurrentContent(context.Text.Hunks);
            }
        }
    }
}
