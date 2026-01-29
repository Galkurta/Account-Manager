using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using RobloxAccountManager.Services;

namespace RobloxAccountManager.Converters
{
    public class LogLevelToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is LogLevel level)
            {
                return level switch
                {
                    LogLevel.Info => Brushes.DarkGray,
                    LogLevel.Warning => Brushes.Yellow,
                    LogLevel.Error => Brushes.Red,
                    LogLevel.Success => Brushes.LightGreen,
                    _ => Brushes.White
                };
            }
            return Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
