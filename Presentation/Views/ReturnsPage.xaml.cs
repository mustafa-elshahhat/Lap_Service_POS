using System.Windows.Controls;
using CarPartsShopWPF.Presentation.ViewModels;
using CarPartsShopWPF.Presentation.Helpers;

namespace CarPartsShopWPF.Presentation.Views
{

    public partial class ReturnsPage : Page
    {
        public ReturnsPage()
        {
            InitializeComponent();
            var vm = new ReturnsViewModel();
            DataContext = vm;
            CarPartsShopWPF.Presentation.Helpers.FocusHelper.SetupSearchFocus(this, SearchTextBox);
        }
    }
}
