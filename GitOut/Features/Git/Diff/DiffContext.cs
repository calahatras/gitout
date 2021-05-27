using System;
using System.IO;
using System.Threading.Tasks;
using GitOut.Features.IO;

namespace GitOut.Features.Git.Diff
{
    public class DiffContext
    {
        private readonly IGitRepository repository;

        private DiffContext(
            FileInfo info,
            IGitRepository repository,
            GitFileId? source,
            GitFileId? destination
        )
        {
            FileExtension = info.Extension;
            this.repository = repository;
            SourceId = source;
            DestinationId = destination;
        }

        private DiffContext(
            FileInfo info,
            BinaryDiffResult result,
            IGitRepository repository,
            GitFileId? source,
            GitFileId? destination
        ) : this(info, repository, source, destination) => Blob = result;

        private DiffContext(
            FileInfo info,
            TextDiffResult result,
            IGitRepository repository,
            GitFileId? source,
            GitFileId? destination
        ) : this(info, repository, source, destination) => Text = result;

        public string FileExtension { get; }

        public GitFileId? SourceId { get; }
        public GitFileId? DestinationId { get; }

        public BinaryDiffResult? Blob { get; }
        public TextDiffResult? Text { get; }

        public Task<Stream> GetSourceStreamAsync() => SourceId is null
            ? throw new InvalidOperationException("Cannot get stream for empty source")
            : repository.GetBlobStreamAsync(SourceId);

        public static async Task<DiffContext> DiffAsync(
            IGitRepository repository,
            GitStatusChange change,
            DiffOptions options
        ) => CreateFromResult(
            repository,
            new FileInfo(change.Path.Directory),
            change.Type == GitStatusChangeType.Untracked
            ? GitDiffResult.Builder().Feed(repository.GetUntrackedBlobStream(change.Path), GitStatusChangeType.Untracked).Build()
            : change.Type == GitStatusChangeType.RenamedOrCopied && change.SourceId! != change.DestinationId!
                ? await repository.DiffAsync(change.SourceId!, change.DestinationId!, options)
                : await repository.DiffAsync(change.Path, options),
            change.SourceId,
            change.DestinationId
        );

        public static async Task<DiffContext> SnapshotFileAsync(
            IGitRepository repository,
            RelativeDirectoryPath directory,
            FileName file,
            GitFileId sourceId
        ) => CreateFromResult(
            repository,
            new FileInfo(Path.Combine(repository.WorkingDirectory.Directory, directory.ToString(), file.ToString())),
            GitDiffResult.Builder().Feed(await repository.GetBlobStreamAsync(sourceId)).Build(),
            sourceId,
            null
        );

        public static async Task<DiffContext> DiffFileAsync(
            IGitRepository repository,
            RelativeDirectoryPath directory,
            FileName file,
            GitFileId sourceId,
            GitFileId destinationId
        ) => CreateFromResult(
            repository,
            new FileInfo(Path.Combine(repository.WorkingDirectory.Directory, directory.ToString(), file.ToString())),
            await repository.DiffAsync(sourceId, destinationId, DiffOptions.Builder().Build()),
            sourceId,
            destinationId
        );

        private static DiffContext CreateFromResult(
            IGitRepository repository,
            FileInfo info,
            GitDiffResult result,
            GitFileId? sourceId,
            GitFileId? destinationId
        ) => result.Text is null
            ? new DiffContext(
                info,
                new BinaryDiffResult(result.Blob!.Stream, repository, sourceId),
                repository,
                sourceId,
                destinationId
            )
            : new DiffContext(
                info,
                result.Text,
                repository,
                sourceId,
                destinationId
            );
    }

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

        public Stream GetBaseStream()
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
