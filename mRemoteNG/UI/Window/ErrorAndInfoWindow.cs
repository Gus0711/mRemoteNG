using System;
using System.Drawing;
using System.Collections;
using System.Globalization;
using System.Windows.Forms;
using System.Text;
using WeifenLuo.WinFormsUI.Docking;
using mRemoteNG.App;
using mRemoteNG.Messages;
using mRemoteNG.UI.Forms;
using mRemoteNG.Themes;
using mRemoteNG.UI.Controls.ConnectionTree;
using mRemoteNG.UI.GraphicsUtilities;
using mRemoteNG.Resources.Language;
using Message = mRemoteNG.Messages.Message;
using System.Runtime.Versioning;

namespace mRemoteNG.UI.Window
{
    [SupportedOSPlatform("windows")]
    public partial class ErrorAndInfoWindow : BaseWindow
    {
        private ControlLayout _layout = ControlLayout.Vertical;
        private readonly ThemeManager _themeManager;
        private readonly DisplayProperties _display;

        public DockContent PreviousActiveForm { get; set; }

        public ErrorAndInfoWindow() : this(new DockContent())
        {
        }

        public ErrorAndInfoWindow(DockContent panel)
        {
            WindowType = WindowType.ErrorsAndInfos;
            DockPnl = panel;
            _display = new DisplayProperties();
            InitializeComponent();
            Icon = Resources.ImageConverter.GetImageAsIcon(Properties.Resources.StatusInformation_16x);
            lblMsgDate.Width = _display.ScaleWidth(lblMsgDate.Width);
            _themeManager = ThemeManager.getInstance();
            ApplyTheme();
            _themeManager.ThemeChanged += ApplyTheme;
            LayoutVertical();
            FillImageList();
            SetupModernList();
            ApplyLanguage();
        }

        #region Form Stuff

        private void ErrorsAndInfos_Load(object sender, EventArgs e)
        {
        }

        internal void RefreshUiLanguage() => ApplyLanguage();

        private void ApplyLanguage()
        {
            clmMessage.Text = Language.Message;
            cMenMCCopy.Text = Language.CopyAll;
            cMenMCDelete.Text = Language.DeleteAll;
            TabText = Language.Notifications;
            Text = Language.Notifications;
        }

        #endregion

        #region Private Methods

        private new void ApplyTheme()
        {
            if (lvErrorCollector.IsHandleCreated)
                NativeScrollBarTheme.Apply(lvErrorCollector);
            if (!_themeManager.ActiveAndExtended) return;
            if (UseModernStyle)
            {
                ApplyModernTheme();
                return;
            }

            lvErrorCollector.BackColor = _themeManager.ActiveTheme.ExtendedPalette.getColor("TextBox_Background");
            lvErrorCollector.ForeColor = _themeManager.ActiveTheme.ExtendedPalette.getColor("TextBox_Foreground");

            pnlErrorMsg.BackColor = _themeManager.ActiveTheme.ExtendedPalette.getColor("Dialog_Background");
            pnlErrorMsg.ForeColor = _themeManager.ActiveTheme.ExtendedPalette.getColor("Dialog_Foreground");
            txtMsgText.BackColor = _themeManager.ActiveTheme.ExtendedPalette.getColor("TextBox_Background");
            txtMsgText.ForeColor = _themeManager.ActiveTheme.ExtendedPalette.getColor("TextBox_Foreground");
            lblMsgDate.BackColor = _themeManager.ActiveTheme.ExtendedPalette.getColor("Dialog_Background");
            lblMsgDate.ForeColor = _themeManager.ActiveTheme.ExtendedPalette.getColor("Dialog_Foreground");
        }

        private void FillImageList()
        {
            // The images only reserve the row height: rows are owner drawn with severity glyphs.
            imgListMC.ImageSize = new Size(1, _display.ScaleHeight(ModernRowHeight));
            for (int i = 0; i < 4; i++)
                imgListMC.Images.Add(new Bitmap(1, imgListMC.ImageSize.Height));
        }

        #region Modern list

        private const int ModernRowHeight = 30;

        // Segoe Fluent Icons code points of the severity badges.
        private const char GlyphDebug = '\uE9D9';
        private const char GlyphInfo = '\uF167';
        private const char GlyphWarning = '\uE814';
        private const char GlyphError = '\uEB90';

        private int _hotIndex = -1;

        private bool UseModernStyle => _themeManager.ActiveAndExtended;

