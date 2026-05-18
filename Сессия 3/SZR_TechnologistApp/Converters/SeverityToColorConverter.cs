using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SZR_TechnologistApp.Converters
{
    public class SeverityToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return new SolidColorBrush(Colors.Gray);
            string severity = value as string;
            if (severity == "Критично") return new SolidColorBrush(Colors.Red);
            if (severity == "Предупреждение") return new SolidColorBrush(Colors.Orange);
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}