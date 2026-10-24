using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Runtime.Versioning;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Container;
using mRemoteNG.Properties;
using mRemoteNG.Themes;
using mRemoteNG.Tree.Root;

namespace mRemoteNG.UI.Controls.ConnectionTree
{
    /// <summary>
    /// Decides which Fluent glyph and tint represents a node of the connection tree, and loads the
    /// user-chosen .ico icons (Icon property) when a connection uses a non-default one.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal static class ConnectionTreeIcons
    {
        // Segoe Fluent Icons / MDL2 code points used only by the connection tree.
        internal const char FolderFilled = '\uE8D5';
        internal const char NewFolder = '\uE8F4';
        internal const char ChevronRightSmall = '\uE970';
        internal const char ChevronDownSmall = '\uE96E';
        internal const char ExpandAllBox = '\uF164';
        internal const char CollapseAllBox = '\uF165';
        internal const char Puzzle = '\uEA86';
        internal const char RemoteScreens = '\uEBC6';

        private const string DefaultIconName = "mRemoteNG";

        private static readonly ConcurrentDictionary<(string, int), Bitmap?> CustomIconCache = new();

        /// <summary>Glyph to draw for the node (folder, root, or protocol).</summary>
        public static char GetGlyph(ConnectionInfo node, bool isExpanded)
        {
            switch (node)
            {
                case RootPuttySessionsNodeInfo:
                    return FluentIcons.Terminal;
                case RootNodeInfo:
                case ContainerInfo { IsRootGroup: true }:
                    return FluentIcons.Network;
                case ContainerInfo:
                    return FolderFilled;
            }

            return node.Protocol switch
            {
                ProtocolType.RDP => FluentIcons.Desktop,
                ProtocolType.VNC or ProtocolType.ARD or ProtocolType.AnyDesk => RemoteScreens,
                ProtocolType.SSH1 or ProtocolType.SSH2 or ProtocolType.Telnet or ProtocolType.Rlogin or ProtocolType.RAW => FluentIcons.Terminal,
                ProtocolType.HTTP or ProtocolType.HTTPS => FluentIcons.Globe,
                ProtocolType.PowerShell or ProtocolType.Terminal or ProtocolType.WSL => FluentIcons.Code,
                ProtocolType.IntApp => Puzzle,
                _ => FluentIcons.Server
            };
        }

        /// <summary>Tint of the node glyph. Each protocol family gets its own hue, tuned for dark and light themes.</summary>
        public static Color GetGlyphColor(ConnectionInfo node, ModernPalette palette)
        {
            bool dark = palette.IsDark;
            switch (node)
            {
                case RootPuttySessionsNodeInfo:
                    return dark ? Color.FromArgb(0x4E, 0xC9, 0x8F) : Color.FromArgb(0x1A, 0x8F, 0x5A);
                case RootNodeInfo:
                case ContainerInfo { IsRootGroup: true }:
                    return palette.Accent;
                case ContainerInfo:
                    return dark ? Color.FromArgb(0xE0, 0xB0, 0x50) : Color.FromArgb(0xD0, 0x96, 0x24);
            }

            return node.Protocol switch
            {
                ProtocolType.RDP => dark ? Color.FromArgb(0x5A, 0xA9, 0xF8) : Color.FromArgb(0x1F, 0x6F, 0xD1),
                ProtocolType.VNC or ProtocolType.ARD or ProtocolType.AnyDesk => dark ? Color.FromArgb(0xB3, 0x8E, 0xF6) : Color.FromArgb(0x7A, 0x4F, 0xD0),
                ProtocolType.SSH1 or ProtocolType.SSH2 or ProtocolType.Telnet or ProtocolType.Rlogin or ProtocolType.RAW => dark ? Color.FromArgb(0x4E, 0xC9, 0x8F) : Color.FromArgb(0x1A, 0x8F, 0x5A),
                ProtocolType.HTTP or ProtocolType.HTTPS => dark ? Color.FromArgb(0x3C, 0xC5, 0xC8) : Color.FromArgb(0x0E, 0x8A, 0x8F),
                ProtocolType.PowerShell or ProtocolType.Terminal or ProtocolType.WSL => dark ? Color.FromArgb(0xF0, 0xA3, 0x5E) : Color.FromArgb(0xC2, 0x66, 0x1B),
                ProtocolType.IntApp => dark ? Color.FromArgb(0xE8, 0x79, 0xB9) : Color.FromArgb(0xB8, 0x32, 0x7F),
                _ => palette.TextSecondary
            };
        }

        /// <summary>
        /// The .ico chosen by the user for this connection, rendered at <paramref name="pixelSize"/>,
        /// or null when the connection uses the default icon (then the protocol glyph is shown).
        /// </summary>
        public static Bitmap? GetCustomIcon(ConnectionInfo node, int pixelSize)
        {
            if (node is ContainerInfo || node is PuttySessionInfo)
                return null;

            string iconName = node.Icon;
            if (string.IsNullOrEmpty(iconName) || IsDefaultIcon(iconName))
                return null;

            return CustomIconCache.GetOrAdd((iconName, pixelSize), key => LoadIcon(key.Item1, key.Item2));
        }

        private static bool IsDefaultIcon(string iconName) =>
            iconName.Equals(DefaultIconName, StringComparison.OrdinalIgnoreCase) ||
            iconName.Equals(Settings.Default.ConDefaultIcon, StringComparison.OrdinalIgnoreCase);

        private static Bitmap? LoadIcon(string iconName, int pixelSize)
        {
            using Icon? icon = ConnectionIcon.FromString(iconName);
            if (icon == null)
                return null;

            try
            {
                // Pick the best frame of the .ico for the requested size, then scale it.
                using Icon sized = new(icon, new Size(pixelSize, pixelSize));
                using Bitmap frame = sized.ToBitmap();
                if (frame.Width == pixelSize && frame.Height == pixelSize)
                    return new Bitmap(frame);

                Bitmap result = new(pixelSize, pixelSize);
                using Graphics g = Graphics.FromImage(result);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(frame, new Rectangle(0, 0, pixelSize, pixelSize));
                return result;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }
}
