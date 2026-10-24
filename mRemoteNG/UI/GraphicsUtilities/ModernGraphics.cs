using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.Versioning;

namespace mRemoteNG.UI.GraphicsUtilities
{
    /// <summary>
    /// Drawing helpers shared by the custom-painted modern controls.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class ModernGraphics
    {
        private static readonly Lazy<string> UiFontFamilyName = new(ResolveUiFontFamily);

        /// <summary>
        /// "Segoe UI Variable Text" on Windows 11, "Segoe UI" otherwise.
        /// </summary>
        public static string UiFontFamily => UiFontFamilyName.Value;

        public static Font CreateUiFont(float size = 9f, FontStyle style = FontStyle.Regular) =>
            new(UiFontFamily, size, style, GraphicsUnit.Point);

        public static GraphicsPath CreateRoundedRectangle(RectangleF bounds, float radius)
        {
            GraphicsPath path = new();
            if (radius <= 0 || bounds.Width <= 0 || bounds.Height <= 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            float diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
            RectangleF arc = new(bounds.Location, new SizeF(diameter, diameter));
            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void FillRoundedRectangle(Graphics g, Color color, RectangleF bounds, float radius)
        {
            SmoothingMode previous = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using GraphicsPath path = CreateRoundedRectangle(bounds, radius);
            using SolidBrush brush = new(color);
            g.FillPath(brush, path);
            g.SmoothingMode = previous;
        }

        public static void DrawRoundedRectangle(Graphics g, Color color, RectangleF bounds, float radius, float width = 1f)
        {
            SmoothingMode previous = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            // Inset by half the pen width so the stroke stays inside the bounds.
            RectangleF inset = RectangleF.Inflate(bounds, -width / 2f, -width / 2f);
            using GraphicsPath path = CreateRoundedRectangle(inset, radius);
            using Pen pen = new(color, width);
            g.DrawPath(pen, path);
            g.SmoothingMode = previous;
        }

        /// <summary>Linear blend: amount 0 returns <paramref name="from"/>, 1 returns <paramref name="to"/>.</summary>
        public static Color Blend(Color from, Color to, float amount)
        {
            amount = Math.Clamp(amount, 0f, 1f);
            return Color.FromArgb(
                (int)(from.A + (to.A - from.A) * amount),
                (int)(from.R + (to.R - from.R) * amount),
                (int)(from.G + (to.G - from.G) * amount),
                (int)(from.B + (to.B - from.B) * amount));
        }

        /// <summary>Scales a logical pixel value by the DPI of the given graphics surface.</summary>
        public static int Scale(Graphics g, int logicalPixels) => (int)Math.Round(logicalPixels * g.DpiX / 96f);

        private static string ResolveUiFontFamily()
        {
            using InstalledFontCollection fonts = new();
            foreach (FontFamily family in fonts.Families)
            {
                if (family.Name.Equals("Segoe UI Variable Text", StringComparison.OrdinalIgnoreCase))
                    return family.Name;
            }

            return "Segoe UI";
        }
    }
}
