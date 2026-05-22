namespace DigitalBattleMap.DataClasses;

public class HexEdge
{
    // Canonical hex coordinates and edge direction (0-5 matching HexGeometry.NeighborDirections).
    // Canonical means the hex with the smaller (Q, then R) owns the record.
    public int Q { get; set; }
    public int R { get; set; }
    public int EdgeDir { get; set; }
    public string TypeId { get; set; }
}
