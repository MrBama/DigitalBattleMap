using System;
using System.Globalization;
using System.Windows.Data;

namespace DigitalBattleMap
{
    public class TokenSizeToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var tokenSize = (TokenSize)value;
            var parameterSize = Enum.Parse<TokenSize>((string)parameter);

            if(tokenSize == parameterSize)
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
}
