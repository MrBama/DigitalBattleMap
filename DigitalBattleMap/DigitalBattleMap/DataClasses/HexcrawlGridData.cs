using System.Collections.Generic;

namespace DigitalBattleMap.DataClasses;

public class HexcrawlGridData
{
    public double HexSize { get; set; } = 80.0;
    public int GridWidth { get; set; } = 30;
    public int GridHeight { get; set; } = 20;
    public List<HexCell> Cells { get; set; } = new();
    public List<HexCoord> ExploredHexes { get; set; } = new();
    public HexCoord PartyTokenPos { get; set; }
    public string PartyTokenImagePath { get; set; }
    public bool IsPartyLost { get; set; }
    // DM-only actual party position while lost; null when not lost or not yet moved.
    public HexCoord PartyLostPos { get; set; }
    public List<HexLabelEntry> HexLabels { get; set; } = new();
    public List<HexCoord> RevealedLabels { get; set; } = new();
    public List<HexEdge> Edges { get; set; } = new();
    public List<HexInfo> HexInfos { get; set; } = new();
    public HexcrawlDayState DayState { get; set; } = new();
    // Zoom and position of the hex view; null in older saves (the grid is centred then).
    public HexViewState View { get; set; }
}

// The view is stored as zoom plus the world point at the centre of the canvas,
// so it lines up again when the window has a different size.
public class HexViewState
{
    public double Scale { get; set; }
    public double CenterX { get; set; }
    public double CenterY { get; set; }
}

public class HexCoord
{
    public int Q { get; set; }
    public int R { get; set; }
}

public class HexLabelEntry
{
    public int Q { get; set; }
    public int R { get; set; }
    public string Label { get; set; }
}
