using DigitalBattleMap.DataClasses;
using System.Windows.Media;

namespace DigitalBattleMap.ViewModels;

public class HexcrawlEdgeTypeViewModel
{
    private readonly HexEdgeType _type;

    public HexcrawlEdgeTypeViewModel(HexEdgeType type)
    {
        _type = type;
    }

    public string Id   => _type.Id;
    public string Name => _type.Name;

    public Brush PreviewBrush
    {
        get
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(_type.Color);
                var b = new SolidColorBrush(color);
                b.Freeze();
                return b;
            }
            catch
            {
                return Brushes.DodgerBlue;
            }
        }
    }
}
