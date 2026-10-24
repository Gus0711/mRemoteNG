#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.App;
using mRemoteNG.Config.Connections;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Resources.Language;
using mRemoteNG.Themes;
using mRemoteNG.UI.GraphicsUtilities;
using mRemoteNG.UI.Modern;
using WeifenLuo.WinFormsUI.Docking;

namespace mRemoteNG.UI.Window
{
    /// <summary>
    /// Home / welcome document: greeting, quick connect field, statistics, favorite and recent
    /// connection cards and quick actions. Everything is custom painted with the modern palette.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public sealed class HomeWindow : BaseWindow
    {
        private readonly HomeView _view;

        public HomeWindow()
        {
            DockAreas = DockAreas.Document | DockAreas.Float;
            HideOnClose = false;
            ShowHint = DockState.Document;
            _view = new HomeView { Dock = DockStyle.Fill };
            Controls.Add(_view);
            Icon = Resources.ImageConverter.GetImageAsIcon(FluentIcons.Get(FluentIcons.Home, 16, ModernPalette.Current.Accent));
            ApplyLanguage();
        }

        internal void ApplyLanguage()
        {
            SetFormText(Language.HomeTitle);
            _view.Invalidate();
        }

        internal void FocusQuickConnect() => _view.FocusQuickConnect();

        // Not restored from the layout file (the home page is opened by ModernExperience instead).
        protected override string GetPersistString() => string.Empty;
    }

    /// <summary>The scrollable, custom-painted content of <see cref="HomeWindow"/>.</summary>
    [SupportedOSPlatform("windows")]
    internal sealed class HomeView : ScrollableControl
    {
        private const int MaxContentWidth = 1040;
        private const int MaxRecentCards = 12;
        private const int MaxFavoriteCards = 12;

        private readonly QuickConnectField _field;
        private readonly Timer _refreshTimer;
        private readonly ToolTip _toolTip = new();
        private readonly List<HitRegion> _regions = [];
        private readonly Bitmap? _logo;

        private float _scale = 1f;
        private Fonts? _fonts;
        private Snapshot _snapshot = Snapshot.Empty;
        private string? _hoverId;
        private string? _pressedId;
        private string? _toolTipId;
        private int _contentHeight;

        public HomeView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            AutoScroll = true;

            _field = new QuickConnectField();
            _field.BackdropColorAt = y => BackdropColorAt(_field.Top + y);
            _field.Submitted += (_, text) => SubmitQuickConnect(text);
            Controls.Add(_field);

            try
            {
                using Icon icon = new(Properties.Resources.mRemoteNG_Icon, 256, 256);
                _logo = icon.ToBitmap();
            }
            catch (Exception)
            {
                _logo = null;
            }

            _refreshTimer = new Timer { Interval = 3000 };
            _refreshTimer.Tick += (_, _) => RefreshData();

            RecentConnections.Instance.Changed += OnDataChanged;
            Runtime.ConnectionsService.ConnectionsLoaded += OnConnectionsLoaded;
            ThemeManager.getInstance().ThemeChanged += OnThemeChanged;

