using System;

namespace DigitalBattleMap.DataClasses;

public class GridCell : IEquatable<GridCell>
{
    public int X { get; set; }
    public int Y { get; set; }
    public GridCell(int x, int y) { X = x; Y = y; }

    public bool Equals(GridCell? other)
    {
        if (other == null)
        {
            return false;
        }
        
        return X == other.X && Y == other.Y;
    }

    public override bool Equals(object? obj)
    {
        return obj is GridCell other && Equals(other);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
