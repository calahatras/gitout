using System.Windows;
using System.Windows.Input;

namespace GitOut.Features.Input;

/// <summary>
/// A small control that displays the current hotkey (with optional modifiers) and lets the user
/// record a new combination by clicking "CHANGE" and then pressing any key.
/// Escape cancels recording without changing anything.
/// </summary>
public partial class KeyRecorderControl : System.Windows.Controls.UserControl
{
    /// <summary>The currently configured hotkey. Supports two-way binding.</summary>
    public static readonly DependencyProperty HotKeyProperty = DependencyProperty.Register(
        nameof(HotKey),
        typeof(Key),
        typeof(KeyRecorderControl),
        new FrameworkPropertyMetadata(
            Key.OemQuestion,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            static (d, _) => ((KeyRecorderControl)d).UpdateDisplay()
        )
    );

    /// <summary>The modifier keys that accompany <see cref="HotKey"/>. Supports two-way binding.</summary>
    public static readonly DependencyProperty HotModifiersProperty = DependencyProperty.Register(
        nameof(HotModifiers),
        typeof(ModifierKeys),
        typeof(KeyRecorderControl),
        new FrameworkPropertyMetadata(
            ModifierKeys.None,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            static (d, _) => ((KeyRecorderControl)d).UpdateDisplay()
        )
    );

    public KeyRecorderControl()
    {
        InitializeComponent();
        UpdateDisplay();
    }

    /// <summary>The currently configured hotkey.</summary>
    public Key HotKey
    {
        get => (Key)GetValue(HotKeyProperty);
        set => SetValue(HotKeyProperty, value);
    }

    /// <summary>Modifier keys (Ctrl, Alt, Shift) that must be held alongside <see cref="HotKey"/>.</summary>
    public ModifierKeys HotModifiers
    {
        get => (ModifierKeys)GetValue(HotModifiersProperty);
        set => SetValue(HotModifiersProperty, value);
    }

    private void OnChangeClick(object sender, RoutedEventArgs e) => StartRecording();

    private void OnCancelClick(object sender, RoutedEventArgs e) => StopRecording(null);

    private void StartRecording()
    {
        KeyDisplay.Text = "Press a key…";
        ChangeButton.Visibility = Visibility.Collapsed;
        CancelButton.Visibility = Visibility.Visible;

        if (Window.GetWindow(this) is Window window)
        {
            window.PreviewKeyDown += OnWindowPreviewKeyDown;
        }
    }

    private void StopRecording(Window? window)
    {
        ChangeButton.Visibility = Visibility.Visible;
        CancelButton.Visibility = Visibility.Collapsed;
        UpdateDisplay();

        Window? w = window ?? Window.GetWindow(this);
        w?.PreviewKeyDown -= OnWindowPreviewKeyDown;
    }

    private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Resolve the real key when the event is fired for a system key.
        Key key = e.Key == Key.System ? e.SystemKey : e.Key;

        // Ignore standalone modifier keys — the user must press a non-modifier key.
        if (
            key
            is Key.LeftCtrl
                or Key.RightCtrl
                or Key.LeftAlt
                or Key.RightAlt
                or Key.LeftShift
                or Key.RightShift
                or Key.LWin
                or Key.RWin
                or Key.System
        )
        {
            // Update the display to show the held modifiers as a hint, e.g. "Ctrl+Shift+…"
            string modPart = KeyboardShortcutsAdorner.ModifiersLabel(e.KeyboardDevice.Modifiers);
            KeyDisplay.Text = modPart.Length == 0 ? "Press a key…" : $"{modPart}+…";
            return;
        }

        StopRecording(sender as Window);

        // Escape cancels without changing the hotkey.
        if (key != Key.Escape)
        {
            HotKey = key;
            HotModifiers = e.KeyboardDevice.Modifiers;
        }

        e.Handled = true;
    }

    /// <summary>Updates the TextBox label from the current <see cref="HotKey"/> and <see cref="HotModifiers"/>.</summary>
    private void UpdateDisplay() =>
        // KeyboardShortcutsAdorner.FullLabel is internal static — same namespace, same assembly.
        KeyDisplay.Text = KeyboardShortcutsAdorner.FullLabel(HotModifiers, HotKey);
}
