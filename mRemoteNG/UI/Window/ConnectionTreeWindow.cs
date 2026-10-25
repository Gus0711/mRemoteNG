using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using mRemoteNG.App;
using mRemoteNG.Config.Connections;
using mRemoteNG.Connection;
using mRemoteNG.Container;
using mRemoteNG.Properties;
using mRemoteNG.Themes;
using mRemoteNG.Tree;
using mRemoteNG.Tree.ClickHandlers;
using mRemoteNG.Tree.Root;
using mRemoteNG.UI.Controls;
using mRemoteNG.UI.Controls.ConnectionTree;
using mRemoteNG.UI.GraphicsUtilities;
using mRemoteNG.UI.TaskDialog;
using WeifenLuo.WinFormsUI.Docking;
using mRemoteNG.Resources.Language;
using System.Runtime.Versioning;

// ReSharper disable ArrangeAccessorOwnerBody

namespace mRemoteNG.UI.Window
{
    [SupportedOSPlatform("windows")]
    public partial class ConnectionTreeWindow
    {
        private ThemeManager _themeManager;
        private bool _sortedAz = true;

        public ConnectionInfo SelectedNode => ConnectionTree.SelectedNode;

        public ConnectionTree ConnectionTree { get; set; }

        public ConnectionTreeWindow() : this(new DockContent())
        {
        }

        public ConnectionTreeWindow(DockContent panel)
        {
            WindowType = WindowType.Tree;
            DockPnl = panel;
            Icon = Resources.ImageConverter.GetImageAsIcon(Properties.Resources.ASPWebSite_16x);
            InitializeComponent();
            ApplyModernFonts();
            msMain.DpiChangedAfterParent += (_, _) => ApplyToolbarIcons();
            // the designer docks the search bar at the bottom; apply the saved preference at startup
            PlaceSearchBar(Settings.Default.PlaceSearchBarAboveConnectionTree);
            SetMenuEventHandlers();
            SetConnectionTreeEventHandlers();
            Settings.Default.PropertyChanged += OnAppSettingsChanged;
            ApplyLanguage();
        }

        private void OnAppSettingsChanged(object o, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (propertyChangedEventArgs.PropertyName == nameof(Settings.UseFilterSearch))
            {
                ConnectionTree.UseFiltering = Settings.Default.UseFilterSearch;
                ApplyFiltering();
            }

            if (propertyChangedEventArgs.PropertyName == nameof(Settings.SlowClickRenameEnabled))
                ConnectionTree.SetupSlowClickRename();

            PlaceSearchBar(Settings.Default.PlaceSearchBarAboveConnectionTree);
            SetConnectionTreeClickHandlers();
        }

        private void PlaceSearchBar(bool placeSearchBarAboveConnectionTree)
        {
            searchBoxLayoutPanel.Dock = placeSearchBarAboveConnectionTree ? DockStyle.Top : DockStyle.Bottom;
        }

        private void ApplyModernFonts()
        {
            ConnectionTree.Font = SidebarFonts.Regular;
            txtSearch.Font = SidebarFonts.Regular;
        }

        /// <summary>
        /// Icon-only header buttons drawn with Fluent glyphs in the secondary text color,
        /// so they follow the theme and stay crisp at any DPI.
        /// </summary>
        private void ApplyToolbarIcons()
        {
            Color color = _themeManager is { ThemingActive: true } ? ModernPalette.Current.TextSecondary : SystemColors.ControlText;
            SetToolbarIcon(mMenAddConnection, FluentIcons.Add, color);
            SetToolbarIcon(mMenAddFolder, ConnectionTreeIcons.NewFolder, color);
            SetToolbarIcon(mMenAddRoot, FluentIcons.Network, color);
            SetToolbarIcon(mMenViewExpandAllFolders, ConnectionTreeIcons.ExpandAllBox, color);
            SetToolbarIcon(mMenViewCollapseAllFolders, ConnectionTreeIcons.CollapseAllBox, color);
            SetToolbarIcon(mMenSort, FluentIcons.Sort, color);
            SetToolbarIcon(mMenFavorites, FluentIcons.Favorite, color);
        }

