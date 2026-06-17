using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AlJohary.ServiceHub.Presentation.Converters
{
    /// <summary>
    /// Converts a boolean to Visibility inverted: false -> Visible, true -> Collapsed.
    /// Used for table empty-state overlays bound to a DataGrid's HasItems. UI-011.
    /// </summary>
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = value is bool b && b;
            return flag ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility v && v != Visibility.Visible;
        }
    }
}
