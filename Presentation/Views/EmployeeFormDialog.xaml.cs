using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using AlJohary.ServiceHub.Presentation.Interfaces;
using AlJohary.ServiceHub.Presentation.ViewModels;

namespace AlJohary.ServiceHub.Presentation.Views
{
    public partial class EmployeeFormDialog : Window
    {
        private readonly EmployeeFormViewModel _viewModel;

        public EmployeeFormDialog(EmployeeFormViewModel viewModel = null)
        {
            InitializeComponent();
            _viewModel = viewModel ?? new EmployeeFormViewModel();
            DataContext = _viewModel;
            FullNameTextBox.Focus();
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
            e.Handled = new Regex("[^0-9]+").IsMatch(e.Text);
        }

        private void DecimalTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = new Regex("[^0-9.]+").IsMatch(e.Text);
        }
    }
}
