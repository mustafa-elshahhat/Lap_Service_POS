using System.Windows;
using CarPartsShopWPF.Presentation.ViewModels;

namespace CarPartsShopWPF.Presentation.Views
{
    public partial class RepairDeviceDialog : Window
    {
        public RepairDeviceDialog(RepairDeviceFormViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.RequestClose += () => Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
