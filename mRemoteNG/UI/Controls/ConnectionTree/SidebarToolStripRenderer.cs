using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.Themes;
using mRemoteNG.UI.GraphicsUtilities;

namespace mRemoteNG.UI.Controls.ConnectionTree
{
    /// <summary>
    /// Flat renderer for the small icon-only tool strips at the top of side panels:
    /// panel-colored background, no border, rounded hover/pressed highlights.
    /// Only use it for the strip itself; drop-downs keep the theme renderer.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class SidebarToolStripRenderer : ToolStripRenderer
    {
        private readonly Color _background;

        public SidebarToolStripRenderer(Color background)
        {
            _background = background;
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using SolidBrush brush = new(_background);
            e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            // flat: no border
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e) => RenderItemHighlight(e);

        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e) => RenderItemHighlight(e);

        protected override void OnRenderDropDownButtonBackground(ToolStripItemRenderEventArgs e) => RenderItemHighlight(e);

        protected override void OnRenderSplitButtonBackground(ToolStripItemRenderEventArgs e) => RenderItemHighlight(e);

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            ModernPalette palette = ModernPalette.Current;
            Rectangle bounds = e.Item.ContentRectangle;
            int x = bounds.X + bounds.Width / 2;
            int inset = bounds.Height / 4;
            using Pen pen = new(palette.Border);
            e.Graphics.DrawLine(pen, x, bounds.Top + inset, x, bounds.Bottom - inset);
        }

        protected override void OnRenderGrip(ToolStripGripRenderEventArgs e)
        {
            // no grip
        }

        private static void RenderItemHighlight(ToolStripItemRenderEventArgs e)
        {
            ToolStripItem item = e.Item;
            ModernPalette palette = ModernPalette.Current;
            bool isChecked = item is ToolStripButton { Checked: true };
            Color fill;
            if (item.Pressed)
                fill = palette.SurfacePressed;
            else if (isChecked)
                fill = item.Selected ? ModernGraphics.Blend(palette.AccentSubtle, palette.Accent, 0.12f) : palette.AccentSubtle;
            else if (item.Selected && item.Enabled)
                fill = palette.SurfaceHover;
            else
                return;

            Rectangle bounds = new(Point.Empty, item.Size);
            bounds.Inflate(-1, -1);
            float radius = ModernGraphics.Scale(e.Graphics, ModernPalette.CornerRadius) - 1;
            ModernGraphics.FillRoundedRectangle(e.Graphics, fill, bounds, radius);
        }

        protected override void OnRenderItemImage(ToolStripItemImageRenderEventArgs e)
        {
            if (e.Image == null)
                return;

            if (e.Item.Enabled)
            {
                e.Graphics.DrawImage(e.Image, e.ImageRectangle);
                return;
            }

            // Fade disabled icons instead of the default gray-scale conversion.
            using ImageAttributes attributes = new();
            attributes.SetColorMatrix(new ColorMatrix { Matrix33 = 0.4f });
            e.Graphics.DrawImage(e.Image, e.ImageRectangle, 0, 0, e.Image.Width, e.Image.Height, GraphicsUnit.Pixel, attributes);
        }
    }
}
