using System.Windows;
using AlJohary.ServiceHub.Application.DTOs;
using AlJohary.ServiceHub.Presentation.ViewModels;

namespace AlJohary.ServiceHub.Presentation.Views
{
    public partial class SupplierPurchaseDialog : Window
    {
        private readonly SupplierPurchaseDialogViewModel _viewModel;

        public SupplierPurchaseDialogResult Result => _viewModel.Result;

        public SupplierPurchaseDialog(string supplierName, decimal currentDebt)
        {
            InitializeComponent();
            _viewModel = new SupplierPurchaseDialogViewModel(supplierName, currentDebt);
            _viewModel.CloseAction = CloseWithResult;
            DataContext = _viewModel;
        }

        private void CloseWithResult(bool result)
        {
            DialogResult = result;
            Close();
        }
    }
}
