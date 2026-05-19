using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Utilities;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DigitalBattleMap.ViewModels;

public class HexcrawlTileViewModel : PropertyHandler
{
    private readonly HexTile _tile;

    public HexcrawlTileViewModel(HexTile tile)
    {
        _tile = tile;
    }

    public string Id => _tile.Id;
    public string Name => _tile.Name;
    public bool IsPreset => _tile.IsPreset;

    public Brush PreviewBrush => _tile.IsPreset
        ? new SolidColorBrush(_tile.PresetColor)
        : _tile.Bitmap != null ? new ImageBrush(_tile.Bitmap) { Stretch = Stretch.UniformToFill } : Brushes.Gray;
}
