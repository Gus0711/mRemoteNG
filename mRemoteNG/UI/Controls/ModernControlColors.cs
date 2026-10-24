using System;
using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.Themes;
using mRemoteNG.UI.GraphicsUtilities;

namespace mRemoteNG.UI.Controls
{
    /// <summary>
    /// Resolved colors used by the custom-painted Mrng* form controls.
    /// The Modern themes use the <see cref="ModernPalette"/> design tokens; legacy themes
    /// (vs2015*, darcula, user themes) map the same roles onto their extended palette so the
    /// modern shapes keep working with any theme.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal sealed class ModernControlColors
    {
        public Color Background { get; private init; }
        public Color Card { get; private init; }
        public Color CardBorder { get; private init; }
        public Color Surface { get; private init; }
        public Color SurfaceHover { get; private init; }
        public Color SurfacePressed { get; private init; }
        public Color SurfaceDisabled { get; private init; }
        public Color Border { get; private init; }
        public Color BorderHover { get; private init; }
        public Color Text { get; private init; }
        public Color TextSecondary { get; private init; }
        public Color TextDisabled { get; private init; }
        public Color InputBackground { get; private init; }
        public Color InputBackgroundFocused { get; private init; }
        public Color InputBackgroundDisabled { get; private init; }
        public Color InputText { get; private init; }
        public Color InputBorder { get; private init; }
        public Color ControlStroke { get; private init; }
        public Color Accent { get; private init; }
        public Color AccentHover { get; private init; }
        public Color AccentPressed { get; private init; }
        public Color AccentText { get; private init; }
        public Color Selection { get; private init; }
        public Color SelectionText { get; private init; }
        public bool IsDark { get; private init; }

        /// <summary>True when the Mrng controls should paint themselves (theming active and the theme is extended).</summary>
        public static bool Enabled
        {
            get
            {
                try
                {
                    return ThemeManager.getInstance().ActiveAndExtended;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public static bool IsModernTheme(ThemeInfo? theme) =>
            theme != null &&
            (theme.Name == ThemeManager.ModernDarkThemeName || theme.Name == ThemeManager.ModernLightThemeName ||
             // user themes derived from a Modern theme keep the "Modern" prefix by default
             theme.Name.StartsWith("Modern", StringComparison.OrdinalIgnoreCase));

        public static ModernControlColors Current
        {
            get
            {
                ThemeManager manager = ThemeManager.getInstance();
                ThemeInfo theme = manager.ActiveTheme;
                ExtendedColorPalette ext = theme.ExtendedPalette;
                Color dialogBack = Safe(ext, "Dialog_Background", SystemColors.Control);

                if (IsModernTheme(theme))
                {
                    ModernPalette p = ModernPalette.Current;
                    return new ModernControlColors
                    {
                        IsDark = p.IsDark,
                        Background = dialogBack,
                        Card = p.IsDark ? ModernGraphics.Blend(p.Surface, p.SurfaceHover, 0.25f) : p.Surface,
                        CardBorder = p.IsDark ? p.Border : p.Border,
                        Surface = p.IsDark ? p.SurfaceHover : p.Surface,
                        SurfaceHover = p.IsDark ? p.SurfacePressed : p.SurfaceHover,
                        SurfacePressed = p.IsDark ? p.Surface : p.SurfacePressed,
                        SurfaceDisabled = p.IsDark ? p.Surface : ModernGraphics.Blend(p.Surface, p.Chrome, 0.6f),
                        Border = p.IsDark ? ModernGraphics.Blend(p.Border, p.TextDisabled, 0.25f) : ModernGraphics.Blend(p.Border, p.TextDisabled, 0.2f),
                        BorderHover = ModernGraphics.Blend(p.Border, p.TextDisabled, 0.6f),
                        Text = p.Text,
                        TextSecondary = p.TextSecondary,
                        TextDisabled = p.TextDisabled,
                        InputBackground = p.IsDark ? ModernGraphics.Blend(p.Surface, p.SurfaceHover, 0.4f) : p.Surface,
                        InputBackgroundFocused = p.IsDark ? p.Content : p.Surface,
                        InputBackgroundDisabled = p.IsDark ? p.Surface : ModernGraphics.Blend(p.Surface, p.Chrome, 0.7f),
                        InputText = p.Text,
                        InputBorder = p.IsDark ? ModernGraphics.Blend(p.Border, p.TextDisabled, 0.3f) : ModernGraphics.Blend(p.Border, p.TextDisabled, 0.25f),
                        ControlStroke = p.TextSecondary,
                        Accent = p.Accent,
                        AccentHover = p.AccentHover,
                        AccentPressed = p.AccentPressed,
                        AccentText = p.AccentText,
                        Selection = p.Selection,
                        SelectionText = p.Text
                    };
                }

                bool dark = manager.IsActiveThemeDark;
                Color text = Safe(ext, "Dialog_Foreground", SystemColors.ControlText);
                Color accent = Safe(ext, "List_Item_Selected_Background", SystemColors.Highlight);
                return new ModernControlColors
                {
                    IsDark = dark,
                    Background = dialogBack,
                    Card = ModernGraphics.Blend(dialogBack, dark ? Color.White : Color.Black, dark ? 0.05f : 0.025f),
                    CardBorder = Safe(ext, "GroupBox_Line", SystemColors.ControlDark),
                    Surface = Safe(ext, "Button_Background", SystemColors.Control),
                    SurfaceHover = Safe(ext, "Button_Hover_Background", SystemColors.ControlLight),
                    SurfacePressed = Safe(ext, "Button_Pressed_Background", SystemColors.ControlDark),
                    SurfaceDisabled = Safe(ext, "Button_Disabled_Background", SystemColors.Control),
                    Border = Safe(ext, "Button_Border", SystemColors.ControlDark),
                    BorderHover = Safe(ext, "Button_Hover_Border", SystemColors.ControlDarkDark),
                    Text = text,
                    TextSecondary = ModernGraphics.Blend(text, dialogBack, 0.3f),
                    TextDisabled = Safe(ext, "TextBox_Disabled_Foreground", SystemColors.GrayText),
                    InputBackground = Safe(ext, "TextBox_Background", SystemColors.Window),
                    InputBackgroundFocused = Safe(ext, "TextBox_Background", SystemColors.Window),
                    InputBackgroundDisabled = Safe(ext, "TextBox_Disabled_Background", SystemColors.Control),
                    InputText = Safe(ext, "TextBox_Foreground", SystemColors.WindowText),
                    InputBorder = Safe(ext, "TextBox_Border", SystemColors.ControlDark),
                    ControlStroke = Safe(ext, "CheckBox_Border", SystemColors.ControlDarkDark),
                    Accent = accent,
                    AccentHover = ModernGraphics.Blend(accent, Color.White, 0.12f),
                    AccentPressed = ModernGraphics.Blend(accent, Color.Black, 0.12f),
                    AccentText = Safe(ext, "List_Item_Selected_Foreground", SystemColors.HighlightText),
                    Selection = accent,
                    SelectionText = Safe(ext, "List_Item_Selected_Foreground", SystemColors.HighlightText)
                };
            }
        }

        /// <summary>The effective (non transparent) background color behind a control.</summary>
        public static Color ParentBackground(Control control)
        {
            Control? parent = control.Parent;
            while (parent != null)
            {
                if (parent.BackColor.A == 255)
                    return parent.BackColor;
                parent = parent.Parent;
            }

            return Enabled ? Current.Background : SystemColors.Control;
        }

        public static float DpiScale(Control control) => control.DeviceDpi / 96f;

        public static int Scale(Control control, int logicalPixels) =>
            (int)Math.Round(logicalPixels * DpiScale(control));

        private static Color Safe(ExtendedColorPalette? palette, string key, Color fallback)
        {
            if (palette == null) return fallback;
            Color color = palette.getColor(key);
            return color.IsEmpty || color.A == 0 ? fallback : color;
        }
    }
}