            ApplyTheme();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UpdateScale();
            RefreshData(force: true);
            _refreshTimer.Start();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible) RefreshData(force: true);
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            UpdateScale();
            PerformHomeLayout();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                RecentConnections.Instance.Changed -= OnDataChanged;
                Runtime.ConnectionsService.ConnectionsLoaded -= OnConnectionsLoaded;
                ThemeManager.getInstance().ThemeChanged -= OnThemeChanged;
                _refreshTimer.Dispose();
                _toolTip.Dispose();
                _fonts?.Dispose();
                _logo?.Dispose();
            }

            base.Dispose(disposing);
        }

        public void FocusQuickConnect()
        {
            if (_field.CanFocus) _field.FocusInput();
        }

        #region Data

        private void OnDataChanged(object? sender, EventArgs e) => RunOnUi(() => RefreshData(force: true));

        private void OnConnectionsLoaded(object? sender, ConnectionsLoadedEventArgs e) => RunOnUi(() => RefreshData(force: true));

        private void OnThemeChanged() => RunOnUi(() =>
        {
            ApplyTheme();
            PerformHomeLayout();
        });

        private void RunOnUi(Action action)
        {
            if (IsDisposed || !IsHandleCreated) return;
            if (InvokeRequired) BeginInvoke(action);
            else action();
        }

        private void RefreshData(bool force = false)
        {
            if (IsDisposed || (!force && !Visible)) return;

            Snapshot snapshot = Snapshot.Capture();
            if (!force && snapshot.Signature == _snapshot.Signature) return;
            _snapshot = snapshot;
            PerformHomeLayout();
        }

        private sealed class Snapshot
        {
            public static readonly Snapshot Empty = new();

            public int ConnectionCount { get; private init; }
            public int FolderCount { get; private init; }
            public int OpenCount { get; private init; }
            public List<CardData> Favorites { get; private init; } = [];
            public List<CardData> Recents { get; private init; } = [];

            /// <summary>A few connections from the tree, shown while there is little history.</summary>
            public List<CardData> Suggestions { get; private init; } = [];

            public string Signature { get; private init; } = string.Empty;

            public static Snapshot Capture()
            {
                IReadOnlyList<ConnectionInfo> connections = ConnectionCatalog.GetConnections();
                List<(RecentConnectionEntry Entry, ConnectionInfo? Connection)> recents = RecentConnections.Instance.Resolve().ToList();
                Dictionary<string, RecentConnectionEntry> recentById = recents.Where(r => r.Connection != null)
                                                                              .GroupBy(r => r.Connection!.ConstantID)
                                                                              .ToDictionary(g => g.Key, g => g.First().Entry);

                List<CardData> favorites = connections.Where(c => c.Favorite)
                                                      .OrderBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase)
                                                      .Take(MaxFavoriteCards)
                                                      .Select(c => CardData.From(c, recentById.GetValueOrDefault(c.ConstantID), isFavoriteRow: true))
                                                      .ToList();

                List<CardData> recentCards = recents.Take(MaxRecentCards)
                                                    .Select(r => r.Connection != null
                                                                ? CardData.From(r.Connection, r.Entry, isFavoriteRow: false)
                                                                : CardData.FromQuickConnect(r.Entry))
                                                    .ToList();

                List<CardData> suggestions = [];
                if (recentCards.Count < 4)
                {
                    HashSet<string> shown = recents.Where(r => r.Connection != null).Select(r => r.Connection!.ConstantID).ToHashSet();
                    suggestions = connections.Where(c => !shown.Contains(c.ConstantID) && !c.Favorite)
                                             .OrderBy(c => ConnectionCatalog.GetFolderPath(c), StringComparer.CurrentCultureIgnoreCase)
                                             .ThenBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase)
                                             .Take(8)
                                             .Select(c => CardData.From(c, null, isFavoriteRow: false))
                                             .ToList();
                }

                int open = connections.Count(c => c.OpenConnections is { Count: > 0 });
                int folders = ConnectionCatalog.CountFolders();

                string signature = string.Join("|", new[]
                {
                    connections.Count.ToString(CultureInfo.InvariantCulture),
                    folders.ToString(CultureInfo.InvariantCulture),
                    open.ToString(CultureInfo.InvariantCulture),
                    string.Join(",", favorites.Select(f => f.Signature)),
                    string.Join(",", recentCards.Select(r => r.Signature)),
                    string.Join(",", suggestions.Select(r => r.Signature))
                });

                return new Snapshot
                {
                    ConnectionCount = connections.Count,
                    FolderCount = folders,
                    OpenCount = open,
                    Favorites = favorites,
                    Recents = recentCards,
                    Suggestions = suggestions,
                    Signature = signature
                };
            }
        }

        private sealed class CardData
        {
            public string Id { get; private init; } = string.Empty;
            public string Name { get; private init; } = string.Empty;
            public string Host { get; private init; } = string.Empty;
            public string Folder { get; private init; } = string.Empty;
            public ProtocolType Protocol { get; private init; }
            public string When { get; private init; } = string.Empty;
            public bool IsOpen { get; private init; }
            public bool IsFavorite { get; private init; }
            public bool IsFavoriteRow { get; private init; }
            public ConnectionInfo? Connection { get; private init; }
            public RecentConnectionEntry? Entry { get; private init; }

            public string Signature => $"{Id}:{Name}:{Host}:{Protocol}:{When}:{IsOpen}:{IsFavorite}";

            public static CardData From(ConnectionInfo connection, RecentConnectionEntry? entry, bool isFavoriteRow) => new()
            {
                Id = (isFavoriteRow ? "fav:" : "recent:") + connection.ConstantID,
                Name = connection.Name ?? string.Empty,
                Host = connection.Hostname ?? string.Empty,
                Folder = ConnectionCatalog.GetFolderPath(connection),
                Protocol = connection.Protocol,
                When = entry != null ? RelativeTime.Format(entry.LastUsedUtc) : string.Empty,
                IsOpen = connection.OpenConnections is { Count: > 0 },
                IsFavorite = connection.Favorite,
                IsFavoriteRow = isFavoriteRow,
                Connection = connection,
                Entry = entry
            };

            public static CardData FromQuickConnect(RecentConnectionEntry entry) => new()
            {
                Id = $"quick:{entry.Protocol}:{entry.Hostname}:{entry.Port}",
                Name = entry.Name,
                Host = entry.Port > 0 ? $"{entry.Hostname}:{entry.Port}" : entry.Hostname,
                Folder = Language.QuickConnect,
                Protocol = entry.ProtocolType,
                When = RelativeTime.Format(entry.LastUsedUtc),
                Entry = entry
            };

            public void Open()
            {
                if (Entry != null)
                    RecentConnections.Open(Entry, Connection);
                else if (Connection != null)
                    ConnectionCatalog.Open(Connection);
            }
        }

        #endregion

        #region Actions

        private void SubmitQuickConnect(string text)
        {
            text = text.Trim();
            if (text.Length == 0)
            {
                ModernExperience.ShowPalette();
                return;
            }

            ConnectionInfo? exact = ConnectionCatalog.GetConnections()
                                                     .FirstOrDefault(c => string.Equals(c.Name, text, StringComparison.CurrentCultureIgnoreCase));
            if (exact != null)
            {
                ConnectionCatalog.Open(exact);
                _field.Clear();
                return;
            }

            if (ConnectionCatalog.TryParseQuickConnect(text, ConnectionCatalog.DefaultQuickConnectProtocol, out string target, out ProtocolType protocol, out _))
            {
                ConnectionCatalog.QuickConnect(target, protocol);
                _field.Clear();
                return;
            }

            // Not a host: let the palette search connections and commands for it.
            ModernExperience.ShowPalette(text);
        }

        private static void ToggleFavorite(ConnectionInfo connection)
        {
            connection.Favorite = !connection.Favorite;
        }

        #endregion

        #region Layout

        private int S(float logical) => (int)Math.Round(logical * _scale);

        private void UpdateScale()
        {
            _scale = DeviceDpi / 96f;
            _fonts?.Dispose();
            _fonts = new Fonts(_scale);
            _field.SetScale(_scale);
        }

        private void ApplyTheme()
        {
            ModernPalette palette = ModernPalette.Current;
            BackColor = palette.Content;
            _field.ApplyTheme();
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            PerformHomeLayout();
        }

        private Rectangle ContentBounds()
        {
            int side = S(ClientSize.Width < S(700) ? 24 : 48);
            int width = Math.Min(S(MaxContentWidth), Math.Max(S(280), ClientSize.Width - side * 2));
            int left = Math.Max(side, (ClientSize.Width - width) / 2);
            return new Rectangle(left, S(40), width, 0);
        }

        /// <summary>Computes the content height and positions the quick connect field.</summary>
        private void PerformHomeLayout()
        {
            if (_fonts == null || IsDisposed) return;

            using Graphics g = CreateGraphics();
            _regions.Clear();
            int height = Paint(g, layoutOnly: true);
            _contentHeight = height;

            Size min = new(0, height);
            if (AutoScrollMinSize != min) AutoScrollMinSize = min;
            Invalidate();
        }

        #endregion

        #region Painting

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_fonts == null) return;
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            _regions.Clear();
            Paint(g, layoutOnly: false);
        }

        /// <summary>
        /// Lays out and (unless <paramref name="layoutOnly"/>) paints the page in one pass so that the
        /// painted elements and the hit regions can never disagree. Returns the content height.
        /// </summary>
        private int Paint(Graphics g, bool layoutOnly)
        {
            Fonts fonts = _fonts!;
            ModernPalette palette = ModernPalette.Current;
            Point scroll = AutoScrollPosition;
            Rectangle content = ContentBounds();
            int x = content.X;
            int width = content.Width;
            int y = content.Y;

            GraphicsState? state = null;
            if (!layoutOnly)
            {
                g.Clear(palette.Content);
                PaintBackdrop(g, palette, scroll);
                state = g.Save();
                g.TranslateTransform(scroll.X, scroll.Y);
            }

            // Hero: logo, greeting, subtitle, date.
            int logoSize = S(60);
            if (!layoutOnly)
            {
                Rectangle logoTile = new(x, y, logoSize, logoSize);
                ModernGraphics.FillRoundedRectangle(g, palette.Surface, logoTile, S(14));
                ModernGraphics.DrawRoundedRectangle(g, palette.BorderSubtle, logoTile, S(14));
                if (_logo != null)
                    g.DrawImage(_logo, Rectangle.Inflate(logoTile, -S(10), -S(10)));

                int textX = logoTile.Right + S(18);
                string greeting = Greeting();
                ModernText.Draw(g, greeting, fonts.Greeting, new Rectangle(textX, y - S(2), width - logoSize - S(18), S(38)), palette.Text);
                ModernText.Draw(g, Language.HomeSubtitle, fonts.Subtitle, new Rectangle(textX, y + S(36), width - logoSize - S(18), S(24)), palette.TextSecondary);

                string date = DateTime.Now.ToString("dddd d MMMM", CultureInfo.CurrentUICulture);
                if (date.Length > 0) date = char.ToUpper(date[0], CultureInfo.CurrentUICulture) + date[1..];
                Size dateSize = ModernText.Measure(g, date, fonts.Small);
                if (width > S(620))
                    ModernText.Draw(g, date, fonts.Small, new Rectangle(x + width - dateSize.Width - S(2), y + S(4), dateSize.Width + S(4), S(20)), palette.TextSecondary);
            }

            y += logoSize + S(28);

            // Quick connect field (a real child control, positioned in client coordinates).
            int fieldHeight = S(54);
            Rectangle fieldBounds = new(x + scroll.X, y + scroll.Y, width, fieldHeight);
            if (layoutOnly && _field.Bounds != fieldBounds) _field.Bounds = fieldBounds;
            y += fieldHeight + S(12);

            // Ctrl+K hint
            {
                int capHeight = S(22);
                if (!layoutOnly)
                {
                    int capsWidth = ModernText.DrawKeyCaps(g, ["Ctrl", "K"], fonts.Key, new Point(x + S(2), y), capHeight, _scale);
                    string hint = Language.HomePaletteHint;
                    ModernText.Draw(g, hint, fonts.Small, new Rectangle(x + capsWidth + S(12), y, width - capsWidth - S(12), capHeight), palette.TextSecondary);
                }

                AddRegion("hint:palette", new Rectangle(x, y, S(90), capHeight), ModernExperience.ShowPalette, Language.PaletteTitle);
                y += capHeight + S(30);
            }

            // Statistics tiles
            y = PaintStats(g, layoutOnly, palette, fonts, x, y, width) + S(36);

            // Favorites
            if (_snapshot.Favorites.Count > 0)
            {
                y = PaintSectionHeader(g, layoutOnly, palette, fonts, Language.Favorites, null, null, x, y, width);
                y = PaintCards(g, layoutOnly, palette, fonts, _snapshot.Favorites, x, y, width) + S(32);
            }

            // Recents
            y = PaintSectionHeader(g, layoutOnly, palette, fonts, Language.PaletteGroupRecent,
                                   _snapshot.Recents.Count > 0 ? Language.HomeClearRecent : null,
                                   () => RecentConnections.Instance.Clear(), x, y, width);
            if (_snapshot.Recents.Count > 0)
            {
                y = PaintCards(g, layoutOnly, palette, fonts, _snapshot.Recents, x, y, width) + S(32);
            }
            else
            {
                Rectangle empty = new(x, y, width, S(64));
                if (!layoutOnly)
                {
                    using (Pen dashed = new(palette.Border, Math.Max(1, S(1))) { DashStyle = DashStyle.Dash })
                    using (GraphicsPath path = ModernGraphics.CreateRoundedRectangle(Rectangle.Inflate(empty, -1, -1), S(ModernPalette.LargeCornerRadius)))
                        g.DrawPath(dashed, path);
                    FluentIcons.Draw(g, FluentIcons.History, new Rectangle(empty.X + S(24), empty.Y + (empty.Height - S(24)) / 2, S(24), S(24)), palette.TextDisabled, S(20));
                    ModernText.Draw(g, Language.HomeRecentEmpty, fonts.Body, new Rectangle(empty.X + S(64), empty.Y, empty.Width - S(80), empty.Height), palette.TextSecondary);
                }

                y = empty.Bottom + S(32);
            }

            // Suggestions while the history is short
            if (_snapshot.Suggestions.Count > 0)
            {
                y = PaintSectionHeader(g, layoutOnly, palette, fonts, Language.Connections, null, null, x, y, width);
                y = PaintCards(g, layoutOnly, palette, fonts, _snapshot.Suggestions, x, y, width) + S(32);
            }

            // Quick actions
            y = PaintSectionHeader(g, layoutOnly, palette, fonts, Language.HomeQuickActions, null, null, x, y, width);
            y = PaintActions(g, layoutOnly, palette, fonts, x, y, width) + S(36);

            // Show at startup toggle
            {
                bool on = RecentConnections.Instance.ShowHomeOnStartup;
                Size labelSize = ModernText.Measure(g, Language.HomeShowOnStartup, fonts.Body);
                Rectangle toggle = new(x, y, S(40) + S(12) + labelSize.Width, S(22));
                if (!layoutOnly)
                {
                    Rectangle track = new(x, y + S(1), S(40), S(20));
                    bool hover = _hoverId == "toggle:startup";
                    Color trackColor = on ? (hover ? palette.AccentHover : palette.Accent) : (hover ? palette.SurfaceHover : palette.Surface);
                    ModernGraphics.FillRoundedRectangle(g, trackColor, track, track.Height / 2f);
                    if (!on) ModernGraphics.DrawRoundedRectangle(g, palette.TextSecondary, track, track.Height / 2f);
                    int knob = S(12);
                    Rectangle knobBounds = new(on ? track.Right - knob - S(4) : track.X + S(4), track.Y + (track.Height - knob) / 2, knob, knob);
                    using (SolidBrush knobBrush = new(on ? palette.AccentText : palette.TextSecondary))
                        g.FillEllipse(knobBrush, knobBounds);
                    ModernText.Draw(g, Language.HomeShowOnStartup, fonts.Body, new Rectangle(track.Right + S(12), y, labelSize.Width + S(4), S(22)), palette.Text);
                }

                AddRegion("toggle:startup", toggle, () =>
                {
                    RecentConnections.Instance.ShowHomeOnStartup = !RecentConnections.Instance.ShowHomeOnStartup;
                    Invalidate();
                });
                y = toggle.Bottom + S(40);
            }

            if (state != null) g.Restore(state);
            return y;
        }

        private int BackdropHeight => S(300);

        private static Color BackdropTop(ModernPalette palette) =>
            ModernGraphics.Blend(palette.Content, palette.Accent, palette.IsDark ? 0.13f : 0.07f);

        /// <summary>Color of the backdrop gradient at the given client y coordinate.</summary>
        internal Color BackdropColorAt(int clientY)
        {
            ModernPalette palette = ModernPalette.Current;
            float t = (clientY - AutoScrollPosition.Y) / (float)Math.Max(1, BackdropHeight);
            return ModernGraphics.Blend(BackdropTop(palette), palette.Content, t);
        }

        private void PaintBackdrop(Graphics g, ModernPalette palette, Point scroll)
        {
            // Soft accent glow behind the hero that scrolls away with the content.
            Rectangle band = new(0, scroll.Y, ClientSize.Width, BackdropHeight);
            if (band.Bottom <= 0 || band.Width <= 0) return;
            using LinearGradientBrush brush = new(new Rectangle(band.X, band.Y, band.Width, band.Height + 1), BackdropTop(palette), palette.Content, LinearGradientMode.Vertical);
            g.FillRectangle(brush, band);
        }

        private int PaintStats(Graphics g, bool layoutOnly, ModernPalette palette, Fonts fonts, int x, int y, int width)
        {
            (string Value, string Label, char Glyph, Color Hue)[] stats =
            [
                (_snapshot.ConnectionCount.ToString(CultureInfo.CurrentCulture), Language.Connections, FluentIcons.Server, palette.Accent),
                (_snapshot.FolderCount.ToString(CultureInfo.CurrentCulture), Language.HomeStatFolders, FluentIcons.Folder, palette.Warning),
                (_snapshot.OpenCount.ToString(CultureInfo.CurrentCulture), Language.HomeStatOpenSessions, FluentIcons.Connect, palette.Success),
                (_snapshot.Favorites.Count.ToString(CultureInfo.CurrentCulture), Language.Favorites, FluentIcons.FavoriteFilled, palette.Danger)
            ];

            int gap = S(12);
            int columns = width >= S(600) ? 4 : 2;
            int tileWidth = (width - gap * (columns - 1)) / columns;
            int tileHeight = S(76);
            for (int i = 0; i < stats.Length; i++)
            {
                int column = i % columns;
                int row = i / columns;
                Rectangle tile = new(x + column * (tileWidth + gap), y + row * (tileHeight + gap), tileWidth, tileHeight);
                if (layoutOnly) continue;

                ModernGraphics.FillRoundedRectangle(g, palette.Surface, tile, S(ModernPalette.LargeCornerRadius));
                ModernGraphics.DrawRoundedRectangle(g, palette.BorderSubtle, tile, S(ModernPalette.LargeCornerRadius));

                Rectangle badge = new(tile.X + S(16), tile.Y + (tile.Height - S(36)) / 2, S(36), S(36));
                ModernGraphics.FillRoundedRectangle(g, ModernGraphics.Blend(palette.Surface, stats[i].Hue, palette.IsDark ? 0.18f : 0.12f), badge, S(ModernPalette.CornerRadius));
                FluentIcons.Draw(g, stats[i].Glyph, Rectangle.Inflate(badge, -S(8), -S(8)), stats[i].Hue, S(17));

                int textX = badge.Right + S(14);
                ModernText.Draw(g, stats[i].Value, fonts.StatValue, new Rectangle(textX, tile.Y + S(12), tile.Right - textX - S(8), S(30)), palette.Text);
                ModernText.Draw(g, stats[i].Label, fonts.Small, new Rectangle(textX, tile.Y + S(42), tile.Right - textX - S(8), S(20)), palette.TextSecondary);
            }

            int rows = (stats.Length + columns - 1) / columns;
            return y + rows * tileHeight + (rows - 1) * gap;
        }

        private int PaintSectionHeader(Graphics g, bool layoutOnly, ModernPalette palette, Fonts fonts, string title, string? linkText, Action? linkAction, int x, int y, int width)
        {
            int height = S(28);
            if (!layoutOnly)
                ModernText.Draw(g, title, fonts.Section, new Rectangle(x, y, width, height), palette.Text);

            if (linkText != null && linkAction != null)
            {
                Size size = ModernText.Measure(g, linkText, fonts.Small);
                Rectangle link = new(x + width - size.Width - S(12), y + (height - S(24)) / 2, size.Width + S(12), S(24));
                string id = "link:" + title;
                if (!layoutOnly)
                {
                    if (_hoverId == id) ModernGraphics.FillRoundedRectangle(g, palette.SurfaceHover, link, S(4));
                    ModernText.Draw(g, linkText, fonts.Small, link, _hoverId == id ? palette.Text : palette.TextSecondary, TextFormatFlags.HorizontalCenter);
                }

                AddRegion(id, link, linkAction);
            }

            return y + height + S(12);
        }

        private int PaintCards(Graphics g, bool layoutOnly, ModernPalette palette, Fonts fonts, List<CardData> cards, int x, int y, int width)
        {
            int gap = S(12);
            int minWidth = S(236);
            int columns = Math.Max(1, Math.Min(4, (width + gap) / (minWidth + gap)));
            int cardWidth = (width - gap * (columns - 1)) / columns;
            int cardHeight = S(96);

            for (int i = 0; i < cards.Count; i++)
            {
                CardData card = cards[i];
                Rectangle bounds = new(x + i % columns * (cardWidth + gap), y + i / columns * (cardHeight + gap), cardWidth, cardHeight);
                string id = "card:" + card.Id;

                // Card actions (registered after the card so that they win the hit test).
                List<(string Id, Rectangle Bounds, char Glyph, string Tip, Action Action, bool Active)> actions = [];
                int buttonSize = S(26);
                int bx = bounds.Right - S(10) - buttonSize;
                int by = bounds.Y + S(10);
                if (card.Entry != null && !card.IsFavoriteRow)
                {
                    RecentConnectionEntry entry = card.Entry;
                    actions.Add((id + ":remove", new Rectangle(bx, by, buttonSize, buttonSize), FluentIcons.Cancel, Language.HomeRemoveRecent,
                                 () => RecentConnections.Instance.Remove(entry), false));
                    bx -= buttonSize + S(2);
                }

                if (card.Connection != null)
                {
                    ConnectionInfo connection = card.Connection;
                    actions.Add((id + ":pin", new Rectangle(bx, by, buttonSize, buttonSize),
                                 card.IsFavorite ? FluentIcons.FavoriteFilled : FluentIcons.Favorite,
                                 card.IsFavorite ? Language.HomeUnpin : Language.HomePin,
                                 () =>
                                 {
                                     ToggleFavorite(connection);
                                     RefreshData(force: true);
                                 }, card.IsFavorite));
                }

                bool hover = _hoverId != null && (_hoverId == id || _hoverId.StartsWith(id + ":", StringComparison.Ordinal));
                bool pressed = _pressedId == id;

                if (!layoutOnly)
                {
                    float radius = S(ModernPalette.LargeCornerRadius);
                    Color background = pressed ? palette.SurfacePressed : hover ? palette.SurfaceHover : palette.Surface;
                    ModernGraphics.FillRoundedRectangle(g, background, bounds, radius);
                    ModernGraphics.DrawRoundedRectangle(g, hover ? palette.Border : palette.BorderSubtle, bounds, radius);

                    Rectangle tile = new(bounds.X + S(16), bounds.Y + S(16), S(40), S(40));
                    ProtocolVisuals.DrawTile(g, card.Protocol, tile, S(ModernPalette.CornerRadius + 2));

                    int textX = tile.Right + S(14);
                    int reserved = hover ? (bounds.Right - bx) + S(4) : S(16);
                    int textWidth = Math.Max(0, bounds.Right - textX - reserved);
                    int nameWidth = Math.Min(textWidth, ModernText.Measure(g, card.Name, fonts.CardTitle).Width);
                    ModernText.Draw(g, card.Name, fonts.CardTitle, new Rectangle(textX, bounds.Y + S(14), textWidth, S(22)), palette.Text);
                    if (card.IsOpen)
                    {
                        int dot = S(7);
                        using SolidBrush brush = new(palette.Success);
                        int dotX = Math.Min(textX + nameWidth + S(8), bounds.Right - reserved);
                        g.FillEllipse(brush, dotX, bounds.Y + S(14) + (S(22) - dot) / 2, dot, dot);
                    }

                    string host = string.IsNullOrEmpty(card.Host) ? card.Folder : card.Host;
                    ModernText.Draw(g, host, fonts.Small, new Rectangle(textX, bounds.Y + S(37), Math.Max(0, bounds.Right - textX - S(16)), S(18)), palette.TextSecondary);

                    // Footer: folder or relative time on the left, protocol badge on the right.
                    int footerY = bounds.Bottom - S(30);
                    string label = ProtocolVisuals.Label(card.Protocol);
                    Size labelSize = ModernText.Measure(g, label, fonts.Badge);
                    Rectangle badge = new(bounds.Right - S(16) - labelSize.Width - S(14), footerY + S(2), labelSize.Width + S(14), S(20));
                    Color hue = ProtocolVisuals.Color(card.Protocol);
                    ModernGraphics.FillRoundedRectangle(g, ModernGraphics.Blend(background, hue, 0.16f), badge, badge.Height / 2f);
                    TextRenderer.DrawText(g, label, fonts.Badge, badge, hue,
                                          TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                    string meta = card.When.Length > 0 ? card.When : string.IsNullOrEmpty(card.Host) ? string.Empty : card.Folder;
                    char metaGlyph = card.When.Length > 0 ? FluentIcons.History : FluentIcons.Folder;
                    if (meta.Length > 0)
                    {
                        Rectangle glyph = new(textX, footerY + S(5), S(14), S(14));
                        FluentIcons.Draw(g, metaGlyph, glyph, palette.TextSecondary, S(11));
                        ModernText.Draw(g, meta, fonts.Tiny, new Rectangle(glyph.Right + S(6), footerY, Math.Max(0, badge.Left - glyph.Right - S(12)), S(24)), palette.TextSecondary);
                    }

                    // Hover actions (the active favorite star stays visible).
                    foreach ((string actionId, Rectangle actionBounds, char glyphChar, _, _, bool active) in actions)
                    {
                        if (!hover && !active) continue;
                        if (_hoverId == actionId)
                            ModernGraphics.FillRoundedRectangle(g, palette.SurfacePressed, actionBounds, S(ModernPalette.CornerRadius));
                        Color color = active ? palette.Warning : _hoverId == actionId ? palette.Text : palette.TextSecondary;
                        FluentIcons.Draw(g, glyphChar, Rectangle.Inflate(actionBounds, -S(6), -S(6)), color, S(13));
                    }
                }

                CardData captured = card;
                string cardTip = string.Join("  ·  ", new[] { card.Folder, card.Host }.Where(t => !string.IsNullOrEmpty(t)));
                AddRegion(id, bounds, captured.Open, cardTip.Length > 0 ? cardTip : card.Name);
                foreach ((string actionId, Rectangle actionBounds, _, string tip, Action action, _) in actions)
                    AddRegion(actionId, actionBounds, action, tip);
            }

            int rows = (cards.Count + columns - 1) / columns;
            return y + rows * cardHeight + Math.Max(0, rows - 1) * gap;
        }

        private int PaintActions(Graphics g, bool layoutOnly, ModernPalette palette, Fonts fonts, int x, int y, int width)
        {
            (string Id, string Title, char Glyph, Action Action, string[] Keys)[] actions =
            [
                ("newConnection", Language.NewConnection, FluentIcons.Add, AppCommands.NewConnection, []),
                ("newFolder", Language.NewFolder, AppCommands.GlyphNewFolder, AppCommands.NewFolder, []),
                ("palette", Language.PaletteTitle, FluentIcons.Search, ModernExperience.ShowPalette, ["Ctrl", "K"]),
                ("theme", ThemeManager.getInstance().IsActiveThemeDark ? Language.PaletteSwitchToLight : Language.PaletteSwitchToDark,
                    ThemeManager.getInstance().IsActiveThemeDark ? AppCommands.GlyphBrightness : AppCommands.GlyphMoon, AppCommands.ToggleTheme, []),
                ("options", Language.Options, FluentIcons.Settings, () => AppWindows.Show(WindowType.Options), [])
            ];

            int gap = S(12);
            int minWidth = S(190);
            int columns = Math.Max(1, Math.Min(actions.Length, (width + gap) / (minWidth + gap)));
            int tileWidth = (width - gap * (columns - 1)) / columns;
            int tileHeight = S(56);

            for (int i = 0; i < actions.Length; i++)
            {
                Rectangle bounds = new(x + i % columns * (tileWidth + gap), y + i / columns * (tileHeight + gap), tileWidth, tileHeight);
                string id = "action:" + actions[i].Id;
                if (!layoutOnly)
                {
                    bool hover = _hoverId == id;
                    bool pressed = _pressedId == id;
                    Color background = pressed ? palette.SurfacePressed : hover ? palette.SurfaceHover : palette.Surface;
                    ModernGraphics.FillRoundedRectangle(g, background, bounds, S(ModernPalette.LargeCornerRadius));
                    ModernGraphics.DrawRoundedRectangle(g, hover ? palette.Border : palette.BorderSubtle, bounds, S(ModernPalette.LargeCornerRadius));

                    Rectangle circle = new(bounds.X + S(12), bounds.Y + (bounds.Height - S(32)) / 2, S(32), S(32));
                    ModernGraphics.FillRoundedRectangle(g, hover ? palette.Accent : palette.AccentSubtle, circle, S(16));
                    FluentIcons.Draw(g, actions[i].Glyph, Rectangle.Inflate(circle, -S(8), -S(8)), hover ? palette.AccentText : (palette.IsDark ? palette.AccentHover : palette.Accent), S(14));

                    int textX = circle.Right + S(12);
                    int right = bounds.Right - S(12);
                    if (actions[i].Keys.Length > 0 && tileWidth > S(230))
                    {
                        int capHeight = S(20);
                        int capsWidth = ModernText.MeasureKeyCaps(g, actions[i].Keys, fonts.Key, capHeight, _scale);
                        ModernText.DrawKeyCaps(g, actions[i].Keys, fonts.Key, new Point(right - capsWidth, bounds.Y + (bounds.Height - capHeight) / 2), capHeight, _scale);
                        right -= capsWidth + S(8);
                    }

                    ModernText.Draw(g, actions[i].Title, fonts.Small, new Rectangle(textX, bounds.Y, Math.Max(0, right - textX), bounds.Height), palette.Text);
                }

                Action action = actions[i].Action;
                AddRegion(id, bounds, action, actions[i].Title);
            }

            int rows = (actions.Length + columns - 1) / columns;
            return y + rows * tileHeight + (rows - 1) * gap;
        }

        private static string Greeting()
        {
            int hour = DateTime.Now.Hour;
            return hour < 5 || hour >= 18 ? Language.HomeGreetingEvening
                 : hour < 12 ? Language.HomeGreetingMorning
                 : Language.HomeGreetingAfternoon;
        }

        #endregion

        #region Mouse

        private void AddRegion(string id, Rectangle bounds, Action action, string? toolTip = null) =>
            _regions.Add(new HitRegion(id, bounds, action, toolTip));

        private HitRegion? HitTest(Point clientPoint)
        {
            Point p = new(clientPoint.X - AutoScrollPosition.X, clientPoint.Y - AutoScrollPosition.Y);
            for (int i = _regions.Count - 1; i >= 0; i--)
            {
                if (_regions[i].Bounds.Contains(p)) return _regions[i];
            }

            return null;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            HitRegion? hit = HitTest(e.Location);
            string? id = hit?.Id;
            Cursor = hit != null ? Cursors.Hand : Cursors.Default;
            if (id != _hoverId)
            {
                _hoverId = id;
                Invalidate();
            }

            if (hit?.ToolTip != _toolTipId)
            {
                _toolTipId = hit?.ToolTip;
                if (string.IsNullOrEmpty(hit?.ToolTip)) _toolTip.Hide(this);
                else _toolTip.Show(hit!.ToolTip, this, e.X + S(12), e.Y + S(18), 2500);
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoverId = null;
            _pressedId = null;
            _toolTipId = null;
            _toolTip.Hide(this);
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            _pressedId = HitTest(e.Location)?.Id;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left) return;
            HitRegion? hit = HitTest(e.Location);
            string? pressed = _pressedId;
            _pressedId = null;
            Invalidate();
            if (hit != null && hit.Id == pressed)
                BeginInvoke(hit.Action);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            _hoverId = null;
            Invalidate();
        }

        protected override void OnScroll(ScrollEventArgs se)
        {
            base.OnScroll(se);
            Invalidate();
        }

        private sealed record HitRegion(string Id, Rectangle Bounds, Action Action, string? ToolTip);

        #endregion

        #region Fonts

        private sealed class Fonts : IDisposable
        {
            public Fonts(float scale)
            {
                Greeting = ModernFonts.DisplaySemibold(30 * scale);
                Subtitle = ModernFonts.Regular(15 * scale);
                Section = ModernFonts.Semibold(16 * scale);
                StatValue = ModernFonts.DisplaySemibold(24 * scale);
                CardTitle = ModernFonts.Semibold(14.5f * scale);
                Body = ModernFonts.Regular(14 * scale);
                Small = ModernFonts.Regular(13 * scale);
                Tiny = ModernFonts.Regular(12 * scale);
                Badge = ModernFonts.Semibold(11 * scale);
                Key = ModernFonts.Regular(11.5f * scale);
            }

            public Font Greeting { get; }
            public Font Subtitle { get; }
            public Font Section { get; }
            public Font StatValue { get; }
            public Font CardTitle { get; }
            public Font Body { get; }
            public Font Small { get; }
            public Font Tiny { get; }
            public Font Badge { get; }
            public Font Key { get; }

            public void Dispose()
            {
                Greeting.Dispose();
                Subtitle.Dispose();
                Section.Dispose();
                StatValue.Dispose();
                CardTitle.Dispose();
                Body.Dispose();
                Small.Dispose();
                Tiny.Dispose();
                Badge.Dispose();
                Key.Dispose();
            }
        }

        #endregion
    }

    /// <summary>Large rounded quick connect input with a leading glyph and a "Connect" button.</summary>
    [SupportedOSPlatform("windows")]
    internal sealed class QuickConnectField : Control
    {
        private readonly PlaceholderTextBox _input;
        private float _scale = 1f;
        private Font? _font;
        private Font? _buttonFont;
        private bool _buttonHover;
        private bool _buttonPressed;

        public event EventHandler<string>? Submitted;

        /// <summary>Returns the parent's backdrop color at a y coordinate of this control (for the rounded corners).</summary>
        public Func<int, Color>? BackdropColorAt { get; set; }

        public QuickConnectField()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            _input = new PlaceholderTextBox
            {
                Placeholder = Language.HomeQuickConnectPlaceholder
            };
            _input.KeyDown += (_, e) =>
            {
                if (e.KeyCode != Keys.Enter) return;
                e.Handled = true;
                e.SuppressKeyPress = true;
                Submitted?.Invoke(this, _input.Text);
            };
            _input.GotFocus += (_, _) => Invalidate();
            _input.LostFocus += (_, _) => Invalidate();
            _input.TextChanged += (_, _) => Invalidate();
            Controls.Add(_input);
            Cursor = Cursors.IBeam;
            SetScale(1f);
            ApplyTheme();
        }

        public void SetScale(float scale)
        {
            _scale = scale;
            // The previous input font is not disposed: WinForms may still reference it while it
            // rescales child fonts during a DPI change (it is reclaimed by the GC).
            Font? oldButtonFont = _buttonFont;
            _font = ModernFonts.Regular(16 * scale);
            _buttonFont = ModernFonts.Semibold(13.5f * scale);
            _input.Font = _font;
            oldButtonFont?.Dispose();
            _input.Placeholder = Language.HomeQuickConnectPlaceholder;
            LayoutInput();
            Invalidate();
        }

        public void ApplyTheme()
        {
            ModernPalette palette = ModernPalette.Current;
            BackColor = palette.Content;
            _input.BackColor = palette.Surface;
            _input.ForeColor = palette.Text;
            Invalidate();
        }

        public void FocusInput() => _input.Focus();

        public void Clear() => _input.Clear();

        private int S(float logical) => (int)Math.Round(logical * _scale);

        private Rectangle ButtonBounds()
        {
            using Graphics g = CreateGraphics();
            int textWidth = _buttonFont == null ? S(80) : ModernText.Measure(g, Language.HomeConnectButton, _buttonFont).Width;
            int width = textWidth + S(44);
            int height = Height - S(14);
            return new Rectangle(Width - width - S(7), S(7), width, height);
        }

        private void LayoutInput()
        {
            if (Width <= 0) return;
            int left = S(50);
            int right = ButtonBounds().Left - S(10);
            int height = _input.PreferredHeight;
            _input.Bounds = new Rectangle(left, (Height - height) / 2, Math.Max(S(40), right - left), height);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutInput();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (ButtonBounds().Contains(e.Location))
            {
                _buttonPressed = true;
                Invalidate();
            }
            else
            {
                _input.Focus();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            bool click = _buttonPressed && ButtonBounds().Contains(e.Location);
            _buttonPressed = false;
            Invalidate();
            if (click) Submitted?.Invoke(this, _input.Text);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool hover = ButtonBounds().Contains(e.Location);
            Cursor = hover ? Cursors.Hand : Cursors.IBeam;
            if (hover == _buttonHover) return;
            _buttonHover = hover;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _buttonHover = false;
            _buttonPressed = false;
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
                _buttonFont?.Dispose();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ModernPalette palette = ModernPalette.Current;
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bounds = new(0, 0, Width, Height);

            // The parent paints a gradient: reproduce it behind the rounded corners.
            if (BackdropColorAt != null && Height > 0)
            {
                using LinearGradientBrush backdrop = new(new Rectangle(0, 0, Math.Max(1, Width), Height + 1), BackdropColorAt(0), BackdropColorAt(Height), LinearGradientMode.Vertical);
                g.FillRectangle(backdrop, bounds);
            }
            else
            {
                g.Clear(Parent?.BackColor ?? palette.Content);
            }

            bool focused = _input.Focused;
            float radius = S(ModernPalette.LargeCornerRadius + 2);

            if (focused)
                ModernGraphics.FillRoundedRectangle(g, palette.AccentSubtle, bounds, radius + S(1));

            Rectangle field = focused ? Rectangle.Inflate(bounds, -S(2), -S(2)) : bounds;
            ModernGraphics.FillRoundedRectangle(g, palette.Surface, field, radius);
            ModernGraphics.DrawRoundedRectangle(g, focused ? palette.Accent : palette.Border, field, radius, focused ? Math.Max(1f, 1.5f * _scale) : 1f);

            FluentIcons.Draw(g, FluentIcons.Lightning, new Rectangle(S(18), (Height - S(20)) / 2, S(20), S(20)),
                             palette.IsDark ? palette.AccentHover : palette.Accent, S(17));

            Rectangle button = ButtonBounds();
            Color buttonColor = _buttonPressed ? palette.AccentPressed : _buttonHover ? palette.AccentHover : palette.Accent;
            ModernGraphics.FillRoundedRectangle(g, buttonColor, button, S(ModernPalette.CornerRadius + 1));
            if (_buttonFont != null)
            {
                Rectangle glyph = new(button.X + S(14), button.Y + (button.Height - S(14)) / 2, S(14), S(14));
                FluentIcons.Draw(g, FluentIcons.Play, glyph, palette.AccentText, S(11));
                TextRenderer.DrawText(g, Language.HomeConnectButton, _buttonFont, new Rectangle(glyph.Right + S(6), button.Y, button.Right - glyph.Right - S(12), button.Height),
                                      palette.AccentText, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            }
        }
    }
}
