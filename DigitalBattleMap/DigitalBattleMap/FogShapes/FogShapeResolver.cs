using DigitalBattleMap.DataClasses;
using Microsoft.Xaml.Behaviors.Layout;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace DigitalBattleMap.FogShapes;

public class FogShapeResolver
{

    public FogShape? Resolve(
        FogShapeDraft draft,
        IReadOnlyCollection<FogShape> existingShapes)
    {
        // 1. Convert draft points → geometry
        var draftGeometry = FromPoints(draft.Points);

        // 2. Build exclusion geometry from existing fog
        var exclusionGeometry = BuildExclusionGeometry(existingShapes);

        // 3. Subtract existing fog from draft
        var resolvedGeometry = Geometry.Combine(
            draftGeometry,
            exclusionGeometry,
            GeometryCombineMode.Exclude,
            null);

        if (resolvedGeometry.IsEmpty())
            return null;

        // 4. Normalize geometry
        var outlined = resolvedGeometry.GetOutlinedPathGeometry();

        // 5. Convert geometry → contours + holes
        var region = ToResolvedRegion(outlined);

        // 6. Create final FogShape
        return CreateFromRegion(
            region,
            draft.FogType,
            draft.Visibility);
    }


    private static Geometry BuildExclusionGeometry(
        IReadOnlyCollection<FogShape> existingShapes)
    {
        if (existingShapes.Count == 0)
            return Geometry.Empty;

        Geometry? combined = null;

        foreach (var shape in existingShapes)
        {
            var geometry = FromPoints(shape.Points);

            combined = combined == null
                ? geometry
                : Geometry.Combine(
                    combined,
                    geometry,
                    GeometryCombineMode.Union,
                    null);
        }

        return combined!;
    }


    public static PathGeometry FromPoints(
        IReadOnlyCollection<Point<double>> points)
    {
        var figure = new PathFigure
        {
            IsClosed = true,
            IsFilled = true,
            StartPoint = ToWpf(points.First())
        };

        foreach (var point in points.Skip(1))
        {
            figure.Segments.Add(
                new LineSegment(ToWpf(point), true));
        }

        return new PathGeometry(new[] { figure });
    }


    public static ResolvedRegion ToResolvedRegion(
        PathGeometry geometry)
    {
        var contours = new List<List<Point<double>>>();
        var holes = new List<List<Point<double>>>();

        foreach (var figure in geometry.Figures)
        {
            var points = new List<Point<double>>
            {
                FromWpf(figure.StartPoint)
            };

            foreach (var segment in figure.Segments.OfType<LineSegment>())
            {
                points.Add(FromWpf(segment.Point));
            }

            // Use winding direction to detect holes
            if (IsClockwise(points))
                contours.Add(points);
            else
                holes.Add(points);
        }

        return new ResolvedRegion(
            contours.Single(),
            holes);
    }


    private static bool IsClockwise(
        IReadOnlyList<Point<double>> points)
    {
        double sum = 0;
        for (int i = 0; i < points.Count - 1; i++)
        {
            sum += (points[i + 1].X - points[i].X) *
                   (points[i + 1].Y + points[i].Y);
        }
        return sum > 0;
    }

    private static System.Windows.Point ToWpf(
        Point<double> p) =>
        new System.Windows.Point(p.X, p.Y);

    private static Point<double> FromWpf(
        System.Windows.Point p) =>
        new Point<double>(p.X, p.Y);




        public static FogShape CreateFromRegion(
            ResolvedRegion region,
            FogType type,
            FogVisibility visibility)
        {
            return new ResolvedFogGeometry(
                new ObservableCollection<Point<double>>(region.OuterContour),
                region.Holes.Select(
                    h => new ObservableCollection<Point<double>>(h)).ToList(),
                type,
                visibility);
        }
    


    public ResolvedRegion CreateDraftRegion(
        IReadOnlyCollection<Point<double>> points)
    {
        // This method does NOT resolve overlaps yet.
        // It only establishes a geometry-aware representation boundary.

        if (points == null || points.Count < 3)
            throw new ArgumentException("A region requires at least three points.");

        return new ResolvedRegion(
            outerContour: points.ToList(),
            holes: Array.Empty<IReadOnlyList<Point<double>>>()
        );
    }
}
