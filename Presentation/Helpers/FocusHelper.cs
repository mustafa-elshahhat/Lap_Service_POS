using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media;
using AlJohary.ServiceHub.Presentation.ViewModels;

namespace AlJohary.ServiceHub.Presentation.Helpers
{
    public static class FocusHelper
    {
        public static void FocusTextBox(TextBox textBox)
        {
            if (textBox == null) return;
            
            textBox.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => 
            {
                if (textBox.IsLoaded && textBox.IsVisible)
                {
                    textBox.Focus();
                    Keyboard.Focus(textBox);
                    textBox.SelectAll();
                }
            }));
        }

        public static void SetupSearchFocus(Page page, TextBox searchTextBox)
        {
            if (page == null || searchTextBox == null) return;

            page.Loaded += (s, e) => FocusTextBox(searchTextBox);
            page.IsVisibleChanged += (s, e) => { if ((bool)e.NewValue) FocusTextBox(searchTextBox); };

            page.PreviewMouseUp += (s, e) => 
            {

                DependencyObject dep = e.OriginalSource as DependencyObject;
                bool isInteractive = false;
                
                while (dep != null && !(dep is Page))
                {
                    if (dep is TextBox || dep is ComboBox || dep is Button || dep is DataGrid || dep is ListBox)
                    {
                        isInteractive = true;
                        break;
                    }
                    dep = VisualTreeHelper.GetParent(dep);
                }

                if (!isInteractive)
                {
                    FocusTextBox(searchTextBox);
                }
            };

            if (page.DataContext is BaseViewModel viewModel)
            {
                viewModel.RequestSearchFocus += (s, e) => FocusTextBox(searchTextBox);
            }

            page.DataContextChanged += (s, e) => 
            {
                if (e.NewValue is BaseViewModel vm)
                {
                    vm.RequestSearchFocus += (s, e2) => FocusTextBox(searchTextBox);
                }
            };
        }
    }
}
