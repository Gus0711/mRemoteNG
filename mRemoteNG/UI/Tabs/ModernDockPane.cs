using System.ComponentModel;
using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.Themes;
using WeifenLuo.WinFormsUI.Docking;

namespace mRemoteNG.UI.Tabs
{
    /// <summary>
    /// Borderless dock pane: no 1px frame around the content, the pane background is the
    /// Panel surface for tool windows and the Content surface for documents.
    /// </summary>
    [SupportedOSPlatform("windows")]
    [ToolboxItem(false)]
    public class ModernDockPane : DockPane
    {
        public ModernDockPane(IDockContent content, DockState visibleState, bool show)
            : base(content, visibleState, show)
        {
            Initialize();
        }

        public ModernDockPane(IDockContent content, FloatWindow floatWindow, bool show)
            : base(content, floatWindow, show)
        {
            Initialize();
        }

        public ModernDockPane(IDockContent content, DockPane previousPane, DockAlignment alignment, double proportion, bool show)
            : base(content, previousPane, alignment, proportion, show)
        {
            Initialize();
        }

        public ModernDockPane(IDockContent content, Rectangle floatWindowBounds, bool show)
            : base(content, floatWindowBounds, show)
        {
            Initialize();
        }

        private void Initialize()
        {
            ThemeManager.getInstance().ThemeChanged += OnThemeChanged;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                ThemeManager.getInstance().ThemeChanged -= OnThemeChanged;
            base.Dispose(disposing);
        }

        private void OnThemeChanged()
        {
            if (IsDisposed) return;
            Invalidate(true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            ModernPalette palette = DockingStyle.Palette;
            Color background = DockState == DockState.Document ? palette.Content : palette.Panel;
            using SolidBrush brush = new(background);
            e.Graphics.FillRectangle(brush, e.ClipRectangle);
        }
    }

    [SupportedOSPlatform("windows")]
    public class ModernDockPaneFactory : DockPanelExtender.IDockPaneFactory
    {
        public DockPane CreateDockPane(IDockContent content, DockState visibleState, bool show) =>
            new ModernDockPane(content, visibleState, show);

        public DockPane CreateDockPane(IDockContent content, FloatWindow floatWindow, bool show) =>
            new ModernDockPane(content, floatWindow, show);

        public DockPane CreateDockPane(IDockContent content, DockPane previousPane, DockAlignment alignment, double proportion, bool show) =>
            new ModernDockPane(content, previousPane, alignment, proportion, show);

        public DockPane CreateDockPane(IDockContent content, Rectangle floatWindowBounds, bool show) =>
            new ModernDockPane(content, floatWindowBounds, show);
    }
}
