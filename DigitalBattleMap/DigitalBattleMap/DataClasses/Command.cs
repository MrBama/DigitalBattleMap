using DigitalBattleMap.DrawingShapes;
using DigitalBattleMap.Utilities;
using System.Collections.Generic;
using System.ComponentModel;

namespace DigitalBattleMap.DataClasses;

public class TokenMoveCommand
{
    public TokenMoveCommand(TokenIdentifier tokenIdentifier, Point<int> offset) : this(new List<TokenIdentifier> { tokenIdentifier }, offset)
    {
    }

    public TokenMoveCommand(List<TokenIdentifier> tokenIdentifiers, Point<int> offset)
    {
        TokenIdentifiers = new List<TokenIdentifier>(tokenIdentifiers.Clone());
        Offset = offset;
    }

    public List<TokenIdentifier> TokenIdentifiers { get; set; } = new();
    public Point<int> Offset { get; set; }
}

public class ZoomAndMoveCommand
{
    public ZoomAndMoveCommand(Point<int> steps, int gridSize)
    {
        Steps = steps;
        GridSize = gridSize;
    }

    public ZoomAndMoveCommand(ArrowDirection arrowDirection, int steps)
    {
        switch (arrowDirection)
        {
            case ArrowDirection.Up:
                Steps = new Point<int>(0, -steps);
                break;
            case ArrowDirection.Down:
                Steps = new Point<int>(0, steps);
                break;
            case ArrowDirection.Left:
                Steps = new Point<int>(-steps, 0);
                break;
            case ArrowDirection.Right:
                Steps = new Point<int>(steps, 0);
                break;
            default:
                throw new InvalidEnumArgumentException();
        }
    }

    public ZoomAndMoveCommand(int gridSize)
    {
        GridSize = gridSize;
    }

    public Point<int>? Steps { get; set; } = null;
    public int? GridSize { get; set; } = null;
}

public class DrawingShapeCommand
{
    public DrawingShapeCommand(DrawingShape drawingShape, DrawingShapeCommandAction action)
    {
        DrawingShape = drawingShape;
        Action = action;
    }

    public DrawingShape DrawingShape { get; set; }
    public DrawingShapeCommandAction Action { get; set; }
    public DrawingShapeInfo Before { get; set; }
    public DrawingShapeInfo After { get; set; }
    public int RemovedAtIndex { get; set; }
    public DrawingShapeErasedEventArgs EraseData { get; set; }
}
