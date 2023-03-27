using DigitalBattleMap.Common;
using DigitalBattleMapServer.Application;

namespace DigitalBattleMapServer.Utility;

public static class EnumExtensions
{
    private static readonly int DirectionMax = Enum.GetValues(typeof(Direction)).Cast<int>().Max();
    
    public static Direction GetOrientatedDirection(this Direction direction, Orientation orientation)
    {
        int orientationValue = (int)orientation * 2;
        int directionValue = (int)direction;
        
        int newDirection = directionValue + orientationValue;
        if (newDirection > DirectionMax)
            newDirection -= DirectionMax + 1;

        return (Direction)newDirection;
    }
}