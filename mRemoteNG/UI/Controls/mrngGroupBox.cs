using System;
using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.Themes;
using mRemoteNG.UI.GraphicsUtilities;

namespace mRemoteNG.UI.Controls
{
    [SupportedOSPlatform("windows")]
    // Group box rendered as a Fluent "card": rounded, slightly raised surface with a subtle border and the
    // title in semibold secondary text inside the top-left corner.
    public class MrngGroupBox : GroupBox
    {
        private bool _appliedCardColor;
        private Font? _titleFont;

        public MrngGroupBox()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            ThemeManager.getInstance().ThemeChanged += OnThemeChanged;
        }

        private void OnThemeChanged()
        {
            if (IsDisposed) return;
            ApplyCardColor();
            Invalidate(true);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                ThemeManager.getInstance().ThemeChanged -= OnThemeChanged;

            base.Dispose(disposing);

            // The title font is only used for drawing (never assigned to Control.Font); release it last.
            if (disposing)
            {
                _titleFont?.Dispose();
                _titleFont = null;
            }
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            ApplyCardColor();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            _titleFont?.Dispose();
            _titleFont = null;
            base.OnFontChanged(e);
        }

        // Children inherit the card color (ambient BackColor) so labels/check boxes blend with the card.
        private void ApplyCardColor()
        {
            if (ModernControlColors.Enabled)
            {
                BackColor = ModernControlColors.Current.Card;
                _appliedCardColor = true;
            }
            else if (_appliedCardColor)
            {
                ResetBackColor();
                _appliedCardColor = false;
            }
        }

        private Font TitleFont => _titleFont ??= ModernFonts.CreateSemibold(Font.SizeInPoints);

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            if (!ModernControlColors.Enabled)
            {
                base.OnPaintBackground(pevent);
                return;
            }

            pevent.Graphics.Clear(ModernControlColors.ParentBackground(this));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (!ModernControlColors.Enabled)
            {
                base.OnPaint(e);
                return;
            }

            ModernControlColors c = ModernControlColors.Current;
            Graphics g = e.Graphics;
            float radius = ModernControlColors.Scale(this, ModernPalette.LargeCornerRadius);
            RectangleF bounds = new(0, 0, Width, Height);
            ModernGraphics.FillRoundedRectangle(g, c.Card, bounds, radius);
            ModernGraphics.DrawRoundedRectangle(g, c.CardBorder, bounds, radius);

            if (string.IsNullOrEmpty(Text)) return;

            TextFormatFlags flags = TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.Top;
            if (!ShowKeyboardCues) flags |= TextFormatFlags.HidePrefix;
            int padX = ModernControlColors.Scale(this, 9);
            int padY = ModernControlColors.Scale(this, 2);
            Rectangle titleRect = new(padX, padY, Math.Max(0, Width - 2 * padX), TitleFont.Height + padY);
            if (RightToLeft == RightToLeft.Yes)
                flags |= TextFormatFlags.Right | TextFormatFlags.RightToLeft;
            TextRenderer.DrawText(g, Text, TitleFont, titleRect, Enabled ? c.TextSecondary : c.TextDisabled, flags);
        }
    }
}
