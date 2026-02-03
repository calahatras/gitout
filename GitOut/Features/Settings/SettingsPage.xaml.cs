using System.Windows.Controls;

namespace GitOut.Features.Settings;

public partial class SettingsPage : UserControl
{
    public SettingsPage(SettingsViewModel dataContext)
    {
        InitializeComponent();
        DataContext = dataContext;
    }
}
