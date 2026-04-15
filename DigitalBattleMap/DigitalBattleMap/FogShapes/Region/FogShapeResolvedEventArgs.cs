using System;

namespace DigitalBattleMap.FogShapes.Region;

/// <summary>
/// Event arguments fired when a fog shape has been resolved by FogShapeResolver.
/// </summary>
public class FogShapeResolvedEventArgs : EventArgs
{
    /// <summary>
    /// The original FogShape that was resolved (for tracking and removal).
    /// </summary>
    public FogShape OriginalShape { get; }

    /// <summary>
    /// The resolved fog region ready for rendering.
    /// </summary>
    public ResolvedFogRegion ResolvedRegion { get; }

    public FogShapeResolvedEventArgs(FogShape originalShape, ResolvedFogRegion resolvedRegion)
    {
        OriginalShape = originalShape ?? throw new ArgumentNullException(nameof(originalShape));
        ResolvedRegion = resolvedRegion ?? throw new ArgumentNullException(nameof(resolvedRegion));
    }
}
