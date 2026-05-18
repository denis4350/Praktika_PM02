using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SZR_OperatorApp.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string status = value as string;

            switch (status)
            {
                case "Не начат":
                    return Brushes.Gray;
                case "Выполняется":
                    return Brushes.Orange;
                case "Завершен":
                    return Brushes.Green;
                default:
                    return Brushes.Black;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}