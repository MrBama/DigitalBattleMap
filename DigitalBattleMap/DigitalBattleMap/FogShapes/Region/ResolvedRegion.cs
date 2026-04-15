using DigitalBattleMap.DataClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalBattleMap.FogShapes.Region;

public sealed class ResolvedRegion
{
    public IReadOnlyList<Point<double>> OuterContour { get; }
    public IReadOnlyList<IReadOnlyList<Point<double>>> Holes { get; }

    public ResolvedRegion(
        IReadOnlyList<Point<double>> outerContour,
        IReadOnlyList<IReadOnlyList<Point<double>>> holes)
    {
        OuterContour = outerContour;
        Holes = holes;
    }
}
