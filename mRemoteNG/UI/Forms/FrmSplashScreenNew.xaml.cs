using System;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using mRemoteNG.App.Info;

namespace mRemoteNG.UI.Forms
{
    [SupportedOSPlatform("windows")]
    /// <summary>
    /// Interaction logic for FrmSplashScreenNew.xaml
    /// Modern splash: rounded card, logo, name, version and a slim indeterminate accent progress bar.
    /// </summary>
    public partial class FrmSplashScreenNew
    {
        static FrmSplashScreenNew instance = null;

        public FrmSplashScreenNew()
        {
            InitializeComponent();
            LoadFont();
            LoadLogo();
            ApplyColors();
            lblLogoPartD.Text = $@"Version {GeneralAppInfo.ApplicationVersion}";
            Loaded += (_, _) =>
            {
                if (TryFindResource("IndeterminateAnimation") is Storyboard animation)
                    animation.Begin(this, true);
            };
        }

        public static FrmSplashScreenNew GetInstance()
        {
            //instance == null
            instance ??= new FrmSplashScreenNew();
            return instance;
        }

        void LoadFont()
        {
            try
            {
                txtLogo.FontFamily = new FontFamily(new Uri("pack://application:,,,/"), "./UI/Font/#HandelGotDBol");
            }
            catch (Exception)
            {
                // keep Segoe UI Semibold
            }
        }

        void LoadLogo()
        {
            try
            {
                using System.Drawing.Icon icon = new(Properties.Resources.mRemoteNG_Icon, 64, 64);
                BitmapSource source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                    icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                source.Freeze();
                imgLogo.Source = source;
            }
            catch (Exception)
            {
                imgLogo.Visibility = Visibility.Collapsed;
            }
        }

        // The splash is shown before the themes are loaded: use the persisted dark/light flag of the last theme.
        void ApplyColors()
        {
            bool dark = true;
            try
            {
                dark = Properties.OptionsThemePage.Default.IsActiveThemeDark;
            }
            catch (Exception)
            {
                // default to dark
            }

            if (dark) return;

            Card.Background = MakeBrush(0xF3, 0xF3, 0xF6);
            Card.BorderBrush = MakeBrush(0xE1, 0xE1, 0xE8);
            txtLogo.Foreground = MakeBrush(0x1B, 0x1B, 0x1F);
            lblLogoPartA.Foreground = MakeBrush(0x4F, 0x5B, 0xD5);
            Brush secondary = MakeBrush(0x6B, 0x6B, 0x76);
            lblLogoPartC.Foreground = secondary;
            lblLogoPartD.Foreground = secondary;
            lblStatus.Foreground = secondary;
            ProgressTrack.Background = MakeBrush(0xE4, 0xE4, 0xEA);
            ProgressIndicator.Background = MakeBrush(0x4F, 0x5B, 0xD5);
        }

        static SolidColorBrush MakeBrush(byte r, byte g, byte b)
        {
            SolidColorBrush brush = new(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }
    }
}
