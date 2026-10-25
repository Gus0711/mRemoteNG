using System;
using System.IO;
using System.Runtime.Versioning;
using mRemoteNG.App.Info;
using mRemoteNG.Messages;

namespace mRemoteNG.App.Initialization
{
    /// <summary>
    /// Releases up to 1.76 kept their data in %APPDATA%\mRemoteNG; this version uses
    /// %APPDATA%\&lt;product name&gt;. On the first start of an installed build, copy the old
    /// connections and external tools over so the user finds them again. The originals are
    /// left untouched so the previous version can still read them.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class LegacyDataImporter
    {
        private static readonly string[] FilesToImport =
        {
            ConnectionsFileInfo.DefaultConnectionsFile,
            SettingsFileInfo.ExtAppsFilesName
        };

        public static void ImportIfNeeded()
        {
            if (Runtime.IsPortableEdition)
                return;

            try
            {
                string legacyPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "mRemoteNG");
                string settingsPath = SettingsFileInfo.SettingsPath;

                if (string.Equals(Path.GetFullPath(legacyPath).TrimEnd('\\'), Path.GetFullPath(settingsPath).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                    return;

                // Only for a fresh data folder: never overwrite anything this version already wrote.
                if (File.Exists(Path.Combine(settingsPath, ConnectionsFileInfo.DefaultConnectionsFile)))
                    return;

                if (!File.Exists(Path.Combine(legacyPath, ConnectionsFileInfo.DefaultConnectionsFile)))
                    return;

                Directory.CreateDirectory(settingsPath);
                foreach (string fileName in FilesToImport)
                {
                    string source = Path.Combine(legacyPath, fileName);
                    string destination = Path.Combine(settingsPath, fileName);
                    if (!File.Exists(source) || File.Exists(destination))
                        continue;

                    File.Copy(source, destination);
                    Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg, $"Imported {source} to {destination}", true);
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("Could not import the data of the previous mRemoteNG version.", ex);
            }
        }
    }
}
