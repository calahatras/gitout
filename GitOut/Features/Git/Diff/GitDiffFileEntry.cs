using System;
using System.Collections.Generic;
using GitOut.Features.IO;

namespace GitOut.Features.Git.Diff;

public class GitDiffFileEntry
{
    private GitDiffFileEntry(
        GitFileType fileType,
        GitFileEntry source,
        GitFileEntry destination,
        GitDiffType type
    )
    {
        FileType = fileType;
        Source = source;
        Destination = destination;
        Type = type;
    }

    public GitFileEntry Source { get; }
    public GitFileEntry Destination { get; }

    public GitFileType FileType { get; }
    public GitDiffType Type { get; }

    public static IGitDiffFileEntryBuilder Parse(ReadOnlySpan<char> line)
    {
        if (line.Length < 98)
        {
            throw new ArgumentException(
                $"Line must be longer than 100 chars, was {line.Length}",
                nameof(line)
            );
        }

        _ = Enum.TryParse(new string(line[4..5]), out PosixFileModes sourceUser);
        _ = Enum.TryParse(new string(line[5..6]), out PosixFileModes sourceGroup);
        _ = Enum.TryParse(new string(line[6..7]), out PosixFileModes sourceOther);

        _ = Enum.TryParse(new string(line[11..12]), out PosixFileModes destinationUser);
        _ = Enum.TryParse(new string(line[12..13]), out PosixFileModes destinationGroup);
        _ = Enum.TryParse(new string(line[13..14]), out PosixFileModes destinationOther);

        var sourceId = GitFileId.FromHash(line[15..55]);
        var destinationId = GitFileId.FromHash(line[56..96]);

        GitDiffType type = new string(line[97..98]) switch
        {
            "M" => GitDiffType.InPlaceEdit,
            "C" => GitDiffType.CopyEdit,
            "R" => GitDiffType.RenameEdit,
            "A" => GitDiffType.Create,
            "D" => GitDiffType.Delete,
            "U" => GitDiffType.Unmerged,
            _ => GitDiffType.None,
        };

        GitFileType fileType = new string(
            type == GitDiffType.Create ? line[8..11] : line[1..4]
        ) switch
        {
            "100" => GitFileType.Blob,
            "040" => GitFileType.Tree,
            _ => GitFileType.None,
        };

        return new GitDiffFileEntryBuilder(
            fileType,
            sourceId,
            destinationId,
            new[] { sourceUser, sourceGroup, sourceOther },
            new[] { destinationUser, destinationGroup, destinationOther },
            type
        );
    }

    private class GitDiffFileEntryBuilder : IGitDiffFileEntryBuilder
    {
        private readonly GitFileType fileType;
        private readonly GitFileId sourceId;
        private readonly GitFileId destinationId;
        private readonly IEnumerable<PosixFileModes> sourceFileModes;
        private readonly IEnumerable<PosixFileModes> destinationFileModes;

        public GitDiffFileEntryBuilder(
            GitFileType fileType,
            GitFileId sourceId,
            GitFileId destinationId,
            IEnumerable<PosixFileModes> sourceFileModes,
            IEnumerable<PosixFileModes> destinationFileModes,
            GitDiffType type
        )
        {
            this.fileType = fileType;
            this.sourceId = sourceId;
            this.destinationId = destinationId;
            this.sourceFileModes = sourceFileModes;
            this.destinationFileModes = destinationFileModes;
            Type = type;
        }

        public GitDiffType Type { get; }

        public GitDiffFileEntry Build(string sourcePath)
        {
            if (Type is GitDiffType.CopyEdit or GitDiffType.RenameEdit)
            {
                throw new InvalidOperationException(
                    "Cannot build file entry for copy or rename without destination path"
                );
            }
            var path = RelativeDirectoryPath.Create(sourcePath);
            return new GitDiffFileEntry(
                fileType,
                new GitFileEntry(sourceId, fileType, sourceFileModes, path),
                new GitFileEntry(destinationId, fileType, destinationFileModes, path.Parent),
                Type
            );
        }

        public GitDiffFileEntry Build(string sourcePath, string destinationPath)
        {
            var source = RelativeDirectoryPath.Create(sourcePath);
            var destination = RelativeDirectoryPath.Create(destinationPath);
            return new GitDiffFileEntry(
                fileType,
                new GitFileEntry(sourceId, fileType, sourceFileModes, source),
                new GitFileEntry(destinationId, fileType, destinationFileModes, destination),
                Type
            );
        }
    }
}
