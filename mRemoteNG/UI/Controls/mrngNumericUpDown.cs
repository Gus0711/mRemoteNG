using System;
using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.Themes;
using mRemoteNG.UI.GraphicsUtilities;

// ReSharper disable LocalizableElement

namespace mRemoteNG.UI.Controls
{
    [SupportedOSPlatform("windows")]
    // Fluent style NumericUpDown: rounded field like MrngTextBox, the native spin buttons (which cannot be
    // themed) are replaced by small chevron buttons with hover states and auto-repeat.
    internal class MrngNumericUpDown : NumericUpDown
    {
        private SpinButton? _up;
        private SpinButton? _down;
        private Control? _nativeButtons;
        private bool _hover;

        public MrngNumericUpDown()
        {
            ThemeManager.getInstance().ThemeChanged += OnThemeChanged;
        }

        private void OnThemeChanged()
        {
            if (IsDisposed) return;
            ApplyTheme();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                ThemeManager.getInstance().ThemeChanged -= OnThemeChanged;
            base.Dispose(disposing);
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            bool themed = ModernControlColors.Enabled;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

            if (themed)
            {
                ModernControlColors c = ModernControlColors.Current;
                ForeColor = c.InputText;
                UpdateBackColor();
                EnsureSpinButtons();
            }
            else
            {
                RestoreNativeButtons();
            }

            Invalidate(true);
        }

        private void UpdateBackColor()
        {
            if (!ModernControlColors.Enabled) return;
            ModernControlColors c = ModernControlColors.Current;
            BackColor = !Enabled || ReadOnly
                ? c.InputBackgroundDisabled
                : ContainsFocus ? c.InputBackgroundFocused : c.InputBackground;
        }

        private void EnsureSpinButtons()
        {
            if (_up != null) return;

            foreach (Control child in Controls)
            {
                if (child.GetType().FullName != "System.Windows.Forms.UpDownBase+UpDownButtons") continue;
                _nativeButtons = child;
                break;
            }

            if (_nativeButtons != null)
                _nativeButtons.Visible = false;

            _up = new SpinButton(this, true);
            _down = new SpinButton(this, false);
            Controls.Add(_up);
            Controls.Add(_down);
            _up.BringToFront();
            _down.BringToFront();
            LayoutSpinButtons();
        }

        private void RestoreNativeButtons()
        {
            if (_up == null) return;
            Controls.Remove(_up);
            Controls.Remove(_down);
            _up.Dispose();
            _down?.Dispose();
            _up = null;
            _down = null;
            if (_nativeButtons != null)
                _nativeButtons.Visible = true;
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            LayoutSpinButtons();
        }

        private void LayoutSpinButtons()
        {
            if (_up == null || _down == null) return;
            int border = ModernControlColors.Scale(this, 2);
            int width = _nativeButtons?.Width > 0 ? _nativeButtons.Width : ModernControlColors.Scale(this, 16);
            int x = Width - width - border;
            int innerHeight = Math.Max(2, Height - 2 * border);
            int half = innerHeight / 2;
            _up.SetBounds(x, border, width, half);
            _down.SetBounds(x, border + half, width, innerHeight - half);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            UpdateBackColor();
            base.OnEnabledChanged(e);
            Invalidate(true);
        }

        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            UpdateBackColor();
            Invalidate();
        }

        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);
            UpdateBackColor();
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = ClientRectangle.Contains(PointToClient(Cursor.Position));
            Invalidate();
            base.OnMouseLeave(e);
        }

        internal void SpinUp() => UpButton();
        internal void SpinDown() => DownButton();

        protected override void OnPaint(PaintEventArgs e)
        {
            if (!ModernControlColors.Enabled)
            {
                base.OnPaint(e);
                return;
            }

            ModernControlColors c = ModernControlColors.Current;
            Graphics g = e.Graphics;
            g.Clear(ModernControlColors.ParentBackground(this));
            if (BorderStyle == BorderStyle.None)
            {
                using SolidBrush b = new(BackColor);
                g.FillRectangle(b, ClientRectangle);
                return;
            }

            bool focused = ContainsFocus && Enabled;
            Color border = !Enabled
                ? ModernGraphics.Blend(c.InputBorder, c.Background, 0.2f)
                : focused
                    ? ModernGraphics.Blend(c.InputBorder, c.Accent, 0.35f)
                    : _hover ? c.BorderHover : c.InputBorder;

            RectangleF bounds = new(0, 0, Width, Height);
            float radius = Math.Min(ModernControlColors.Scale(this, 4), Height / 2f);
            ModernGraphics.FillRoundedRectangle(g, BackColor, bounds, radius);
            ModernGraphics.DrawRoundedRectangle(g, border, bounds, radius);

            if (focused)
            {
                int thickness = Math.Max(1, ModernControlColors.Scale(this, 2));
                using System.Drawing.Drawing2D.GraphicsPath shape = ModernGraphics.CreateRoundedRectangle(bounds, radius);
                g.SetClip(shape);
                using SolidBrush accent = new(c.Accent);
                g.FillRectangle(accent, 0, Height - thickness, Width, thickness);
                g.ResetClip();
            }
        }

        /// <summary>Chevron spin button with hover/pressed state and auto-repeat while held.</summary>
        private sealed class SpinButton : Control
        {
            private readonly MrngNumericUpDown _owner;
            private readonly bool _isUp;
            private readonly Timer _repeat = new();
            private bool _hover;
            private bool _pressed;

            public SpinButton(MrngNumericUpDown owner, bool isUp)
            {
                _owner = owner;
                _isUp = isUp;
                TabStop = false;
                Cursor = Cursors.Default;
                SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
                SetStyle(ControlStyles.Selectable, false);
                _repeat.Tick += (_, _) =>
                {
                    _repeat.Interval = 50;
                    Spin();
                };
            }

            private void Spin()
            {
                if (_isUp) _owner.SpinUp();
                else _owner.SpinDown();
            }

            protected override void OnMouseEnter(EventArgs e)
            {
                _hover = true;
                Invalidate();
                base.OnMouseEnter(e);
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                _hover = false;
                Invalidate();
                base.OnMouseLeave(e);
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);
                if (e.Button != MouseButtons.Left || !_owner.Enabled) return;
                _pressed = true;
                _owner.Focus();
                Spin();
                _repeat.Interval = 400;
                _repeat.Start();
                Invalidate();
            }

            protected override void OnMouseUp(MouseEventArgs e)
            {
                base.OnMouseUp(e);
                _pressed = false;
                _repeat.Stop();
                Invalidate();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing) _repeat.Dispose();
                base.Dispose(disposing);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                ModernControlColors c = ModernControlColors.Current;
                Graphics g = e.Graphics;
                g.Clear(_owner.BackColor);
                if (_owner.Enabled && (_hover || _pressed))
                {
                    RectangleF r = new(1, _isUp ? 1 : 0, Width - 2, Height - 1);
                    ModernGraphics.FillRoundedRectangle(g, _pressed ? c.SurfacePressed : c.SurfaceHover, r,
                                                        ModernControlColors.Scale(this, 3));
                }

                int glyph = Math.Max(6, Math.Min(Width, Height + 2) - ModernControlColors.Scale(this, 4));
                Rectangle rect = new((Width - glyph) / 2, (Height - glyph) / 2, glyph, glyph);
                FluentIcons.Draw(g, _isUp ? ModernGlyphs.ChevronUpSmall : ModernGlyphs.ChevronDownSmall, rect,
                                 _owner.Enabled ? (_pressed ? c.Text : c.TextSecondary) : c.TextDisabled, glyph * 0.8f);
            }
        }
    }
}
