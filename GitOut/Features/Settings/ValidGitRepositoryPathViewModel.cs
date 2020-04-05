using System.IO;

namespace GitOut.Features.Settings
{
    public class ValidGitRepositoryPathViewModel
    {
        private ValidGitRepositoryPathViewModel(DirectoryInfo dir)
        {
            Name = dir.Parent.Name;
            WorkingDirectory = dir.Parent.FullName;
        }

        public string Name { get; }
        public string WorkingDirectory { get; }

        public static ValidGitRepositoryPathViewModel FromGitFolder(DirectoryInfo dir) => new ValidGitRepositoryPathViewModel(dir);
    }
}
