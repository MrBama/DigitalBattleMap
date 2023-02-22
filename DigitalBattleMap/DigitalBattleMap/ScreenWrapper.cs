using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace DigitalBattleMap
{
    /// <summary>
    /// Unfortunately, there is no equivalent of the Screen class in WPF.
    /// This class is created to wrap System.Windows.Forms.
    /// </summary>
    public static class ScreenWrapper
    {
        public static List<ScreenPosition> GetScreenPositions()
        {
            var screenPositions = new List<ScreenPosition>();

            var scaleRatio = Math.Max(Screen.PrimaryScreen.WorkingArea.Width / SystemParameters.PrimaryScreenWidth,
                            Screen.PrimaryScreen.WorkingArea.Height / SystemParameters.PrimaryScreenHeight);

            foreach (var screen in Screen.AllScreens)
            {
                var x = (int)(screen.WorkingArea.Left / scaleRatio);
                var y = (int)(screen.WorkingArea.Top / scaleRatio);
                screenPositions.Add(new ScreenPosition { X = x, Y = y });
            }

            return screenPositions;
        }
    }

    public class ScreenPosition
    {
        public int X { get; set; }
        public int Y { get; set; }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            return obj is ScreenPosition other && other.X == X && other.Y == Y;
        }

        public override string ToString()
        {
            return $"X: {X} Y: {Y}";
        }
    }
}
