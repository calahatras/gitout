using System;
using System.IO;
using System.Linq;

namespace GitOut.Features.IO
{
    public class DirectoryPath
    {
        private DirectoryPath(string directory)
        {
            char[] invalidCharacters = Path.GetInvalidPathChars();
            if (directory.Any(invalidCharacters.Contains))
            {
                throw new ArgumentException("Path contains invalid characters", nameof(directory));
            }
            if (!Path.IsPathRooted(directory))
            {
                throw new ArgumentException("Path needs to be rooted", nameof(directory));
            }

            Directory = directory;
        }

        public string Directory { get; }

        public override string ToString() => Directory;

        public static DirectoryPath Create(string path) => new DirectoryPath(path);
    }
}
