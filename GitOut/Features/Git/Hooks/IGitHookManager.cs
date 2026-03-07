using System.Threading.Tasks;

namespace GitOut.Features.Git.Hooks;

public interface IGitHookManager
{
    Task<GitHook?> GetHookAsync(IGitRepository repository, GitHookType type);
    Task SaveHookAsync(IGitRepository repository, GitHook hook);
    Task CopyHookAsync(IGitRepository source, IGitRepository target, GitHookType type);
}
