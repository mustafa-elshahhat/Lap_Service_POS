using System.Windows.Controls;
using CarPartsShopWPF.Presentation.ViewModels;

namespace CarPartsShopWPF.Presentation.Views
{

    public partial class UsersPage : Page
    {
        public UsersPage()
        {
            InitializeComponent();
            DataContext = new UsersViewModel();
        }
    }
}
