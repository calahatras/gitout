using System;
using System.ComponentModel;
using System.Threading.Tasks;
using GitOut.Features.Git.Diff;
using GitOut.Features.IO;

namespace GitOut.Features.Git.Files
{
    public class GitFileViewModel : IGitFileEntryViewModel, INotifyPropertyChanged
    {
        private readonly IGitRepository repository;
        private readonly GitFileId sourceId;
        private readonly GitFileId? destinationId;

        private GitDiffResult? result;

        private GitFileViewModel(
            IGitRepository repository,
            RelativeDirectoryPath path,
            string fileName,
            GitFileId sourceId,
            GitFileId? destinationId = null,
            GitDiffType diffType = GitDiffType.None
        )
        {
            this.repository = repository;
            Path = path;
            FileName = fileName;
            this.sourceId = sourceId;
            this.destinationId = destinationId;
            Status = diffType;
            IconResourceKey = diffType switch
            {
                GitDiffType.None => "File",
                GitDiffType.Create => "FilePlus",
                GitDiffType.Delete => "FileRemove",
                GitDiffType.InPlaceEdit => "FileEdit",
                GitDiffType.RenameEdit => "FileMove",
                GitDiffType.CopyEdit => "FileReplace",
                _ => "FileHidden"
            };
        }

        public RelativeDirectoryPath Path { get; }
        public string FileName { get; }
        public GitDiffType Status { get; }
        public string IconResourceKey { get; }
        // note: this viewmodel is used in tree view and as such requires IsExpanded property
        public bool IsExpanded { get; set; }

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

        public static GitFileViewModel Wrap(IGitRepository repository, GitFileEntry file) => file.Type != GitFileType.Blob
            ? throw new ArgumentException($"Invalid file type for directory {file.Type}", nameof(file))
            : new GitFileViewModel(repository, file.Directory, file.FileName.ToString(), file.Id);

        public static GitFileViewModel Wrap(IGitRepository repository, GitDiffFileEntry file) => file.FileType != GitFileType.Blob
            ? throw new ArgumentException($"Invalid file type for directory {file.Type}", nameof(file))
            : new GitFileViewModel(repository, file.SourceFileName, file.SourceFileName.ToString(), file.SourceId, file.DestinationId, file.Type);

        private async Task RefreshDiffAsync()
        {
            result = destinationId is null
                ? await repository.GetFileContentsAsync(sourceId)
                : await repository.DiffAsync(sourceId, destinationId, DiffOptions.Builder().Build());
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DiffResult)));
        }
    }
}
