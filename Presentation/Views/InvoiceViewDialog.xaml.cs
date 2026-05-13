using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using CarPartsShopWPF.Application.Interfaces;
using CarPartsShopWPF.Presentation.Interfaces;
using CarPartsShopWPF.Presentation.ViewModels;
using CarPartsShopWPF.Shared.Helpers;

namespace CarPartsShopWPF.Presentation.Views
{
    public partial class InvoiceViewDialog : Window
    {
        private InvoiceViewModel _viewModel;

        public InvoiceViewDialog(string invoiceNumber)
        {
            InitializeComponent();
            var dialogService = ServiceContainer.GetService<IDialogService>();
            _viewModel = new InvoiceViewModel(invoiceNumber, dialogService);

            _viewModel.CloseAction = () => this.Close();
            _viewModel.PrintAction = () => this.PrintInvoice();

            DataContext = _viewModel;
        }

        public void PrintInvoice()
        {
            try
            {
                if (_viewModel != null && _viewModel.PrintCommand.CanExecute(null))
                {
                    _viewModel.PrintCommand.Execute(null);
                }
            }
            catch (System.Exception ex)
            {
                ServiceContainer.GetService<IDialogService>().ShowError("خطأ", ex.Message);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

