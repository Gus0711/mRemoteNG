#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.App;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Resources.Language;
using mRemoteNG.Themes;
using mRemoteNG.UI.GraphicsUtilities;
using mRemoteNG.UI.Modern;

namespace mRemoteNG.UI.Forms
{
    /// <summary>
    /// Ctrl+K command palette: a borderless popup over the main window that fuzzy-searches the
    /// connections, the recent connections and the application commands, and quick connects to a
    /// typed host.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public sealed class CommandPalette : Form
    {
        private const int LogicalWidth = 660;
        private const int LogicalSearchHeight = 58;
        private const int LogicalFooterHeight = 36;
        private const int LogicalMaxListHeight = 432;
        private const int LogicalTopOffset = 96;
        private const int MaxConnectionResults = 60;
        private const int MaxRecentItems = 6;

        private static CommandPalette? _current;

        private readonly PlaceholderTextBox _input;
        private readonly PaletteList _list;
        private readonly Form _owner;
        private readonly float _scale;
        private readonly Font _inputFont;
        private readonly Font _footerFont;
        private readonly Font _footerKeyFont;
        private bool _closing;

        public static void ShowFor(Form owner, string query = "")
        {
            if (_current is { IsDisposed: false })
            {
                if (query.Length > 0) _current.SetQuery(query);
                _current.Activate();
                return;
            }

            _current = new CommandPalette(owner);
            if (query.Length > 0) _current.SetQuery(query);
            _current.Show(owner);
        }

        private void SetQuery(string query)
        {
            _input.Text = query;
            _input.SelectionStart = _input.TextLength;
        }

        private CommandPalette(Form owner)
        {
            _owner = owner;
            _scale = owner.DeviceDpi / 96f;

            SuspendLayout();
            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            ShowIcon = false;
            StartPosition = FormStartPosition.Manual;
            KeyPreview = true;
            Text = Language.PaletteTitle;
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

            _inputFont = new Font(ModernGraphics.UiFontFamily, 17f * _scale, FontStyle.Regular, GraphicsUnit.Pixel);
            _footerFont = new Font(ModernGraphics.UiFontFamily, 12f * _scale, FontStyle.Regular, GraphicsUnit.Pixel);
            _footerKeyFont = new Font(ModernGraphics.UiFontFamily, 11f * _scale, FontStyle.Regular, GraphicsUnit.Pixel);

            _input = new PlaceholderTextBox
            {
                Font = _inputFont,
                Placeholder = Language.PalettePlaceholder,
                TabStop = true
            };
            _input.TextChanged += (_, _) => Rebuild();
            _input.KeyDown += Input_KeyDown;

            _list = new PaletteList(_scale);
            _list.ItemActivated += (_, item) => Execute(item);

            Controls.Add(_input);
            Controls.Add(_list);
            ApplyColors();
            ResumeLayout(false);

            Rebuild();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                const int CS_DROPSHADOW = 0x00020000;
                const int WS_EX_TOOLWINDOW = 0x00000080;
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                cp.ExStyle |= WS_EX_TOOLWINDOW;
                return cp;
            }
        }

        protected override bool ShowWithoutActivation => false;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ModernPalette palette = ModernPalette.Current;
            NativeMethods.UseImmersiveDarkMode(Handle, palette.IsDark);
            NativeMethods.SetWindowCornerPreference(Handle, NativeMethods.WindowCornerPreference.Round);
            NativeMethods.SetCaptionColors(Handle, palette.Surface, palette.Text, palette.Border);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _input.Focus();
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            CloseSafely();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (_current == this) _current = null;
            base.OnFormClosed(e);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _inputFont.Dispose();
                _footerFont.Dispose();
                _footerKeyFont.Dispose();
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                if (_input.TextLength > 0)
                    _input.Clear();
                else
                    CloseSafely();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void CloseSafely()
        {
            if (_closing || IsDisposed) return;
            _closing = true;
            BeginInvoke(new MethodInvoker(Close));
        }

