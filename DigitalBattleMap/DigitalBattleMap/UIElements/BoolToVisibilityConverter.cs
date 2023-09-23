using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DigitalBattleMap.UIElements;

public class BoolToVisibilityConverter : IValueConverter
{
    public bool IsInverted { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var visible = (bool)value;
        visible = IsInverted ? !visible : visible;

        if (visible)
        {
            return Visibility.Visible;
        }
        else
        {
            return (Visibility)parameter;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var visibility = (Visibility)value;
        var visible = visibility == Visibility.Visible;
        visible = IsInverted ? !visible : visible;

        return visible;
    }
}
