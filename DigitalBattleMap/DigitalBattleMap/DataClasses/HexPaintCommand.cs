using System.Collections.Generic;

namespace DigitalBattleMap.DataClasses;

public class HexPaintCommand : IHexMapCommand
{
    public List<HexCellChange> Changes { get; } = new();

    public record HexCellChange(int Q, int R, string OldTileId, string NewTileId);
}
