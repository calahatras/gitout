using System.IO;
using System.Threading.Tasks;
using GitOut.Features.IO;

namespace GitOut.Features.Git.Diff;

public class DiffContext
{
    private DiffContext(FileInfo info, GitFileId? source, GitFileId? destination)
    {
        FileExtension = info.Extension;
        SourceId = source;
        DestinationId = destination;
    }

    private DiffContext(
        FileInfo info,
        BinaryDiffResult result,
        GitFileId? source,
        GitFileId? destination
    )
        : this(info, source, destination) => Blob = result;

    private DiffContext(
        FileInfo info,
        TextDiffResult result,
        GitFileId? source,
        GitFileId? destination
    )
        : this(info, source, destination) => Text = result;

    public string FileExtension { get; }

    public GitFileId? SourceId { get; }
    public GitFileId? DestinationId { get; }

    public BinaryDiffResult? Blob { get; }
    public TextDiffResult? Text { get; }

    public static async Task<DiffContext> DiffAsync(
        IGitRepository repository,
        GitStatusChange change,
        DiffOptions options
    ) =>
        CreateFromResult(
            repository,
            new FileInfo(change.Path.Directory),
            change.Type == GitStatusChangeType.Untracked
                    ? GitDiffResult
                        .Builder()
                        .Feed(
                            repository.GetUntrackedBlobStream(change.Path),
                            GitStatusChangeType.Untracked
                        )
                        .Build()
                : change.Type == GitStatusChangeType.RenamedOrCopied
                && change.SourceId! != change.DestinationId!
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
    ) =>
        CreateFromResult(
            repository,
            new FileInfo(
                Path.Combine(
                    repository.WorkingDirectory.Directory,
                    directory.ToString(),
                    file.ToString()
                )
            ),
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
    ) =>
        CreateFromResult(
            repository,
            new FileInfo(
                Path.Combine(
                    repository.WorkingDirectory.Directory,
                    directory.ToString(),
                    file.ToString()
                )
            ),
            await repository.DiffAsync(sourceId, destinationId, DiffOptions.Builder().Build()),
            sourceId,
            destinationId
        );

    public static async Task<DiffContext> DiffFilesAsync(
        IGitRepository repository,
        RelativeDirectoryPath source,
        RelativeDirectoryPath destination
    ) =>
        CreateFromResult(
            repository,
            new FileInfo(
                Path.Combine(repository.WorkingDirectory.Directory, destination.ToString())
            ),
            await repository.DiffAsync(source, destination, DiffOptions.Builder().Build()),
            null,
            null
        );

    private static DiffContext CreateFromResult(
        IGitRepository repository,
        FileInfo info,
        GitDiffResult result,
        GitFileId? sourceId,
        GitFileId? destinationId
    ) =>
        result.Text is null
            ? new DiffContext(
                info,
                new BinaryDiffResult(result.Blob!.Stream, repository, sourceId),
                sourceId,
                destinationId
            )
            : new DiffContext(info, result.Text, sourceId, destinationId);
}
