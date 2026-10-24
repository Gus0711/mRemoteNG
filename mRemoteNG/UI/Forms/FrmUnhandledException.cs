using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Windows.Forms;
using mRemoteNG.App;
using mRemoteNG.App.Info;
using mRemoteNG.Resources.Language;
using mRemoteNG.Themes;
using mRemoteNG.UI.Controls;

namespace mRemoteNG.UI.Forms
{
    [SupportedOSPlatform("windows")]
    public partial class FrmUnhandledException : Form
    {
        private readonly bool _isFatal;

        public FrmUnhandledException()
            : this(null, false)
        {
        }

        public FrmUnhandledException(Exception exception, bool isFatal)
        {
            _isFatal = isFatal;
            InitializeComponent();
            SetLanguage();
            ApplyTheme();

            if (exception == null)
                return;

            textBoxExceptionMessage.Text = exception.Message;
            textBoxStackTrace.Text = exception.StackTrace;
            SetEnvironmentText();
        }

        // Apply the dark/light title bar before the window is shown to avoid a white flash.
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                ThemeManager.getInstance().ApplyThemeToTitleBar(this);
            }
            catch (Exception)
            {
                // never let theming break the crash report
            }
        }

        // Best effort only: this dialog is shown when something went wrong, possibly in the theme code.
        private void ApplyTheme()
        {
            try
            {
                buttonClose.Primary = true;
                labelExceptionCaught.Font = ModernFonts.CreateDisplaySemibold(14f);
                if (!ModernControlColors.Enabled) return;
                ModernControlColors c = ModernControlColors.Current;
                BackColor = c.Background;
                ForeColor = c.Text;
                tableLayoutPanel1.BackColor = c.Background;
                labelExceptionCaught.ForeColor = c.Text;
                labelExceptionIsFatalHeader.ForeColor = ModernPalette.Current.Danger;
                foreach (Label header in new[] { labelExceptionMessageHeader, labelStackTraceHeader, labelEnvironment })
                {
                    header.ForeColor = c.TextSecondary;
                    header.Font = ModernFonts.CreateSemibold(8.25f);
                }
            }
            catch (Exception)
            {
                // keep the default look
            }
        }

        private void SetEnvironmentText()
        {
            textBoxEnvironment.Text = new StringBuilder()
                .AppendLine($"OS: {Environment.OSVersion}")
                .AppendLine($"{GeneralAppInfo.ProductName} Version: {GeneralAppInfo.ApplicationVersion}")
                .AppendLine("Edition: " + (Runtime.IsPortableEdition ? "Portable" : "MSI"))
                .AppendLine("Cmd line args: " + string.Join(" ", Environment.GetCommandLineArgs().Skip(1)))
                .ToString();
        }

        private void SetLanguage()
        {
            Text = Language.mRemoteNGUnhandledException;
            labelExceptionCaught.Text = Language.UnhandledExceptionOccured;

            labelExceptionIsFatalHeader.Text = _isFatal
                ? Language.ExceptionForcesmRemoteNGToClose
                : string.Empty;

            labelExceptionMessageHeader.Text = Language.ExceptionMessage;
            labelStackTraceHeader.Text = Language.StackTrace;
            labelEnvironment.Text = Language.Environment;
            buttonCreateBug.Text = Language.MenuItem_ReportIssue;
            buttonCopyAll.Text = Language.CopyAll;
            buttonClose.Text = _isFatal
                ? Language.Exit
                : Language._Close;
        }

        private void buttonCopyAll_Click(object sender, EventArgs e)
        {
            string text = new StringBuilder()
               .AppendLine("```")
               .AppendLine(labelExceptionMessageHeader.Text)
               .AppendLine("\"" + textBoxExceptionMessage.Text + "\"")
               .AppendLine()
               .AppendLine(labelStackTraceHeader.Text)
               .AppendLine(textBoxStackTrace.Text)
               .AppendLine()
               .AppendLine(labelEnvironment.Text)
               .AppendLine(textBoxEnvironment.Text)
               .AppendLine("```")
               .ToString();

            Clipboard.SetText(text);
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            if (_isFatal)
                Shutdown.Quit();

            Close();
        }

        private void buttonCreateBug_Click(object sender, EventArgs e)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = GeneralAppInfo.UrlBugs,
                UseShellExecute = true
            };
            Process.Start(startInfo);
        }
    }
}