using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using mRemoteNG.Connection;
using mRemoteNG.Themes;
using mRemoteNG.UI.GraphicsUtilities;
using mRemoteNG.UI.Window;
using WeifenLuo.WinFormsUI.Docking;
using mRemoteNG.Resources.Language;
using System.Runtime.Versioning;

namespace mRemoteNG.UI.Tabs
{
    [SupportedOSPlatform("windows")]
    /// <summary>
    /// Tab strip of the dock panes, originally lifted from VS2013DockPaneStrip (DockPanelSuite) and
    /// redesigned for the modern UI:
    /// <list type="bullet">
    /// <item>Connection tabs (documents of a connection panel) are browser-like: rounded-top tabs
    /// on the Chrome surface, the active one merging into the Content area with an accent
    /// indicator, close/minimize glyphs shown on the active or hovered tab.</item>
    /// <item>Panel tabs (top level documents) are flat header tabs with an accent underline.</item>
    /// <item>Tool window tabs (grouped tool windows) are text tabs with an accent underline.</item>
    /// </list>
    /// </summary>
    [ToolboxItem(false)]
    internal class DockPaneStripNG : DockPaneStripBase
    {
        private class MremoteNGTab(IDockContent content) : Tab(content)
        {
            public int TabX { get; set; }

            public int TabWidth { get; set; }

            public int MaxWidth { get; set; }

            protected internal bool Flag { get; set; }
        }

        protected override Tab CreateTab(IDockContent content)
        {
            return new MremoteNGTab(content);
        }

        #region Metrics (logical pixels)

        private const int ToolWindowStripMargin = 6;
        private const int ToolWindowTabPadding = 12;

        private const int DocumentStripMarginLeft = 6;
        private const int DocumentTabGap = 2;
        private const int DocumentTabMaxWidth = 240;
        private const int DocumentTabPaddingLeft = 10;
        private const int DocumentIconSize = 16;
        private const int DocumentIconGapRight = 8;
        private const int DocumentTextGapRight = 8;
        private const int DocumentTabPaddingRight = 12;
        private const int TabButtonSize = 20;
        private const int TabButtonGap = 2;
        private const int TabButtonPaddingRight = 6;
        private const int StripButtonSize = 28;
        private const int StripButtonGapRight = 6;
        private const int StripButtonGapLeft = 4;

        #endregion

        #region Members

        private DockGlyphButton m_buttonOverflow;
        private DockGlyphButton m_buttonWindowList;
        private readonly ToolTip m_toolTip;
        private int m_startDisplayingTab;
        private bool m_documentTabsOverflow;
        private static string m_toolTipSelect;
        private bool m_suspendDrag;
        private static GraphicsPath _graphicsPath;

        #endregion

        #region Properties

        private int Px(float logicalPixels) => DockingStyle.Scale(this, logicalPixels);

        private static ModernPalette Palette => DockingStyle.Palette;

        private Font TabFont => DockingStyle.GetFont(this);

        /// <summary>
        /// Top level documents (connection panels) get flat header tabs; the connection tabs inside a
        /// panel get browser-like tabs.
        /// </summary>
        private bool IsPanelStrip =>
            Appearance == DockPane.AppearanceStyle.Document && DockPane.DockPanel.Parent is not ConnectionWindow;

        private bool IsTabStripAtBottom =>
            DockPane.DockPanel.DocumentTabStripLocation == DocumentTabStripLocation.Bottom;

        private Rectangle TabStripRectangle => ClientRectangle;

        private Rectangle TabsRectangle
        {
            get
            {
                Rectangle rect = TabStripRectangle;
                if (Appearance == DockPane.AppearanceStyle.ToolWindow)
                    return rect;

                int left = Px(DocumentStripMarginLeft);
                int right = Px(StripButtonGapLeft) + Px(StripButtonSize) + Px(StripButtonGapRight);
                return new Rectangle(rect.X + left, rect.Y, Math.Max(0, rect.Width - left - right), rect.Height);
            }
        }

        /// <summary>Top of the tabs inside the strip (browser tabs float below a small gap).</summary>
        private int DocumentTabTop => IsPanelStrip || IsTabStripAtBottom ? 0 : Px(DockingStyle.DocumentTabTop);

        private ContextMenuStrip SelectMenu { get; }

        public int SelectMenuMargin { get; set; } = 5;

        private DockGlyphButton ButtonOverflow
        {
            get
            {
                if (m_buttonOverflow != null) return m_buttonOverflow;
                m_buttonOverflow = CreateStripButton();
                return m_buttonOverflow;
            }
        }

        private DockGlyphButton ButtonWindowList
        {
            get
            {
                if (m_buttonWindowList != null) return m_buttonWindowList;
                m_buttonWindowList = CreateStripButton();
                return m_buttonWindowList;
            }
        }

        private DockGlyphButton CreateStripButton()
        {
            DockGlyphButton button = new(FluentIcons.ChevronDown, 11f)
            {
                SurfaceColor = () => Palette.Chrome
            };
            button.Click += WindowList_Click;
            m_toolTip.SetToolTip(button, ToolTipSelect.Replace("&&", "&"));
            Controls.Add(button);
            return button;
        }

        private static GraphicsPath GraphicsPath => _graphicsPath ??= new GraphicsPath();

        private IContainer Components { get; }

        public Font TextFont => DockPane.DockPanel.Theme.Skin.DockPaneStripSkin.TextFont;

        private int StartDisplayingTab
        {
            get => m_startDisplayingTab;
            set
            {
                m_startDisplayingTab = value;
                Invalidate();
            }
        }

        private int EndDisplayingTab { get; set; }

        private int FirstDisplayingTab { get; set; }

        private bool DocumentTabsOverflow
        {
            set
            {
                if (m_documentTabsOverflow == value)
                    return;

                m_documentTabsOverflow = value;
                SetInertButtons();
            }
        }

        private static string ToolTipSelect => m_toolTipSelect ??= Language.TabsAndPanels;

        private TextFormatFlags ToolWindowTextFormat
        {
            get
            {
                const TextFormatFlags textFormat = TextFormatFlags.EndEllipsis |
                                                   TextFormatFlags.HorizontalCenter |
                                                   TextFormatFlags.SingleLine |
                                                   TextFormatFlags.VerticalCenter;
                if (RightToLeft == RightToLeft.Yes)
                    return textFormat | TextFormatFlags.RightToLeft | TextFormatFlags.Right;
                return textFormat;
            }
        }

