using System.ComponentModel;
using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.Themes;
using WeifenLuo.WinFormsUI.Docking;

namespace mRemoteNG.UI.Tabs
{
    /// <summary>
    /// Splitter between two panes of the same dock window: a comfortable hit area painted with the
    /// surrounding surface and a single hairline in the middle.
    /// </summary>
    [SupportedOSPlatform("windows")]
    [ToolboxItem(false)]
    internal sealed class ModernDockPaneSplitter : DockPane.SplitterControlBase
    {
        public ModernDockPaneSplitter(DockPane pane) : base(pane)
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Rectangle client = ClientRectangle;
            if (client.Width <= 0 || client.Height <= 0)
                return;

            ModernPalette palette = DockingStyle.Palette;
            bool document = DockPane.DockState == DockState.Document;
            e.Graphics.Clear(document ? palette.Content : palette.Panel);

            using Pen pen = new(document ? palette.Border : palette.BorderSubtle);
            if (Alignment is DockAlignment.Left or DockAlignment.Right)
            {
                int x = client.Left + client.Width / 2;
                e.Graphics.DrawLine(pen, x, client.Top, x, client.Bottom);
            }
            else
            {
                int y = client.Top + client.Height / 2;
                e.Graphics.DrawLine(pen, client.Left, y, client.Right, y);
            }
        }
    }

    /// <summary>
    /// Splitter of a dock window / auto-hide window: Panel surface with a hairline on the edge facing
    /// the document area, so tool windows read as a side bar layered next to the content.
    /// </summary>
    [SupportedOSPlatform("windows")]
    [ToolboxItem(false)]
    internal sealed class ModernWindowSplitter : SplitterBase
    {
        private readonly ISplitterHost _host;

        public ModernWindowSplitter(ISplitterHost host)
        {
            _host = host;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
        }

        protected override int SplitterSize =>
            _host.IsDockWindow
                ? _host.DockPanel.Theme.Measures.SplitterSize
                : _host.DockPanel.Theme.Measures.AutoHideSplitterSize;

        protected override void StartDrag()
        {
            _host.DockPanel.BeginDrag((ISplitterDragSource)_host, ((IDragSource)_host).DragControl.RectangleToScreen(Bounds));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Rectangle client = ClientRectangle;
            if (client.Width <= 0 || client.Height <= 0)
                return;

            ModernPalette palette = DockingStyle.Palette;
            Graphics g = e.Graphics;
            g.Clear(palette.Panel);

            using Pen pen = new(palette.Border);
            switch (Dock)
            {
                case DockStyle.Right:
                    g.DrawLine(pen, client.Right - 1, client.Top, client.Right - 1, client.Bottom);
                    break;
                case DockStyle.Left:
                    g.DrawLine(pen, client.Left, client.Top, client.Left, client.Bottom);
                    break;
                case DockStyle.Bottom:
                    g.DrawLine(pen, client.Left, client.Bottom - 1, client.Right, client.Bottom - 1);
                    break;
                case DockStyle.Top:
                    g.DrawLine(pen, client.Left, client.Top, client.Right, client.Top);
                    break;
            }
        }
    }

    [SupportedOSPlatform("windows")]
    public class ModernDockPaneSplitterControlFactory : DockPanelExtender.IDockPaneSplitterControlFactory
    {
        public DockPane.SplitterControlBase CreateSplitterControl(DockPane pane) => new ModernDockPaneSplitter(pane);
    }

    [SupportedOSPlatform("windows")]
    public class ModernWindowSplitterControlFactory : DockPanelExtender.IWindowSplitterControlFactory
    {
        public SplitterBase CreateSplitterControl(ISplitterHost host) => new ModernWindowSplitter(host);
    }
}
