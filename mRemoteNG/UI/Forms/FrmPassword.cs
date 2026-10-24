using System;
using System.Security;
using System.Drawing;
using System.Windows.Forms;
using mRemoteNG.UI.Controls;
using mRemoteNG.UI.GraphicsUtilities;
using mRemoteNG.Security;
using mRemoteNG.Themes;
using mRemoteNG.Tools;
using mRemoteNG.Resources.Language;
using System.Runtime.Versioning;

namespace mRemoteNG.UI.Forms
{
    [SupportedOSPlatform("windows")]
    public partial class FrmPassword : IKeyProvider
    {
        private readonly string _passwordName;
        private SecureString _password = new();

        /// <summary>
        /// Puts the dialog into the New Password mode. An extra
        /// password box is shown which must match the first password
        /// to continue.
        /// </summary>
        private bool NewPasswordMode { get; }

        /// <summary>
        /// Creates a new password form for entering or setting a password.
        /// </summary>
        /// <param name="passwordName"></param>
        /// <param name="newPasswordMode">
        /// Puts the dialog into the New Password mode. An extra
        /// password box is shown which must match the first password
        /// to continue.
        /// </param>
        public FrmPassword(string? passwordName = null, bool newPasswordMode = true)
        {
            InitializeComponent();
            _passwordName = passwordName;
            NewPasswordMode = newPasswordMode;
        }

        /// <summary>
        /// Dispaly a dialog box requesting that the user 
        /// enter their password.
        /// </summary>
        /// <returns></returns>
        public Optional<SecureString> GetKey() => GetKey(null);

        public Optional<SecureString> GetKey(IWin32Window? owner)
        {
            DialogResult dialog = owner == null
                ? ShowDialog()
                : ShowDialog(owner);
            return dialog == DialogResult.OK
                ? _password
                : Optional<SecureString>.Empty;
        }

        #region Event Handlers

        private void FrmPassword_Load(object sender, EventArgs e)
        {
            ApplyLanguage();
            ApplyTheme();
            pbLock.Image = CreateLockTile();

            if (NewPasswordMode)
            {
                FitToContent();
                txtPassword.Focus();
                return;
            }
            lblVerify.Visible = false;
            txtVerify.Visible = false;
            FitToContent();
            txtPassword.Focus();
        }

        private void PasswordForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _password = txtPassword.Text.ConvertToSecureString();
            txtPassword.Text = "";
            txtVerify.Text = "";
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            if (NewPasswordMode)
                VerifyNewPassword();

            DialogResult = DialogResult.OK;
        }

        private void TxtPassword_TextChanged(object sender, EventArgs e)
        {
            HideStatus();
        }

        #endregion

        #region Private Methods

        private void ApplyLanguage()
        {
            Text = string.IsNullOrEmpty(_passwordName)
                ? Language.TitlePassword
                : string.Format(Language.TitlePasswordWithName, _passwordName);

            lblPassword.Text = Language.Password;
            lblVerify.Text = Language.Verify;
            btnCancel.Text = Language._Cancel;
            btnOK.Text = Language._Ok;
        }

        // Apply the dark/light title bar before the window is shown to avoid a white flash.
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ThemeManager.getInstance().ApplyThemeToTitleBar(this);
        }

        private void ApplyTheme()
        {
            ThemeManager.getInstance().ApplyThemeToTitleBar(this);
            if (!ThemeManager.getInstance().ActiveAndExtended)
                return;

            ThemeInfo activeTheme = ThemeManager.getInstance().ActiveTheme;

            BackColor = activeTheme.ExtendedPalette.getColor("Dialog_Background");
            ForeColor = activeTheme.ExtendedPalette.getColor("Dialog_Foreground");
            tableLayoutPanel1.BackColor = BackColor;
        }

        // Size the dialog to its visible rows (the verify box is hidden when only asking for a password).
        private void FitToContent()
        {
            Size preferred = tableLayoutPanel1.GetPreferredSize(new Size(ClientSize.Width, 0));
            if (preferred.Height > 0)
                ClientSize = new Size(ClientSize.Width, preferred.Height);
        }

        // Fluent lock glyph on a rounded accent tile (replaces the old bitmap icon).
        private Bitmap CreateLockTile()
        {
            int size = (int)Math.Round(48 * DeviceDpi / 96f);
            bool themed = ModernControlColors.Enabled;
            ModernControlColors? c = themed ? ModernControlColors.Current : null;
            Color tile = c != null ? ModernGraphics.Blend(c.Background, c.Accent, c.IsDark ? 0.22f : 0.14f) : SystemColors.ControlLight;
            Color glyph = c?.Accent ?? SystemColors.HotTrack;
            if (c is { IsDark: true }) glyph = ModernGraphics.Blend(c.Accent, Color.White, 0.35f);

            Bitmap bitmap = new(size, size);
            using Graphics g = Graphics.FromImage(bitmap);
            g.Clear(Color.Transparent);
            ModernGraphics.FillRoundedRectangle(g, tile, new RectangleF(0, 0, size, size), size / 4f);
            int inner = (int)(size * 0.5f);
            FluentIcons.Draw(g, FluentIcons.Lock, new Rectangle((size - inner) / 2, (size - inner) / 2, inner, inner), glyph, inner * 0.9f);
            return bitmap;
        }

        // ReSharper disable once UnusedMethodReturnValue.Local
        private bool VerifyNewPassword()
        {
            if (txtPassword.Text.Length >= 3)
            {
                if (txtPassword.Text == txtVerify.Text)
                    return true;
                ShowStatus(Language.PasswordStatusMustMatch);
                return false;
            }

            ShowStatus(Language.PasswordStatusTooShort);
            return false;
        }

        private void ShowStatus(string status)
        {
            lblStatus.ForeColor = ModernControlColors.Enabled ? ModernPalette.Current.Danger : Color.Red;
            lblStatus.Visible = true;
            lblStatus.Text = status;
        }

        private void HideStatus()
        {
            lblStatus.Visible = false;
        }

        #endregion
    }
}