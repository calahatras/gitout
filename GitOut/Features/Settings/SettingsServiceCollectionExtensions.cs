using GitOut.Features.Menu;
using Microsoft.Extensions.DependencyInjection;

namespace GitOut.Features.Settings
{
    public static class SettingsServiceCollectionExtensions
    {
        public static void AddSettingsFeature(this IServiceCollection services, IMenuItemCollection menu)
        {
            services.AddScoped<SettingsPage>();
            services.AddScoped<SettingsViewModel>();
            menu.Add(new MenuItemContext
            {
                PageName = typeof(SettingsPage).FullName,
                Name = "Inställningar",
                IconResourceKey = "cog"
            });
        }
    }
}
