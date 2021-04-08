using System.Collections.Generic;
using System.Threading.Tasks;
using GitOut.Features.Git.Diff;
using GitOut.Features.Git.Patch;
using GitOut.Features.IO;

namespace GitOut.Features.Git
{
    public interface IGitRepository
    {
        DirectoryPath WorkingDirectory { get; }
        string Name { get; }

        GitStatusResult? CachedStatus { get; }

        Task<bool> IsInsideWorkTree();
        Task<GitHistoryEvent> GetHeadAsync();
        IAsyncEnumerable<GitRemote> GetRemotesAsync();

        Task ExecuteFetchAsync(GitRemote remote);
        Task<IEnumerable<GitHistoryEvent>> ExecuteLogAsync(LogOptions options);
        IAsyncEnumerable<GitStash> ExecuteStashListAsync();
        Task<GitStatusResult> ExecuteStatusAsync();
        IAsyncEnumerable<GitDiffFileEntry> ExecuteListDiffChangesAsync(GitObjectId change, GitObjectId? parent, DiffOptions? options = default);
        Task<GitDiffResult> ExecuteDiffAsync(GitFileId source, GitFileId target, DiffOptions options);
        Task<GitDiffResult> ExecuteDiffAsync(RelativeDirectoryPath file, DiffOptions options);
        Task<GitDiffResult> ExecuteUntrackedDiffAsync(RelativeDirectoryPath path);
        IAsyncEnumerable<GitFileEntry> ExecuteListTreeAsync(GitObjectId id, DiffOptions? options = default);
        Task<GitDiffResult> GetFileContentsAsync(GitFileId file);

        Task ExecuteAddAllAsync();
        Task ExecuteResetAllAsync();
        Task ExecuteAddAsync(GitStatusChange change);
        Task ExecuteCheckoutAsync(GitStatusChange change);
        Task ExecuteCheckoutBranchAsync(GitBranchName name);
        Task ExecuteResetAsync(GitStatusChange change);

        Task ExecuteApplyAsync(GitPatch patch);
        Task ExecuteCommitAsync(GitCommitOptions options);
    }
}
