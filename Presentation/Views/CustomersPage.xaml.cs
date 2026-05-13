using System.Windows.Controls;
using CarPartsShopWPF.Presentation.ViewModels;
using CarPartsShopWPF.Presentation.Helpers;

namespace CarPartsShopWPF.Presentation.Views
{

    public partial class CustomersPage : Page
    {
        public CustomersPage()
        {
            InitializeComponent();
            var vm = new CustomersViewModel();
            DataContext = vm;
            CarPartsShopWPF.Presentation.Helpers.FocusHelper.SetupSearchFocus(this, SearchTextBox);
        }
    }
}
