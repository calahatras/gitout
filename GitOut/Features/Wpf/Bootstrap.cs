using System;
using System.Threading;
using System.Threading.Tasks;
using GitOut.Features.Navigation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace GitOut.Features.Wpf
{
    public class Bootstrap : IHostedService
    {
        private readonly INavigationService navigation;
        private readonly NavigationRegistrationOptions options;

        public Bootstrap(INavigationService navigation, IOptions<NavigationRegistrationOptions> options)
        {
            this.navigation = navigation;
            this.options = options.Value;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (options.StartupWindow == null)
            {
                throw new InvalidOperationException("Invalid Startup Window");
            }
            if (options.StartupType == null)
            {
                throw new InvalidOperationException("Invalid Startup Type");
            }
            navigation.Navigate(options.StartupWindow, null);
            navigation.Navigate(options.StartupType, null);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    }
}
