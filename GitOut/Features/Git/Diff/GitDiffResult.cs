using System;
using System.Collections.Generic;
using System.IO;

namespace GitOut.Features.Git.Diff
{
    public class GitDiffResult
    {
        private GitDiffResult(TextDiffResult text) => Text = text;

        private GitDiffResult(BlobDiffResult blob) => Blob = blob;

        public TextDiffResult? Text { get; }
        public BlobDiffResult? Blob { get; }

        public static IGitDiffBuilder Builder() => new GitDiffBuilder();

        private class GitDiffBuilder : IGitDiffBuilder
        {
            private readonly ICollection<GitDiffHunk> hunks = new List<GitDiffHunk>();
            private readonly ICollection<string> parts = new List<string>();
            private string? header;

            private Stream? stream;
            private bool hasCreatedHeader;

            public bool IsBinaryFile { get; private set; }

            public GitDiffResult Build()
            {
                if (IsBinaryFile)
                {
                    return new GitDiffResult(
                        new BlobDiffResult(
                            stream
                                ?? throw new InvalidOperationException(
                                    "Cannot create binary diff without stream"
                                )
                        )
                    );
                }
                if (header is null)
                {
                    // Note: if parts.Count is 3 then we have an empty file
                    return parts.Count > 3
                        ? throw new InvalidOperationException(
                            "Expected header and parts but none was found"
                        )
                        : new GitDiffResult(
                            new TextDiffResult(string.Empty, Array.Empty<GitDiffHunk>())
                        );
                }
                var lastHunk = GitDiffHunk.Parse(parts);
                hunks.Add(lastHunk);
                return new GitDiffResult(new TextDiffResult(header, hunks));
            }

            public IGitDiffBuilder Feed(Stream stream, GitStatusChangeType type)
            {
                bool isBinary = IsBinary(stream);
                if (isBinary)
                {
                    IsBinaryFile = true;
                    this.stream = stream;
                }
                else
                {
                    List<string> content = new();
                    using (var reader = new StreamReader(stream))
                    {
                        string? line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            content.Add(line);
                        }
                    }
                    Feed(
                        $"{GitDiffHunk.HunkIdentifier} -1,{content.Count} +1,{content.Count} {GitDiffHunk.HunkIdentifier}"
                    );
                    char diffChar = type == GitStatusChangeType.Untracked ? '+' : ' ';
                    foreach (string line in content)
                    {
                        Feed($"{diffChar}{line}");
                    }
                }
                return this;
            }

            public IGitDiffBuilder Feed(string line)
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
                    IsBinaryFile = true;
                    hasCreatedHeader = true;
                }
                parts.Add(line);
                return this;
            }

            private static bool IsBinary(Stream stream)
            {
                const int limit = 8000;
                byte[] buffer = new byte[limit];
                int read;
                int position = 0;
                while ((read = stream.Read(buffer)) > 0)
                {
                    int maxCharacters = limit - position;
                    for (int i = 0; i < maxCharacters && i < read; ++i)
                    {
                        if (buffer[i] == '\0')
                        {
                            stream.Position = 0;
                            return true;
                        }
                    }
                    position += read;
                    if (position > limit)
                    {
                        break;
                    }
                }
                stream.Position = 0;
                return false;
            }
        }
    }
}