        private void SetToolbarIcon(ToolStripMenuItem item, char glyph, Color color)
        {
            int size = (int)Math.Round(16 * msMain.DeviceDpi / 96f);
            int box = (int)Math.Round(28 * msMain.DeviceDpi / 96f);
            item.Image = FluentIcons.Get(glyph, size, color);
            item.ImageScaling = ToolStripItemImageScaling.None;
            item.DisplayStyle = ToolStripItemDisplayStyle.Image;
            item.AutoSize = false;
            item.Padding = Padding.Empty;
            item.Margin = new Padding(0, 0, (int)Math.Round(2 * msMain.DeviceDpi / 96f), 0);
            item.Size = new Size(box, box);
        }


        #region Form Stuff

        private void Tree_Load(object sender, EventArgs e)
        {
            //work on the theme change
            _themeManager = ThemeManager.getInstance();
            _themeManager.ThemeChanged += ApplyTheme;
            ApplyTheme();

        }

        private void ApplyLanguage()
        {
            Text = Language.Connections;
            TabText = Language.Connections;

            mMenAddConnection.ToolTipText = Language.NewConnection;
            mMenAddFolder.ToolTipText = Language.NewFolder;
            mMenAddRoot.ToolTipText = Language.AddRoot;
            mMenViewExpandAllFolders.ToolTipText = Language.ExpandAllFolders;
            mMenViewCollapseAllFolders.ToolTipText = Language.CollapseAllFolders;
            mMenSort.ToolTipText = Language.Sort;
            mMenFavorites.ToolTipText = Language.Favorites;

            searchBoxLayoutPanel.ApplyLanguage();
        }

        internal void RefreshUiLanguage()
        {
            if (IsDisposed)
                return;

            ApplyLanguage();
            if (ConnectionTree.ContextMenuStrip is ConnectionContextMenu connectionContextMenu)
            {
                connectionContextMenu.RefreshUiLanguage();
            }
        }

        private new void ApplyTheme()
        {
            ApplyToolbarIcons();
            searchBoxLayoutPanel.ApplyTheme();

            if (!_themeManager.ThemingActive)
                return;

            ThemeInfo activeTheme = _themeManager.ActiveTheme;
            vsToolStripExtender.SetStyle(msMain, activeTheme.Version, activeTheme.Theme);
            vsToolStripExtender.SetStyle(mMenFavorites.DropDown, activeTheme.Version, activeTheme.Theme);
            vsToolStripExtender.SetStyle(ConnectionTree.ContextMenuStrip, activeTheme.Version,
                activeTheme.Theme);

            if (!_themeManager.ActiveAndExtended)
                return;

            // flat header strip on the panel background (the favorites drop-down keeps the theme renderer)
            Color panelBackground = activeTheme.ExtendedPalette.getColor("TreeView_Background");
            msMain.Renderer = new SidebarToolStripRenderer(panelBackground);
            msMain.BackColor = panelBackground;
            BackColor = panelBackground;
        }

        #endregion

        #region ConnectionTree

        private void SetConnectionTreeEventHandlers()
        {
            ConnectionTree.NodeDeletionConfirmer =
                new SelectedConnectionDeletionConfirmer(prompt => CTaskDialog.MessageBox(
                    Application.ProductName, prompt, "", ETaskDialogButtons.YesNo, ESysIcons.Question));
            ConnectionTree.KeyDown += TvConnections_KeyDown;
            ConnectionTree.KeyPress += TvConnections_KeyPress;
            SetTreePostSetupActions();
            SetConnectionTreeClickHandlers();
            Runtime.ConnectionsService.ConnectionsLoaded += ConnectionsServiceOnConnectionsLoaded;
        }

