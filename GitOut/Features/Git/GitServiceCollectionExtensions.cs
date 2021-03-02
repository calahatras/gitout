using GitOut.Features.Diagnostics;
using GitOut.Features.Git.Diagnostics;
using GitOut.Features.Git.Log;
using GitOut.Features.Git.RepositoryList;
using GitOut.Features.Git.Stage;
using GitOut.Features.Git.Storage;
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
            services.AddScoped<GitLogPage>();
            services.AddScoped<GitLogViewModel>();
            services.AddScoped<GitStagePage>();
            services.AddScoped<GitStageViewModel>();
            services.AddScoped<RepositoryListPage>();
            services.AddScoped<RepositoryListViewModel>();
        }
    }
}
