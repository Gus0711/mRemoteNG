using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.Themes;
using mRemoteNG.UI.GraphicsUtilities;

namespace mRemoteNG.UI.Controls
{
    [SupportedOSPlatform("windows")]
    [ToolboxBitmap(typeof(Button))]
    // Fluent style button: rounded surface with a subtle border, hover/pressed states and an
    // accent ("primary") variant used for the default button of a dialog.
    public class MrngButton : Button
    {
        private bool _primary;

        /// <summary>
        /// Store the mouse state, required for coloring the component according to the mouse state
        /// </summary>
        public enum MouseState
        {
            HOVER,
            DOWN,
            OUT
        }

        public MrngButton()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            ThemeManager.getInstance().ThemeChanged += OnThemeChanged;
        }

        // ReSharper disable once InconsistentNaming
        public MouseState _mice { get; set; }

        /// <summary>
        /// Paints the button with the accent color. Buttons set as the form's AcceptButton are primary automatically.
        /// </summary>
        [DefaultValue(false)]
        [Category("Appearance")]
        public bool Primary
        {
            get => _primary;
            set
            {
                if (_primary == value) return;
                _primary = value;
                Invalidate();
            }
        }

        private bool IsPrimary
        {
            get
            {
                if (_primary) return true;
                Form? form = FindForm();
                return form != null && ReferenceEquals(form.AcceptButton, this);
            }
        }

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

        protected override void OnMouseEnter(EventArgs e)
        {
            _mice = MouseState.HOVER;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _mice = MouseState.OUT;
            Invalidate();
            base.OnMouseLeave(e);
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

        protected override void OnEnabledChanged(EventArgs e)
        {
            if (!Enabled) _mice = MouseState.OUT;
            base.OnEnabledChanged(e);
            Invalidate();
        }

        /// <summary>
        /// Repaint the component: rounded background, border, optional image and the text
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            if (!ModernControlColors.Enabled)
            {
                base.OnPaint(e);
                return;
            }

            ModernControlColors c = ModernControlColors.Current;
            Graphics g = e.Graphics;
            bool primary = IsPrimary;

            Color back;
            Color fore;
            Color border;
            if (!Enabled)
            {
                back = primary ? ModernGraphics.Blend(c.Accent, c.Background, 0.55f) : c.SurfaceDisabled;
                fore = primary ? ModernGraphics.Blend(c.AccentText, c.Background, 0.35f) : c.TextDisabled;
                border = primary ? back : ModernGraphics.Blend(c.Border, c.Background, 0.4f);
            }
            else if (primary)
            {
                back = _mice switch
                {
                    MouseState.HOVER => c.AccentHover,
                    MouseState.DOWN => c.AccentPressed,
                    _ => c.Accent
                };
                fore = c.AccentText;
                border = _mice == MouseState.DOWN ? back : ModernGraphics.Blend(back, Color.White, 0.08f);
            }
            else
            {
                back = _mice switch
                {
                    MouseState.HOVER => c.SurfaceHover,
                    MouseState.DOWN => c.SurfacePressed,
                    _ => c.Surface
                };
                fore = _mice == MouseState.DOWN ? c.TextSecondary : c.Text;
                border = _mice == MouseState.HOVER ? c.BorderHover : c.Border;
            }

            g.Clear(ModernControlColors.ParentBackground(this));
            float radius = ModernControlColors.Scale(this, ModernPalette.CornerRadius) * 0.75f;
            RectangleF bounds = new(0, 0, Width, Height);
            ModernGraphics.FillRoundedRectangle(g, back, bounds, radius);
            ModernGraphics.DrawRoundedRectangle(g, border, bounds, radius);

            // keyboard focus ring
            if (Focused && ShowFocusCues && Enabled)
            {
                float inset = ModernControlColors.Scale(this, 2);
                RectangleF focus = RectangleF.Inflate(bounds, -inset, -inset);
                ModernGraphics.DrawRoundedRectangle(g, primary ? c.AccentText : c.Accent, focus,
                                                    Math.Max(1, radius - inset), Math.Max(1f, ModernControlColors.DpiScale(this)));
            }

            TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                                    TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
            if (!ShowKeyboardCues) flags |= TextFormatFlags.HidePrefix;

            Rectangle textRect = ClientRectangle;
            if (Image != null)
            {
                int gap = ModernControlColors.Scale(this, 6);
                Size textSize = string.IsNullOrEmpty(Text)
                    ? Size.Empty
                    : TextRenderer.MeasureText(g, Text, Font, Size.Empty, flags | TextFormatFlags.NoPadding);
                int contentWidth = Image.Width + (textSize.Width > 0 ? gap + textSize.Width : 0);
                int x = Math.Max(ModernControlColors.Scale(this, 4), (Width - contentWidth) / 2);
                int y = (Height - Image.Height) / 2;
                if (Enabled)
                    g.DrawImage(Image, x, y, Image.Width, Image.Height);
                else
                    ControlPaint.DrawImageDisabled(g, Image, x, y, back);
                textRect = new Rectangle(x + Image.Width + gap, 0, Math.Max(0, Width - (x + Image.Width + gap)), Height);
                flags = (flags & ~TextFormatFlags.HorizontalCenter) | TextFormatFlags.Left | TextFormatFlags.NoPadding;
            }

            g.SmoothingMode = SmoothingMode.None;
            TextRenderer.DrawText(g, Text, Font, textRect, fore, flags);
        }
    }
}
