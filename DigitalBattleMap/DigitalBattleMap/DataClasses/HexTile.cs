using Newtonsoft.Json;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DigitalBattleMap.DataClasses;

public class HexTile
{
    public const double DefaultTravelHours = 1.0;
    public const int DefaultEncounterOn = 5;

    public string Id { get; set; }
    public string Name { get; set; }
    public bool IsPreset { get; set; }

    // Terrain rules used by the hexcrawl play tracker.
    // Hours of exploring time spent when the party enters a hex of this terrain.
    public double TravelHours { get; set; } = DefaultTravelHours;
    // Whether entering this terrain requires a Survival navigation check.
    public bool RequiresNavigation { get; set; }
    // Morning encounter triggers on a d6 result of this value or higher when the day starts here.
    public int EncounterOn { get; set; } = DefaultEncounterOn;

    [JsonIgnore]
    public BitmapSource Bitmap { get; set; }

    [JsonIgnore]
    public Color PresetColor { get; set; }
}
