using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using GitOut.Features.IO;
using GitOut.Features.Wpf;

namespace GitOut.Features.Themes
{
    public partial class ThemeSettingsPicker : UserControl
    {
        public static readonly DependencyProperty ThemeSelectedProperty = DependencyProperty.Register(
            "ThemeSelected",
            typeof(ICommand),
            typeof(ThemeSettingsPicker)
        );

        public static readonly DependencyProperty SelectThemeCommandProperty = DependencyProperty.Register(
            "SelectThemeCommand",
            typeof(ICommand),
            typeof(ThemeSettingsPicker)
        );

        public ThemeSettingsPicker()
        {
            InitializeComponent();
            DataContext = new ThemeSettingsViewModel(this);
        }

        public ICommand ThemeSelected
        {
            get => (ICommand)GetValue(ThemeSelectedProperty);
            set => SetValue(ThemeSelectedProperty, value);
        }

        public ICommand SelectThemeCommand
        {
            get { return (ICommand)GetValue(SelectThemeCommandProperty); }
            set { SetValue(SelectThemeCommandProperty, value); }
        }

        private class ThemeSettingsViewModel
        {
            public ThemeSettingsViewModel(ThemeSettingsPicker owner)
            {
                var themes = new ObservableCollection<ThemePaletteViewModel>();
                Themes = CollectionViewSource.GetDefaultView(themes);
                themes.Add(ThemePaletteViewModel.CreateDefaultTheme());
                themes.Add(ThemePaletteViewModel.CreateThemeFromResource(FileName.Create("PaleRed")));
                SelectThemeCommand = new CallbackCommand<ThemePaletteViewModel>(
                    theme =>
                    {
                        if (owner.ThemeSelected != null && owner.ThemeSelected.CanExecute(theme))
                        {
                            owner.ThemeSelected.Execute(theme);
                        }
                    },
                    theme => theme != null);
            }

            public ICollectionView Themes { get; }
            public ICommand SelectThemeCommand { get; }
        }
    }
}
