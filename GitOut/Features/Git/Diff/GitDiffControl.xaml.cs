using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using GitOut.Features.Collections;
using GitOut.Features.Git.Patch;

namespace GitOut.Features.Git.Diff
{
    public partial class GitDiffControl : UserControl, INotifyPropertyChanged, IHunkLineVisitorProvider
    {
        public static readonly DependencyProperty DiffProperty = DependencyProperty.Register(
            nameof(Diff),
            typeof(DiffContext),
            typeof(GitDiffControl),
            new PropertyMetadata(OnDiffChanges)
        );

        public static readonly DependencyProperty ShowSpacesAsDotsProperty = DependencyProperty.Register(
            nameof(ShowSpacesAsDots),
            typeof(bool),
            typeof(GitDiffControl),
            new PropertyMetadata(false)
        );

        public static readonly DependencyProperty CurrentContentProperty = DependencyProperty.Register(
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

        public Rect SelectionPosition => CurrentContent is GitDiffViewModel document && document.Selection is TextRange selection
            ? new Rect(selection.Start.GetCharacterRect(LogicalDirection.Forward).TopLeft, selection.End.GetCharacterRect(LogicalDirection.Forward).BottomRight)
            : Rect.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        public IHunkLineVisitor? GetHunkVisitor(PatchMode mode)
        {
            if (CurrentContent is GitDiffViewModel document && document.Selection is TextRange selection)
            {
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

        private static async void OnDiffChanges(DependencyObject d, DependencyPropertyChangedEventArgs e)
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
                    if (context.Result.Blob is not null)
                    {
                        if (IsImageFile(extension))
                        {
                            if (context.SourceId is not null)
                            {
                                Stream sourceImage = await context.GetSourceStreamAsync();
                                control.CurrentContent = new ImageViewModel(context.Result.Blob.Stream, sourceImage);
                            }
                            else
                            {
                                control.CurrentContent = new ImageViewModel(context.Result.Blob.Stream);
                            }
                        }
                    }
                    else if (context.Result.Text is not null)
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
                        var vm = GitDiffViewModel.ParseDiff(context.Result.Text.Hunks, display);
                        control.CurrentContent = vm;
                    }
                }
            }
        }

        private static bool IsImageFile(string extension) => new[]
        {
            ".bmp",
            ".png",
            ".jpg",
            ".jpeg",
            ".tiff"
        }
        .Contains(extension);
    }
}
