using System.Windows;
using System.Windows.Input;

namespace AlJohary.ServiceHub.Presentation.Views
{
    public partial class RegisterPurchaseDialog : Window
    {
        public RegisterPurchaseDialog()
        {
            InitializeComponent();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }
    }
}
