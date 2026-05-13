using System.Windows.Controls;
using CarPartsShopWPF.Presentation.ViewModels;
using CarPartsShopWPF.Presentation.Helpers;

namespace CarPartsShopWPF.Presentation.Views
{

    public partial class InventoryPage : Page
    {
        public InventoryPage()
        {
            InitializeComponent();
            var vm = new InventoryViewModel();
            DataContext = vm;
            CarPartsShopWPF.Presentation.Helpers.FocusHelper.SetupSearchFocus(this, SearchTextBox);
        }
    }
}
