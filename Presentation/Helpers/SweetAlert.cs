using System;
using System.Windows;
using CarPartsShopWPF.Presentation.Views;

namespace CarPartsShopWPF.Presentation.Helpers
{
    public static class SweetAlert
    {
        public static void Show(string title, string message, SweetAlertWindow.AlertType type, int autoCloseSeconds = 3)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    var window = new SweetAlertWindow(title, message, type, false, "موافق", "إلغاء", autoCloseSeconds);

                    var activeWindow = GetActiveWindow();
                    if (activeWindow != null && activeWindow != window)
                    {
                        window.Owner = activeWindow;
                    }

                    window.ShowDialog();
                }
                catch
                {
                    MessageBoxImage icon = MessageBoxImage.None;
                    if (type == SweetAlertWindow.AlertType.Success) icon = MessageBoxImage.Information;
                    else if (type == SweetAlertWindow.AlertType.Error) icon = MessageBoxImage.Error;
                    else if (type == SweetAlertWindow.AlertType.Warning) icon = MessageBoxImage.Warning;
                    else if (type == SweetAlertWindow.AlertType.Info) icon = MessageBoxImage.Information;

                    MessageBox.Show(message, title, MessageBoxButton.OK, icon);
                }
            });
        }

        public static void Success(string title, string message)
        {
            Show(title, message, SweetAlertWindow.AlertType.Success);
        }

        public static void Error(string title, string message)
        {
            Show(title, message, SweetAlertWindow.AlertType.Error);
        }

        public static void Warning(string title, string message)
        {
            Show(title, message, SweetAlertWindow.AlertType.Warning);
        }

        public static void Info(string title, string message)
        {
            Show(title, message, SweetAlertWindow.AlertType.Info);
        }

        public static bool Confirm(string title, string message, string confirmText = "نعم", string cancelText = "لا")
        {
            bool result = false;
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var window = new SweetAlertWindow(title, message, SweetAlertWindow.AlertType.Question, true, confirmText, cancelText, 0);

                var activeWindow = GetActiveWindow();
                if (activeWindow != null && activeWindow != window)
                {
                    window.Owner = activeWindow;
                }

                var dialogResult = window.ShowDialog();
                result = dialogResult.HasValue && dialogResult.Value;
            });
            return result;
        }

        private static Window GetActiveWindow()
        {
            Window activeWindow = null;
            foreach (Window window in System.Windows.Application.Current.Windows)
            {
                if (window.IsActive && window.IsVisible)
                {
                    activeWindow = window;
                    break;
                }
            }

            if (activeWindow == null)
            {
                var main = System.Windows.Application.Current.MainWindow;
                if (main != null && main.IsVisible)
                {
                    activeWindow = main;
                }
            }

            if (activeWindow == null)
            {
                 foreach (Window window in System.Windows.Application.Current.Windows)
                 {
                     if (window.IsVisible && window.IsLoaded)
                     {
                         activeWindow = window;
                         break;
                     }
                 }
            }

            return activeWindow;
        }
    }
}
