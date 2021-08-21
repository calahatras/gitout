using System;
using System.Collections.Generic;
using GitOut.Features.IO;

namespace GitOut.Features.Git
{
    public class GitFileEntry
    {
        public GitFileEntry(
            GitFileId id,
            GitFileType type,
            IEnumerable<PosixFileModes> fileModes,
            RelativeDirectoryPath directory
        )
        {
            Id = id;
            Type = type;
            FileModes = fileModes;
            Directory = directory.Parent;
            FileName = directory.Name;
        }

        public GitFileId Id { get; }

        public RelativeDirectoryPath Directory { get; }
        public FileName FileName { get; }

        public IEnumerable<PosixFileModes> FileModes { get; }
        public GitFileType Type { get; }

        public static GitFileEntry Parse(string fileLine)
        {
            string[] parts = fileLine.Split('\t', 2);
            string[] metadata = parts[0].Split(' ', 3);
            _ = Enum.TryParse(metadata[0][3..4], out PosixFileModes user);
            _ = Enum.TryParse(metadata[0][4..5], out PosixFileModes group);
            _ = Enum.TryParse(metadata[0][5..6], out PosixFileModes other);

            if (!Enum.TryParse(metadata[1], true, out GitFileType type))
            {
                throw new ArgumentException($"Invalid file type {metadata[1]}", nameof(fileLine));
            }

            string path = parts[1];
            int lastPos = path.LastIndexOf(RelativeDirectoryPath.GitDirectorySeparatorChar);

            return new GitFileEntry(
                GitFileId.FromHash(metadata[2]),
                type,
                new[] { user, group, other },
                RelativeDirectoryPath.Create(path)
            );
        }
    }
}
