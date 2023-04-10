using System.Collections.Generic;
using System.IO;

namespace DigitalBattleMap
{
    public class Settings
    {
        private static string _settingsPath = Path.Combine(Constants.SettingsPath, "Settings.json");

        public ScreenPosition MonitorPosition { get; set; } = new ScreenPosition();
        public int DefaultGridSize { get; set; } = 65;
        public bool IsSoftwareInstalled { get; set; }
        public string ServerAddress { get; set; } = "http://localhost:8000";
        public List<Token> CustomTokens { get; set; } = new List<Token>();
        public List<TokenGroup> TokenGroups { get; set; } = new List<TokenGroup>();

        public static Settings Load()
        {
            if (!FileManager.OpenFile(_settingsPath, out Settings storage))
            {
                return new Settings();
            }
            return storage;
        }

        public void Save()
        {
            FileManager.SaveFile(this, _settingsPath);
        }
    }
}
