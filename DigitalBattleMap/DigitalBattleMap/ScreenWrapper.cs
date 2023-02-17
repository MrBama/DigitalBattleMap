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
        public static int GetScreenCount()
        {
            return Screen.AllScreens.Count();
        }

        public static (int x, int y) GetScreenPosition(int screenNumber)
        {
            var scaleRatio = Math.Max(Screen.PrimaryScreen.WorkingArea.Width / SystemParameters.PrimaryScreenWidth,
                            Screen.PrimaryScreen.WorkingArea.Height / SystemParameters.PrimaryScreenHeight);

            var x = (int)(Screen.AllScreens[screenNumber].WorkingArea.Left / scaleRatio);
            var y = (int)(Screen.AllScreens[screenNumber].WorkingArea.Top / scaleRatio);

            return (x, y);
        }
    }
}
