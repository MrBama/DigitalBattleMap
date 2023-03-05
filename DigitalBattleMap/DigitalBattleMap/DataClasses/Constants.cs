using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalBattleMap
{
    public static class Constants
    {
        public static string SettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DigitalBattleMap");
        public static string MonsterTokensPath = Path.Combine(SettingsPath, "Tokens", "Monsters");
        public static string CustomTokensPath = Path.Combine(SettingsPath, "Tokens", "Custom");
    }
}
