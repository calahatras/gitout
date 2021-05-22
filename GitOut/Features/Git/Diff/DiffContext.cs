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
            GitDiffResult result,
            IGitRepository repository,
            GitFileId? source,
            GitFileId? destination
        )
        {
            FileExtension = info.Extension;
            Result = result;
            this.repository = repository;
            SourceId = source;
            DestinationId = destination;
        }

        public string FileExtension { get; }

        public GitFileId? SourceId { get; }
        public GitFileId? DestinationId { get; }

        public GitDiffResult Result { get; }

        public Task<Stream> GetSourceStreamAsync() => SourceId is null
            ? throw new InvalidOperationException("Cannot get stream for empty source")
            : repository.GetBlobStreamAsync(SourceId);

        public static async Task<DiffContext> DiffAsync(IGitRepository repository, GitStatusChange change, DiffOptions options)
        {
            var info = new FileInfo(change.Path.Directory);
            if (change.Type == GitStatusChangeType.Untracked)
            {
                return new DiffContext(
                    info,
                    GitDiffResult.Builder().Feed(repository.GetUntrackedBlobStream(change.Path), GitStatusChangeType.Untracked).Build(),
                    repository,
                    change.SourceId,
                    change.DestinationId
                );
            }
            GitDiffResult result = change.Type == GitStatusChangeType.RenamedOrCopied && change.SourceId! != change.DestinationId!
                ? await repository.DiffAsync(change.SourceId!, change.DestinationId!, options)
                : await repository.DiffAsync(change.Path, options);
            return new DiffContext(info, result, repository, change.SourceId, change.DestinationId);
        }

        public static async Task<DiffContext> SnapshotFileAsync(
            IGitRepository repository,
            RelativeDirectoryPath directory,
            FileName file,
            GitFileId sourceId
        ) => new DiffContext(
            new FileInfo(Path.Combine(repository.WorkingDirectory.Directory, directory.ToString(), file.ToString())),
            GitDiffResult.Builder().Feed(await repository.GetBlobStreamAsync(sourceId)).Build(),
            repository,
            sourceId,
            null
        );

        public static async Task<DiffContext> DiffFileAsync(
            IGitRepository repository,
            RelativeDirectoryPath directory,
            FileName file,
            GitFileId sourceId,
            GitFileId destinationId
        ) => new DiffContext(
            new FileInfo(Path.Combine(repository.WorkingDirectory.Directory, directory.ToString(), file.ToString())),
            await repository.DiffAsync(sourceId, destinationId, DiffOptions.Builder().Build()),
            repository,
            sourceId,
            destinationId
        );
    }
}
