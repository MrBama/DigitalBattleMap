using DigitalBattleMap.Utilities;
using System.Collections.Generic;
using System.IO;
using static DigitalBattleMap.Utilities.FileManager;

namespace DigitalBattleMap.DataClasses;

public class Settings
{
    private static string _settingsPath = Path.Combine(Constants.SettingsPath, "Settings.json");

    public ScreenPosition MonitorPosition { get; set; } = new();
    public int DefaultGridSize { get; set; } = 65;
    public bool IsSoftwareInstalled { get; set; }
    public bool ShowMapWindow { get; set; } = true;
    public string ServerAddress { get; set; } = "http://localhost:8000";
    public string CurrentCampaignName { get; set; } = "";
    public List<Token> CustomTokens { get; set; } = new();
    public List<TokenGroup> TokenGroups { get; set; } = new();
    public List<Campaign> Campaigns { get; set; } = new();

    public static Settings Load()
    {
        if (!FileManager.OpenFile(_settingsPath, new DerivedClassJsonConverter<Statblock>(), out Settings storage))
        {
            return new();
        }
        return storage;
    }

    public void Save()
    {
        FileManager.SaveFile(this, _settingsPath);
    }
}
