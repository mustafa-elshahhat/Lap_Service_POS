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

        // ---- B-1: user-selected dates must survive and drive the reload (not reset to defaults) ----

        private static ReportsViewModel BuildVmWith(RecordingReportService fake)
        {
            ServiceContainer.Register<IReportService>(fake);
            return new ReportsViewModel(new FakeDialogService());
        }

        [Fact]
        public void Daily_ChangeFilterStartDate_NotResetToToday_AndUsedInLoad()
        {
            var fake = new RecordingReportService();
            var vm = BuildVmWith(fake);

            vm.LoadReportCommand.Execute("Daily");
            Assert.Equal(DateTime.Today, vm.FilterStartDate);

            fake.Reset();
            var past = DateTime.Today.AddDays(-7);
            vm.FilterStartDate = past;

            Assert.Equal(past, vm.FilterStartDate);                          // not reset to today
            Assert.Equal(past.ToString("yyyy-MM-dd"), fake.LastDailyDate);   // reload used the picked date
        }

        [Fact]
        public void Daily_ChangeFilterEndDate_Preserved_AndReloads()
        {
            var fake = new RecordingReportService();
            var vm = BuildVmWith(fake);

            vm.LoadReportCommand.Execute("Daily");
            fake.Reset();

            var newEnd = DateTime.Today.AddDays(3);
            vm.FilterEndDate = newEnd;

            Assert.Equal(newEnd, vm.FilterEndDate);                          // preserved, not reset to today
            Assert.True(fake.DailyCallCount >= 1);                           // reloaded using current filters
            Assert.Equal(DateTime.Today.ToString("yyyy-MM-dd"), fake.LastDailyDate);
        }

        [Fact]
        public void Monthly_ChangeStartAndEnd_BothPreserved_AndPeriodUsed()
        {
            var fake = new RecordingReportService();
            var vm = BuildVmWith(fake);

            vm.LoadReportCommand.Execute("Monthly");

            var s = new DateTime(2025, 1, 1);
            var e = new DateTime(2025, 3, 31);
            vm.FilterStartDate = s;
            vm.FilterEndDate = e;

            Assert.Equal(s, vm.FilterStartDate);
            Assert.Equal(e, vm.FilterEndDate);
            // Monthly now uses GetPeriodSummary over the selected start/end (end no longer ignored).
            Assert.Equal("2025-01-01", fake.LastPeriodStart);
            Assert.Equal("2025-03-31", fake.LastPeriodEnd);
        }

        [Fact]
        public void Returns_ChangeRange_NotResetToLast30Days()
        {
            var fake = new RecordingReportService();
            var vm = BuildVmWith(fake);

            vm.LoadReportCommand.Execute("Returns");
            Assert.Equal(DateTime.Today.AddDays(-30), vm.FilterStartDate);

            var s = DateTime.Today.AddDays(-90);
            var e = DateTime.Today.AddDays(-60);
            vm.FilterStartDate = s;
            vm.FilterEndDate = e;

            Assert.Equal(s, vm.FilterStartDate);   // not reset back to today-30
            Assert.Equal(e, vm.FilterEndDate);
        }

        [Fact]
        public void Daily_InvalidRange_DoesNotReload()
        {
            var fake = new RecordingReportService();
            var vm = BuildVmWith(fake);

            vm.LoadReportCommand.Execute("Daily");
            fake.Reset();

            vm.FilterStartDate = DateTime.Today.AddDays(5); // start > end(today) => invalid

            Assert.False(vm.IsDateRangeValid);
            Assert.NotNull(vm.DateValidationMessage);
            Assert.Equal(0, fake.DailyCallCount);           // invalid range must not reload
        }

        [Fact]
        public void DefaultDateRanges_OnReportTypeSelect_AreUnchanged()
        {
            var fake = new RecordingReportService();
            var vm = BuildVmWith(fake);

            vm.LoadReportCommand.Execute("Daily");
            Assert.Equal(DateTime.Today, vm.FilterStartDate);
            Assert.Equal(DateTime.Today, vm.FilterEndDate);

            vm.LoadReportCommand.Execute("Monthly");
            var first = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var last = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month));
            Assert.Equal(first, vm.FilterStartDate);
            Assert.Equal(last, vm.FilterEndDate);

            vm.LoadReportCommand.Execute("Returns");
            Assert.Equal(DateTime.Today.AddDays(-30), vm.FilterStartDate);
            Assert.Equal(DateTime.Today, vm.FilterEndDate);
        }
    }

    // Records the date arguments the ViewModel passes so tests can assert the selected
    // dates actually drive the report load. Returns minimal valid summaries.
    public class RecordingReportService : IReportService
    {
        public string LastDailyDate;
        public string LastPeriodStart;
        public string LastPeriodEnd;
        public int DailyCallCount;
        public int PeriodCallCount;

        public void Reset()
        {
            LastDailyDate = null;
            LastPeriodStart = null;
            LastPeriodEnd = null;
            DailyCallCount = 0;
            PeriodCallCount = 0;
        }

        public Dictionary<string, object> GetDailySummary(string targetDate = null)
        {
            LastDailyDate = targetDate;
            DailyCallCount++;
            return EmptySummary();
        }

        public Dictionary<string, object> GetMonthlySummary(int year, int month)
        {
            string start = $"{year}-{month:D2}-01";
            string end = new DateTime(year, month, DateTime.DaysInMonth(year, month)).ToString("yyyy-MM-dd");
            return GetPeriodSummary(start, end);
        }

        public Dictionary<string, object> GetPeriodSummary(string startDate, string endDate)
        {
            LastPeriodStart = startDate;
            LastPeriodEnd = endDate;
            PeriodCallCount++;
            return EmptySummary();
        }

        public List<Dictionary<string, object>> GetFinancialOperations(string startDate, string endDate)
        {
            LastPeriodStart = startDate;
            LastPeriodEnd = endDate;
            return new List<Dictionary<string, object>>();
        }

        private static Dictionary<string, object> EmptySummary() => new Dictionary<string, object>
        {
            { "payment_inflows", new Dictionary<string, decimal>() },
            { "payment_outflows", new Dictionary<string, decimal>() }
        };
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
