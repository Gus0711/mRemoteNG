#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.App;
using mRemoteNG.Resources.Language;
using mRemoteNG.Themes;
using mRemoteNG.UI.Forms;
using mRemoteNG.UI.Panels;
using mRemoteNG.UI.Window;
using WeifenLuo.WinFormsUI.Docking;

namespace mRemoteNG.UI.Modern
{
    /// <summary>An application command that can be run from the command palette or the home page.</summary>
    [SupportedOSPlatform("windows")]
    public sealed class AppCommand(string id, Func<string> title, char glyph, Action execute, string keywords = "", string[]? keys = null)
    {
        public string Id { get; } = id;
        public string Title => title();
        public char Glyph { get; } = glyph;

        /// <summary>Extra search terms (English and French) that are matched but not displayed.</summary>
        public string Keywords { get; } = keywords;

        /// <summary>Keyboard shortcut shown as key caps, e.g. ["Ctrl", "S"].</summary>
        public string[] Keys { get; } = keys ?? [];

        public void Execute()
        {
            try
            {
                execute();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace($"Command '{Id}' failed", ex);
            }
        }
    }

    /// <summary>The commands offered by the command palette and the home page.</summary>
    [SupportedOSPlatform("windows")]
    public static class AppCommands
    {
        internal const char GlyphNewFolder = '';
        internal const char GlyphBrightness = '';
        internal const char GlyphMoon = '';
        internal const char GlyphPanel = '';
        internal const char GlyphTree = '';
        internal const char GlyphBell = '';
        internal const char GlyphProperties = '';

        private static readonly Lazy<IReadOnlyList<AppCommand>> LazyAll = new(Create);

        public static IReadOnlyList<AppCommand> All => LazyAll.Value;

        private static DockPanel DockPanel => FrmMain.Default.pnlDock;

        private static IReadOnlyList<AppCommand> Create() =>
        [
            new("home", () => Language.HomeTitle, FluentIcons.Home, ModernExperience.ShowHome, "home start welcome accueil demarrage bienvenue"),
            new("newConnection", () => Language.NewConnection, FluentIcons.Add, NewConnection, "add create new connection ajouter creer nouvelle connexion"),
            new("newFolder", () => Language.NewFolder, GlyphNewFolder, NewFolder, "add create folder container ajouter creer dossier"),
            new("save", () => Language.SaveConnectionFile, FluentIcons.Save, () => Runtime.ConnectionsService.SaveConnectionsAsync(), "save write enregistrer sauvegarder", ["Ctrl", "S"]),
            new("theme", ThemeCommandTitle, GlyphBrightness, ToggleTheme, "theme dark light mode appearance theme sombre clair apparence"),
            new("options", () => Language.Options, FluentIcons.Settings, () => AppWindows.Show(WindowType.Options), "options settings preferences parametres reglages"),
            new("fullscreen", () => Language.Fullscreen, FluentIcons.FullScreen, ToggleFullscreen, "fullscreen full screen plein ecran", ["F11"]),
            new("showTree", () => string.Format(Language.PaletteShowPanel, Language.Connections), GlyphTree, () => ShowToolWindow(AppWindows.TreeForm), "tree panel connections sidebar arbre panneau"),
            new("showConfig", () => string.Format(Language.PaletteShowPanel, Language.Config), GlyphProperties, () => ShowToolWindow(AppWindows.ConfigForm), "config properties panel proprietes panneau"),
            new("showNotifications", () => string.Format(Language.PaletteShowPanel, Language.Notifications), GlyphBell, () => ShowToolWindow(AppWindows.ErrorsForm), "notifications errors messages log erreurs journal"),
            new("newPanel", () => Language.AddConnectionPanel, GlyphPanel, () => new PanelAdder().AddPanel(), "panel tab window panneau onglet fenetre"),
            new("about", () => Language.MenuItem_About, FluentIcons.Info, ShowAbout, "about version a propos")
        ];

        private static string ThemeCommandTitle() =>
            ThemeManager.getInstance().IsActiveThemeDark ? Language.PaletteSwitchToLight : Language.PaletteSwitchToDark;

        public static void ToggleTheme()
        {
            ThemeManager themeManager = ThemeManager.getInstance();
            string targetName = themeManager.IsActiveThemeDark ? ThemeManager.ModernLightThemeName : ThemeManager.ModernDarkThemeName;
            ThemeInfo? target = themeManager.getTheme(targetName);
            if (target == null) return;

            if (!themeManager.ThemingActive)
                themeManager.ThemingActive = true;
            Properties.OptionsThemePage.Default.ThemingActive = true;
            themeManager.ActiveTheme = target;
            Properties.OptionsThemePage.Default.Save();
        }

        public static void NewConnection()
        {
            ShowToolWindow(AppWindows.TreeForm);
            AppWindows.TreeForm.ConnectionTree?.AddConnection();
        }

        public static void NewFolder()
        {
            ShowToolWindow(AppWindows.TreeForm);
            AppWindows.TreeForm.ConnectionTree?.AddFolder();
        }

        private static void ToggleFullscreen()
        {
            FrmMain.Default.Fullscreen.Value = !FrmMain.Default.Fullscreen.Value;
        }

        private static void ShowToolWindow(DockContent window)
        {
            if (window.IsDisposed) return;
            if (window.DockPanel == null || window.IsHidden || window.DockState == DockState.Unknown)
                window.Show(DockPanel);
            window.Activate();
        }

        private static void ShowAbout()
        {
            if (frmAbout.Instance == null || frmAbout.Instance.IsDisposed)
                frmAbout.Instance = new frmAbout();
            frmAbout.Instance.Show(DockPanel);
        }
    }
}
