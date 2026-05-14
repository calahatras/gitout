using GitOut.Features.Options;
using GitOut.Features.Wpf;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GitOut.Features.Navigation;

public static class NavigationServiceCollectionExtensions
{
    public static void AddNavigationServiceWithStartPage<T>(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        _ = services.AddScoped<INavigationService, NavigationService>();
        _ = services.AddScoped<NavigatorShellViewModel>();

        _ = services
            .AddOptions<NavigationRegistrationOptions>()
            .Configure(options => options.StartupType = typeof(T).FullName!);
        services
            .AddWritableOptions<NavigationWindowOptions>()
            .Bind(configuration, NavigationWindowOptions.SectionKey);
    }
}
