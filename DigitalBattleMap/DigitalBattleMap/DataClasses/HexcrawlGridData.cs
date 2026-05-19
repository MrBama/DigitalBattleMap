using System.Collections.Generic;

namespace DigitalBattleMap.DataClasses;

public class HexcrawlGridData
{
    public double HexSize { get; set; } = 80.0;
    public int GridWidth { get; set; } = 30;
    public int GridHeight { get; set; } = 20;
    public List<HexCell> Cells { get; set; } = new();
    public List<HexCoord> ExploredHexes { get; set; } = new();
}

public class HexCoord
{
    public int Q { get; set; }
    public int R { get; set; }
}
