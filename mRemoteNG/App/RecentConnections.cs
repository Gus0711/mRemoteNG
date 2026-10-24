#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Serialization;
using mRemoteNG.App.Info;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Container;
using mRemoteNG.Resources.Language;
using mRemoteNG.Tree;

namespace mRemoteNG.App
{
    /// <summary>
    /// One entry of the most-recently-used connection list.
    /// </summary>
    public sealed class RecentConnectionEntry
    {
        /// <summary><see cref="AbstractConnectionRecord.ConstantID"/> of the connection in the tree (empty for quick connects).</summary>
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Hostname { get; set; } = string.Empty;
        public string Protocol { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Username { get; set; } = string.Empty;
        public bool IsQuickConnect { get; set; }
        public DateTime LastUsedUtc { get; set; }
        public int UseCount { get; set; }

        [JsonIgnore]
        public ProtocolType ProtocolType => Converter.StringToProtocol(Protocol);
    }

    /// <summary>
    /// Small persistent store (JSON next to the other settings files) holding the recently opened
    /// connections and the home page preferences. All members must be used from the UI thread.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public sealed class RecentConnections
    {
        private const int MaxEntries = 24;
        private const string FileName = "home.json";

        private static readonly Lazy<RecentConnections> LazyInstance = new(() => new RecentConnections());
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        private StoreData _data = new();
        private bool _loaded;

        public static RecentConnections Instance => LazyInstance.Value;

        /// <summary>Raised (on the calling thread) whenever the list or the preferences change.</summary>
        public event EventHandler? Changed;

        private RecentConnections()
        {
        }

        public IReadOnlyList<RecentConnectionEntry> Entries
        {
            get
            {
                EnsureLoaded();
                return _data.Recent;
            }
        }

        /// <summary>Whether the home page opens when the application starts.</summary>
        public bool ShowHomeOnStartup
        {
            get
            {
                EnsureLoaded();
                return _data.ShowHomeOnStartup;
            }
            set
            {
                EnsureLoaded();
                if (_data.ShowHomeOnStartup == value) return;
                _data.ShowHomeOnStartup = value;
                Save();
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>Records that <paramref name="connection"/> was just opened.</summary>
        public void Record(ConnectionInfo? connection)
        {
            if (connection == null || connection is ContainerInfo) return;

            try
            {
                EnsureLoaded();
                bool quick = connection.IsQuickConnect;
                RecentConnectionEntry? entry = quick
                    ? _data.Recent.FirstOrDefault(e => e.IsQuickConnect &&
                                                       string.Equals(e.Hostname, connection.Hostname, StringComparison.OrdinalIgnoreCase) &&
                                                       e.Protocol == connection.Protocol.ToString() &&
                                                       e.Port == connection.Port)
                    : _data.Recent.FirstOrDefault(e => e.Id == connection.ConstantID) ??
                      _data.Recent.FirstOrDefault(e => !e.IsQuickConnect && e.Name == connection.Name &&
                                                       string.Equals(e.Hostname, connection.Hostname ?? string.Empty, StringComparison.OrdinalIgnoreCase) &&
                                                       e.Protocol == connection.Protocol.ToString());

                if (entry != null)
                    _data.Recent.Remove(entry);
                else
                    entry = new RecentConnectionEntry();

                entry.Id = quick ? string.Empty : connection.ConstantID;
                entry.Name = connection.Name ?? string.Empty;
                entry.Hostname = connection.Hostname ?? string.Empty;
                entry.Protocol = connection.Protocol.ToString();
                entry.Port = connection.Port;
                entry.Username = quick ? connection.Username ?? string.Empty : string.Empty;
                entry.IsQuickConnect = quick;
                entry.LastUsedUtc = DateTime.UtcNow;
                entry.UseCount++;

                _data.Recent.Insert(0, entry);
                if (_data.Recent.Count > MaxEntries)
                    _data.Recent.RemoveRange(MaxEntries, _data.Recent.Count - MaxEntries);

                Save();
                Changed?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("Updating the recent connections list failed", ex, Messages.MessageClass.WarningMsg, false);
            }
        }

        public void Remove(RecentConnectionEntry entry)
        {
            EnsureLoaded();
            if (!_data.Recent.Remove(entry)) return;
            Save();
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public void Clear()
        {
            EnsureLoaded();
            if (_data.Recent.Count == 0) return;
            _data.Recent.Clear();
            Save();
            Changed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Recent entries whose connection still exists (tree connections are resolved by id, quick
        /// connects are always kept), most recent first.
        /// </summary>
        public IEnumerable<(RecentConnectionEntry Entry, ConnectionInfo? Connection)> Resolve()
        {
            Dictionary<string, ConnectionInfo> byId = ConnectionCatalog.GetConnections()
                                                                       .GroupBy(c => c.ConstantID)
                                                                       .ToDictionary(g => g.Key, g => g.First());
            foreach (RecentConnectionEntry entry in Entries.ToList())
            {
                if (entry.IsQuickConnect)
                {
                    yield return (entry, null);
                    continue;
                }

                if (byId.TryGetValue(entry.Id, out ConnectionInfo? connection))
                {
                    yield return (entry, connection);
                    continue;
                }

                // Ids can change (e.g. a file without ids, or re-imported connections): fall back to
                // a unique connection with the same name, host and protocol.
                List<ConnectionInfo> sameNode = byId.Values.Where(c => c.Name == entry.Name &&
                                                                       string.Equals(c.Hostname ?? string.Empty, entry.Hostname, StringComparison.OrdinalIgnoreCase) &&
                                                                       c.Protocol.ToString() == entry.Protocol)
                                                           .Take(2)
                                                           .ToList();
                if (sameNode.Count == 1)
                    yield return (entry, sameNode[0]);
            }
        }

        /// <summary>Opens the connection of a recent entry (rebuilding a quick connect if needed).</summary>
        public static void Open(RecentConnectionEntry entry, ConnectionInfo? connection)
        {
            if (connection != null)
            {
                ConnectionCatalog.Open(connection);
                return;
            }

            if (!entry.IsQuickConnect || string.IsNullOrWhiteSpace(entry.Hostname)) return;

            string target = entry.Hostname;
            if (!string.IsNullOrEmpty(entry.Username)) target = entry.Username + "@" + target;
            if (entry.Port > 0) target += ":" + entry.Port;
            ConnectionCatalog.QuickConnect(target, entry.ProtocolType);
        }

        private void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            try
            {
                string path = GetFilePath();
                if (!File.Exists(path)) return;
                StoreData? data = JsonSerializer.Deserialize<StoreData>(File.ReadAllText(path), JsonOptions);
                if (data != null)
                {
                    data.Recent ??= [];
                    data.Recent.RemoveAll(e => e == null);
                    _data = data;
                }
            }
            catch (Exception ex)
            {
                _data = new StoreData();
                Runtime.MessageCollector.AddExceptionMessage("Loading the recent connections list failed", ex, Messages.MessageClass.WarningMsg, false);
            }
        }

        private void Save()
        {
            try
            {
                string path = GetFilePath();
                string? directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                string temp = path + ".tmp";
                File.WriteAllText(temp, JsonSerializer.Serialize(_data, JsonOptions));
                File.Move(temp, path, true);
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("Saving the recent connections list failed", ex, Messages.MessageClass.WarningMsg, false);
            }
        }

        private static string GetFilePath() => Path.Combine(SettingsFileInfo.SettingsPath, FileName);

        private sealed class StoreData
        {
            public int Version { get; set; } = 1;
            public bool ShowHomeOnStartup { get; set; } = true;
            public List<RecentConnectionEntry> Recent { get; set; } = [];
        }
    }

    /// <summary>
    /// Read access to the connection tree and the common "open" operations, shared by the home page
    /// and the command palette.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class ConnectionCatalog
    {
        /// <summary>All connections (not folders) currently in the tree, including PuTTY sessions.</summary>
        public static IReadOnlyList<ConnectionInfo> GetConnections()
        {
            ConnectionTreeModel? model = Runtime.ConnectionsService?.ConnectionTreeModel;
            if (model == null) return [];

            try
            {
                return model.GetRecursiveChildList()
                            .Where(c => c.GetTreeNodeType() is TreeNodeType.Connection or TreeNodeType.PuttySession)
                            .ToList();
            }
            catch (InvalidOperationException)
            {
                // The tree changed while it was enumerated; callers simply retry on the next refresh.
                return [];
            }
        }

        /// <summary>Number of folders (excluding the root nodes).</summary>
        public static int CountFolders()
        {
            ConnectionTreeModel? model = Runtime.ConnectionsService?.ConnectionTreeModel;
            if (model == null) return 0;

            try
            {
                return model.GetRecursiveChildList().Count(c => c.GetTreeNodeType() == TreeNodeType.Container);
            }
            catch (InvalidOperationException)
            {
                return 0;
            }
        }

        /// <summary>"Folder / Sub folder" path of the connection, without the root node.</summary>
        public static string GetFolderPath(ConnectionInfo connection)
        {
            List<string> parts = [];
            ContainerInfo? parent = connection.Parent;
            while (parent != null && parent.GetTreeNodeType() == TreeNodeType.Container)
            {
                parts.Insert(0, parent.Name);
                parent = parent.Parent;
            }

            return string.Join(" / ", parts);
        }

        /// <summary>Opens (or switches to) the connection, exactly like a double click in the tree.</summary>
        public static void Open(ConnectionInfo connection)
        {
            try
            {
                Runtime.ConnectionInitiator.OpenConnection(connection);
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace(Language.ConnectionOpenFailed, ex);
            }
        }

        /// <summary>
        /// Parses quick connect text: "host", "user@host:port" or "scheme://host[:port]"
        /// (rdp, vnc, ssh, ssh1, telnet, rlogin, raw, http, https, ard).
        /// </summary>
        public static bool TryParseQuickConnect(string text, ProtocolType defaultProtocol, out string target, out ProtocolType protocol, out bool explicitProtocol)
        {
            target = string.Empty;
            protocol = defaultProtocol;
            explicitProtocol = false;
            text = text.Trim();
            if (text.Length == 0 || text.Any(char.IsWhiteSpace)) return false;

            int schemeEnd = text.IndexOf("://", StringComparison.Ordinal);
            if (schemeEnd > 0)
            {
                string scheme = text[..schemeEnd].ToLowerInvariant();
                ProtocolType? parsed = scheme switch
                {
                    "rdp" or "mstsc" => ProtocolType.RDP,
                    "vnc" => ProtocolType.VNC,
                    "ssh" or "ssh2" => ProtocolType.SSH2,
                    "ssh1" => ProtocolType.SSH1,
                    "telnet" => ProtocolType.Telnet,
                    "rlogin" => ProtocolType.Rlogin,
                    "raw" => ProtocolType.RAW,
                    "http" => ProtocolType.HTTP,
                    "https" => ProtocolType.HTTPS,
                    "ard" => ProtocolType.ARD,
                    _ => null
                };
                if (parsed == null) return false;
                protocol = parsed.Value;
                explicitProtocol = true;
                text = text[(schemeEnd + 3)..].TrimEnd('/');
                int slash = text.IndexOf('/');
                if (slash >= 0) text = text[..slash];
            }

            // host[:port] with an optional user@ prefix; reject anything else.
            string hostPart = text;
            int at = hostPart.LastIndexOf('@');
            if (at >= 0)
            {
                if (at == 0 || at == hostPart.Length - 1) return false;
                hostPart = hostPart[(at + 1)..];
            }

            string host = hostPart;
            int colon = hostPart.LastIndexOf(':');
            if (colon >= 0)
            {
                if (!int.TryParse(hostPart[(colon + 1)..], out int port) || port <= 0 || port > 65535) return false;
                host = hostPart[..colon];
            }

            if (host.Length == 0 || host.Any(c => !(char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '_'))) return false;

            target = text;
            return true;
        }

        /// <summary>Creates a temporary quick connect connection and opens it.</summary>
        public static void QuickConnect(string target, ProtocolType protocol)
        {
            try
            {
                ConnectionInfo? connection = Runtime.ConnectionsService.CreateQuickConnect(target, protocol);
                if (connection == null) return;
                Runtime.ConnectionInitiator.OpenConnection(connection, ConnectionInfo.Force.DoNotJump);
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace(Language.QuickConnectFailed, ex);
            }
        }

        /// <summary>The protocol selected in the quick connect toolbar.</summary>
        public static ProtocolType DefaultQuickConnectProtocol =>
            Converter.StringToProtocol(Properties.Settings.Default.QuickConnectProtocol);
    }
}
