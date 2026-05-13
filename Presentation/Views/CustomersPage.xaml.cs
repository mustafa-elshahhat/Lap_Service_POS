using System.Windows.Controls;
using AlJohary.ServiceHub.Presentation.ViewModels;
using AlJohary.ServiceHub.Presentation.Helpers;

namespace AlJohary.ServiceHub.Presentation.Views
{

    public partial class CustomersPage : Page
    {
        public CustomersPage()
        {
            InitializeComponent();
            var vm = new CustomersViewModel();
            DataContext = vm;
            AlJohary.ServiceHub.Presentation.Helpers.FocusHelper.SetupSearchFocus(this, SearchTextBox);
        }
    }
}
