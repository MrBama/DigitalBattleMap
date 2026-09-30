using DigitalBattleMap.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using static DigitalBattleMap.Utilities.FileManager;

namespace DigitalBattleMap.DataClasses;

public class Settings
{
    private Dictionary<string, object> _oldValues = new();
    public static string SettingsPath = Path.Combine(Constants.SettingsPath, "Settings.json");

    public event EventHandler<SettingChangedEventArgs> OnSettingChanged;

    public ScreenPosition MonitorPosition { get; set; } = new();
    public int DefaultGridSize { get; set; } = 65;
    public bool IsSoftwareInstalled { get; set; }
    public bool ShowMapWindow { get; set; } = true;
    public bool HideDungeonMasterFeatures { get; set; }
    public BackgroundColor DefaultBackgroundColor { get; set; } = BackgroundColor.Black;
    public bool IsAutoSaveEnabled { get; set; } = true;
    public bool IsAutoUpdateEnabled { get; set; } = true;
    public string ServerAddress { get; set; } = "http://localhost:8000";
    public string CurrentCampaignName { get; set; } = "";
    public string Version { get; set; } = "0";
    public List<Token> CustomTokens { get; set; } = new();
    public List<TokenGroup> TokenGroups { get; set; } = new();
    public List<Campaign> Campaigns { get; set; } = new();
    public Dictionary<string, string> WebExtensionVersions { get; set; } = new();

    public static Settings Load()
    {
        if (!FileManager.OpenFile(SettingsPath, out Settings storage, new DerivedClassJsonConverter<Statblock>()))
        {
            return new();
        }
        storage.BackupValues();
        return storage;
    }

    public void Save()
    {
        FileManager.SaveFile(this, SettingsPath);
        NotifySettingsChanged();
        BackupValues();
    }

    private void BackupValues()
    {
        foreach (PropertyInfo property in typeof(Settings).GetProperties())
        {
            _oldValues[property.Name] = JsonConvert.SerializeObject(property.GetValue(this));
        }
    }

    private void NotifySettingsChanged()
    {
        foreach (PropertyInfo property in typeof(Settings).GetProperties())
        {
            dynamic? oldValue = _oldValues.TryGetValue(property.Name, out var propertyValue) ? propertyValue : null;
            dynamic? newValue = JsonConvert.SerializeObject(property.GetValue(this));
            if (oldValue != newValue)
            {
                OnSettingChanged?.Invoke(this, new SettingChangedEventArgs { SettingName = property.Name });
            }
        }
    }
}
