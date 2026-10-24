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
    // Fluent style check box: rounded square, accent fill with a white check mark when checked.
    public class MrngCheckBox : CheckBox
    {
        private const int BoxLogicalSize = 14;
        private const int GapLogicalSize = 6;

        public MrngCheckBox()
        {
            InitializeComponent();
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.ResizeRedraw, true);
            ThemeManager.getInstance().ThemeChanged += OnThemeChanged;
        }

        public enum MouseState
        {
            HOVER,
            DOWN,
            OUT
        }

        // ReSharper disable once InconsistentNaming
        public MouseState _mice { get; set; }

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
            int glyph = ModernControlColors.Scale(this, BoxLogicalSize);
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

            int box = ModernControlColors.Scale(this, BoxLogicalSize);
            int gap = ModernControlColors.Scale(this, GapLogicalSize);
            int lineHeight = Math.Max(Font.Height, box);
            bool multiLine = Height >= lineHeight * 2;
            int boxY = multiLine ? Padding.Top + (Font.Height - box) / 2 + 1 : (Height - box) / 2;
            boxY = Math.Max(0, boxY);
            RectangleF boxRect = new(1, boxY, box, box);

            DrawGlyph(g, c, boxRect, CheckState, Enabled, _mice, ModernControlColors.DpiScale(this));

            Rectangle textRect = new(box + gap + 1, 0, Math.Max(0, Width - box - gap - 1), Height);
            TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.EndEllipsis;
            flags |= multiLine ? TextFormatFlags.WordBreak | TextFormatFlags.Top : TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine;
            if (multiLine) textRect.Y = Padding.Top;
            if (!ShowKeyboardCues) flags |= TextFormatFlags.HidePrefix;
            TextRenderer.DrawText(g, Text, Font, textRect, Enabled ? c.Text : c.TextDisabled, flags);

            if (Focused && ShowFocusCues)
            {
                float inset = ModernControlColors.Scale(this, 2);
                RectangleF ring = RectangleF.Inflate(boxRect, inset, inset);
                ring.Intersect(new RectangleF(0, 0, Width, Height));
                ModernGraphics.DrawRoundedRectangle(g, c.Text, ring, ModernControlColors.Scale(this, 5));
            }
        }

        internal static void DrawGlyph(Graphics g, ModernControlColors c, RectangleF boxRect, CheckState state,
                                       bool enabled, MouseState mouse, float scale)
        {
            float radius = 3.5f * scale;
            bool on = state != CheckState.Unchecked;

            if (on)
            {
                Color fill = !enabled
                    ? ModernGraphics.Blend(c.Accent, c.Background, 0.6f)
                    : mouse switch
                    {
                        MouseState.HOVER => c.AccentHover,
                        MouseState.DOWN => c.AccentPressed,
                        _ => c.Accent
                    };
                ModernGraphics.FillRoundedRectangle(g, fill, boxRect, radius);

                Color mark = enabled ? c.AccentText : ModernGraphics.Blend(c.AccentText, c.Background, 0.3f);
                SmoothingMode previous = g.SmoothingMode;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using Pen pen = new(mark, Math.Max(1.4f, boxRect.Width * 0.11f))
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round,
                    LineJoin = LineJoin.Round
                };
                float x = boxRect.X, y = boxRect.Y, w = boxRect.Width;
                if (state == CheckState.Indeterminate)
                {
                    g.DrawLine(pen, x + w * 0.28f, y + w * 0.5f, x + w * 0.72f, y + w * 0.5f);
                }
                else
                {
                    g.DrawLines(pen, new[]
                    {
                        new PointF(x + w * 0.25f, y + w * 0.52f),
                        new PointF(x + w * 0.42f, y + w * 0.69f),
                        new PointF(x + w * 0.76f, y + w * 0.33f)
                    });
                }

                g.SmoothingMode = previous;
            }
            else
            {
                Color fill = !enabled
                    ? c.InputBackgroundDisabled
                    : mouse switch
                    {
                        MouseState.HOVER => c.SurfaceHover,
                        MouseState.DOWN => c.SurfacePressed,
                        _ => c.InputBackground
                    };
                Color stroke = !enabled
                    ? ModernGraphics.Blend(c.ControlStroke, c.Background, 0.55f)
                    : mouse == MouseState.OUT ? c.ControlStroke : c.Text;
                ModernGraphics.FillRoundedRectangle(g, fill, boxRect, radius);
                ModernGraphics.DrawRoundedRectangle(g, stroke, boxRect, radius, Math.Max(1f, scale));
            }
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            //
            // NGCheckBox
            //
            Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            ResumeLayout(false);
        }
    }
}
