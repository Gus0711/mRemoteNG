using System;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Runtime.Versioning;
using mRemoteNG.UI.GraphicsUtilities;

namespace mRemoteNG.UI.Controls
{
    /// <summary>
    /// Font helpers for headers and titles of the modern dialogs (semibold / display faces).
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal static class ModernFonts
    {
        private static readonly Lazy<string[]> InstalledFamilies = new(() =>
        {
            using InstalledFontCollection fonts = new();
            return fonts.Families.Select(f => f.Name).ToArray();
        });

        private static readonly Lazy<string?> SemiboldFamily = new(() =>
            FirstInstalled("Segoe UI Variable Text Semibold", "Segoe UI Semibold"));

        private static readonly Lazy<string?> DisplaySemiboldFamily = new(() =>
            FirstInstalled("Segoe UI Variable Display Semibold", "Segoe UI Semibold"));

        /// <summary>Semibold UI font (falls back to bold when no semibold face is installed).</summary>
        public static Font CreateSemibold(float size) =>
            SemiboldFamily.Value != null
                ? new Font(SemiboldFamily.Value, size, FontStyle.Regular, GraphicsUnit.Point)
                : ModernGraphics.CreateUiFont(size, FontStyle.Bold);

        /// <summary>Semibold display font for large titles.</summary>
        public static Font CreateDisplaySemibold(float size) =>
            DisplaySemiboldFamily.Value != null
                ? new Font(DisplaySemiboldFamily.Value, size, FontStyle.Regular, GraphicsUnit.Point)
                : CreateSemibold(size);

        private static readonly Lazy<FontFamily?> LogoFamily = new(LoadLogoFamily);

        // Keeps the private collection (and the font memory) alive for the lifetime of the process.
        private static PrivateFontCollection? _logoFonts;

        /// <summary>
        /// The mRemoteNG logo typeface (HandelGotDBol, embedded as a WPF resource), or null when unavailable.
        /// </summary>
        public static FontFamily? LogoFontFamily => LogoFamily.Value;

        private static FontFamily? LoadLogoFamily()
        {
            try
            {
                System.Reflection.Assembly assembly = typeof(ModernFonts).Assembly;
                string? name = assembly.GetManifestResourceNames()
                                       .FirstOrDefault(n => n.EndsWith(".g.resources", StringComparison.OrdinalIgnoreCase));
                if (name == null) return null;
                using System.IO.Stream? stream = assembly.GetManifestResourceStream(name);
                if (stream == null) return null;
                using System.Resources.ResourceReader reader = new(stream);
                foreach (System.Collections.DictionaryEntry entry in reader)
                {
                    if (entry.Key is not string key || !key.EndsWith("handelgotdbol.ttf", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (entry.Value is not System.IO.Stream fontStream) continue;
                    using System.IO.MemoryStream ms = new();
                    fontStream.CopyTo(ms);
                    byte[] data = ms.ToArray();
                    PrivateFontCollection collection = new();
                    IntPtr buffer = System.Runtime.InteropServices.Marshal.AllocCoTaskMem(data.Length);
                    System.Runtime.InteropServices.Marshal.Copy(data, 0, buffer, data.Length);
                    collection.AddMemoryFont(buffer, data.Length); // memory intentionally kept for the process lifetime
                    _logoFonts = collection;
                    return collection.Families.FirstOrDefault();
                }
            }
            catch (Exception)
            {
                // fall back to the UI font
            }

            return null;
        }

        private static string? FirstInstalled(params string[] names) =>
            names.FirstOrDefault(n => InstalledFamilies.Value.Contains(n, StringComparer.OrdinalIgnoreCase));
    }
}
