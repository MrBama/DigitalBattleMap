using System.Collections.Generic;

namespace DigitalBattleMap.DataClasses;

public class HexEdgeType
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Color { get; set; } = "#0070C0";
    public double StrokeWidth { get; set; } = 0.12;
    public bool IsCurved { get; set; } = false;
    public List<string> EndTileIds { get; set; } = new();
}
