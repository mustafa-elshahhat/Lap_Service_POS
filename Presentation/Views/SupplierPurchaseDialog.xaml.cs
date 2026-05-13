using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CarPartsShopWPF.Shared.Helpers;

namespace CarPartsShopWPF.Presentation.Views
{
    public partial class SupplierPurchaseDialog : Window
    {
        public string SupplierName { get; set; }
        public string CurrentDebtText { get; set; }

        public decimal PurchaseAmount { get; private set; }
        public decimal PaidAmount { get; private set; }
        public string PaymentMethod { get; private set; }

        private decimal _purchaseTotal = 0;

        public SupplierPurchaseDialog(string supplierName, decimal currentDebt)
        {
            InitializeComponent();
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
            _purchaseTotal = ParseDecimal(AmountTextBox.Text);
            ValidateAll();
        }

        private void PaidTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateAll();
        }

        private void ValidateAll()
        {
            if (SaveButton == null || PaidValidationText == null ||
                AmountValidationText == null || RemainingDebtText == null) return;

            decimal paid = ParseDecimal(PaidTextBox?.Text);

            bool purchaseValid = _purchaseTotal > 0;
            bool paidValid = paid >= 0 && paid <= _purchaseTotal;

            if (!purchaseValid)
            {
                AmountValidationText.Text = "⚠ يجب إدخال قيمة مشتريات أكبر من الصفر";
                AmountValidationText.Visibility = Visibility.Visible;
                SetBorderInvalid(AmountTextBox);
                PaidValidationText.Visibility = Visibility.Collapsed;
                RemainingDebtText.Text = "سيتم إضافة المتبقي للمديونية";
                SaveButton.IsEnabled = false;
                return;
            }

            AmountValidationText.Visibility = Visibility.Collapsed;
            SetBorderValid(AmountTextBox);

            if (!paidValid)
            {
                PaidValidationText.Text =
                    $"⚠ المبلغ المقدم ({Formatting.FormatCurrency(paid)}) يتجاوز قيمة المشتريات ({Formatting.FormatCurrency(_purchaseTotal)})";
                PaidValidationText.Visibility = Visibility.Visible;
                SetBorderInvalid(PaidTextBox);
                RemainingDebtText.Text = string.Empty;
                SaveButton.IsEnabled = false;
                return;
            }

            PaidValidationText.Visibility = Visibility.Collapsed;
            SetBorderValid(PaidTextBox);

            decimal remaining = _purchaseTotal - paid;
            RemainingDebtText.Text = remaining > 0
                ? $"المتبقي المضاف للدين: {Formatting.FormatCurrency(remaining)}"
                : "سيتم سداد المشتريات بالكامل — لن يُضاف دين";

            SaveButton.IsEnabled = true;
        }

        private static void SetBorderInvalid(TextBox tb)
        {
            if (tb == null) return;
            tb.BorderBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));
            tb.BorderThickness = new Thickness(1.5);
        }

        private static void SetBorderValid(TextBox tb)
        {
            if (tb == null) return;
            tb.ClearValue(Control.BorderBrushProperty);
            tb.ClearValue(Control.BorderThicknessProperty);
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
            decimal paid   = ParseDecimal(PaidTextBox.Text);

            if (amount <= 0)
            {
                MessageBox.Show("الرجاء إدخال قيمة مشتريات صحيحة أكبر من الصفر",
                    "خطأ في الإدخال", MessageBoxButton.OK, MessageBoxImage.Warning);
                AmountTextBox.Focus();
                return;
            }

            if (paid < 0 || paid > amount)
            {
                MessageBox.Show(
                    $"المبلغ المقدم يجب أن يكون بين 0 و {Formatting.FormatCurrency(amount)}",
                    "خطأ في الإدخال", MessageBoxButton.OK, MessageBoxImage.Warning);
                PaidTextBox.Focus();
                return;
            }

            PurchaseAmount = amount;
            PaidAmount     = paid;

            PaymentMethod = PaymentMethodComboBox.SelectedItem is ComboBoxItem item
                ? item.Content.ToString()
                : "نقدي";

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
