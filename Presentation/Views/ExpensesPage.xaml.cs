using System.Windows.Controls;
using CarPartsShopWPF.Presentation.ViewModels;
using CarPartsShopWPF.Presentation.Helpers;

namespace CarPartsShopWPF.Presentation.Views
{

    public partial class ExpensesPage : Page
    {
        public ExpensesPage()
        {
            InitializeComponent();
            var vm = new ExpensesViewModel();
            DataContext = vm;
            CarPartsShopWPF.Presentation.Helpers.FocusHelper.SetupSearchFocus(this, SearchTextBox);
        }
    }
}
