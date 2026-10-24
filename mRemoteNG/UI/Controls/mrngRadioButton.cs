using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.Themes;
using mRemoteNG.UI.GraphicsUtilities;

namespace mRemoteNG.UI.Controls
{
    [SupportedOSPlatform("windows")]
    // Fluent style radio button: thin ring when unchecked, accent ring with a centered dot when checked.
    class MrngRadioButton : RadioButton
    {
        private const int CircleLogicalSize = 16;
        private const int GapLogicalSize = 6;

        private enum MouseState
        {
            HOVER,
            DOWN,
            OUT
        }

        private MouseState _mice;

        public MrngRadioButton()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.ResizeRedraw, true);
            ThemeManager.getInstance().ThemeChanged += OnThemeChanged;
        }

        private bool CustomPainted =>
            ModernControlColors.Enabled && Appearance == Appearance.Normal && RightToLeft != RightToLeft.Yes;

        private void OnThemeChanged()
        {
            if (IsDisposed) return;
            SyncPaintMode();
            Invalidate();
        }

        protected override void OnCreateControl()
        {
            SyncPaintMode();
            base.OnCreateControl();
        }

        // WinForms dark mode (Application.SetColorMode) renders Standard buttons natively and skips OnPaint:
        // use the Flat style (always user painted) while the modern look is active.
        private void SyncPaintMode()
        {
            FlatStyle wanted = ModernControlColors.Enabled ? FlatStyle.Flat : FlatStyle.Standard;
            if (FlatStyle != wanted && FlatStyle != FlatStyle.System)
                FlatStyle = wanted;
            SetStyle(ControlStyles.UserPaint, true);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                ThemeManager.getInstance().ThemeChanged -= OnThemeChanged;
            base.Dispose(disposing);
        }

        protected override void OnMouseEnter(EventArgs eventargs)
        {
            _mice = MouseState.HOVER;
            Invalidate();
            base.OnMouseEnter(eventargs);
        }

        protected override void OnMouseLeave(EventArgs eventargs)
        {
            _mice = MouseState.OUT;
            Invalidate();
            base.OnMouseLeave(eventargs);
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            if (mevent.Button == MouseButtons.Left)
            {
                _mice = MouseState.DOWN;
                Invalidate();
            }

            base.OnMouseDown(mevent);
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            _mice = ClientRectangle.Contains(mevent.Location) ? MouseState.HOVER : MouseState.OUT;
            Invalidate();
            base.OnMouseUp(mevent);
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            Size size = base.GetPreferredSize(proposedSize);
            if (!CustomPainted || !AutoSize) return size;
            // glyph + gap + text, measured the way OnPaint draws it
            TextFormatFlags flags = TextFormatFlags.SingleLine | (ShowKeyboardCues ? 0 : TextFormatFlags.HidePrefix);
            Size text = TextRenderer.MeasureText(Text ?? string.Empty, Font, Size.Empty, flags);
            int glyph = ModernControlColors.Scale(this, CircleLogicalSize);
            int width = glyph + ModernControlColors.Scale(this, GapLogicalSize) + text.Width + 2 + Padding.Horizontal;
            int height = System.Math.Max(size.Height, glyph + 2);
            return new Size(System.Math.Max(width, size.Width), height);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (!CustomPainted)
            {
                base.OnPaint(e);
                return;
            }

            ModernControlColors c = ModernControlColors.Current;
            Graphics g = e.Graphics;
            g.Clear(BackColor.A == 255 ? BackColor : ModernControlColors.ParentBackground(this));
            float scale = ModernControlColors.DpiScale(this);

            int size = Math.Min(ModernControlColors.Scale(this, CircleLogicalSize), Math.Max(8, Height - 1));
            int gap = ModernControlColors.Scale(this, GapLogicalSize);
            bool multiLine = Height >= Math.Max(Font.Height, size) * 2;
            int y = multiLine ? Padding.Top + (Font.Height - size) / 2 + 1 : (Height - size) / 2;
            RectangleF circle = new(1, Math.Max(0, y), size, size);

            SmoothingMode previous = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (Checked)
            {
                Color ring = !Enabled
                    ? ModernGraphics.Blend(c.Accent, c.Background, 0.6f)
                    : _mice switch
                    {
                        MouseState.HOVER => c.AccentHover,
                        MouseState.DOWN => c.AccentPressed,
                        _ => c.Accent
                    };
                using (SolidBrush ringBrush = new(ring))
                    g.FillEllipse(ringBrush, circle);

                // Fluent: the inner dot grows on hover and shrinks when pressed.
                float dotRatio = _mice switch
                {
                    MouseState.HOVER => 0.5f,
                    MouseState.DOWN => 0.36f,
                    _ => 0.44f
                };
                float dot = size * dotRatio;
                RectangleF dotRect = new(circle.X + (size - dot) / 2f, circle.Y + (size - dot) / 2f, dot, dot);
                using SolidBrush dotBrush = new(Enabled ? c.AccentText : ModernGraphics.Blend(c.AccentText, c.Background, 0.3f));
                g.FillEllipse(dotBrush, dotRect);
            }
            else
            {
                Color fill = !Enabled
                    ? c.InputBackgroundDisabled
                    : _mice switch
                    {
                        MouseState.HOVER => c.SurfaceHover,
                        MouseState.DOWN => c.SurfacePressed,
                        _ => c.InputBackground
                    };
                Color stroke = !Enabled
                    ? ModernGraphics.Blend(c.ControlStroke, c.Background, 0.55f)
                    : _mice == MouseState.OUT ? c.ControlStroke : c.Text;
                using (SolidBrush fillBrush = new(fill))
                    g.FillEllipse(fillBrush, circle);
                float penWidth = Math.Max(1f, scale);
                using Pen pen = new(stroke, penWidth);
                g.DrawEllipse(pen, RectangleF.Inflate(circle, -penWidth / 2f, -penWidth / 2f));
                if (_mice == MouseState.DOWN && Enabled)
                {
                    float dot = size * 0.3f;
                    using SolidBrush pressed = new(c.TextSecondary);
                    g.FillEllipse(pressed, circle.X + (size - dot) / 2f, circle.Y + (size - dot) / 2f, dot, dot);
                }
            }

            if (Focused && ShowFocusCues)
            {
                float inset = 2 * scale;
                using Pen focusPen = new(c.Text, Math.Max(1f, scale));
                RectangleF ring = RectangleF.Inflate(circle, inset - 1, inset - 1);
                ring.Intersect(new RectangleF(0, 0, Width - 1, Height - 1));
                g.DrawEllipse(focusPen, ring);
            }

            g.SmoothingMode = previous;

            Rectangle textRect = new(size + gap + 1, 0, Math.Max(0, Width - size - gap - 1), Height);
            TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.EndEllipsis;
            flags |= multiLine ? TextFormatFlags.WordBreak | TextFormatFlags.Top : TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine;
            if (multiLine) textRect.Y = Padding.Top;
            if (!ShowKeyboardCues) flags |= TextFormatFlags.HidePrefix;
            TextRenderer.DrawText(g, Text, Font, textRect, Enabled ? c.Text : c.TextDisabled, flags);
        }
    }
}
