using System;
using System.Collections.Generic;

namespace GitOut.Features.Git.Diff
{
    public class GitDiffResult
    {
        private GitDiffResult(DiffOptions options, string header, ICollection<GitDiffHunk> hunks)
        {
            Options = options;
            Header = header;
            Hunks = hunks;
        }

        public DiffOptions Options { get; }
        public string Header { get; }
        public IEnumerable<GitDiffHunk> Hunks { get; }

        public static IGitDiffBuilder Builder() => new GitDiffBuilder();

        private class GitDiffBuilder : IGitDiffBuilder
        {
            private readonly ICollection<GitDiffHunk> hunks = new List<GitDiffHunk>();
            private readonly ICollection<string> parts = new List<string>();
            private string? header;

            private bool hasHunk = false;

            public GitDiffResult Build(DiffOptions options)
            {
                if (header is null)
                {
                    throw new ArgumentNullException(nameof(header), "Expected header but header was null");
                }
                var lastHunk = GitDiffHunk.Parse(parts);
                hunks.Add(lastHunk);
                return new GitDiffResult(options, header, hunks);
            }

            public void Feed(string line)
            {
                if (line.StartsWith(GitDiffHunk.HunkIdentifier))
                {
                    if (hasHunk)
                    {
                        hunks.Add(GitDiffHunk.Parse(parts));
                    }
                    else
                    {
                        header = string.Join("\r\n", parts);
                    }
                    parts.Clear();
                    hasHunk = true;
                }
                parts.Add(line);
            }
        }
    }
}
