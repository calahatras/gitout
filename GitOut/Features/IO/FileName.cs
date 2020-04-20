using System;
using System.IO;
using System.Linq;

namespace GitOut.Features.IO
{
    public class FileName
    {
        private FileName(string filename)
        {
            char[] invalidCharacters = Path.GetInvalidFileNameChars();
            if (filename.Any(invalidCharacters.Contains))
            {
                throw new ArgumentException("File name contains invalid characters", nameof(filename));
            }

            Name = filename;
        }

        public string Name { get; }

        public static FileName Create(string path) => new FileName(path);
    }
}
