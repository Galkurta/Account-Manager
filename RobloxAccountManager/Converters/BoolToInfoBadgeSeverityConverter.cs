using System;
using System.Globalization;
using System.Windows.Data;
using Wpf.Ui.Controls;

namespace RobloxAccountManager.Converters
{
    public class BoolToInfoBadgeSeverityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                bool isInverse = parameter as string == "Inverse";
                bool check = isInverse ? !boolValue : boolValue;

                return check ? InfoBadgeSeverity.Success : InfoBadgeSeverity.Critical;
            }
            return InfoBadgeSeverity.Informational;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
