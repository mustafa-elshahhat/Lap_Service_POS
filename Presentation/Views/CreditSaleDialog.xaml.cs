using System.Windows;
using System.Windows.Input;

namespace CarPartsShopWPF.Presentation.Views
{

    public partial class CreditSaleDialog : Window
    {
        public CreditSaleDialog()
        {
            InitializeComponent();
            Loaded += (s, e) => CustomerNameTextBox.Focus();
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
