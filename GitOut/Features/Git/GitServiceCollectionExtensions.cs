using GitOut.Features.Git.Log;
using GitOut.Features.Git.RepositoryList;
using GitOut.Features.Git.Storage;
using GitOut.Features.Menu;
using Microsoft.Extensions.DependencyInjection;

namespace GitOut.Features.Git
{
    public static class GitServiceCollectionExtensions
    {
        public static void AddGitFeature(this IServiceCollection services, IMenuItemCollection menu)
        {
            services.AddScoped<IGitRepositoryStorage, GitRepositoryStorage>();
            services.AddScoped<GitLogPage>();
            services.AddScoped<GitLogViewModel>();
            services.AddScoped<RepositoryListPage>();
            services.AddScoped<RepositoryListViewModel>();
            menu.Add(new MenuItemContext
            {
                PageName = typeof(RepositoryListPage).FullName,
                Name = "Repositories",
                IconResourceKey = "SourceCommit"
            });
        }
    }
}
