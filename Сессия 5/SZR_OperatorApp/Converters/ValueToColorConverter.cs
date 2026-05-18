using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SZR_OperatorApp.Converters
{
    public class ValueToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return Brushes.Black;

            decimal val;
            if (value is decimal dec)
                val = dec;
            else if (value is int intVal)
                val = intVal;
            else if (value is double dbl)
                val = (decimal)dbl;
            else
                return Brushes.Black;

            if (parameter != null)
            {
                string[] parts = parameter.ToString().Split(',');
                if (parts.Length == 2)
                {
                    if (decimal.TryParse(parts[0], out decimal min) && decimal.TryParse(parts[1], out decimal max))
                    {
                        if (val < min) return Brushes.Orange;
                        if (val > max) return Brushes.Red;
                        return Brushes.Green;
                    }
                }
            }

            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}