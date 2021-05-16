using System.IO;
using System.Threading.Tasks;
using GitOut.Features.IO;

namespace GitOut.Features.Git.Diff
{
    public class DiffContext
    {
        private DiffContext(FileInfo info, GitDiffResult result)
        {
            FileExtension = info.Extension;
            Result = result;
        }

        private DiffContext(FileInfo info, GitDiffResult result, Stream stream)
            : this(info, result) => Stream = stream;

        public string FileExtension { get; }

        public GitDiffResult Result { get; }
        public Stream? Stream { get; }

        public static async Task<DiffContext> DiffAsync(IGitRepository repository, GitStatusChange change, IDiffOptionsBuilder options)
        {
            var info = new FileInfo(change.Path.Directory);
            if (change.Type == GitStatusChangeType.Untracked)
            {
                return new DiffContext(info, await repository.UntrackedDiffAsync(change.Path));
            }
            GitDiffResult result = change.Type == GitStatusChangeType.RenamedOrCopied && change.SourceId! != change.DestinationId!
                ? await repository.DiffAsync(change.SourceId!, change.DestinationId!, options.Build())
                : await repository.DiffAsync(change.Path, options.Build());
            if (!result.IsBinary || change.DestinationId is null)
            {
                return new DiffContext(info, result);
            }
            Stream? contents = await repository.GetBlobStreamAsync(change.DestinationId);
            return new DiffContext(info, result, contents);
        }

        public static async Task<DiffContext> DiffFileAsync(IGitRepository repository, RelativeDirectoryPath directory, FileName file, GitFileId sourceId, GitFileId? destinationId)
        {
            var info = new FileInfo(Path.Combine(repository.WorkingDirectory.Directory, directory.ToString(), file.ToString()));
            return destinationId is null
                ? new DiffContext(info, await repository.GetFileContentsAsync(sourceId))
                : new DiffContext(info, await repository.DiffAsync(sourceId, destinationId, DiffOptions.Builder().Build()));
        }
    }
}
