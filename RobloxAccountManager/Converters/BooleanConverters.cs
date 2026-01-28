using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RobloxAccountManager.Converters
{
    public class BoolToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                string param = parameter as string ?? "True|False";
                var parts = param.Split('|');
                if (parts.Length == 2)
                {
                    return b ? parts[0] : parts[1];
                }
            }
            return value?.ToString() ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                bool inverse = parameter as string == "Inverse";
                bool isGreen = inverse ? !b : b;
                
                return isGreen ? Brushes.LightGreen : Brushes.Red;
            }
            return Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
