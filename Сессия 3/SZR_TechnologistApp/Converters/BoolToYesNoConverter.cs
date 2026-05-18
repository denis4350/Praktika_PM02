using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SZR_TechnologistApp.Converters
{
    /// <summary>
    /// Конвертер bool → "Да"/"Нет"
    /// </summary>
    public class BoolToYesNoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool b && b) ? "Да" : "Нет";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}