using System.Windows;
using AlJohary.ServiceHub.Presentation.ViewModels;

namespace AlJohary.ServiceHub.Presentation.Views
{
    public partial class RepairPartsDialog : Window
    {
        public RepairPartsDialog(RepairPartsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.RequestClose += () => Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
