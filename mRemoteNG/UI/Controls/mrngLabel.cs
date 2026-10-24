using System;
using mRemoteNG.Themes;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.Versioning;

namespace mRemoteNG.UI.Controls
{
    [SupportedOSPlatform("windows")]
    //Themable label to overide the winforms behavior of drawing the forecolor of disabled with a system color
    //This class repaints the control to avoid Disabled state mismatch of the theme
    [ToolboxBitmap(typeof(Label))]
    public class MrngLabel : Label
    {
        private ThemeManager _themeManager;
        private TextFormatFlags _textFormatFlags;

        public MrngLabel()
        {
            _themeManager = ThemeManager.getInstance();
            _themeManager.ThemeChanged += OnThemeChanged;
        }

        /// <summary>Renders the text with the theme's secondary (dimmed) text color.</summary>
        [System.ComponentModel.DefaultValue(false)]
        [System.ComponentModel.Category("Appearance")]
        public bool SecondaryText { get; set; }

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
            _themeManager = ThemeManager.getInstance();
            if (!_themeManager.ActiveAndExtended) return;
            // Transparent so labels blend with whatever surface hosts them (dialog, card group box, header...)
            BackColor = Color.Transparent;
            ForeColor = SecondaryText
                ? ModernControlColors.Current.TextSecondary
                : _themeManager.ActiveTheme.ExtendedPalette.getColor("Dialog_Foreground");
            BuildTextFormatFlags();
            Invalidate();
        }

        protected override void OnTextAlignChanged(EventArgs e)
        {
            BuildTextFormatFlags();
            base.OnTextAlignChanged(e);
        }

        protected override void OnAutoSizeChanged(EventArgs e)
        {
            BuildTextFormatFlags();
            base.OnAutoSizeChanged(e);
        }

        private void BuildTextFormatFlags()
        {
            _textFormatFlags = TextFormatFlags.TextBoxControl;
            if (!UseMnemonic)
                _textFormatFlags |= TextFormatFlags.NoPrefix;

            // in default labels, wordwrap is enabled when autosize is false
            if (AutoSize == false)
                _textFormatFlags |= TextFormatFlags.WordBreak;

            switch (TextAlign)
            {
                case ContentAlignment.TopLeft:
                    _textFormatFlags |= TextFormatFlags.Top | TextFormatFlags.Left;
                    break;
                case ContentAlignment.TopCenter:
                    _textFormatFlags |= TextFormatFlags.Top | TextFormatFlags.HorizontalCenter;
                    break;
                case ContentAlignment.TopRight:
                    _textFormatFlags |= TextFormatFlags.Top | TextFormatFlags.Right;
                    break;
                case ContentAlignment.MiddleLeft:
                    _textFormatFlags |= TextFormatFlags.VerticalCenter | TextFormatFlags.Left;
                    break;
                case ContentAlignment.MiddleCenter:
                    _textFormatFlags |= TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter;
                    break;
                case ContentAlignment.MiddleRight:
                    _textFormatFlags |= TextFormatFlags.VerticalCenter | TextFormatFlags.Right;
                    break;
                case ContentAlignment.BottomLeft:
                    _textFormatFlags |= TextFormatFlags.Bottom | TextFormatFlags.Left;
                    break;
                case ContentAlignment.BottomCenter:
                    _textFormatFlags |= TextFormatFlags.Bottom | TextFormatFlags.HorizontalCenter;
                    break;
                case ContentAlignment.BottomRight:
                    _textFormatFlags |= TextFormatFlags.Bottom | TextFormatFlags.Right;
                    break;
                default:
                    _textFormatFlags |= TextFormatFlags.Top | TextFormatFlags.Left;
                    break;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (!_themeManager.ActiveAndExtended)
            {
                base.OnPaint(e);
                return;
            }

            // let's use the defaults - this looks terrible in my testing....
            //e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            //e.Graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
            if (Enabled)
            {
                TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, ForeColor, _textFormatFlags);
            }
            else
            {
                Color disabledtextLabel = ModernControlColors.Current.TextDisabled;
                TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, disabledtextLabel, _textFormatFlags);
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // NGLabel
            // 
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular,
                                                System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ResumeLayout(false);
        }
    }
}