using System;
using System.Windows;
using AlJohary.ServiceHub.Presentation.Services;
using AlJohary.ServiceHub.Presentation.Views;

namespace AlJohary.ServiceHub.Presentation.Helpers
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

                    DialogService.ConfigureOwnedWindow(window);

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

                DialogService.ConfigureOwnedWindow(window);

                var dialogResult = window.ShowDialog();
                result = dialogResult.HasValue && dialogResult.Value;
            });
            return result;
        }
    }
}
