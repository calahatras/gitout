using System.Windows;

namespace GitOut.Features.Themes;

public interface IThemeService
{
    void ChangeTheme(ThemePaletteViewModel theme);
    void RegisterResourceProvider(ResourceDictionary resources);
}
