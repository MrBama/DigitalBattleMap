using DigitalBattleMap.DataClasses;
using System.Collections.Generic;
using System.Drawing;

namespace DigitalBattleMap.FogShapes;
public class FogOverviewBitmap : OverviewBitmap
{
    public bool IsFogEnabled { get; set; }
    public List<PointF> ScaledPoints { get; set; } = new();
}
