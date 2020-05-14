using System;
using System.Collections.Generic;

namespace GitOut.Features.Git
{
    public class GitDiffResult
    {
        private GitDiffResult(GitStatusChange change, DiffOptions options, string header, ICollection<GitDiffHunk> hunks)
        {
            Change = change;
            Options = options;
            Header = header;
            Hunks = hunks;
        }

        public GitStatusChange Change { get; }
        public DiffOptions Options { get; }
        public string Header { get; }
        public IEnumerable<GitDiffHunk> Hunks { get; }

        public static IGitDiffBuilder ResultFor(GitStatusChange change, DiffOptions options)
            => new GitDiffBuilder(change, options);

        private class GitDiffBuilder : IGitDiffBuilder
        {
            private const string HunkIdentifier = "@@";

            private readonly GitStatusChange change;
            private readonly DiffOptions options;
            private readonly ICollection<GitDiffHunk> hunks = new List<GitDiffHunk>();
            private readonly ICollection<string> parts = new List<string>();
            private string? header;

            private bool hasHunk = false;

            public GitDiffBuilder(GitStatusChange change, DiffOptions options)
            {
                this.change = change;
                this.options = options;
            }

            public GitDiffResult Build()
            {
                if (header is null)
                {
                    throw new ArgumentNullException(nameof(header), "Expected header but header was null");
                }
                var lastHunk = GitDiffHunk.Parse(parts);
                hunks.Add(lastHunk);
                return new GitDiffResult(change, options, header, hunks);
            }

            public void Feed(string line)
            {
                if (line.StartsWith(HunkIdentifier))
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
