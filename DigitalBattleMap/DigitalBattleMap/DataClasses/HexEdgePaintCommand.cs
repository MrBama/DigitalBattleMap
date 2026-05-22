using System.Collections.Generic;

namespace DigitalBattleMap.DataClasses;

public class HexEdgePaintCommand : IHexMapCommand
{
    public List<HexEdgeChange> Changes { get; } = new();

    public record HexEdgeChange(int Q, int R, int EdgeDir, string OldTypeId, string NewTypeId);
}
