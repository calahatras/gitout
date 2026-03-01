using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GitOut.Features.Git.Diff;
using GitOut.Features.Git.Patch;
using GitOut.Features.IO;

namespace GitOut.Features.Git;

public interface IGitRepository
{
    DirectoryPath WorkingDirectory { get; }
    string Name { get; }

    GitStatusResult? CachedStatus { get; }

    Task<bool> IsInsideWorkTree();
    Task<GitCommitId?> GetCommitIdAsync(string reference);
    Task<GitHistoryEvent> GetHeadAsync();
    IAsyncEnumerable<GitRemote> GetRemotesAsync();

    Task FetchAsync(GitRemote remote);
    Task PruneRemoteAsync(GitRemote remote);
    Task<IEnumerable<GitHistoryEvent>> LogAsync(LogOptions options);
    IAsyncEnumerable<GitStash> StashListAsync();
    Task<GitStatusResult> StatusAsync();
    IAsyncEnumerable<GitDiffFileEntry> ListDiffChangesAsync(
        GitObjectId change,
        GitObjectId? parent,
        DiffOptions? options = default
    );
    Task<GitDiffResult> DiffAsync(GitFileId source, GitFileId target, DiffOptions options);
    Task<GitDiffResult> DiffAsync(RelativeDirectoryPath file, DiffOptions options);
    Task<GitDiffResult> DiffAsync(
        RelativeDirectoryPath source,
        RelativeDirectoryPath destination,
        DiffOptions options
    );
    IAsyncEnumerable<GitFileEntry> ListTreeAsync(GitObjectId id, DiffOptions? options = default);
    Stream GetUntrackedBlobStream(RelativeDirectoryPath path);
    Task<Stream> GetBlobStreamAsync(GitFileId file);

    Task AddAllAsync();
    Task ResetAllAsync();
    Task ResetToCommitAsync(GitCommitId id);
    Task AddAsync(GitStatusChange change, AddOptions options);
    Task CheckoutAsync(GitStatusChange change);
    Task CreateBranchAsync(GitBranchName name, GitCreateBranchOptions? options = default);
    Task<GitDeleteResult> DeleteBranchAsync(
        GitBranchName name,
        GitDeleteBranchOptions? options = default
    );
    Task CheckoutCommitDetachedAsync(GitCommitId id);
    Task CheckoutBranchAsync(GitBranchName name, GitCheckoutBranchOptions? options = default);
    Task ResetAsync(GitStatusChange change);
    Task RestoreAsync(GitStatusChange change);
    Task ApplyAsync(GitPatch patch);
    Task StashIndexAsync();
    Task CommitAsync(GitCommitOptions options);
    Task RestoreWorkspaceAsync(GitStatusChange change);
}
