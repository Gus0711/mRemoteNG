using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using mRemoteNG.UI.Controls;
using mRemoteNG.UI.GraphicsUtilities;
using mRemoteNG.App.Info;
using mRemoteNG.Themes;
using mRemoteNG.Resources.Language;
using System.Reflection;
using mRemoteNG.Properties;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using mRemoteNG.UI.Window;
using mRemoteNG.App;
using mRemoteNG.Messages;

namespace mRemoteNG.UI.Forms
{
    [SupportedOSPlatform("windows")]
    public partial class frmAbout : BaseWindow
    {
        public static frmAbout Instance { get; set; } = new frmAbout();

        public frmAbout()
        {
            InitializeComponent();
            Icon = Resources.ImageConverter.GetImageAsIcon(Properties.Resources.UIAboutBox_16x);
            lblTitle.Font = ModernFonts.CreateDisplaySemibold(13f);
            ThemeManager.getInstance().ThemeChanged += ApplyTheme;
            ApplyLanguage();
            ApplyTheme();
        }

        private void ApplyLanguage()
        {
            lblLicense.Text = Language.ReleasedUnderGPL;
            base.Text = Language.MenuItem_About;
            TabText = Language.MenuItem_About;
            llChangelog.Text = Language.Changelog;
            llCredits.Text = Language.Credits;
            llLicense.Text = Language.License;
            lblCopyright.Text = GeneralAppInfo.Copyright;
            lblVersion.Text = $@"Version {GeneralAppInfo.ApplicationVersion}";
            lblTitle.Text = "Fructus temporum";
            AddPortableString();
        }

        [Conditional("PORTABLE")]
        private void AddPortableString() => lblTitle.Text += $@" {Language.PortableEdition}";

        // Apply the dark/light title bar before the window is shown to avoid a white flash.
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ThemeManager.getInstance().ApplyThemeToTitleBar(this);
        }

        private new void ApplyTheme()
        {
            ThemeManager.getInstance().ApplyThemeToTitleBar(this);
            bool themed = ModernControlColors.Enabled;
            ModernControlColors? c = themed ? ModernControlColors.Current : null;
            Color back = c?.Background ?? SystemColors.Control;
            BackColor = back;
            pnlBottom.BackColor = back;
            pnlBottom.ForeColor = c?.Text ?? SystemColors.ControlText;
            flpLinks.BackColor = back;
            pbLogo.BackColor = back;
            Color accent = c?.Accent ?? SystemColors.HotTrack;
            foreach (LinkLabel link in new[] { llChangelog, llCredits, llLicense })
            {
                link.LinkColor = accent;
                link.VisitedLinkColor = accent;
                link.ActiveLinkColor = c?.AccentPressed ?? SystemColors.Highlight;
                link.BackColor = back;
                if (link.Font.Name != ModernGraphics.UiFontFamily)
                    link.Font = ModernGraphics.CreateUiFont(9.75f);
            }

            pbLogo.Invalidate();
        }

        private void PbLogo_Resize(object? sender, EventArgs e) => pbLogo.Invalidate();

        // Hero banner: soft accent gradient, application icon and the "mRemoteNG" logotype.
        private void PbLogo_Paint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            bool themed = ModernControlColors.Enabled;
            ModernControlColors? c = themed ? ModernControlColors.Current : null;
            Color back = c?.Background ?? SystemColors.Control;
            Color accent = c?.Accent ?? Color.FromArgb(82, 137, 249);
            Color text = c?.Text ?? SystemColors.ControlText;
            Color secondary = c?.TextSecondary ?? SystemColors.GrayText;
            float scale = pbLogo.DeviceDpi / 96f;
            Rectangle bounds = pbLogo.ClientRectangle;
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            using (LinearGradientBrush gradient = new(bounds, ModernGraphics.Blend(back, accent, c?.IsDark == false ? 0.10f : 0.16f), back, 70f))
                g.FillRectangle(gradient, bounds);
            using (Pen separator = new(c?.CardBorder ?? SystemColors.ControlDark))
                g.DrawLine(separator, 0, bounds.Bottom - 1, bounds.Right, bounds.Bottom - 1);

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            int iconSize = (int)Math.Round(64 * scale);
            using Font logoFont = ModernFonts.LogoFontFamily != null
                ? new Font(ModernFonts.LogoFontFamily, 34f * scale, FontStyle.Regular, GraphicsUnit.Pixel)
                : ModernFonts.CreateDisplaySemibold(24f);
            using Font subtitleFont = ModernGraphics.CreateUiFont(9.75f);
            using StringFormat format = new(StringFormat.GenericTypographic) { FormatFlags = StringFormatFlags.NoWrap };