        private void SetupModernList()
        {
            lvErrorCollector.OwnerDraw = true;
            lvErrorCollector.DrawItem += LvErrorCollector_DrawItem;
            lvErrorCollector.DrawSubItem += (_, e) => e.DrawDefault = false;
            lvErrorCollector.DrawColumnHeader += (_, e) => e.DrawDefault = true;
            lvErrorCollector.MouseMove += LvErrorCollector_MouseMove;
            lvErrorCollector.MouseLeave += (_, _) => SetHotIndex(-1);
            lvErrorCollector.HandleCreated += (_, _) => NativeScrollBarTheme.Apply(lvErrorCollector);
            lvErrorCollector.Font = SidebarFonts.Regular;
            txtMsgText.Font = SidebarFonts.Regular;
            lblMsgDate.Font = SidebarFonts.Small;
            // MrngTextBox / MrngLabel apply the legacy dialog colors when their handle is created,
            // so re-apply the flat colors right after that.
            txtMsgText.HandleCreated += (_, _) => ReapplyModernThemeLater(txtMsgText);
            lblMsgDate.HandleCreated += (_, _) => ReapplyModernThemeLater(lblMsgDate);
            pbError.HandleCreated += (_, _) => ReapplyModernThemeLater(pbError);
        }

        private void ReapplyModernThemeLater(Control control)
        {
            NativeScrollBarTheme.Apply(control);
            control.BeginInvoke(() =>
            {
                if (IsDisposed || !UseModernStyle) return;
                if (lvErrorCollector.SelectedItems.Count == 1 && lvErrorCollector.SelectedItems[0].Tag is IMessage message)
                    ShowModernMessageDetails(message);
                else
                    ApplyModernTheme();
            });
        }

        private void ApplyModernTheme()
        {
            ModernPalette palette = ModernPalette.Current;
            Color background = _themeManager.ActiveTheme.ExtendedPalette.getColor("TreeView_Background");
            BackColor = background;
            lvErrorCollector.BackColor = background;
            lvErrorCollector.ForeColor = palette.Text;
            pnlErrorMsg.BackColor = background;
            pnlErrorMsg.ForeColor = palette.Text;
            txtMsgText.BackColor = background;
            txtMsgText.ForeColor = palette.Text;
            lblMsgDate.BackColor = background;
            lblMsgDate.ForeColor = palette.TextSecondary;
            pbError.BackColor = background;
            NativeScrollBarTheme.Apply(txtMsgText);
            lvErrorCollector.Invalidate();
            if (lvErrorCollector.SelectedItems.Count == 1)
                lvErrorCollector_SelectedIndexChanged(lvErrorCollector, EventArgs.Empty);
        }

        private static (char Glyph, Color Color) GetSeverityStyle(MessageClass messageClass, ModernPalette palette) =>
            messageClass switch
            {
                MessageClass.ErrorMsg => (GlyphError, palette.Danger),
                MessageClass.WarningMsg => (GlyphWarning, palette.Warning),
                MessageClass.InformationMsg => (GlyphInfo, palette.Accent),
                _ => (GlyphDebug, palette.TextSecondary)
            };

        private void LvErrorCollector_MouseMove(object sender, MouseEventArgs e)
        {
            ListViewItem item = lvErrorCollector.GetItemAt(e.X, e.Y);
            SetHotIndex(item?.Index ?? -1);
        }

        private void SetHotIndex(int index)
        {
            if (index == _hotIndex) return;
            int previous = _hotIndex;
            _hotIndex = index;
            if (previous >= 0 && previous < lvErrorCollector.Items.Count)
                lvErrorCollector.Invalidate(lvErrorCollector.Items[previous].Bounds);
            if (index >= 0 && index < lvErrorCollector.Items.Count)
                lvErrorCollector.Invalidate(lvErrorCollector.Items[index].Bounds);
        }

        private void LvErrorCollector_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            if (!UseModernStyle)
            {
                e.DrawDefault = true;
                return;
            }

            Graphics g = e.Graphics;
            ModernPalette palette = ModernPalette.Current;
            int Scale(int v) => (int)Math.Round(v * lvErrorCollector.DeviceDpi / 96f);

            Rectangle bounds = new(0, e.Bounds.Y, Math.Min(e.Bounds.Right, lvErrorCollector.ClientSize.Width), e.Bounds.Height);
            using (SolidBrush back = new(lvErrorCollector.BackColor))
                g.FillRectangle(back, bounds);

