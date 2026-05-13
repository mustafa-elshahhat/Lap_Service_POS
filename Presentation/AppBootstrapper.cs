using System;
using System.Windows;
using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Presentation.Interfaces;
using AlJohary.ServiceHub.Infrastructure.Data;
using AlJohary.ServiceHub.Presentation.Services;
using AlJohary.ServiceHub.Application.Services;
using AlJohary.ServiceHub.Infrastructure.Persistence;
using AlJohary.ServiceHub.Infrastructure.Services;
using AlJohary.ServiceHub.Domain.Interfaces;

namespace AlJohary.ServiceHub.Presentation
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
                AlJohary.ServiceHub.Shared.Helpers.Logger.LogException(ex, "AppBootstrapper Initialize");
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
                AlJohary.ServiceHub.Shared.Helpers.Logger.LogException(ex, "AppBootstrapper Cleanup");
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
            var repairRepo = new RepairRepository();

            var authService = new AuthService(userRepo);
            AuthService.Instance = authService;

            var productService  = new ProductService(productRepo);
            var customerService = new CustomerService(customerRepo);
            var paymentService  = new PaymentService(paymentRepo);
            var returnService   = new ReturnService(saleRepo, productRepo, txManager);
            var saleService     = new SaleService(saleRepo, productRepo, customerService, paymentService, returnService, txManager, authService);
            var expenseService  = new ExpenseService(expenseRepo, authService, txManager);
            var supplierService = new SupplierService(supplierRepo, authService);
            var reportService   = new ReportService(reportRepo);
            var maintenanceService = new MaintenanceService(repairRepo, productRepo, customerRepo, txManager);

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
            ServiceContainer.Register<IMaintenanceService>(maintenanceService);
            ServiceContainer.Register<ISettingsService>(settingsService);
            ServiceContainer.Register<IPrintService>(new AlJohary.ServiceHub.Infrastructure.Printing.PrintService());
        }

        private static void InitializeDatabase()
        {
            try
            {
                DatabaseManager.Instance.Initialize();

                try
                {
                    AlJohary.ServiceHub.Infrastructure.SQLiteMigrations.Migration002_BusinessDateLocalRepair.Execute();
                    AlJohary.ServiceHub.Infrastructure.SQLiteMigrations.Migration003_AddCashRefundToReturns.Execute();
                    AlJohary.ServiceHub.Infrastructure.SQLiteMigrations.Migration004_RepairOrdersFullSchema.Execute();
                    AlJohary.ServiceHub.Infrastructure.SQLiteMigrations.Migration005_DropDeadPaymentMethodSchema.Execute();
                    AlJohary.ServiceHub.Infrastructure.SQLiteMigrations.Migration006_RepairPartPurchaseCost.Execute();
                }
                catch (Exception migrationEx)
                {
                    AlJohary.ServiceHub.Shared.Helpers.Logger.LogException(migrationEx, "Migrations");
                }
            }
            catch (Exception ex)
            {
                AlJohary.ServiceHub.Shared.Helpers.Logger.LogException(ex, "AppBootstrapper InitializeDatabase");
                MessageBox.Show($"فشل تهيئة قاعدة البيانات:\n{ex.Message}", "خطأ في قاعدة البيانات", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Windows.Application.Current.Shutdown(1);
            }
        }

        private static void InitializeLanguage()
        {
            AlJohary.ServiceHub.Application.Services.LanguageService.Instance.SetLanguage("ar-EG");
        }
    }
}
