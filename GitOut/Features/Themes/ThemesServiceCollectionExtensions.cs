using Microsoft.Extensions.DependencyInjection;

namespace GitOut.Features.Themes
{
    public static class ThemesServiceCollectionExtensions
    {
        public static void AddThemeFeature(this IServiceCollection services) => services.AddSingleton<IThemeService, ThemeService>();
    }
}
