using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GitOut.Features.IO
{
    public class RelativeDirectoryPath
    {
        public const char GitDirectorySeparatorChar = '/';
        public static readonly RelativeDirectoryPath Root = new(string.Empty);

        private readonly Lazy<RelativeDirectoryPath> parent;

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
            string[] segments = Directory.Trim(GitDirectorySeparatorChar).Split(GitDirectorySeparatorChar);
            parent = new Lazy<RelativeDirectoryPath>(() => segments[..^1].Length == 0 ? Root : new RelativeDirectoryPath(string.Join(GitDirectorySeparatorChar, segments[..^1])));
            Segments = segments;
            Name = FileName.Create(segments[^1] ?? string.Empty);
        }

        public FileName Name { get; }
        public string Directory { get; }
        public IReadOnlyCollection<string> Segments { get; }
        public RelativeDirectoryPath Parent => parent.Value;

        public override string ToString() => Directory;

        public static RelativeDirectoryPath Create(string path) => new(path);
        internal RelativeDirectoryPath Append(string directoryName) => new(Path.Combine(Directory, directoryName));
    }
}
