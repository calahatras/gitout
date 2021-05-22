using System.Collections.Generic;

namespace GitOut.Features.Git.Diff
{
    public class TextDiffResult
    {
        public TextDiffResult(string header, ICollection<GitDiffHunk> hunks)
        {
            Header = header;
            Hunks = hunks;
        }

        public string Header { get; }
        public IEnumerable<GitDiffHunk> Hunks { get; }
    }

}
