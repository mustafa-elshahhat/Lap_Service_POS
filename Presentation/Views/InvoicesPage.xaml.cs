using System.Windows.Controls;
using AlJohary.ServiceHub.Presentation.ViewModels;
using AlJohary.ServiceHub.Presentation.Helpers;

namespace AlJohary.ServiceHub.Presentation.Views
{
    public partial class InvoicesPage : Page
    {
        public InvoicesPage()
        {
            InitializeComponent();
            var vm = new InvoicesViewModel();
            DataContext = vm;
            AlJohary.ServiceHub.Presentation.Helpers.FocusHelper.SetupSearchFocus(this, SearchTextBox);
        }
    }
}