        private TextFormatFlags DocumentTextFormat
        {
            get
            {
                const TextFormatFlags textFormat = TextFormatFlags.EndEllipsis |
                                                   TextFormatFlags.SingleLine |
                                                   TextFormatFlags.VerticalCenter |
                                                   TextFormatFlags.Left;
                if (RightToLeft == RightToLeft.Yes)
                    return textFormat | TextFormatFlags.RightToLeft | TextFormatFlags.Right;
                return textFormat;
            }
        }

        #endregion

        public DockPaneStripNG(DockPane pane)
            : base(pane)
        {
            SetStyle(ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);

            SuspendLayout();

            Components = new System.ComponentModel.Container();
            m_toolTip = new ToolTip(Components);
            SelectMenu = new ContextMenuStrip(Components);
            pane.DockPanel.Theme.ApplyTo(SelectMenu);

            ResumeLayout();

            ThemeManager.getInstance().ThemeChanged += OnThemeChanged;
        }

        // This seems like a bogus warning - suppressing since Components is being disposed...
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "<Components>k__BackingField")]
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ThemeManager.getInstance().ThemeChanged -= OnThemeChanged;
                Components?.Dispose();
            }

            base.Dispose(disposing);
        }

        private void OnThemeChanged()
        {
            if (IsDisposed) return;
            Invalidate(true);
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            PerformLayout();
            Invalidate();
        }

        protected override int MeasureHeight()
        {
            if (Appearance == DockPane.AppearanceStyle.ToolWindow)
                return MeasureHeight_ToolWindow();
            return MeasureHeight_Document();
        }

        private int MeasureHeight_ToolWindow()
        {
            if (DockPane.IsAutoHide || Tabs.Count <= 1)
                return 0;

            return Px(DockingStyle.ToolWindowStripHeight);
        }

        private int MeasureHeight_Document()
        {
            return Px(IsPanelStrip ? DockingStyle.PanelStripHeight : DockingStyle.DocumentStripHeight);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            CalculateTabs();
            if (Appearance == DockPane.AppearanceStyle.Document && DockPane.ActiveContent != null)
            {
                if (EnsureDocumentTabVisible(DockPane.ActiveContent, false))
                    CalculateTabs();
            }

            DrawTabStrip(e.Graphics);
        }

        protected override void OnRefreshChanges()
        {
            SetInertButtons();
            Invalidate();
        }

        public override GraphicsPath GetOutline(int index)
        {
            return Appearance == DockPane.AppearanceStyle.Document
                ? GetOutline_Document(index)
                : GetOutline_ToolWindow(index);
        }

        private GraphicsPath GetOutline_Document(int index)
        {
            Rectangle? rectangle = Tabs[index].Rectangle;
            if (rectangle == null) return null;
            Rectangle rectTab = rectangle.Value;
            rectTab.Intersect(TabsRectangle);
            rectTab = RectangleToScreen(DrawHelper.RtlTransform(this, rectTab));
            Rectangle rectPaneClient = DockPane.RectangleToScreen(DockPane.ClientRectangle);

            GraphicsPath path = new();
            GraphicsPath pathTab = GetTabOutline_Document(Tabs[index], true, true);
            path.AddPath(pathTab, true);

            if (IsTabStripAtBottom)
            {
                path.AddLine(rectTab.Right, rectTab.Top, rectPaneClient.Right, rectTab.Top);
                path.AddLine(rectPaneClient.Right, rectTab.Top, rectPaneClient.Right, rectPaneClient.Top);
                path.AddLine(rectPaneClient.Right, rectPaneClient.Top, rectPaneClient.Left, rectPaneClient.Top);
                path.AddLine(rectPaneClient.Left, rectPaneClient.Top, rectPaneClient.Left, rectTab.Top);
                path.AddLine(rectPaneClient.Left, rectTab.Top, rectTab.Right, rectTab.Top);
            }
            else
            {
                path.AddLine(rectTab.Right, rectTab.Bottom, rectPaneClient.Right, rectTab.Bottom);
                path.AddLine(rectPaneClient.Right, rectTab.Bottom, rectPaneClient.Right, rectPaneClient.Bottom);
                path.AddLine(rectPaneClient.Right, rectPaneClient.Bottom, rectPaneClient.Left, rectPaneClient.Bottom);
                path.AddLine(rectPaneClient.Left, rectPaneClient.Bottom, rectPaneClient.Left, rectTab.Bottom);
                path.AddLine(rectPaneClient.Left, rectTab.Bottom, rectTab.Right, rectTab.Bottom);
            }

            return path;
        }

        private GraphicsPath GetOutline_ToolWindow(int index)
        {
            Rectangle? rectangle = Tabs[index].Rectangle;
            if (rectangle == null) return null;
            Rectangle rectTab = rectangle.Value;
            rectTab.Intersect(TabsRectangle);
            rectTab = RectangleToScreen(DrawHelper.RtlTransform(this, rectTab));
            Rectangle rectPaneClient = DockPane.RectangleToScreen(DockPane.ClientRectangle);

            GraphicsPath path = new();
            GraphicsPath pathTab = GetTabOutline(Tabs[index], true, true);
            path.AddPath(pathTab, true);
            path.AddLine(rectTab.Left, rectTab.Top, rectPaneClient.Left, rectTab.Top);
            path.AddLine(rectPaneClient.Left, rectTab.Top, rectPaneClient.Left, rectPaneClient.Top);
            path.AddLine(rectPaneClient.Left, rectPaneClient.Top, rectPaneClient.Right, rectPaneClient.Top);
            path.AddLine(rectPaneClient.Right, rectPaneClient.Top, rectPaneClient.Right, rectTab.Top);
            path.AddLine(rectPaneClient.Right, rectTab.Top, rectTab.Right, rectTab.Top);
            return path;
        }

        #region Tab layout

        private void CalculateTabs()
        {
            if (Appearance == DockPane.AppearanceStyle.ToolWindow)
                CalculateTabs_ToolWindow();
            else
                CalculateTabs_Document();
        }

        private void CalculateTabs_ToolWindow()
        {
            if (Tabs.Count <= 1 || DockPane.IsAutoHide)
                return;

            Rectangle rectTabStrip = TabStripRectangle;
            int margin = Px(ToolWindowStripMargin);

            // Calculate tab widths
            int countTabs = Tabs.Count;
            foreach (Tab tab1 in Tabs)
            {
                MremoteNGTab tab = (MremoteNGTab)tab1;
                tab.MaxWidth = GetMaxTabWidth(Tabs.IndexOf(tab));
                tab.Flag = false;
            }

            // Set tab whose max width less than average width
            bool anyWidthWithinAverage;
            int totalWidth = Math.Max(0, rectTabStrip.Width - 2 * margin);
            int totalAllocatedWidth = 0;
            int averageWidth = totalWidth / countTabs;
            int remainedTabs = countTabs;
            for (anyWidthWithinAverage = true; anyWidthWithinAverage && remainedTabs > 0;)
            {
                anyWidthWithinAverage = false;
                foreach (Tab tab1 in Tabs)
                {
                    MremoteNGTab tab = (MremoteNGTab)tab1;
                    if (tab.Flag)
                        continue;

                    if (tab.MaxWidth > averageWidth) continue;
                    tab.Flag = true;
                    tab.TabWidth = tab.MaxWidth;
                    totalAllocatedWidth += tab.TabWidth;
                    anyWidthWithinAverage = true;
                    remainedTabs--;
                }

                if (remainedTabs != 0)
                    averageWidth = (totalWidth - totalAllocatedWidth) / remainedTabs;
            }

            // If any tab width not set yet, set it to the average width
            if (remainedTabs > 0)
            {
                int roundUpWidth = (totalWidth - totalAllocatedWidth) - (averageWidth * remainedTabs);
                foreach (Tab tab1 in Tabs)
                {
                    MremoteNGTab tab = (MremoteNGTab)tab1;
                    if (tab.Flag)
                        continue;

                    tab.Flag = true;
                    if (roundUpWidth > 0)
                    {
                        tab.TabWidth = averageWidth + 1;
                        roundUpWidth--;
                    }
                    else
                        tab.TabWidth = averageWidth;
                }
            }

            // Set the X position of the tabs
            int x = rectTabStrip.X + margin;
            foreach (Tab tab1 in Tabs)
            {
                MremoteNGTab tab = (MremoteNGTab)tab1;
                tab.TabX = x;
                x += tab.TabWidth;
            }
        }

        private bool CalculateDocumentTab(Rectangle rectTabStrip, ref int x, int index)
        {
            bool overflow = false;

            if (Tabs[index] is not MremoteNGTab tab) return false;
            tab.MaxWidth = GetMaxTabWidth(index);
            int width = Math.Min(tab.MaxWidth, Px(DocumentTabMaxWidth));
            if (x + width < rectTabStrip.Right || index == StartDisplayingTab)
            {
                tab.TabX = x;
                tab.TabWidth = width;
                EndDisplayingTab = index;
            }
            else
            {
                tab.TabX = 0;
                tab.TabWidth = 0;
                overflow = true;
            }

            x += width + Px(DocumentTabGap);

            return overflow;
        }

        /// <summary>
        /// Calculate which tabs are displayed and in what order.
        /// </summary>
        private void CalculateTabs_Document()
        {
            if (m_startDisplayingTab >= Tabs.Count)
                m_startDisplayingTab = 0;

            Rectangle rectTabStrip = TabsRectangle;

            int x = rectTabStrip.X;
            bool overflow = false;

            // Originally all new documents that were considered overflow
            // (not enough pane strip space to show all tabs) were added to
            // the far left (assuming not right to left) and the tabs on the
            // right were dropped from view. If StartDisplayingTab is not 0
            // then we are dealing with making sure a specific tab is kept in focus.
            if (m_startDisplayingTab > 0)
            {
                int tempX = x;
                if (Tabs[m_startDisplayingTab] is MremoteNGTab tab)
                    tab.MaxWidth = GetMaxTabWidth(m_startDisplayingTab);

                // Add the active tab and tabs to the left
                for (int i = StartDisplayingTab; i >= 0; i--)
                    CalculateDocumentTab(rectTabStrip, ref tempX, i);

                // Store which tab is the first one displayed so that it
                // will be drawn correctly (without part of the tab cut off)
                FirstDisplayingTab = EndDisplayingTab;

                tempX = x; // Reset X location because we are starting over

                // Start with the first tab displayed - name is a little misleading.
                // Loop through each tab and set its location. If there is not enough
                // room for all of them overflow will be returned.
                for (int i = EndDisplayingTab; i < Tabs.Count; i++)
                    overflow = CalculateDocumentTab(rectTabStrip, ref tempX, i);

                // If not all tabs are shown then we have an overflow.
                if (FirstDisplayingTab != 0)
                    overflow = true;
            }
            else
            {
                for (int i = StartDisplayingTab; i < Tabs.Count; i++)
                    overflow = CalculateDocumentTab(rectTabStrip, ref x, i);
                for (int i = 0; i < StartDisplayingTab; i++)
                    overflow = CalculateDocumentTab(rectTabStrip, ref x, i);

                FirstDisplayingTab = StartDisplayingTab;
            }

            if (!overflow)
            {
                m_startDisplayingTab = 0;
                FirstDisplayingTab = 0;
                x = rectTabStrip.X;
                foreach (Tab tab1 in Tabs)
                {
                    MremoteNGTab tab = (MremoteNGTab)tab1;
                    tab.TabX = x;
                    x += tab.TabWidth + Px(DocumentTabGap);
                }
            }

            DocumentTabsOverflow = overflow;
        }

        protected override void EnsureTabVisible(IDockContent content)
        {
            if (Appearance != DockPane.AppearanceStyle.Document || !Tabs.Contains(content))
                return;

            CalculateTabs();
            EnsureDocumentTabVisible(content, true);
        }

        private bool EnsureDocumentTabVisible(IDockContent content, bool repaint)
        {
            int index = Tabs.IndexOf(content);
            if (index == -1) // TODO: should prevent it from being -1;
                return false;

            if (Tabs[index] is MremoteNGTab tab && tab.TabWidth != 0)
                return false;

            StartDisplayingTab = index;
            if (repaint)
                Invalidate();

            return true;
        }

        private int GetMaxTabWidth(int index)
        {
            if (Appearance == DockPane.AppearanceStyle.ToolWindow)
                return GetMaxTabWidth_ToolWindow(index);
            return GetMaxTabWidth_Document(index);
        }

        private int GetMaxTabWidth_ToolWindow(int index)
        {
            IDockContent content = Tabs[index].Content;
            Size sizeString = TextRenderer.MeasureText(content.DockHandler.TabText, TabFont);
            return sizeString.Width + 2 * Px(ToolWindowTabPadding);
        }

        private int GetMaxTabWidth_Document(int index)
        {
            IDockContent content = Tabs[index].Content;
            Size sizeText = TextRenderer.MeasureText(content.DockHandler.TabText, TabFont, Size.Empty,
                                                     TextFormatFlags.SingleLine);

            int width = Px(DocumentTabPaddingLeft) + sizeText.Width;
            if (DockPane.DockPanel.ShowDocumentIcon)
                width += Px(DocumentIconSize) + Px(DocumentIconGapRight);

            int buttons = (ShouldShowCloseButton(content) ? 1 : 0) + (CanMinimize(content) ? 1 : 0);
            if (buttons > 0)
                width += Px(DocumentTextGapRight) + buttons * Px(TabButtonSize) + (buttons - 1) * Px(TabButtonGap) +
                         Px(TabButtonPaddingRight);
            else
                width += Px(DocumentTabPaddingRight);

            return width;
        }

        private Rectangle GetTabRectangle(int index)
        {
            return Appearance == DockPane.AppearanceStyle.ToolWindow
                ? GetTabRectangle_ToolWindow(index)
                : GetTabRectangle_Document(index);
        }

        private Rectangle GetTabRectangle_ToolWindow(int index)
        {
            Rectangle rectTabStrip = TabStripRectangle;

            MremoteNGTab tab = (MremoteNGTab)Tabs[index];
            return new Rectangle(tab.TabX, rectTabStrip.Y, tab.TabWidth, rectTabStrip.Height);
        }

        private Rectangle GetTabRectangle_Document(int index)
        {
            Rectangle rectTabStrip = TabStripRectangle;
            MremoteNGTab tab = (MremoteNGTab)Tabs[index];

            int top = DocumentTabTop;
            int height = rectTabStrip.Height - top;
            if (IsTabStripAtBottom && !IsPanelStrip)
                height -= Px(DockingStyle.DocumentTabTop);

            return new Rectangle(tab.TabX, rectTabStrip.Y + top, tab.TabWidth, height);
        }

        #endregion

        #region Painting

        private void DrawTabStrip(Graphics g)
        {
            ModernPalette palette = Palette;
            g.Clear(Appearance == DockPane.AppearanceStyle.Document ? palette.Chrome : palette.Panel);

            if (Appearance == DockPane.AppearanceStyle.Document)
                DrawTabStrip_Document(g, palette);
            else
                DrawTabStrip_ToolWindow(g, palette);
        }

        private void DrawTabStrip_Document(Graphics g, ModernPalette palette)
        {
            Rectangle client = ClientRectangle;
            if (IsPanelStrip)
            {
                using Pen line = new(palette.BorderSubtle);
                int y = IsTabStripAtBottom ? client.Top : client.Bottom - 1;
                g.DrawLine(line, client.Left, y, client.Right, y);
            }

            int count = Tabs.Count;
            if (count == 0)
                return;

            Rectangle rectTabOnly = TabsRectangle;
            MremoteNGTab tabActive = null;

            // Leave room around the tabs for the curved "feet" of the active browser tab.
            Rectangle clip = new(client.X, client.Y, rectTabOnly.Right + Px(StripButtonGapLeft) - client.X, client.Height);
            g.SetClip(DrawHelper.RtlTransform(this, clip));

            for (int i = 0; i < count; i++)
            {
                Rectangle rectTab = GetTabRectangle(i);
                if (Tabs[i] is not MremoteNGTab tab) continue;
                tab.Rectangle = rectTab;
                if (tab.Content == DockPane.ActiveContent)
                {
                    tabActive = tab;
                    continue;
                }

                if (tab.TabWidth == 0 || !rectTab.IntersectsWith(rectTabOnly)) continue;
                DrawTab_Document(g, tab, palette);
            }

            if (!IsPanelStrip)
                DrawDocumentSeparators(g, palette, tabActive);

            if (tabActive?.Rectangle != null && tabActive.TabWidth != 0)
            {
                Rectangle rectTab = tabActive.Rectangle.Value;
                if (rectTab.IntersectsWith(rectTabOnly))
                {
                    rectTab.Intersect(rectTabOnly);
                    tabActive.Rectangle = rectTab;
                    DrawTab_Document(g, tabActive, palette);
                }
            }

            g.ResetClip();
        }

        /// <summary>Hairlines between two adjacent idle browser tabs.</summary>
        private void DrawDocumentSeparators(Graphics g, ModernPalette palette, MremoteNGTab tabActive)
        {
            int gap = Px(DocumentTabGap);
            int inset = Px(10);
            using Pen pen = new(palette.Border);
            foreach (Tab tab1 in Tabs)
            {
                MremoteNGTab left = (MremoteNGTab)tab1;
                if (left.TabWidth == 0 || left == tabActive || left.Content == DockPane.MouseOverTab)
                    continue;

                foreach (Tab tab2 in Tabs)
                {
                    MremoteNGTab right = (MremoteNGTab)tab2;
                    if (right.TabWidth == 0 || right.TabX != left.TabX + left.TabWidth + gap)
                        continue;

                    if (right == tabActive || right.Content == DockPane.MouseOverTab)
                        break;

                    Rectangle rect = GetTabRectangle_Document(Tabs.IndexOf(left));
                    int x = rect.Right + gap / 2;
                    Point top = DrawHelper.RtlTransform(this, new Point(x, rect.Top + inset));
                    Point bottom = DrawHelper.RtlTransform(this, new Point(x, rect.Bottom - inset));
                    g.DrawLine(pen, top, bottom);
                    break;
                }
            }
        }

        private void DrawTabStrip_ToolWindow(Graphics g, ModernPalette palette)
        {
            Rectangle rect = TabStripRectangle;
            using (Pen line = new(palette.BorderSubtle))
                g.DrawLine(line, rect.Left, rect.Top, rect.Right, rect.Top);

            for (int i = 0; i < Tabs.Count; i++)
            {
                if (Tabs[i] is not MremoteNGTab tab) continue;
                tab.Rectangle = GetTabRectangle(i);
                DrawTab_ToolWindow(g, tab, palette);
            }
        }

        private GraphicsPath GetTabOutline(Tab tab, bool rtlTransform, bool toScreen)
        {
            return Appearance == DockPane.AppearanceStyle.ToolWindow
                ? GetTabOutline_ToolWindow(tab, rtlTransform, toScreen)
                : GetTabOutline_Document(tab, rtlTransform, toScreen);
        }

        private GraphicsPath GetTabOutline_ToolWindow(Tab tab, bool rtlTransform, bool toScreen)
        {
            Rectangle rect = GetTabRectangle(Tabs.IndexOf(tab));
            if (rtlTransform)
                rect = DrawHelper.RtlTransform(this, rect);
            if (toScreen)
                rect = RectangleToScreen(rect);

            GraphicsPath.Reset();
            GraphicsPath.AddRectangle(rect);
            return GraphicsPath;
        }

        private GraphicsPath GetTabOutline_Document(Tab tab, bool rtlTransform, bool toScreen)
        {
            GraphicsPath.Reset();
            Rectangle rect = GetTabRectangle(Tabs.IndexOf(tab));

            // Shorten TabOutline so it doesn't get overdrawn by icons next to it
            rect.Intersect(TabsRectangle);
            rect.Width--;

            if (rtlTransform)
                rect = DrawHelper.RtlTransform(this, rect);
            if (toScreen)
                rect = RectangleToScreen(rect);

            GraphicsPath.AddRectangle(rect);
            return GraphicsPath;
        }

        private void DrawTab_ToolWindow(Graphics g, MremoteNGTab tab, ModernPalette palette)
        {
            if (tab.Rectangle == null) return;
            Rectangle rect = DrawHelper.RtlTransform(this, tab.Rectangle.Value);
            bool selected = DockPane.ActiveContent == tab.Content;
            bool hovered = !selected && tab.Content == DockPane.MouseOverTab;

            if (hovered)
            {
                Rectangle highlight = new(rect.X + Px(2), rect.Y + Px(4), rect.Width - Px(4), rect.Height - Px(8));
                ModernGraphics.FillRoundedRectangle(g, palette.SurfaceHover, highlight, Px(ModernPalette.CornerRadius - 2));
            }

            string text = tab.Content.DockHandler.TabText;
            Rectangle rectText = new(rect.X + Px(ToolWindowTabPadding) / 2, rect.Y, rect.Width - Px(ToolWindowTabPadding), rect.Height);
            Color textColor = selected || hovered ? palette.Text : palette.TextSecondary;
            TextRenderer.DrawText(g, text, TabFont, rectText, textColor, ToolWindowTextFormat);

            if (selected)
            {
                int textWidth = Math.Min(TextRenderer.MeasureText(text, TabFont).Width, rectText.Width);
                int thickness = Px(2);
                Rectangle indicator = new(rect.X + (rect.Width - textWidth) / 2, rect.Bottom - Px(4) - thickness,
                                          textWidth, thickness);
                ModernGraphics.FillRoundedRectangle(g, DockingStyle.Indicator(DockPane.IsActivePane, palette.Panel),
                                                    indicator, thickness / 2f);
            }
        }

        private void DrawTab_Document(Graphics g, MremoteNGTab tab, ModernPalette palette)
        {
            if (tab.Rectangle == null || tab.TabWidth == 0) return;
            Rectangle rect = tab.Rectangle.Value;

            bool active = DockPane.ActiveContent == tab.Content;
            bool hovered = !active && tab.Content == DockPane.MouseOverTab;
            bool panelStrip = IsPanelStrip;
            bool atBottom = IsTabStripAtBottom;

            Rectangle rectCloseButton = GetCloseButtonRect(rect, tab.Content);
            Rectangle rectMinimizeButton = GetMinimizeButtonRect(rect, tab.Content);
            if (rectMinimizeButton == rectCloseButton)
                rectMinimizeButton = Rectangle.Empty;

            int iconSize = Px(DocumentIconSize);
            Rectangle rectIcon = new(rect.X + Px(DocumentTabPaddingLeft), rect.Y + (rect.Height - iconSize) / 2,
                                     iconSize, iconSize);
            int textLeft = rect.X + Px(DocumentTabPaddingLeft);
            if (DockPane.DockPanel.ShowDocumentIcon)
                textLeft = rectIcon.Right + Px(DocumentIconGapRight);

            int textRight = rect.Right - Px(DocumentTabPaddingRight);
            if (!rectCloseButton.IsEmpty)
                textRight = Math.Min(textRight, rectCloseButton.Left - Px(DocumentTextGapRight));
            if (!rectMinimizeButton.IsEmpty)
                textRight = Math.Min(textRight, rectMinimizeButton.Left - Px(DocumentTextGapRight));
            Rectangle rectText = new(textLeft, rect.Y, Math.Max(0, textRight - textLeft), rect.Height);

            Rectangle visual = DrawHelper.RtlTransform(this, rect);
            rectText = DrawHelper.RtlTransform(this, rectText);
            rectIcon = DrawHelper.RtlTransform(this, rectIcon);
            Rectangle rectCloseVisual = DrawHelper.RtlTransform(this, rectCloseButton);
            Rectangle rectMinimizeVisual = DrawHelper.RtlTransform(this, rectMinimizeButton);

            Color? customTabColor = GetCustomTabColor(tab.Content);
            Color tabSurface = palette.Chrome;

            if (panelStrip)
            {
                if (hovered)
                {
                    Rectangle highlight = new(visual.X, visual.Y + Px(5), visual.Width, visual.Height - Px(10));
                    ModernGraphics.FillRoundedRectangle(g, palette.SurfaceHover, highlight, Px(ModernPalette.CornerRadius));
                    tabSurface = palette.SurfaceHover;
                }

                if (active)
                {
                    int thickness = Px(2);
                    int inset = Px(8);
                    Rectangle underline = new(visual.X + inset,
                                              atBottom ? visual.Top : visual.Bottom - thickness,
                                              visual.Width - 2 * inset, thickness);
                    ModernGraphics.FillRoundedRectangle(g, customTabColor ?? palette.Accent, underline, thickness / 2f);
                }
            }
            else if (active)
            {
                tabSurface = palette.Content;
                DrawActiveBrowserTab(g, visual, palette, atBottom,
                                     customTabColor ?? DockingStyle.Indicator(DockPane.IsActiveDocumentPane, palette.Content));
            }
            else
            {
                if (hovered)
                {
                    Rectangle highlight = atBottom
                        ? new Rectangle(visual.X, visual.Y + Px(4), visual.Width, visual.Height - Px(6))
                        : new Rectangle(visual.X, visual.Y + Px(2), visual.Width, visual.Height - Px(6));
                    ModernGraphics.FillRoundedRectangle(g, palette.SurfaceHover, highlight, Px(ModernPalette.CornerRadius));
                    tabSurface = palette.SurfaceHover;
                }

                if (customTabColor.HasValue)
                {
                    // Per-connection tab color: a colored base line instead of filling the whole tab
                    // (the active tab carries it as its top indicator instead).
                    int thickness = Px(2);
                    int inset = Px(DocumentTabPaddingLeft);
                    Rectangle bar = new(visual.X + inset,
                                        atBottom ? visual.Top + Px(4) : visual.Bottom - Px(3) - thickness,
                                        visual.Width - 2 * inset, thickness);
                    ModernGraphics.FillRoundedRectangle(g, customTabColor.Value, bar, thickness / 2f);
                }
            }

            if (DockPane.DockPanel.ShowDocumentIcon && tab.Content.DockHandler.Icon != null &&
                visual.Contains(rectIcon))
            {
                g.DrawIcon(tab.Content.DockHandler.Icon, rectIcon);
            }

            Color textColor = active || hovered ? palette.Text : palette.TextSecondary;
            TextRenderer.DrawText(g, tab.Content.DockHandler.TabText, TabFont, rectText, textColor, DocumentTextFormat);

            if (active || hovered)
            {
                if (!rectMinimizeVisual.IsEmpty)
                    DrawTabButton(g, rectMinimizeVisual, FluentIcons.ChevronDown, rectMinimizeVisual == ActiveMinimize,
                                  tabSurface, palette);
                if (!rectCloseVisual.IsEmpty)
                    DrawTabButton(g, rectCloseVisual, FluentIcons.Cancel, rectCloseVisual == ActiveClose,
                                  tabSurface, palette);
            }
        }

        /// <summary>
        /// Rounded tab filled with the Content color so it merges into the document area, with curved
        /// "feet" at the base and an accent (or connection color) indicator on its outer edge.
        /// </summary>
        private void DrawActiveBrowserTab(Graphics g, Rectangle visual, ModernPalette palette, bool atBottom, Color indicatorColor)
        {
            int radius = Px(ModernPalette.LargeCornerRadius);
            int foot = Px(ModernPalette.CornerRadius);
            SmoothingMode previousMode = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using GraphicsPath path = CreateTabPath(visual, radius, atBottom);
            using (SolidBrush content = new(palette.Content))
            {
                g.FillPath(content, path);

                // Inverted corners joining the tab to the content area below it.
                int baseY = atBottom ? visual.Top : visual.Bottom;
                using SolidBrush chrome = new(palette.Chrome);
                foreach (int side in new[] { -1, 1 })
                {
                    int edgeX = side < 0 ? visual.Left : visual.Right;
                    Rectangle square = new(side < 0 ? edgeX - foot : edgeX, atBottom ? baseY : baseY - foot, foot, foot);
                    g.FillRectangle(content, square);
                    Rectangle circle = new(side < 0 ? edgeX - 2 * foot : edgeX, atBottom ? baseY : baseY - 2 * foot,
                                           2 * foot, 2 * foot);
                    g.FillEllipse(chrome, circle);
                }
            }

            Region previousClip = g.Clip;
            g.SetClip(path, CombineMode.Intersect);
            int thickness = Px(2);
            using (SolidBrush indicator = new(indicatorColor))
            {
                Rectangle bar = new(visual.X, atBottom ? visual.Bottom - thickness : visual.Top, visual.Width, thickness);
                g.FillRectangle(indicator, bar);
            }

            g.Clip = previousClip;
            previousClip.Dispose();
            g.SmoothingMode = previousMode;
        }

        private static GraphicsPath CreateTabPath(Rectangle rect, int radius, bool roundBottom)
        {
            GraphicsPath path = new();
            int d = Math.Max(1, Math.Min(radius * 2, Math.Min(rect.Width, rect.Height)));
            if (!roundBottom)
            {
                path.AddLine(rect.Left, rect.Bottom, rect.Left, rect.Top + d / 2);
                path.AddArc(rect.Left, rect.Top, d, d, 180, 90);
                path.AddArc(rect.Right - d, rect.Top, d, d, 270, 90);
                path.AddLine(rect.Right, rect.Top + d / 2, rect.Right, rect.Bottom);
            }
            else
            {
                path.AddLine(rect.Right, rect.Top, rect.Right, rect.Bottom - d / 2);
                path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
                path.AddArc(rect.Left, rect.Bottom - d, d, d, 90, 90);
                path.AddLine(rect.Left, rect.Bottom - d / 2, rect.Left, rect.Top);
            }

            path.CloseFigure();
            return path;
        }

        private void DrawTabButton(Graphics g, Rectangle rect, char glyph, bool hot, Color surface, ModernPalette palette)
        {
            if (hot)
            {
                Color highlight = IsMouseDown
                    ? palette.SurfacePressed
                    : surface == palette.SurfaceHover ? palette.SurfacePressed : palette.SurfaceHover;
                ModernGraphics.FillRoundedRectangle(g, highlight, rect, Px(4));
            }

            FluentIcons.Draw(g, glyph, rect, hot ? palette.Text : palette.TextSecondary, Px(10));
        }

        private Color? GetCustomTabColor(IDockContent content)
        {
            try
            {
                if (content is ConnectionTab connectionTab)
                {
                    string tabColorStr = InterfaceControl.FindInterfaceControl(connectionTab)?.Info?.TabColor ??
                                         (connectionTab.Tag as ConnectionInfo)?.TabColor;
                    if (!string.IsNullOrEmpty(tabColorStr))
                    {
                        ColorConverter converter = new();
                        Color color = (Color)converter.ConvertFromString(tabColorStr);
                        if (!color.IsEmpty && color.A > 0)
                            return Color.FromArgb(255, color);
                    }
                }
            }
            catch
            {
                // If there's any error parsing the color, just return null to use default
            }

            return null;
        }

        #endregion

        #region Mouse

        private bool m_isMouseDown;

        protected bool IsMouseDown
        {
            get => m_isMouseDown;
            private set
            {
                if (m_isMouseDown == value)
                    return;

                m_isMouseDown = value;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (IsMouseDown)
                IsMouseDown = false;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            // suspend drag if mouse is down on an action button.
            m_suspendDrag = ActiveCloseHitTest(e.Location) || ActiveMinimizeHitTest(e.Location);
            if (!IsMouseDown)
                IsMouseDown = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (!m_suspendDrag)
                base.OnMouseMove(e);

            int index = HitTest(PointToClient(MousePosition));
            string toolTip = string.Empty;

            bool tabUpdate = false;
            bool buttonUpdate = false;
            if (index != -1)
            {
                if (Tabs[index] is MremoteNGTab tab)
                {
                    tabUpdate = SetMouseOverTab(tab.Content == DockPane.ActiveContent ? null : tab.Content);

                    if (!string.IsNullOrEmpty(tab.Content.DockHandler.ToolTipText))
                        toolTip = tab.Content.DockHandler.ToolTipText;
                    else if (tab.MaxWidth > tab.TabWidth)
                        toolTip = tab.Content.DockHandler.TabText;

                    Point mousePos = PointToClient(MousePosition);
                    if (tab.Rectangle != null)
                    {
                        Rectangle tabRect = tab.Rectangle.Value;
                        Rectangle minimizeButtonRect = DrawHelper.RtlTransform(this, GetMinimizeButtonRect(tabRect, tab.Content));
                        Rectangle closeButtonRect = DrawHelper.RtlTransform(this, GetCloseButtonRect(tabRect, tab.Content));
                        Rectangle mouseRect = new(mousePos, new Size(1, 1));
                        bool minimizeUpdated = SetActiveMinimize(minimizeButtonRect.IntersectsWith(mouseRect)
                                                                     ? minimizeButtonRect
                                                                     : Rectangle.Empty);
                        bool closeUpdated = SetActiveClose(closeButtonRect.IntersectsWith(mouseRect)
                                                               ? closeButtonRect
                                                               : Rectangle.Empty);
                        buttonUpdate = minimizeUpdated || closeUpdated;
                    }
                }
            }
            else
            {
                tabUpdate = SetMouseOverTab(null);
                buttonUpdate = SetActiveMinimize(Rectangle.Empty) | SetActiveClose(Rectangle.Empty);
            }

            if (tabUpdate || buttonUpdate)
                Invalidate();

            if (m_toolTip.GetToolTip(this) == toolTip) return;
            m_toolTip.Active = false;
            m_toolTip.SetToolTip(this, toolTip);
            m_toolTip.Active = true;
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button != MouseButtons.Left || Appearance != DockPane.AppearanceStyle.Document)
                return;

            int indexHit = HitTest();
            if (indexHit > -1)
            {
                if (TabMinimizeButtonHit(indexHit))
                    return;
                TabCloseButtonHit(indexHit);
            }
        }

        private void TabCloseButtonHit(int index)
        {
            Point mousePos = PointToClient(MousePosition);
            Rectangle tabRect = GetTabBounds(Tabs[index]);
            if (tabRect.Contains(ActiveClose) && ActiveCloseHitTest(mousePos))
                CloseTab(index);
        }

        private bool TabMinimizeButtonHit(int index)
        {
            Point mousePos = PointToClient(MousePosition);
            Rectangle tabRect = GetTabBounds(Tabs[index]);
            if (tabRect.Contains(ActiveMinimize) && ActiveMinimizeHitTest(mousePos))
            {
                MinimizeConnectionTab(index);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Closes the tab at <paramref name="index"/>, keeping the current selection when
        /// the tab does not actually close.
        /// </summary>
        /// <remarks>
        /// DockPanelSuite's TryCloseTab selects the neighbouring tab afterwards without
        /// checking whether the close happened, so dismissing the "are you sure" prompt
        /// still switched the active connection.
        /// </remarks>
        private void CloseTab(int index)
        {
            IDockContent tabContent = Tabs[index].Content;
            IDockContent? activeBeforeClose = DockPane.ActiveContent;

            TryCloseTab(index);

            if (activeBeforeClose == null || ReferenceEquals(DockPane.ActiveContent, activeBeforeClose))
                return;

            // The tab is still on display, so the close was refused and the selection
            // moved for nothing.
            if (DockPane.DisplayingContents.Contains(tabContent) &&
                DockPane.DisplayingContents.Contains(activeBeforeClose))
            {
                activeBeforeClose.DockHandler.Activate();
            }
        }

        internal void MinimizeConnectionTab(int index)
        {
            if (Tabs[index].Content is not ConnectionTab connectionTab)
                return;

            if (!connectionTab.CanMinimizeToBottomAutoHide())
                return;

            connectionTab.MinimizeToBottomAutoHide();
        }

        #endregion

        #region Tab buttons

        private Rectangle GetCloseButtonRect(Rectangle rectTab, IDockContent content)
        {
            if (Appearance != DockPane.AppearanceStyle.Document || !ShouldShowCloseButton(content))
            {
                return Rectangle.Empty;
            }

            return GetRightAlignedDocumentButtonRect(rectTab);
        }

        private Rectangle GetRightAlignedDocumentButtonRect(Rectangle rectTab)
        {
            int size = Px(TabButtonSize);
            int rightEdge = Math.Min(rectTab.Right, TabsRectangle.Right) - Px(TabButtonPaddingRight);
            return new Rectangle(rightEdge - size, rectTab.Y + (rectTab.Height - size) / 2, size, size);
        }

        private Rectangle GetMinimizeButtonRect(Rectangle rectTab, IDockContent content)
        {
            if (Appearance != DockPane.AppearanceStyle.Document || !CanMinimize(content))
            {
                return Rectangle.Empty;
            }

            Rectangle closeButtonRect = GetCloseButtonRect(rectTab, content);
            if (closeButtonRect.IsEmpty)
            {
                Rectangle rightAlignedButtonRect = GetRightAlignedDocumentButtonRect(rectTab);
                return rightAlignedButtonRect.Left >= rectTab.Left ? rightAlignedButtonRect : Rectangle.Empty;
            }

            Rectangle minimizeButtonRect = new(closeButtonRect.X - closeButtonRect.Width - Px(TabButtonGap), closeButtonRect.Y,
                                               closeButtonRect.Width, closeButtonRect.Height);
            return minimizeButtonRect.Left >= rectTab.Left ? minimizeButtonRect : closeButtonRect;
        }

        private static bool CanMinimize(IDockContent content)
        {
            return content is ConnectionTab connectionTab && connectionTab.CanMinimizeToBottomAutoHide();
        }

        private static bool ShouldShowCloseButton(IDockContent content)
        {
            return content is DockContent dockContent &&
                   dockContent.CloseButton &&
                   dockContent.CloseButtonVisible;
        }

        #endregion

        #region Window list

        private void WindowList_Click(object sender, EventArgs e)
        {
            SelectMenu.Items.Clear();
            foreach (Tab tab1 in Tabs)
            {
                MremoteNGTab tab = (MremoteNGTab)tab1;
                IDockContent content = tab.Content;
                ToolStripItem item = SelectMenu.Items.Add(content.DockHandler.TabText, content.DockHandler.Icon?.ToBitmap());
                item.Tag = tab.Content;
                item.Click += ContextMenuItem_Click;
            }

            Control button = m_documentTabsOverflow ? ButtonOverflow : ButtonWindowList;
            Rectangle workingArea =
                Screen.GetWorkingArea(button.PointToScreen(new Point(button.Width / 2, button.Height / 2)));
            Rectangle menu = new(button.PointToScreen(new Point(0, button.Height)), SelectMenu.Size);
            Rectangle menuMargined = new(menu.X - SelectMenuMargin, menu.Y - SelectMenuMargin,
                                         menu.Width + SelectMenuMargin, menu.Height + SelectMenuMargin);
            if (workingArea.Contains(menuMargined))
            {
                SelectMenu.Show(menu.Location);
            }
            else
            {
                Point newPoint = menu.Location;
                newPoint.X = DrawHelper.Balance(SelectMenu.Width, SelectMenuMargin, newPoint.X, workingArea.Left,
                                                workingArea.Right);
                newPoint.Y = DrawHelper.Balance(SelectMenu.Size.Height, SelectMenuMargin, newPoint.Y, workingArea.Top,
                                                workingArea.Bottom);
                Point buttonBottom = button.PointToScreen(new Point(0, button.Height));
                if (newPoint.Y < buttonBottom.Y)
                {
                    // flip the menu up to be above the button.
                    newPoint.Y = buttonBottom.Y - button.Height;
                    SelectMenu.Show(newPoint, ToolStripDropDownDirection.AboveRight);
                }
                else
                {
                    SelectMenu.Show(newPoint);
                }
            }
        }

        private void ContextMenuItem_Click(object sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem item) return;
            IDockContent content = (IDockContent)item.Tag;
            DockPane.ActiveContent = content;
        }

        private void SetInertButtons()
        {
            if (Appearance == DockPane.AppearanceStyle.ToolWindow)
            {
                if (m_buttonOverflow != null)
                    m_buttonOverflow.Left = -m_buttonOverflow.Width;

                if (m_buttonWindowList != null)
                    m_buttonWindowList.Left = -m_buttonWindowList.Width;
            }
            else
            {
                ButtonOverflow.Visible = m_documentTabsOverflow;
                ButtonOverflow.RefreshChanges();

                ButtonWindowList.Visible = !m_documentTabsOverflow;
                ButtonWindowList.RefreshChanges();
            }
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            if (Appearance == DockPane.AppearanceStyle.Document)
            {
                LayoutButtons();
                OnRefreshChanges();
            }

            base.OnLayout(levent);
        }

        private void LayoutButtons()
        {
            Rectangle rectTabStrip = TabStripRectangle;
            int size = Px(StripButtonSize);
            int top = DocumentTabTop;
            int bottomGap = IsTabStripAtBottom && !IsPanelStrip ? Px(DockingStyle.DocumentTabTop) : 0;
            int y = rectTabStrip.Y + top + (rectTabStrip.Height - top - bottomGap - size) / 2;
            int x = rectTabStrip.Right - Px(StripButtonGapRight) - size;
            Rectangle bounds = DrawHelper.RtlTransform(this, new Rectangle(x, y, size, size));

            ButtonOverflow.Bounds = bounds;
            ButtonWindowList.Bounds = bounds;
        }

        #endregion

        private void Close_Click(object sender, EventArgs e)
        {
            CloseProtocol();

            if (PatchController.EnableMemoryLeakFix == true)
            {
                ContentClosed();
            }
        }

        protected override int HitTest(Point point)
        {
            if (!TabsRectangle.Contains(point))
                return -1;

            foreach (Tab tab in Tabs)
            {
                GraphicsPath path = GetTabOutline(tab, true, false);
                if (path.IsVisible(point))
                    return Tabs.IndexOf(tab);
            }

            return -1;
        }

        protected override bool MouseDownActivateTest(MouseEventArgs e)
        {
            bool result = base.MouseDownActivateTest(e);
            if (result && (e.Button == MouseButtons.Left) && (Appearance == DockPane.AppearanceStyle.Document))
            {
                // don't activate if mouse is down on an action button
                result = !ActiveCloseHitTest(e.Location) && !ActiveMinimizeHitTest(e.Location);
            }

            return result;
        }

        private bool ActiveCloseHitTest(Point ptMouse)
        {
            if (ActiveClose.IsEmpty) return false;
            Rectangle mouseRect = new(ptMouse, new Size(1, 1));
            return ActiveClose.IntersectsWith(mouseRect);
        }

        private bool ActiveMinimizeHitTest(Point ptMouse)
        {
            if (ActiveMinimize.IsEmpty) return false;
            Rectangle mouseRect = new(ptMouse, new Size(1, 1));
            return ActiveMinimize.IntersectsWith(mouseRect);
        }

        protected override Rectangle GetTabBounds(Tab tab)
        {
            GraphicsPath path = GetTabOutline(tab, true, false);
            RectangleF rectangle = path.GetBounds();
            return new Rectangle((int)rectangle.Left, (int)rectangle.Top, (int)rectangle.Width, (int)rectangle.Height);
        }

        private Rectangle ActiveClose { get; set; }

        private Rectangle ActiveMinimize { get; set; }

        private bool SetActiveClose(Rectangle rectangle)
        {
            if (ActiveClose == rectangle)
                return false;

            ActiveClose = rectangle;
            return true;
        }

        private bool SetActiveMinimize(Rectangle rectangle)
        {
            if (ActiveMinimize == rectangle)
                return false;

            ActiveMinimize = rectangle;
            return true;
        }

        private bool SetMouseOverTab(IDockContent content)
        {
            if (DockPane.MouseOverTab == content)
                return false;

            DockPane.MouseOverTab = content;
            return true;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            bool tabUpdate = SetMouseOverTab(null);
            bool buttonUpdate = SetActiveMinimize(Rectangle.Empty) | SetActiveClose(Rectangle.Empty);
            if (tabUpdate || buttonUpdate)
                Invalidate();

            base.OnMouseLeave(e);
        }

        protected override void OnRightToLeftChanged(EventArgs e)
        {
            base.OnRightToLeftChanged(e);
            PerformLayout();
        }

        private void CloseProtocol()
        {
            InterfaceControl ic = InterfaceControl.FindInterfaceControl(DockPane.DockPanel);
            ic?.Protocol.Close();
        }

        #region Native Methods

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == (int)Msgs.WM_LBUTTONDBLCLK)
            {
                // If the option is not set, do nothing. Do not send the message to base.
                if (!Properties.OptionsTabsPanelsPage.Default.DoubleClickOnTabClosesIt) return;

                // Option is set, close the tab, then send to base.
                //DockPane.CloseActiveContent();
                CloseProtocol();

                if (PatchController.EnableMemoryLeakFix == true)
                {
                    ContentClosed();
                }

                return;
            }

            base.WndProc(ref m);
        }

        #endregion
    }
}
