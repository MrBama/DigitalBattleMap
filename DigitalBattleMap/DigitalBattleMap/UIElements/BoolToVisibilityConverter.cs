using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DigitalBattleMap.UIElements;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var visibile = (bool)value;
        if (visibile)
        {
            return Visibility.Visible;
        }
        else
        {
            return Visibility.Hidden;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var visibility = (Visibility)value;
        return visibility == Visibility.Visible;
    }
}