            const string first = "m";
            const string rest = "RemoteNG";
            SizeF firstSize = g.MeasureString(first, logoFont, PointF.Empty, format);
            SizeF restSize = g.MeasureString(rest, logoFont, PointF.Empty, format);
            string subtitle = "Multi-Remote Next Generation Connection Manager";
            SizeF subtitleSize = g.MeasureString(subtitle, subtitleFont, PointF.Empty, format);

            float gap = 18 * scale;
            float textWidth = Math.Max(firstSize.Width + restSize.Width, subtitleSize.Width);
            float totalWidth = iconSize + gap + textWidth;
            float x = 30 * scale; // aligned with the body text below
            float centerY = bounds.Height / 2f;

            Icon? appIcon = AppIcon;
            if (appIcon != null)
            {
                using Icon sized = new(appIcon, iconSize, iconSize);
                using Bitmap bitmap = sized.ToBitmap();
                g.DrawImage(bitmap, new RectangleF(x, centerY - iconSize / 2f, iconSize, iconSize));
            }

            float textX = x + iconSize + gap;
            float logoY = centerY - (firstSize.Height + subtitleSize.Height + 4 * scale) / 2f;
            using (SolidBrush accentBrush = new(accent))
                g.DrawString(first, logoFont, accentBrush, textX, logoY, format);
            using (SolidBrush textBrush = new(text))
                g.DrawString(rest, logoFont, textBrush, textX + firstSize.Width, logoY, format);
            using (SolidBrush secondaryBrush = new(secondary))
                g.DrawString(subtitle, subtitleFont, secondaryBrush, textX + 2 * scale, logoY + firstSize.Height + 4 * scale, format);
        }

        private static Icon? _appIcon;

        private static Icon? AppIcon
        {
            get
            {
                if (_appIcon != null) return _appIcon;
                try
                {
                    // multi-resolution application icon (contains 64 and 256 px images)
                    _appIcon = Properties.Resources.mRemoteNG_Icon;
                }
                catch (Exception)
                {
                    _appIcon = null;
                }

                return _appIcon;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            
            // Don't cancel close when shown in DockPanel
            // This allows the tab to close properly without showing connection close dialog
        }

        private void llLicense_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var updateChannel = Properties.OptionsUpdatesPage.Default.CurrentUpdateChannelType;
            if (version != null && updateChannel != null)
            {
                var versionString = version.ToString();
                OpenUrl("https://raw.githubusercontent.com/mRemoteNG/mRemoteNG/v" + versionString[..^2] + "-" + updateChannel + "/COPYING.txt");
            }
            Close();
        }

        private void llChangelog_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var updateChannel = Properties.OptionsUpdatesPage.Default.CurrentUpdateChannelType;
            if (version != null && updateChannel != null)
            {
                var versionString = version.ToString();
                OpenUrl("https://raw.githubusercontent.com/mRemoteNG/mRemoteNG/v" + versionString[..^2] + "-" + updateChannel + "/CHANGELOG.md");
            }
            Close();
        }

        private void llCredits_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var updateChannel = Properties.OptionsUpdatesPage.Default.CurrentUpdateChannelType;
            if (version != null && updateChannel != null)
            {
                var versionString = version.ToString();
                OpenUrl("https://raw.githubusercontent.com/mRemoteNG/mRemoteNG/v" + versionString[..^2] + "-" + updateChannel + "/CREDITS.md");
            }
            Close();
        }

        private static void OpenUrl(string url)
        {
            // Validate URL format to prevent injection
            if (string.IsNullOrWhiteSpace(url))
                return;
            
            // Basic URL validation - ensure it starts with http:// or https://
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                // Invalid URL format - don't try to open it
                return;
            }
            
            try
            {
                // Use the standard .NET approach for opening URLs securely
                // UseShellExecute=true delegates to the OS default handler
                var startInfo = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                Process.Start(startInfo);
            }
            catch
            {
                // Fallback for older .NET Core versions with bug: https://github.com/dotnet/corefx/issues/10361
                // Use platform-specific URL launchers
                try
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        // Use rundll32 with url.dll as fallback
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = "rundll32.exe",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        startInfo.ArgumentList.Add("url.dll,FileProtocolHandler");
                        startInfo.ArgumentList.Add(url);
                        Process.Start(startInfo);
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = "xdg-open",
                            UseShellExecute = false
                        };
                        startInfo.ArgumentList.Add(url);
                        Process.Start(startInfo);
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = "open",
                            UseShellExecute = false
                        };
                        startInfo.ArgumentList.Add(url);
                        Process.Start(startInfo);
                    }
                }
                catch
                {
                    // Unable to open URL - notify the user
                    Runtime.MessageCollector?.AddMessage(MessageClass.WarningMsg, 
                        "Unable to open URL in browser. Please open manually: " + url, true);
                }
            }
        }
    }
}