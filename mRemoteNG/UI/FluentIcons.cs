using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.Themes;

namespace mRemoteNG.UI
{
    /// <summary>
    /// Renders monochrome icons from the Windows icon font ("Segoe Fluent Icons" on Windows 11,
    /// "Segoe MDL2 Assets" on Windows 10). Icons are crisp at any DPI and follow the theme colors.
    /// Bitmaps are cached per glyph/size/color; do not dispose the returned images.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class FluentIcons
    {
        // Glyph code points shared by Segoe Fluent Icons and Segoe MDL2 Assets.
        public const char Add = '';
        public const char Back = '';
        public const char Cancel = '';
        public const char ChevronDown = '';
        public const char ChevronRight = '';
        public const char Clear = '';
        public const char Cloud = '';
        public const char Code = '';
        public const char CollapseAll = '';
        public const char CommandPrompt = '';
        public const char Connect = '';
        public const char Copy = '';
        public const char Delete = '';
        public const char Desktop = '';
        public const char Disconnect = '';
        public const char Document = '';
        public const char Download = '';
        public const char Edit = '';
        public const char Error = '';
        public const char ExpandAll = '';
        public const char Export = '';
        public const char Favorite = '';
        public const char FavoriteFilled = '';
        public const char Filter = '';
        public const char Folder = '';
        public const char FolderOpen = '';
        public const char FullScreen = '';
        public const char Globe = '';
        public const char Help = '';
        public const char History = '';
        public const char Home = '';
        public const char Import = '';
        public const char Info = '';
        public const char Keyboard = '';
        public const char Lightning = '';
        public const char Link = '';
        public const char Lock = '';
        public const char Mail = '';
        public const char More = '';
        public const char Network = '';
        public const char NewWindow = '';
        public const char OpenFile = '';
        public const char Permissions = '';
        public const char Pin = '';
        public const char Play = '';
        public const char Plug = '';
        public const char Power = '';
        public const char Print = '';
        public const char Refresh = '';
        public const char Remote = '';
        public const char Rename = '';
        public const char Save = '';
        public const char SaveAs = '';
        public const char Screenshot = '';
        public const char Search = '';
        public const char Server = '';
        public const char Settings = '';
        public const char Shield = '';
        public const char Sort = '';
        public const char Switch = '';
        public const char Sync = '';
        public const char Tag = '';
        public const char Terminal = '';
        public const char Tools = '';
        public const char Unpin = '';
        public const char Upload = '';
        public const char View = '';
        public const char Warning = '';
        public const char WiFi = '';
        public const char Window = '';
        public const char ZoomIn = '';

        private static readonly Lazy<string> IconFontFamilyName = new(ResolveIconFontFamily);
        private static readonly ConcurrentDictionary<(char, int, int), Bitmap> Cache = new();

        /// <summary>"Segoe Fluent Icons" when installed, otherwise "Segoe MDL2 Assets".</summary>
        public static string FontFamily => IconFontFamilyName.Value;

        /// <summary>
        /// Returns a cached bitmap of the glyph, <paramref name="pixelSize"/> pixels square, in the given color
        /// (defaults to the theme's primary text color).
        /// </summary>
        public static Bitmap Get(char glyph, int pixelSize = 16, Color? color = null)
        {
            Color c = color ?? ModernPalette.Current.Text;
            return Cache.GetOrAdd((glyph, pixelSize, c.ToArgb()), key => Render(key.Item1, key.Item2, Color.FromArgb(key.Item3)));
        }

        /// <summary>Same as <see cref="Get"/> but sized for the DPI of <paramref name="control"/> (logical size × scale).</summary>
        public static Bitmap GetScaled(Control control, char glyph, int logicalSize = 16, Color? color = null)
        {
            float scale = control.DeviceDpi / 96f;
            return Get(glyph, (int)Math.Round(logicalSize * scale), color);
        }

        /// <summary>Draws the glyph directly, centered in <paramref name="bounds"/>.</summary>
        public static void Draw(Graphics g, char glyph, Rectangle bounds, Color color, float? emSize = null)
        {
            float size = emSize ?? Math.Min(bounds.Width, bounds.Height) * 0.75f;
            using Font font = new(FontFamily, size, FontStyle.Regular, GraphicsUnit.Pixel);
            TextRenderingHint previous = g.TextRenderingHint;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            using StringFormat format = new(StringFormat.GenericTypographic)
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            using SolidBrush brush = new(color);
            g.DrawString(glyph.ToString(), font, brush, bounds, format);
            g.TextRenderingHint = previous;
        }

        private static Bitmap Render(char glyph, int pixelSize, Color color)
        {
            Bitmap bitmap = new(pixelSize, pixelSize, PixelFormat.Format32bppPArgb);
            using Graphics g = Graphics.FromImage(bitmap);
            g.Clear(Color.Transparent);
            Draw(g, glyph, new Rectangle(0, 0, pixelSize, pixelSize), color, pixelSize * 0.8f);
            return bitmap;
        }

        private static string ResolveIconFontFamily()
        {
            using InstalledFontCollection fonts = new();
            foreach (System.Drawing.FontFamily family in fonts.Families)
            {
                if (family.Name.Equals("Segoe Fluent Icons", StringComparison.OrdinalIgnoreCase))
                    return family.Name;
            }

            return "Segoe MDL2 Assets";
        }
    }
}
