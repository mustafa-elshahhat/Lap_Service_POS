using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AlJohary.ServiceHub.Presentation.ViewModels;
using AlJohary.ServiceHub.Presentation.Helpers;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Presentation.Views
{

    public partial class POSPage : Page
    {
        private POSViewModel _viewModel;

        public POSPage()
        {
            InitializeComponent();
            _viewModel = new POSViewModel();
            DataContext = _viewModel;
            AlJohary.ServiceHub.Presentation.Helpers.FocusHelper.SetupSearchFocus(this, SearchTextBox);
        }
    }
}
