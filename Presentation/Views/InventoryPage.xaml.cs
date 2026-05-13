using System.Windows.Controls;
using AlJohary.ServiceHub.Presentation.ViewModels;
using AlJohary.ServiceHub.Presentation.Helpers;

namespace AlJohary.ServiceHub.Presentation.Views
{

    public partial class InventoryPage : Page
    {
        public InventoryPage()
        {
            InitializeComponent();
            var vm = new InventoryViewModel();
            DataContext = vm;
            AlJohary.ServiceHub.Presentation.Helpers.FocusHelper.SetupSearchFocus(this, SearchTextBox);
        }
    }
}
