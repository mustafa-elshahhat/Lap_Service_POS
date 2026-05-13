using System.Windows;
using AlJohary.ServiceHub.Presentation.ViewModels;

namespace AlJohary.ServiceHub.Presentation.Views
{
    public partial class RepairOrderDialog : Window
    {
        public RepairOrderDialog(RepairOrderFormViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.RequestClose += () => Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
