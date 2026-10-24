using System.Drawing;
using System.Runtime.Versioning;
using mRemoteNG.UI.GraphicsUtilities;

namespace mRemoteNG.UI.Controls.ConnectionTree
{
    /// <summary>
    /// Shared UI fonts of the side panels. They are created once and never disposed, so a control
    /// can never end up holding a disposed font (which makes handle creation fail).
    /// Point-sized, so they scale with the DPI of the control using them.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal static class SidebarFonts
    {
        public static Font Regular { get; } = ModernGraphics.CreateUiFont(9f);

        public static Font Small { get; } = ModernGraphics.CreateUiFont(8.25f);
    }
}