            Rectangle row = Rectangle.Inflate(bounds, -Scale(6), -Scale(1));
            float radius = Scale(ModernPalette.CornerRadius) - 1;
            if (e.Item.Selected)
                ModernGraphics.FillRoundedRectangle(g, lvErrorCollector.Focused ? palette.Selection : ModernGraphics.Blend(palette.Selection, lvErrorCollector.BackColor, 0.45f), row, radius);
            else if (e.ItemIndex == _hotIndex)
                ModernGraphics.FillRoundedRectangle(g, palette.SurfaceHover, row, radius);

            if (e.Item.Tag is not IMessage message)
                return;

            (char glyph, Color color) = GetSeverityStyle(message.Class, palette);
            Rectangle glyphBounds = new(row.X + Scale(8), row.Y, Scale(18), row.Height);
            FluentIcons.Draw(g, glyph, glyphBounds, color, Scale(14));

            string time = message.Date.ToString("HH:mm:ss", CultureInfo.CurrentCulture);
            Font font = lvErrorCollector.Font;
            const TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                                          TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
            int timeWidth = TextRenderer.MeasureText(g, time, font, Size.Empty, flags).Width;
            Rectangle timeBounds = new(row.Right - timeWidth - Scale(12), row.Y, timeWidth + Scale(2), row.Height);
            TextRenderer.DrawText(g, time, font, timeBounds, palette.TextSecondary, flags | TextFormatFlags.Right);

            string text = message.Text ?? string.Empty;
            int lineBreak = text.IndexOfAny(new[] { '\r', '\n' });
            if (lineBreak >= 0)
                text = text.Substring(0, lineBreak);
            int textLeft = glyphBounds.Right + Scale(8);
            Rectangle textBounds = new(textLeft, row.Y, Math.Max(0, timeBounds.Left - Scale(12) - textLeft), row.Height);
            TextRenderer.DrawText(g, text, font, textBounds, palette.Text, flags | TextFormatFlags.EndEllipsis);
        }

        private void ShowModernMessageDetails(IMessage message)
        {
            ModernPalette palette = ModernPalette.Current;
            (char glyph, Color color) = GetSeverityStyle(message.Class, palette);
            pbError.Image = FluentIcons.Get(glyph, _display.ScaleHeight(22), color);
            Color background = _themeManager.ActiveTheme.ExtendedPalette.getColor("TreeView_Background");
            pnlErrorMsg.BackColor = background;
            pnlErrorMsg.ForeColor = palette.Text;
            txtMsgText.BackColor = background;
            txtMsgText.ForeColor = palette.Text;
            lblMsgDate.BackColor = background;
            lblMsgDate.ForeColor = palette.TextSecondary;
            pbError.BackColor = background;
        }

        #endregion

        private void LayoutVertical()
        {
            try
            {
                pnlErrorMsg.Location = new Point(0, Height - _display.ScaleHeight(200));
                pnlErrorMsg.Size = new Size(Width, Height - pnlErrorMsg.Top);
                pnlErrorMsg.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                txtMsgText.Size = new Size(
                                           pnlErrorMsg.Width - pbError.Width - _display.ScaleWidth(8),
                                           pnlErrorMsg.Height - _display.ScaleHeight(20));
                lvErrorCollector.Location = new Point(0, 0);
                lvErrorCollector.Size = new Size(Width, Height - pnlErrorMsg.Height - _display.ScaleHeight(5));
                lvErrorCollector.Anchor =
                    AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;

                _layout = ControlLayout.Vertical;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddMessage(MessageClass.ErrorMsg,
                                                    "LayoutVertical (UI.Window.ErrorsAndInfos) failed" +
                                                    Environment.NewLine + ex.Message, true);
            }
        }

        private void LayoutHorizontal()
        {
            try
            {
                pnlErrorMsg.Location = new Point(0, 0);
                pnlErrorMsg.Size = new Size(_display.ScaleWidth(200), Height);
                pnlErrorMsg.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Top;

                txtMsgText.Size = new Size(
                                           pnlErrorMsg.Width - pbError.Width - _display.ScaleWidth(8),
                                           pnlErrorMsg.Height - _display.ScaleHeight(20));
                lvErrorCollector.Location = new Point(pnlErrorMsg.Width + _display.ScaleWidth(5), 0);
                lvErrorCollector.Size = new Size(Width - pnlErrorMsg.Width - _display.ScaleWidth(5), Height);
                lvErrorCollector.Anchor =
                    AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;

                _layout = ControlLayout.Horizontal;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddMessage(MessageClass.ErrorMsg,
                                                    "LayoutHorizontal (UI.Window.ErrorsAndInfos) failed" +
                                                    Environment.NewLine + ex.Message, true);
            }
        }

