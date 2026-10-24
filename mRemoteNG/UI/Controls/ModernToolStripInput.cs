#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.Themes;
using mRemoteNG.UI.GraphicsUtilities;

namespace mRemoteNG.UI.Controls
{
    /// <summary>
    /// Rounded, search-style text input for tool strips: a borderless text box on a rounded surface with an
    /// optional leading Fluent glyph, a placeholder and small trailing "pill" buttons (e.g. a protocol picker).
    /// Colors follow <see cref="ModernPalette.Current"/> and update on theme changes.
    /// </summary>
    [SupportedOSPlatform("windows")]
    [ToolboxItem(false)]
    public class ModernInputBox : Control
    {
        /// <summary>A clickable area drawn inside the right end of the input.</summary>
        public sealed class TrailingButton
        {
            public string? Text { get; set; }
            public char? Glyph { get; set; }
            public bool ShowChevron { get; set; }
            public bool Visible { get; set; } = true;
            public string? ToolTip { get; set; }

            /// <summary>Invoked with the button bounds (client coordinates of the input).</summary>
            public Action<Rectangle>? Click { get; set; }

            internal Rectangle Bounds { get; set; }
        }

        private const char ChevronDownSmallGlyph = '';
        public const int LogicalHeight = 30;

        private readonly TextBox _textBox;
        private readonly List<TrailingButton> _trailingButtons = [];
        private readonly ToolTip _toolTip = new();
        // Shared, never-disposed font: the hosted TextBox may recreate its handle at any time (re-parenting by
        // the tool strip, theme changes...), so it must never reference a font that could be disposed.
        private static Font _font => ModernToolStripRenderer.UiFont;
        private TrailingButton? _hoverButton;
        private TrailingButton? _pressedButton;
        private bool _mouseOver;
        private char? _leadingGlyph;

        public ModernInputBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);

