#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Resources.Language;
using mRemoteNG.Themes;
using mRemoteNG.UI.GraphicsUtilities;

namespace mRemoteNG.UI.Modern
{
    /// <summary>Glyph, identity color and short label of each protocol.</summary>
    [SupportedOSPlatform("windows")]
    public static class ProtocolVisuals
    {
        public static char Glyph(ProtocolType protocol) => protocol switch
        {
            ProtocolType.RDP => FluentIcons.Desktop,
            ProtocolType.VNC => FluentIcons.Remote,
            ProtocolType.ARD => FluentIcons.Desktop,
            ProtocolType.AnyDesk => FluentIcons.Remote,
            ProtocolType.SSH1 or ProtocolType.SSH2 => FluentIcons.Terminal,
            ProtocolType.Telnet or ProtocolType.Rlogin or ProtocolType.RAW => FluentIcons.CommandPrompt,
            ProtocolType.HTTP or ProtocolType.HTTPS => FluentIcons.Globe,
            ProtocolType.PowerShell or ProtocolType.Terminal or ProtocolType.WSL => FluentIcons.Code,
            ProtocolType.IntApp => FluentIcons.Tools,
            _ => FluentIcons.Plug
        };

        /// <summary>
        /// Identity hue of the protocol family (the palette has no per-protocol tokens). Slightly
        /// brighter variants are used on dark surfaces for contrast.
        /// </summary>
        public static Color Color(ProtocolType protocol)
        {
            bool dark = ModernPalette.Current.IsDark;
            return protocol switch
            {
                ProtocolType.RDP => dark ? Hex(0x6BA4FF) : Hex(0x2563EB),
                ProtocolType.VNC => dark ? Hex(0xC08BFF) : Hex(0x9333EA),
                ProtocolType.ARD => dark ? Hex(0xF472B6) : Hex(0xDB2777),
                ProtocolType.AnyDesk => dark ? Hex(0xFF7A7A) : Hex(0xDC2626),
                ProtocolType.SSH1 or ProtocolType.SSH2 => dark ? Hex(0x4ADE80) : Hex(0x16A34A),
                ProtocolType.Telnet or ProtocolType.Rlogin or ProtocolType.RAW => dark ? Hex(0xFBBF24) : Hex(0xB45309),
                ProtocolType.HTTP or ProtocolType.HTTPS => dark ? Hex(0x22D3EE) : Hex(0x0891B2),
                ProtocolType.PowerShell or ProtocolType.Terminal or ProtocolType.WSL => dark ? Hex(0x7DD3FC) : Hex(0x0369A1),
                _ => ModernPalette.Current.TextSecondary
            };
        }

        public static string Label(ProtocolType protocol) => protocol switch
        {
            ProtocolType.SSH2 => "SSH",
            ProtocolType.SSH1 => "SSH1",
            ProtocolType.PowerShell => "PS",
            ProtocolType.IntApp => "App",
            ProtocolType.AnyDesk => "AnyDesk",
            _ => protocol.ToString().ToUpperInvariant()
        };

        /// <summary>Draws the rounded, tinted icon tile used for connections in lists and cards.</summary>
        public static void DrawTile(Graphics g, ProtocolType protocol, Rectangle bounds, float radius)
        {
            Color hue = Color(protocol);
            ModernPalette palette = ModernPalette.Current;
            Color background = ModernGraphics.Blend(palette.Surface, hue, palette.IsDark ? 0.18f : 0.12f);
            ModernGraphics.FillRoundedRectangle(g, background, bounds, radius);
            int glyphSize = (int)(bounds.Height * 0.62f);
            Rectangle glyphBounds = new(bounds.X + (bounds.Width - glyphSize) / 2, bounds.Y + (bounds.Height - glyphSize) / 2, glyphSize, glyphSize);
            FluentIcons.Draw(g, Glyph(protocol), glyphBounds, hue, glyphSize * 0.82f);
        }

