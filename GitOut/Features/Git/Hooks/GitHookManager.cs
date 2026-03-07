using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace GitOut.Features.Git.Hooks;

public class GitHookManager : IGitHookManager
{
    private readonly IOptionsMonitor<GitHooksOptions> hooksOptions;

    public GitHookManager(IOptionsMonitor<GitHooksOptions> hooksOptions) => this.hooksOptions = hooksOptions;

    public Task CopyHookAsync(IGitRepository source, IGitRepository target, GitHookType type)
    {
        string sourcePath = GetHookFilePath(source, type);
        string targetPath = GetHookFilePath(target, type);

        if (File.Exists(sourcePath))
        {
            File.Copy(sourcePath, targetPath, true);
        }

        return Task.CompletedTask;
    }

    public async Task<GitHook?> GetHookAsync(IGitRepository repository, GitHookType type)
    {
        string path = GetHookFilePath(repository, type);
        if (!File.Exists(path))
        {
            return null;
        }

        using var reader = new StreamReader(path);
        string content = await reader.ReadToEndAsync();
        return new GitHook(type, content);
    }

    public Task SaveHookAsync(IGitRepository repository, GitHook hook)
    {
        string path = GetHookFilePath(repository, hook.Type);

        string content = hook.Content;
        if (!string.IsNullOrEmpty(content) && !content.StartsWith("#!"))
        {
            string shell = hooksOptions.CurrentValue.PreferredShellPath;
            if (string.IsNullOrEmpty(shell))
            {
                shell = "/usr/bin/env pwsh"; // Default
            }
            content = $"#!{shell}\n\n{content}";
        }

        // Apply placeholders when saving? NO, placeholders are for arguments, the user writes them in the script.
        // Wait, the placeholders should be replaced when the script runs?
        // A git hook is a static file, we can replace the placeholders when we generate it!
        // Git authors, counts, etc might change, but repository name/path are static for this repository.
        // Let's replace the static ones now string.Replace("{RepositoryName}", repository.Name).
        content = GitHookPlaceholderReplacer.Replace(content, repository);

        File.WriteAllText(path, content);

        return Task.CompletedTask;
    }

    private static string GetHookFilePath(IGitRepository repository, GitHookType type)
    {
        string name = string.Concat(
                type.ToString()
                    .Select((x, i) => i > 0 && char.IsUpper(x) ? "-" + x.ToString() : x.ToString())
            )
            .ToLowerInvariant();

        return Path.Combine(repository.WorkingDirectory.Directory, ".git", "hooks", name);
    }
}
