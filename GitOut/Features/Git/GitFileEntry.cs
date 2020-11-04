using System;
using System.Collections.Generic;
using GitOut.Features.IO;

namespace GitOut.Features.Git
{
    public class GitFileEntry
    {
        private GitFileEntry(GitFileId id, GitFileType type, PosixFileModes[] fileModes, string fileName)
        {
            Id = id;
            Type = type;
            FileModes = fileModes;
            FileName = FileName.Create(fileName);
        }

        public GitFileId Id { get; }

        public FileName FileName { get; }

        public IEnumerable<PosixFileModes> FileModes { get; }
        public GitFileType Type { get; }

        public static GitFileEntry Parse(string fileLine)
        {
            string[] parts = fileLine.Split('\t', 2);
            string[] metadata = parts[0].Split(' ', 3);
            Enum.TryParse(metadata[0][3..4], out PosixFileModes user);
            Enum.TryParse(metadata[0][4..5], out PosixFileModes group);
            Enum.TryParse(metadata[0][5..6], out PosixFileModes other);

            if (!Enum.TryParse(metadata[1], true, out GitFileType type))
            {
                throw new ArgumentException($"Invalid file type {metadata[1]}", nameof(fileLine));
            }

            return new GitFileEntry(GitFileId.FromHash(metadata[2]), type, new[] { user, group, other }, parts[1]);
        }
    }
}