        private void SetTreePostSetupActions()
        {
            List<IConnectionTreeDelegate> actions = new()
            {
                new PreviouslyOpenedFolderExpander(),
                new RootNodeExpander()
            };

            if (Properties.OptionsStartupExitPage.Default.OpenConsFromLastSession && !Properties.OptionsAdvancedPage.Default.NoReconnect)
                actions.Add(new PreviousSessionOpener(Runtime.ConnectionInitiator));

            ConnectionTree.PostSetupActions = actions;
        }

        private void SetConnectionTreeClickHandlers()
        {
            List<ITreeNodeClickHandler<ConnectionInfo>> singleClickHandlers = new();
            List<ITreeNodeClickHandler<ConnectionInfo>> doubleClickHandlers = new()
            {
                new ExpandNodeClickHandler(ConnectionTree)
            };

            if (Settings.Default.SingleClickOnConnectionOpensIt)
                singleClickHandlers.Add(new OpenConnectionClickHandler(Runtime.ConnectionInitiator));
            else
                doubleClickHandlers.Add(new OpenConnectionClickHandler(Runtime.ConnectionInitiator));

            if (Settings.Default.SingleClickSwitchesToOpenConnection)
                singleClickHandlers.Add(new SwitchToConnectionClickHandler(Runtime.ConnectionInitiator));

            ConnectionTree.SingleClickHandler = new TreeNodeCompositeClickHandler { ClickHandlers = singleClickHandlers };
            ConnectionTree.DoubleClickHandler = new TreeNodeCompositeClickHandler { ClickHandlers = doubleClickHandlers };
        }

        private void ConnectionsServiceOnConnectionsLoaded(object o, ConnectionsLoadedEventArgs connectionsLoadedEventArgs)
        {
            if (ConnectionTree.InvokeRequired)
            {
                ConnectionTree.Invoke(() => ConnectionsServiceOnConnectionsLoaded(o, connectionsLoadedEventArgs));
                return;
            }

            ConnectionTree.ConnectionTreeModel = connectionsLoadedEventArgs.NewConnectionTreeModel;
            ConnectionTree.SelectedObject = connectionsLoadedEventArgs.NewConnectionTreeModel.RootNodes.FirstOrDefault();
        }

        #endregion

        #region Top Menu

        private void SetMenuEventHandlers()
        {
            mMenViewExpandAllFolders.Click += (sender, args) => ConnectionTree.ExpandAll();
            mMenViewCollapseAllFolders.Click += (sender, args) =>
            {
                ConnectionTree.CollapseAll();
                RootNodeInfo rootNode = ConnectionTree.ConnectionTreeModel?.RootNodes.OfType<RootNodeInfo>().FirstOrDefault();
                if (rootNode != null)
                    ConnectionTree.Expand(rootNode);
            };
            mMenSort.Click += (sender, args) =>
            {
                if (_sortedAz)
                {
                    ConnectionTree.SortRecursive(ConnectionTree.GetRootConnectionNode(), ListSortDirection.Ascending);
                    _sortedAz = false;
                }
                else
                {
                    ConnectionTree.SortRecursive(ConnectionTree.GetRootConnectionNode(), ListSortDirection.Descending);
                    _sortedAz = true;
                }
            };
            mMenFavorites.Click += (sender, args) =>
            {
                mMenFavorites.DropDownItems.Clear();
                List<ContainerInfo> rootNodes = Runtime.ConnectionsService.ConnectionTreeModel.RootNodes;
                List<ToolStripMenuItem> favoritesList = new();

                foreach (ContainerInfo node in rootNodes)
                {
                    foreach (ConnectionInfo containerInfo in Runtime.ConnectionsService.ConnectionTreeModel.GetRecursiveFavoriteChildList(node))
                    {
                        ToolStripMenuItem favoriteMenuItem = new()
                        {
                            Text = containerInfo.Name,
                            Tag = containerInfo,
                            Image = containerInfo.OpenConnections.Count > 0 ? Properties.Resources.Run_16x : Properties.Resources.Stop_16x
                        };
                        favoriteMenuItem.MouseUp += FavoriteMenuItem_MouseUp;
                        favoritesList.Add(favoriteMenuItem);
                    }
                }

                mMenFavorites.DropDownItems.AddRange(favoritesList.ToArray());
                mMenFavorites.ShowDropDown();
            };
        }

