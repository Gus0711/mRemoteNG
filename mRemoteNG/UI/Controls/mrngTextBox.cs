using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.Themes;
using mRemoteNG.UI.GraphicsUtilities;

namespace mRemoteNG.UI.Controls
{
    [SupportedOSPlatform("windows")]
    // Fluent style text box. The native edit control still renders the text; the non-client frame is
    // replaced by a rounded border with inner horizontal padding and an accent underline when focused.
    public class MrngTextBox : TextBox
    {
        private bool _hover;

        public MrngTextBox()
        {
            InitializeComponent();
            ThemeManager.getInstance().ThemeChanged += OnThemeChanged;
        }

        private bool UseModernFrame => ModernControlColors.Enabled && BorderStyle != BorderStyle.None;

        private int HorizontalPadding =>
            Width < ModernControlColors.Scale(this, 48)
                ? ModernControlColors.Scale(this, 1)
                : ModernControlColors.Scale(this, 5);

        private void OnThemeChanged()
        {
            if (IsDisposed) return;
            SyncBorderStyle();
            ApplyColors();
            if (IsHandleCreated)
                ModernControlNative.RecalculateFrame(Handle);
            Invalidate();
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
            SyncBorderStyle();
            ApplyColors();
        }

        private bool _fixedSingleSwapped;

        // FixedSingle boxes get an extra border drawn by WinForms dark mode along the client edge;
        // the modern frame is painted for the (otherwise identical) Fixed3D style instead.
        private void SyncBorderStyle()
        {
            if (ModernControlColors.Enabled && BorderStyle == BorderStyle.FixedSingle)
            {
                BorderStyle = BorderStyle.Fixed3D;
                _fixedSingleSwapped = true;
            }
            else if (!ModernControlColors.Enabled && _fixedSingleSwapped)
            {
                BorderStyle = BorderStyle.FixedSingle;
                _fixedSingleSwapped = false;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (UseModernFrame)
            {
                ModernControlNative.RecalculateFrame(Handle);
                Invalidate();
            }
        }

        private void ApplyColors()
        {
            if (!ModernControlColors.Enabled) return;
            ModernControlColors c = ModernControlColors.Current;
            ForeColor = c.InputText;
            if (!Enabled || ReadOnly)
                BackColor = c.InputBackgroundDisabled;
            else
                BackColor = Focused && BorderStyle != BorderStyle.None ? c.InputBackgroundFocused : c.InputBackground;
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            ApplyColors();
            base.OnEnabledChanged(e);
            RedrawFrame();
            Invalidate();
        }

        protected override void OnReadOnlyChanged(EventArgs e)
        {
            ApplyColors();
            base.OnReadOnlyChanged(e);
            RedrawFrame();
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            ApplyColors();
            RedrawFrame();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            ApplyColors();
            RedrawFrame();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            RedrawFrame();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            RedrawFrame();
            base.OnMouseLeave(e);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            RedrawFrame();
        }

        private void RedrawFrame()
        {
            if (IsHandleCreated && UseModernFrame)
                ModernControlNative.RedrawFrame(Handle);
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case ModernControlNative.WM_NCCALCSIZE when UseModernFrame && !Multiline:
                    base.WndProc(ref m);
                    AddHorizontalPadding(ref m);
                    return;
                case ModernControlNative.WM_NCPAINT when UseModernFrame:
                    if (Multiline)
                        base.WndProc(ref m); // keeps the native scroll bars
                    PaintFrame();
                    m.Result = IntPtr.Zero;
                    return;
            }

            base.WndProc(ref m);
        }

        private void AddHorizontalPadding(ref Message m)
        {
            int padding = HorizontalPadding;
            if (m.WParam != IntPtr.Zero)
            {
                ModernControlNative.NCCALCSIZE_PARAMS p =
                    Marshal.PtrToStructure<ModernControlNative.NCCALCSIZE_PARAMS>(m.LParam);
                if (p.rgrc0.Right - p.rgrc0.Left <= padding * 4) return;
                p.rgrc0.Left += padding;
                p.rgrc0.Right -= padding;
                Marshal.StructureToPtr(p, m.LParam, false);
            }
            else
            {
                ModernControlNative.RECT r = Marshal.PtrToStructure<ModernControlNative.RECT>(m.LParam);
                if (r.Right - r.Left <= padding * 4) return;
                r.Left += padding;
                r.Right -= padding;
                Marshal.StructureToPtr(r, m.LParam, false);
            }
        }

        private void PaintFrame()
        {
            if (!ModernControlNative.GetWindowRect(Handle, out ModernControlNative.RECT window)) return;
            int width = window.Right - window.Left;
            int height = window.Bottom - window.Top;
            if (width <= 0 || height <= 0) return;

            Point clientOrigin = PointToScreen(Point.Empty);
            Rectangle client = new(clientOrigin.X - window.Left, clientOrigin.Y - window.Top, ClientSize.Width,
                                   ClientSize.Height);

            // For multi-line boxes the native frame contains scroll bars: only repaint the outer border ring.
            Rectangle inner = client;
            if (Multiline)
            {
                int ring = BorderStyle == BorderStyle.Fixed3D
                    ? SystemInformation.Border3DSize.Width
                    : SystemInformation.BorderSize.Width;
                inner = Rectangle.Inflate(new Rectangle(0, 0, width, height), -ring, -ring);
            }

            IntPtr hdc = ModernControlNative.GetWindowDC(Handle);
            if (hdc == IntPtr.Zero) return;
            try
            {
                using Graphics g = Graphics.FromHdc(hdc);
                g.ExcludeClip(inner);

                ModernControlColors c = ModernControlColors.Current;
                bool focused = Focused && Enabled && !ReadOnly;
                Color border = !Enabled
                    ? ModernGraphics.Blend(c.InputBorder, c.Background, 0.2f)
                    : focused
                        ? ModernGraphics.Blend(c.InputBorder, c.Accent, 0.35f)
                        : _hover && !ReadOnly ? c.BorderHover : c.InputBorder;

                Rectangle bounds = new(0, 0, width, height);
                float radius = Multiline
                    ? ModernControlColors.Scale(this, 3)
                    : Math.Min(ModernControlColors.Scale(this, 4), height / 2f);

                g.Clear(ModernControlColors.ParentBackground(this));
                ModernGraphics.FillRoundedRectangle(g, BackColor, bounds, radius);
                ModernGraphics.DrawRoundedRectangle(g, border, bounds, radius);

                if (focused)
                {
                    // Windows 11 style accent underline, clipped to the rounded shape.
                    int thickness = Math.Max(1, Math.Min(ModernControlColors.Scale(this, 2), height - client.Bottom));
                    using GraphicsPath shape = ModernGraphics.CreateRoundedRectangle(bounds, radius);
                    g.SetClip(shape, CombineMode.Intersect);
                    using SolidBrush accent = new(c.Accent);
                    g.FillRectangle(accent, 0, height - thickness, width, thickness);
                }
            }
            finally
            {
                ModernControlNative.ReleaseDC(Handle, hdc);
            }
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            //
            // NGTextBox
            //
            Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            ResumeLayout(false);
        }
    }
}
