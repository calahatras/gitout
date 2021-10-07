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

        private DiffContext? result;

        private GitFileViewModel(
            IGitRepository repository,
            RelativeDirectoryPath path,
            FileName fileName,
            string displayName,
            GitFileId sourceId,
            GitFileId? destinationId = null,
            GitDiffType diffType = GitDiffType.None
        )
        {
            this.repository = repository;
            Path = path;
            FileName = fileName;
            DisplayName = displayName;
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
        public FileName FileName { get; }
        public GitDiffType Status { get; }
        public string DisplayName { get; }
        public string IconResourceKey { get; }
        // note: this viewmodel is used in tree view and as such requires IsExpanded property
        public bool IsExpanded { get; set; }

        public DiffContext? DiffResult
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

        public static GitFileViewModel Snapshot(IGitRepository repository, GitFileEntry file) => file.Type != GitFileType.Blob
            ? throw new ArgumentException($"Invalid file type for blob {file.Type}", nameof(file))
            : new GitFileViewModel(
                repository,
                file.Directory,
                file.FileName,
                file.FileName.ToString(),
                file.Id
            );

        public static GitFileViewModel Difference(IGitRepository repository, GitDiffFileEntry file) => file.FileType != GitFileType.Blob
            ? throw new ArgumentException($"Invalid file type for blob {file.Type}", nameof(file))
            : new GitFileViewModel(
                repository,
                file.Source.Directory,
                file.Source.FileName,
                file.Source.FileName.ToString(),
                file.Source.Id,
                file.Destination.Id,
                file.Type
            );

        public static GitFileViewModel RelativeDifference(IGitRepository repository, GitDiffFileEntry file) => file.FileType != GitFileType.Blob
            ? throw new ArgumentException($"Invalid file type for blob {file.Type}", nameof(file))
            : new GitFileViewModel(
                repository,
                file.Source.Directory,
                file.Source.FileName,
                System.IO.Path.Combine(file.Source.Directory.ToString(), file.Source.FileName.ToString()).Replace('\\', '/'),
                file.Source.Id,
                file.Destination.Id,
                file.Type
            );

        private async Task RefreshDiffAsync()
        {
            result = destinationId is null
                ? await DiffContext.SnapshotFileAsync(repository, Path, FileName, sourceId)
                : await DiffContext.DiffFileAsync(repository, Path, FileName, sourceId, destinationId);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DiffResult)));
        }
    }
}