            _textBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = _font,
                Multiline = false
            };
            _textBox.GotFocus += (_, _) => Invalidate();
            _textBox.LostFocus += (_, _) => Invalidate();
            _textBox.MouseEnter += (_, _) => SetMouseOver(true);
            _textBox.MouseLeave += (_, _) => SetMouseOver(ClientRectangle.Contains(PointToClient(MousePosition)));
            _textBox.TextChanged += (_, _) => OnTextChanged(EventArgs.Empty);
            Controls.Add(_textBox);

            Size = new Size(240, LogicalHeight);
            Cursor = Cursors.IBeam;

            ThemeManager.getInstance().ThemeChanged += ApplyTheme;
            ApplyTheme();
        }

        /// <summary>The hosted text box (for key handling, auto completion...).</summary>
        public TextBox InnerTextBox => _textBox;

        [AllowNull]
        public override string Text
        {
            get => _textBox.Text;
            set => _textBox.Text = value ?? string.Empty;
        }

        public string PlaceholderText
        {
            get => _textBox.PlaceholderText;
            set => _textBox.PlaceholderText = value;
        }

        public char? LeadingGlyph
        {
            get => _leadingGlyph;
            set
            {
                _leadingGlyph = value;
                LayoutContent();
                Invalidate();
            }
        }

        public IList<TrailingButton> TrailingButtons => _trailingButtons;

        public bool IsInputFocused => _textBox.Focused;

        public void FocusInput()
        {
            _textBox.Focus();
            _textBox.SelectionStart = _textBox.TextLength;
        }

        /// <summary>Re-measures and repaints after trailing buttons were changed.</summary>
        public void RefreshButtons()
        {
            LayoutContent();
            Invalidate();
        }

        private int Scale(int logical) => (int)Math.Round(logical * DeviceDpi / 96f);

        private void ApplyTheme()
        {
            ModernPalette palette = ModernPalette.Current;
            _textBox.BackColor = palette.Surface;
            _textBox.ForeColor = palette.Text;
            Invalidate();
        }

        private void SetMouseOver(bool value)
        {
            if (_mouseOver == value)
                return;
            _mouseOver = value;
            Invalidate();
        }

        #region Layout

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            LayoutContent();
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            LayoutContent();
            Invalidate();
        }

        private void LayoutContent()
        {
            if (Width <= 0 || Height <= 0)
                return;

            int padding = Scale(10);
            int left = padding;
            if (_leadingGlyph.HasValue)
                left += Scale(16) + Scale(8);

            int right = Width - Scale(4);
            for (int i = _trailingButtons.Count - 1; i >= 0; i--)
            {
                TrailingButton button = _trailingButtons[i];
                if (!button.Visible)
                {
                    button.Bounds = Rectangle.Empty;
                    continue;
                }

                int width = Scale(8) * 2;
                if (!string.IsNullOrEmpty(button.Text))
                    width += TextRenderer.MeasureText(button.Text, _font, Size.Empty, TextFormatFlags.NoPadding).Width;
                if (button.Glyph.HasValue)
                    width += Scale(14) + (string.IsNullOrEmpty(button.Text) ? 0 : Scale(6));
                if (button.ShowChevron)
                    width += Scale(10) + Scale(4);
                if (string.IsNullOrEmpty(button.Text) && !button.ShowChevron)
                    width = Height - Scale(8);

                int height = Height - Scale(8);
                button.Bounds = new Rectangle(right - width, (Height - height) / 2, width, height);
                right -= width + Scale(2);
            }

            int textHeight = _textBox.PreferredHeight;
            int textRight = Math.Max(left + Scale(20), right - Scale(4));
            _textBox.SetBounds(left, (Height - textHeight) / 2, textRight - left, textHeight);
        }

        #endregion

        #region Painting

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            pevent.Graphics.Clear(OuterBackground());
        }

        private Color OuterBackground()
        {
            if (Parent is ToolStrip toolStrip)
                return ModernToolStripRenderer.StripBackground(toolStrip);
            return Parent?.BackColor ?? ModernPalette.Current.Chrome;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ModernPalette palette = ModernPalette.Current;
            Graphics g = e.Graphics;
            RectangleF bounds = new(0.5f, 0.5f, Width - 1f, Height - 1f);
            float radius = Scale(ModernPalette.CornerRadius);

            bool focused = _textBox.Focused;
            ModernGraphics.FillRoundedRectangle(g, palette.Surface, bounds, radius);

            Color border = focused
                ? palette.Accent
                : _mouseOver ? ModernGraphics.Blend(palette.Border, palette.TextSecondary, 0.35f) : palette.Border;
            ModernGraphics.DrawRoundedRectangle(g, border, new RectangleF(0, 0, Width, Height), radius, focused ? Math.Max(1f, Scale(1) * 1.5f) : 1f);

            if (_leadingGlyph.HasValue)
            {
                int size = Scale(16);
                Rectangle glyphBounds = new(Scale(10), (Height - size) / 2, size, size);
                ModernToolStripRenderer.DrawGlyph(g, _leadingGlyph.Value, glyphBounds, focused ? palette.Accent : palette.TextSecondary, Scale(14));
            }

            foreach (TrailingButton button in _trailingButtons)
            {
                if (!button.Visible || button.Bounds.IsEmpty)
                    continue;
                DrawTrailingButton(g, button, palette);
            }
        }

        private void DrawTrailingButton(Graphics g, TrailingButton button, ModernPalette palette)
        {
            Rectangle r = button.Bounds;
            bool isText = !string.IsNullOrEmpty(button.Text);
            Color fill = button == _pressedButton ? palette.SurfacePressed
                : button == _hoverButton ? palette.SurfaceHover
                : isText ? ModernGraphics.Blend(palette.Surface, palette.SurfaceHover, 0.6f) : Color.Empty;
            if (!fill.IsEmpty)
                ModernGraphics.FillRoundedRectangle(g, fill, r, Scale(4));

            Color fore = button == _hoverButton ? palette.Text : palette.TextSecondary;
            int x = r.Left + Scale(8);
            if (!isText && !button.ShowChevron && button.Glyph.HasValue)
            {
                ModernToolStripRenderer.DrawGlyph(g, button.Glyph.Value, r, fore, Scale(13));
                return;
            }

            if (button.Glyph.HasValue)
            {
                int size = Scale(14);
                ModernToolStripRenderer.DrawGlyph(g, button.Glyph.Value, new Rectangle(x, r.Top + (r.Height - size) / 2, size, size), fore, Scale(13));
                x += size + Scale(6);
            }

            if (isText)
            {
                Size textSize = TextRenderer.MeasureText(g, button.Text, _font, Size.Empty, TextFormatFlags.NoPadding);
                Rectangle textBounds = new(x, r.Top, textSize.Width + 1, r.Height);
                TextRenderer.DrawText(g, button.Text, _font, textBounds, fore,
                                      TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.SingleLine);
                x += textSize.Width + Scale(4);
            }

            if (button.ShowChevron)
            {
                int size = Scale(12);
                ModernToolStripRenderer.DrawGlyph(g, ChevronDownSmallGlyph, new Rectangle(x, r.Top + (r.Height - size) / 2, size, size), fore, Scale(9));
            }
        }

        #endregion

        #region Mouse

        private TrailingButton? HitTest(Point location)
        {
            foreach (TrailingButton button in _trailingButtons)
            {
                if (button.Visible && button.Bounds.Contains(location))
                    return button;
            }

            return null;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            SetMouseOver(true);
            TrailingButton? hit = HitTest(e.Location);
            Cursor = hit != null ? Cursors.Hand : Cursors.IBeam;
            if (hit == _hoverButton)
                return;

            _hoverButton = hit;
            _toolTip.SetToolTip(this, hit?.ToolTip ?? string.Empty);
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoverButton = null;
            _pressedButton = null;
            SetMouseOver(_textBox.ClientRectangle.Contains(_textBox.PointToClient(MousePosition)));
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left)
                return;

            _pressedButton = HitTest(e.Location);
            if (_pressedButton == null)
                FocusInput();
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            TrailingButton? pressed = _pressedButton;
            _pressedButton = null;
            Invalidate();
            if (pressed != null && pressed == HitTest(e.Location))
                pressed.Click?.Invoke(pressed.Bounds);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            _textBox.Focus();
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ThemeManager.getInstance().ThemeChanged -= ApplyTheme;
                _toolTip.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    /// <summary>Hosts a <see cref="ModernInputBox"/> in a tool strip with a fixed, DPI-scaled size.</summary>
    [SupportedOSPlatform("windows")]
    [ToolboxItem(false)]
    public class ModernInputHost : ToolStripControlHost
    {
        private readonly int _logicalWidth;

        public ModernInputHost(ModernInputBox input, int logicalWidth) : base(input)
        {
            _logicalWidth = logicalWidth;
            AutoSize = false;
            ApplySize();
            input.DpiChangedAfterParent += (_, _) => ApplySize();
        }

        public ModernInputBox Input => (ModernInputBox)Control;

        private void ApplySize()
        {
            float scale = Control.DeviceDpi / 96f;
            Size = new Size((int)Math.Round(_logicalWidth * scale), (int)Math.Round(ModernInputBox.LogicalHeight * scale));
            Margin = new Padding((int)Math.Round(2 * scale), 0, (int)Math.Round(4 * scale), 0);
        }
    }
}
