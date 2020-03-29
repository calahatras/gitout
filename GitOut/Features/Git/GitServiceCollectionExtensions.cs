using GitOut.Features.Git.Log;
using GitOut.Features.Menu;
using Microsoft.Extensions.DependencyInjection;

namespace GitOut.Features.Git
{
    public static class GitServiceCollectionExtensions
    {
        public static void AddGitFeature(this IServiceCollection services, IMenuItemCollection menu)
        {
            services.AddScoped<GitLogPage>();
            services.AddScoped<GitLogViewModel>();
            menu.Add(new MenuItemContext
            {
                PageName = typeof(GitLogPage).FullName,
                Name = "Log",
                Icon = "source-commit"
            });
        }
    }
}
