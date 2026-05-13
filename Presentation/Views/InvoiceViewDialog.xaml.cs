using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Presentation.Interfaces;
using AlJohary.ServiceHub.Presentation.ViewModels;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Presentation.Views
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

