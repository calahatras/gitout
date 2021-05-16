using System.Collections.Generic;
using System.IO;
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

        Task FetchAsync(GitRemote remote);
        Task<IEnumerable<GitHistoryEvent>> LogAsync(LogOptions options);
        IAsyncEnumerable<GitStash> StashListAsync();
        Task<GitStatusResult> StatusAsync();
        IAsyncEnumerable<GitDiffFileEntry> ListDiffChangesAsync(GitObjectId change, GitObjectId? parent, DiffOptions? options = default);
        Task<GitDiffResult> DiffAsync(GitFileId source, GitFileId target, DiffOptions options);
        Task<GitDiffResult> DiffAsync(RelativeDirectoryPath file, DiffOptions options);
        Task<GitDiffResult> UntrackedDiffAsync(RelativeDirectoryPath path);
        IAsyncEnumerable<GitFileEntry> ListTreeAsync(GitObjectId id, DiffOptions? options = default);
        Task<Stream> GetBlobStreamAsync(GitFileId file);
        Task<GitDiffResult> GetFileContentsAsync(GitFileId file);

        Task AddAllAsync();
        Task ResetAllAsync();
        Task AddAsync(GitStatusChange change, AddOptions options);
        Task CheckoutAsync(GitStatusChange change);
        Task CheckoutBranchAsync(GitBranchName name);
        Task ResetAsync(GitStatusChange change);

        Task ApplyAsync(GitPatch patch);
        Task CommitAsync(GitCommitOptions options);
    }
}
