using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.Resources.Language;
using mRemoteNG.Themes;
using WeifenLuo.WinFormsUI.Docking;

namespace mRemoteNG.UI.Tabs
{
    /// <summary>
    /// Tool window caption ("CONNECTIONS ········ pin ×"): flat Panel background, small uppercase
    /// semibold title and Fluent glyph buttons with rounded hover highlights. Replaces the VS2013
    /// caption with its dotted grip.
    /// </summary>
    [SupportedOSPlatform("windows")]
    [ToolboxItem(false)]
    internal sealed class ModernDockPaneCaption : DockPaneCaptionBase
    {
        private const int TextGapLeft = 12;
        private const int ButtonSize = 24;
        private const int ButtonGap = 2;
        private const int ButtonGapRight = 6;

        private readonly IContainer _components;
        private readonly ToolTip _toolTip;
        private DockGlyphButton? _buttonClose;
        private DockGlyphButton? _buttonAutoHide;
        private DockGlyphButton? _buttonOptions;

        public ModernDockPaneCaption(DockPane pane) : base(pane)
        {
            SuspendLayout();
            _components = new System.ComponentModel.Container();
            _toolTip = new ToolTip(_components);
            ResumeLayout();
            ThemeManager.getInstance().ThemeChanged += OnThemeChanged;
        }

        private DockGlyphButton ButtonClose
        {
            get
            {
                if (_buttonClose != null) return _buttonClose;
                _buttonClose = CreateButton(FluentIcons.Cancel, 10f, Language._Close.Replace("&", string.Empty));
                _buttonClose.Click += (_, _) => DockPane.CloseActiveContent();
                return _buttonClose;
            }
        }

        private DockGlyphButton ButtonAutoHide
        {
            get
            {
                if (_buttonAutoHide != null) return _buttonAutoHide;
                _buttonAutoHide = CreateButton(FluentIcons.Pin, 12f, Language.AutoHide);
                _buttonAutoHide.Click += AutoHide_Click;
                return _buttonAutoHide;
            }
        }

        private DockGlyphButton ButtonOptions
        {
            get
            {
                if (_buttonOptions != null) return _buttonOptions;
                _buttonOptions = CreateButton(FluentIcons.ChevronDown, 10f, Language.Options);
                _buttonOptions.Click += (_, _) => ShowTabPageContextMenu(PointToClient(MousePosition));
                return _buttonOptions;
            }
        }

        private DockGlyphButton CreateButton(char glyph, float glyphSize, string toolTip)
        {
            DockGlyphButton button = new(glyph, glyphSize)
            {
                SurfaceColor = () => DockingStyle.Palette.Panel
            };
            _toolTip.SetToolTip(button, toolTip);
            Controls.Add(button);
            return button;
        }

        private bool CloseButtonEnabled => DockPane.ActiveContent?.DockHandler.CloseButton ?? false;

        private bool CloseButtonVisible => DockPane.ActiveContent?.DockHandler.CloseButtonVisible ?? false;

        private bool ShouldShowAutoHideButton => !DockPane.IsFloat;

        protected override bool CanDragAutoHide => true;

        protected override int MeasureHeight() => DockingStyle.Scale(this, DockingStyle.CaptionHeight);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ThemeManager.getInstance().ThemeChanged -= OnThemeChanged;
                _components.Dispose();
            }

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
            Rectangle client = ClientRectangle;
            if (client.Width == 0 || client.Height == 0)
                return;

            ModernPalette palette = DockingStyle.Palette;
            Graphics g = e.Graphics;
            g.Clear(palette.Panel);

            Rectangle text = client;
            text.X += DockingStyle.Scale(this, TextGapLeft);
            text.Width = ButtonsLeft() - text.X - DockingStyle.Scale(this, 4);
            if (text.Width <= 0)
                return;

            string caption = (DockPane.CaptionText ?? string.Empty).ToUpper(CultureInfo.CurrentCulture);
            Color color = DockPane.IsActivePane ? palette.Text : palette.TextSecondary;
            TextFormatFlags flags = TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine |
                                    TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix;
            if (RightToLeft == RightToLeft.Yes)
                flags |= TextFormatFlags.RightToLeft | TextFormatFlags.Right;

            TextRenderer.DrawText(g, caption, DockingStyle.GetFont(this, DockingStyle.CaptionPixelSize, true),
                                  DrawHelper.RtlTransform(this, text), color, flags);
        }

        /// <summary>X of the left-most visible caption button (in logical, non-RTL coordinates).</summary>
        private int ButtonsLeft()
        {
            int x = ClientRectangle.Right - DockingStyle.Scale(this, ButtonGapRight);
            int step = DockingStyle.Scale(this, ButtonSize + ButtonGap);
            if (CloseButtonVisible) x -= step;
            if (ShouldShowAutoHideButton) x -= step;
            if (HasTabPageContextMenu) x -= step;
            return x;
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            SetButtonsPosition();
            base.OnLayout(levent);
        }

        protected override void OnRefreshChanges()
        {
            SetButtons();
            Invalidate();
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            SetButtonsPosition();
        }

        private void SetButtons()
        {
            ButtonClose.Enabled = CloseButtonEnabled;
            ButtonClose.Visible = CloseButtonVisible;
            ButtonAutoHide.Visible = ShouldShowAutoHideButton;
            ButtonAutoHide.Glyph = DockPane.IsAutoHide ? FluentIcons.Unpin : FluentIcons.Pin;
            ButtonOptions.Visible = HasTabPageContextMenu;
            ButtonClose.RefreshChanges();
            ButtonAutoHide.RefreshChanges();
            ButtonOptions.RefreshChanges();
            SetButtonsPosition();
        }

        private void SetButtonsPosition()
        {
            Rectangle client = ClientRectangle;
            int size = DockingStyle.Scale(this, ButtonSize);
            int step = size + DockingStyle.Scale(this, ButtonGap);
            Point location = new(client.Right - DockingStyle.Scale(this, ButtonGapRight) - size,
                                 client.Y + (client.Height - size) / 2);
            Size buttonSize = new(size, size);

            ButtonClose.Bounds = DrawHelper.RtlTransform(this, new Rectangle(location, buttonSize));
            if (CloseButtonVisible)
                location.Offset(-step, 0);

            ButtonAutoHide.Bounds = DrawHelper.RtlTransform(this, new Rectangle(location, buttonSize));
            if (ShouldShowAutoHideButton)
                location.Offset(-step, 0);

            ButtonOptions.Bounds = DrawHelper.RtlTransform(this, new Rectangle(location, buttonSize));
        }

        private void AutoHide_Click(object? sender, EventArgs e)
        {
            DockPane.DockState = DockHelper.ToggleAutoHideState(DockPane.DockState);
            if (!DockHelper.IsDockStateAutoHide(DockPane.DockState)) return;
            DockPane.DockPanel.ActiveAutoHideContent = null;
            DockPane.NestedDockingStatus.NestedPanes.SwitchPaneWithFirstChild(DockPane);
        }

        protected override void OnRightToLeftChanged(EventArgs e)
        {
            base.OnRightToLeftChanged(e);
            PerformLayout();
        }
    }

    [SupportedOSPlatform("windows")]
    public class ModernDockPaneCaptionFactory : DockPanelExtender.IDockPaneCaptionFactory
    {
        public DockPaneCaptionBase CreateDockPaneCaption(DockPane pane) => new ModernDockPaneCaption(pane);
    }
}
