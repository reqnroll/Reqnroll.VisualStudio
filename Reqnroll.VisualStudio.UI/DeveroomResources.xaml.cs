#nullable disable
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

namespace Reqnroll.VisualStudio.UI;

partial class DeveroomResources
{
    public DeveroomResources()
    {
        InitializeComponent();

        if (IsRunningFromVs())
            OverwriteVsStyles();
    }

    private void OverwriteVsStyles()
    {
        SetThemedBrush(ThemedDialogColors.WindowPanelBrushKey, "ThemedWindowBackgroundBrush");
        SetThemedBrush(ThemedDialogColors.WindowButtonBrushKey);
        SetThemedBrush(ThemedDialogColors.WindowPanelTextBrushKey);
        SetThemedBrush(ThemedDialogColors.WindowButtonGlyphBrushKey);
        SetThemedBrush(ThemedDialogColors.HeaderTextBrushKey);

        SetThemedBrush(ThemedDialogColors.WindowButtonHoverBrushKey);
        SetThemedBrush(ThemedDialogColors.WindowButtonHoverGlyphBrushKey, "ThemedWindowButtonHoverForegroundBrushKey");
        SetThemedBrush(ThemedDialogColors.WindowButtonHoverBorderBrushKey);
        SetThemedBrushFromColorKey(ThemedDialogColors.CloseWindowButtonHoverColorKey,
            "ThemedCloseWindowButtonHoverBrushKey");
        SetThemedBrushFromColorKey(ThemedDialogColors.CloseWindowButtonHoverTextColorKey,
            "ThemedCloseWindowButtonHoverTextBrushKey");

        SetThemedStyle(VsResourceKeys.TextBlockEnvironment283PercentFontSizeStyleKey, "ThemedHeader");
        SetThemedStyle(VsResourceKeys.ThemedDialogLabelStyleKey);
        SetThemedStyle(VsResourceKeys.ThemedDialogCheckBoxStyleKey);
        SetThemedStyle(VsResourceKeys.ThemedDialogComboBoxStyleKey);
        SetThemedStyle(VsResourceKeys.ThemedDialogButtonStyleKey);
    }

    private void SetThemedStyle(object styleKey, string key = null)
    {
        key ??= styleKey.ToString().Replace("StyleKey", "");

        if (Application.Current.FindResource(styleKey) is Style style)
            this[key] = style;
    }

    private void SetThemedBrush(ThemeResourceKey themedResourceKey, string key = null)
    {
        key ??= "Themed" + themedResourceKey;
        var brush = Application.Current.TryFindResource(themedResourceKey);
        if (brush != null)
            this[key] = brush;
    }

    private void SetThemedBrushFromColorKey(ThemeResourceKey themedResourceKey, string key)
    {
        var brush = ToBrushFromColorKey(themedResourceKey);
        if (brush != null)
            this[key] = brush;
    }

    private static Brush ToBrushFromColorKey(ThemeResourceKey key)
    {
        try
        {
            var color = VSColorTheme.GetThemedColor(key);
            return new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex, "VS-COLORS");
            return null;
        }
    }

    private bool IsRunningFromVs() => "devenv".Equals(Process.GetCurrentProcess().ProcessName,
        StringComparison.InvariantCultureIgnoreCase);
}
