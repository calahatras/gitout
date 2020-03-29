using GitOut.Features.Wpf;

namespace GitOut.Features.Git.Log
{
    public class GitLogViewModel
    {
        public GitLogViewModel(
            ITitleService title
        ) => title.Title = "Log";
    }
}
