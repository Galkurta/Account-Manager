using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RobloxAccountManager.Converters
{
    public class GroupDeletionVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string groupName)
            {
                // Cannot delete "Default" group
                if (string.Equals(groupName, "Default", StringComparison.OrdinalIgnoreCase))
                {
                    return Visibility.Collapsed;
                }
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