        private void ErrorsAndInfos_Resize(object sender, EventArgs e)
        {
            try
            {
                if (Width > Height)
                {
                    if (_layout == ControlLayout.Vertical)
                        LayoutHorizontal();
                }
                else
                {
                    if (_layout == ControlLayout.Horizontal)
                        LayoutVertical();
                }

                lvErrorCollector.Columns[0].Width = lvErrorCollector.Width - 20;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddMessage(MessageClass.ErrorMsg,
                                                    "ErrorsAndInfos_Resize (UI.Window.ErrorsAndInfos) failed" +
                                                    Environment.NewLine + ex.Message, true);
            }
        }

        private void SetStyleWhenNoMessageSelected()
        {
            try
            {
                pbError.Image = null;
                txtMsgText.Text = "";
                lblMsgDate.Text = "";
                if (UseModernStyle)
                {
                    ApplyModernTheme();
                    return;
                }

                pnlErrorMsg.BackColor = _themeManager.ActiveTheme.ExtendedPalette.getColor("Dialog_Background");
                txtMsgText.BackColor = _themeManager.ActiveTheme.ExtendedPalette.getColor("TextBox_Background");
                lblMsgDate.BackColor = _themeManager.ActiveTheme.ExtendedPalette.getColor("Dialog_Background");
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddMessage(MessageClass.ErrorMsg,
                                                    "pnlErrorMsg_ResetDefaultStyle (UI.Window.ErrorsAndInfos) failed" +
                                                    Environment.NewLine +
                                                    ex.Message, true);
            }
        }

