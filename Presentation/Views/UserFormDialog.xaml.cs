using System.Windows;
using System.Windows.Controls;
using CarPartsShopWPF.Presentation.ViewModels;

namespace CarPartsShopWPF.Presentation.Views
{
    public partial class UserFormDialog : Window
    {
        private UserFormViewModel _viewModel;

        public UserFormDialog(UserFormViewModel viewModel = null)
        {
            InitializeComponent();
            _viewModel = viewModel ?? new UserFormViewModel();
            this.DataContext = _viewModel;

            _viewModel.CloseAction = (result) =>
            {
                this.DialogResult = result;
                this.Close();
            };

            PasswordBox.PasswordChanged += PasswordBox_PasswordChanged;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
                _viewModel.Password = PasswordBox.Password;
        }

        private void TogglePassword_Click(object sender, RoutedEventArgs e)
        {
            if (PasswordBox.Visibility == Visibility.Visible)
            {
                PasswordBox.Visibility = Visibility.Collapsed;
                PasswordTextBox.Visibility = Visibility.Visible;
                TogglePasswordButton.Content = "🙈";
            }
            else
            {
                PasswordBox.Password = PasswordTextBox.Text;
                PasswordTextBox.Visibility = Visibility.Collapsed;
                PasswordBox.Visibility = Visibility.Visible;
                TogglePasswordButton.Content = "👁️";
            }
        }
    }
}
