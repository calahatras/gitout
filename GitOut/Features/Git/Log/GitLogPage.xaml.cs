using System;
using System.Windows;
using System.Windows.Controls;
using GitOut.Features.Input;
using Microsoft.Extensions.Options;

namespace GitOut.Features.Git.Log;

public partial class GitLogPage : UserControl
{
    public GitLogPage(
        GitLogViewModel dataContext,
        IOptionsMonitor<KeyboardShortcutsOptions> shortcutsOptions
    )
    {
        InitializeComponent();
        DataContext = dataContext;
        Loaded += OnLoaded;
        new KeyboardShortcutsAdornerController(
            this,
            shortcutsOptions,
            Array.Empty<KeyboardShortcutEntry>()
        );
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => Root.Focus();
}
