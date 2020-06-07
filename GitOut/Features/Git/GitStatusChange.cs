using System;
using System.Collections.Generic;

namespace GitOut.Features.Git
{
    public class GitStatusChange
    {
        private GitStatusChange(
            GitStatusChangeType type,
            GitModifiedStatusType? indexStatus,
            GitModifiedStatusType? workspaceStatus,
            PosixFileModes[]? headFileModes,
            PosixFileModes[]? indexFileModes,
            PosixFileModes[]? worktreeFileModes,
            string path,
            string? mergedPath
        )
        {
            Type = type;
            IndexStatus = indexStatus;
            WorkspaceStatus = workspaceStatus;
            HeadFileModes = headFileModes;
            IndexFileModes = indexFileModes;
            WorktreeFileModes = worktreeFileModes;
            Path = path;
            MergedPath = mergedPath;
        }

        public GitStatusChangeType Type { get; }
        public GitModifiedStatusType? IndexStatus { get; }
        public GitModifiedStatusType? WorkspaceStatus { get; }

        public IReadOnlyCollection<PosixFileModes>? HeadFileModes { get; }
        public IReadOnlyCollection<PosixFileModes>? IndexFileModes { get; }
        public IReadOnlyCollection<PosixFileModes>? WorktreeFileModes { get; }

        public string Path { get; }
        public string? MergedPath { get; }

        public static IGitStatusChangeBuilder Parse(string change) => new GitStatusChangeBuilder(change);

        private class GitStatusChangeBuilder : IGitStatusChangeBuilder
        {
            private readonly GitModifiedStatusType? stagedStatus;
            private readonly GitModifiedStatusType? unstagedStatus;
            private readonly PosixFileModes[]? headFileModes;
            private readonly PosixFileModes[]? indexFileModes;
            private readonly PosixFileModes[]? worktreeFileModes;
            private readonly string path;

            private string? mergedPath;

            public GitStatusChangeBuilder(string change)
            {
                if (change.Length < 3)
                {
                    throw new ArgumentException($"Change must be longer than 3 characters but was {change.Length}", nameof(change));
                }
                Type = GetStatusChangeType(change[0]);
                if (Type == GitStatusChangeType.Untracked)
                {
                    path = change.Substring(2);
                }
                else
                {
                    if (change.Length < 114)
                    {
                        throw new ArgumentException($"Change must be longer than 113 characters but was {change.Length}", nameof(change));
                    }

                    stagedStatus = GetModifiedStatusType(change[2]);
                    unstagedStatus = GetModifiedStatusType(change[3]);
                    headFileModes = GetFileModes(change.Substring(10, 6));
                    indexFileModes = GetFileModes(change.Substring(17, 6));
                    worktreeFileModes = GetFileModes(change.Substring(24, 6));
                    path = change.Substring(113);
                }
            }

            public GitStatusChangeType Type { get; }

            public GitStatusChange Build() => new GitStatusChange(Type, stagedStatus, unstagedStatus, headFileModes, indexFileModes, worktreeFileModes, path, mergedPath);

            public IGitStatusChangeBuilder MergedFrom(string path)
            {
                mergedPath = path;
                return this;
            }

            private static GitStatusChangeType GetStatusChangeType(char type) => type switch
            {
                '1' => GitStatusChangeType.Ordinary,
                '2' => GitStatusChangeType.RenamedOrCopied,
                'u' => GitStatusChangeType.Unmerged,
                '?' => GitStatusChangeType.Untracked,
                '!' => GitStatusChangeType.Ignored,
                _ => GitStatusChangeType.None
            };

            private static GitModifiedStatusType GetModifiedStatusType(char type) => type switch
            {
                '.' => GitModifiedStatusType.Unmodified,
                'M' => GitModifiedStatusType.Modified,
                'A' => GitModifiedStatusType.Added,
                'D' => GitModifiedStatusType.Deleted,
                'R' => GitModifiedStatusType.Renamed,
                'C' => GitModifiedStatusType.Copied,
                'U' => GitModifiedStatusType.UpdatedButUnmerged,
                _ => GitModifiedStatusType.None
            };

            private static PosixFileModes[] GetFileModes(string modes)
            {
                if (modes.Length != 6)
                {
                    throw new ArgumentException($"Modes must be of length 6 but was {modes.Length}", nameof(modes));
                }
                Enum.TryParse(modes.Substring(3, 1), out PosixFileModes user);
                Enum.TryParse(modes.Substring(4, 1), out PosixFileModes group);
                Enum.TryParse(modes.Substring(5, 1), out PosixFileModes other);
                return new[]
                {
                    user, group, other
                };
            }
        }
    }
}
