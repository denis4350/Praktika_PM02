using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SZR_OperatorApp.Converters
{
    public class StringToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string status = value as string;

            switch (status)
            {
                case "В работе":
                    return new SolidColorBrush(Color.FromRgb(39, 174, 96));
                case "Подготовлена":
                    return new SolidColorBrush(Color.FromRgb(52, 152, 219));
                case "Ожидает контроля":
                    return new SolidColorBrush(Color.FromRgb(243, 156, 18));
                case "Завершена":
                    return new SolidColorBrush(Color.FromRgb(46, 204, 113));
                case "Заблокирована":
                    return new SolidColorBrush(Color.FromRgb(231, 76, 60));
                default:
                    return new SolidColorBrush(Color.FromRgb(149, 165, 166));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}