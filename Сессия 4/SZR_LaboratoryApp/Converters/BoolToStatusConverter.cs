using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SZR_LaboratoryApp.Converters
{
    public class BoolToStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "—";
            return (bool)value ? "✓ Соответствует" : "✗ Не соответствует";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}