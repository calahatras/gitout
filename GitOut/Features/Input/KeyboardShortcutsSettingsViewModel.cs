using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using GitOut.Features.Options;
using Microsoft.Extensions.Options;

namespace GitOut.Features.Input;

/// <summary>
/// ViewModel that exposes the keyboard-shortcuts overlay hotkey as an editable setting.
/// Reads from and persists to <see cref="KeyboardShortcutsOptions"/> via the options infrastructure.
/// </summary>
public sealed class KeyboardShortcutsSettingsViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IOptionsWriter<KeyboardShortcutsOptions> writer;
    private readonly IDisposable? subscription;
    private Key hotKey = Key.OemQuestion;
    private ModifierKeys modifiers = ModifierKeys.None;

    public KeyboardShortcutsSettingsViewModel(
        IOptionsMonitor<KeyboardShortcutsOptions> options,
        IOptionsWriter<KeyboardShortcutsOptions> writer
    )
    {
        this.writer = writer;
        hotKey = ParseKey(options.CurrentValue.HotKey);
        modifiers = ParseModifiers(options.CurrentValue.Modifiers);
        // Keep in sync when another process or the settings UI writes a change.
        subscription = options.OnChange(o =>
        {
            _ = SetProperty(ref hotKey, ParseKey(o.HotKey), nameof(HotKey));
            _ = SetProperty(ref modifiers, ParseModifiers(o.Modifiers), nameof(Modifiers));
        });
    }

    /// <summary>The key that triggers the shortcuts overlay.</summary>
    public Key HotKey
    {
        get => hotKey;
        set
        {
            if (SetProperty(ref hotKey, value))
                writer.Update(s => s.HotKey = value.ToString());
        }
    }

    /// <summary>The modifier keys (Ctrl, Alt, Shift) that accompany <see cref="HotKey"/>.</summary>
    public ModifierKeys Modifiers
    {
        get => modifiers;
        set
        {
            if (SetProperty(ref modifiers, value))
                writer.Update(s => s.Modifiers = value.ToString());
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Dispose() => subscription?.Dispose();

    /// <summary>
    /// Parses a <see cref="Key"/> from its enum name, falling back to <see cref="Key.OemQuestion"/>
    /// when the stored string is unknown.
    /// </summary>
    internal static Key ParseKey(string name) =>
        Enum.TryParse(name, out Key key) ? key : Key.OemQuestion;

    /// <summary>
    /// Parses <see cref="ModifierKeys"/> from its flags string (e.g. <c>"Control, Shift"</c>),
    /// falling back to <see cref="ModifierKeys.None"/> when the stored string is unknown.
    /// </summary>
    internal static ModifierKeys ParseModifiers(string name) =>
        Enum.TryParse(name, out ModifierKeys m) ? m : ModifierKeys.None;

    private bool SetProperty<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null
    )
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
