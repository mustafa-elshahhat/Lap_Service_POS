using System;
using System.Collections.Generic;
using System.Windows;
using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Presentation.Interfaces;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Presentation.Views
{
    public partial class SupplierTransactionsDialog : Window
    {
        private readonly ISupplierService _supplierService;
        private readonly int _supplierId;
        private readonly string _supplierName;

        public SupplierTransactionsDialog(int supplierId, string supplierName)
        {
            InitializeComponent();
            _supplierService = ServiceContainer.GetService<ISupplierService>();
            _supplierId = supplierId;
            _supplierName = supplierName;

            SupplierNameText.Text = supplierName;
            LoadTransactions();
        }

        private void LoadTransactions()
        {
            try
            {
                var transactions = _supplierService.GetSupplierTransactions(_supplierId);

                foreach (var t in transactions)
                {
                    string type = SafeConvert.ToString(t["transaction_type"]);
                    t["transaction_type_ar"] = type == "purchase" ? "🛒 مشتريات" : "💵 دفعة";
                }

                TransactionsGrid.ItemsSource = transactions;
            }
            catch (System.Exception ex)
            {
                ServiceContainer.GetService<IDialogService>().ShowError("خطأ", ex.Message);
            }
        }

        private void PrintReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var transactions = TransactionsGrid.ItemsSource as IEnumerable<Dictionary<string, object>>;
                if (transactions == null) return;

                var printData = new List<Dictionary<string, object>>();
                foreach(var item in transactions)
                {
                    printData.Add(new Dictionary<string, object>
                    {
                        { "Id", item["id"] },
                        { "Date", (SafeConvert.ToDateTime(item["transaction_date"]) ?? DateTime.Now).ToString("yyyy-MM-dd") },
                        { "Type", item["transaction_type_ar"] },
                        { "Amount", Formatting.FormatCurrency(SafeConvert.ToDecimal(item["amount"])) },
                        { "Balance", Formatting.FormatCurrency(SafeConvert.ToDecimal(item["balance_after"])) },
                        { "User", item["created_by"] }
                    });
                }

                string title = $"كشف حساب مورد - {_supplierName}";
                string[] cols = { "User", "Balance", "Amount", "Type", "Date", "Id" };
                string[] headers = { "بواسطة", "الرصيد", "المبلغ", "النوع", "التاريخ", "#" };

                var printService = ServiceContainer.GetService<IPrintService>();
                printService.PrintReport(title, printData, cols, headers);
            }
            catch (Exception ex)
            {
                ServiceContainer.GetService<IDialogService>().ShowError("خطأ", "فشل الطباعة: " + ex.Message);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
