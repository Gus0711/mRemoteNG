using System;
using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.Resources.Language;
using mRemoteNG.Themes;
using mRemoteNG.UI.GraphicsUtilities;

namespace mRemoteNG.UI.Controls.ConnectionTree
{
    /// <summary>
    /// Rounded search field of the connection tree: search glyph, borderless text box with a
    /// placeholder, and a clear button shown while there is text. The field gets an accent
    /// underline while focused, like Windows 11 text boxes.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class ConnectionTreeSearchPanel : Panel
    {
        private const int FieldHeight = 30;
        private const int OuterPaddingX = 8;
        private const int OuterPaddingY = 6;
        private const int GlyphBoxWidth = 30;
        private const int ClearBoxWidth = 28;

        private MrngSearchBox? _searchBox;
        private readonly ToolTip _toolTip = new();
        private bool _clearHot;
        private bool _clearPressed;
        private bool _fieldHot;

        public ConnectionTreeSearchPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            UpdateHeight();
        }

        /// <summary>The text box hosted in the field.</summary>
        public MrngSearchBox? SearchBox
        {
            get => _searchBox;
            set
            {
                if (_searchBox != null)
                {
                    _searchBox.TextChanged -= SearchBoxOnTextChanged;
                    _searchBox.MouseEnter -= SearchBoxOnMouseEnter;
                    _searchBox.MouseLeave -= SearchBoxOnMouseLeave;
                    Controls.Remove(_searchBox);
                }

                _searchBox = value;
                if (_searchBox == null) return;

                _searchBox.BorderStyle = BorderStyle.None;
                _searchBox.Dock = DockStyle.None;
                _searchBox.Anchor = AnchorStyles.None;
                _searchBox.TextChanged += SearchBoxOnTextChanged;
                _searchBox.MouseEnter += SearchBoxOnMouseEnter;
                _searchBox.MouseLeave += SearchBoxOnMouseLeave;
                if (!Controls.Contains(_searchBox))
                    Controls.Add(_searchBox);
                ApplyTheme();
                PerformLayout();
            }
        }

        private int S(int logical) => (int)Math.Round(logical * DeviceDpi / 96f);

        private bool HasText => _searchBox is { TextLength: > 0 };

        private Rectangle FieldBounds =>
            new(S(OuterPaddingX), S(OuterPaddingY), Math.Max(0, ClientSize.Width - 2 * S(OuterPaddingX)), S(FieldHeight));

        private Rectangle ClearBounds
        {
            get
            {
                Rectangle fieldBounds = FieldBounds;
                int size = S(ClearBoxWidth) - S(6);
                return new Rectangle(fieldBounds.Right - S(ClearBoxWidth) + S(2), fieldBounds.Y + (fieldBounds.Height - size) / 2, size, size);
            }
        }

        private void UpdateHeight()
        {
            Height = S(FieldHeight) + 2 * S(OuterPaddingY);
        }

        public void ApplyLanguage()
        {
            if (_searchBox != null)
                _searchBox.CueText = Language.SearchPrompt;
        }

        public void ApplyTheme()
        {
            ModernPalette palette = ModernPalette.Current;
            ThemeManager themeManager = ThemeManager.getInstance();
            BackColor = themeManager.ActiveAndExtended
                ? themeManager.ActiveTheme.ExtendedPalette.getColor("TreeView_Background")
                : SystemColors.Window;

            if (_searchBox != null)
            {
                _searchBox.BackColor = FieldFill(palette);
                _searchBox.ForeColor = themeManager.ThemingActive ? palette.Text : SystemColors.WindowText;
                _searchBox.Invalidate();
            }

            Invalidate();
        }

        private Color FieldFill(ModernPalette palette)
        {
            if (!ThemeManager.getInstance().ThemingActive)
                return SystemColors.Window;
            return palette.Surface;
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            UpdateHeight();
            PerformLayout();
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            if (_searchBox == null) return;

            Rectangle field = FieldBounds;
            int left = field.X + S(GlyphBoxWidth);
            int right = field.Right - S(ClearBoxWidth);
            int height = _searchBox.PreferredHeight;
            _searchBox.SetBounds(left, field.Y + (field.Height - height) / 2 + 1, Math.Max(10, right - left), height);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            ModernPalette palette = ModernPalette.Current;
            using (SolidBrush back = new(BackColor))
                g.FillRectangle(back, ClientRectangle);

            Rectangle field = FieldBounds;
            if (field.Width <= 0) return;

            bool focused = _searchBox?.Focused == true;
            float radius = S(ModernPalette.CornerRadius);
            Color fill = FieldFill(palette);
            Color border = focused ? palette.Border : _fieldHot ? ModernGraphics.Blend(palette.Border, palette.TextSecondary, 0.35f) : palette.Border;

            ModernGraphics.FillRoundedRectangle(g, fill, field, radius);
            ModernGraphics.DrawRoundedRectangle(g, border, field, radius);

            if (focused)
            {
                // Windows 11 style focus: accent underline clipped to the rounded shape.
                using System.Drawing.Drawing2D.GraphicsPath clip = ModernGraphics.CreateRoundedRectangle(field, radius);
                Region previous = g.Clip;
                g.SetClip(clip, System.Drawing.Drawing2D.CombineMode.Intersect);
                using (SolidBrush accent = new(palette.Accent))
                    g.FillRectangle(accent, field.X, field.Bottom - S(2), field.Width, S(2));
                g.Clip = previous;
            }

            Rectangle glyph = new(field.X, field.Y, S(GlyphBoxWidth), field.Height);
            FluentIcons.Draw(g, FluentIcons.Search, glyph, focused ? palette.Text : palette.TextSecondary, S(13));

            if (HasText)
            {
                Rectangle clear = ClearBounds;
                if (_clearHot || _clearPressed)
                    ModernGraphics.FillRoundedRectangle(g, _clearPressed ? palette.SurfacePressed : palette.SurfaceHover, clear, S(4));
                FluentIcons.Draw(g, FluentIcons.Cancel, clear, _clearHot ? palette.Text : palette.TextSecondary, S(10));
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool fieldHot = FieldBounds.Contains(e.Location);
            bool clearHot = HasText && ClearBounds.Contains(e.Location);
            if (clearHot != _clearHot || fieldHot != _fieldHot)
            {
                _clearHot = clearHot;
                _fieldHot = fieldHot;
                Cursor = clearHot ? Cursors.Default : fieldHot ? Cursors.IBeam : Cursors.Default;
                if (clearHot)
                    _toolTip.SetToolTip(this, Language.ClearSearchString);
                else
                    _toolTip.SetToolTip(this, null);
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _clearHot = false;
            _clearPressed = false;
            _fieldHot = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            if (HasText && ClearBounds.Contains(e.Location))
            {
                _clearPressed = true;
                Invalidate();
                return;
            }

            if (FieldBounds.Contains(e.Location))
                _searchBox?.Focus();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (!_clearPressed) return;
            _clearPressed = false;
            if (HasText && ClearBounds.Contains(e.Location) && _searchBox != null)
            {
                _searchBox.Text = string.Empty;
                _searchBox.Focus();
            }

            Invalidate();
        }

        private void SearchBoxOnTextChanged(object? sender, EventArgs e) => Invalidate();

        private void SearchBoxOnMouseEnter(object? sender, EventArgs e)
        {
            _fieldHot = true;
            Invalidate();
        }

        private void SearchBoxOnMouseLeave(object? sender, EventArgs e)
        {
            _fieldHot = PointToClient(MousePosition) is var p && FieldBounds.Contains(p);
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _toolTip.Dispose();
            base.Dispose(disposing);
        }
    }
}
