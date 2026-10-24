using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.Themes;

namespace mRemoteNG.UI.Controls.ConnectionTree
{
    /// <summary>
    /// Gives native list views and tree views dark (or light) Explorer scroll bars that match the
    /// active theme, instead of the classic light ones.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal static class NativeScrollBarTheme
    {
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string? pszSubAppName, string? pszSubIdList);

        public static void Apply(Control control)
        {
            if (!control.IsHandleCreated)
                return;

            try
            {
                bool dark = ThemeManager.getInstance().ThemingActive && ModernPalette.Current.IsDark;
                SetWindowTheme(control.Handle, dark ? "DarkMode_Explorer" : "Explorer", null);
            }
            catch (DllNotFoundException)
            {
                // themes unavailable: keep the default scroll bars
            }
            catch (EntryPointNotFoundException)
            {
            }
        }
    }
}
