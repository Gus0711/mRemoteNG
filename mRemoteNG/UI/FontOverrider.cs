using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace mRemoteNG.UI
{
    [SupportedOSPlatform("windows")]
    public class FontOverrider
    {
        public static void FontOverride(Control ctlParent)
        {
            // Override the font of all controls in a container with the modern UI font (Segoe UI Variable / Segoe UI)
            foreach (Control tempLoopVarCtlChild in ctlParent.Controls)
            {
                Control ctlChild = tempLoopVarCtlChild;
                // Only create a new Font if the font name is different to avoid unnecessary GDI operations
                if (ShouldOverride(ctlChild.Font.Name))
                {
                    ctlChild.Font = new Font(GraphicsUtilities.ModernGraphics.UiFontFamily, ctlChild.Font.Size, ctlChild.Font.Style,
                                             ctlChild.Font.Unit, ctlChild.Font.GdiCharSet);
                }
                if (ctlChild.Controls.Count > 0)
                {
                    FontOverride(ctlChild);
                }
            }
        }

        // Keep the UI font itself, deliberate Segoe UI variants (Semibold / Display titles) and icon fonts.
        private static bool ShouldOverride(string fontName) =>
            fontName != GraphicsUtilities.ModernGraphics.UiFontFamily &&
            !fontName.StartsWith("Segoe UI ", System.StringComparison.OrdinalIgnoreCase) &&
            fontName != FluentIcons.FontFamily;
    }
}