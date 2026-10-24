using mRemoteNG.Themes;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace mRemoteNG.UI.TaskDialog
{
    [SupportedOSPlatform("windows")]
    public sealed partial class CommandButton : Button
    {
        //--------------------------------------------------------------------------------

        #region PRIVATE MEMBERS

        //--------------------------------------------------------------------------------
        private Image imgArrow1;
        private Image imgArrow2;
        private readonly ThemeManager _themeManager;
        private const int LEFT_MARGIN = 10;
        private const int TOP_MARGIN = 10;
        private const int ARROW_WIDTH = 19;

        enum eButtonState
        {
            Normal,
            MouseOver,
            Down
        }

        eButtonState m_State = eButtonState.Normal;

        #endregion

        //--------------------------------------------------------------------------------

        #region PUBLIC PROPERTIES

        //--------------------------------------------------------------------------------
        // Override this to make sure the control is invalidated (repainted) when 'Text' is changed
        public override string? Text
        {
            get => base.Text;
            set
            {
                base.Text = value;
                if (m_autoHeight)
                    Height = GetBestHeight();
                Invalidate();
            }
        }

        // SmallFont is the font used for secondary lines
        private Font SmallFont { get; set; }

        // AutoHeight determines whether the button automatically resizes itself to fit the Text
        bool m_autoHeight = true;

        [Browsable(true)]
        [Category("Behavior")]
        [DefaultValue(true)]
        public bool AutoHeight
        {
            get => m_autoHeight;
            set
            {
                m_autoHeight = value;
                if (m_autoHeight) Invalidate();
            }
        }

        #endregion

        //--------------------------------------------------------------------------------

        #region CONSTRUCTOR

        //--------------------------------------------------------------------------------
        public CommandButton()
        {
            InitializeComponent();
            Font = new Font("Segoe UI", 11.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            SmallFont = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            _themeManager = ThemeManager.getInstance();
            imgArrow1 = Properties.Resources.GlyphRight_16x; // Initialize imgArrow1
            imgArrow2 = Properties.Resources.GlyphRight_16x; // Initialize imgArrow2
        }

        #endregion

        //--------------------------------------------------------------------------------

        #region PUBLIC ROUTINES

        //--------------------------------------------------------------------------------
        public int GetBestHeight()
        {
            return (TOP_MARGIN * 2) + (int)GetSmallTextSizeF().Height + (int)GetLargeTextSizeF().Height;
        }

        #endregion

        //--------------------------------------------------------------------------------

        #region PRIVATE ROUTINES

        //--------------------------------------------------------------------------------
        string GetLargeText()
        {
            if (string.IsNullOrEmpty(Text))
                return string.Empty;
            
            string[] lines = Text.Split('\n');
            return lines[0];
        }

        string GetSmallText()
        {
            if (string.IsNullOrEmpty(Text) || Text.IndexOf('\n') < 0)
                return "";

            string s = Text;
            string[] lines = s.Split('\n');
            s = "";
            for (int i = 1; i < lines.Length; i++)
                s += lines[i] + "\n";
            return s.Trim('\n');
        }

        SizeF GetLargeTextSizeF()
        {
            int x = LEFT_MARGIN + ARROW_WIDTH + 5;
            SizeF mzSize = new(Width - x - LEFT_MARGIN, 5000.0F); // presume RIGHT_MARGIN = LEFT_MARGIN
            Graphics g = Graphics.FromHwnd(Handle);
            SizeF textSize = g.MeasureString(GetLargeText(), Font, mzSize);
            return textSize;
        }

        SizeF GetSmallTextSizeF()
        {
            string s = GetSmallText();
            if (s == "") return new SizeF(0, 0);
            int x = LEFT_MARGIN + ARROW_WIDTH + 8; // <- indent small text slightly more
            SizeF mzSize = new(Width - x - LEFT_MARGIN, 5000.0F); // presume RIGHT_MARGIN = LEFT_MARGIN
            Graphics g = Graphics.FromHwnd(Handle);
            SizeF textSize = g.MeasureString(s, SmallFont, mzSize);
            return textSize;
        }

        #endregion

        //--------------------------------------------------------------------------------

        #region OVERRIDES

        //--------------------------------------------------------------------------------
        protected override void OnCreateControl()
        {
            // WinForms dark mode paints Standard buttons natively (OnPaint is skipped): use the Flat style.
            if (_themeManager.ActiveAndExtended)
            {
                FlatStyle = FlatStyle.Flat;
                SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            }

            base.OnCreateControl();
            imgArrow1 = Properties.Resources.GlyphRight_16x;
            imgArrow2 = Properties.Resources.GlyphRight_16x;
        }

        //--------------------------------------------------------------------------------
        protected override void OnPaint(PaintEventArgs e)
        {
            if (!_themeManager.ActiveAndExtended)
            {
                base.OnPaint(e);
                return;
            }

            // Fluent "command link": rounded surface, accent arrow, title + secondary description.
            mRemoteNG.UI.Controls.ModernControlColors c = mRemoteNG.UI.Controls.ModernControlColors.Current;
            Graphics g = e.Graphics;
            g.Clear(mRemoteNG.UI.Controls.ModernControlColors.ParentBackground(this));

            Color back;
            Color border;
            if (!Enabled)
            {
                back = c.SurfaceDisabled;
                border = mRemoteNG.UI.GraphicsUtilities.ModernGraphics.Blend(c.Border, c.Background, 0.4f);
            }
            else
            {
                back = m_State switch
                {
                    eButtonState.MouseOver => c.SurfaceHover,
                    eButtonState.Down => c.SurfacePressed,
                    _ => c.Surface
                };
                border = m_State == eButtonState.MouseOver ? c.BorderHover : c.Border;
            }

            float radius = mRemoteNG.UI.Controls.ModernControlColors.Scale(this, mRemoteNG.Themes.ModernPalette.CornerRadius);
            RectangleF bounds = new(0, 0, Width, Height);
            mRemoteNG.UI.GraphicsUtilities.ModernGraphics.FillRoundedRectangle(g, back, bounds, radius);
            mRemoteNG.UI.GraphicsUtilities.ModernGraphics.DrawRoundedRectangle(g, border, bounds, radius);

            Color fore = Enabled ? c.Text : c.TextDisabled;
            Color secondary = Enabled ? c.TextSecondary : c.TextDisabled;
            string largetext = GetLargeText();
            string smalltext = GetSmallText();
            SizeF szL = GetLargeTextSizeF();

            int arrow = mRemoteNG.UI.Controls.ModernControlColors.Scale(this, 16);
            Rectangle arrowRect = new(LEFT_MARGIN, TOP_MARGIN + (int)(szL.Height / 2) - arrow / 2, arrow, arrow);
            FluentIcons.Draw(g, '', arrowRect, Enabled ? c.Accent : c.TextDisabled, arrow * 0.85f);

            TextRenderer.DrawText(g, largetext, Font,
                                  new Rectangle(LEFT_MARGIN + imgArrow1.Width + 5, TOP_MARGIN, (int)szL.Width + 2,
                                                (int)szL.Height), fore, TextFormatFlags.Default);

            if (smalltext != "")
            {
                SizeF szS = GetSmallTextSizeF();
                using SolidBrush smallBrush = new(secondary);
                g.DrawString(smalltext, SmallFont, smallBrush,
                             new RectangleF(new PointF(LEFT_MARGIN + imgArrow1.Width + 8, TOP_MARGIN + (int)szL.Height), szS));
            }

            if (Focused && Enabled && ShowFocusCues)
            {
                float inset = mRemoteNG.UI.Controls.ModernControlColors.Scale(this, 2);
                mRemoteNG.UI.GraphicsUtilities.ModernGraphics.DrawRoundedRectangle(g, c.Accent, RectangleF.Inflate(bounds, -inset, -inset),
                                                                      Math.Max(1, radius - inset));
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            m_State = eButtonState.Normal;
            Invalidate();
            base.OnMouseLeave(e);
        }

        //--------------------------------------------------------------------------------
        protected override void OnMouseEnter(EventArgs e)
        {
            m_State = eButtonState.MouseOver;
            Invalidate();
            base.OnMouseEnter(e);
        }

        //--------------------------------------------------------------------------------
        protected override void OnMouseUp(MouseEventArgs e)
        {
            m_State = eButtonState.MouseOver;
            Invalidate();
            base.OnMouseUp(e);
        }

        //--------------------------------------------------------------------------------
        protected override void OnMouseDown(MouseEventArgs e)
        {
            m_State = eButtonState.Down;
            Invalidate();
            base.OnMouseDown(e);
        }

        //--------------------------------------------------------------------------------
        protected override void OnSizeChanged(EventArgs e)
        {
            if (m_autoHeight)
            {
                int h = GetBestHeight();
                if (Height != h)
                {
                    Height = h;
                    return;
                }
            }

            base.OnSizeChanged(e);
        }

        #endregion

        //--------------------------------------------------------------------------------
    }
}