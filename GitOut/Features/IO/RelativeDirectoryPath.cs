using System;
using System.IO;
using System.Linq;

namespace GitOut.Features.IO
{
    public class RelativeDirectoryPath
    {
        private RelativeDirectoryPath(string path)
        {
            char[] invalidCharacters = Path.GetInvalidPathChars();
            if (path.Any(invalidCharacters.Contains))
            {
                throw new ArgumentException("Path contains invalid characters", nameof(path));
            }
            if (Path.IsPathRooted(path))
            {
                throw new ArgumentException("Path needs to be relative", nameof(path));
            }

            Directory = path;
        }

        public string Directory { get; }

        public override string ToString() => Directory;

        public static RelativeDirectoryPath Create(string path) => new RelativeDirectoryPath(path);
    }
}
