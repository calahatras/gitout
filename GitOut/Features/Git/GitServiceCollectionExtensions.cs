using GitOut.Features.Diagnostics;
using GitOut.Features.Git.CherryPick;
using GitOut.Features.Git.Diagnostics;
using GitOut.Features.Git.Hooks;
using GitOut.Features.Git.Log;
using GitOut.Features.Git.RepositoryList;
using GitOut.Features.Git.Stage;
using GitOut.Features.Git.Storage;
using GitOut.Features.IO;
using Microsoft.Extensions.DependencyInjection;

namespace GitOut.Features.Git;

public static class GitServiceCollectionExtensions
{
    public static void AddGitFeature(this IServiceCollection services)
    {
        _ = services.AddScoped<IGitRepositoryStorage, GitRepositoryStorage>();
        _ = services.AddSingleton<IProcessTelemetryCollector, ProcessTelemetryCollector>();
        _ = services.AddScoped<IProcessFactory<IGitProcess>, GitProcessFactory>();
        _ = services.AddScoped<IGitRepositoryFactory, GitRepositoryFactory>();
        _ = services.AddScoped<
            IGitRepositoryWatcherProvider,
            GitRepositoryFileSystemWatcherProvider
        >();
        _ = services.AddScoped<IShellProvider, SystemShellProvider>();
        _ = services.AddScoped<IGitHookManager, GitHookManager>();
        _ = services.AddTransient<GitHooksPage>();
        _ = services.AddTransient<GitHooksViewModel>();
        _ = services.AddTransient<GitLogPage>();
        _ = services.AddTransient<GitLogViewModel>();
        _ = services.AddTransient<GitStagePage>();
        _ = services.AddTransient<GitStageViewModel>();
        _ = services.AddTransient<RepositoryListPage>();
        _ = services.AddTransient<RepositoryListViewModel>();
        _ = services.AddTransient<CherryPickOptionsPage>();
        _ = services.AddTransient<CherryPickOptionsViewModel>();
    }
}
