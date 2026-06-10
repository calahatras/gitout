using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace GitOut.Features.Input;

/// <summary>
/// A WPF adorner that renders a centred keyboard-shortcuts help overlay on top of any <see cref="UIElement"/>.
/// Colors are resolved from the application's active resource dictionary so the overlay
/// automatically matches the current theme.
/// Add it to the element's <see cref="AdornerLayer"/> to show the overlay; remove it to dismiss.
/// </summary>
/// <example>
/// <code>
/// var adorner = new KeyboardShortcutsAdorner(root, [
///     new(Key.J, "Next commit",     "Navigation"),
///     new(Key.K, "Previous commit", "Navigation"),
///     new(Key.S, "Stage file",      "Staging"),
/// ]);
/// AdornerLayer.GetAdornerLayer(root)!.Add(adorner);    // show
/// AdornerLayer.GetAdornerLayer(root)!.Remove(adorner); // hide
/// </code>
/// </example>
public sealed class KeyboardShortcutsAdorner : Adorner
{
    // ── Layout constants (device-independent pixels) ──────────────────────
    private const double PanelPadding = 24;
    private const double PanelCornerRadius = 10;
    private const double ColumnGap = 48;
    private const double RowHeight = 28;
    private const double CategorySpacing = 14;
    private const double CategoryTitleGap = 6;
    private const double BadgePaddingH = 8;
    private const double BadgePaddingV = 3;
    private const double BadgeCornerRadius = 4;
    private const double BadgeDescriptionGap = 10;
    private const double TitleBottomGap = 16;

    // ── Font sizes ────────────────────────────────────────────────────────
    private const double TitleFontSize = 15;
    private const double CategoryFontSize = 11;
    private const double KeyFontSize = 11;
    private const double DescriptionFontSize = 12;

    // Matches the font family used by NormalTextStyle throughout the application.
    private static readonly FontFamily Roboto = new("Roboto");

    private readonly IReadOnlyList<KeyboardShortcutEntry> shortcuts;

    // Cached during OnRender so OnMouseDown can determine whether the click hit the scrim.
    private Rect panelRect = Rect.Empty;

    /// <summary>
    /// Raised when the user clicks the scrim area outside the panel, requesting dismissal.
    /// The controller subscribes to this event to remove the adorner from the layer.
    /// </summary>
    public event EventHandler? DismissRequested;

    /// <param name="adornedElement">The element this adorner is attached to.</param>
    /// <param name="shortcuts">The shortcut entries to display, grouped by <see cref="KeyboardShortcutEntry.Category"/>.</param>
    public KeyboardShortcutsAdorner(
        UIElement adornedElement,
        IReadOnlyList<KeyboardShortcutEntry> shortcuts
    )
        : base(adornedElement)
    {
        this.shortcuts = shortcuts;

        // Capture mouse events so the scrim absorbs clicks instead of letting them fall through.
        IsHitTestVisible = true;
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);

