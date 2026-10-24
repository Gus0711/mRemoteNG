using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Text;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.Themes;
using mRemoteNG.UI.GraphicsUtilities;

namespace mRemoteNG.UI.Tabs
{
    /// <summary>
    /// Shared metrics, fonts and colors of the modern docking parts (tab strips, captions,
    /// splitters, auto-hide strip). All sizes are logical pixels (96 DPI) scaled per control DPI.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal static class DockingStyle
    {
        /// <summary>Height of the connection (document) tab strip.</summary>
        public const int DocumentStripHeight = 38;

        /// <summary>Gap between the top of the document strip and the top of the tabs.</summary>
        public const int DocumentTabTop = 6;

        /// <summary>Height of the panel (top level document) tab strip.</summary>
        public const int PanelStripHeight = 34;

        /// <summary>Height of the tool window tab strip (grouped tool windows).</summary>
        public const int ToolWindowStripHeight = 30;

        /// <summary>Height of a tool window caption.</summary>
        public const int CaptionHeight = 32;

        /// <summary>Height (thickness) of the auto-hide strip.</summary>
        public const int AutoHideStripHeight = 28;

        /// <summary>UI text size in pixels (9 pt).</summary>
        public const float TextPixelSize = 12f;

        /// <summary>Caption text size in pixels (~8.25 pt).</summary>
        public const float CaptionPixelSize = 11f;

        private static readonly ConcurrentDictionary<(int Dpi, float Size, bool Semibold), Font> Fonts = new();
        private static readonly Lazy<string?> SemiboldFamilyName = new(ResolveSemiboldFamily);

        public static ModernPalette Palette => ModernPalette.Current;

        public static int Scale(Control control, float logicalPixels) =>
            (int)Math.Round(logicalPixels * control.DeviceDpi / 96f);

        /// <summary>Cached font for the given DPI. Do not dispose.</summary>
        public static Font GetFont(Control control, float logicalPixelSize = TextPixelSize, bool semibold = false)
        {
            int dpi = control.DeviceDpi;
            return Fonts.GetOrAdd((dpi, logicalPixelSize, semibold), key =>
            {
                float px = key.Size * key.Dpi / 96f;
                if (!key.Semibold)
                    return new Font(ModernGraphics.UiFontFamily, px, FontStyle.Regular, GraphicsUnit.Pixel);

                string? semiboldFamily = SemiboldFamilyName.Value;
                return semiboldFamily != null
                    ? new Font(semiboldFamily, px, FontStyle.Regular, GraphicsUnit.Pixel)
                    : new Font(ModernGraphics.UiFontFamily, px, FontStyle.Bold, GraphicsUnit.Pixel);
            });
        }

        /// <summary>The accent, dimmed when the owning pane does not have the focus.</summary>
        public static Color Indicator(bool focused, Color background)
        {
            ModernPalette p = Palette;
            return focused ? p.Accent : ModernGraphics.Blend(p.Accent, background, 0.45f);
        }

        private static string? ResolveSemiboldFamily()
        {
            string[] candidates = ["Segoe UI Variable Text Semibold", "Segoe UI Variable Small Semibold", "Segoe UI Semibold"];
            using InstalledFontCollection fonts = new();
            foreach (string candidate in candidates)
            {
                foreach (FontFamily family in fonts.Families)
                {
                    if (family.Name.Equals(candidate, StringComparison.OrdinalIgnoreCase))
                        return family.Name;
                }
            }

            return null;
        }
    }
}
