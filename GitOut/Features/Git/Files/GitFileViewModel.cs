using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using GitOut.Features.Git.Diff;

namespace GitOut.Features.Git.Files
{
    public class GitFileViewModel : IGitFileEntryViewModel, INotifyPropertyChanged
    {
        private readonly IGitRepository repository;
        private readonly GitFileEntry file;
        private FlowDocument? document;

        private GitFileViewModel(IGitRepository repository, GitFileEntry file)
        {
            if (file.Type != GitFileType.Blob)
            {
                throw new ArgumentException($"Invalid file type for directory {file.Type}", nameof(file));
            }
            this.repository = repository;
            this.file = file;
        }

        public string FileName => file.FileName.ToString();
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

        public static GitFileViewModel Wrap(IGitRepository repository, GitFileEntry file) => new GitFileViewModel(repository, file);

        private async Task RefreshDiffAsync()
        {
            double pixelsPerDip = VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip;

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
            string[] result = await repository.GetFileContentsAsync(file.Id);
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
            this.document = document;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Document)));
        }
    }
}
