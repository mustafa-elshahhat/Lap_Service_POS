using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Domain.Entities;
using AlJohary.ServiceHub.Presentation.Interfaces;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Presentation.Views
{
    public partial class SupplierTransactionsPage : Page
    {
        private readonly ISupplierService _supplierService;
        private readonly IPrintService _printService;
        private readonly IDialogService _dialogService;
        private readonly int _supplierId;
        private Supplier _supplier;
        private List<Dictionary<string, object>> _transactions = new List<Dictionary<string, object>>();

        public SupplierTransactionsPage(int supplierId)
        {
            InitializeComponent();
            _supplierId = supplierId;
            _supplierService = ServiceContainer.GetService<ISupplierService>();
            _printService = ServiceContainer.GetService<IPrintService>();
            _dialogService = ServiceContainer.GetService<IDialogService>();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                _supplier = _supplierService.GetById(_supplierId);
                SupplierNameText.Text = _supplier?.Name ?? "-";
                CurrentDebtText.Text = Formatting.FormatCurrency(_supplier?.TotalDebt ?? 0);

                _transactions = _supplierService.GetSupplierTransactions(_supplierId);
                foreach (var transaction in _transactions)
                {
                    string type = SafeConvert.ToString(transaction["transaction_type"]);
                    transaction["transaction_type_ar"] = type == "purchase" ? "🛒 مشتريات" : "💵 دفعة";
                }

                TransactionsGrid.ItemsSource = _transactions;
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("خطأ", ex.Message);
            }
        }

        private void TransactionsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            NavigateToDetails(TransactionsGrid.SelectedItem as Dictionary<string, object>);
        }

        private void DetailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
                NavigateToDetails(element.DataContext as Dictionary<string, object>);
        }

        private void NavigateToDetails(Dictionary<string, object> transaction)
        {
            if (transaction == null) return;
            NavigationService?.Navigate(new SupplierTransactionDetailsPage(_supplierId, _supplier?.Name ?? "-", transaction));
        }

        private void PrintReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var itemsByTransaction = new Dictionary<int, List<SupplierPurchaseItem>>();
                foreach (var transaction in _transactions)
                {
                    int id = SafeConvert.ToInt(transaction["id"]);
                    string type = SafeConvert.ToString(transaction["transaction_type"]);
                    if (type == "purchase")
                        itemsByTransaction[id] = _supplierService.GetPurchaseItems(id);
                }

                _printService.PrintSupplierStatement(_supplier?.Name ?? "-", _transactions, itemsByTransaction);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("خطأ", "فشل الطباعة: " + ex.Message);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
                NavigationService.GoBack();
            else
                NavigationService?.Navigate(new SuppliersPage());
        }
    }
}
