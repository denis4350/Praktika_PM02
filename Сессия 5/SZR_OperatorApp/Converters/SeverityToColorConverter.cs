using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SZR_OperatorApp.Converters
{
    public class SeverityToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string severity = value as string;

            if (severity == "Критично" || severity == "Критическая" || severity == "Высокая")
                return Brushes.Red;
            if (severity == "Предупреждение" || severity == "Средняя")
                return Brushes.Orange;
            if (severity == "Информация" || severity == "Низкая")
                return Brushes.Blue;

            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}