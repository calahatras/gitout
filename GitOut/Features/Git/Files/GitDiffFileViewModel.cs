using System;
using System.ComponentModel;
using System.Threading.Tasks;
using GitOut.Features.Git.Diff;

namespace GitOut.Features.Git.Files
{
    public class GitDiffFileViewModel : IGitFileEntryViewModel, INotifyPropertyChanged
    {
        private readonly IGitRepository repository;
        private readonly GitDiffFileEntry file;
        private GitDiffResult? result;

        private GitDiffFileViewModel(IGitRepository repository, GitDiffFileEntry file)
        {
            if (file.FileType != GitFileType.Blob)
            {
                throw new ArgumentException($"Invalid file type for directory {file.Type}", nameof(file));
            }
            this.repository = repository;
            this.file = file;
            IconResourceKey = file.Type switch
            {
                GitDiffType.Create => "FilePlus",
                GitDiffType.Delete => "FileRemove",
                GitDiffType.InPlaceEdit => "FileEdit",
                GitDiffType.RenameEdit => "FileMove",
                GitDiffType.CopyEdit => "FileReplace",
                _ => "FileHidden"
            };
        }

        public string FileName => file.SourceFileName.ToString();
        public string IconResourceKey { get; }
        // note: this viewmodel is used in tree view and as such requires IsExpanded property
        public bool IsExpanded { get; set; }

        public bool ShowSpacesAsDots { get; } = true;

        public GitDiffResult? DiffResult
        {
            get
            {
                if (result == null)
                {
                    _ = RefreshDiffAsync();
                }
                return result;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public static GitDiffFileViewModel Wrap(IGitRepository repository, GitDiffFileEntry file) => new GitDiffFileViewModel(repository, file);

        private async Task RefreshDiffAsync()
        {
            result = await repository.ExecuteDiffAsync(file.SourceId, file.DestinationId, DiffOptions.Builder().Build());
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DiffResult)));
        }
    }
}
