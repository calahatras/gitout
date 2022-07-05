using Microsoft.Extensions.DependencyInjection;

namespace GitOut.Features.Settings
{
    public static class SettingsServiceCollectionExtensions
    {
        public static void AddSettingsFeature(this IServiceCollection services)
        {
            services.AddTransient<SettingsPage>();
            services.AddTransient<SettingsViewModel>();
        }
    }
}
