using System;
using System.Globalization;
using System.Windows.Data;

namespace AlJohary.ServiceHub.Presentation.Converters
{
    /// <summary>
    /// Presentation-only converter: returns true when the bound numeric value is negative.
    /// Used to colour negative net-effect / balance figures red via a DataTrigger
    /// (display-only — the underlying value and sign are never changed). UI-tables §6.
    /// </summary>
    public class IsNegativeNumberConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return false;

            switch (value)
            {
                case decimal dec: return dec < 0m;
                case double dbl: return dbl < 0d;
                case float flt: return flt < 0f;
                case int i: return i < 0;
                case long l: return l < 0L;
            }

            var text = value.ToString();
            if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ||
                decimal.TryParse(text, NumberStyles.Any, culture, out parsed))
            {
                return parsed < 0m;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
