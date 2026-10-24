#nullable enable
using System;
using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.Themes;

namespace mRemoteNG.UI.Modern
{
    /// <summary>
    /// Borderless text box that keeps showing its placeholder (in the theme's secondary text color)
    /// while it has the focus, like modern search fields. The standard PlaceholderText disappears on focus.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal sealed class PlaceholderTextBox : TextBox
    {
        private const int WM_PAINT = 0x000F;
        private string _placeholder = string.Empty;

        public PlaceholderTextBox()
        {
            BorderStyle = BorderStyle.None;
        }

        public string Placeholder
        {
            get => _placeholder;
            set
            {
                _placeholder = value ?? string.Empty;
                Invalidate();
            }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Invalidate();
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

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg != WM_PAINT || TextLength > 0 || _placeholder.Length == 0 || !IsHandleCreated) return;

            using Graphics g = Graphics.FromHwnd(Handle);
            Color color = ModernPalette.Current.TextSecondary;
            TextRenderer.DrawText(g, _placeholder, Font, new Rectangle(1, 0, ClientSize.Width - 1, ClientSize.Height), color, BackColor,
                                  TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.Left | TextFormatFlags.Top);
        }
    }
}
