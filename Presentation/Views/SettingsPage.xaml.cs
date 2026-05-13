using System.Windows.Controls;
using AlJohary.ServiceHub.Presentation.ViewModels;

namespace AlJohary.ServiceHub.Presentation.Views
{

    public partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();
            DataContext = new SettingsViewModel();
        }
    }
}
