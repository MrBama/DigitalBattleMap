using DigitalBattleMap.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using static DigitalBattleMap.Utilities.FileManager;

namespace DigitalBattleMap.DataClasses;

public class Settings
{
    private static string _settingsPath = Path.Combine(Constants.SettingsPath, "Settings.json");
    private Dictionary<string, object> _oldValues = new();

    public event EventHandler<SettingChangedEventArgs> OnSettingChanged;

    public ScreenPosition MonitorPosition { get; set; } = new();
    public int DefaultGridSize { get; set; } = 65;
    public bool IsSoftwareInstalled { get; set; }
    public bool ShowMapWindow { get; set; } = true;
    public bool HideDungeonMasterFeatures { get; set; }
    public bool HasBlackBackground { get; set; } = true;
    public string ServerAddress { get; set; } = "http://localhost:8000";
    public string CurrentCampaignName { get; set; } = "";
    public List<Token> CustomTokens { get; set; } = new();
    public List<TokenGroup> TokenGroups { get; set; } = new();
    public List<Campaign> Campaigns { get; set; } = new();
    public Dictionary<string, string> WebExtensionVersions { get; set; } = new();

    public static Settings Load()
    {
        if (!FileManager.OpenFile(_settingsPath, out Settings storage, new DerivedClassJsonConverter<Statblock>()))
        {
            return new();
        }
        storage.BackupValues();
        return storage;
    }

    public void Save()
    {
        FileManager.SaveFile(this, _settingsPath);
        NotifySettingsChanged();
        BackupValues();
    }

    private void BackupValues()
    {
        foreach (PropertyInfo property in typeof(Settings).GetProperties())
        {
            _oldValues[property.Name] = property.GetValue(this);
        }
    }

    private void NotifySettingsChanged()
    {
        foreach (PropertyInfo property in typeof(Settings).GetProperties())
        {
            dynamic? oldValue = _oldValues[property.Name];
            dynamic? newValue = property.GetValue(this);
            if (oldValue != newValue)
            {
                OnSettingChanged?.Invoke(this, new SettingChangedEventArgs { SettingName = property.Name });
            }
        }
    }
}
