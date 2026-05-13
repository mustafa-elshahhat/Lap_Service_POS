using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using CarPartsShopWPF.Shared.Helpers;
using CarPartsShopWPF.Application.Interfaces;
using CarPartsShopWPF.Presentation.Interfaces;
using CarPartsShopWPF.Application.Services;

namespace CarPartsShopWPF.Presentation.Views
{
    public partial class ReturnDetailsDialog : Window
    {
        private readonly IReturnService _returnService;
        private readonly int _returnId;

        public ReturnDetailsDialog(int returnId)
        {
            InitializeComponent();
            _returnService = ServiceContainer.GetService<IReturnService>();
            _returnId = returnId;
            Loaded += (s, e) => LoadDetails();
        }

        private void LoadDetails()
        {
            try
            {
                var returnInfo = _returnService.GetReturnById(_returnId);
                var items = _returnService.GetReturnItems(_returnId);

                if (returnInfo != null)
                {
                    ReturnNumberText.Text = $"رقم المرتجع: {SafeConvert.ToString(returnInfo["return_number"])}";
                    InvoiceNumberText.Text = SafeConvert.ToString(returnInfo["invoice_number"]);
                    ReturnDateText.Text = SafeConvert.ToString(returnInfo["return_date"]);
                    TotalAmountText.Text = Formatting.FormatCurrency(SafeConvert.ToDecimal(returnInfo["total_amount"]));
                }

                ItemsGrid.ItemsSource = items;
            }
            catch (System.Exception ex)
            {
                ServiceContainer.GetService<IDialogService>().ShowError("خطأ", ex.Message);
            }
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var returnInfo = _returnService.GetReturnById(_returnId);
                var itemsDicts = _returnService.GetReturnItems(_returnId);

                if (returnInfo == null) return;

                var returnEntity = new CarPartsShopWPF.Domain.Entities.Return
                {
                    Id = SafeConvert.ToInt(returnInfo["id"]),
                    ReturnNumber = SafeConvert.ToString(returnInfo["return_number"]),
                    ReturnDate = SafeConvert.ToDateTime(returnInfo["return_date"]) ?? DateTime.Now,
                    InvoiceNumber = SafeConvert.ToString(returnInfo["invoice_number"]),
                    TotalAmount = SafeConvert.ToDecimal(returnInfo["total_amount"]),
                    UserName = SafeConvert.ToString(returnInfo["user_name"]),
                    CustomerName = SafeConvert.ToString(returnInfo["customer_name"])
                };

                var returnItems = new List<CarPartsShopWPF.Domain.Entities.ReturnItem>();
                foreach (var item in itemsDicts)
                {
                    returnItems.Add(new CarPartsShopWPF.Domain.Entities.ReturnItem
                    {
                        ProductName = SafeConvert.ToString(item["product_name"]),
                        Quantity = SafeConvert.ToInt(item["quantity"]),
                        UnitPrice = SafeConvert.ToDecimal(item["unit_price"]),
                        TotalPrice = SafeConvert.ToDecimal(item["total_price"])
                    });
                }

                var printService = ServiceContainer.GetService<IPrintService>();
                printService.PrintReturnReceipt(returnEntity, returnItems);
            }
            catch (Exception ex)
            {
                ServiceContainer.GetService<IDialogService>().ShowError("خطأ", ex.Message);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
        }
    }
}
