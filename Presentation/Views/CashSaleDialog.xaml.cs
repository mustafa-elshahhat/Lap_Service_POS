using System;
using System.Windows;
using System.Windows.Input;

namespace CarPartsShopWPF.Presentation.Views
{

    public partial class CashSaleDialog : Window
    {
        public CashSaleDialog()
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
