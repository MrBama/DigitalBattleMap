using System;
using System.Globalization;
using System.Windows.Data;

namespace DigitalBattleMap.UIElements;

public class ListViewItemSizeConverter : IValueConverter
{
    private const int _listViewItemMargin = 12;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var originalSize = (double)value;
        var sizeFactor = double.Parse((string)parameter, CultureInfo.InvariantCulture);

        var calculatedSize = (originalSize * sizeFactor) - _listViewItemMargin;
        return Math.Max(0, calculatedSize); // Prevent negative values
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var originalSize = (double)value;
        var sizeFactor = double.Parse((string)parameter, CultureInfo.InvariantCulture);

        return (originalSize + _listViewItemMargin) / sizeFactor;
    }
}
