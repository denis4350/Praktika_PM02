using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SZR_LaboratoryApp.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string status = value as string;

            switch (status)
            {
                case "Ожидает":
                case "Создано":
                    return Brushes.Gray;
                case "В работе":
                    return Brushes.Orange;
                case "Разрешена":
                case "Соответствует":
                    return Brushes.Green;
                case "Заблокирована":
                case "Не соответствует":
                    return Brushes.Red;
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