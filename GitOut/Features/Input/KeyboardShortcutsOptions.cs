namespace GitOut.Features.Input;

/// <summary>Persisted options for the keyboard-shortcuts overlay feature.</summary>
public class KeyboardShortcutsOptions
{
    public const string SectionKey = "shortcuts";

    /// <summary>
    /// The key that triggers the shortcuts overlay, stored as the
    /// <see cref="System.Windows.Input.Key"/> enum name (e.g. <c>"OemQuestion"</c> for the <c>?</c> key).
    /// </summary>
    public string HotKey { get; set; } = "OemQuestion";

    /// <summary>
    /// The modifier keys for the hotkey, stored as the <see cref="System.Windows.Input.ModifierKeys"/> flags string
    /// (e.g. <c>"None"</c>, <c>"Control"</c>, <c>"Control, Shift"</c>).
    /// </summary>
    public string Modifiers { get; set; } = "None";
}