        private void ApplyColors()
        {
            ModernPalette palette = ModernPalette.Current;
            BackColor = palette.Surface;
            _input.BackColor = palette.Surface;
            _input.ForeColor = palette.Text;
        }

        private int S(int logical) => (int)Math.Round(logical * _scale);

        private void Input_KeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Down:
                    _list.MoveSelection(1);
                    break;
                case Keys.Up:
                    _list.MoveSelection(-1);
                    break;
                case Keys.Tab:
                    _list.MoveSelection(e.Shift ? -1 : 1);
                    break;
                case Keys.PageDown:
                    _list.MoveSelection(8);
                    break;
                case Keys.PageUp:
                    _list.MoveSelection(-8);
                    break;
                case Keys.Enter:
                    if (_list.SelectedItem != null) Execute(_list.SelectedItem);
                    break;
                default:
                    return;
            }

            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            // Keep Tab inside the search box (used for list navigation).
            if ((keyData & Keys.KeyCode) == Keys.Tab) return true;
            return base.ProcessDialogKey(keyData);
        }

        private void Execute(PaletteItem item)
        {
            if (item.Execute == null) return;
            Action action = item.Execute;
            _closing = true;
            Hide();
            Form owner = _owner;
            Close();
            owner.Activate();
            owner.BeginInvoke(action);
        }

        #region Results

        private void Rebuild()
        {
            string query = _input.Text.Trim();
            List<PaletteItem> items = BuildItems(query);
            _list.SetItems(items, query.Length == 0 ? string.Empty : string.Format(Language.PaletteNoResults, query));
            LayoutPalette();
        }

        private static List<PaletteItem> BuildItems(string query)
        {
            List<PaletteItem> result = [];
            bool commandsOnly = query.StartsWith('>');
            if (commandsOnly) query = query[1..].Trim();

            IReadOnlyList<ConnectionInfo> connections = commandsOnly ? [] : ConnectionCatalog.GetConnections();
            Dictionary<string, int> recentRank = [];
            List<(RecentConnectionEntry Entry, ConnectionInfo? Connection)> recents = [];
            if (!commandsOnly)
            {
                recents = RecentConnections.Instance.Resolve().ToList();
                for (int i = 0; i < recents.Count; i++)
                {
                    if (recents[i].Connection is { } c) recentRank.TryAdd(c.ConstantID, i);
                }
            }

            if (query.Length == 0)
            {
                if (recents.Count > 0)
                {
                    result.Add(PaletteItem.Header(Language.PaletteGroupRecent));
                    foreach ((RecentConnectionEntry entry, ConnectionInfo? connection) in recents.Take(MaxRecentItems))
                        result.Add(RecentItem(entry, connection));
                }

                result.Add(PaletteItem.Header(Language.PaletteGroupCommands));
                result.AddRange(AppCommands.All.Select(c => CommandItem(c, null)));

                if (connections.Count > 0)
                {
                    result.Add(PaletteItem.Header(Language.Connections));
                    result.AddRange(connections.OrderBy(c => ConnectionCatalog.GetFolderPath(c), StringComparer.CurrentCultureIgnoreCase)
                                               .ThenBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase)
                                               .Take(500)
                                               .Select(c => ConnectionItem(c, null, null, 0)));
                }

                return result;
            }

            // Connections
            List<PaletteItem> connectionItems = [];
            foreach (ConnectionInfo connection in connections)
            {
                int bestScore = int.MinValue;
                int[]? nameMatches = null;
                int[]? hostMatches = null;

                if (FuzzyMatcher.TryMatch(query, connection.Name, out int nameScore, out int[] namePositions))
                {
                    bestScore = nameScore;
                    nameMatches = namePositions;
                }

                if (FuzzyMatcher.TryMatch(query, connection.Hostname, out int hostScore, out int[] hostPositions) && hostScore - 50 > bestScore)
                {
                    bestScore = hostScore - 50;
                    hostMatches = hostPositions;
                    nameMatches = null;
                }

                if (bestScore == int.MinValue)
                {
                    string path = ConnectionCatalog.GetFolderPath(connection);
                    if (path.Length == 0 || !FuzzyMatcher.TryMatch(query, path + " " + connection.Name, out int pathScore, out _))
                        continue;
                    bestScore = pathScore - 300;
                }

                if (recentRank.TryGetValue(connection.ConstantID, out int rank))
                    bestScore += Math.Max(0, 120 - rank * 15);

                connectionItems.Add(ConnectionItem(connection, nameMatches, hostMatches, bestScore));
            }

            connectionItems = connectionItems.OrderByDescending(i => i.Score)
                                             .ThenBy(i => i.Title, StringComparer.CurrentCultureIgnoreCase)
                                             .Take(MaxConnectionResults)
                                             .ToList();

            // Commands
            List<PaletteItem> commandItems = [];
            foreach (AppCommand command in AppCommands.All)
            {
                string title = command.Title;
                if (FuzzyMatcher.TryMatch(query, title, out int score, out int[] positions))
                    commandItems.Add(CommandItem(command, positions, score));
                else if (FuzzyMatcher.TryMatch(query, command.Keywords, out int keywordScore, out _) && keywordScore >= 1000)
                    commandItems.Add(CommandItem(command, null, keywordScore - 400));
            }

            commandItems = commandItems.OrderByDescending(i => i.Score).ToList();

            // Quick connect
            List<PaletteItem> quickItems = [];
            ProtocolType defaultProtocol = ConnectionCatalog.DefaultQuickConnectProtocol;
            if (!commandsOnly && ConnectionCatalog.TryParseQuickConnect(query, defaultProtocol, out string target, out ProtocolType protocol, out bool explicitProtocol))
            {
                // A bare word that already matches connections is most likely not a host name.
                bool hostLike = explicitProtocol || target.IndexOfAny(['.', ':', '@']) >= 0 || connectionItems.Count == 0;
                if (hostLike) quickItems.Add(QuickConnectItem(target, protocol));
                if (hostLike && !explicitProtocol)
                {
                    foreach (ProtocolType alternative in new[] { ProtocolType.RDP, ProtocolType.SSH2, ProtocolType.VNC })
                    {
                        if (alternative != protocol) quickItems.Add(QuickConnectItem(target, alternative));
                    }
                }
            }

            bool quickFirst = quickItems.Count > 0 && (connectionItems.Count == 0 || query.Contains("://", StringComparison.Ordinal));
            bool commandsFirst = commandsOnly || (commandItems.Count > 0 && (connectionItems.Count == 0 || commandItems[0].Score > connectionItems[0].Score + 100));

            void AddGroup(string header, List<PaletteItem> group)
            {
                if (group.Count == 0) return;
                result.Add(PaletteItem.Header(header));
                result.AddRange(group);
            }

            if (quickFirst) AddGroup(Language.QuickConnect, quickItems);
            if (commandsFirst) AddGroup(Language.PaletteGroupCommands, commandItems);
            AddGroup(Language.Connections, connectionItems);
            if (!commandsFirst) AddGroup(Language.PaletteGroupCommands, commandItems);
            if (!quickFirst) AddGroup(Language.QuickConnect, quickItems);
            return result;
        }

        private static PaletteItem ConnectionItem(ConnectionInfo connection, int[]? nameMatches, int[]? hostMatches, int score)
        {
            string path = ConnectionCatalog.GetFolderPath(connection);
            string host = connection.Hostname ?? string.Empty;
            return new PaletteItem(PaletteItemKind.Connection)
            {
                Title = connection.Name ?? string.Empty,
                TitleMatches = nameMatches,
                Prefix = path,
                Subtitle = host,
                SubtitleMatches = hostMatches,
                Protocol = connection.Protocol,
                Badge = ProtocolVisuals.Label(connection.Protocol),
                IsOpen = connection.OpenConnections is { Count: > 0 },
                Score = score,
                Execute = () => ConnectionCatalog.Open(connection)
            };
        }

        private static PaletteItem RecentItem(RecentConnectionEntry entry, ConnectionInfo? connection)
        {
            PaletteItem item = connection != null
                ? ConnectionItem(connection, null, null, 0)
                : new PaletteItem(PaletteItemKind.Connection)
                {
                    Title = entry.Name,
                    Prefix = Language.QuickConnect,
                    Subtitle = entry.Hostname,
                    Protocol = entry.ProtocolType,
                    Badge = ProtocolVisuals.Label(entry.ProtocolType)
                };
            item.Detail = RelativeTime.Format(entry.LastUsedUtc);
            item.Execute = () => RecentConnections.Open(entry, connection);
            return item;
        }

        private static PaletteItem CommandItem(AppCommand command, int[]? matches, int score = 0) =>
            new(PaletteItemKind.Command)
            {
                Title = command.Title,
                TitleMatches = matches,
                Glyph = command.Glyph,
                Keys = command.Keys,
                Score = score,
                Execute = command.Execute
            };

        private static PaletteItem QuickConnectItem(string target, ProtocolType protocol) =>
            new(PaletteItemKind.Connection)
            {
                Title = string.Format(Language.PaletteQuickConnectTo, target),
                Subtitle = Language.QuickConnect,
                Protocol = protocol,
                Badge = ProtocolVisuals.Label(protocol),
                Execute = () => ConnectionCatalog.QuickConnect(target, protocol)
            };

        #endregion

        #region Layout & painting

        private void LayoutPalette()
        {
            Rectangle ownerBounds = _owner.WindowState == FormWindowState.Minimized ? Screen.FromControl(_owner).WorkingArea : _owner.Bounds;
            int width = Math.Min(S(LogicalWidth), Math.Max(S(360), ownerBounds.Width - S(48)));
            int listHeight = Math.Min(_list.ContentHeight, S(LogicalMaxListHeight));
            int height = S(LogicalSearchHeight) + 1 + listHeight + S(LogicalFooterHeight);

            int x = ownerBounds.Left + (ownerBounds.Width - width) / 2;
            int y = ownerBounds.Top + S(LogicalTopOffset);
            Rectangle screen = Screen.FromRectangle(ownerBounds).WorkingArea;
            x = Math.Max(screen.Left, Math.Min(x, screen.Right - width));
            y = Math.Max(screen.Top, Math.Min(y, screen.Bottom - height));
            Bounds = new Rectangle(x, y, width, height);

            int inputLeft = S(52);
            int inputHeight = _input.PreferredHeight;
            _input.Bounds = new Rectangle(inputLeft, (S(LogicalSearchHeight) - inputHeight) / 2, width - inputLeft - S(20), inputHeight);
            _list.Bounds = new Rectangle(0, S(LogicalSearchHeight) + 1, width, listHeight);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ModernPalette palette = ModernPalette.Current;
            Graphics g = e.Graphics;
            g.Clear(palette.Surface);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int searchHeight = S(LogicalSearchHeight);
            FluentIcons.Draw(g, FluentIcons.Search, new Rectangle(S(18), (searchHeight - S(20)) / 2, S(20), S(20)), palette.TextSecondary, S(17));

            using (SolidBrush divider = new(palette.BorderSubtle))
                g.FillRectangle(divider, 0, searchHeight, Width, 1);

            // Footer with keyboard hints
            int footerHeight = S(LogicalFooterHeight);
            Rectangle footer = new(0, Height - footerHeight, Width, footerHeight);
            using (SolidBrush footerBrush = new(ModernGraphics.Blend(palette.Surface, palette.Chrome, palette.IsDark ? 0.6f : 0.8f)))
                g.FillRectangle(footerBrush, footer);
            using (SolidBrush divider = new(palette.BorderSubtle))
                g.FillRectangle(divider, 0, footer.Top, Width, 1);

            int capHeight = S(20);
            int capY = footer.Top + (footerHeight - capHeight) / 2;
            int x = S(16);
            x = DrawHint(g, x, capY, capHeight, ["↑", "↓"], Language.PaletteHintNavigate, palette);
            x = DrawHint(g, x, capY, capHeight, ["↵"], Language.PaletteHintOpen, palette);
            x = DrawHint(g, x, capY, capHeight, ["Esc"], Language.PaletteHintClose, palette);
            DrawHint(g, x, capY, capHeight, [">"], Language.PaletteHintCommands, palette);

            // Fallback border when DWM does not draw one (Windows 10).
            if (Environment.OSVersion.Version.Build < 22000)
            {
                using Pen border = new(palette.Border);
                g.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
            }
        }

        private int DrawHint(Graphics g, int x, int capY, int capHeight, string[] keys, string label, ModernPalette palette)
        {
            if (x > Width - S(60)) return x;
            x += ModernText.DrawKeyCaps(g, keys, _footerKeyFont, new Point(x, capY), capHeight, _scale);
            x += S(6);
            Size size = ModernText.Measure(g, label, _footerFont);
            ModernText.Draw(g, label, _footerFont, new Rectangle(x, capY, size.Width + S(2), capHeight), palette.TextSecondary);
            return x + size.Width + S(18);
        }

        #endregion

        #region Item model and list control

        private enum PaletteItemKind
        {
            Header,
            Connection,
            Command
        }

        private sealed class PaletteItem(PaletteItemKind kind)
        {
            public PaletteItemKind Kind { get; } = kind;
            public string Title { get; set; } = string.Empty;
            public int[]? TitleMatches { get; set; }

            /// <summary>Folder path, drawn before the subtitle.</summary>
            public string? Prefix { get; set; }

            public string? Subtitle { get; set; }
            public int[]? SubtitleMatches { get; set; }

            /// <summary>Right-aligned secondary text (e.g. "5 min ago").</summary>
            public string? Detail { get; set; }

            public string? Badge { get; set; }
            public char Glyph { get; set; }
            public ProtocolType? Protocol { get; set; }
            public string[] Keys { get; set; } = [];
            public bool IsOpen { get; set; }
            public int Score { get; set; }
            public Action? Execute { get; set; }

            public bool IsSelectable => Kind != PaletteItemKind.Header && Execute != null;

            public static PaletteItem Header(string title) => new(PaletteItemKind.Header) { Title = title };
        }

        /// <summary>Owner-drawn, virtually scrolled list of palette items.</summary>
        private sealed class PaletteList : Control
        {
            private readonly float _scale;
            private readonly Font _titleFont;
            private readonly Font _titleMatchFont;
            private readonly Font _secondaryFont;
            private readonly Font _secondaryMatchFont;
            private readonly Font _headerFont;
            private readonly Font _badgeFont;
            private List<PaletteItem> _items = [];
            private int[] _offsets = [];
            private int _selected = -1;
            private int _hover = -1;
            private int _scroll;
            private string _emptyText = string.Empty;
            private Point _lastMouse = Point.Empty;

            public event EventHandler<PaletteItem>? ItemActivated;

            public PaletteList(float scale)
            {
                _scale = scale;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
                SetStyle(ControlStyles.Selectable, false);
                TabStop = false;
                _titleFont = new Font(ModernGraphics.UiFontFamily, 14f * scale, FontStyle.Regular, GraphicsUnit.Pixel);
                _titleMatchFont = new Font(ModernGraphics.UiFontFamily, 14f * scale, FontStyle.Bold, GraphicsUnit.Pixel);
                _secondaryFont = new Font(ModernGraphics.UiFontFamily, 12f * scale, FontStyle.Regular, GraphicsUnit.Pixel);
                _secondaryMatchFont = new Font(ModernGraphics.UiFontFamily, 12f * scale, FontStyle.Bold, GraphicsUnit.Pixel);
                _headerFont = new Font(ModernGraphics.UiFontFamily, 11.5f * scale, FontStyle.Bold, GraphicsUnit.Pixel);
                _badgeFont = new Font(ModernGraphics.UiFontFamily, 10.5f * scale, FontStyle.Bold, GraphicsUnit.Pixel);
            }

            public int ContentHeight => _items.Count == 0 ? S(76) : _offsets[^1] + S(8);

            public PaletteItem? SelectedItem => _selected >= 0 && _selected < _items.Count ? _items[_selected] : null;

            private int S(float logical) => (int)Math.Round(logical * _scale);

            private int ItemHeight(PaletteItem item) => item.Kind switch
            {
                PaletteItemKind.Header => S(30),
                PaletteItemKind.Connection => S(50),
                _ => S(40)
            };

            public void SetItems(List<PaletteItem> items, string emptyText)
            {
                _items = items;
                _emptyText = emptyText;
                _offsets = new int[items.Count + 1];
                int y = S(6);
                for (int i = 0; i < items.Count; i++)
                {
                    _offsets[i] = y;
                    y += ItemHeight(items[i]);
                }

                _offsets[items.Count] = y;
                _scroll = 0;
                _hover = -1;
                _selected = items.FindIndex(i => i.IsSelectable);
                Invalidate();
            }

            public void MoveSelection(int delta)
            {
                if (_items.Count == 0) return;
                int index = _selected;
                int step = Math.Sign(delta);
                int remaining = Math.Abs(delta);
                int candidate = index;
                while (remaining > 0)
                {
                    int next = candidate + step;
                    while (next >= 0 && next < _items.Count && !_items[next].IsSelectable) next += step;
                    if (next < 0 || next >= _items.Count)
                    {
                        // Wrap around for single steps, stop at the ends for page moves.
                        if (Math.Abs(delta) == 1)
                        {
                            next = step > 0 ? _items.FindIndex(i => i.IsSelectable) : _items.FindLastIndex(i => i.IsSelectable);
                            if (next < 0) return;
                        }
                        else break;
                    }

                    candidate = next;
                    remaining--;
                }

                if (candidate < 0) return;
                _selected = candidate;
                EnsureVisible(_selected);
                Invalidate();
            }

            private void EnsureVisible(int index)
            {
                int top = _offsets[index];
                int bottom = _offsets[index + 1];

                // Show the group header too when selecting the first item of a group.
                if (index > 0 && _items[index - 1].Kind == PaletteItemKind.Header) top = _offsets[index - 1];
                if (index == _items.FindIndex(i => i.IsSelectable)) top = 0;

                if (top < _scroll) _scroll = top;
                else if (bottom + S(6) > _scroll + Height) _scroll = bottom + S(6) - Height;
                ClampScroll();
            }

            private void ClampScroll()
            {
                int max = Math.Max(0, ContentHeight - Height);
                _scroll = Math.Max(0, Math.Min(_scroll, max));
            }

            private int HitTest(Point location)
            {
                int y = location.Y + _scroll;
                for (int i = 0; i < _items.Count; i++)
                {
                    if (y >= _offsets[i] && y < _offsets[i + 1])
                        return _items[i].IsSelectable ? i : -1;
                }

                return -1;
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                // Ignore synthetic moves caused by scrolling/re-layout under a still cursor.
                if (e.Location == _lastMouse) return;
                _lastMouse = e.Location;
                int hit = HitTest(e.Location);
                if (hit == _hover) return;
                _hover = hit;
                if (hit >= 0) _selected = hit;
                Invalidate();
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                _hover = -1;
                Invalidate();
            }

            protected override void OnMouseClick(MouseEventArgs e)
            {
                base.OnMouseClick(e);
                if (e.Button != MouseButtons.Left) return;
                int hit = HitTest(e.Location);
                if (hit >= 0) ItemActivated?.Invoke(this, _items[hit]);
            }

            protected override void OnMouseWheel(MouseEventArgs e)
            {
                base.OnMouseWheel(e);
                _scroll -= e.Delta * S(48) / 120;
                ClampScroll();
                Invalidate();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _titleFont.Dispose();
                    _titleMatchFont.Dispose();
                    _secondaryFont.Dispose();
                    _secondaryMatchFont.Dispose();
                    _headerFont.Dispose();
                    _badgeFont.Dispose();
                }

                base.Dispose(disposing);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                ModernPalette palette = ModernPalette.Current;
                Graphics g = e.Graphics;
                g.Clear(palette.Surface);
                g.SmoothingMode = SmoothingMode.AntiAlias;

                if (_items.Count == 0)
                {
                    if (_emptyText.Length > 0)
                    {
                        Rectangle bounds = new(S(16), 0, Width - S(32), Height);
                        FluentIcons.Draw(g, FluentIcons.Search, new Rectangle(Width / 2 - S(10), S(14), S(20), S(20)), palette.TextDisabled, S(16));
                        ModernText.Draw(g, _emptyText, _titleFont, new Rectangle(bounds.X, S(38), bounds.Width, S(28)), palette.TextSecondary, TextFormatFlags.HorizontalCenter);
                    }

                    return;
                }

                for (int i = 0; i < _items.Count; i++)
                {
                    int top = _offsets[i] - _scroll;
                    int bottom = _offsets[i + 1] - _scroll;
                    if (bottom < 0) continue;
                    if (top > Height) break;
                    PaintItem(g, _items[i], new Rectangle(0, top, Width, bottom - top), i == _selected, palette);
                }

                PaintScrollBar(g, palette);
            }

            private void PaintScrollBar(Graphics g, ModernPalette palette)
            {
                int content = ContentHeight;
                if (content <= Height) return;
                int trackHeight = Height - S(8);
                int thumbHeight = Math.Max(S(28), trackHeight * Height / content);
                int thumbTop = S(4) + (trackHeight - thumbHeight) * _scroll / Math.Max(1, content - Height);
                Rectangle thumb = new(Width - S(7), thumbTop, S(4), thumbHeight);
                ModernGraphics.FillRoundedRectangle(g, ModernGraphics.Blend(palette.Surface, palette.TextSecondary, 0.35f), thumb, S(2));
            }

            private void PaintItem(Graphics g, PaletteItem item, Rectangle bounds, bool selected, ModernPalette palette)
            {
                int margin = S(8);
                if (item.Kind == PaletteItemKind.Header)
                {
                    Rectangle headerBounds = new(bounds.X + S(20), bounds.Y + S(8), bounds.Width - S(40), bounds.Height - S(8));
                    ModernText.Draw(g, item.Title.ToUpperInvariant(), _headerFont, headerBounds, palette.TextSecondary);
                    return;
                }

                Rectangle row = new(bounds.X + margin, bounds.Y + S(1), bounds.Width - margin * 2 - S(4), bounds.Height - S(2));
                if (selected)
                {
                    ModernGraphics.FillRoundedRectangle(g, palette.Selection, row, S(ModernPalette.CornerRadius));
                    Rectangle accentBar = new(row.X, row.Y + row.Height / 2 - S(8), S(3), S(16));
                    ModernGraphics.FillRoundedRectangle(g, palette.Accent, accentBar, S(1.5f));
                }

                int iconSize = item.Kind == PaletteItemKind.Connection ? S(32) : S(28);
                Rectangle icon = new(row.X + S(12), row.Y + (row.Height - iconSize) / 2, iconSize, iconSize);
                if (item.Protocol is { } protocol)
                {
                    ProtocolVisuals.DrawTile(g, protocol, icon, S(ModernPalette.CornerRadius));
                }
                else
                {
                    Color tile = selected ? ModernGraphics.Blend(palette.Selection, palette.Surface, 0.4f) : palette.SurfaceHover;
                    ModernGraphics.FillRoundedRectangle(g, tile, icon, S(ModernPalette.CornerRadius));
                    FluentIcons.Draw(g, item.Glyph, Rectangle.Inflate(icon, -S(6), -S(6)), selected ? palette.Accent : palette.TextSecondary, S(15));
                }

                int textLeft = icon.Right + S(12);
                int right = row.Right - S(12);

                // Right side: shortcut keys, relative time, protocol badge, open indicator.
                if (item.Keys.Length > 0)
                {
                    int capHeight = S(20);
                    int capsWidth = ModernText.MeasureKeyCaps(g, item.Keys, _badgeFont, capHeight, _scale);
                    ModernText.DrawKeyCaps(g, item.Keys, _badgeFont, new Point(right - capsWidth, row.Y + (row.Height - capHeight) / 2), capHeight, _scale);
                    right -= capsWidth + S(12);
                }

                if (!string.IsNullOrEmpty(item.Badge) && item.Protocol is { } badgeProtocol)
                {
                    Size size = ModernText.Measure(g, item.Badge, _badgeFont);
                    int badgeHeight = S(20);
                    Rectangle badge = new(right - size.Width - S(14), row.Y + (row.Height - badgeHeight) / 2, size.Width + S(14), badgeHeight);
                    Color hue = ProtocolVisuals.Color(badgeProtocol);
                    Color background = selected ? palette.Selection : palette.Surface;
                    ModernGraphics.FillRoundedRectangle(g, ModernGraphics.Blend(background, hue, 0.16f), badge, badgeHeight / 2f);
                    TextRenderer.DrawText(g, item.Badge, _badgeFont, badge, hue,
                                          TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    right = badge.Left - S(10);
                }

                if (!string.IsNullOrEmpty(item.Detail))
                {
                    Size size = ModernText.Measure(g, item.Detail, _secondaryFont);
                    ModernText.Draw(g, item.Detail, _secondaryFont, new Rectangle(right - size.Width, row.Y, size.Width + S(2), row.Height), palette.TextSecondary);
                    right -= size.Width + S(12);
                }

                if (item.IsOpen)
                {
                    int dot = S(8);
                    Rectangle dotBounds = new(right - dot, row.Y + (row.Height - dot) / 2, dot, dot);
                    using SolidBrush brush = new(palette.Success);
                    g.FillEllipse(brush, dotBounds);
                    right = dotBounds.Left - S(10);
                }

                int textWidth = Math.Max(0, right - textLeft);
                bool hasSecondLine = item.Kind == PaletteItemKind.Connection && (!string.IsNullOrEmpty(item.Subtitle) || !string.IsNullOrEmpty(item.Prefix));
                Color matchColor = palette.IsDark ? palette.AccentHover : palette.Accent;
                if (!hasSecondLine)
                {
                    ModernText.DrawHighlighted(g, item.Title, _titleFont, _titleMatchFont, new Rectangle(textLeft, row.Y, textWidth, row.Height),
                                               palette.Text, matchColor, item.TitleMatches);
                    return;
                }

                int lineHeight = S(20);
                int top = row.Y + (row.Height - lineHeight * 2) / 2 + S(1);
                ModernText.DrawHighlighted(g, item.Title, _titleFont, _titleMatchFont, new Rectangle(textLeft, top, textWidth, lineHeight),
                                           palette.Text, matchColor, item.TitleMatches);

                int x = textLeft;
                Rectangle second = new(textLeft, top + lineHeight - S(1), textWidth, lineHeight);
                if (!string.IsNullOrEmpty(item.Prefix))
                {
                    string prefix = item.Prefix + (string.IsNullOrEmpty(item.Subtitle) ? string.Empty : "  ·  ");
                    int prefixWidth = Math.Min(ModernText.Measure(g, prefix, _secondaryFont).Width, textWidth * 2 / 3);
                    ModernText.Draw(g, prefix, _secondaryFont, new Rectangle(x, second.Y, prefixWidth, second.Height), palette.TextSecondary);
                    x += prefixWidth;
                }

                if (!string.IsNullOrEmpty(item.Subtitle))
                {
                    ModernText.DrawHighlighted(g, item.Subtitle, _secondaryFont, _secondaryMatchFont,
                                               new Rectangle(x, second.Y, Math.Max(0, second.Right - x), second.Height),
                                               palette.TextSecondary, matchColor, item.SubtitleMatches);
                }
            }
        }

        #endregion
    }
}
