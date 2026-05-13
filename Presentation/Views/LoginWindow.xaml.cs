using System.Windows;
using System.Windows.Input;
using CarPartsShopWPF.Presentation.ViewModels;

namespace CarPartsShopWPF.Presentation.Views
{

    public partial class LoginWindow : Window
    {
        private LoginViewModel _viewModel;

        public LoginWindow()
        {
            InitializeComponent();
            _viewModel = new LoginViewModel();
            this.DataContext = _viewModel;

            Loaded += (s, e) => UsernameTextBox.Focus();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            Login();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void ShowPasswordToggle_Click(object sender, RoutedEventArgs e)
        {
            if (ShowPasswordToggle.IsChecked == true)
            {

                VisiblePasswordBox.Text = PasswordBox.Password;
                VisiblePasswordBox.Visibility = Visibility.Visible;
                PasswordBox.Visibility = Visibility.Collapsed;
            }
            else
            {

                PasswordBox.Password = VisiblePasswordBox.Text;
                PasswordBox.Visibility = Visibility.Visible;
                VisiblePasswordBox.Visibility = Visibility.Collapsed;
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (sender == UsernameTextBox)
                {
                    if (PasswordBox.Visibility == Visibility.Visible)
                        PasswordBox.Focus();
                    else
                        VisiblePasswordBox.Focus();
                }
                else if (sender == PasswordBox || sender == VisiblePasswordBox)
                {
                    Login();
                }
            }
        }

        private void Login()
        {

            string password = (PasswordBox.Visibility == Visibility.Visible) ? PasswordBox.Password : VisiblePasswordBox.Text;

            _viewModel.Login(password);
        }
    }
}
