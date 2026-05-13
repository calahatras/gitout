using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Microsoft.Extensions.Options;

namespace GitOut.Features.Input;

/// <summary>
/// Manages the keyboard-shortcuts overlay adorner for a <see cref="UserControl"/> page.
/// <para>
/// Construct once in the page's constructor — the controller self-registers on the page's
/// <c>Loaded</c> and <c>Unloaded</c> events to manage its own subscription lifetime, so no
/// further teardown code is needed in the page.
/// </para>
/// <para>
/// Global <see cref="NavigatorShell"/> shortcuts from <see cref="KeyboardShortcutCommands.GlobalShortcuts"/>
/// are automatically appended after the page-specific shortcuts, so callers never have to add them manually.
/// </para>
/// </summary>
/// <example>
/// Minimal usage in a page constructor (no page-specific shortcuts):
/// <code>
/// new KeyboardShortcutsAdornerController(this, shortcutsOptions, []);
/// </code>
/// With page-specific shortcuts:
/// <code>
/// new KeyboardShortcutsAdornerController(this, shortcutsOptions, new KeyboardShortcutEntry[]
/// {
///     new(Key.S, "Stage file", "Workspace"),
///     new(Key.R, "Discard changes", "Workspace"),
/// });
/// </code>
/// </example>
internal sealed class KeyboardShortcutsAdornerController
{
    private readonly UserControl owner;
    private readonly IOptionsMonitor<KeyboardShortcutsOptions> options;

    // Page-specific + global shortcuts merged once at construction time.
    private readonly IReadOnlyList<KeyboardShortcutEntry> shortcuts;

    private KeyboardShortcutsAdorner? adorner;
    private KeyBinding? keyBinding;
    private IDisposable? subscription;

    // Stored so the same delegate instance can be passed to both AddHandler and RemoveHandler.
    private readonly KeyEventHandler onEscapePressed;

    /// <param name="owner">The page that will host the adorner.</param>
    /// <param name="options">Options monitor supplying the user-configured hotkey.</param>
    /// <param name="pageShortcuts">
    /// Page-specific shortcuts to show above the global ones.
    /// Pass an empty array for pages that have no page-specific shortcuts.
    /// </param>
    public KeyboardShortcutsAdornerController(
        UserControl owner,
        IOptionsMonitor<KeyboardShortcutsOptions> options,
        IReadOnlyList<KeyboardShortcutEntry> pageShortcuts
    )
    {
        this.owner = owner;
        this.options = options;

        // Merge: page shortcuts first, global shortcuts last.
        shortcuts =
            pageShortcuts.Count == 0
                ? KeyboardShortcutCommands.GlobalShortcuts
                : (IReadOnlyList<KeyboardShortcutEntry>)
                    pageShortcuts.Concat(KeyboardShortcutCommands.GlobalShortcuts).ToArray();

        onEscapePressed = OnEscapePressed;

        // Registers the RoutedCommand so the NavigatorShell toolbar button routes to this page.
        owner.CommandBindings.Add(
            new CommandBinding(KeyboardShortcutCommands.ShowShortcuts, OnToggle)
        );
        owner.Loaded += OnLoaded;
        owner.Unloaded += OnUnloaded;

        // Apply an initial hotkey binding before first Loaded (it may already be shown).
        ApplyCurrentHotkey();
    }

    private void OnToggle(object sender, ExecutedRoutedEventArgs e)
    {
        if (adorner is null)
            ShowAdorner();
        else
            HideAdorner();
    }

    private void ShowAdorner()
    {
        AdornerLayer? layer = AdornerLayer.GetAdornerLayer(owner);
        if (layer is null)
            return;

        adorner = new KeyboardShortcutsAdorner(owner, shortcuts);
        adorner.DismissRequested += OnDismissRequested;
        layer.Add(adorner);

        // Handle Escape as a dismiss gesture while the adorner is visible.
        owner.AddHandler(UIElement.PreviewKeyDownEvent, onEscapePressed);

        // The toolbar button that invoked this command steals focus from the page.
        // Restore it so the hotkey and Escape handler keep working.
        FocusOwner();
    }

    private void HideAdorner()
    {
        if (adorner is null)
            return;

        AdornerLayer? layer = AdornerLayer.GetAdornerLayer(owner);
        layer?.Remove(adorner);
        adorner.DismissRequested -= OnDismissRequested;
        adorner = null;

        owner.RemoveHandler(UIElement.PreviewKeyDownEvent, onEscapePressed);

        // Return focus to the page so the hotkey can be pressed again immediately.
        FocusOwner();
    }

    /// <summary>
    /// Ensures keyboard focus is within <see cref="owner"/> so that <see cref="InputBinding"/>s
    /// and the Escape <c>PreviewKeyDown</c> handler fire correctly.
    /// This is needed both on initial load (non-focusable root elements) and after a toolbar
    /// button click moves focus out of the page's subtree.
    /// </summary>
    private void FocusOwner()
    {
        if (!owner.IsKeyboardFocusWithin)
            owner.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
    }

    private void OnDismissRequested(object? sender, EventArgs e) => HideAdorner();

    private void OnEscapePressed(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            HideAdorner();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Subscribe first so no OnChange notification is missed between subscribing and reading.
        subscription = options.OnChange(_ => owner.Dispatcher.Invoke(ApplyCurrentHotkey));

        // Re-read CurrentValue in case options changed while this page was in the background
        // (e.g. user opened Settings, changed the hotkey, then navigated back).
        ApplyCurrentHotkey();

        // Ensure keyboard focus is somewhere inside this page so InputBindings can fire.
        // Pages with Focusable="True" (e.g. GitStagePage) will have already captured focus
        // in their own Loaded handler, which runs before ours because it is subscribed first.
        // For pages whose root element is a non-focusable container (e.g. a Grid), focus falls
        // through to the Window and the KeyBinding on the page is never reachable via routing.
        if (!owner.IsKeyboardFocusWithin)
            FocusOwner();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        // If the user navigates away while the overlay is open, dismiss it cleanly.
        HideAdorner();

        // Dispose the live-update subscription; it will be renewed the next time Loaded fires.
        subscription?.Dispose();
        subscription = null;
    }

    private void ApplyCurrentHotkey()
    {
        if (keyBinding is not null)
            owner.InputBindings.Remove(keyBinding);

        keyBinding = new KeyBinding(
            KeyboardShortcutCommands.ShowShortcuts,
            KeyboardShortcutsSettingsViewModel.ParseKey(options.CurrentValue.HotKey),
            KeyboardShortcutsSettingsViewModel.ParseModifiers(options.CurrentValue.Modifiers)
        );
        owner.InputBindings.Add(keyBinding);
    }
}
