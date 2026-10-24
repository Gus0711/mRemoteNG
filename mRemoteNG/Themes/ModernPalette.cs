using System.Drawing;
using System.Runtime.Versioning;

namespace mRemoteNG.Themes
{
    /// <summary>
    /// Semantic design tokens of the modern UI. Custom-painted controls should take their colors
    /// from <see cref="Current"/> instead of hard-coding them, so they follow the dark/light theme.
    /// The values match the ModernDark / ModernLight .vstheme files.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public sealed class ModernPalette
    {
        public static readonly ModernPalette Dark = new()
        {
            IsDark = true,
            Chrome = Hex(0x202024),
            Panel = Hex(0x1A1A1D),
            Content = Hex(0x151518),
            Surface = Hex(0x26262B),
            SurfaceHover = Hex(0x2E2E35),
            SurfacePressed = Hex(0x36363E),
            Border = Hex(0x33333A),
            BorderSubtle = Hex(0x26262C),
            Text = Hex(0xECECF1),
            TextSecondary = Hex(0x9A9AA6),
            TextDisabled = Hex(0x5E5E68),
            Accent = Hex(0x5E6AD2),
            AccentHover = Hex(0x6E79DA),
            AccentPressed = Hex(0x4C57BD),
            AccentText = Color.White,
            AccentSubtle = Hex(0x2B2F55),
            Selection = Hex(0x2F3463),
            Success = Hex(0x3FB950),
            Warning = Hex(0xD29922),
            Danger = Hex(0xF85149)
        };

        public static readonly ModernPalette Light = new()
        {
            IsDark = false,
            Chrome = Hex(0xF3F3F6),
            Panel = Hex(0xFAFAFB),
            Content = Hex(0xFFFFFF),
            Surface = Hex(0xFFFFFF),
            SurfaceHover = Hex(0xEDEDF2),
            SurfacePressed = Hex(0xE4E4EA),
            Border = Hex(0xE1E1E8),
            BorderSubtle = Hex(0xEBEBF0),
            Text = Hex(0x1B1B1F),
            TextSecondary = Hex(0x6B6B76),
            TextDisabled = Hex(0xA6A6B0),
            Accent = Hex(0x4F5BD5),
            AccentHover = Hex(0x5F6ADB),
            AccentPressed = Hex(0x4350C4),
            AccentText = Color.White,
            AccentSubtle = Hex(0xE4E6FB),
            Selection = Hex(0xDDE0FA),
            Success = Hex(0x1A7F37),
            Warning = Hex(0x9A6700),
            Danger = Hex(0xCF222E)
        };

        /// <summary>Palette matching the active theme's brightness.</summary>
        public static ModernPalette Current => ThemeManager.getInstance().IsActiveThemeDark ? Dark : Light;

        /// <summary>Corner radius (in logical pixels) for buttons, inputs, tabs and hover highlights.</summary>
        public const int CornerRadius = 6;

        /// <summary>Corner radius (in logical pixels) for larger surfaces such as cards and popups.</summary>
        public const int LargeCornerRadius = 8;

        public bool IsDark { get; private init; }

        /// <summary>Title bar, menu bar, toolbars and status bar.</summary>
        public Color Chrome { get; private init; }

        /// <summary>Tool window (connection tree, config, notifications) background.</summary>
        public Color Panel { get; private init; }

        /// <summary>Document / connection area background.</summary>
        public Color Content { get; private init; }

        /// <summary>Raised elements: inputs, cards, popups.</summary>
        public Color Surface { get; private init; }

        public Color SurfaceHover { get; private init; }
        public Color SurfacePressed { get; private init; }
        public Color Border { get; private init; }
        public Color BorderSubtle { get; private init; }
        public Color Text { get; private init; }
        public Color TextSecondary { get; private init; }
        public Color TextDisabled { get; private init; }
        public Color Accent { get; private init; }
        public Color AccentHover { get; private init; }
        public Color AccentPressed { get; private init; }

        /// <summary>Foreground on top of <see cref="Accent"/>.</summary>
        public Color AccentText { get; private init; }

        /// <summary>Tinted background for accent-related highlights (badges, focus rings, active items).</summary>
        public Color AccentSubtle { get; private init; }

        /// <summary>Selected rows / items.</summary>
        public Color Selection { get; private init; }

        public Color Success { get; private init; }
        public Color Warning { get; private init; }
        public Color Danger { get; private init; }

        private static Color Hex(int rgb) => Color.FromArgb(255, (rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
    }
}