        // Only dismiss when clicking outside the panel (i.e. on the scrim background).
        // A click on the panel itself does nothing — users may still be reading the shortcuts.
        if (!panelRect.IsEmpty && !panelRect.Contains(e.GetPosition(this)))
        {
            e.Handled = true;
            DismissRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override void OnRender(DrawingContext dc)
    {
        Size size = AdornedElement.RenderSize;
        double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        // Resolve all theme-aware colors once per render pass. Using TryFindResource walks
        // the visual tree resource dictionaries and picks up the active theme automatically.
        ThemeColors theme = ResolveTheme();

        // ── Scrim ─────────────────────────────────────────────────────────
        dc.DrawRectangle(theme.Scrim, null, new Rect(size));

        // ── Group & split entries ─────────────────────────────────────────
        // Preserve the insertion order of categories.
        var groups = shortcuts.GroupBy(s => s.Category).ToList();

        int leftCount = (groups.Count + 1) / 2;
        var leftGroups = groups.Take(leftCount).ToList();
        var rightGroups = groups.Skip(leftCount).ToList();

        // ── Measure to size the panel ─────────────────────────────────────
        double leftColWidth = MeasureColumnWidth(leftGroups, theme, dpi);
        double rightColWidth = MeasureColumnWidth(rightGroups, theme, dpi);
        double colsWidth = leftColWidth + (rightGroups.Count > 0 ? ColumnGap + rightColWidth : 0);

        FormattedText titleText = BuildText(
            "Keyboard Shortcuts",
            TitleFontSize,
            FontWeights.SemiBold,
            theme.Accent,
            dpi
        );
        double panelWidth = Math.Max(colsWidth, titleText.Width) + (PanelPadding * 2);

        double leftColHeight = MeasureColumnHeight(leftGroups, theme, dpi);
        double rightColHeight = MeasureColumnHeight(rightGroups, theme, dpi);
        double contentHeight = Math.Max(leftColHeight, rightColHeight);
        double panelHeight =
            PanelPadding + titleText.Height + TitleBottomGap + contentHeight + PanelPadding;

        // ── Panel ─────────────────────────────────────────────────────────
        double panelX = (size.Width - panelWidth) / 2;
        double panelY = (size.Height - panelHeight) / 2;

        // Cache so OnMouseDown can distinguish scrim clicks from panel clicks.
        panelRect = new Rect(panelX, panelY, panelWidth, panelHeight);

        dc.DrawRoundedRectangle(
            theme.Panel,
            null,
            new Rect(panelX, panelY, panelWidth, panelHeight),
            PanelCornerRadius,
            PanelCornerRadius
        );

        // ── Title ─────────────────────────────────────────────────────────
        dc.DrawText(
            titleText,
            new Point(panelX + ((panelWidth - titleText.Width) / 2), panelY + PanelPadding)
        );

        // ── Columns ───────────────────────────────────────────────────────
        double contentY = panelY + PanelPadding + titleText.Height + TitleBottomGap;
        double leftX = panelX + PanelPadding;

        RenderColumn(dc, leftGroups, leftX, contentY, theme, dpi);
        if (rightGroups.Count > 0)
        {
            RenderColumn(dc, rightGroups, leftX + leftColWidth + ColumnGap, contentY, theme, dpi);
        }
    }

    // ── Theme resolution ──────────────────────────────────────────────────

    /// <summary>
    /// Resolves all brushes and pens from the application's active resource dictionary.
    /// Falls back to sensible dark-theme defaults when a resource is not found.
    /// </summary>
    private ThemeColors ResolveTheme()
    {
        // MaterialGray900 (#212121) with high alpha → opaque dark scrim
        Brush scrim = BuildScrimBrush(ResolveBrush("MaterialGray900"));

        // MaterialGray800 (#424242) — same background used for cards and dialogs
        Brush panel = ResolveBrush("MaterialGray800", Color.FromRgb(66, 66, 66));

        // MaterialGray700 (#616161) — slightly lighter, makes badges pop against the panel
        Brush badgeFill = ResolveBrush("MaterialGray700", Color.FromRgb(97, 97, 97));

        // MaterialGray500 (#9e9e9e) — subtle visible border on the badge
        Brush badgeBorderBrush = ResolveBrush("MaterialGray500", Color.FromRgb(158, 158, 158));
        var badgeBorder = new Pen(badgeBorderBrush, 1);

        // PrimaryHueLightBrush — accent colour from AppTheme.xaml, used for headings
        Brush accent = ResolveBrush("PrimaryHueLightBrush", Color.FromRgb(195, 94, 255));

        // MaterialForegroundBase (#ffffff) — primary text colour
        Brush text = ResolveBrush("MaterialForegroundBase", Colors.White);

        return new ThemeColors(scrim, panel, badgeFill, badgeBorder, accent, text);
    }

    /// <summary>Looks up a <see cref="Brush"/> resource, returning a fallback <see cref="SolidColorBrush"/> if not found.</summary>
    private Brush ResolveBrush(string key, Color fallbackColor) =>
        TryFindResource(key) is Brush found ? found : new SolidColorBrush(fallbackColor);

    /// <summary>Looks up a <see cref="Brush"/> resource, returning transparent black if not found.</summary>
    private Brush ResolveBrush(string key) =>
        TryFindResource(key) is Brush found ? found : Brushes.Black;

    /// <summary>
    /// Takes the base color of a brush and returns a new semi-transparent brush suitable for the scrim.
    /// This makes the scrim match the theme's dark background rather than being a fixed dark color.
    /// </summary>
    private static SolidColorBrush BuildScrimBrush(Brush baseBrush)
    {
        Color baseColor = baseBrush is SolidColorBrush solid
            ? solid.Color
            : Color.FromRgb(33, 33, 33);

        return new SolidColorBrush(Color.FromArgb(210, baseColor.R, baseColor.G, baseColor.B));
    }

    // ── Rendering helpers ─────────────────────────────────────────────────

    private static void RenderColumn(
        DrawingContext dc,
        List<IGrouping<string, KeyboardShortcutEntry>> groups,
        double x,
        double startY,
        ThemeColors theme,
        double dpi
    )
    {
        double y = startY;
        foreach (IGrouping<string, KeyboardShortcutEntry> group in groups)
        {
            // Category heading in accent colour, uppercased for visual hierarchy.
            FormattedText categoryText = BuildText(
                group.Key.ToUpperInvariant(),
                CategoryFontSize,
                FontWeights.Bold,
                theme.Accent,
                dpi
            );
            dc.DrawText(categoryText, new Point(x, y));
            y += categoryText.Height + CategoryTitleGap;

            foreach (KeyboardShortcutEntry entry in group)
            {
                double midY = y + (RowHeight / 2);

                // ── Key badges (modifier badges + key badge) ───────────────
                double badgesWidth = RenderBadges(dc, entry, x, midY, theme, dpi, render: true);

                // ── Description ────────────────────────────────────────────
                FormattedText descText = BuildText(
                    entry.Description,
                    DescriptionFontSize,
                    FontWeights.Normal,
                    theme.Text,
                    dpi
                );
                dc.DrawText(
                    descText,
                    new Point(x + badgesWidth + BadgeDescriptionGap, midY - (descText.Height / 2))
                );

                y += RowHeight;
            }

            y += CategorySpacing;
        }
    }

    /// <summary>
    /// Renders (or measures) all badges for a shortcut entry (modifier badges followed by the key badge).
    /// When <paramref name="render"/> is <see langword="false"/>, nothing is drawn and only the total width is returned.
    /// </summary>
    private static double RenderBadges(
        DrawingContext dc,
        KeyboardShortcutEntry entry,
        double x,
        double midY,
        ThemeColors theme,
        double dpi,
        bool render
    )
    {
        double offsetX = x;
        foreach (string label in BadgeLabels(entry))
        {
            // "+" separator drawn between consecutive badges
            if (offsetX > x)
            {
                FormattedText plus = BuildText(
                    "+",
                    KeyFontSize,
                    FontWeights.Normal,
                    theme.Text,
                    dpi
                );
                if (render)
                {
                    dc.DrawText(plus, new Point(offsetX + 2, midY - (plus.Height / 2)));
                }
                offsetX += plus.Width + 4;
            }

            FormattedText badgeText = BuildText(
                label,
                KeyFontSize,
                FontWeights.SemiBold,
                theme.Text,
                dpi
            );
            double bw = badgeText.Width + (BadgePaddingH * 2);
            double bh = badgeText.Height + (BadgePaddingV * 2);

            if (render)
            {
                var rect = new Rect(offsetX, midY - (bh / 2), bw, bh);
                dc.DrawRoundedRectangle(
                    theme.BadgeFill,
                    theme.BadgeBorder,
                    rect,
                    BadgeCornerRadius,
                    BadgeCornerRadius
                );
                dc.DrawText(
                    badgeText,
                    new Point(offsetX + BadgePaddingH, midY - (badgeText.Height / 2))
                );
            }

            offsetX += bw;
        }
        return offsetX - x;
    }

    /// <summary>Returns the ordered badge labels for an entry, e.g. ["Ctrl", "Shift", ","].</summary>
    private static IEnumerable<string> BadgeLabels(KeyboardShortcutEntry entry)
    {
        if (entry.Modifiers.HasFlag(ModifierKeys.Control))
        {
            yield return "Ctrl";
        }
        if (entry.Modifiers.HasFlag(ModifierKeys.Alt))
        {
            yield return "Alt";
        }
        if (entry.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            yield return "Shift";
        }
        yield return KeyLabel(entry.Key);
    }

    /// <summary>
    /// Returns a human-readable modifier prefix such as <c>"Ctrl+Alt"</c>, or an empty string
    /// when <paramref name="modifiers"/> is <see cref="ModifierKeys.None"/>.
    /// </summary>
    internal static string ModifiersLabel(ModifierKeys modifiers)
    {
        if (modifiers == ModifierKeys.None)
        {
            return string.Empty;
        }

        var parts = new List<string>(4);
        if ((modifiers & ModifierKeys.Control) != 0)
        {
            parts.Add("Ctrl");
        }
        if ((modifiers & ModifierKeys.Alt) != 0)
        {
            parts.Add("Alt");
        }
        if ((modifiers & ModifierKeys.Shift) != 0)
        {
            parts.Add("Shift");
        }
        if ((modifiers & ModifierKeys.Windows) != 0)
        {
            parts.Add("Win");
        }
        return string.Join("+", parts);
    }

    /// <summary>
    /// Returns a combined display label such as <c>"Ctrl+Shift+?"</c> or just <c>"?"</c>
    /// when there are no modifiers.
    /// </summary>
    internal static string FullLabel(ModifierKeys modifiers, Key key)
    {
        string keyPart = KeyLabel(key);
        string modPart = ModifiersLabel(modifiers);
        return modPart.Length == 0 ? keyPart : $"{modPart}+{keyPart}";
    }

    // ── Measurement helpers ───────────────────────────────────────────────

    private static double MeasureColumnWidth(
        List<IGrouping<string, KeyboardShortcutEntry>> groups,
        ThemeColors theme,
        double dpi
    )
    {
        double maxWidth = 0;
        foreach (IGrouping<string, KeyboardShortcutEntry> group in groups)
        {
            double catWidth = BuildText(
                group.Key.ToUpperInvariant(),
                CategoryFontSize,
                FontWeights.Bold,
                theme.Accent,
                dpi
            ).Width;
            maxWidth = Math.Max(maxWidth, catWidth);

            foreach (KeyboardShortcutEntry entry in group)
            {
                // Measure all badges without rendering, then add description width.
                double badgesWidth = RenderBadges(null!, entry, 0, 0, theme, dpi, render: false);
                FormattedText descText = BuildText(
                    entry.Description,
                    DescriptionFontSize,
                    FontWeights.Normal,
                    theme.Text,
                    dpi
                );
                double rowWidth = badgesWidth + BadgeDescriptionGap + descText.Width;
                maxWidth = Math.Max(maxWidth, rowWidth);
            }
        }
        return maxWidth;
    }

    private static double MeasureColumnHeight(
        List<IGrouping<string, KeyboardShortcutEntry>> groups,
        ThemeColors theme,
        double dpi
    )
    {
        double height = 0;
        foreach (IGrouping<string, KeyboardShortcutEntry> group in groups)
        {
            height +=
                BuildText(
                    group.Key.ToUpperInvariant(),
                    CategoryFontSize,
                    FontWeights.Bold,
                    theme.Accent,
                    dpi
                ).Height + CategoryTitleGap;
            height += group.Count() * RowHeight;
            height += CategorySpacing;
        }
        return height;
    }

    // ── Text factory ──────────────────────────────────────────────────────

    private static FormattedText BuildText(
        string text,
        double fontSize,
        FontWeight weight,
        Brush brush,
        double pixelsPerDip
    ) =>
        new(
            text,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            new Typeface(Roboto, FontStyles.Normal, weight, FontStretches.Normal),
            fontSize,
            brush,
            pixelsPerDip
        );

    // ── Key label mapping ─────────────────────────────────────────────────

    /// <summary>Returns a short, human-friendly label for a <see cref="Key"/> value.</summary>
    internal static string KeyLabel(Key key) =>
        key switch
        {
            Key.Escape => "Esc",
            Key.Return => "Enter",
            Key.Space => "Space",
            Key.Tab => "Tab",
            Key.Back => "Backspace",
            Key.Delete => "Del",
            Key.Home => "Home",
            Key.End => "End",
            Key.PageUp => "PgUp",
            Key.PageDown => "PgDn",
            Key.Left => "←",
            Key.Right => "→",
            Key.Up => "↑",
            Key.Down => "↓",
            Key.OemQuestion => "?",
            Key.OemPeriod => ".",
            Key.OemComma => ",",
            Key.OemPlus => "+",
            Key.OemMinus => "-",
            _ => key.ToString(),
        };

    // ── Theme value container ─────────────────────────────────────────────

    /// <summary>Holds all resolved theme brushes/pens for a single render pass.</summary>
    private readonly record struct ThemeColors(
        Brush Scrim,
        Brush Panel,
        Brush BadgeFill,
        Pen BadgeBorder,
        Brush Accent,
        Brush Text
    );
}
