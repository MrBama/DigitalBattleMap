using System.Collections.Generic;

namespace DigitalBattleMap.DataClasses;

public class HexPaintCommand
{
    public List<HexCellChange> Changes { get; } = new();

    public record HexCellChange(int Q, int R, string OldTileId, string NewTileId);
}
