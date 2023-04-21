using DigitalBattleMap.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace DigitalBattleMap.UIElements
{
    public class ConditionToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var conditions = (List<Condition>)value;
            var parameterCondition = Enum.Parse<Condition>((string)parameter);

            return conditions.Contains(parameterCondition);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
