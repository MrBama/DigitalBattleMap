using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalBattleMap
{
    public class Settings
    {
        private static string _settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DigitalBattleMap", "Settings.json");

        public ScreenPosition MonitorPosition { get; set; } = new ScreenPosition();
        public int DefaultGridSize { get; set; } = 65;

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
