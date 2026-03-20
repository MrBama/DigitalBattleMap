using DigitalBattleMap.DataClasses;
using System;
using System.Globalization;
using System.Windows.Data;

namespace DigitalBattleMap.UIElements;

public class TokenOrientationToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var tokenOrientation = (TokenOrientation)value;
        var parameterSize = (TokenOrientation)parameter;

        if (tokenOrientation == parameterSize)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
