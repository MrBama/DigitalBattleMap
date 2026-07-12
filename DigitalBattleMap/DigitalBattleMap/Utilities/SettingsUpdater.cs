using DigitalBattleMap.DataClasses;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DigitalBattleMap.Utilities;

public static class SettingsUpdater
{
    public static readonly string SettingsVersion = "3";

    // Versions are defined from top to bottom where the bottom has the newest version.
    // Updates are executed in the same order.
    private static readonly List<IVersionUpdate> _versions = new() {
        new VersionUpdate0(),
        new VersionUpdate1(),
        new VersionUpdate2()
    };

    public static bool IsUpdateRequired(string version)
    {
        // First check if the version is not equal to application version.
        // Then make sure it's a known version. This is needed for backwards compatibility when
        // loading an older version of the software.

        return version != SettingsVersion && _versions.SingleOrDefault(v => v.Version == version) != null;
    }

    public static void Update(string version)
    {
        if (IsUpdateRequired(version))
        {
            var currentVersion = version;
            while (currentVersion != SettingsVersion)
            {
                var index = _versions.FindIndex(v => v.Version == currentVersion);
                if (index == -1)
                {
                    throw new NotImplementedException($"There is no updater implemented for version: {currentVersion}");
                }

                var updater = _versions[index];
                updater.Update();
                currentVersion = updater.NewVersion;
            }
        }
    }

    private interface IVersionUpdate
    {
        public string Version { get; }
        public string NewVersion { get; }

        public void Update();
    }

    private class VersionUpdate0 : IVersionUpdate
    {
        public string Version => "0";
        public string NewVersion => "1";

        /// <summary>
        /// This Update introduced the 2024 monsters. The way the SourceStatblock class in custom tokens are handled changed
        /// from using source book to using a URL directly. Since the existing custom tokens that copy a monster
        /// did not have this URL field it will be null.
        /// 
        /// This update ensures to fill in the URL for custom tokens that copy a monster. If a tokens has both a legacy and normal version, 
        /// prefere the legacy one as the normal ones (2024) did not exist before this update.
        /// </summary>
        public void Update()
        {
            var settings = Settings.Load();
            foreach (var customToken in settings.CustomTokens)
            {
                if(customToken.Statblock is SourceStatblock statblock)
                {
                    if(statblock.StatblockUrl == null)
                    {
                        var monsterTokens = new MonsterTokens();
                        monsterTokens.ReloadTokens();
                        var tokens = monsterTokens.GetTokens();
                        var token = tokens.SingleOrDefault(t => t.Name == statblock.SourceName + " (2014)");
                        if(token == null)
                        {
                            token = tokens.Single(t => t.Name == statblock.SourceName);
                        }
                        customToken.Statblock = token.Statblock?.Clone<Statblock>();
                        customToken.Statblock!.Name = customToken.Name;
                    }
                }
            }
            settings.Save();
        }
    }

    private class VersionUpdate1 : IVersionUpdate
    {
        public string Version => "1";
        public string NewVersion => "2";

        /// <summary>
        /// This update introduced the auto save functionality. The path where auto save files are stored does not exist
        /// if the application has already been installed before (during first startup).
        /// 
        /// This update will create the directory if the application was already installed.
        /// </summary>
        public void Update()
        {
            IO.Directory.CreateDirectory(Constants.AutoSavesPath);
        }
    }

    private class VersionUpdate2 : IVersionUpdate
    {
        public string Version => "2";
        public string NewVersion => "3";

        /// <summary>
        /// This update converted the setting HasBlackBackground to DefaultBackgroundColor.
        /// 
        /// This update will convert the exiting bool HasBlackBackground to the correct enum value for DefaultBackgroundColor.
        /// </summary>
        public void Update()
        {
            if (FileManager.OpenFile(Settings.SettingsPath, out TempSetting tempSetting))
            {
                var settings = Settings.Load();
                settings.DefaultBackgroundColor = tempSetting.HasBlackBackground ? BackgroundColor.Black : BackgroundColor.White;
                settings.Save();
            }

        }

        private class TempSetting
        {
            public bool HasBlackBackground { get; set; } = true;
        }
    }
}
