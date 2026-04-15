using DigitalBattleMap.DataClasses;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DigitalBattleMap.FogShapes.Region;

/// <summary>
/// Resolves fog shapes by handling geometry operations and converting to renderable regions.
/// Triggers events when resolution is complete, allowing FogCanvas to handle the rendering.
/// </summary>
public class FogShapeResolver
{
    /// <summary>
    /// Fired when a fog shape has been resolved and is ready for rendering.
    /// </summary>
    public event EventHandler<FogShapeResolvedEventArgs>? OnFogShapeResolved;

    /// <summary>
    /// Initiates the resolution process for a fog shape.
    /// Takes the original FogShape, converts it to a draft, and resolves it.
    /// </summary>
    /// <param name="fogShape">The fog shape to resolve</param>
    /// <param name="existingShapes">Existing fog shapes for geometry operations</param>
    public void ResolveShape(FogShape fogShape, IReadOnlyCollection<FogShape> existingShapes)
    {
        if (fogShape == null)
            throw new ArgumentNullException(nameof(fogShape));


        // Resolve the draft
        var resolvedRegion = Resolve(fogShape, existingShapes);

        if (resolvedRegion != null)
        {
            // Trigger event for FogCanvas to handle rendering
            OnFogShapeResolved?.Invoke(this, new FogShapeResolvedEventArgs(fogShape, resolvedRegion));
        }
    }

    /// <summary>
    /// Resolves a fog shape draft against existing shapes.
    /// Currently bypasses geometry operations and returns a resolved region with original properties.
    /// </summary>
    private ResolvedFogRegion? Resolve(
        FogShape fogShape,
        IReadOnlyCollection<FogShape> existingShapes)
    {
        // TODO: Implement full geometry resolution:
        // 1. Convert draft points → geometry
        // 2. Build exclusion geometry from existing fog
        // 3. Subtract existing fog from draft
        // 4. Normalize geometry
        // 5. Convert geometry → contours + holes

        // For now, we bypass geometry operations and return the region as-is
        // This is a placeholder for future implementation

        // Create a resolved region from the draft with rendering properties from FogShape
        return new ResolvedFogRegion(
            null,
            fogShape.Points,
            fogShape.ColorOuter,
            fogShape.ColorInner,
            fogShape.PenSize,
            fogShape.PenSizeCanvas,
            fogShape.IsFogEnabled);
    }
}
