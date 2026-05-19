using Newtonsoft.Json;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DigitalBattleMap.DataClasses;

public class HexTile
{
    public string Id { get; set; }
    public string Name { get; set; }
    public bool IsPreset { get; set; }

    [JsonIgnore]
    public BitmapSource Bitmap { get; set; }

    [JsonIgnore]
    public Color PresetColor { get; set; }
}
