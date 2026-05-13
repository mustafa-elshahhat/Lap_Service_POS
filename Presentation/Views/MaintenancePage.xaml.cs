using System.Windows.Controls;
using AlJohary.ServiceHub.Presentation.ViewModels;

namespace AlJohary.ServiceHub.Presentation.Views
{
    public partial class MaintenancePage : Page
    {
        public MaintenancePage()
        {
            InitializeComponent();
            DataContext = new MaintenanceViewModel();
        }
    }
}
