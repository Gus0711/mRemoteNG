#nullable enable
using System;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.App;
using mRemoteNG.Connection;
using mRemoteNG.Resources.Language;
using mRemoteNG.UI.Forms;
using mRemoteNG.UI.Window;
using WeifenLuo.WinFormsUI.Docking;

namespace mRemoteNG.UI.Modern
{
    /// <summary>
    /// Entry point of the modern UX features (home page and command palette): installs the
    /// keyboard shortcuts and the View menu entries, and opens the home page at startup.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class ModernExperience
    {
        private static FrmMain? _mainForm;
        private static HomeWindow? _home;
        private static ToolStripMenuItem? _homeMenuItem;
        private static ToolStripMenuItem? _paletteMenuItem;
        private static bool _installed;

        /// <summary>Call once from the main form's Load handler.</summary>
        public static void Install(FrmMain mainForm, ToolStripMenuItem viewMenu)
        {
            if (_installed) return;
            _installed = true;
            _mainForm = mainForm;

            Application.AddMessageFilter(new ShortcutFilter());
            AddMenuItems(viewMenu);
            ProgramRoot.UiCultureChanged += (_, _) => ApplyLanguage();

            if (RecentConnections.Instance.ShowHomeOnStartup)
            {
                // Deferred so that it comes after the startup panels created later in Load.
                mainForm.BeginInvoke(new MethodInvoker(() =>
                {
                    if (!mainForm.IsDisposed && !mainForm.IsClosing)
                        ShowHome();
                }));
            }
        }

        /// <summary>Shows (creating it if needed) and activates the home page document.</summary>
        public static void ShowHome()
        {
            FrmMain? mainForm = _mainForm ?? (FrmMain.IsCreated ? FrmMain.Default : null);
            if (mainForm == null) return;

            try
            {
                if (_home == null || _home.IsDisposed)
                    _home = new HomeWindow();

                if (_home.DockPanel == null || _home.IsHidden || _home.DockState == DockState.Unknown)
                    _home.Show(mainForm.pnlDock, DockState.Document);

                _home.Activate();
                _home.FocusQuickConnect();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace("Showing the home page failed", ex);
            }
        }

        /// <summary>Opens the command palette over the main window.</summary>
        public static void ShowPalette() => ShowPalette(string.Empty);

        /// <summary>Opens the command palette with an initial search text.</summary>
        public static void ShowPalette(string query)
        {
            FrmMain? mainForm = _mainForm ?? (FrmMain.IsCreated ? FrmMain.Default : null);
            if (mainForm == null || mainForm.IsDisposed) return;
            CommandPalette.ShowFor(mainForm, query);
        }

        private static void AddMenuItems(ToolStripMenuItem viewMenu)
        {
            _homeMenuItem = new ToolStripMenuItem
            {
                Name = "mMenViewHome",
                Image = FluentIcons.Get(FluentIcons.Home, 16)
            };
            _homeMenuItem.Click += (_, _) => ShowHome();

            _paletteMenuItem = new ToolStripMenuItem
            {
                Name = "mMenViewCommandPalette",
                Image = FluentIcons.Get(FluentIcons.Search, 16),
                ShortcutKeyDisplayString = "Ctrl+K"
            };
            // The shortcut itself is handled by ShortcutFilter so that it also works from tool windows.
            _paletteMenuItem.Click += (_, _) => ShowPalette();

            viewMenu.DropDownItems.Insert(0, _homeMenuItem);
            viewMenu.DropDownItems.Insert(1, _paletteMenuItem);
            viewMenu.DropDownItems.Insert(2, new ToolStripSeparator());
            viewMenu.DropDownOpening += (_, _) => RefreshMenuImages();
            ApplyLanguage();
        }

        private static void RefreshMenuImages()
        {
            // Follow the theme's text color (the glyph bitmaps are cached per color).
            if (_homeMenuItem != null) _homeMenuItem.Image = FluentIcons.Get(FluentIcons.Home, 16);
            if (_paletteMenuItem != null) _paletteMenuItem.Image = FluentIcons.Get(FluentIcons.Search, 16);
        }

        private static void ApplyLanguage()
        {
            if (_homeMenuItem != null) _homeMenuItem.Text = Language.HomeTitle;
            if (_paletteMenuItem != null) _paletteMenuItem.Text = Language.PaletteTitle + "…";
            _home?.ApplyLanguage();
        }

        /// <summary>
        /// Ctrl+K / Ctrl+Shift+P open the palette while the main window is active, except when the
        /// keyboard focus is inside a remote session (so the keys still reach the remote host).
        /// </summary>
        private sealed class ShortcutFilter : IMessageFilter
        {
            private const int WM_KEYDOWN = 0x0100;

            public bool PreFilterMessage(ref Message m)
            {
                if (m.Msg != WM_KEYDOWN) return false;

                Keys key = (Keys)(int)m.WParam & Keys.KeyCode;
                Keys modifiers = Control.ModifierKeys;
                bool palette = (key == Keys.K && modifiers == Keys.Control) ||
                               (key == Keys.P && modifiers == (Keys.Control | Keys.Shift));
                if (!palette) return false;

                FrmMain? mainForm = _mainForm;
                if (mainForm == null || mainForm.IsDisposed || Form.ActiveForm != mainForm) return false;
                if (IsInsideRemoteSession(m.HWnd)) return false;

                mainForm.BeginInvoke(new MethodInvoker(ShowPalette));
                return true;
            }

            private static bool IsInsideRemoteSession(IntPtr handle)
            {
                for (Control? control = Control.FromChildHandle(handle); control != null; control = control.Parent)
                {
                    if (control is InterfaceControl) return true;
                }

                return false;
            }
        }
    }
}
