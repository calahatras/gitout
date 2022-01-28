using GitOut.Features.Diagnostics;
using GitOut.Features.Git.Diagnostics;
using GitOut.Features.Git.Log;
using GitOut.Features.Git.RepositoryList;
using GitOut.Features.Git.Stage;
using GitOut.Features.Git.Storage;
using GitOut.Features.IO;
using Microsoft.Extensions.DependencyInjection;

namespace GitOut.Features.Git
{
    public static class GitServiceCollectionExtensions
    {
        public static void AddGitFeature(this IServiceCollection services)
        {
            services.AddScoped<IGitRepositoryStorage, GitRepositoryStorage>();
            services.AddSingleton<IProcessTelemetryCollector, ProcessTelemetryCollector>();
            services.AddScoped<IProcessFactory<IGitProcess>, GitProcessFactory>();
            services.AddScoped<IGitRepositoryFactory, GitRepositoryFactory>();
            services.AddScoped<IGitRepositoryWatcherProvider, GitRepositoryFileSystemWatcherProvider>();
            services.AddTransient<GitLogPage>();
            services.AddTransient<GitLogViewModel>();
            services.AddTransient<GitStagePage>();
            services.AddTransient<GitStageViewModel>();
            services.AddTransient<RepositoryListPage>();
            services.AddTransient<RepositoryListViewModel>();
        }
    }
}
