using Newtonsoft.Json;

namespace DigitalBattleMap.DataClasses;

public enum HexSpecial
{
    None,
    PointOfInterest,
    Dungeon
}

// DM-only per-hex information, saved with the map.
public class HexInfo
{
    public int Q { get; set; }
    public int R { get; set; }
    public HexSpecial Special { get; set; }
    public string Notes { get; set; }
    // Set from the play tab when the party searches the hex.
    public bool Searched { get; set; }

    [JsonIgnore]
    public bool IsEmpty => Special == HexSpecial.None && string.IsNullOrWhiteSpace(Notes) && !Searched;
}
