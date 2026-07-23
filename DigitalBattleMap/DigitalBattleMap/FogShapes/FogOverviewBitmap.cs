using DigitalBattleMap.DataClasses;
using System.Collections.Generic;

namespace DigitalBattleMap.FogShapes;
public class FogOverviewBitmap : OverviewBitmap
{
    public bool IsFogEnabled { get; set; }
    public List<Point<float>> ScaledPoints { get; set; } = new();
}
