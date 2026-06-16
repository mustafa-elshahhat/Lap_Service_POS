using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AlJohary.ServiceHub.Infrastructure.Data;
using AlJohary.ServiceHub.Application.Services;
using AlJohary.ServiceHub.Shared.Helpers;

using AlJohary.ServiceHub.Presentation.ViewModels;
using AlJohary.ServiceHub.Presentation.Helpers;

namespace AlJohary.ServiceHub.Presentation.Views
{

    public partial class SuppliersPage : Page
    {
        public SuppliersPage()
        {
            InitializeComponent();
            var vm = new SuppliersViewModel();
            vm.NavigateToTransactionsAction = supplier =>
            {
                NavigationService?.Navigate(new SupplierTransactionsPage(supplier.Id));
            };
            DataContext = vm;
            FocusHelper.SetupSearchFocus(this, SearchTextBox);
        }

    }
}
