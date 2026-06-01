using System.Collections.Generic;
using System.Linq;
using GitOut.Features.Text;

namespace GitOut.Features.Git.Files;

public class GitBlameViewModel
{
    public GitBlameViewModel(IEnumerable<GitBlameHunkViewModel> hunks)
    {
        Hunks = hunks.ToList();
    }

    public IReadOnlyList<GitBlameHunkViewModel> Hunks { get; }

    public static GitBlameViewModel ParseBlame(
        GitBlameResult result,
        ISyntaxHighlighter highlighter
    )
    {
        var hunkViewModels = result.Hunks.Select(hunk =>
            GitBlameHunkViewModel.Parse(hunk, highlighter)
        );
        return new GitBlameViewModel(hunkViewModels);
    }
}
