namespace GitOut.Features.Git.Files
{
    public class LoadingViewModel : IGitFileEntryViewModel
    {
        public static readonly LoadingViewModel Proxy = new LoadingViewModel();
        private LoadingViewModel() { }

        public string FileName => string.Empty;
        public string IconResourceKey => string.Empty;
    }
}
