using System.Windows.Input;

namespace GitOut.Features.Input;

/// <summary>
/// Describes a single keyboard shortcut for display in the <see cref="KeyboardShortcutsAdorner"/>.
/// </summary>
/// <param name="Key">The keyboard key to display.</param>
/// <param name="Description">A short human-readable description of what the key does.</param>
/// <param name="Category">Groups related shortcuts under a shared heading in the overlay.</param>
/// <param name="Modifiers">Optional modifier keys (Ctrl, Shift, Alt) required alongside <paramref name="Key"/>.</param>
/// <example>
/// <code>
/// new KeyboardShortcutEntry(Key.S, "Stage file", "Workspace")
/// new KeyboardShortcutEntry(Key.OemComma, "Previous file", "Navigation", ModifierKeys.Control)
/// </code>
/// </example>
public sealed record KeyboardShortcutEntry(
    Key Key,
    string Description,
    string Category,
    ModifierKeys Modifiers = ModifierKeys.None
);
