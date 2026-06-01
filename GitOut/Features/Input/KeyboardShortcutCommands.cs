using System.Collections.Generic;
using System.Windows.Input;

namespace GitOut.Features.Input;

/// <summary>
/// Application-wide routed commands related to keyboard shortcut discovery.
/// Pages that support the shortcuts overlay register a <see cref="CommandBinding"/>
/// for <see cref="ShowShortcuts"/> on their root element.
/// </summary>
public static class KeyboardShortcutCommands
{
    /// <summary>
    /// Toggles the keyboard shortcuts overlay for the currently active page.
    /// A page that handles this command should add/remove a <see cref="KeyboardShortcutsAdorner"/>
    /// from its <see cref="System.Windows.Documents.AdornerLayer"/>.
    /// Pages that do not register a <see cref="CommandBinding"/> for this command will cause
    /// <see cref="RoutedCommand.CanExecute"/> to return <see langword="false"/>, which lets
    /// the toolbar button disable itself automatically.
    /// </summary>
    public static readonly RoutedCommand ShowShortcuts = new(
        nameof(ShowShortcuts),
        typeof(KeyboardShortcutCommands)
    );

    /// <summary>
    /// Global shortcuts defined on <c>NavigatorShell</c> that apply to every page.
    /// <see cref="KeyboardShortcutsAdornerController"/> appends these to each page's adorner automatically.
    /// </summary>
    public static readonly IReadOnlyList<KeyboardShortcutEntry> GlobalShortcuts =
        new KeyboardShortcutEntry[]
        {
            new(Key.P, "Open Settings", "Global", ModifierKeys.Control),
            new(Key.Left, "Navigate Back", "Global", ModifierKeys.Alt),
            new(Key.F11, "Toggle Full Screen", "Global"),
        };
}
