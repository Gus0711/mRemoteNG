namespace mRemoteNG.UI.Forms
{
    public partial class frmAbout
    {
        #region  Windows Form Designer generated code
        private void InitializeComponent()
        {
            pbLogo = new System.Windows.Forms.PictureBox();
            pnlBottom = new System.Windows.Forms.Panel();
            flpLinks = new System.Windows.Forms.FlowLayoutPanel();
            llChangelog = new System.Windows.Forms.LinkLabel();
            llCredits = new System.Windows.Forms.LinkLabel();
            llLicense = new System.Windows.Forms.LinkLabel();
            lblTitle = new Controls.MrngLabel();
            lblVersion = new Controls.MrngLabel();
            lblLicense = new Controls.MrngLabel();
            lblCopyright = new Controls.MrngLabel();
            ((System.ComponentModel.ISupportInitialize)pbLogo).BeginInit();
            pnlBottom.SuspendLayout();
            flpLinks.SuspendLayout();
            SuspendLayout();
            //
            // pbLogo
            //
            pbLogo.BackColor = System.Drawing.Color.FromArgb(32, 32, 36);
            pbLogo.Dock = System.Windows.Forms.DockStyle.Top;
            pbLogo.Location = new System.Drawing.Point(0, 0);
            pbLogo.Name = "pbLogo";
            pbLogo.Size = new System.Drawing.Size(584, 150);
            pbLogo.TabIndex = 1;
            pbLogo.TabStop = false;
            pbLogo.Paint += PbLogo_Paint;
            pbLogo.Resize += PbLogo_Resize;
            //
            // pnlBottom
            //
            pnlBottom.BackColor = System.Drawing.SystemColors.Control;
            pnlBottom.Controls.Add(flpLinks);
            pnlBottom.Controls.Add(lblTitle);
            pnlBottom.Controls.Add(lblVersion);
            pnlBottom.Controls.Add(lblLicense);
            pnlBottom.Controls.Add(lblCopyright);
            pnlBottom.Dock = System.Windows.Forms.DockStyle.Fill;
            pnlBottom.ForeColor = System.Drawing.SystemColors.ControlText;
            pnlBottom.Location = new System.Drawing.Point(0, 150);
            pnlBottom.Name = "pnlBottom";
            pnlBottom.Size = new System.Drawing.Size(584, 200);
            pnlBottom.TabIndex = 1;
            //
            // flpLinks
            //
            flpLinks.AutoSize = true;
            flpLinks.Controls.Add(llChangelog);
            flpLinks.Controls.Add(llCredits);
            flpLinks.Controls.Add(llLicense);
            flpLinks.Location = new System.Drawing.Point(28, 148);
            flpLinks.Name = "flpLinks";
            flpLinks.Size = new System.Drawing.Size(300, 25);
            flpLinks.TabIndex = 8;
            flpLinks.WrapContents = false;
            //
            // llChangelog
            //
            llChangelog.AutoSize = true;
            llChangelog.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            llChangelog.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            llChangelog.Location = new System.Drawing.Point(3, 3);
            llChangelog.Margin = new System.Windows.Forms.Padding(3, 3, 18, 3);
            llChangelog.Name = "llChangelog";
            llChangelog.Size = new System.Drawing.Size(71, 17);
            llChangelog.TabIndex = 9;
            llChangelog.TabStop = true;
            llChangelog.Text = "Changelog";
            llChangelog.LinkClicked += llChangelog_LinkClicked;
            //
            // llCredits
            //
            llCredits.AutoSize = true;
            llCredits.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            llCredits.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            llCredits.Location = new System.Drawing.Point(95, 3);
            llCredits.Margin = new System.Windows.Forms.Padding(3, 3, 18, 3);
            llCredits.Name = "llCredits";
            llCredits.Size = new System.Drawing.Size(49, 17);
            llCredits.TabIndex = 10;
            llCredits.TabStop = true;
            llCredits.Text = "Credits";
            llCredits.LinkClicked += llCredits_LinkClicked;
            //
            // llLicense
            //
            llLicense.AutoSize = true;
            llLicense.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            llLicense.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            llLicense.Location = new System.Drawing.Point(165, 3);
            llLicense.Name = "llLicense";
            llLicense.Size = new System.Drawing.Size(50, 17);
            llLicense.TabIndex = 11;
            llLicense.TabStop = true;
            llLicense.Text = "License";
            llLicense.LinkClicked += llLicense_LinkClicked;
            //
            // lblTitle
            //
            lblTitle.AutoSize = true;
            lblTitle.Font = new System.Drawing.Font("Segoe UI", 12.75F, System.Drawing.FontStyle.Bold);
            lblTitle.Location = new System.Drawing.Point(30, 18);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new System.Drawing.Size(149, 23);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Fructus temporum";
            //
            // lblVersion
            //
            lblVersion.AutoSize = true;
            lblVersion.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            lblVersion.Location = new System.Drawing.Point(31, 48);
            lblVersion.Name = "lblVersion";
            lblVersion.SecondaryText = true;
            lblVersion.Size = new System.Drawing.Size(49, 17);
            lblVersion.TabIndex = 1;
            lblVersion.Text = "Version";
            //
            // lblLicense
            //
            lblLicense.AutoSize = true;
            lblLicense.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            lblLicense.Location = new System.Drawing.Point(31, 108);
            lblLicense.Name = "lblLicense";
            lblLicense.SecondaryText = true;
            lblLicense.Size = new System.Drawing.Size(48, 17);
            lblLicense.TabIndex = 5;
            lblLicense.Text = "License";
            //
            // lblCopyright
            //
            lblCopyright.AutoSize = true;
            lblCopyright.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            lblCopyright.Location = new System.Drawing.Point(31, 84);
            lblCopyright.Name = "lblCopyright";
            lblCopyright.SecondaryText = true;
            lblCopyright.Size = new System.Drawing.Size(63, 17);
            lblCopyright.TabIndex = 2;
            lblCopyright.Text = "Copyright";
            //
            // frmAbout
            //
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            BackColor = System.Drawing.SystemColors.Control;
            ClientSize = new System.Drawing.Size(584, 350);
            Controls.Add(pnlBottom);
            Controls.Add(pbLogo);
            Font = new System.Drawing.Font("Segoe UI", 8.25F);
            ForeColor = System.Drawing.SystemColors.ControlText;
            Name = "frmAbout";
            Text = "About";
            TabText = "About";
            ((System.ComponentModel.ISupportInitialize)pbLogo).EndInit();
            pnlBottom.ResumeLayout(false);
            pnlBottom.PerformLayout();
            flpLinks.ResumeLayout(false);
            flpLinks.PerformLayout();
            ResumeLayout(false);
        }
        #endregion

        internal Controls.MrngLabel lblCopyright;
        internal Controls.MrngLabel lblTitle;
        internal Controls.MrngLabel lblVersion;
        internal Controls.MrngLabel lblLicense;
        internal System.Windows.Forms.Panel pnlBottom;
        internal System.Windows.Forms.PictureBox pbLogo;
        private System.Windows.Forms.FlowLayoutPanel flpLinks;
        private System.Windows.Forms.LinkLabel llCredits;
        private System.Windows.Forms.LinkLabel llChangelog;
        private System.Windows.Forms.LinkLabel llLicense;
    }
}
