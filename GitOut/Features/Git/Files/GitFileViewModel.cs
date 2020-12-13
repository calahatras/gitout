using System;
using System.ComponentModel;
using System.Threading.Tasks;
using GitOut.Features.Git.Diff;

namespace GitOut.Features.Git.Files
{
    public class GitFileViewModel : IGitFileEntryViewModel, INotifyPropertyChanged
    {
        private readonly IGitRepository repository;
        private readonly GitFileEntry file;
        private GitDiffResult? result;

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

        public static GitFileViewModel Wrap(IGitRepository repository, GitFileEntry file) => new GitFileViewModel(repository, file);

        private async Task RefreshDiffAsync()
        {
            result = await repository.GetFileContentsAsync(file.Id);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DiffResult)));
        }
    }
}
