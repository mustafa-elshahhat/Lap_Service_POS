using System.Windows;
using CarPartsShopWPF.Application.Interfaces;
using CarPartsShopWPF.Presentation.Interfaces;
using CarPartsShopWPF.Presentation.ViewModels;
using CarPartsShopWPF.Shared.Helpers;

namespace CarPartsShopWPF.Presentation.Views
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

