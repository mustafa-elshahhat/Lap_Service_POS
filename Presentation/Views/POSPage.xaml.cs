using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CarPartsShopWPF.Presentation.ViewModels;
using CarPartsShopWPF.Presentation.Helpers;
using CarPartsShopWPF.Shared.Helpers;

namespace CarPartsShopWPF.Presentation.Views
{

    public partial class POSPage : Page
    {
        private POSViewModel _viewModel;

        public POSPage()
        {
            InitializeComponent();
            _viewModel = new POSViewModel();
            DataContext = _viewModel;
            CarPartsShopWPF.Presentation.Helpers.FocusHelper.SetupSearchFocus(this, SearchTextBox);
        }
    }
}
