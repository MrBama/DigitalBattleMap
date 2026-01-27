using DigitalBattleMap.DataClasses;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace DigitalBattleMap.Interfaces;
public interface IShape
{
    Color Color { get; set; }

    double PenSize { get; set; }

    ObservableCollection<Point<double>> Points { get; set; }

    double PenSizeCanvas { get; }
}
