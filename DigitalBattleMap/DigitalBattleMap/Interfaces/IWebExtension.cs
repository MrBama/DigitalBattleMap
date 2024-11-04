namespace DigitalBattleMap.Interfaces;

public interface IWebExtension
{
    public string Name { get; set; }
    public string Version { get; set; }

    public bool Install();
    public bool IsUpdateAvailable(string currentVersion);
    public bool Update(string currentVersion);
}
