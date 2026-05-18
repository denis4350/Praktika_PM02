using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SZR_TechnologistApp.Converters
{
    /// <summary>
    /// Универсальный конвертер видимости по статусу.
    /// ConverterParameter: строка со статусами, разделёнными точкой с запятой (;) или одна строка.
    /// </summary>
    public class StatusToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            string status = value.ToString().Trim();

            string paramStr = parameter.ToString();
            if (string.IsNullOrWhiteSpace(paramStr))
                return Visibility.Collapsed;

            // Разделяем по ";" на несколько возможных статусов
            string[] validStatuses = paramStr.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var s in validStatuses)
            {
                if (status.Equals(s.Trim(), StringComparison.InvariantCultureIgnoreCase))
                    return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}