        private static Color Hex(int rgb) => System.Drawing.Color.FromArgb(255, (rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
    }

    /// <summary>Text drawing helpers for the custom-painted lists and cards.</summary>
    [SupportedOSPlatform("windows")]
    public static class ModernText
    {
        private const TextFormatFlags BaseFlags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter;

        public static void Draw(Graphics g, string text, Font font, Rectangle bounds, Color color, TextFormatFlags extra = TextFormatFlags.Left)
        {
            TextRenderer.DrawText(g, text, font, bounds, color, BaseFlags | TextFormatFlags.EndEllipsis | extra);
        }

        public static Size Measure(Graphics g, string text, Font font) =>
            TextRenderer.MeasureText(g, text, font, new Size(int.MaxValue, int.MaxValue), BaseFlags);

        /// <summary>
        /// Draws <paramref name="text"/> left-aligned and vertically centered, with the characters at
        /// <paramref name="highlighted"/> positions in <paramref name="highlightFont"/>/<paramref name="highlightColor"/>.
        /// Truncates with an ellipsis when it does not fit. Returns the drawn width.
        /// </summary>
        public static int DrawHighlighted(Graphics g, string text, Font font, Font highlightFont, Rectangle bounds,
                                          Color color, Color highlightColor, ICollection<int>? highlighted)
        {
            if (string.IsNullOrEmpty(text) || bounds.Width <= 0) return 0;
            if (highlighted == null || highlighted.Count == 0)
            {
                Draw(g, text, font, bounds, color);
                return Math.Min(Measure(g, text, font).Width, bounds.Width);
            }

            // Truncate so that the text (measured with the wider highlight font) fits with an ellipsis.
            string visible = text;
            bool truncated = false;
            while (visible.Length > 1 && Measure(g, visible + (truncated ? "…" : string.Empty), highlightFont).Width > bounds.Width)
            {
                visible = visible[..^1];
                truncated = true;
            }

            int x = bounds.X;
            int start = 0;
            while (start < visible.Length)
            {
                bool isHighlight = highlighted.Contains(start);
                int end = start + 1;
                while (end < visible.Length && highlighted.Contains(end) == isHighlight) end++;

                string segment = visible[start..end];
                Font segmentFont = isHighlight ? highlightFont : font;
                Size size = Measure(g, segment, segmentFont);
                TextRenderer.DrawText(g, segment, segmentFont, new Rectangle(x, bounds.Y, size.Width + 2, bounds.Height),
                                      isHighlight ? highlightColor : color, BaseFlags);
                x += size.Width;
                start = end;
            }

            if (truncated)
            {
                Size ellipsis = Measure(g, "…", font);
                TextRenderer.DrawText(g, "…", font, new Rectangle(x, bounds.Y, ellipsis.Width + 2, bounds.Height), color, BaseFlags);
                x += ellipsis.Width;
            }

            return x - bounds.X;
        }

        /// <summary>Draws a small "key cap" (e.g. Ctrl, K) and returns its width.</summary>
        public static int DrawKeyCap(Graphics g, string key, Font font, Point location, int height, float scale)
        {
            ModernPalette palette = ModernPalette.Current;
            Size size = Measure(g, key, font);
            int padding = (int)(6 * scale);
            Rectangle cap = new(location.X, location.Y, Math.Max(size.Width + padding * 2, height), height);
            ModernGraphics.FillRoundedRectangle(g, palette.IsDark ? palette.SurfaceHover : palette.Chrome, cap, 4 * scale);
            ModernGraphics.DrawRoundedRectangle(g, palette.Border, cap, 4 * scale);
            TextRenderer.DrawText(g, key, font, cap, palette.TextSecondary, BaseFlags | TextFormatFlags.HorizontalCenter);
            return cap.Width;
        }

        /// <summary>Draws several key caps separated by a small gap; returns the total width.</summary>
        public static int DrawKeyCaps(Graphics g, IReadOnlyList<string> keys, Font font, Point location, int height, float scale)
        {
            int x = location.X;
            int gap = (int)(4 * scale);
            for (int i = 0; i < keys.Count; i++)
            {
                x += DrawKeyCap(g, keys[i], font, new Point(x, location.Y), height, scale);
                if (i < keys.Count - 1) x += gap;
            }

            return x - location.X;
        }

        /// <summary>Width that <see cref="DrawKeyCaps"/> would use.</summary>
        public static int MeasureKeyCaps(Graphics g, IReadOnlyList<string> keys, Font font, int height, float scale)
        {
            int total = 0;
            int padding = (int)(6 * scale);
            int gap = (int)(4 * scale);
            for (int i = 0; i < keys.Count; i++)
            {
                total += Math.Max(Measure(g, keys[i], font).Width + padding * 2, height);
                if (i < keys.Count - 1) total += gap;
            }

            return total;
        }
    }

    /// <summary>Short localized "time ago" strings.</summary>
    public static class RelativeTime
    {
        public static string Format(DateTime utc)
        {
            if (utc == default) return string.Empty;
            TimeSpan elapsed = DateTime.UtcNow - utc;
            if (elapsed < TimeSpan.FromMinutes(1)) return Language.TimeJustNow;
            if (elapsed < TimeSpan.FromHours(1)) return string.Format(Language.TimeMinutesAgo, (int)elapsed.TotalMinutes);
            if (elapsed < TimeSpan.FromDays(1) && utc.ToLocalTime().Date == DateTime.Today)
                return string.Format(Language.TimeHoursAgo, (int)elapsed.TotalHours);
            if (utc.ToLocalTime().Date == DateTime.Today.AddDays(-1)) return Language.TimeYesterday;
            if (elapsed < TimeSpan.FromDays(30)) return string.Format(Language.TimeDaysAgo, Math.Max(2, (int)Math.Ceiling(elapsed.TotalDays)));
            return utc.ToLocalTime().ToString("d");
        }
    }

    /// <summary>Pixel-sized fonts of the modern UI (callers own and dispose the returned fonts).</summary>
    [SupportedOSPlatform("windows")]
    public static class ModernFonts
    {
        private static readonly Lazy<string[]> Installed = new(() =>
        {
            using InstalledFontCollection fonts = new();
            return fonts.Families.Select(f => f.Name).ToArray();
        });

        private static readonly Lazy<string> TextSemiboldFamily = new(() => Resolve("Segoe UI Variable Text Semibold", "Segoe UI Semibold"));
        private static readonly Lazy<string> DisplayFamily = new(() => Resolve("Segoe UI Variable Display", ModernGraphics.UiFontFamily));
        private static readonly Lazy<string> DisplaySemiboldFamily = new(() => Resolve("Segoe UI Variable Display Semib", "Segoe UI Semibold"));

        public static Font Regular(float pixels) => new(ModernGraphics.UiFontFamily, pixels, FontStyle.Regular, GraphicsUnit.Pixel);

        public static Font Semibold(float pixels) => Create(TextSemiboldFamily.Value, pixels);

        public static Font Display(float pixels) => new(DisplayFamily.Value, pixels, FontStyle.Regular, GraphicsUnit.Pixel);

        public static Font DisplaySemibold(float pixels) => Create(DisplaySemiboldFamily.Value, pixels);

        private static Font Create(string family, float pixels) =>
            family.Length == 0
                ? new Font(ModernGraphics.UiFontFamily, pixels, FontStyle.Bold, GraphicsUnit.Pixel)
                : new Font(family, pixels, FontStyle.Regular, GraphicsUnit.Pixel);

        private static string Resolve(params string[] candidates)
        {
            foreach (string candidate in candidates)
            {
                if (Installed.Value.Any(n => n.Equals(candidate, StringComparison.OrdinalIgnoreCase)))
                    return candidate;
            }

            return string.Empty;
        }
    }
}
