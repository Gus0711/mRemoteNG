using System;
using System.Drawing;
using System.Runtime.Versioning;
using mRemoteNG.UI.Tabs;
using WeifenLuo.WinFormsUI.Docking;
using WeifenLuo.WinFormsUI.ThemeVS2015;

namespace mRemoteNG.Themes
{
    [SupportedOSPlatform("windows")]

    /// <summary>
    /// Visual Studio 2015 Light theme.
    /// </summary>
    public class MremoteNGThemeBase : VS2015ThemeBase
    {
        public MremoteNGThemeBase(byte[] themeResource)
            : base(themeResource)
        {
            ApplyModernMeasures(this);
            ShowAutoHideContentOnHover = false;
        }

        /// <summary>
        /// Splitters are painted as a single hairline (see ModernSplitters) but keep a comfortable
        /// hit area; no padding around the dock area so panels sit flush with the window edges.
        /// </summary>
        internal static void ApplyModernMeasures(ThemeBase theme)
        {
            int scale = Math.Max(1, (int)Math.Round(DpiScale()));
            theme.Measures.SplitterSize = 5 * scale;
            theme.Measures.AutoHideSplitterSize = 4 * scale;
            theme.Measures.DockPadding = 0;
            theme.Measures.AutoHideTabLineWidth = 2 * scale;
        }

        private static float DpiScale()
        {
            try
            {
                using Graphics g = Graphics.FromHwnd(IntPtr.Zero);
                return g.DpiX / 96f;
            }
            catch
            {
                return 1f;
            }
        }
    }

    [SupportedOSPlatform("windows")]
    public class MremoteDockPaneStripFactory : DockPanelExtender.IDockPaneStripFactory
    {
        public DockPaneStripBase CreateDockPaneStrip(DockPane pane) => new DockPaneStripNG(pane);
    }

    public class MremoteFloatWindowFactory : DockPanelExtender.IFloatWindowFactory
    {
        public FloatWindow CreateFloatWindow(DockPanel dockPanel, DockPane pane, Rectangle bounds)
        {
            Rectangle? activeDocumentBounds = (dockPanel?.ActiveDocument as ConnectionTab)?.Bounds;

            // dockPanel is non-null per the IFloatWindowFactory.CreateFloatWindow contract
            return new FloatWindowNG(dockPanel!, pane, activeDocumentBounds ?? bounds);
        }

        public FloatWindow CreateFloatWindow(DockPanel dockPanel, DockPane pane)
        {
            return new FloatWindowNG(dockPanel, pane);
        }
    }
}