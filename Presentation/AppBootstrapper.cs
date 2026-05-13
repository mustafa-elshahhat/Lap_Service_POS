using System;
using System.Windows;
using CarPartsShopWPF.Application.Interfaces;
using CarPartsShopWPF.Presentation.Interfaces;
using CarPartsShopWPF.Infrastructure.Data;
using CarPartsShopWPF.Presentation.Services;
using CarPartsShopWPF.Application.Services;
using CarPartsShopWPF.Infrastructure.Persistence;
using CarPartsShopWPF.Infrastructure.Services;
using CarPartsShopWPF.Domain.Interfaces;

namespace CarPartsShopWPF.Presentation
{
    public static class AppBootstrapper
    {
        public static void Initialize()
        {
            try
            {
                RegisterServices();
                InitializeDatabase();
                InitializeLanguage();
            }
            catch (Exception ex)
            {
                CarPartsShopWPF.Shared.Helpers.Logger.LogException(ex, "AppBootstrapper Initialize");
                MessageBox.Show($"خطأ أثناء تهيئة النظام: {ex.Message}", "خطأ في التشغيل", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Windows.Application.Current.Shutdown(1);
            }
        }

        public static void Cleanup()
        {
            try
            {
                DatabaseManager.Instance.Close();
            }
            catch (Exception ex)
            {
                CarPartsShopWPF.Shared.Helpers.Logger.LogException(ex, "AppBootstrapper Cleanup");
            }
        }

        private static void RegisterServices()
        {
            var txManager = new DbTransactionManager();
            var settingsService = new SettingsService();

            var userRepo = new UserRepository();
            var productRepo = new ProductRepository();
            var saleRepo = new SaleRepository();
            var customerRepo = new CustomerRepository();
            var paymentRepo = new PaymentRepository();
            var expenseRepo = new ExpenseRepository();
            var supplierRepo = new SupplierRepository();
            var reportRepo = new ReportRepository();

            var authService = new AuthService(userRepo);
            AuthService.Instance = authService;

            var productService = new ProductService(productRepo);
            var paymentService = new PaymentService(saleRepo, customerRepo, paymentRepo, txManager);
            var returnService = new ReturnService(saleRepo, productRepo, customerRepo, paymentService, txManager);
            var saleService = new SaleService(saleRepo, productRepo, customerRepo, paymentService, returnService, txManager, authService);
            var expenseService = new ExpenseService(expenseRepo, authService, txManager);
            var supplierService = new SupplierService(supplierRepo, authService);
            var customerService = new CustomerService(customerRepo);
            var reportService = new ReportService(reportRepo);

            var dialogService = new DialogService();

            ServiceContainer.Register<IAuthService>(authService);
            ServiceContainer.Register<IDialogService>(dialogService);
            ServiceContainer.Register<IProductService>(productService);
            ServiceContainer.Register<ISaleService>(saleService);
            ServiceContainer.Register<IReturnService>(returnService);
            ServiceContainer.Register<IPaymentService>(paymentService);
            ServiceContainer.Register<IExpenseService>(expenseService);
            ServiceContainer.Register<ISupplierService>(supplierService);
            ServiceContainer.Register<ICustomerService>(customerService);
            ServiceContainer.Register<IReportService>(reportService);
            ServiceContainer.Register<ISettingsService>(settingsService);
            ServiceContainer.Register<IPrintService>(new CarPartsShopWPF.Infrastructure.Printing.PrintService());
        }

        private static void InitializeDatabase()
        {
            try
            {
                DatabaseManager.Instance.Initialize();

                try
                {
                    CarPartsShopWPF.Infrastructure.SQLiteMigrations.Migration001_PaymentMethodEnum.Execute();
                    CarPartsShopWPF.Infrastructure.SQLiteMigrations.Migration002_BusinessDateLocalRepair.Execute();
                    CarPartsShopWPF.Infrastructure.SQLiteMigrations.Migration003_AddCashRefundToReturns.Execute();
                }
                catch (Exception migrationEx)
                {
                    CarPartsShopWPF.Shared.Helpers.Logger.LogException(migrationEx, "Migrations");
                }
            }
            catch (Exception ex)
            {
                CarPartsShopWPF.Shared.Helpers.Logger.LogException(ex, "AppBootstrapper InitializeDatabase");
                MessageBox.Show($"فشل تهيئة قاعدة البيانات:\n{ex.Message}", "خطأ في قاعدة البيانات", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Windows.Application.Current.Shutdown(1);
            }
        }

        private static void InitializeLanguage()
        {
            CarPartsShopWPF.Application.Services.LanguageService.Instance.SetLanguage("ar-EG");
        }
    }
}
