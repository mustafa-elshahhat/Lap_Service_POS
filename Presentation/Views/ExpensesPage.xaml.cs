using System.Windows.Controls;
using AlJohary.ServiceHub.Presentation.ViewModels;
using AlJohary.ServiceHub.Presentation.Helpers;

namespace AlJohary.ServiceHub.Presentation.Views
{

    public partial class ExpensesPage : Page
    {
        public ExpensesPage()
        {
            InitializeComponent();
            var vm = new ExpensesViewModel();
            DataContext = vm;
            AlJohary.ServiceHub.Presentation.Helpers.FocusHelper.SetupSearchFocus(this, SearchTextBox);
        }
    }
}
