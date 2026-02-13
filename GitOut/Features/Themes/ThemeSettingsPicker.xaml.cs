using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using GitOut.Features.IO;

namespace GitOut.Features.Themes;

public partial class ThemeSettingsPicker : UserControl
{
    public static readonly DependencyProperty SelectThemeCommandProperty =
        DependencyProperty.Register(
            nameof(SelectThemeCommand),
            typeof(ICommand),
            typeof(ThemeSettingsPicker)
        );

    public ThemeSettingsPicker()
    {
        InitializeComponent();

        var themes = new ObservableCollection<ThemePaletteViewModel>();
        Themes = CollectionViewSource.GetDefaultView(themes);
        themes.Add(ThemePaletteViewModel.CreateDefaultTheme());
        themes.Add(ThemePaletteViewModel.CreateThemeFromResource(FileName.Create("PaleRed")));
    }

    public ICollectionView Themes { get; }

    public ICommand SelectThemeCommand
    {
        get => (ICommand)GetValue(SelectThemeCommandProperty);
        set => SetValue(SelectThemeCommandProperty, value);
    }
}
