using System.Collections.Generic;
using System.Threading.Tasks;
using GitOut.Features.IO;

namespace GitOut.Features.Git
{
    public interface IGitRepository
    {
        DirectoryPath WorkingDirectory { get; }
        string? Name { get; }

        GitStatusResult? CachedStatus { get; }

        Task<IEnumerable<GitHistoryEvent>> ExecuteLogAsync(LogOptions options);
        Task<GitHistoryEvent> GetHeadAsync();
        IAsyncEnumerable<GitStash> ExecuteStashListAsync();
        Task<GitStatusResult> ExecuteStatusAsync();
        IAsyncEnumerable<GitDiffFileEntry> ExecuteListDiffChangesAsync(GitObjectId change, GitObjectId? parent);
        Task<GitDiffResult> ExecuteDiffAsync(GitObjectId source, GitObjectId target, DiffOptions options);
        Task<GitDiffResult> ExecuteDiffAsync(RelativeDirectoryPath file, DiffOptions options);
        IAsyncEnumerable<GitFileEntry> ExecuteListFilesAsync(GitObjectId id);
        Task<string[]> GetFileContentsAsync(GitFileId file);

        Task ExecuteAddAllAsync();
        Task ExecuteResetAllAsync();
        Task ExecuteAddAsync(GitStatusChange change);
        Task ExecuteResetAsync(GitStatusChange change);

        Task ExecuteApplyAsync(GitPatch patch);
        Task ExecuteCommitAsync(GitCommitOptions options);
    }
}
