using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SZR_LaboratoryApp.Converters
{
    public class StatusToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string status = value as string;

            switch (status)
            {
                case "Ожидает":
                    return new SolidColorBrush(Color.FromRgb(245, 245, 245));
                case "В работе":
                    return new SolidColorBrush(Color.FromRgb(255, 243, 224));
                case "Разрешена":
                    return new SolidColorBrush(Color.FromRgb(232, 245, 233));
                case "Заблокирована":
                    return new SolidColorBrush(Color.FromRgb(253, 235, 236));
                default:
                    return Brushes.White;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}