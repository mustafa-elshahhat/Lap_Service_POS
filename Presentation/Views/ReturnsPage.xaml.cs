using System.Windows.Controls;
using AlJohary.ServiceHub.Presentation.ViewModels;
using AlJohary.ServiceHub.Presentation.Helpers;

namespace AlJohary.ServiceHub.Presentation.Views
{

    public partial class ReturnsPage : Page
    {
        public ReturnsPage()
        {
            InitializeComponent();
            var vm = new ReturnsViewModel();
            DataContext = vm;
            AlJohary.ServiceHub.Presentation.Helpers.FocusHelper.SetupSearchFocus(this, SearchTextBox);
        }
    }
}
