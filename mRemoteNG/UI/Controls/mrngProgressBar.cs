using System;
using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.Themes;
using mRemoteNG.UI.GraphicsUtilities;

namespace mRemoteNG.UI.Controls
{
    [SupportedOSPlatform("windows")]
    // Slim Fluent progress bar: thin rounded track with an accent fill, vertically centered in the control.
    // Marquee style shows an animated accent segment.
    internal class MrngProgressBar : ProgressBar
    {
        private const int TrackLogicalHeight = 4;
        private readonly Timer _marqueeTimer = new() { Interval = 16 };
        private float _marqueePosition;

        public MrngProgressBar()
        {
            ThemeManager.getInstance().ThemeChanged += OnThemeChanged;
            _marqueeTimer.Tick += (_, _) =>
            {
                _marqueePosition += 0.012f;
                if (_marqueePosition > 1.4f) _marqueePosition = -0.4f;
                Invalidate();
            };
        }

        private void OnThemeChanged()
        {
            if (IsDisposed) return;
            ApplyTheme();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ThemeManager.getInstance().ThemeChanged -= OnThemeChanged;
                _marqueeTimer.Dispose();
            }

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
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, themed);
            UpdateMarqueeTimer();
            Invalidate();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            UpdateMarqueeTimer();
        }

        private void UpdateMarqueeTimer()
        {
            bool run = ModernControlColors.Enabled && Style == ProgressBarStyle.Marquee && Visible && !IsDisposed;
            if (run && !_marqueeTimer.Enabled) _marqueeTimer.Start();
            else if (!run && _marqueeTimer.Enabled) _marqueeTimer.Stop();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (!ModernControlColors.Enabled)
            {
                base.OnPaint(e);
                return;
            }

            UpdateMarqueeTimer();
            ModernControlColors c = ModernControlColors.Current;
            Graphics g = e.Graphics;
            g.Clear(ModernControlColors.ParentBackground(this));

            float height = Math.Min(Height, ModernControlColors.Scale(this, TrackLogicalHeight));
            float y = (Height - height) / 2f;
            float radius = height / 2f;
            RectangleF track = new(0, y, Width, height);

            // thinner, subtle track behind the fill (Fluent style)
            float trackHeight = Math.Max(1f, height / 3f);
            RectangleF thinTrack = new(0, (Height - trackHeight) / 2f, Width, trackHeight);
            ModernGraphics.FillRoundedRectangle(g, c.ControlStroke.A > 0 ? ModernGraphics.Blend(c.Background, c.ControlStroke, 0.45f) : c.Border,
                                                thinTrack, trackHeight / 2f);

            Color fill = Enabled ? c.Accent : c.TextDisabled;
            if (Style == ProgressBarStyle.Marquee)
            {
                float segment = Width * 0.4f;
                float x = _marqueePosition * Width - segment / 2f;
                RectangleF bar = RectangleF.Intersect(new RectangleF(x, y, segment, height), track);
                if (bar.Width > 0)
                    ModernGraphics.FillRoundedRectangle(g, fill, bar, radius);
                return;
            }

            int range = Maximum - Minimum;
            if (range <= 0) return;
            float done = Width * (float)(Value - Minimum) / range;
            if (done <= 0) return;
            ModernGraphics.FillRoundedRectangle(g, fill, new RectangleF(0, y, Math.Max(done, height), height), radius);
        }
    }
}
