using System;
using System.Collections.Generic;

namespace GitOut.Features.Git.Diff
{
    public class GitDiffResult
    {
        private GitDiffResult(string header, ICollection<GitDiffHunk> hunks, bool isBinary)
        {
            Header = header;
            Hunks = hunks;
            IsBinary = isBinary;
        }

        public bool IsBinary { get; }
        public string Header { get; }
        public IEnumerable<GitDiffHunk> Hunks { get; }

        public static IGitDiffBuilder Builder() => new GitDiffBuilder();

        private class GitDiffBuilder : IGitDiffBuilder
        {
            private readonly ICollection<GitDiffHunk> hunks = new List<GitDiffHunk>();
            private readonly ICollection<string> parts = new List<string>();
            private string? header;

            private bool hasCreatedHeader;
            private bool isBinaryFile;

            public GitDiffResult Build()
            {
                if (header is null)
                {
                    // Note: if parts.Count is 3 then we have an empty file
                    return parts.Count > 3
                        ? throw new ArgumentNullException(nameof(header), "Expected header and parts but none was found")
                        : new GitDiffResult(string.Empty, Array.Empty<GitDiffHunk>(), false);
                }
                var lastHunk = GitDiffHunk.Parse(parts);
                hunks.Add(lastHunk);
                return new GitDiffResult(header, hunks, isBinaryFile);
            }

            public void Feed(string line)
            {
                if (line.StartsWith(GitDiffHunk.HunkIdentifier, StringComparison.Ordinal))
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
                else if (line.StartsWith("Binary files ", StringComparison.Ordinal))
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
                    isBinaryFile = true;
                    hasCreatedHeader = true;
                }
                parts.Add(line);
            }
        }
    }
}