        private void MC_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode != Keys.Escape) return;
                try
                {
                    if (PreviousActiveForm != null)
                        PreviousActiveForm.Show(FrmMain.Default.pnlDock);
                    else
                        AppWindows.TreeForm.Show(FrmMain.Default.pnlDock);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddMessage(MessageClass.ErrorMsg,
                                                    "MC_KeyDown (UI.Window.ErrorsAndInfos) failed" +
                                                    Environment.NewLine + ex.Message, true);
            }
        }

        private void lvErrorCollector_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (lvErrorCollector.SelectedItems.Count == 0 | lvErrorCollector.SelectedItems.Count > 1)
                {
                    SetStyleWhenNoMessageSelected();
                    return;
                }

                ListViewItem sItem = lvErrorCollector.SelectedItems[0];
                Message eMsg = (Message)sItem.Tag;
                if (UseModernStyle)
                {
                    ShowModernMessageDetails(eMsg);
                    lblMsgDate.Text = eMsg.Date.ToString(CultureInfo.InvariantCulture);
                    txtMsgText.Text = eMsg.Text;
                    return;
                }

                switch (eMsg.Class)
                {
                    case MessageClass.DebugMsg:
                        pbError.Image = _display.ScaleImage(Properties.Resources.Test_16x);
                        if (_themeManager.ThemingActive)
                        {
                            pnlErrorMsg.BackColor = Color.LightSteelBlue;
                            txtMsgText.BackColor = Color.LightSteelBlue;
                            lblMsgDate.BackColor = Color.LightSteelBlue;
                        }

                        break;
                    case MessageClass.InformationMsg:
                        pbError.Image = _display.ScaleImage(Properties.Resources.StatusInformation_16x);
                        if (_themeManager.ThemingActive)
                        {
                            pnlErrorMsg.BackColor = Color.LightSteelBlue;
                            txtMsgText.BackColor = Color.LightSteelBlue;
                            lblMsgDate.BackColor = Color.LightSteelBlue;
                        }

                        break;
                    case MessageClass.WarningMsg:
                        pbError.Image = _display.ScaleImage(Properties.Resources.LogWarning_16x);
                        if (_themeManager.ActiveAndExtended)
                        {
                            //Inverse colors for dramatic effect
                            pnlErrorMsg.BackColor =
                                _themeManager.ActiveTheme.ExtendedPalette.getColor("WarningText_Foreground");
                            pnlErrorMsg.ForeColor =
                                _themeManager.ActiveTheme.ExtendedPalette.getColor("WarningText_Background");
                            txtMsgText.BackColor =
                                _themeManager.ActiveTheme.ExtendedPalette.getColor("WarningText_Foreground");
                            txtMsgText.ForeColor =
                                _themeManager.ActiveTheme.ExtendedPalette.getColor("WarningText_Background");
                            lblMsgDate.BackColor =
                                _themeManager.ActiveTheme.ExtendedPalette.getColor("WarningText_Foreground");
                            lblMsgDate.ForeColor =
                                _themeManager.ActiveTheme.ExtendedPalette.getColor("WarningText_Background");
                        }

                        break;
                    case MessageClass.ErrorMsg:
                        pbError.Image = _display.ScaleImage(Properties.Resources.LogError_16x);
                        if (_themeManager.ActiveAndExtended)
                        {
                            pnlErrorMsg.BackColor =
                                _themeManager.ActiveTheme.ExtendedPalette.getColor("ErrorText_Foreground");
                            pnlErrorMsg.ForeColor =
                                _themeManager.ActiveTheme.ExtendedPalette.getColor("ErrorText_Background");
                            txtMsgText.BackColor =
                                _themeManager.ActiveTheme.ExtendedPalette.getColor("ErrorText_Foreground");
                            txtMsgText.ForeColor =
                                _themeManager.ActiveTheme.ExtendedPalette.getColor("ErrorText_Background");
                            lblMsgDate.BackColor =
                                _themeManager.ActiveTheme.ExtendedPalette.getColor("ErrorText_Foreground");
                            lblMsgDate.ForeColor =
                                _themeManager.ActiveTheme.ExtendedPalette.getColor("ErrorText_Background");
                        }

                        break;
                }

                lblMsgDate.Text = eMsg.Date.ToString(CultureInfo.InvariantCulture);
                txtMsgText.Text = eMsg.Text;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddMessage(MessageClass.ErrorMsg,
                                                    "lvErrorCollector_SelectedIndexChanged (UI.Window.ErrorsAndInfos) failed" +
                                                    Environment.NewLine +
                                                    ex.Message, true);
            }
        }

        private void cMenMC_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (lvErrorCollector.Items.Count > 0)
            {
                cMenMCCopy.Enabled = true;
                cMenMCDelete.Enabled = true;
                pbError.Visible = true;
            }
            else
            {
                cMenMCCopy.Enabled = false;
                cMenMCDelete.Enabled = false;
            }

            if (lvErrorCollector.SelectedItems.Count > 0)
            {
                cMenMCCopy.Text = Language.Copy;
                cMenMCDelete.Text = Language.Delete;
            }
            else
            {
                cMenMCCopy.Text = Language.CopyAll;
                cMenMCDelete.Text = Language.DeleteAll;
            }
        }

        private void cMenMCCopy_Click(object sender, EventArgs e)
        {
            CopyMessagesToClipboard();
        }

        private void CopyMessagesToClipboard()
        {
            try
            {
                IEnumerable items;
                if (lvErrorCollector.SelectedItems.Count > 0)
                {
                    items = lvErrorCollector.SelectedItems;
                }
                else
                {
                    items = lvErrorCollector.Items;
                }

                StringBuilder stringBuilder = new();
                stringBuilder.AppendLine("----------");

                lvErrorCollector.BeginUpdate();

                foreach (ListViewItem item in items)
                {
                    if (!(item.Tag is Message message))
                    {
                        continue;
                    }

                    stringBuilder.AppendLine(message.Class.ToString());
                    stringBuilder.AppendLine(message.Date.ToString(CultureInfo.InvariantCulture));
                    stringBuilder.AppendLine(message.Text);
                    stringBuilder.AppendLine("----------");
                }

                Clipboard.SetText(stringBuilder.ToString());
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddMessage(MessageClass.ErrorMsg,
                                                    "UI.Window.ErrorsAndInfos.CopyMessagesToClipboard() failed." +
                                                    Environment.NewLine + ex.Message,
                                                    true);
            }
            finally
            {
                lvErrorCollector.EndUpdate();
            }
        }

        private void cMenMCDelete_Click(object sender, EventArgs e)
        {
            DeleteMessages();
        }

        private void DeleteMessages()
        {
            try
            {
                lvErrorCollector.BeginUpdate();

                if (lvErrorCollector.SelectedItems.Count > 0)
                {
                    foreach (ListViewItem item in lvErrorCollector.SelectedItems)
                        item.Remove();
                }
                else
                {
                    lvErrorCollector.Items.Clear();
                }

                if (lvErrorCollector.Items.Count == 0)
                {
                    pbError.Visible = false;
                    txtMsgText.Visible = false;
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddMessage(MessageClass.ErrorMsg,
                                                    "UI.Window.ErrorsAndInfos.DeleteMessages() failed" +
                                                    Environment.NewLine + ex.Message, true);
            }
            finally
            {
                lvErrorCollector.EndUpdate();
            }
        }

        #endregion

        private enum ControlLayout
        {
            Vertical = 0,
            Horizontal = 1
        }
    }
}