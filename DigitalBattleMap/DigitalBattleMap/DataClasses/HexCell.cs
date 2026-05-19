namespace DigitalBattleMap.DataClasses;

public class HexCell
{
    public int Q { get; set; }
    public int R { get; set; }
    public string TileId { get; set; }

    public string Label { get; set; }

    public HexCell() { }
    public HexCell(int q, int r, string tileId) { Q = q; R = r; TileId = tileId; }
}
