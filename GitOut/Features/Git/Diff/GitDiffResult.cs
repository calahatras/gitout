using System;
using System.Collections.Generic;

namespace GitOut.Features.Git.Diff
{
    public class GitDiffResult
    {
        private GitDiffResult(string header, ICollection<GitDiffHunk> hunks)
        {
            Header = header;
            Hunks = hunks;
        }

        public string Header { get; }
        public IEnumerable<GitDiffHunk> Hunks { get; }

        public static IGitDiffBuilder Builder() => new GitDiffBuilder();

        private class GitDiffBuilder : IGitDiffBuilder
        {
            private readonly ICollection<GitDiffHunk> hunks = new List<GitDiffHunk>();
            private readonly ICollection<string> parts = new List<string>();
            private string? header;

            private bool hasCreatedHeader = false;

            public GitDiffResult Build()
            {
                if (header is null)
                {
                    throw new ArgumentNullException(nameof(header), "Expected header but header was null");
                }
                var lastHunk = GitDiffHunk.Parse(parts);
                hunks.Add(lastHunk);
                return new GitDiffResult(header, hunks);
            }

            public void Feed(string line)
            {
                if (line.StartsWith(GitDiffHunk.HunkIdentifier))
                {
                    if (hasCreatedHeader)
                    {
                        hunks.Add(GitDiffHunk.Parse(parts));
                    }
                    else
                    {
                        header = string.Join("\r\n", parts);
                    }
                    parts.Clear();
                    hasCreatedHeader = true;
                }
                else if (line.StartsWith("Binary files "))
                {
                    if (hasCreatedHeader)
                    {
                        hunks.Add(GitDiffHunk.Parse(parts));
                    }
                    else
                    {
                        header = string.Join("\r\n", parts);
                    }
                    parts.Clear();
                    hasCreatedHeader = true;
                }
                parts.Add(line);
            }
        }
    }
}
