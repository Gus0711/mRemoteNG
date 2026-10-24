using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using mRemoteNG.App;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Container;
using mRemoteNG.Properties;
using mRemoteNG.Themes;
using mRemoteNG.Tools;
using mRemoteNG.Resources.Language;
using System.Runtime.Versioning;

namespace mRemoteNG.UI.Controls
{
    /// <summary>
    /// Quick connect toolbar: rounded search-style input (with protocol pill and history), a primary
    /// accent "Connect" button and the connections drop-down.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class QuickConnectToolStrip : ToolStrip
    {
        private IContainer components;
        private ToolStripDropDownButton _btnConnections;
        private ToolStripButton _btnQuickConnect;
        private ContextMenuStrip _mnuQuickConnectProtocol;
        private QuickConnectComboBox _cmbQuickConnect;
        private ContextMenuStrip _mnuConnections;
        private readonly ThemeManager _themeManager;
        private WeifenLuo.WinFormsUI.Docking.VisualStudioToolStripExtender vsToolStripExtender;


        public QuickConnectToolStrip()
        {
            Initialize();
            _themeManager = ThemeManager.getInstance();
            _themeManager.ThemeChanged += ApplyTheme;
            PopulateQuickConnectProtocolMenu();
            ApplyTheme();
            ApplyLanguage();
        }

        public void ApplyLanguage()
        {
            _btnQuickConnect.Text = Language.Connect;
            _btnQuickConnect.ToolTipText = Language.QuickConnect;
            _btnConnections.Text = Language.Connections;
            _cmbQuickConnect.PlaceholderText = Language.QuickConnectPlaceholder;
        }

        private void Initialize()
        {
            components = new System.ComponentModel.Container();
            _cmbQuickConnect = new QuickConnectComboBox();
            _btnQuickConnect = new ToolStripButton();
            _mnuQuickConnectProtocol = new ContextMenuStrip(components);
            _btnConnections = new ToolStripDropDownButton();
            _mnuConnections = new ContextMenuStrip(components);
            SuspendLayout();
            //
            //Theming support
            //
            vsToolStripExtender = new WeifenLuo.WinFormsUI.Docking.VisualStudioToolStripExtender(components);
            //
            // cmbQuickConnect
            //
            _cmbQuickConnect.Name = "cmbQuickConnect";
            _cmbQuickConnect.ProtocolMenu = _mnuQuickConnectProtocol;
            _cmbQuickConnect.ConnectRequested += cmbQuickConnect_ConnectRequested;
            _cmbQuickConnect.ProtocolChanged += cmbQuickConnect_ProtocolChanged;
            //
            // tsQuickConnect
            //
            Dock = DockStyle.None;
            GripMargin = new Padding(2);
            Items.AddRange(new ToolStripItem[]
            {
                _cmbQuickConnect,
                _btnQuickConnect,
                _btnConnections
            });
            Location = new Point(3, 24);
            Name = "tsQuickConnect";
            TabIndex = 18;
            //
            // btnQuickConnect
            //
            _btnQuickConnect.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            _btnQuickConnect.Name = "btnQuickConnect";
            _btnQuickConnect.Text = Language.Connect;
            _btnQuickConnect.Click += btnQuickConnect_ButtonClick;
            ToolStripGlyphs.Set(_btnQuickConnect, FluentIcons.Play);
            ModernToolStripRenderer.SetPrimary(_btnQuickConnect);
            //
            // mnuQuickConnectProtocol
            //
            _mnuQuickConnectProtocol.Name = "mnuQuickConnectProtocol";
            _mnuQuickConnectProtocol.ShowCheckMargin = true;
            _mnuQuickConnectProtocol.ShowImageMargin = false;
            _mnuQuickConnectProtocol.ItemClicked += mnuQuickConnectProtocol_ItemClicked;
            //
            // btnConnections
            //
            _btnConnections.DisplayStyle = ToolStripItemDisplayStyle.Image;
            _btnConnections.DropDown = _mnuConnections;
            _btnConnections.Name = "btnConnections";
            _btnConnections.Text = Language.Connections;
            _btnConnections.DropDownOpening += btnConnections_DropDownOpening;
            ToolStripGlyphs.Set(_btnConnections, ChromeGlyphs.List);
            //
            // mnuConnections
            //
            _mnuConnections.Name = "mnuConnections";
            _mnuConnections.OwnerItem = _btnConnections;

            ApplyMetrics();
            ResumeLayout();
        }

        /// <summary>Header-friendly sizes: a 40px (logical) bar with 30px controls.</summary>
        private void ApplyMetrics()
        {
            float scale = DeviceDpi / 96f;
            int s(int v) => (int)Math.Round(v * scale);

            Padding = new Padding(s(4), s(5), s(8), s(5));
            MinimumSize = new Size(0, s(40));
            _btnQuickConnect.Padding = new Padding(s(10), s(5), s(12), s(5));
            _btnQuickConnect.Margin = new Padding(s(2), 0, s(2), 0);
            _btnConnections.Padding = new Padding(s(6), s(5), s(4), s(5));
            _btnConnections.Margin = new Padding(s(2), 0, 0, 0);
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            ApplyMetrics();
        }

        private void ApplyTheme()
        {
            Font = ModernToolStripRenderer.UiFont;
            if (!_themeManager.ThemingActive) return;
            vsToolStripExtender.SetStyle(_mnuQuickConnectProtocol, _themeManager.ActiveTheme.Version,
                                         _themeManager.ActiveTheme.Theme);
            vsToolStripExtender.SetStyle(_mnuConnections, _themeManager.ActiveTheme.Version,
                                         _themeManager.ActiveTheme.Theme);
            vsToolStripExtender.SetStyle(_cmbQuickConnect.HistoryDropDown, _themeManager.ActiveTheme.Version,
                                         _themeManager.ActiveTheme.Theme);
            _cmbQuickConnect.Input.Invalidate();
        }

        #region Quick Connect

        private void PopulateQuickConnectProtocolMenu()
        {
            try
            {
                _mnuQuickConnectProtocol.Items.Clear();
                foreach (System.Reflection.FieldInfo fieldInfo in typeof(ProtocolType).GetFields())
                {
                    if (fieldInfo.Name == "value__" || fieldInfo.Name == "IntApp") continue;
                    ToolStripMenuItem menuItem = new(fieldInfo.Name);
                    if (fieldInfo.Name == Settings.Default.QuickConnectProtocol)
                    {
                        menuItem.Checked = true;
                        _cmbQuickConnect.ProtocolText = Settings.Default.QuickConnectProtocol;
                    }

                    _mnuQuickConnectProtocol.Items.Add(menuItem);
                }

                if (string.IsNullOrEmpty(_cmbQuickConnect.ProtocolText))
                    _cmbQuickConnect.ProtocolText = Settings.Default.QuickConnectProtocol;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace("PopulateQuickConnectProtocolMenu() failed.", ex);
            }
        }

        private void cmbQuickConnect_ConnectRequested(object sender, QuickConnectComboBox.ConnectRequestedEventArgs e)
        {
            btnQuickConnect_ButtonClick(sender, e);
        }

        private void btnQuickConnect_ButtonClick(object sender, EventArgs e)
        {
            try
            {
                ConnectionInfo connectionInfo = Runtime.ConnectionsService.CreateQuickConnect(_cmbQuickConnect.Text.Trim(),
                                                                                   Converter.StringToProtocol(Settings
                                                                                                              .Default
                                                                                                              .QuickConnectProtocol));
                if (connectionInfo == null)
                {
                    _cmbQuickConnect.Focus();
                    return;
                }

                _cmbQuickConnect.Add(connectionInfo);
                Runtime.ConnectionInitiator.OpenConnection(connectionInfo, ConnectionInfo.Force.DoNotJump);
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace("btnQuickConnect_ButtonClick() failed.", ex);
            }
        }

        private void cmbQuickConnect_ProtocolChanged(object sender, QuickConnectComboBox.ProtocolChangedEventArgs e)
        {
            SetQuickConnectProtocol(Converter.ProtocolToString(e.Protocol));
        }

        private void mnuQuickConnectProtocol_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            SetQuickConnectProtocol(e.ClickedItem.Text);
            if (string.IsNullOrEmpty(_cmbQuickConnect.Text))
                _cmbQuickConnect.Focus();
            else
                btnQuickConnect_ButtonClick(this, e);
        }

