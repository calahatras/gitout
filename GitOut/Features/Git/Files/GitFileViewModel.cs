using System;

namespace GitOut.Features.Git.Files
{
    public class GitFileViewModel : IGitFileEntryViewModel
    {
        private readonly GitFileEntry file;

        private GitFileViewModel(GitFileEntry file)
        {
            if (file.Type != GitFileType.Blob)
            {
                throw new ArgumentException($"Invalid file type for directory {file.Type}", nameof(file));
            }
            this.file = file;
        }

        public string FileName => file.FileName;
        public string IconResourceKey => "File";

        public static GitFileViewModel Wrap(GitFileEntry file) => new GitFileViewModel(file);
    }
}
