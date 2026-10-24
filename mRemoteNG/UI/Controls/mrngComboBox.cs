using System;
using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.Themes;
using mRemoteNG.UI.GraphicsUtilities;

namespace mRemoteNG.UI.Controls
{
    [SupportedOSPlatform("windows")]
    // Fluent style combo box: flat rounded field with a chevron, owner drawn drop-down list with rounded
    // selection. Only DropDownList combos are custom painted; editable combos keep the native look
    // (with themed colors) because the inner edit control cannot be painted reliably.
    internal class MrngComboBox : ComboBox
    {
        private bool _customPaint;

        public enum MouseState
        {
            HOVER,
            DOWN,
            OUT
        }

        // ReSharper disable once InconsistentNaming
        public MouseState _mice { get; set; }

        public MrngComboBox()
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

        protected override void OnDropDownStyleChanged(EventArgs e)
        {
            base.OnDropDownStyleChanged(e);
            if (Created) ApplyTheme();
        }

        private void ApplyTheme()
        {
            bool themed = ModernControlColors.Enabled;
            bool custom = themed && DropDownStyle == ComboBoxStyle.DropDownList;

            if (themed)
            {
                ModernControlColors c = ModernControlColors.Current;
                BackColor = c.InputBackground;
                ForeColor = c.InputText;
            }

            if (custom != _customPaint)
            {
                _customPaint = custom;
                SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, custom);
                if (custom)
                {
                    DrawMode = DrawMode.OwnerDrawVariable;
                }
                else if (DrawMode == DrawMode.OwnerDrawVariable)
                {
                    DrawMode = DrawMode.Normal;
                }
            }

            Invalidate();
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

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _mice = MouseState.DOWN;
                Invalidate();
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _mice = ClientRectangle.Contains(e.Location) ? MouseState.HOVER : MouseState.OUT;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnDropDownClosed(EventArgs e)
        {
            _mice = ClientRectangle.Contains(PointToClient(Cursor.Position)) ? MouseState.HOVER : MouseState.OUT;
            Invalidate();
            base.OnDropDownClosed(e);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
        }

        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            base.OnSelectedIndexChanged(e);
            Invalidate();
        }

        protected override void OnMeasureItem(MeasureItemEventArgs e)
        {
            if (_customPaint)
                e.ItemHeight = Math.Max(ItemHeight, Font.Height + ModernControlColors.Scale(this, 10));
            base.OnMeasureItem(e);
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (!_customPaint || e.Index < 0)
            {
                base.OnDrawItem(e);
                return;
            }

            // The edit portion is painted in OnPaint
            if ((e.State & DrawItemState.ComboBoxEdit) == DrawItemState.ComboBoxEdit)
                return;

            ModernControlColors c = ModernControlColors.Current;
            Graphics g = e.Graphics;
            using (SolidBrush background = new(c.InputBackground))
                g.FillRectangle(background, e.Bounds);

            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            int inset = ModernControlColors.Scale(this, 3);
            if (selected)
            {
                RectangleF highlight = new(e.Bounds.X + inset, e.Bounds.Y + 1, e.Bounds.Width - 2 * inset, e.Bounds.Height - 2);
                ModernGraphics.FillRoundedRectangle(g, c.Selection, highlight, ModernControlColors.Scale(this, 4));
                int pillHeight = Math.Max(4, e.Bounds.Height / 2 - 2);
                RectangleF pill = new(highlight.X, e.Bounds.Y + (e.Bounds.Height - pillHeight) / 2f,
                                      ModernControlColors.Scale(this, 3), pillHeight);
                ModernGraphics.FillRoundedRectangle(g, c.Accent, pill, pill.Width / 2f);
            }

            Rectangle textRect = new(e.Bounds.X + ModernControlColors.Scale(this, 10), e.Bounds.Y,
                                     e.Bounds.Width - ModernControlColors.Scale(this, 14), e.Bounds.Height);
            TextRenderer.DrawText(g, GetItemText(Items[e.Index]), Font, textRect,
                                  selected ? c.SelectionText : c.InputText,
                                  TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                                  TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (!_customPaint)
            {
                base.OnPaint(e);
                return;
            }

            ModernControlColors c = ModernControlColors.Current;
            Graphics g = e.Graphics;
            g.Clear(ModernControlColors.ParentBackground(this));

            Color back;
            Color border;
            Color fore = Enabled ? c.InputText : c.TextDisabled;
            if (!Enabled)
            {
                back = c.InputBackgroundDisabled;
                border = ModernGraphics.Blend(c.InputBorder, c.Background, 0.2f);
            }
            else if (DroppedDown || _mice == MouseState.DOWN)
            {
                back = c.SurfacePressed;
                border = c.BorderHover;
            }
            else if (_mice == MouseState.HOVER)
            {
                back = c.SurfaceHover;
                border = c.BorderHover;
            }
            else
            {
                back = c.InputBackground;
                border = c.InputBorder;
            }

            float radius = Math.Min(ModernControlColors.Scale(this, 4), Height / 2f);
            RectangleF bounds = new(0, 0, Width, Height);
            ModernGraphics.FillRoundedRectangle(g, back, bounds, radius);
            ModernGraphics.DrawRoundedRectangle(g, border, bounds, radius);

            if (Focused && Enabled && !DroppedDown)
            {
                // focus: accent underline like the text boxes
                int thickness = Math.Max(1, ModernControlColors.Scale(this, 2));
                using System.Drawing.Drawing2D.GraphicsPath shape = ModernGraphics.CreateRoundedRectangle(bounds, radius);
                g.SetClip(shape);
                using SolidBrush accent = new(c.Accent);
                g.FillRectangle(accent, 0, Height - thickness, Width, thickness);
                g.ResetClip();
            }

            int chevronSize = ModernControlColors.Scale(this, 12);
            int chevronArea = ModernControlColors.Scale(this, 26);
            Rectangle chevron = new(Width - chevronArea + (chevronArea - chevronSize) / 2 - ModernControlColors.Scale(this, 2),
                                    (Height - chevronSize) / 2, chevronSize, chevronSize);
            FluentIcons.Draw(g, FluentIcons.ChevronDown, chevron, Enabled ? c.TextSecondary : c.TextDisabled,
                             chevronSize * 0.75f);

            Rectangle textRect = new(ModernControlColors.Scale(this, 8), 0,
                                     Math.Max(0, Width - chevronArea - ModernControlColors.Scale(this, 8)), Height);
            TextRenderer.DrawText(g, Text, Font, textRect, fore,
                                  TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                                  TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
    }
}
