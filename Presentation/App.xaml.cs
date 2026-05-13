using System;
using System.Windows;
using CarPartsShopWPF.Presentation.Services;
using CarPartsShopWPF.Shared.Helpers;

namespace CarPartsShopWPF.Presentation
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);

                this.DispatcherUnhandledException += App_DispatcherUnhandledException;
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

                AppBootstrapper.Initialize();

                var loginWindow = new Views.LoginWindow();
                loginWindow.Show();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "App Startup");
                MessageBox.Show($"حدث خطأ أثناء تشغيل التطبيق: {ex.Message}", "خطأ فادح", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Logger.LogException(e.Exception, "UI Dispatcher");
            MessageBox.Show($"حدث خطأ في النظام: {e.Exception.Message}", "خطأ في التطبيق", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                Logger.LogException(ex, "AppDomain Unhandled");
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            AppBootstrapper.Cleanup();
            base.OnExit(e);
        }
    }
}
