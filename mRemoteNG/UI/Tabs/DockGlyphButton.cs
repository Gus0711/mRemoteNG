using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.Themes;
using mRemoteNG.UI.GraphicsUtilities;

namespace mRemoteNG.UI.Tabs
{
    /// <summary>
    /// Small icon button used by the docking chrome (caption pin/close, tab list dropdown).
    /// Draws a Segoe Fluent Icons glyph with a rounded hover/pressed highlight.
    /// </summary>
    [SupportedOSPlatform("windows")]
    [ToolboxItem(false)]
    internal sealed class DockGlyphButton : Control
    {
        private char _glyph;
        private bool _isMouseOver;
        private bool _isMouseDown;

        public DockGlyphButton(char glyph, float glyphSize = 12f)
        {
            _glyph = glyph;
            GlyphSize = glyphSize;
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Selectable, false);
        }

        public char Glyph
        {
            get => _glyph;
            set
            {
                if (_glyph == value) return;
                _glyph = value;
                Invalidate();
            }
        }

        /// <summary>Glyph em size in logical pixels.</summary>
        public float GlyphSize { get; set; }

        /// <summary>Returns the color behind the button (the parent's painted surface).</summary>
        public Func<Color>? SurfaceColor { get; set; }

        /// <summary>Optional override of the idle glyph color.</summary>
        public Func<Color>? GlyphColor { get; set; }

        protected override Size DefaultSize => new(24, 24);

        public void RefreshChanges()
        {
            if (IsDisposed) return;
            bool over = ClientRectangle.Contains(PointToClient(MousePosition));
            if (over != _isMouseOver)
                _isMouseOver = over;
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isMouseOver = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isMouseOver = false;
            _isMouseDown = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            _isMouseDown = true;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isMouseDown = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ModernPalette palette = ModernPalette.Current;
            Color surface = SurfaceColor?.Invoke() ?? Parent?.BackColor ?? palette.Panel;
            Graphics g = e.Graphics;
            g.Clear(surface);

            Color glyphColor;
            if (!Enabled)
            {
                glyphColor = palette.TextDisabled;
            }
            else if (_isMouseOver)
            {
                Color highlight = _isMouseDown ? palette.SurfacePressed : palette.SurfaceHover;
                ModernGraphics.FillRoundedRectangle(g, highlight, ClientRectangle,
                                                    DockingStyle.Scale(this, ModernPalette.CornerRadius - 2));
                glyphColor = palette.Text;
            }
            else
            {
                glyphColor = GlyphColor?.Invoke() ?? palette.TextSecondary;
            }

            FluentIcons.Draw(g, _glyph, ClientRectangle, glyphColor, GlyphSize * DeviceDpi / 96f);
        }
    }
}
