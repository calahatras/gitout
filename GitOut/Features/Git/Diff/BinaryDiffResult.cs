using System;
using System.IO;
using System.Threading.Tasks;

namespace GitOut.Features.Git.Diff
{
    public class BinaryDiffResult
    {
        private readonly Stream destinationStream;
        private readonly IGitRepository repository;
        private readonly GitFileId? sourceId;

        private Stream? sourceStream;

        public BinaryDiffResult(Stream destinationStream, IGitRepository repository, GitFileId? sourceId)
        {
            this.destinationStream = destinationStream;
            this.repository = repository;
            this.sourceId = sourceId;
        }

#pragma warning disable CA1024 // Use properties where appropriate
        public Stream GetBaseStream()
#pragma warning restore CA1024 // Use properties where appropriate
        {
            destinationStream.Position = 0;
            return destinationStream;
        }

        public async Task<Stream> GetSourceStreamAsync()
        {
            if (sourceStream is null)
            {
                if (sourceId is null)
                {
                    throw new InvalidOperationException("Cannot get stream for empty source");
                }
                sourceStream = await repository.GetBlobStreamAsync(sourceId);
            }
            sourceStream.Position = 0;
            return sourceStream;
        }
    }
}
