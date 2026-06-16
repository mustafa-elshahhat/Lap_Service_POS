using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Presentation.Converters
{
    public class FlexibleNumberConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Formatting.FormatNumber(value, GetMaxDecimals(parameter));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType == typeof(int) || targetType == typeof(int?))
                return SafeConvert.ToInt(value);
            if (targetType == typeof(double) || targetType == typeof(double?))
                return (double)SafeConvert.ToDecimal(value);
            if (targetType == typeof(string))
                return value?.ToString() ?? string.Empty;
            return SafeConvert.ToDecimal(value);
        }

        private static int GetMaxDecimals(object parameter)
        {
            return parameter != null && int.TryParse(parameter.ToString(), out int maxDecimals)
                ? maxDecimals
                : 2;
        }
    }

    public class FlexibleCurrencyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool includeSymbol = parameter == null || !bool.TryParse(parameter.ToString(), out bool parsed) || parsed;
            return Formatting.FormatCurrency(value, includeSymbol);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return targetType == typeof(string) ? value?.ToString() ?? string.Empty : Binding.DoNothing;
        }
    }
}
