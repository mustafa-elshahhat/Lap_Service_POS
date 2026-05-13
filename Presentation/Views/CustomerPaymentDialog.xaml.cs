using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CarPartsShopWPF.Presentation.ViewModels;

namespace CarPartsShopWPF.Presentation.Views
{
    public partial class CustomerPaymentDialog : Window
    {
        private CustomerPaymentViewModel _viewModel;

        public bool DataChanged => _viewModel?.DataChanged ?? false;

        public CustomerPaymentDialog(Dictionary<string, object> customer)
        {
            InitializeComponent();
            _viewModel = new CustomerPaymentViewModel(customer);
            _viewModel.CloseAction = () => this.Close();
            DataContext = _viewModel;

            this.Loaded += (s, e) => 
            {

            };
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
        }

        private void InvoicesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _viewModel?.UpdateSelection(InvoicesGrid.SelectedItems);
        }

        private void PaymentAmountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
        
        private void PayButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}

