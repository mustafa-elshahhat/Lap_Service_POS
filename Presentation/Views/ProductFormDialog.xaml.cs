using System;
using System.Windows;
using AlJohary.ServiceHub.Presentation.ViewModels;

namespace AlJohary.ServiceHub.Presentation.Views
{

    public partial class ProductFormDialog : Window
    {
        public ProductFormDialog(ProductFormViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;

            if (viewModel != null)
            {
                viewModel.CloseAction = (result) =>
                {
                    this.DialogResult = result;
                    this.Close();
                };
            }

            Loaded += (s, e) => NameTextBox.Focus();
        }
        
    }
}

