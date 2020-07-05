using System;
using System.Collections.Generic;

namespace GitOut.Features.Git
{
    public class GitFileEntry
    {
        private GitFileEntry(GitFileId id, GitFileType type, PosixFileModes[] fileModes, string fileName)
        {
            Id = id;
            Type = type;
            FileModes = fileModes;
            FileName = fileName;
        }

        public GitFileId Id { get; }

        public string FileName { get; }

        public IEnumerable<PosixFileModes> FileModes { get; }
        public GitFileType Type { get; }

        public static GitFileEntry Parse(string fileLine)
        {
            string[] parts = fileLine.Split('\t', 2);
            string[] metadata = parts[0].Split(' ', 3);
            Enum.TryParse(metadata[0].Substring(3, 1), out PosixFileModes user);
            Enum.TryParse(metadata[0].Substring(4, 1), out PosixFileModes group);
            Enum.TryParse(metadata[0].Substring(5, 1), out PosixFileModes other);

            if (!Enum.TryParse(metadata[1], true, out GitFileType type))
            {
                throw new ArgumentException($"Invalid file type {metadata[1]}", nameof(fileLine));
            }

            return new GitFileEntry(GitFileId.FromHash(metadata[2]), type, new[] { user, group, other }, parts[1]);
        }
    }
}
