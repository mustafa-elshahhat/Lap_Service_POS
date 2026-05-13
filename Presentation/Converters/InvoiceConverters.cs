using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using AlJohary.ServiceHub.Domain.Entities;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Presentation.Converters
{
    public class SaleTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var type = value as string;
            if (string.IsNullOrEmpty(type)) return "";
            return type.ToLower() switch
            {
                "cash" => "نقدي",
                _ => type
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class InvoiceStatusTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Sale sale)
            {
                if (sale.RemainingAmount <= 0.01m) return "خالصة";
                if (sale.PaidAmount > 0) return "مدفوعة جزئياً";
                return "غير مدفوعة";
            }
            return "";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class InvoiceStatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Sale sale)
            {
                if (sale.RemainingAmount <= 0.01m) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DCFCE7"));
                if (sale.PaidAmount > 0) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF9C3"));
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEE2E2"));
            }
            return Brushes.Transparent;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class InvoiceStatusTextColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
             if (value is Sale sale)
            {
                if (sale.RemainingAmount <= 0.01m) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#166534"));
                if (sale.PaidAmount > 0) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#854D0E"));
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#991B1B"));
            }
            return Brushes.Black;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
