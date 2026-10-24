using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Versioning;
using System.Windows.Forms;
using BrightIdeasSoftware;
using mRemoteNG.Connection;
using mRemoteNG.Container;
using mRemoteNG.Themes;
using mRemoteNG.Tree.Root;
using mRemoteNG.UI.GraphicsUtilities;

namespace mRemoteNG.UI.Controls.ConnectionTree
{
    /// <summary>
    /// Draws the connection tree as a modern sidebar: full-width rounded hover/selection rows,
    /// chevrons instead of +/- boxes, tinted Fluent glyphs per protocol, a green status dot for
    /// connected nodes and the per-connection Color as a text tint.
    /// All geometry is in logical pixels, scaled with the DPI of the list.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class ConnectionTreeRenderer : TreeListView.TreeRenderer
    {
        // Logical (96 DPI) metrics.
        internal const int RowHeightLogical = 28;
        private const int RowInsetX = 6;
        private const int RowInsetY = 1;
        private const int LeadingPadding = 4;
        private const int IndentPerLevel = 16;
        private const int ChevronBoxWidth = 18;
        private const int IconSize = 16;
        private const int IconTextGap = 8;
        private const int TrailingPadding = 10;
        private const int AccentBarWidth = 3;
        private const int AccentBarHeight = 14;
        private const int StatusDotSize = 7;

        public ConnectionTreeRenderer()
        {
            IsShowLines = false;
            UseTriangles = false;
        }

        private float Scale => ListView != null ? ListView.DeviceDpi / 96f : 1f;

        private int S(int logical) => (int)Math.Round(logical * Scale);

        /// <summary>Total width a row needs to show its content without clipping.</summary>
        internal static int MeasureRowWidth(Control list, Font font, string text, int level)
        {
            float scale = list.DeviceDpi / 96f;
            int Sc(int v) => (int)Math.Round(v * scale);
            int textWidth = TextRenderer.MeasureText(text ?? string.Empty, font, Size.Empty, TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine).Width;
            return Sc(RowInsetX) + Sc(LeadingPadding) + Math.Max(0, level - 1) * Sc(IndentPerLevel) + Sc(ChevronBoxWidth) +
                   Sc(IconSize) + Sc(IconTextGap) + textWidth + Sc(TrailingPadding) + Sc(RowInsetX);
        }

        private TreeListView.Branch? CurrentBranch => TreeListView.TreeModel?.GetBranch(RowObject);

        private Rectangle GetRowRectangle(Rectangle cell)
        {
            Rectangle row = cell;
            // Rows always span the visible width, even if the column is narrower or scrolled.
            int visibleRight = ListView.ClientSize.Width;
            row.Width = Math.Max(row.Width, visibleRight - row.X);
            row.Inflate(-S(RowInsetX), -S(RowInsetY));
            return row;
        }

        private Rectangle GetChevronRectangle(Rectangle row, int level) =>
            new(row.X + S(LeadingPadding) + Math.Max(0, level - 1) * S(IndentPerLevel), row.Y, S(ChevronBoxWidth), row.Height);

        private Rectangle GetIconRectangle(Rectangle chevron)
        {
            int size = S(IconSize);
            return new Rectangle(chevron.Right, chevron.Y + (chevron.Height - size) / 2, size, size);
        }

        private Rectangle GetTextRectangle(Rectangle row, Rectangle icon) =>
            new(icon.Right + S(IconTextGap), row.Y, Math.Max(0, row.Right - icon.Right - S(IconTextGap) - S(TrailingPadding) / 2), row.Height);

        public override void Render(Graphics g, Rectangle r)
        {
            ModernPalette palette = ModernPalette.Current;
            Color background = ListView.BackColor;
            // Paint one pixel past the cell: the shared back buffer is not cleared between cells.
            SmoothingMode smoothing = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.None;
            using (SolidBrush backBrush = new(background))
                g.FillRectangle(backBrush, Rectangle.Inflate(r, 1, 1));
            g.SmoothingMode = smoothing;

            if (RowObject is not ConnectionInfo node)
                return;

            TreeListView.Branch? branch = CurrentBranch;
            int level = branch?.Level ?? 1;
            bool canExpand = branch?.CanExpand ?? false;
            bool expanded = branch?.IsExpanded ?? false;

            Rectangle row = GetRowRectangle(r);
            Rectangle chevron = GetChevronRectangle(row, level);
            Rectangle icon = GetIconRectangle(chevron);
            Rectangle text = GetTextRectangle(row, icon);

            SmoothingMode previousSmoothing = g.SmoothingMode;
            try
            {
                DrawIndentGuides(g, branch, r, row, palette);
                DrawRowBackground(g, row, palette);
                if (canExpand)
                    DrawChevron(g, chevron, expanded, palette);
                DrawNodeIcon(g, node, icon, expanded, palette);
                DrawLabel(g, node, text, palette);
            }
            finally
            {
                g.SmoothingMode = previousSmoothing;
            }
        }

        private bool IsHot => ListView.HotRowIndex >= 0 && ListItem != null && ListView.HotRowIndex == ListItem.Index;

        /// <summary>Fill of the row highlight, or null when the row has none.</summary>
        private Color? GetRowFill(ModernPalette palette)
        {
            if (IsItemSelected)
            {
                Color fill = ListView.Focused ? palette.Selection : ModernGraphics.Blend(palette.Selection, ListView.BackColor, 0.45f);
                return IsHot ? ModernGraphics.Blend(fill, palette.Accent, 0.08f) : fill;
            }

            return IsHot ? palette.SurfaceHover : null;
        }

        private void DrawRowBackground(Graphics g, Rectangle row, ModernPalette palette)
        {
            float radius = S(ModernPalette.CornerRadius) - 1;
            bool focused = ListView.Focused;
            Color? rowFill = GetRowFill(palette);

            if (IsItemSelected)
            {
                ModernGraphics.FillRoundedRectangle(g, rowFill!.Value, row, radius);

                // Accent pill on the leading edge, as in the Windows 11 navigation view.
                int barHeight = Math.Min(S(AccentBarHeight), row.Height - S(6));
                RectangleF bar = new(row.X + 1, row.Y + (row.Height - barHeight) / 2f, S(AccentBarWidth), barHeight);
                Color barColor = focused ? palette.Accent : ModernGraphics.Blend(palette.Accent, ListView.BackColor, 0.4f);
                ModernGraphics.FillRoundedRectangle(g, barColor, bar, bar.Width / 2f);
            }
            else if (rowFill.HasValue)
            {
                ModernGraphics.FillRoundedRectangle(g, rowFill.Value, row, radius);
            }
        }

        private void DrawIndentGuides(Graphics g, TreeListView.Branch? branch, Rectangle cell, Rectangle row, ModernPalette palette)
        {
            if (branch == null || branch.Level <= 2)
                return;

            // One faint vertical line per ancestor folder (the top-level root needs none).
            Color guide = ModernGraphics.Blend(palette.BorderSubtle, palette.Border, 0.5f);
            using Pen pen = new(guide, Math.Max(1, S(1)));
            for (int ancestorLevel = 2; ancestorLevel < branch.Level; ancestorLevel++)
            {
                Rectangle ancestorChevron = GetChevronRectangle(row, ancestorLevel);
                int x = ancestorChevron.X + ancestorChevron.Width / 2;
                g.DrawLine(pen, x, cell.Top, x, cell.Bottom);
            }
        }

        private void DrawChevron(Graphics g, Rectangle chevron, bool expanded, ModernPalette palette)
        {
            char glyph = expanded ? ConnectionTreeIcons.ChevronDownSmall : ConnectionTreeIcons.ChevronRightSmall;
            bool hot = IsHot && ListView.HotCellHitLocation == HitTestLocation.ExpandButton;
            Color color = hot ? palette.Text : palette.TextSecondary;
            FluentIcons.Draw(g, glyph, chevron, color, S(10));
        }

        private void DrawNodeIcon(Graphics g, ConnectionInfo node, Rectangle icon, bool expanded, ModernPalette palette)
        {
            Bitmap? custom = ConnectionTreeIcons.GetCustomIcon(node, icon.Width);
            if (custom != null)
            {
                g.DrawImage(custom, icon);
            }
            else
            {
                Color glyphColor = ConnectionTreeIcons.GetGlyphColor(node, palette);
                FluentIcons.Draw(g, ConnectionTreeIcons.GetGlyph(node, expanded), Rectangle.Inflate(icon, S(2), S(2)), glyphColor, S(15));
            }

            if (node is not ContainerInfo && node.OpenConnections.Count > 0)
                DrawStatusDot(g, icon, palette);
        }

        private void DrawStatusDot(Graphics g, Rectangle icon, ModernPalette palette)
        {
            int size = S(StatusDotSize);
            int ring = Math.Max(1, S(2));
            RectangleF dot = new(icon.Right - size + S(2), icon.Bottom - size + S(2), size, size);
            RectangleF halo = RectangleF.Inflate(dot, ring, ring);

            g.SmoothingMode = SmoothingMode.AntiAlias;
            // The halo takes the color under the dot so it reads as a cut-out badge.
            Color under = GetRowFill(palette) ?? ListView.BackColor;
            using (SolidBrush haloBrush = new(under))
                g.FillEllipse(haloBrush, halo);
            using (SolidBrush dotBrush = new(palette.Success))
                g.FillEllipse(dotBrush, dot);
        }

        private void DrawLabel(Graphics g, ConnectionInfo node, Rectangle text, ModernPalette palette)
        {
            if (text.Width <= 0)
                return;

            Color color = palette.Text;
            Color? userColor = GetUserColor(node);
            if (userColor.HasValue)
                color = EnsureReadable(userColor.Value, ListView.BackColor, palette.Text);

            bool isRoot = node is RootNodeInfo || node is ContainerInfo { IsRootGroup: true };
            Font font = isRoot ? GetSemiboldFont() : ListView.Font;

            TextRenderer.DrawText(g, node.Name, font, text, color,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix |
                TextFormatFlags.PreserveGraphicsClipping | TextFormatFlags.PreserveGraphicsTranslateTransform);
        }

        private Font? _semiboldFont;
        private Font? _semiboldBase;

        private Font GetSemiboldFont()
        {
            Font baseFont = ListView.Font;
            if (_semiboldFont != null && ReferenceEquals(_semiboldBase, baseFont))
                return _semiboldFont;

            _semiboldFont?.Dispose();
            _semiboldBase = baseFont;
            try
            {
                // "Segoe UI Variable Display Semib" / "Segoe UI Semibold" read better than synthetic bold.
                string family = baseFont.FontFamily.Name.StartsWith("Segoe UI Variable", StringComparison.OrdinalIgnoreCase)
                    ? "Segoe UI Variable Text Semibold"
                    : "Segoe UI Semibold";
                _semiboldFont = new Font(family, baseFont.Size, FontStyle.Regular, baseFont.Unit);
                if (!_semiboldFont.Name.Equals(family, StringComparison.OrdinalIgnoreCase))
                {
                    _semiboldFont.Dispose();
                    _semiboldFont = new Font(baseFont, FontStyle.Bold);
                }
            }
            catch (ArgumentException)
            {
                _semiboldFont = new Font(baseFont, FontStyle.Bold);
            }

            return _semiboldFont;
        }

        private static readonly Dictionary<string, Color?> ColorCache = new(StringComparer.OrdinalIgnoreCase);

        internal static Color? GetUserColor(ConnectionInfo node)
        {
            string colorString = node.Color;
            if (string.IsNullOrEmpty(colorString))
                return null;

            if (ColorCache.TryGetValue(colorString, out Color? cached))
                return cached;

            Color? parsed = null;
            try
            {
                parsed = (Color?)new ColorConverter().ConvertFromString(colorString);
            }
            catch (Exception)
            {
                // invalid color strings are ignored, as before
            }

            if (parsed is { IsEmpty: true } || parsed is { A: 0 })
                parsed = null;
            ColorCache[colorString] = parsed;
            return parsed;
        }

        /// <summary>
        /// Keeps the user's tint but pulls it towards the normal text color when it would be
        /// too close to the background to read (e.g. dark red on the dark theme).
        /// </summary>
        private static Color EnsureReadable(Color color, Color background, Color text)
        {
            float contrast = ContrastRatio(color, background);
            if (contrast >= 3f)
                return color;

            for (float amount = 0.15f; amount <= 0.75f; amount += 0.15f)
            {
                Color blended = ModernGraphics.Blend(color, text, amount);
                if (ContrastRatio(blended, background) >= 3f)
                    return blended;
            }

            return ModernGraphics.Blend(color, text, 0.75f);
        }

        private static float ContrastRatio(Color a, Color b)
        {
            double la = RelativeLuminance(a) + 0.05;
            double lb = RelativeLuminance(b) + 0.05;
            return (float)(Math.Max(la, lb) / Math.Min(la, lb));
        }

        private static double RelativeLuminance(Color c)
        {
            static double Channel(int v)
            {
                double s = v / 255.0;
                return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
            }

            return 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);
        }

