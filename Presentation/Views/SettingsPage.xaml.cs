using System.Windows.Controls;
using CarPartsShopWPF.Presentation.ViewModels;

namespace CarPartsShopWPF.Presentation.Views
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
