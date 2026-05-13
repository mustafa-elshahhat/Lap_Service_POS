using System.Windows.Controls;
using CarPartsShopWPF.Presentation.ViewModels;
using CarPartsShopWPF.Presentation.Helpers;

namespace CarPartsShopWPF.Presentation.Views
{
    public partial class InvoicesPage : Page
    {
        public InvoicesPage()
        {
            InitializeComponent();
            var vm = new InvoicesViewModel();
            DataContext = vm;
            CarPartsShopWPF.Presentation.Helpers.FocusHelper.SetupSearchFocus(this, SearchTextBox);
        }
    }
}
