using GitOut.Features.Wpf;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GitOut.Features.Navigation
{
    public static class NavigationServiceCollectionExtensions
    {
        public static void AddNavigationServiceWithStartPage<T>(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<INavigationService, NavigationService>();
            services.AddScoped<NavigatorShellViewModel>();

            services.AddOptions<NavigationRegistrationOptions>().Configure(options => options.StartupType = typeof(T).FullName!);
            services.AddOptions<NavigationWindowOptions>().Bind(configuration.GetSection(NavigationWindowOptions.SectionKey));
        }
    }
}
