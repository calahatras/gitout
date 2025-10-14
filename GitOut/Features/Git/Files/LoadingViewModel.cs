using GitOut.Features.IO;

namespace GitOut.Features.Git.Files
{
    public class LoadingViewModel : IGitFileEntryViewModel
    {
        public static readonly LoadingViewModel Proxy = new();

        private LoadingViewModel() { }

        // Note: used by xaml binding
        public bool IsExpanded { get; set; }
        public RelativeDirectoryPath Path => RelativeDirectoryPath.Root;
        public FileName FileName => FileName.Create(string.Empty);
        public string FullPath => string.Empty;
        public string IconResourceKey => string.Empty;
    }
}
