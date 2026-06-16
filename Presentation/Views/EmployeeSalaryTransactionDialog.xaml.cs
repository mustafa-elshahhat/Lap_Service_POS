using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AlJohary.ServiceHub.Domain.Entities;

namespace AlJohary.ServiceHub.Presentation.Views
{
    public partial class EmployeeSalaryTransactionDialog : Window
    {
        private readonly string _transactionType;

        public string DialogTitle { get; set; }
        public string EmployeeName { get; set; }
        public string AmountLabel { get; set; }
        public string AmountHelperText { get; set; }
        public System.Collections.Generic.List<string> PaymentMethodOptions => AlJohary.ServiceHub.Shared.Helpers.PaymentMethods.GetAll();
        public decimal Amount { get; private set; }
        public string PaymentMethod { get; private set; }
        public DateTime TransactionDate { get; private set; }
        public string Notes { get; private set; }

        public EmployeeSalaryTransactionDialog(Employee employee, string transactionType)
        {
            InitializeComponent();
            _transactionType = transactionType;
            EmployeeName = employee?.FullName ?? string.Empty;
            DialogTitle = transactionType == "salary" ? "💰 تسجيل صرف راتب" : "➖ تسجيل خصم موظف";
            if (transactionType == "salary")
            {
                AmountLabel = "المبلغ المدفوع نقداً *";
                AmountHelperText = "المبلغ الفعلي المسلَّم للموظف نقداً (يُحسب ضمن التدفق النقدي الخارج).";
            }
            else
            {
                AmountLabel = "مبلغ الخصم *";
                AmountHelperText = "خصم (يقلل التكلفة وليس نقداً) — لا يُحتسب كتدفق نقدي.";
            }
            DataContext = this;
            TransactionDatePicker.SelectedDate = DateTime.Today;
            PaymentMethodPanel.Visibility = transactionType == "salary" ? Visibility.Visible : Visibility.Collapsed;
            AmountTextBox.Focus();
        }

        private void DecimalTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = new Regex("[^0-9.]+").IsMatch(e.Text);
        }

        private static decimal ParseDecimal(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            string s = text.Trim();
            if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
                return result;
            string alt = s.Replace(",", ".");
            if (decimal.TryParse(alt, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                return result;
            return 0;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            decimal amount = ParseDecimal(AmountTextBox.Text);
            if (amount <= 0)
            {
                MessageBox.Show("يجب إدخال مبلغ أكبر من الصفر", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                AmountTextBox.Focus();
                return;
            }

            Amount = amount;
            PaymentMethod = PaymentMethodComboBox.SelectedItem as string ?? AlJohary.ServiceHub.Shared.Helpers.PaymentMethods.Cash;
            TransactionDate = (TransactionDatePicker.SelectedDate ?? DateTime.Today).Date.Add(DateTime.Now.TimeOfDay);
            Notes = NotesTextBox.Text;

            if (_transactionType == "deduction") PaymentMethod = null;

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
