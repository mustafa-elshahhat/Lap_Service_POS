using System.Windows.Controls;
using AlJohary.ServiceHub.Presentation.Interfaces;
using AlJohary.ServiceHub.Presentation.ViewModels;

namespace AlJohary.ServiceHub.Presentation.Views
{
    public partial class EmployeesPage : Page
    {
        public EmployeesPage()
        {
            InitializeComponent();
            DataContext = new EmployeesViewModel(ServiceContainer.GetService<IDialogService>());
        }
    }
}
