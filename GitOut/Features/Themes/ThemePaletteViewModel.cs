using System;
using System.Windows;
using System.Windows.Media;
using GitOut.Features.IO;

namespace GitOut.Features.Themes;

public class ThemePaletteViewModel
{
    private ThemePaletteViewModel(
        string name,
        Color primaryHueMidColor,
        Color primaryHueMidForegroundColor,
        Brush primaryHueLightBrush,
        Brush primaryHueLightForegroundBrush,
        Brush primaryHueMidBrush,
        Brush primaryHueMidForegroundBrush,
        Brush primaryHueDarkBrush,
        Brush primaryHueDarkForegroundBrush,
        Brush secondaryAccentBrush,
        Brush secondaryAccentForegroundBrush
    )
    {
        Name = name;
        PrimaryHueMidColor = primaryHueMidColor;
        PrimaryHueMidForegroundColor = primaryHueMidForegroundColor;
        PrimaryHueLightBrush = primaryHueLightBrush;
        PrimaryHueLightForegroundBrush = primaryHueLightForegroundBrush;
        PrimaryHueMidBrush = primaryHueMidBrush;
        PrimaryHueMidForegroundBrush = primaryHueMidForegroundBrush;
        PrimaryHueDarkBrush = primaryHueDarkBrush;
        PrimaryHueDarkForegroundBrush = primaryHueDarkForegroundBrush;
        SecondaryAccentBrush = secondaryAccentBrush;
        SecondaryAccentForegroundBrush = secondaryAccentForegroundBrush;
    }

    public string Name { get; }
    public Color PrimaryHueMidColor { get; } = SystemColors.WindowColor;
    public Color PrimaryHueMidForegroundColor { get; }
    public Brush PrimaryHueLightBrush { get; }
    public Brush PrimaryHueLightForegroundBrush { get; }
    public Brush PrimaryHueMidBrush { get; }
    public Brush PrimaryHueMidForegroundBrush { get; }
    public Brush PrimaryHueDarkBrush { get; }
    public Brush PrimaryHueDarkForegroundBrush { get; }
    public Brush SecondaryAccentBrush { get; }
    public Brush SecondaryAccentForegroundBrush { get; }

    public static ThemePaletteViewModel CreateDefaultTheme() =>
        new(
            "Default",
            SystemParameters.WindowGlassColor,
            Colors.White,
            SystemParameters.WindowGlassBrush,
            Brushes.White,
            SystemParameters.WindowGlassBrush,
            Brushes.White,
            SystemParameters.WindowGlassBrush,
            Brushes.White,
            SystemParameters.WindowGlassBrush,
            Brushes.White
        );

    public static ThemePaletteViewModel CreateThemeFromResource(FileName resourceName)
    {
        var uri = new Uri(
            $"pack://application:,,,/Themes/variants/AppTheme.{resourceName.Name}.xaml",
            UriKind.RelativeOrAbsolute
        );
        var resource = new ResourceDictionary() { Source = uri };
        return new ThemePaletteViewModel(
            resourceName.Name,
            (Color)resource["PrimaryHueMidColor"],
            (Color)resource["PrimaryHueMidForegroundColor"],
            (Brush)resource["PrimaryHueLightBrush"],
            (Brush)resource["PrimaryHueLightForegroundBrush"],
            (Brush)resource["PrimaryHueMidBrush"],
            (Brush)resource["PrimaryHueMidForegroundBrush"],
            (Brush)resource["PrimaryHueDarkBrush"],
            (Brush)resource["PrimaryHueDarkForegroundBrush"],
            (Brush)resource["SecondaryAccentBrush"],
            (Brush)resource["SecondaryAccentForegroundBrush"]
        );
    }
}
