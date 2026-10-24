using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.App;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Resources.Language;

namespace mRemoteNG.UI.Controls
{
    /// <summary>
    /// Quick connect input of the quick connect toolbar: a rounded, search-style box with a placeholder,
    /// a protocol pill and a "recent connections" history (Alt+Down / F4 or the clock button; Delete removes
    /// the highlighted entry). Enter raises <see cref="ConnectRequested"/>.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class QuickConnectComboBox : ModernInputHost
    {
        private const int LogicalWidth = 300;

        private readonly ModernInputBox _input;
        private readonly TextBox _textBox;
        private readonly ModernInputBox.TrailingButton _historyButton;
        private readonly ModernInputBox.TrailingButton _protocolButton;
        private readonly HistoryMenu _historyMenu;
        private readonly List<HistoryItem> _history = [];

        public QuickConnectComboBox() : base(new ModernInputBox(), LogicalWidth)
        {
            _input = Input;
            _textBox = _input.InnerTextBox;
            _input.LeadingGlyph = FluentIcons.Search;
            _input.PlaceholderText = Language.QuickConnectPlaceholder;

            _textBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            _textBox.AutoCompleteSource = AutoCompleteSource.CustomSource;
            _textBox.AutoCompleteCustomSource = new AutoCompleteStringCollection();
            _textBox.KeyDown += TextBox_KeyDown;

            _historyMenu = new HistoryMenu();
            _historyMenu.DeleteRequested += HistoryMenu_DeleteRequested;
            _historyMenu.Closed += (_, _) => _input.Invalidate();

            _historyButton = new ModernInputBox.TrailingButton
            {
                Glyph = FluentIcons.History,
                Visible = false,
                ToolTip = Language.QuickConnectRecent,
                Click = _ => ShowHistory()
            };
            _protocolButton = new ModernInputBox.TrailingButton
            {
                ShowChevron = true,
                ToolTip = Language.Protocol,
                Click = ShowProtocolMenu
            };
            _input.TrailingButtons.Add(_historyButton);
            _input.TrailingButtons.Add(_protocolButton);
            _input.RefreshButtons();

            CausesValidation = false;
        }

        /// <summary>Menu listing the protocols, shown below the protocol pill.</summary>
        public ContextMenuStrip ProtocolMenu { get; set; }

        /// <summary>Protocol displayed in the protocol pill.</summary>
        public string ProtocolText
        {
            get => _protocolButton.Text;
            set
            {
                _protocolButton.Text = value;
                _input.RefreshButtons();
            }
        }

        public string PlaceholderText
        {
            get => _input.PlaceholderText;
            set => _input.PlaceholderText = value;
        }

        /// <summary>The menu showing the history (styled by the owner like the other menus).</summary>
        public ContextMenuStrip HistoryDropDown => _historyMenu;

        public new void Focus() => _input.FocusInput();

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    OnConnectRequested(new ConnectRequestedEventArgs(_textBox.Text));
                    break;
                case Keys.Down when e.Alt:
                case Keys.F4:
                case Keys.Down when _textBox.TextLength == 0:
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    ShowHistory();
                    break;
            }
        }

        private void ShowProtocolMenu(Rectangle bounds)
        {
            if (ProtocolMenu == null)
                return;

            ProtocolMenu.Show(_input, new Point(bounds.Left, _input.Height + _input.LogicalToDeviceUnits(4)));
        }

        private void ShowHistory()
        {
            if (_history.Count == 0)
                return;

            _historyMenu.Items.Clear();
            foreach (HistoryItem historyItem in _history)
            {
                ToolStripMenuItem menuItem = new(historyItem.ToString(true)) { Tag = historyItem };
                ToolStripGlyphs.Set(menuItem, FluentIcons.History, ToolStripGlyphs.GlyphRole.Secondary);
                menuItem.Click += HistoryMenuItem_Click;
                _historyMenu.Items.Add(menuItem);
            }

            _historyMenu.MinimumSize = new Size(_input.Width, 0);
            _historyMenu.Show(_input, new Point(0, _input.Height + _input.LogicalToDeviceUnits(4)));
            if (_historyMenu.Items.Count > 0)
                _historyMenu.Items[0].Select();
        }

        private void HistoryMenuItem_Click(object sender, EventArgs e)
        {
            if (sender is not ToolStripItem { Tag: HistoryItem historyItem })
                return;

            _textBox.Text = historyItem.ToString();
            _input.FocusInput();
            OnProtocolChanged(new ProtocolChangedEventArgs(historyItem.ConnectionInfo.Protocol));
        }

        private void HistoryMenu_DeleteRequested(object sender, ToolStripItem item)
        {
            if (item.Tag is not HistoryItem historyItem)
                return;

            _history.Remove(historyItem);
            _textBox.AutoCompleteCustomSource.Remove(historyItem.ToString());
            _historyMenu.Items.Remove(item);
            item.Dispose();
            UpdateHistoryButton();

            if (_historyMenu.Items.Count == 0)
                _historyMenu.Close();
        }

        private void UpdateHistoryButton()
        {
            bool visible = _history.Count > 0;
            if (_historyButton.Visible == visible)
                return;

            _historyButton.Visible = visible;
            _input.RefreshButtons();
        }

        private readonly struct HistoryItem(ConnectionInfo connectionInfo) : IEquatable<HistoryItem>
        {
            public ConnectionInfo ConnectionInfo { get; } = connectionInfo;

            public bool Equals(HistoryItem other)
            {
                if (ConnectionInfo.Hostname != other.ConnectionInfo.Hostname)
                {
                    return false;
                }

                if (ConnectionInfo.Port != other.ConnectionInfo.Port)
                {
                    return false;
                }

                return ConnectionInfo.Protocol == other.ConnectionInfo.Protocol;
            }

            public override bool Equals(object obj) => obj is HistoryItem other && Equals(other);

            public override int GetHashCode() =>
                HashCode.Combine(ConnectionInfo.Hostname, ConnectionInfo.Port, ConnectionInfo.Protocol);

            public override string ToString()
            {
                return ToString(false);
            }

            public string ToString(bool includeProtocol)
            {
                string port = string.Empty;
                if (ConnectionInfo.Port != ConnectionInfo.GetDefaultPort())
                {
                    port = $":{ConnectionInfo.Port}";
                }

                return includeProtocol
                    ? $"{ConnectionInfo.Hostname}{port} ({ConnectionInfo.Protocol})"
                    : $"{ConnectionInfo.Hostname}{port}";
            }
        }

        public void Add(ConnectionInfo connectionInfo)
        {
            try
            {
                HistoryItem historyItem = new(connectionInfo);
                if (_history.Any(item => item.Equals(historyItem)))
                    return;

                _history.Insert(0, historyItem);
                string text = historyItem.ToString();
                if (!_textBox.AutoCompleteCustomSource.Contains(text))
                    _textBox.AutoCompleteCustomSource.Add(text);
                UpdateHistoryButton();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage(Language.QuickConnectAddFailed, ex);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _historyMenu.Dispose();
            base.Dispose(disposing);
        }

        /// <summary>History popup: Delete removes the highlighted entry.</summary>
        private sealed class HistoryMenu : ContextMenuStrip
        {
            public event EventHandler<ToolStripItem> DeleteRequested;

            public HistoryMenu()
            {
                ShowImageMargin = true;
                ShowCheckMargin = false;
            }

            protected override bool ProcessDialogKey(Keys keyData)
            {
                if (keyData == Keys.Delete)
                {
                    ToolStripItem selected = Items.Cast<ToolStripItem>().FirstOrDefault(item => item.Selected);
                    if (selected != null)
                    {
                        DeleteRequested?.Invoke(this, selected);
                        return true;
                    }
                }

                return base.ProcessDialogKey(keyData);
            }
        }

        #region Events

        public class ConnectRequestedEventArgs(string connectionString) : EventArgs
        {
            public string ConnectionString { get; } = connectionString;
        }

        public delegate void ConnectRequestedEventHandler(object sender, ConnectRequestedEventArgs e);

        private ConnectRequestedEventHandler ConnectRequestedEvent;

        public event ConnectRequestedEventHandler ConnectRequested
        {
            add => ConnectRequestedEvent = (ConnectRequestedEventHandler)Delegate.Combine(ConnectRequestedEvent, value);
            remove => ConnectRequestedEvent = (ConnectRequestedEventHandler)Delegate.Remove(ConnectRequestedEvent, value);
        }


        private void OnConnectRequested(ConnectRequestedEventArgs e)
        {
            ConnectRequestedEvent?.Invoke(this, e);
        }

        public class ProtocolChangedEventArgs(ProtocolType protocol) : EventArgs
        {
            public ProtocolType Protocol { get; } = protocol;
        }

        public delegate void ProtocolChangedEventHandler(object sender, ProtocolChangedEventArgs e);

        private ProtocolChangedEventHandler ProtocolChangedEvent;

        public event ProtocolChangedEventHandler ProtocolChanged
        {
            add => ProtocolChangedEvent = (ProtocolChangedEventHandler)Delegate.Combine(ProtocolChangedEvent, value);
            remove => ProtocolChangedEvent = (ProtocolChangedEventHandler)Delegate.Remove(ProtocolChangedEvent, value);
        }


        private void OnProtocolChanged(ProtocolChangedEventArgs e)
        {
            ProtocolChangedEvent?.Invoke(this, e);
        }

        #endregion
    }
}
