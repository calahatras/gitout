using System;

namespace GitOut.Features.Git.Hooks;

public static class GitHookPlaceholderReplacer
{
    public static string Replace(string input, IGitRepository repository) => string.IsNullOrEmpty(input)
            ? input
            : input
            .Replace("{RepositoryName}", repository.Name)
            .Replace("{RepositoryPath}", repository.WorkingDirectory.Directory)
            .Replace(
                "{GitAuthor}",
                Environment.GetEnvironmentVariable("GIT_AUTHOR_NAME") ?? "Unknown"
            );
}
