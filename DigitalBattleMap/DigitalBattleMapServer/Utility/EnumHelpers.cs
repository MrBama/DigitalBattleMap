using DigitalBattleMap.Common;

namespace DigitalBattleMapServer.Utility;

public static class OrientationHelper
{
    public static string GetAwesomeFontIconByOrientation(Orientation orientation)
    {
        return orientation switch
        {
            Orientation.Down => "fa-arrow-circle-down",
            Orientation.Right => "fa-arrow-circle-right",
            Orientation.Up => "fa-arrow-circle-up",
            Orientation.Left => "fa-arrow-circle-left",
            _ => "fa-exclamation-circle"
        };
    }
}