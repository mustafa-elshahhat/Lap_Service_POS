using System;
using System.Collections.Generic;
using AlJohary.ServiceHub.Application.DTOs;
using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Application.Services;
using AlJohary.ServiceHub.Domain.Entities;
using AlJohary.ServiceHub.Infrastructure.Data;
using AlJohary.ServiceHub.Infrastructure.Persistence;
using AlJohary.ServiceHub.Infrastructure.Services;
using AlJohary.ServiceHub.Presentation;
using AlJohary.ServiceHub.Presentation.Interfaces;
using AlJohary.ServiceHub.Presentation.ViewModels;
using Xunit;

namespace AlJohary.ServiceHub.Tests
{
    [Collection("Database")]
    public class ReportsViewModelTests : IDisposable
    {
        public ReportsViewModelTests()
        {
            DatabaseManager.Instance.InitializeForTests();
            var reportRepo = new ReportRepository();
            var reportService = new ReportService(reportRepo);
            var productRepo = new ProductRepository();
            var productService = new ProductService(productRepo);
            var returnRepo = new ReturnRepository();
            var saleRepo = new SaleRepository();
            var txManager = new DbTransactionManager();
            var returnService = new ReturnService(saleRepo, returnRepo, productRepo, txManager);

            ServiceContainer.Register<IReportService>(reportService);
            ServiceContainer.Register<IProductService>(productService);
            ServiceContainer.Register<IReturnService>(returnService);
            ServiceContainer.Register<IDialogService>(new FakeDialogService());
            ServiceContainer.Register<IPrintService>(new FakePrintService());
        }

        public void Dispose() { }

        [Fact]
        public void FilterStartDate_AfterFilterEndDate_ValidationFails()
        {
            var vm = new ReportsViewModel(new FakeDialogService());
            vm.FilterStartDate = new DateTime(2025, 6, 15);
            vm.FilterEndDate = new DateTime(2025, 6, 10);
            Assert.False(vm.IsDateRangeValid);
            Assert.NotNull(vm.DateValidationMessage);
        }

        [Fact]
        public void FilterStartDate_BeforeFilterEndDate_ValidationPasses()
        {
            var vm = new ReportsViewModel(new FakeDialogService());
            vm.FilterStartDate = new DateTime(2025, 6, 1);
            vm.FilterEndDate = new DateTime(2025, 6, 15);
            Assert.True(vm.IsDateRangeValid);
            Assert.Null(vm.DateValidationMessage);
        }

        [Fact]
        public void FilterStartDate_EqualToFilterEndDate_ValidationPasses()
        {
            var vm = new ReportsViewModel(new FakeDialogService());
            vm.FilterStartDate = new DateTime(2025, 6, 10);
            vm.FilterEndDate = new DateTime(2025, 6, 10);
            Assert.True(vm.IsDateRangeValid);
            Assert.Null(vm.DateValidationMessage);
        }
    }

    public class FakeDialogService : IDialogService
    {
        public void ShowMessage(string title, string message) { }
        public void ShowInfo(string title, string message) { }
        public void ShowError(string title, string message) { }
        public void ShowWarning(string title, string message) { }
        public void ShowSuccess(string title, string message) { }
        public bool Confirm(string title, string message) => true;
        public bool? ShowCashSaleDialog(decimal total, out string customerName, out string customerPhone, out string paymentMethod)
        { customerName = null; customerPhone = null; paymentMethod = null; return true; }
        public bool? ShowInputDialog(string title, string message, string defaultValue, out string result)
        { result = null; return true; }
        public void ShowInvoiceViewDialog(string invoiceNumber) { }
        public bool? ShowUserFormDialog(UserFormViewModel viewModel) => true;
        public bool? ShowProductFormDialog(ProductFormViewModel viewModel) => true;
        public void ShowCustomerInvoicesDialog(int customerId, string customerName) { }
        public bool? ShowExpenseDialog(ExpenseFormViewModel viewModel) => true;
        public bool? ShowEmployeeFormDialog(EmployeeFormViewModel viewModel) => true;
        public bool? ShowEmployeeSalaryTransactionDialog(Employee employee, string transactionType, out decimal amount, out string paymentMethod, out DateTime transactionDate, out string notes)
        { amount = 0; paymentMethod = null; transactionDate = DateTime.Now; notes = null; return true; }
        public bool? ShowSupplierFormDialog(SupplierFormViewModel viewModel) => true;
        public bool? ShowSupplierPurchaseDialog(string supplierName, decimal currentDebt, out SupplierPurchaseDialogResult result)
        { result = null; return true; }
        public bool? ShowSupplierPaymentDialog(string supplierName, decimal currentDebt, out decimal paymentAmount, out string paymentMethod)
        { paymentAmount = 0; paymentMethod = null; return true; }
        public void ShowReturnDetailsDialog(int returnId) { }
        public void ShowMainWindow() { }
        public void ShowLoginWindow() { }
    }

    public class FakePrintService : IPrintService
    {
        public void PrintSaleReceipt(Sale sale, List<SaleItem> items) { }
        public void PrintReturnReceipt(Return @return, List<ReturnItem> items) { }
        public void PrintInventory(List<Product> products, Dictionary<string, object> totals) { }
        public void PrintSupplierStatement(string supplierName, IEnumerable<Dictionary<string, object>> transactions, Dictionary<int, List<SupplierPurchaseItem>> transactionItems) { }
        public void PrintReport(string title, IEnumerable<Dictionary<string, object>> data, string[] columns, string[] headers) { }
        public void PrintGroupedReport(string title, IEnumerable<GroupedReportItem> data, string[] itemColumns, string[] itemHeaders) { }
        public void PrintRepairIntake(RepairOrder order, List<RepairDevice> devices) { }
        public void PrintRepairInvoice(RepairOrder order, List<RepairDevice> devices, List<RepairPart> parts, List<RepairPayment> payments) { }
    }
}
