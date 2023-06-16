using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace EnterpriseMessengerUI.Converters
{
    public class MathDivideConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return (double?)value / (double?)parameter;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return (double?)value * (double?)parameter;
        }
    }
}
