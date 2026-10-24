using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.Themes;
using mRemoteNG.UI.GraphicsUtilities;
using WeifenLuo.WinFormsUI.Docking;

namespace mRemoteNG.UI.Tabs
{
    /// <summary>
    /// Auto-hide strip (collapsed tool windows along the window edges): flat Chrome background,
    /// text tabs with a rounded hover highlight and an accent indicator on the tab whose window is open.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal sealed class MremoteNGAutoHideStrip : AutoHideStripBase
    {
        private sealed class TabNG : Tab
        {
            internal TabNG(IDockContent content) : base(content)
            {
            }

            public int TabX { get; set; }

            public int TabWidth { get; set; }
        }

        private const int TabPadding = 10;
        private const int TabGap = 2;
        private const int StripMargin = 4;

        private static readonly DockState[] AllStates =
        [
            DockState.DockTopAutoHide,
            DockState.DockRightAutoHide,
            DockState.DockBottomAutoHide,
            DockState.DockLeftAutoHide
        ];

        private static GraphicsPath? _graphicsPath;
        private TabNG? _hoveredTab;

        internal static GraphicsPath GraphicsPath => _graphicsPath ??= new GraphicsPath();

        public MremoteNGAutoHideStrip(DockPanel panel) : base(panel)
        {
            SetStyle(ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = DockingStyle.Palette.Chrome;
            ThemeManager.getInstance().ThemeChanged += OnThemeChanged;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                ThemeManager.getInstance().ThemeChanged -= OnThemeChanged;
            base.Dispose(disposing);
        }

        private void OnThemeChanged()
        {
            if (IsDisposed) return;
            BackColor = DockingStyle.Palette.Chrome;
            Invalidate();
        }

        private Font TextFont => DockingStyle.GetFont(this);

        private static bool IsVertical(DockState state) =>
            state is DockState.DockLeftAutoHide or DockState.DockRightAutoHide;

        protected override void OnPaint(PaintEventArgs e)
        {
            ModernPalette palette = DockingStyle.Palette;
            Graphics g = e.Graphics;
            g.Clear(palette.Chrome);

            foreach (DockState state in AllStates)
            {
                foreach (Pane pane in GetPanes(state))
                {
                    foreach (Tab tab in pane.AutoHideTabs)
                        DrawTab(g, (TabNG)tab, palette);
                }
            }
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            CalculateTabs();
            base.OnLayout(levent);
        }

        private void CalculateTabs()
        {
            foreach (DockState state in AllStates)
            {
                int x = DockingStyle.Scale(this, StripMargin);
                foreach (Pane pane in GetPanes(state))
                {
                    foreach (Tab tab1 in pane.AutoHideTabs)
                    {
                        TabNG tab = (TabNG)tab1;
                        int width = TextRenderer.MeasureText(tab.Content.DockHandler.TabText, TextFont).Width +
                                    2 * DockingStyle.Scale(this, TabPadding);
                        tab.TabX = x;
                        tab.TabWidth = width;
                        x += width + DockingStyle.Scale(this, TabGap);
                    }
                }
            }
        }

        private Rectangle GetTabRectangle(TabNG tab)
        {
            DockState state = tab.Content.DockHandler.DockState;
            Rectangle strip = GetTabStripRectangle(state);
            if (IsVertical(state))
                return new Rectangle(strip.X, strip.Y + tab.TabX, strip.Width, tab.TabWidth);

            return new Rectangle(strip.X + tab.TabX, strip.Y, tab.TabWidth, strip.Height);
        }

        private Rectangle RtlTransform(Rectangle rect, DockState state) =>
            IsVertical(state) ? rect : DrawHelper.RtlTransform(this, rect);

        private void DrawTab(Graphics g, TabNG tab, ModernPalette palette)
        {
            Rectangle rect = GetTabRectangle(tab);
            if (rect.IsEmpty)
                return;

            DockState state = tab.Content.DockHandler.DockState;
            rect = RtlTransform(rect, state);
            bool open = DockPanel.ActiveAutoHideContent == tab.Content;
            bool hovered = tab == _hoveredTab;

            int inset = DockingStyle.Scale(this, 3);
            Rectangle highlight = Rectangle.Inflate(rect, IsVertical(state) ? -inset : 0, IsVertical(state) ? 0 : -inset);
            if (hovered || open)
            {
                ModernGraphics.FillRoundedRectangle(g, hovered ? palette.SurfaceHover : palette.Surface, highlight,
                                                    DockingStyle.Scale(this, ModernPalette.CornerRadius - 2));
            }

            if (open)
            {
                // Indicator on the edge facing the opened window.
                int thickness = DockingStyle.Scale(this, 2);
                int pad = DockingStyle.Scale(this, TabPadding);
                Rectangle indicator = state switch
                {
                    DockState.DockBottomAutoHide => new Rectangle(rect.X + pad, rect.Y, rect.Width - 2 * pad, thickness),
                    DockState.DockTopAutoHide => new Rectangle(rect.X + pad, rect.Bottom - thickness, rect.Width - 2 * pad, thickness),
                    DockState.DockLeftAutoHide => new Rectangle(rect.Right - thickness, rect.Y + pad, thickness, rect.Height - 2 * pad),
                    _ => new Rectangle(rect.X, rect.Y + pad, thickness, rect.Height - 2 * pad)
                };
                ModernGraphics.FillRoundedRectangle(g, palette.Accent, indicator, thickness / 2f);
            }

            Color textColor = hovered || open ? palette.Text : palette.TextSecondary;
            string text = tab.Content.DockHandler.TabText;
            if (IsVertical(state))
            {
                using StringFormat format = new()
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center,
                    FormatFlags = StringFormatFlags.DirectionVertical | StringFormatFlags.NoWrap,
                    Trimming = StringTrimming.EllipsisCharacter
                };
                TextRenderingHint previous = g.TextRenderingHint;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                using SolidBrush brush = new(textColor);
                g.DrawString(text, TextFont, brush, rect, format);
                g.TextRenderingHint = previous;
            }
            else
            {
                TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                                        TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;
                if (RightToLeft == RightToLeft.Yes)
                    flags |= TextFormatFlags.RightToLeft;
                TextRenderer.DrawText(g, text, TextFont, rect, textColor, flags);
            }
        }

        private GraphicsPath GetTabOutline(TabNG tab, bool rtlTransform)
        {
            Rectangle rect = GetTabRectangle(tab);
            if (rtlTransform)
                rect = RtlTransform(rect, tab.Content.DockHandler.DockState);

            GraphicsPath.Reset();
            GraphicsPath.AddRectangle(rect);
            return GraphicsPath;
        }

        protected override IDockContent HitTest(Point point) => TabHitTest(point)?.Content;

        private TabNG? TabHitTest(Point point)
        {
            foreach (DockState state in AllStates)
            {
                if (!GetTabStripRectangle(state).Contains(point))
                    continue;

                foreach (Pane pane in GetPanes(state))
                {
                    foreach (Tab tab in pane.AutoHideTabs)
                    {
                        if (GetTabOutline((TabNG)tab, true).IsVisible(point))
                            return (TabNG)tab;
                    }
                }
            }

            return null;
        }

        protected override Rectangle GetTabBounds(Tab tab)
        {
            RectangleF bounds = GetTabOutline((TabNG)tab, true).GetBounds();
            return new Rectangle((int)bounds.Left, (int)bounds.Top, (int)bounds.Width, (int)bounds.Height);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            TabNG? tab = TabHitTest(PointToClient(MousePosition));
            if (tab == _hoveredTab) return;
            _hoveredTab = tab;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoveredTab = null;
            Invalidate();
        }

        protected override int MeasureHeight() => DockingStyle.Scale(this, DockingStyle.AutoHideStripHeight);

        protected override void OnRefreshChanges()
        {
            CalculateTabs();
            Invalidate();
        }

        protected override Tab CreateTab(IDockContent content) => new TabNG(content);
    }

    [SupportedOSPlatform("windows")]
    public class MremoteNGAutoHideStripFactory : DockPanelExtender.IAutoHideStripFactory
    {
        public AutoHideStripBase CreateAutoHideStrip(DockPanel panel) => new MremoteNGAutoHideStrip(panel);
    }
}
