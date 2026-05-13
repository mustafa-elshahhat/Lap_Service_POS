using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Presentation.Interfaces;
using AlJohary.ServiceHub.Presentation.ViewModels;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Presentation.Views
{
    public partial class SupplierFormDialog : Window
    {
        private readonly SupplierFormViewModel _viewModel;

        public string SupplierName => _viewModel.Name;
        public string PhoneNumber => _viewModel.Phone;
        public string Address => _viewModel.Address;

        public SupplierFormDialog(SupplierFormViewModel viewModel = null)
        {
            InitializeComponent();
            _viewModel = viewModel ?? new SupplierFormViewModel();
            DataContext = _viewModel;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.Validate(out string error))
            {
                DialogResult = true;
                Close();
            }
            else
            {
                ServiceContainer.GetService<IDialogService>().ShowWarning("تنبيه", error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void PhoneTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}

