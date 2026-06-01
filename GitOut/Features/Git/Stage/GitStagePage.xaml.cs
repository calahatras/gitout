using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GitOut.Features.Input;
using Microsoft.Extensions.Options;

namespace GitOut.Features.Git.Stage;

public partial class GitStagePage : UserControl
{
    private static readonly IReadOnlyList<KeyboardShortcutEntry> Shortcuts =
        new KeyboardShortcutEntry[]
        {
            new(Key.S, "Stage file", "Workspace"),
            new(Key.R, "Discard changes", "Workspace"),
            new(Key.I, "Mark as intent-to-add", "Workspace"),
            new(Key.R, "Unstage file", "Index"),
            new(Key.S, "Stage selection", "Diff"),
            new(Key.R, "Reset selection", "Diff"),
            new(Key.E, "Edit hunk", "Diff"),
            new(Key.OemComma, "Previous workspace file", "Navigation", ModifierKeys.Control),
            new(Key.OemPeriod, "Next workspace file", "Navigation", ModifierKeys.Control),
            new(
                Key.OemComma,
                "Previous index file",
                "Navigation",
                ModifierKeys.Control | ModifierKeys.Shift
            ),
            new(
                Key.OemPeriod,
                "Next index file",
                "Navigation",
                ModifierKeys.Control | ModifierKeys.Shift
            ),
            new(
                Key.C,
                "Focus commit message",
                "Navigation",
                ModifierKeys.Control | ModifierKeys.Shift
            ),
            new(Key.Return, "Commit", "Commit", ModifierKeys.Shift),
        };

    public GitStagePage(
        GitStageViewModel dataContext,
        IOptionsMonitor<KeyboardShortcutsOptions> shortcutsOptions
    )
    {
        InitializeComponent();
        DataContext = dataContext;
        Loaded += OnLoaded;
        new KeyboardShortcutsAdornerController(this, shortcutsOptions, Shortcuts);
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => Focus();
}
