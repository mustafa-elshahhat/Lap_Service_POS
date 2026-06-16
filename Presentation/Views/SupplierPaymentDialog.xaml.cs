using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Presentation.Views
{
    public partial class SupplierPaymentDialog : Window
    {
        public string SupplierName { get; set; }
        public string CurrentDebtText { get; set; }
        public System.Collections.Generic.List<string> PaymentMethodOptions => PaymentMethods.GetAll();

        public decimal PaymentAmount { get; private set; }
        public string PaymentMethod { get; private set; }

        private readonly decimal _currentDebt;

        public SupplierPaymentDialog(string supplierName, decimal currentDebt)
        {
            InitializeComponent();
            _currentDebt = currentDebt;
            SupplierName = supplierName;
            CurrentDebtText = Formatting.FormatCurrency(currentDebt);
            DataContext = this;
            AmountTextBox.Focus();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9.]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void AmountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidatePaymentAmount();
        }

        private void ValidatePaymentAmount()
        {
            if (PaymentValidationText == null || SaveButton == null || RemainingAfterText == null) return;

            decimal amount = ParseDecimal(AmountTextBox?.Text);

            if (amount <= 0)
            {
                PaymentValidationText.Text = "⚠ يجب إدخال مبلغ أكبر من الصفر";
                PaymentValidationText.Visibility = string.IsNullOrWhiteSpace(AmountTextBox?.Text)
                    ? Visibility.Collapsed
                    : Visibility.Visible;
                SetBorderState(AmountTextBox, false);
                RemainingAfterText.Visibility = Visibility.Collapsed;
                SaveButton.IsEnabled = false;
                return;
            }

            if (amount > _currentDebt)
            {
                PaymentValidationText.Text =
                    $"⚠ مبلغ السداد ({Formatting.FormatCurrency(amount)}) يتجاوز المديونية الحالية ({Formatting.FormatCurrency(_currentDebt)})";
                PaymentValidationText.Visibility = Visibility.Visible;
                SetBorderState(AmountTextBox, false);
                RemainingAfterText.Visibility = Visibility.Collapsed;
                SaveButton.IsEnabled = false;
                return;
            }

            PaymentValidationText.Visibility = Visibility.Collapsed;
            SetBorderState(AmountTextBox, true);

            decimal remaining = _currentDebt - amount;
            RemainingAfterText.Text = remaining > 0
                ? $"المتبقي بعد السداد: {Formatting.FormatCurrency(remaining)}"
                : "سيتم تصفية المديونية بالكامل";
            RemainingAfterText.Foreground = remaining > 0
                ? (Brush)TryFindResource("InfoBrush") ?? Brushes.SteelBlue
                : new SolidColorBrush(Color.FromRgb(22, 163, 74));
            RemainingAfterText.Visibility = Visibility.Visible;

            SaveButton.IsEnabled = true;
        }

        private static void SetBorderState(TextBox tb, bool valid)
        {
            if (tb == null) return;
            if (!valid)
            {
                tb.BorderBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));
                tb.BorderThickness = new Thickness(1.5);
            }
            else
            {
                tb.ClearValue(Control.BorderBrushProperty);
                tb.ClearValue(Control.BorderThicknessProperty);
            }
        }

        private static decimal ParseDecimal(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            string s = text.Trim();
            if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal r))
                return Math.Max(0, r);
            string alt = s.Replace(".", "").Replace(",", ".");
            if (decimal.TryParse(alt, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal r2))
                return Math.Max(0, r2);
            return 0;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            decimal amount = ParseDecimal(AmountTextBox.Text);

            if (amount <= 0)
            {
                MessageBox.Show("الرجاء إدخال مبلغ سداد صحيح أكبر من الصفر",
                    "خطأ في الإدخال", MessageBoxButton.OK, MessageBoxImage.Warning);
                AmountTextBox.Focus();
                return;
            }

            if (amount > _currentDebt)
            {
                MessageBox.Show(
                    $"مبلغ السداد ({Formatting.FormatCurrency(amount)}) يتجاوز المديونية الحالية ({Formatting.FormatCurrency(_currentDebt)})",
                    "خطأ في الإدخال", MessageBoxButton.OK, MessageBoxImage.Warning);
                AmountTextBox.Focus();
                return;
            }

            PaymentAmount = amount;

            PaymentMethod = PaymentMethodComboBox.SelectedItem as string ?? PaymentMethods.Cash;

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
