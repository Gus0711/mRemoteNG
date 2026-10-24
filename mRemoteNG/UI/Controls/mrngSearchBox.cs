using System;
using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.Themes;

namespace mRemoteNG.UI.Controls
{
    /// <summary>
    /// Borderless text box used inside the connection tree search panel.
    /// Shows <see cref="CueText"/> as a placeholder (in the theme's secondary text color) while empty,
    /// so the real <see cref="Control.Text"/> is always just what the user typed.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class MrngSearchBox : TextBox
    {
        private const int WM_PAINT = 0x000F;
        private string _cueText = string.Empty;

        public MrngSearchBox()
        {
            BorderStyle = BorderStyle.None;
        }

        /// <summary>Placeholder shown while the box is empty.</summary>
        public string CueText
        {
            get => _cueText;
            set
            {
                _cueText = value ?? string.Empty;
                Invalidate();
            }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == WM_PAINT && TextLength == 0 && _cueText.Length > 0)
                DrawCueText();
        }

        private void DrawCueText()
        {
            using Graphics g = CreateGraphics();
            Color color = ModernPalette.Current.TextSecondary;
            if (!ThemeManager.getInstance().ThemingActive)
                color = SystemColors.GrayText;
            // EM_GETMARGINS default left margin of a borderless edit is ~1px at 96 DPI
            Rectangle bounds = ClientRectangle;
            bounds.X += 1;
            TextRenderer.DrawText(g, _cueText, Font, bounds, color, BackColor,
                TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            if (TextLength == 0)
                Invalidate();
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Parent?.Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Parent?.Invalidate();
        }
    }
}
