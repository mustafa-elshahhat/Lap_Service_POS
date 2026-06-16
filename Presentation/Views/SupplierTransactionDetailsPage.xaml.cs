using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Presentation.Interfaces;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Presentation.Views
{
    public partial class SupplierTransactionDetailsPage : Page
    {
        private readonly ISupplierService _supplierService;
        private readonly IDialogService _dialogService;
        private readonly int _supplierId;
        private readonly string _supplierName;
        private readonly Dictionary<string, object> _transaction;

        public SupplierTransactionDetailsPage(int supplierId, string supplierName, Dictionary<string, object> transaction)
        {
            InitializeComponent();
            _supplierId = supplierId;
            _supplierName = supplierName;
            _transaction = transaction;
            _supplierService = ServiceContainer.GetService<ISupplierService>();
            _dialogService = ServiceContainer.GetService<IDialogService>();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                SupplierNameText.Text = _supplierName;
                TransactionDateText.Text = SafeConvert.ToString(_transaction["transaction_date"]);
                AmountText.Text = Formatting.FormatCurrency(SafeConvert.ToDecimal(_transaction["amount"]));
                PaidAmountText.Text = Formatting.FormatCurrency(SafeConvert.ToDecimal(_transaction.ContainsKey("paid_amount") ? _transaction["paid_amount"] : 0));
                BalanceAfterText.Text = Formatting.FormatCurrency(SafeConvert.ToDecimal(_transaction["balance_after"]));
                string type = SafeConvert.ToString(_transaction["transaction_type"]);
                TypeText.Text = type == "purchase" ? "مشتريات" : "دفعة";

                int transactionId = SafeConvert.ToInt(_transaction["id"]);
                var items = _supplierService.GetPurchaseItems(transactionId);
                ItemsGrid.ItemsSource = items;
                EmptyItemsText.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (System.Exception ex)
            {
                _dialogService.ShowError("خطأ", ex.Message);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
                NavigationService.GoBack();
            else
                NavigationService?.Navigate(new SupplierTransactionsPage(_supplierId));
        }
    }
}