        private void FavoriteMenuItem_MouseUp(object sender, MouseEventArgs e)
        {
            if (((ToolStripMenuItem)sender).Tag is ContainerInfo) return;
            Runtime.ConnectionInitiator.OpenConnection((ConnectionInfo)((ToolStripMenuItem)sender).Tag);
        }

        #endregion

        #region Tree Context Menu

        private void CMenTreeAddConnection_Click(object sender, EventArgs e)
        {
            ConnectionTree.AddConnection();
        }

        private void CMenTreeAddFolder_Click(object sender, EventArgs e)
        {
            ConnectionTree.AddFolder();
        }

        private void CMenTreeAddRoot_Click(object sender, EventArgs e)
        {
            ConnectionTree.AddRoot();
        }

        #endregion

        #region Search

        private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                switch (e.KeyCode)
                {
                    case Keys.Escape:
                        e.Handled = true;
                        ConnectionTree.Focus();
                        break;
                    case Keys.Up:
                        {
                            ConnectionInfo? match = ConnectionTree.NodeSearcher.PreviousMatch();
                            JumpToNode(match);
                            e.Handled = true;
                            break;
                        }
                    case Keys.Down:
                        {
                            ConnectionInfo? match = ConnectionTree.NodeSearcher.NextMatch();
                            JumpToNode(match);
                            e.Handled = true;
                            break;
                        }
                    default:
                        TvConnections_KeyDown(sender, e);
                        break;
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace("txtSearch_KeyDown (UI.Window.ConnectionTreeWindow) failed", ex);
            }
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            ApplyFiltering();
        }

        private void ApplyFiltering()
        {
            if (Settings.Default.UseFilterSearch)
            {
                if (txtSearch.Text == "" || txtSearch.Text == Language.SearchPrompt)
                {
                    ConnectionTree.RemoveFilter();
                    return;
                }

                ConnectionTree.ApplyFilter(txtSearch.Text);
            }
            else
            {
                if (txtSearch.Text == "") return;
                ConnectionTree.NodeSearcher?.SearchByName(txtSearch.Text);
                JumpToNode(ConnectionTree.NodeSearcher?.CurrentMatch);
            }
        }

        public void JumpToNode(ConnectionInfo? connectionInfo)
        {
            if (connectionInfo == null)
            {
                ConnectionTree.SelectedObject = null;
                return;
            }

            ExpandParentsRecursive(connectionInfo);
            ConnectionTree.SelectObject(connectionInfo);
            ConnectionTree.EnsureModelVisible(connectionInfo);
        }

        private void ExpandParentsRecursive(ConnectionInfo connectionInfo)
        {
            while (true)
            {
                if (connectionInfo?.Parent == null) return;
                ConnectionTree.Expand(connectionInfo.Parent);
                connectionInfo = connectionInfo.Parent;
            }
        }

        private void TvConnections_KeyPress(object sender, KeyPressEventArgs e)
        {
            try
            {
                if (!char.IsLetterOrDigit(e.KeyChar)) return;
                txtSearch.Focus();
                txtSearch.Text = e.KeyChar.ToString();
                txtSearch.SelectionStart = txtSearch.TextLength;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace("tvConnections_KeyPress (UI.Window.ConnectionTreeWindow) failed", ex);
            }
        }

        private void TvConnections_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    if (SelectedNode == null)
                        return;
                    Runtime.ConnectionInitiator.OpenConnection(SelectedNode);
                }
                else if (e.Control && e.KeyCode == Keys.F)
                {
                    txtSearch.Focus();
                    txtSearch.SelectAll();
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace("tvConnections_KeyDown (UI.Window.ConnectionTreeWindow) failed", ex);
            }
        }

        #endregion
    }
}
