using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace GitOut.Features.Themes
{
    public class ThemeService : IThemeService
    {
        private readonly IList<ResourceDictionary> resourceProviders = new List<ResourceDictionary>();
        private ThemePaletteViewModel currentTheme;

        public ThemeService()
        {
            currentTheme = ThemePaletteViewModel.CreateDefaultTheme();
            SystemParameters.StaticPropertyChanged += OnSystemParameterChanged;
        }

        public void ChangeTheme(ThemePaletteViewModel theme)
        {
            currentTheme = theme;
            UpdateTheme();
        }

        public void RegisterResourceProvider(ResourceDictionary resources)
        {
            resourceProviders.Add(resources);
            UpdateTheme();
        }

        private void OnSystemParameterChanged(object sender, PropertyChangedEventArgs e) => UpdateTheme();

        private void UpdateTheme()
        {
            foreach (ResourceDictionary provider in resourceProviders)
            {
                provider["PrimaryHueMidColor"] = currentTheme.PrimaryHueMidColor;
                provider["PrimaryHueMidForegroundColor"] = Color.FromArgb(255, 255, 255, 255);

                provider["PrimaryHueLightBrush"] = currentTheme.PrimaryHueLightBrush;
                provider["PrimaryHueLightForegroundBrush"] = currentTheme.PrimaryHueLightForegroundBrush;
                provider["PrimaryHueMidBrush"] = currentTheme.PrimaryHueMidBrush;
                provider["PrimaryHueMidForegroundBrush"] = currentTheme.PrimaryHueMidForegroundBrush;
                provider["PrimaryHueDarkBrush"] = currentTheme.PrimaryHueDarkBrush;
                provider["PrimaryHueDarkForegroundBrush"] = currentTheme.PrimaryHueDarkForegroundBrush;
                provider["SecondaryAccentBrush"] = currentTheme.SecondaryAccentBrush;
                provider["SecondaryAccentForegroundBrush"] = currentTheme.SecondaryAccentForegroundBrush;
            }
        }
    }
}
