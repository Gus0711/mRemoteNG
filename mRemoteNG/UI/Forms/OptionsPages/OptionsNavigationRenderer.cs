using System;
using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;
using BrightIdeasSoftware;
using mRemoteNG.Themes;
using mRemoteNG.UI.Controls;
using mRemoteNG.UI.GraphicsUtilities;

namespace mRemoteNG.UI.Forms.OptionsPages
{
    /// <summary>
    /// Renders the Options navigation list like the Windows 11 Settings app: Fluent icon + text,
    /// rounded hover / selected highlight and an accent indicator on the selected page.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal sealed class OptionsNavigationRenderer : BaseRenderer
    {
        /// <summary>Fluent icon of an options page (by page type, so it does not depend on the UI language).</summary>
        public static char GlyphFor(OptionsPage? page) => page switch
        {
            StartupExitPage => FluentIcons.Power,
            AppearancePage => ModernGlyphs.Personalize,
            ConnectionsPage => FluentIcons.Remote,
            TabsPanelsPage => ModernGlyphs.TabView,
            NotificationsPage => ModernGlyphs.Ringer,
            CredentialsPage => ModernGlyphs.Key,
            SqlServerPage => ModernGlyphs.Database,
            UpdatesPage => FluentIcons.Sync,
            ThemePage => ModernGlyphs.Color,
            SecurityPage => FluentIcons.Shield,
            PluginsPage => ModernGlyphs.Puzzle,
            AdvancedPage => ModernGlyphs.Equalizer,
            BackupPage => FluentIcons.History,
            null => FluentIcons.Settings,
            _ => FluentIcons.Settings
        };

        public override void Render(Graphics g, Rectangle r)
        {
            ObjectListView list = ListView;
            float scale = list.DeviceDpi / 96f;
            int S(int v) => (int)Math.Round(v * scale);

            bool themed = ModernControlColors.Enabled;
            ModernControlColors? c = themed ? ModernControlColors.Current : null;
            Color background = themed ? list.BackColor : SystemColors.Window;
            Color text = c?.Text ?? SystemColors.WindowText;
            Color secondary = c?.TextSecondary ?? SystemColors.GrayText;
            Color accent = c?.Accent ?? SystemColors.Highlight;
            Color selection = c != null ? (c.IsDark ? c.SurfaceHover : ModernGraphics.Blend(c.SurfaceHover, c.SurfacePressed, 0.3f)) : SystemColors.ControlLight;
            Color hover = c != null ? (c.IsDark ? ModernGraphics.Blend(c.Background, c.SurfaceHover, 0.6f) : c.SurfaceHover) : SystemColors.ControlLight;

            using (SolidBrush back = new(background))
                g.FillRectangle(back, r);

            bool selected = IsItemSelected;
            bool hot = ListItem != null && list.HotRowIndex == ListItem.Index;
            RectangleF pill = new(r.X + S(4), r.Y + S(2), r.Width - S(8), r.Height - S(4));
            if (selected || hot)
                ModernGraphics.FillRoundedRectangle(g, selected ? selection : hover, pill, S(ModernPalette.CornerRadius) * 0.75f);

            if (selected)
            {
                float indicatorHeight = Math.Max(S(12), pill.Height * 0.4f);
                RectangleF indicator = new(pill.X, pill.Y + (pill.Height - indicatorHeight) / 2f, S(3), indicatorHeight);
                ModernGraphics.FillRoundedRectangle(g, accent, indicator, indicator.Width / 2f);
            }

            int iconSize = S(16);
            Rectangle icon = new((int)pill.X + S(14), r.Y + (r.Height - iconSize) / 2, iconSize, iconSize);
            FluentIcons.Draw(g, GlyphFor(RowObject as OptionsPage), icon, selected ? accent : (hot ? text : secondary),
                             iconSize * 0.95f);

            string label = (RowObject as OptionsPage)?.PageName ?? GetText() ?? string.Empty;
            Rectangle textRect = new(icon.Right + S(12), r.Y, Math.Max(0, (int)pill.Right - icon.Right - S(16)), r.Height);
            Font font = list.Font;
            TextRenderer.DrawText(g, label, font, textRect, text,
                                  TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                                  TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix |
                                  // OLV renders into an offset buffer: keep its translate transform
                                  TextFormatFlags.PreserveGraphicsTranslateTransform | TextFormatFlags.PreserveGraphicsClipping);
        }
    }
}
