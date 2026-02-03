using System;
using System.Threading;
using System.Threading.Tasks;
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
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
