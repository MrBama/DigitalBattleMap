using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Utilities;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DigitalBattleMap.UIElements;

public class DrawingColorToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var color = (Color)value;
        var drawingButton = (DrawingButton)parameter;

        if (color == drawingButton.ToColor())
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

