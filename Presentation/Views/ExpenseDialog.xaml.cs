using System.Windows;
using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Presentation.Interfaces;
using AlJohary.ServiceHub.Presentation.ViewModels;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Presentation.Views
{
    public partial class ExpenseDialog : Window
    {
        private readonly ExpenseFormViewModel _viewModel;

        public ExpenseDialog(ExpenseFormViewModel viewModel = null)
        {
            InitializeComponent();
            _viewModel = viewModel ?? new ExpenseFormViewModel();
            DataContext = _viewModel;
            
            Loaded += (s, e) => 
            {
                DescriptionTextBox.Focus();

            };
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
    }
}

