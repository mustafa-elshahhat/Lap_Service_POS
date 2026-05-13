using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CarPartsShopWPF.Presentation.ViewModels;
using CarPartsShopWPF.Shared.Helpers;
using CarPartsShopWPF.Application.Interfaces;
using CarPartsShopWPF.Presentation.Interfaces;
using CarPartsShopWPF.Application.Services;

namespace CarPartsShopWPF.Presentation.Views
{
    public partial class CustomerInvoicesDialog : Window
    {
        private CustomerInvoicesViewModel _viewModel;

        public CustomerInvoicesDialog(int customerId, string customerName)
        {
            InitializeComponent();
            var dialogService = ServiceContainer.GetService<IDialogService>();
            _viewModel = new CustomerInvoicesViewModel(customerId, customerName, dialogService);
            _viewModel.CloseAction = () => this.Close();
            DataContext = _viewModel;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
        
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
        }


        private void FilterType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_viewModel == null) return;
            if (FilterType.SelectedItem is ComboBoxItem item)
            {
                string tag = item.Tag?.ToString();
                _viewModel.FilterType = string.IsNullOrEmpty(tag) ? "All" : tag;
            }
        }

        private void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.ViewDetailsCommand.CanExecute(null))
                _viewModel.ViewDetailsCommand.Execute(null);
        }

        private void InvoicesGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
             if (_viewModel.ViewDetailsCommand.CanExecute(null))
                _viewModel.ViewDetailsCommand.Execute(null);
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
             if (_viewModel.PrintCommand.CanExecute(null))
                _viewModel.PrintCommand.Execute(null);
        }

        private void PrintStatement_Click(object sender, RoutedEventArgs e)
        {
             if (_viewModel.PrintStatementCommand.CanExecute(null))
                _viewModel.PrintStatementCommand.Execute(null);
        }
    }
}

