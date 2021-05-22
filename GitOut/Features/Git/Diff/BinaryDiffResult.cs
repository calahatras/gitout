using System.IO;

namespace GitOut.Features.Git.Diff
{
    public class BinaryDiffResult
    {
        public BinaryDiffResult(Stream stream) => Stream = stream;

        public Stream Stream { get; }
    }

}
