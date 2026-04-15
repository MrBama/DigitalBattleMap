using DigitalBattleMap.DataClasses;
using System;
using System.Collections.Generic;

namespace DigitalBattleMap.FogShapes.Region;

/// <summary>
/// Represents a draft/work-in-progress fog shape before resolution.
/// Contains the raw user input points and metadata before geometry operations.
/// </summary>
public sealed class FogShapeDraft
{
    public IReadOnlyCollection<Point<double>> Points { get; }
    public object? FogType { get; }
    public object? Visibility { get; }

    public FogShapeDraft(
        IReadOnlyCollection<Point<double>> points,
        object? fogType = null,
        object? visibility = null)
    {
        Points = points ?? throw new ArgumentNullException(nameof(points));
        FogType = fogType;
        Visibility = visibility;
    }
}

