using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GitOut.Features.Git;
using GitOut.Features.Git.Log;
using GitOut.Features.IO;
using GitOut.Features.Navigation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace GitOut.Features.Wpf;

public class Bootstrap : IHostedService
{
    private readonly NavigationRegistrationOptions options;
    private readonly IServiceProvider provider;

    public Bootstrap(IServiceProvider provider, IOptions<NavigationRegistrationOptions> options)
    {
        this.options = options.Value;
        this.provider = provider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (options.StartupType == null)
        {
            throw new InvalidOperationException("Invalid Startup Type");
        }
        if (provider.GetService(typeof(INavigationService)) is INavigationService navigation)
        {
            navigation.Navigate(options.StartupType, null);

            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                string path = Path.GetFullPath(args[1]);
                if (
                    Directory.Exists(path)
                    && provider.GetService(typeof(IGitRepositoryFactory))
                        is IGitRepositoryFactory factory
                )
                {
                    IGitRepository repo = factory.Create(DirectoryPath.Create(path));
                    navigation.Navigate(
                        typeof(GitLogPage).FullName!,
                        GitLogPageOptions.OpenRepository(repo)
                    );
                }
            }
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