        private void SetQuickConnectProtocol(string protocol)
        {
            Settings.Default.QuickConnectProtocol = protocol;
            _cmbQuickConnect.ProtocolText = protocol;
            foreach (ToolStripMenuItem menuItem in _mnuQuickConnectProtocol.Items)
            {
                menuItem.Checked = menuItem.Text.Equals(protocol);
            }
        }

        #endregion

        #region Connections DropDown

        private void btnConnections_DropDownOpening(object sender, EventArgs e)
        {
            _btnConnections.DropDownItems.Clear();
            ConnectionsTreeToMenuItemsConverter menuItemsConverter = new()
            {
                MouseUpEventHandler = ConnectionsMenuItem_MouseUp
            };

            // ReSharper disable once CoVariantArrayConversion
            ToolStripItem[] rootMenuItems = menuItemsConverter
                                            .CreateToolStripDropDownItems(Runtime.ConnectionsService
                                                                                 .ConnectionTreeModel).ToArray();
            _btnConnections.DropDownItems.AddRange(rootMenuItems);

            ToolStripMenuItem favorites = new(Language.Favorites);
            ToolStripGlyphs.Set(favorites, FluentIcons.Favorite);
            List<ContainerInfo> rootNodes = Runtime.ConnectionsService.ConnectionTreeModel.RootNodes;
            List<ToolStripMenuItem> favoritesList = [];

            foreach (ContainerInfo node in rootNodes)
            {
                foreach (ConnectionInfo containerInfo in Runtime.ConnectionsService.ConnectionTreeModel.GetRecursiveFavoriteChildList(node))
                {
                    ToolStripMenuItem favoriteMenuItem = new()
                    {
                        Text = containerInfo.Name,
                        Tag = containerInfo
                    };
                    if (containerInfo.OpenConnections.Count > 0)
                        ToolStripGlyphs.Set(favoriteMenuItem, FluentIcons.Play, ToolStripGlyphs.GlyphRole.Success);
                    else
                        ToolStripGlyphs.Set(favoriteMenuItem, FluentIcons.Desktop, ToolStripGlyphs.GlyphRole.Secondary);
                    favoriteMenuItem.MouseUp += ConnectionsMenuItem_MouseUp;
                    favoritesList.Add(favoriteMenuItem);
                }
            }
            favorites.DropDownItems.AddRange(favoritesList.ToArray());
            _btnConnections.DropDownItems.Add(favorites);
        }

        private void ConnectionsMenuItem_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            ToolStripMenuItem menuItem = (ToolStripMenuItem)sender;

            switch (menuItem.Tag)
            {
                // While we can connect to a whole folder at once, it is
                // probably not the expected behavior when navigating through
                // a nested menu. Just return
                case ContainerInfo _:
                    return;
                case ConnectionInfo connectionInfo:
                    Runtime.ConnectionInitiator.OpenConnection(connectionInfo);
                    break;
            }
        }

        #endregion

        // CodeAyalysis doesn't like null propagation
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed",
            MessageId = "components")]
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!disposing) return;
                _themeManager.ThemeChanged -= ApplyTheme;
                components?.Dispose();
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
    }
}
