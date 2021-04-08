using System;
using System.Collections.Generic;
using GitOut.Features.IO;

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
            GitFileId? sourceId,
            GitFileId? destinationId,
            RelativeDirectoryPath path,
            RelativeDirectoryPath? mergedPath
        )
        {
            Type = type;
            IndexStatus = indexStatus;
            WorkspaceStatus = workspaceStatus;
            HeadFileModes = headFileModes;
            IndexFileModes = indexFileModes;
            WorktreeFileModes = worktreeFileModes;
            SourceId = sourceId;
            DestinationId = destinationId;
            Path = path;
            MergedPath = mergedPath;
        }

        public GitStatusChangeType Type { get; }
        public GitModifiedStatusType? IndexStatus { get; }
        public GitModifiedStatusType? WorkspaceStatus { get; }

        public IReadOnlyCollection<PosixFileModes>? HeadFileModes { get; }
        public IReadOnlyCollection<PosixFileModes>? IndexFileModes { get; }
        public IReadOnlyCollection<PosixFileModes>? WorktreeFileModes { get; }

        public GitFileId? SourceId { get; }
        public GitFileId? DestinationId { get; }

        public RelativeDirectoryPath Path { get; }
        public RelativeDirectoryPath? MergedPath { get; }

        public static IGitStatusChangeBuilder Parse(string change) => new GitStatusChangeBuilder(change);

        private class GitStatusChangeBuilder : IGitStatusChangeBuilder
        {
            private readonly GitModifiedStatusType? stagedStatus;
            private readonly GitModifiedStatusType? unstagedStatus;
            private readonly PosixFileModes[]? headFileModes;
            private readonly PosixFileModes[]? indexFileModes;
            private readonly PosixFileModes[]? worktreeFileModes;

            private readonly GitFileId? sourceId;
            private readonly GitFileId? destinationId;

            private readonly RelativeDirectoryPath path;

            private RelativeDirectoryPath? mergedPath;

            public GitStatusChangeBuilder(string change)
            {
                if (change.Length < 3)
                {
                    throw new ArgumentException($"Change must be longer than 3 characters but was {change.Length}", nameof(change));
                }
                Type = GetStatusChangeType(change[0]);
                if (Type == GitStatusChangeType.Untracked)
                {
                    path = RelativeDirectoryPath.Create(change[2..]);
                }
                else
                {
                    if (change.Length < 114)
                    {
                        throw new ArgumentException($"Change must be longer than 113 characters but was {change.Length}", nameof(change));
                    }

                    stagedStatus = GetModifiedStatusType(change[2]);
                    unstagedStatus = GetModifiedStatusType(change[3]);
                    headFileModes = GetFileModes(change[10..16]);
                    indexFileModes = GetFileModes(change[17..23]);
                    worktreeFileModes = GetFileModes(change[24..30]);

                    sourceId = GitFileId.FromHash(change.AsSpan()[31..71]);
                    destinationId = GitFileId.FromHash(change.AsSpan()[72..112]);

                    path = Type == GitStatusChangeType.RenamedOrCopied
                        ? RelativeDirectoryPath.Create(change[(change.IndexOf(' ', 113) + 1)..])
                        : RelativeDirectoryPath.Create(change[113..]);
                }
            }

            public GitStatusChangeType Type { get; }

            public GitStatusChange Build() => new(
                Type,
                stagedStatus,
                unstagedStatus,
                headFileModes,
                indexFileModes,
                worktreeFileModes,
                sourceId,
                destinationId,
                path,
                mergedPath);

            public IGitStatusChangeBuilder MergedFrom(string path)
            {
                mergedPath = RelativeDirectoryPath.Create(path);
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
                _ = Enum.TryParse(modes.Substring(3, 1), out PosixFileModes user);
                _ = Enum.TryParse(modes.Substring(4, 1), out PosixFileModes group);
                _ = Enum.TryParse(modes.Substring(5, 1), out PosixFileModes other);
                return new[]
                {
                    user, group, other
                };
            }
        }
    }
}
