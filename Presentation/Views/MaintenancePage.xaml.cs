using System.Windows.Controls;
using CarPartsShopWPF.Presentation.ViewModels;

namespace CarPartsShopWPF.Presentation.Views
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
