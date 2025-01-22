using DigitalBattleMap.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;

namespace DigitalBattleMap;

public static class ImageTester
{
    private static Dictionary<string, Window> _windows = new();

    public static void ShowImage(Bitmap bitmap, string windowName)
    {
        if(windowName == null)
        {
            throw new ArgumentNullException(nameof(windowName));
        }

        var window = GetWindow(windowName);
        var grid = new Grid();

        var image = new System.Windows.Controls.Image
        {
            Source = bitmap.ToBitmapImage()
        };
        grid.Children.Add(image);
        window.Content = grid;

        window.Show();
    }

    private static Window GetWindow(string windowName)
    {
        if(_windows.ContainsKey(windowName))
        {
            return _windows[windowName];
        }
        else
        {
            return CreateWindow(windowName);
        }
    }

    private static Window CreateWindow(string windowName)
    {
        var window = new Window
        {
            Title = windowName
        };
        window.Closed += WindowClosed;
        _windows[windowName] = window;
        return window;
    }

    private static void WindowClosed(object? sender, System.EventArgs e)
    {
        var window = (Window)sender!;
        _windows.Remove(window.Title);
    }
}
