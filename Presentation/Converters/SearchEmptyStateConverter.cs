using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AlJohary.ServiceHub.Presentation.Converters
{
    /// <summary>
    /// Presentation-only converter that distinguishes "no data at all" from
    /// "search returned nothing" on searchable list pages (UI-tables H4).
    /// Inputs (in order): [0] HasItems (bool), [1] SearchText (string).
    /// ConverterParameter selects which overlay this instance drives:
    ///   - "NoResults": Visible when HasItems == false AND SearchText is non-empty.
    ///   - otherwise ("NoData"): Visible when HasItems == false AND SearchText is empty.
    /// No data/command logic — purely drives overlay visibility.
    /// </summary>
    public class SearchEmptyStateConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool hasItems = values != null && values.Length > 0 && values[0] is bool b && b;
            string searchText = values != null && values.Length > 1 ? values[1] as string : null;
            bool hasSearch = !string.IsNullOrWhiteSpace(searchText);

            bool isNoResultsMode = string.Equals(parameter as string, "NoResults", StringComparison.OrdinalIgnoreCase);

            bool visible = isNoResultsMode
                ? (!hasItems && hasSearch)
                : (!hasItems && !hasSearch);

            return visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
