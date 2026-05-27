using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GitOut.Features.Input;

/// <summary>
/// Attached behavior that wires up a <see cref="KeyboardShortcutsAdornerController"/> for a
/// <see cref="UserControl"/> page purely from XAML, without requiring constructor-injected DI
/// services in code-behind.
/// </summary>
/// <remarks>
/// <para>
/// Usage — on the page root and on each <see cref="KeyBinding"/> that should appear in the overlay:
/// <code>
/// &lt;UserControl xmlns:input="clr-namespace:GitOut.Features.Input"
///              input:KeyboardShortcutsBehavior.Register="True"&gt;
///   &lt;UserControl.InputBindings&gt;
///     &lt;KeyBinding Key="S" Command="..."
///       input:KeyboardShortcutsBehavior.Description="Stage file"
///       input:KeyboardShortcutsBehavior.Category="Workspace" /&gt;
///   &lt;/UserControl.InputBindings&gt;
///   ...
///   &lt;ListView&gt;
///     &lt;ListView.InputBindings&gt;
///       &lt;KeyBinding Key="S" Command="..."
///         input:KeyboardShortcutsBehavior.Description="Stage selection"
///         input:KeyboardShortcutsBehavior.Category="Diff" /&gt;
///     &lt;/ListView.InputBindings&gt;
///   &lt;/ListView&gt;
/// &lt;/UserControl&gt;
/// </code>
/// </para>
/// <para>
/// When <c>Register</c> is set to <see langword="true"/> on a <see cref="UserControl"/>, the
/// behavior hooks <c>Loaded</c>. On each load it walks the logical tree to collect every
/// <see cref="KeyBinding"/> that has a non-empty <c>Description</c> attached, builds the
/// <see cref="KeyboardShortcutEntry"/> list and creates a
/// <see cref="KeyboardShortcutsAdornerController"/> — resolving
/// <see cref="IOptionsMonitor{TOptions}"/> from <see cref="App.Services"/> so pages do not need
/// to inject it through their constructors.
/// </para>
/// </remarks>
public static class KeyboardShortcutsBehavior
{
    // ── Attached properties set on individual <KeyBinding> elements ────────

    /// <summary>
    /// A short human-readable description of what the key binding does.
    /// When non-empty, the binding is included in the keyboard shortcuts overlay.
    /// </summary>
    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.RegisterAttached(
            "Description",
            typeof(string),
            typeof(KeyboardShortcutsBehavior),
            new PropertyMetadata(string.Empty)
        );

    /// <summary>
    /// The category heading under which this binding appears in the overlay.
    /// If omitted, falls back to <c>"Other"</c>.
    /// </summary>
    public static readonly DependencyProperty CategoryProperty =
        DependencyProperty.RegisterAttached(
            "Category",
            typeof(string),
            typeof(KeyboardShortcutsBehavior),
            new PropertyMetadata("Other")
        );

    // ── Attached property set on the UserControl root element ──────────────

    /// <summary>
    /// When set to <see langword="true"/> on a <see cref="UserControl"/>, the behavior
    /// registers the keyboard shortcuts overlay for that page automatically on <c>Loaded</c>.
    /// </summary>
    public static readonly DependencyProperty RegisterProperty =
        DependencyProperty.RegisterAttached(
            "Register",
            typeof(bool),
            typeof(KeyboardShortcutsBehavior),
            new PropertyMetadata(false, OnRegisterChanged)
        );

    public static string GetDescription(DependencyObject obj) =>
        (string)obj.GetValue(DescriptionProperty);

    public static void SetDescription(DependencyObject obj, string value) =>
        obj.SetValue(DescriptionProperty, value);

    public static string GetCategory(DependencyObject obj) =>
        (string)obj.GetValue(CategoryProperty);

    public static void SetCategory(DependencyObject obj, string value) =>
        obj.SetValue(CategoryProperty, value);

    public static bool GetRegister(DependencyObject obj) => (bool)obj.GetValue(RegisterProperty);

    public static void SetRegister(DependencyObject obj, bool value) =>
        obj.SetValue(RegisterProperty, value);

    // ── Behavior implementation ────────────────────────────────────────────

    private static void OnRegisterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UserControl page)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            page.Loaded += OnPageFirstLoaded;
        }
    }

    /// <summary>
    /// Fires once on the first <c>Loaded</c> event. Walks the logical tree to collect
    /// annotated <see cref="KeyBinding"/>s, then creates the controller (which re-subscribes
    /// to <c>Loaded</c>/<c>Unloaded</c> itself for subsequent navigation events).
    /// </summary>
    private static void OnPageFirstLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not UserControl page)
        {
            return;
        }

        // Only wire up once — the controller manages its own Loaded/Unloaded from here on.
        page.Loaded -= OnPageFirstLoaded;

        List<KeyboardShortcutEntry> entries = CollectEntries(page);

        IOptionsMonitor<KeyboardShortcutsOptions> options = App.Services.GetRequiredService<
            IOptionsMonitor<KeyboardShortcutsOptions>
        >();

        // The controller self-registers on Loaded/Unloaded; no reference needs to be stored.
        _ = new KeyboardShortcutsAdornerController(page, options, entries);
    }

    /// <summary>
    /// Walks the logical tree rooted at <paramref name="root"/> and returns one
    /// <see cref="KeyboardShortcutEntry"/> for every <see cref="KeyBinding"/> that has a
    /// non-empty <c>Description</c> attached, preserving document order.
    /// </summary>
    private static List<KeyboardShortcutEntry> CollectEntries(UserControl root)
    {
        var entries = new List<KeyboardShortcutEntry>();
        CollectFromElement(root, entries);
        return entries;
    }

    private static void CollectFromElement(
        DependencyObject element,
        List<KeyboardShortcutEntry> entries
    )
    {
        // Collect from this element's own InputBindings (if it has any).
        if (element is UIElement ui)
        {
            foreach (InputBinding binding in ui.InputBindings)
            {
                if (binding is KeyBinding kb && GetDescription(kb) is { Length: > 0 } description)
                {
                    entries.Add(
                        new KeyboardShortcutEntry(
                            kb.Key,
                            description,
                            GetCategory(kb),
                            kb.Modifiers
                        )
                    );
                }
            }
        }

        // Recurse into logical children so bindings on nested ListViews, TextBoxes etc. are found.
        foreach (object child in LogicalTreeHelper.GetChildren(element))
        {
            if (child is DependencyObject childDo)
            {
                CollectFromElement(childDo, entries);
            }
        }
    }
}
