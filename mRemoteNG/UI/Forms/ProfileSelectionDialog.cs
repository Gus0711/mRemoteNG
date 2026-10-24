using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.Config.UserProfiles;
using mRemoteNG.Themes;
using mRemoteNG.UI.Controls;
using mRemoteNG.UI.GraphicsUtilities;

namespace mRemoteNG.UI.Forms
{
    [SupportedOSPlatform("windows")]
    internal sealed class ProfileSelectionDialog : Form
    {
        private readonly MrngComboBox _profiles = new() { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList };

        private ProfileSelectionDialog(IReadOnlyList<ConnectionProfile> profiles)
        {
            Text = "Select connection profile";
            Font = ModernGraphics.CreateUiFont(9f);
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96F, 96F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(380, 132);
            StartPosition = FormStartPosition.CenterParent;
            Padding = new Padding(20, 16, 20, 16);

            MrngLabel prompt = new() { Text = "Profile", Dock = DockStyle.Top, Height = 24, SecondaryText = true };
            _profiles.DataSource = profiles.ToList();
            _profiles.DisplayMember = nameof(ConnectionProfile.Name);

            MrngButton accept = new()
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Size = new Size(96, 32),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Location = new Point(ClientSize.Width - Padding.Right - 96, ClientSize.Height - Padding.Bottom - 32)
            };

            Controls.Add(accept);
            Controls.Add(_profiles);
            Controls.Add(prompt);
            AcceptButton = accept;
            ApplyTheme();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ThemeManager.getInstance().ApplyThemeToTitleBar(this);
        }

        private void ApplyTheme()
        {
            if (!ModernControlColors.Enabled) return;
            ModernControlColors colors = ModernControlColors.Current;
            BackColor = colors.Background;
            ForeColor = colors.Text;
        }

        public static ConnectionProfile Select(IReadOnlyList<ConnectionProfile> profiles)
        {
            using ProfileSelectionDialog dialog = new(profiles);
            return dialog.ShowDialog() == DialogResult.OK ? (ConnectionProfile)dialog._profiles.SelectedItem : null;
        }
    }
}
