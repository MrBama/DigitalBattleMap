using DigitalBattleMap.DataClasses;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Forms;

namespace DigitalBattleMap.Utilities;

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
