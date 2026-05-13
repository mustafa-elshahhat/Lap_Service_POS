using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using CarPartsShopWPF.Shared.Helpers;

namespace CarPartsShopWPF.Presentation.Views
{
    public partial class SupplierPaymentDialog : Window
    {
        public string SupplierName { get; set; }
        public string CurrentDebtText { get; set; }

        public decimal PaymentAmount { get; private set; }
        public string PaymentMethod { get; private set; }

        public SupplierPaymentDialog(string supplierName, decimal currentDebt)
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

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(AmountTextBox.Text, out decimal amount) && amount > 0)
            {
                PaymentAmount = amount;
                
                if (PaymentMethodComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem item)
                {
                    PaymentMethod = item.Content.ToString();
                }
                else
                {
                    PaymentMethod = "نقدي";
                }

                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("الرجاء إدخال مبلغ سداد صحيح", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
