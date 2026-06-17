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
            _viewModel = ServiceContainer.GetService<POSViewModel>();
            DataContext = _viewModel;
            AlJohary.ServiceHub.Presentation.Helpers.FocusHelper.SetupSearchFocus(this, SearchTextBox);
        }

        // UI-029: responsive POS layout. Narrow the cart panel at smaller widths so the
        // product search/results area keeps usable space (purely visual; no sale logic).
        private void PosRootGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double width = e.NewSize.Width;
            double cartWidth = width < 1080 ? 280 : 320;
            if (CartColumn.Width.Value != cartWidth)
                CartColumn.Width = new GridLength(cartWidth);
        }
    }
}
