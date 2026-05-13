using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CarPartsShopWPF.Infrastructure.Data;
using CarPartsShopWPF.Application.Services;
using CarPartsShopWPF.Shared.Helpers;

using CarPartsShopWPF.Presentation.ViewModels;
using CarPartsShopWPF.Presentation.Helpers;

namespace CarPartsShopWPF.Presentation.Views
{

    public partial class SuppliersPage : Page
    {
        public SuppliersPage()
        {
            InitializeComponent();
            var vm = new SuppliersViewModel();
            DataContext = vm;
            FocusHelper.SetupSearchFocus(this, SearchTextBox);
        }

    }
}
