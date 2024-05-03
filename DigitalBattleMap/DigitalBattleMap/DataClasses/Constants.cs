using System;
using System.IO;

namespace DigitalBattleMap.DataClasses;

public static class Constants
{
    public static readonly Size<int> MapSize = new(1920, 1080);
    public static readonly string SettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DigitalBattleMap");
    public static readonly string MonsterTokensPath = Path.Combine(SettingsPath, "Tokens", "Monsters");
    public static readonly string CustomTokensPath = Path.Combine(SettingsPath, "Tokens", "Custom");
    public static readonly int FeetPerGridCell = 5;
    public static readonly string TempDirectoryPath = Path.Combine(SettingsPath, "Temp");
    public static readonly int MinimalZoomGridSize = 10;
    public static readonly int DefaultZoomSize = 10;
}