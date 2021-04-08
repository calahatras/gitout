using System.IO;

namespace GitOut.Features.Settings
{
    public class ValidGitRepositoryPathViewModel
    {
        private ValidGitRepositoryPathViewModel(DirectoryInfo dir)
        {
            Name = dir.Parent?.Name ?? string.Empty;
            WorkingDirectory = dir.Parent?.FullName ?? string.Empty;
        }

        public string Name { get; }
        public string WorkingDirectory { get; }

        public static ValidGitRepositoryPathViewModel FromGitFolder(DirectoryInfo dir) => new(dir);
    }
}
