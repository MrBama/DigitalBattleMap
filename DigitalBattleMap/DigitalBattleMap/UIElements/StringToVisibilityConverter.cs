using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DigitalBattleMap.UIElements;

public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var visible = value != null && value as string != string.Empty;
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
        throw new NotImplementedException();
    }
}
