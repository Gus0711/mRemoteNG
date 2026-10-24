using System.Windows;
using System.Windows.Forms;
using mRemoteNG.Themes;
using mRemoteNG.UI.Controls;

namespace mRemoteNG.UI.Forms
{
    partial class FrmOptions
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_isInitialized)
                {
                    ThemeManager.getInstance().ThemeChanged -= ApplyTheme;
                }

                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.pnlBottom = new System.Windows.Forms.Panel();
            this.btnApply = new mRemoteNG.UI.Controls.MrngButton();
            this.btnCancel = new mRemoteNG.UI.Controls.MrngButton();
            this.btnOK = new mRemoteNG.UI.Controls.MrngButton();
            this.pnlNav = new System.Windows.Forms.Panel();
            this.lstOptionPages = new mRemoteNG.UI.Controls.MrngListView();
            this.PageName = new BrightIdeasSoftware.OLVColumn();
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.lblPageTitle = new System.Windows.Forms.Label();
            this.pnlMain = new System.Windows.Forms.Panel();
            this.pnlBottom.SuspendLayout();
            this.pnlNav.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.lstOptionPages)).BeginInit();
            this.pnlHeader.SuspendLayout();
            this.SuspendLayout();
            //
            // pnlBottom
            //
            this.pnlBottom.Controls.Add(this.btnApply);
            this.pnlBottom.Controls.Add(this.btnCancel);
            this.pnlBottom.Controls.Add(this.btnOK);
            this.pnlBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlBottom.Location = new System.Drawing.Point(0, 665);
            this.pnlBottom.Name = "pnlBottom";
            this.pnlBottom.Size = new System.Drawing.Size(1075, 60);
            this.pnlBottom.TabIndex = 0;
            this.pnlBottom.Paint += new System.Windows.Forms.PaintEventHandler(this.PnlBottom_Paint);
            //
            // btnApply
            //
            this.btnApply._mice = mRemoteNG.UI.Controls.MrngButton.MouseState.OUT;
            this.btnApply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnApply.Location = new System.Drawing.Point(955, 14);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(96, 32);
            this.btnApply.TabIndex = 2;
            this.btnApply.Text = "Apply";
            this.btnApply.UseVisualStyleBackColor = true;
            this.btnApply.Click += new System.EventHandler(this.BtnApply_Click);
            //
            // btnCancel
            //
            this.btnCancel._mice = mRemoteNG.UI.Controls.MrngButton.MouseState.OUT;
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.Location = new System.Drawing.Point(851, 14);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(96, 32);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            //
            // btnOK
            //
            this.btnOK._mice = mRemoteNG.UI.Controls.MrngButton.MouseState.OUT;
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.Location = new System.Drawing.Point(747, 14);
            this.btnOK.Name = "btnOK";
            this.btnOK.Primary = true;
            this.btnOK.Size = new System.Drawing.Size(96, 32);
            this.btnOK.TabIndex = 0;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.BtnOK_Click);
            //
            // pnlNav
            //
            this.pnlNav.Controls.Add(this.lstOptionPages);
            this.pnlNav.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlNav.Location = new System.Drawing.Point(0, 0);
            this.pnlNav.Name = "pnlNav";
            this.pnlNav.Padding = new System.Windows.Forms.Padding(8, 16, 4, 8);
            this.pnlNav.Size = new System.Drawing.Size(232, 665);
            this.pnlNav.TabIndex = 2;
            //
            // lstOptionPages
            //
            this.lstOptionPages.AllColumns.Add(this.PageName);
            this.lstOptionPages.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lstOptionPages.CellEditUseWholeCell = false;
            this.lstOptionPages.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.PageName});
            this.lstOptionPages.DecorateLines = false;
            this.lstOptionPages.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstOptionPages.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lstOptionPages.FullRowSelect = true;
            this.lstOptionPages.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.lstOptionPages.LabelWrap = false;
            this.lstOptionPages.Location = new System.Drawing.Point(8, 16);
            this.lstOptionPages.MultiSelect = false;
            this.lstOptionPages.Name = "lstOptionPages";
            this.lstOptionPages.OwnerDraw = true;
            this.lstOptionPages.RowHeight = 38;
            this.lstOptionPages.ShowGroups = false;
            this.lstOptionPages.Size = new System.Drawing.Size(220, 641);
            this.lstOptionPages.TabIndex = 1;
            this.lstOptionPages.UseCompatibleStateImageBehavior = false;
            this.lstOptionPages.UseHotControls = true;
            this.lstOptionPages.View = System.Windows.Forms.View.Details;
            //
            // PageName
            //
            this.PageName.AspectName = "PageName";
            this.PageName.FillsFreeSpace = true;
            this.PageName.Groupable = false;
            this.PageName.IsEditable = false;
            //
            // pnlHeader
            //
            this.pnlHeader.Controls.Add(this.lblPageTitle);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Location = new System.Drawing.Point(232, 0);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Padding = new System.Windows.Forms.Padding(20, 14, 24, 4);
            this.pnlHeader.Size = new System.Drawing.Size(843, 62);
            this.pnlHeader.TabIndex = 3;
            //
            // lblPageTitle
            //
            this.lblPageTitle.AutoEllipsis = true;
            this.lblPageTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPageTitle.Font = new System.Drawing.Font("Segoe UI", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblPageTitle.Location = new System.Drawing.Point(20, 14);
            this.lblPageTitle.Name = "lblPageTitle";
            this.lblPageTitle.Size = new System.Drawing.Size(799, 44);
            this.lblPageTitle.TabIndex = 0;
            this.lblPageTitle.Text = "General";
            this.lblPageTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblPageTitle.UseMnemonic = false;
            //
            // pnlMain
            //
            this.pnlMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlMain.Location = new System.Drawing.Point(232, 62);
            this.pnlMain.Name = "pnlMain";
            this.pnlMain.Padding = new System.Windows.Forms.Padding(18, 0, 16, 8);
            this.pnlMain.Size = new System.Drawing.Size(843, 603);
            this.pnlMain.TabIndex = 4;
            //
            // FrmOptions
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(1075, 725);
            this.Controls.Add(this.pnlMain);
            this.Controls.Add(this.pnlHeader);
            this.Controls.Add(this.pnlNav);
            this.Controls.Add(this.pnlBottom);
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmOptions";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "mRemoteNG Options";
            this.Load += new System.EventHandler(this.FrmOptions_Load);
            this.Shown += new System.EventHandler(this.FrmOptions_Shown);
            this.pnlBottom.ResumeLayout(false);
            this.pnlNav.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.lstOptionPages)).EndInit();
            this.pnlHeader.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlBottom;
        private Controls.MrngListView lstOptionPages;
        private System.Windows.Forms.Panel pnlNav;
        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Label lblPageTitle;
        private System.Windows.Forms.Panel pnlMain;
        private MrngButton btnOK;
        private MrngButton btnCancel;
        private BrightIdeasSoftware.OLVColumn PageName;
        private MrngButton btnApply;
    }
}