        protected override void HandleHitTest(Graphics g, OlvListViewHitTestInfo hti, int x, int y)
        {
            TreeListView.Branch? branch = CurrentBranch;
            if (branch == null)
            {
                hti.HitTestLocation = HitTestLocation.InCell;
                return;
            }

            Rectangle row = GetRowRectangle(Bounds);
            Rectangle chevron = GetChevronRectangle(row, branch.Level);
            if (branch.CanExpand && x >= chevron.Left && x < chevron.Right)
            {
                hti.HitTestLocation = HitTestLocation.ExpandButton;
                return;
            }

            Rectangle icon = GetIconRectangle(chevron);
            if (x >= icon.Left && x < icon.Right)
            {
                hti.HitTestLocation = HitTestLocation.Image;
                return;
            }

            Rectangle text = GetTextRectangle(row, icon);
            int textWidth = TextRenderer.MeasureText(g, GetText() ?? string.Empty, ListView.Font, Size.Empty, TextFormatFlags.NoPrefix).Width;
            hti.HitTestLocation = x >= text.Left && x < text.Left + textWidth ? HitTestLocation.Text : HitTestLocation.InCell;
        }

        protected override Rectangle HandleGetEditRectangle(Graphics g, Rectangle cellBounds, OLVListItem item, int subItemIndex, Size preferredSize)
        {
            TreeListView.Branch? branch = CurrentBranch;
            Rectangle row = GetRowRectangle(cellBounds);
            Rectangle chevron = GetChevronRectangle(row, branch?.Level ?? 1);
            Rectangle icon = GetIconRectangle(chevron);

            int left = icon.Right + S(IconTextGap) - S(3);
            int width = Math.Max(S(80), row.Right - left - S(4));
            int height = Math.Min(preferredSize.Height, row.Height);
            return new Rectangle(left, row.Y + (row.Height - height) / 2, width, height);
        }
    }
}
