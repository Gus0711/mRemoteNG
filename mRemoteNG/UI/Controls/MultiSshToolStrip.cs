using System.ComponentModel;
using System.Windows.Forms;
using mRemoteNG.Themes;
using System;
using System.Collections;
using System.Linq;
using mRemoteNG.App;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Resources.Language;
using System.Runtime.Versioning;

namespace mRemoteNG.UI.Controls
{
    [SupportedOSPlatform("windows")]
    public partial class MultiSshToolStrip : ToolStrip
    {
        private IContainer components;
        private ModernInputHost hostMultiSsh;
        private TextBox txtMultiSsh;
        private int previousCommandIndex = 0;
        private readonly ArrayList processHandlers = [];
        private readonly ArrayList quickConnectConnections = [];
        private readonly ArrayList previousCommands = [];
        private readonly ThemeManager _themeManager;

        private int CommandHistoryLength { get; set; } = 100;

        public MultiSshToolStrip()
        {
            InitializeComponent();
            _themeManager = ThemeManager.getInstance();
            _themeManager.ThemeChanged += ApplyTheme;
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            // The rounded input follows the modern palette by itself.
            Font = ModernToolStripRenderer.UiFont;
            hostMultiSsh.Input.Invalidate();
        }

        public void ApplyLanguage()
        {
            hostMultiSsh.Input.PlaceholderText = Language.MultiSshPlaceholder;
            hostMultiSsh.ToolTipText = Language.MultiSshToolTip;
        }

        /// <summary>Header-friendly sizes: a 40px (logical) bar with a 30px input.</summary>
        private void ApplyMetrics()
        {
            float scale = DeviceDpi / 96f;
            Padding = new Padding((int)Math.Round(4 * scale), (int)Math.Round(5 * scale), (int)Math.Round(8 * scale), (int)Math.Round(5 * scale));
            MinimumSize = new System.Drawing.Size(MinimumSize.Width, (int)Math.Round(40 * scale));
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            ApplyMetrics();
        }

        private ArrayList ProcessOpenConnections(ConnectionInfo connection)
        {
            ArrayList handlers = new();

            foreach (ProtocolBase _base in connection.OpenConnections)
            {
                if (_base.GetType().IsSubclassOf(typeof(PuttyBase)))
                {
                    handlers.Add((PuttyBase)_base);
                }
            }

            return handlers;
        }

        private void SendAllKeystrokes(int keyType, int keyData)
        {
            if (processHandlers.Count == 0) return;

            foreach (PuttyBase proc in processHandlers)
            {
                NativeMethods.PostMessage(proc.PuttyHandle, keyType, new IntPtr(keyData), new IntPtr(0));
            }
        }

        #region Key Event Handler

        private void RefreshActiveConnections(object sender, EventArgs e)
        {
            processHandlers.Clear();
            foreach (ConnectionInfo connection in quickConnectConnections)
            {
                processHandlers.AddRange(ProcessOpenConnections(connection));
            }

            System.Collections.Generic.IEnumerable<ConnectionInfo> connectionTreeConnections = Runtime.ConnectionsService.ConnectionTreeModel.GetRecursiveChildList().Where(item => item.OpenConnections.Count > 0);

            foreach (ConnectionInfo connection in connectionTreeConnections)
            {
                processHandlers.AddRange(ProcessOpenConnections(connection));
            }
        }

        private void ProcessKeyPress(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down)
            {
                e.SuppressKeyPress = true;
                try
                {
                    switch (e.KeyCode)
                    {
                        case Keys.Up when previousCommandIndex - 1 >= 0:
                            previousCommandIndex -= 1;
                            break;
                        case Keys.Down when previousCommandIndex + 1 < previousCommands.Count:
                            previousCommandIndex += 1;
                            break;
                        default:
                            return;
                    }
                }
                catch { }

                txtMultiSsh.Text = previousCommands[previousCommandIndex].ToString();
                txtMultiSsh.SelectAll();
            }

            if (e.Control && e.KeyCode != Keys.V && e.Alt == false)
            {
                SendAllKeystrokes(NativeMethods.WM_KEYDOWN, e.KeyValue);
            }

            if (e.KeyCode == Keys.Enter)
            {
                foreach (char chr1 in txtMultiSsh.Text)
                {
                    SendAllKeystrokes(NativeMethods.WM_CHAR, Convert.ToByte(chr1));
                }

                SendAllKeystrokes(NativeMethods.WM_KEYDOWN, 13); // Enter = char13
                e.SuppressKeyPress = true; // no "ding" from the single-line text box
            }
        }

        private void ProcessKeyRelease(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            if (string.IsNullOrWhiteSpace(txtMultiSsh.Text)) return;

            previousCommands.Add(txtMultiSsh.Text.Trim());

            if (previousCommands.Count >= CommandHistoryLength) previousCommands.RemoveAt(0);

            previousCommandIndex = previousCommands.Count - 1;
            txtMultiSsh.Clear();
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if(components != null)
                    components.Dispose();
                _themeManager.ThemeChanged -= ApplyTheme;
            }

            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            ModernInputBox inputMultiSsh = new()
            {
                LeadingGlyph = FluentIcons.Terminal,
                PlaceholderText = Language.MultiSshPlaceholder
            };
            this.hostMultiSsh = new ModernInputHost(inputMultiSsh, 340);
            this.txtMultiSsh = inputMultiSsh.InnerTextBox;
            this.SuspendLayout();
            // 
            // hostMultiSsh / txtMultiSsh
            // 
            this.hostMultiSsh.Name = "_hostMultiSsh";
            this.hostMultiSsh.ToolTipText = Language.MultiSshToolTip;
            this.txtMultiSsh.Name = "_txtMultiSsh";
            this.txtMultiSsh.Enter += RefreshActiveConnections;
            this.txtMultiSsh.KeyDown += ProcessKeyPress;
            this.txtMultiSsh.KeyUp += ProcessKeyRelease;

            this.GripMargin = new Padding(2);
            this.Items.AddRange(new ToolStripItem[]
            {
                hostMultiSsh
            });
            ApplyMetrics();
            this.ResumeLayout(false);
        }

        #endregion

    }
}