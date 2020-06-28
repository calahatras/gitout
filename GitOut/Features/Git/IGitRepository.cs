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
        GitHistoryEvent? Head { get; }

        Task<IEnumerable<GitHistoryEvent>> ExecuteLogAsync(LogOptions options);
        Task<GitStatusResult> ExecuteStatusAsync();
        Task<GitDiffResult> ExecuteDiffAsync(GitStatusChange change, DiffOptions options);
        IAsyncEnumerable<GitFileEntry> ExecuteListFilesAsync(GitObjectId id);

        Task ExecuteAddAllAsync();
        Task ExecuteResetAllAsync();
        Task ExecuteAddAsync(GitStatusChange change);
        Task ExecuteResetAsync(GitStatusChange change);

        Task ExecuteApplyAsync(GitPatch patch);
        Task ExecuteCommitAsync(GitCommitOptions options);
    }
}
