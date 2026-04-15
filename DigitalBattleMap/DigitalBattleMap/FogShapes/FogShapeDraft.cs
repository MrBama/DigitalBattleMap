using DigitalBattleMap.DataClasses;
using Microsoft.Xaml.Behaviors.Layout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalBattleMap.FogShapes;

public sealed class FogShapeDraft
{
    public IReadOnlyCollection<Point<double>> Points { get; }
    public FogType FogType { get; }
    public FogVisibility Visibility { get; }

    public FogShapeDraft(
        IReadOnlyCollection<Point<double>> points,
        FogType fogType,
        FogVisibility visibility)
    {
        Points = points;
        FogType = fogType;
        Visibility = visibility;
    }
}

