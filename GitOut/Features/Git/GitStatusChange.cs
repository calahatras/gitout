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
            RelativeDirectoryPath? mergedPath,
            DirectoryPath? workingDirectory
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
            WorkingDirectory = workingDirectory;
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
        public string FullPath => System.IO.Path.Combine(WorkingDirectory?.ToString() ?? string.Empty, Path.ToString().Replace("/", "\\", StringComparison.CurrentCulture));

        public DirectoryPath? WorkingDirectory { get; }

        public static IGitStatusChangeBuilder Parse(ReadOnlySpan<char> change) => new GitStatusChangeBuilder(change);

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
            private DirectoryPath? workingDirectory;

            public GitStatusChangeBuilder(ReadOnlySpan<char> change)
            {
                if (change.Length < 3)
                {
                    throw new ArgumentException($"Change must be longer than 3 characters but was {change.Length}", nameof(change));
                }
                Type = GetStatusChangeType(change[0]);
                if (Type == GitStatusChangeType.Untracked)
                {
                    path = RelativeDirectoryPath.Create(change[2..].ToString());
                }
                else if (Type == GitStatusChangeType.Unmerged)
                {
                    if (change.Length < 161)
                    {
                        throw new ArgumentException($"Change must be longer than 160 characters but was {change.Length}", nameof(change));
                    }
                    // u <xy> <sub> <m1> <m2> <m3> <mW> <h1> <h2> <h3> <path>
                    stagedStatus = GetModifiedStatusType(change[2]);
                    unstagedStatus = GetModifiedStatusType(change[3]);

                    PosixFileModes[]? first = GetFileModes(change[10..16].ToString());
                    PosixFileModes[]? second = GetFileModes(change[17..23].ToString());
                    PosixFileModes[]? third = GetFileModes(change[24..30].ToString());
                    worktreeFileModes = GetFileModes(change[31..37].ToString());

                    var firstObjectId = GitFileId.FromHash(change[38..78]);
                    var secondObjectId = GitFileId.FromHash(change[79..119]);
                    var thirdObjectId = GitFileId.FromHash(change[120..160]);

                    path = RelativeDirectoryPath.Create(change[161..].ToString());
                }
                else
                {
                    if (change.Length < 114)
                    {
                        throw new ArgumentException($"Change must be longer than 113 characters but was {change.Length}", nameof(change));
                    }

                    stagedStatus = GetModifiedStatusType(change[2]);
                    unstagedStatus = GetModifiedStatusType(change[3]);
                    headFileModes = GetFileModes(change[10..16].ToString());
                    indexFileModes = GetFileModes(change[17..23].ToString());
                    worktreeFileModes = GetFileModes(change[24..30].ToString());

                    sourceId = GitFileId.FromHash(change[31..71]);
                    destinationId = GitFileId.FromHash(change[72..112]);

                    path = Type == GitStatusChangeType.RenamedOrCopied
                        ? RelativeDirectoryPath.Create(change[(change[113..].IndexOf(' ') + 114)..].ToString())
                        : RelativeDirectoryPath.Create(change[113..].ToString());
                }
            }

            public GitStatusChangeType Type { get; }

            public void WorkingDirectory(DirectoryPath workingDirectory) => this.workingDirectory = workingDirectory;
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
                mergedPath,
                workingDirectory
            );

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
