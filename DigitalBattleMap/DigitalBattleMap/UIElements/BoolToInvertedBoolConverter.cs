using System;
using System.Globalization;
using System.Windows.Data;

namespace DigitalBattleMap.UIElements;

class BoolToInvertedBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var boolean = (bool)value;
        return !boolean;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var boolean = (bool)value;
        return !boolean;
    }
}

