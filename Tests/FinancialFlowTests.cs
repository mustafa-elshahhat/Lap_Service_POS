using System;
using System.Collections.Generic;
using System.Linq;
using AlJohary.ServiceHub.Application.DTOs;
using AlJohary.ServiceHub.Application.Services;
using AlJohary.ServiceHub.Domain.Entities;
using AlJohary.ServiceHub.Infrastructure.Data;
using AlJohary.ServiceHub.Infrastructure.Persistence;
using AlJohary.ServiceHub.Infrastructure.Services;
using AlJohary.ServiceHub.Infrastructure.SQLiteMigrations;
using AlJohary.ServiceHub.Shared.Helpers;
using Xunit;

namespace AlJohary.ServiceHub.Tests
{
    [Collection("Database")]
    public class FinancialFlowTests : IDisposable
    {
        private readonly DbTransactionManager _tx = new DbTransactionManager();
        private readonly ActivityLog _activityLog = new ActivityLog();
        private readonly string _today = DateTime.Today.ToString("yyyy-MM-dd");

        public FinancialFlowTests()
        {
            DatabaseManager.Instance.InitializeForTests();
        }

        public void Dispose() { }

        private int SeedProduct(decimal cost, decimal sell, int qty, string code = "P1")
        {
            return (int)DatabaseManager.Instance.ExecuteAndGetId(@"
                INSERT INTO products (code, name, purchase_price, selling_price, quantity)
                VALUES (@code, 'منتج', @cost, @sell, @qty)",
                new Dictionary<string, object>
                {
                    { "@code", code }, { "@cost", cost }, { "@sell", sell }, { "@qty", qty }
                });
        }

        private SaleService BuildSaleService(FakeAuthService auth)
        {
            var saleRepo = new SaleRepository();
            var productRepo = new ProductRepository();
            var customerService = new CustomerService(new CustomerRepository());
            var paymentService = new PaymentService(new PaymentRepository());
            var returnRepo = new ReturnRepository();
            var returnService = new ReturnService(saleRepo, returnRepo, productRepo, _tx);
            return new SaleService(saleRepo, productRepo, customerService, paymentService, returnService, _tx, auth);
        }

        // ---- Sales: cash 100, cost 60 ----
        [Fact]
        public void CashSale_FullyPaid_RecordsProfitAndZeroRemaining()
        {
            int productId = SeedProduct(cost: 60, sell: 100, qty: 5);
            var auth = new FakeAuthService { Admin = false, BypassPriceLimits = false, MaxDiscount = 10, MaxMarkup = 20 };
            var svc = BuildSaleService(auth);

            var items = new List<SaleItem> { new SaleItem { ProductId = productId, Quantity = 1, UnitFinalPrice = 100 } };
            var result = svc.CreateCashSale(items, paymentMethod: PaymentMethods.Cash);

            Assert.True(result.Success, result.Message);
            var sale = svc.GetSaleById((int)result.SaleId);
            Assert.Equal(100m, sale.TotalAmount);
            Assert.Equal(0m, sale.RemainingAmount);
            Assert.Equal(40m, sale.Profit);
            Assert.Equal(PaymentMethods.Cash, sale.PaymentMethod);

            var saleItems = svc.GetSaleItems((int)result.SaleId);
            Assert.Single(saleItems);
            Assert.Equal(40m, saleItems[0].Profit);

            var product = new ProductRepository().GetById(productId);
            Assert.Equal(4, product.Quantity); // stock reduced by 1
        }

        [Fact]
        public void CreditSale_Rejected()
        {
            int productId = SeedProduct(60, 100, 5);
            var svc = BuildSaleService(new FakeAuthService());
            var items = new List<SaleItem> { new SaleItem { ProductId = productId, Quantity = 1, UnitFinalPrice = 100 } };

            var result = svc.CreateSale("credit", 1, items, paidAmount: 0);
            Assert.False(result.Success);
        }

        [Fact]
        public void DiscountWithinLimit_Accepted_PersistsDiscountAmount()
        {
            int productId = SeedProduct(60, 100, 5);
            var auth = new FakeAuthService { Admin = false, BypassPriceLimits = false, MaxDiscount = 10, MaxMarkup = 20 };
            var svc = BuildSaleService(auth);
            var items = new List<SaleItem> { new SaleItem { ProductId = productId, Quantity = 1, UnitFinalPrice = 95 } };

            var result = svc.CreateCashSale(items, paymentMethod: PaymentMethods.Cash);
            Assert.True(result.Success, result.Message);

            var saleItems = svc.GetSaleItems((int)result.SaleId);
            Assert.Equal(5m, saleItems[0].DiscountAmount);
            Assert.Equal(35m, saleItems[0].Profit);
        }

        [Fact]
        public void DiscountOverLimit_Rejected()
        {
            int productId = SeedProduct(60, 100, 5);
            var auth = new FakeAuthService { Admin = false, BypassPriceLimits = false, MaxDiscount = 10, MaxMarkup = 20 };
            var svc = BuildSaleService(auth);
            var items = new List<SaleItem> { new SaleItem { ProductId = productId, Quantity = 1, UnitFinalPrice = 80 } };

            var result = svc.CreateCashSale(items, paymentMethod: PaymentMethods.Cash);
            Assert.False(result.Success);
        }

        [Fact]
        public void AdminBypassesCeiling_ButNeverBelowCost()
        {
            int productId = SeedProduct(60, 100, 5);
            var admin = new FakeAuthService { Admin = true, BypassPriceLimits = true };
            var svc = BuildSaleService(admin);

            // Above the employee markup ceiling -> accepted for admin, profit = 90.
            var high = svc.CreateCashSale(
                new List<SaleItem> { new SaleItem { ProductId = productId, Quantity = 1, UnitFinalPrice = 150 } },
                paymentMethod: PaymentMethods.Cash);
            Assert.True(high.Success, high.Message);
            Assert.Equal(90m, svc.GetSaleById((int)high.SaleId).Profit);

            // Below cost -> rejected even for admin.
            var low = svc.CreateCashSale(
                new List<SaleItem> { new SaleItem { ProductId = productId, Quantity = 1, UnitFinalPrice = 50 } },
                paymentMethod: PaymentMethods.Cash);
            Assert.False(low.Success);
        }

        // ---- Payment method normalization (Migration009) ----
        [Fact]
        public void Migration009_FoldsLegacyKashToCanonicalCash()
        {
            var db = DatabaseManager.Instance;
            int productId = SeedProduct(60, 100, 5);
            long saleId = db.ExecuteAndGetId(@"
                INSERT INTO sales (invoice_number, sale_type, user_id, total_amount, paid_amount, remaining_amount, payment_method, sale_date)
                VALUES ('INV-K', 'cash', 1, 100, 100, 0, 'كاش', @d)",
                new Dictionary<string, object> { { "@d", DateTime.Today.AddHours(9).ToString("yyyy-MM-dd HH:mm:ss") } });
            db.Execute(@"INSERT INTO sale_payments (sale_id, payment_method, amount, payment_date)
                         VALUES (@s, 'كاش', 100, @d)",
                new Dictionary<string, object> { { "@s", saleId }, { "@d", DateTime.Today.AddHours(9).ToString("yyyy-MM-dd HH:mm:ss") } });

            Migration009_PaymentMethodNormalization.Execute();

            var sale = db.FetchOne("SELECT payment_method FROM sales WHERE id = @s", new Dictionary<string, object> { { "@s", saleId } });
            Assert.Equal("نقدي", SafeConvert.ToString(sale["payment_method"]));
            var pay = db.FetchOne("SELECT payment_method FROM sale_payments WHERE sale_id = @s", new Dictionary<string, object> { { "@s", saleId } });
            Assert.Equal("نقدي", SafeConvert.ToString(pay["payment_method"]));
        }

        // ---- Returns: cash refund shows in payment_outflows by method ----
        [Fact]
        public void CashRefund_AppearsInPaymentOutflowsByMethod()
        {
            var db = DatabaseManager.Instance;
            string ts = DateTime.Today.AddHours(11).ToString("yyyy-MM-dd HH:mm:ss");
            long saleId = db.ExecuteAndGetId(@"
                INSERT INTO sales (invoice_number, sale_type, user_id, total_amount, paid_amount, remaining_amount, payment_method, sale_date)
                VALUES ('INV-R', 'cash', 1, 100, 100, 0, 'نقدي', @d)",
                new Dictionary<string, object> { { "@d", ts } });
            db.Execute(@"INSERT INTO returns (return_number, sale_id, user_id, total_amount, cash_refund, debt_deduction, payment_method, return_date)
                         VALUES ('RET-1', @s, 1, 100, 100, 0, 'نقدي', @d)",
                new Dictionary<string, object> { { "@s", saleId }, { "@d", ts } });

            var summary = new ReportRepository().GetPeriodSummary(_today, _today);
            Assert.Equal(100m, SafeConvert.ToDecimal(summary["cash_refunds"]));
            var outflows = (Dictionary<string, decimal>)summary["payment_outflows"];
            Assert.True(outflows.ContainsKey(PaymentMethods.Cash));
            Assert.Equal(100m, outflows[PaymentMethods.Cash]);
            // gross_sales unaffected by the return
            Assert.Equal(100m, SafeConvert.ToDecimal(summary["gross_sales"]));
        }

        // ---- Expenses: soft delete excludes from totals + audit row retained ----
        [Fact]
        public void Expense_SoftDelete_ExcludedFromReportsAndLogged()
        {
            var auth = new FakeAuthService { Admin = true };
            var repo = new ExpenseRepository();
            var svc = new ExpenseService(repo, auth, _tx, _activityLog);

            svc.CreateExpense("كهرباء", 40, "فواتير", PaymentMethods.Cash, DateTime.Today.AddHours(10));
            var report = new ReportRepository();
            Assert.Equal(40m, SafeConvert.ToDecimal(report.GetPeriodSummary(_today, _today)["total_expenses"]));

            int expenseId = SafeConvert.ToInt(DatabaseManager.Instance.FetchOne("SELECT id FROM expenses LIMIT 1")["id"]);
            svc.DeleteExpense(expenseId);

            Assert.Equal(0m, SafeConvert.ToDecimal(report.GetPeriodSummary(_today, _today)["total_expenses"]));
            var row = DatabaseManager.Instance.FetchOne("SELECT is_deleted, deleted_by FROM expenses WHERE id = @id",
                new Dictionary<string, object> { { "@id", expenseId } });
            Assert.Equal(1, SafeConvert.ToInt(row["is_deleted"]));

            long logCount = Convert.ToInt64(DatabaseManager.Instance.FetchScalar(
                "SELECT COUNT(*) FROM activity_log WHERE table_name = 'expenses'"));
            Assert.True(logCount >= 2); // create + delete
        }

        [Fact]
        public void Expense_ProtectedCategory_Rejected()
        {
            var svc = new ExpenseService(new ExpenseRepository(), new FakeAuthService { Admin = true }, _tx, _activityLog);
            Assert.Throws<InvalidOperationException>(() =>
                svc.CreateExpense("راتب", 500, "مرتبات", PaymentMethods.Cash, DateTime.Today));
        }

        // ---- Supplier: purchase 300 paid 100, payment 50, debt reconciles ----
        [Fact]
        public void Supplier_DebtReconstruction_AndPaidCountedOnce()
        {
            var repo = new SupplierRepository();
            var auth = new FakeAuthService { Admin = true };
            var svc = new SupplierService(repo, auth, _tx, _activityLog);

            int supplierId = (int)DatabaseManager.Instance.ExecuteAndGetId(
                "INSERT INTO suppliers (name, total_debt) VALUES ('مورد', 0)");

            var lines = new List<SupplierPurchaseLineInput>
            {
                new SupplierPurchaseLineInput { ProductName = "صنف", Quantity = 3, UnitPurchasePrice = 100 }
            };
            var purchase = svc.AddSupplierPurchaseWithItems(supplierId, 300, 100, PaymentMethods.Cash, lines);
            Assert.True(purchase.Success, purchase.Message);

            svc.AddSupplierPayment(supplierId, 50, PaymentMethods.Cash);

            var sup = DatabaseManager.Instance.FetchOne("SELECT total_debt FROM suppliers WHERE id = @id",
                new Dictionary<string, object> { { "@id", supplierId } });
            decimal debt = SafeConvert.ToDecimal(sup["total_debt"]);
            Assert.Equal(150m, debt); // 300 - (100 + 50)

            // Reconstruction: Σ purchases − Σ payments == total_debt
            decimal purchases = Convert.ToDecimal(DatabaseManager.Instance.FetchScalar(
                "SELECT COALESCE(SUM(amount),0) FROM supplier_transactions WHERE transaction_type='purchase' AND supplier_id=@id",
                new Dictionary<string, object> { { "@id", supplierId } }));
            decimal payments = Convert.ToDecimal(DatabaseManager.Instance.FetchScalar(
                "SELECT COALESCE(SUM(amount),0) FROM supplier_transactions WHERE transaction_type='payment' AND supplier_id=@id",
                new Dictionary<string, object> { { "@id", supplierId } }));
            Assert.Equal(150m, purchases - payments);
        }

        [Fact]
        public void Supplier_OverPayment_Rejected_NoOrphanRow()
        {
            var repo = new SupplierRepository();
            var svc = new SupplierService(repo, new FakeAuthService { Admin = true }, _tx, _activityLog);
            int supplierId = (int)DatabaseManager.Instance.ExecuteAndGetId(
                "INSERT INTO suppliers (name, total_debt) VALUES ('مورد2', 100)");

            Assert.ThrowsAny<Exception>(() => svc.AddSupplierPayment(supplierId, 500, PaymentMethods.Cash));

            long rows = Convert.ToInt64(DatabaseManager.Instance.FetchScalar(
                "SELECT COUNT(*) FROM supplier_transactions WHERE supplier_id=@id",
                new Dictionary<string, object> { { "@id", supplierId } }));
            Assert.Equal(0, rows);
            decimal debt = SafeConvert.ToDecimal(DatabaseManager.Instance.FetchOne(
                "SELECT total_debt FROM suppliers WHERE id=@id", new Dictionary<string, object> { { "@id", supplierId } })["total_debt"]);
            Assert.Equal(100m, debt);
        }

        // ---- Salaries: reversal nets cost to 0, original retained ----
        [Fact]
        public void SalaryReversal_NetsToZero_OriginalRetained()
        {
            var auth = new FakeAuthService { Admin = true, UserId = 1 };
            var empRepo = new EmployeeRepository();
            var svc = new EmployeeService(empRepo, auth, _tx, _activityLog);

            int empId = (int)DatabaseManager.Instance.ExecuteAndGetId(
                "INSERT INTO employees (full_name, base_salary, is_active) VALUES ('موظف', 3000, 1)");

            long txId = svc.RegisterSalaryPayment(empId, 200, PaymentMethods.Cash, DateTime.Today.AddHours(10), null);
            var report = new ReportRepository();
            Assert.Equal(200m, SafeConvert.ToDecimal(report.GetPeriodSummary(_today, _today)["total_salary_payments"]));

            svc.ReverseSalaryTransaction(txId, "خطأ إدخال");

            Assert.Equal(0m, SafeConvert.ToDecimal(report.GetPeriodSummary(_today, _today)["total_salary_payments"]));
            long rowCount = Convert.ToInt64(DatabaseManager.Instance.FetchScalar(
                "SELECT COUNT(*) FROM employee_salary_transactions WHERE employee_id=@id",
                new Dictionary<string, object> { { "@id", empId } }));
            Assert.Equal(2, rowCount); // original retained + compensating
        }

        // ---- Maintenance: payment on cancelled order rejected ----
        [Fact]
        public void Maintenance_PaymentOnCancelledOrder_Rejected()
        {
            var repairRepo = new RepairRepository();
            var svc = new MaintenanceService(repairRepo, new ProductRepository(), new CustomerService(new CustomerRepository()), _tx, _activityLog);

            long orderId = DatabaseManager.Instance.ExecuteAndGetId(@"
                INSERT INTO repair_orders (order_number, user_id, order_status, total_amount, paid_amount, remaining_amount, intake_date, created_at, updated_at)
                VALUES ('MNT-C', 1, 'cancelled', 200, 0, 200, datetime('now'), datetime('now'), datetime('now'))");

            Assert.Throws<InvalidOperationException>(() =>
                svc.RegisterPayment(orderId, 50, PaymentMethods.Cash, 1, null));
        }

        // ---- Per-method breakdown reconciliation ----
        // ---- Partial and repeated returns ----
        [Fact]
        public void PartialReturns_ThenMoreReturns_ThenOverReturn_GuardsCorrectly()
        {
            int productId = SeedProduct(cost: 60, sell: 100, qty: 5, code: "P-RET");
            var auth = new FakeAuthService { Admin = false, BypassPriceLimits = false, MaxDiscount = 10, MaxMarkup = 20 };
            var svc = BuildSaleService(auth);

            var items = new List<SaleItem>
            {
                new SaleItem { ProductId = productId, Quantity = 3, UnitFinalPrice = 100 }
            };
            var result = svc.CreateCashSale(items, paymentMethod: PaymentMethods.Cash);
            Assert.True(result.Success, result.Message);
            int saleId = (int)result.SaleId;

            var saleItems = svc.GetSaleItems(saleId);
            Assert.Single(saleItems);
            int saleItemId = saleItems[0].Id;

            // First return: 1 of 3 units
            var return1 = svc.CreateReturn(saleId, new List<ReturnItem> { new ReturnItem { SaleItemId = saleItemId, Quantity = 1 } }, userId: 1);
            Assert.NotNull(return1);

            var sale1 = svc.GetSaleById(saleId);
            Assert.Equal(200m, sale1.PaidAmount);
            Assert.Equal(0m, sale1.RemainingAmount);
            var productRepo = new ProductRepository();
            Assert.Equal(3, productRepo.GetById(productId).Quantity); // 5 - 3 + 1

            // Second return: 1 more unit
            var return2 = svc.CreateReturn(saleId, new List<ReturnItem> { new ReturnItem { SaleItemId = saleItemId, Quantity = 1 } }, userId: 1);
            Assert.NotNull(return2);

            var sale2 = svc.GetSaleById(saleId);
            Assert.Equal(100m, sale2.PaidAmount);
            Assert.Equal(4, productRepo.GetById(productId).Quantity); // 5 - 3 + 2

            // Over-return attempt: 2 units, but only 1 available for return (3 sold - 2 already returned = 1)
            Assert.Throws<Exception>(() =>
                svc.CreateReturn(saleId, new List<ReturnItem> { new ReturnItem { SaleItemId = saleItemId, Quantity = 2 } }, userId: 1));

            // Stock unchanged after failed over-return
            Assert.Equal(4, productRepo.GetById(productId).Quantity);
        }

        // ---- Multi-item distribution with invoice-level discount ----
        [Fact]
        public void MultiItemSale_WithInvoiceDiscount_DistributionSumsExactly()
        {
            int productA = SeedProduct(cost: 60, sell: 100, qty: 10, code: "P-A");
            int productB = SeedProduct(cost: 35, sell: 90, qty: 10, code: "P-B");
            int productC = SeedProduct(cost: 110, sell: 170, qty: 10, code: "P-C");

            var auth = new FakeAuthService { Admin = false, BypassPriceLimits = false, MaxDiscount = 10, MaxMarkup = 20 };
            var svc = BuildSaleService(auth);

            var items = new List<SaleItem>
            {
                new SaleItem { ProductId = productA, Quantity = 2, UnitFinalPrice = 100 },
                new SaleItem { ProductId = productB, Quantity = 1, UnitFinalPrice = 90 },
                new SaleItem { ProductId = productC, Quantity = 1, UnitFinalPrice = 170 }
            };

            decimal invoiceDiscount = 25m;
            var result = svc.CreateCashSale(items, discountAmount: invoiceDiscount, paymentMethod: PaymentMethods.Cash);

            Assert.True(result.Success, result.Message);
            var sale = svc.GetSaleById((int)result.SaleId);
            Assert.Equal(435m, sale.TotalAmount);

            var saleItems = svc.GetSaleItems((int)result.SaleId);
            Assert.Equal(3, saleItems.Count);

            decimal sumTotalPrice = 0, sumProfit = 0;
            foreach (var si in saleItems) { sumTotalPrice += si.TotalPrice; sumProfit += si.Profit; }

            Assert.Equal(sale.TotalAmount, sumTotalPrice);
            Assert.Equal(sale.Profit, sumProfit);

            var productRepo = new ProductRepository();
            Assert.Equal(8, productRepo.GetById(productA).Quantity);
            Assert.Equal(9, productRepo.GetById(productB).Quantity);
            Assert.Equal(9, productRepo.GetById(productC).Quantity);

            var payments = svc.GetSalePayments((int)result.SaleId);
            Assert.Single(payments);
            Assert.Equal(435m, SafeConvert.ToDecimal(payments[0]["amount"]));
        }

        // ---- Invoice-level discount guard (P2-T01 validation) ----
        [Fact]
        public void InvoiceDiscount_BelowTotalCost_Rejected()
        {
            int productId = SeedProduct(cost: 100, sell: 150, qty: 5, code: "P-BC");
            var auth = new FakeAuthService { Admin = false, BypassPriceLimits = false, MaxDiscount = 50, MaxMarkup = 20 };
            var svc = BuildSaleService(auth);

            var items = new List<SaleItem>
            {
                new SaleItem { ProductId = productId, Quantity = 1, UnitFinalPrice = 150 }
            };
            // discount=100 => total=50 < cost=100 => rejected
            var result = svc.CreateCashSale(items, discountAmount: 100m, paymentMethod: PaymentMethods.Cash);
            Assert.False(result.Success);
        }

        [Fact]
        public void InvoiceDiscount_ExceedsEmployeeCeiling_Rejected()
        {
            int productId = SeedProduct(cost: 100, sell: 150, qty: 5, code: "P-EC");
            var auth = new FakeAuthService { Admin = false, BypassPriceLimits = false, MaxDiscount = 10, MaxMarkup = 20 };
            var svc = BuildSaleService(auth);

            var items = new List<SaleItem>
            {
                new SaleItem { ProductId = productId, Quantity = 1, UnitFinalPrice = 150 }
            };
            // discount=30 => 20% > 10% ceiling => rejected
            var result = svc.CreateCashSale(items, discountAmount: 30m, paymentMethod: PaymentMethods.Cash);
            Assert.False(result.Success);
        }

        [Fact]
        public void InvoiceDiscount_Admin_CannotGoBelowCost()
        {
            int productId = SeedProduct(cost: 100, sell: 150, qty: 5, code: "P-AB");
            var auth = new FakeAuthService { Admin = true, BypassPriceLimits = true, MaxDiscount = 100, MaxMarkup = 100 };
            var svc = BuildSaleService(auth);

            var items = new List<SaleItem>
            {
                new SaleItem { ProductId = productId, Quantity = 1, UnitFinalPrice = 150 }
            };
            // discount=100 => total=50 < cost=100 => rejected even for admin
            var result = svc.CreateCashSale(items, discountAmount: 100m, paymentMethod: PaymentMethods.Cash);
            Assert.False(result.Success);
        }

        [Fact]
        public void InvoiceDiscount_AtCostFloor_Accepted()
        {
            int productId = SeedProduct(cost: 100, sell: 150, qty: 5, code: "P-AF");
            var auth = new FakeAuthService { Admin = false, BypassPriceLimits = false, MaxDiscount = 50, MaxMarkup = 20 };
            var svc = BuildSaleService(auth);

            var items = new List<SaleItem>
            {
                new SaleItem { ProductId = productId, Quantity = 1, UnitFinalPrice = 150 }
            };
            // discount=50 => total=100, exactly at cost floor => accepted
            var result = svc.CreateCashSale(items, discountAmount: 50m, paymentMethod: PaymentMethods.Cash);
            Assert.True(result.Success, result.Message);
            Assert.Equal(100m, svc.GetSaleById((int)result.SaleId).TotalAmount);
        }

        [Fact]
        public void InvoiceDiscount_ValidInBounds_AcceptedAndDistributesCorrectly()
        {
            int productId = SeedProduct(cost: 60, sell: 100, qty: 5, code: "P-VI");
            var auth = new FakeAuthService { Admin = false, BypassPriceLimits = false, MaxDiscount = 10, MaxMarkup = 20 };
            var svc = BuildSaleService(auth);

            var items = new List<SaleItem>
            {
                new SaleItem { ProductId = productId, Quantity = 1, UnitFinalPrice = 100 }
            };
            var result = svc.CreateCashSale(items, discountAmount: 5m, paymentMethod: PaymentMethods.Cash);
            Assert.True(result.Success, result.Message);

            var sale = svc.GetSaleById((int)result.SaleId);
            Assert.Equal(95m, sale.TotalAmount);

            var saleItems = svc.GetSaleItems((int)result.SaleId);
            Assert.Single(saleItems);
            Assert.Equal(95m, saleItems[0].TotalPrice);
            Assert.Equal(35m, saleItems[0].Profit); // 95 - 60
        }

        [Fact]
        public void PerMethodBreakdowns_ReconcileWithRawSql()
        {
            var db = DatabaseManager.Instance;
            string ts = DateTime.Today.AddHours(12).ToString("yyyy-MM-dd HH:mm:ss");

            long saleId = db.ExecuteAndGetId(@"
                INSERT INTO sales (invoice_number, sale_type, user_id, total_amount, paid_amount, remaining_amount, payment_method, sale_date)
                VALUES ('INV-X', 'cash', 1, 100, 100, 0, 'نقدي', @d)", new Dictionary<string, object> { { "@d", ts } });
            db.Execute("INSERT INTO sale_payments (sale_id, payment_method, amount, payment_date) VALUES (@s,'نقدي',100,@d)",
                new Dictionary<string, object> { { "@s", saleId }, { "@d", ts } });
            db.Execute("INSERT INTO expenses (description, amount, category, payment_method, expense_date, user_id) VALUES ('x',30,'c','نقدي',@d,1)",
                new Dictionary<string, object> { { "@d", ts } });

            var summary = new ReportRepository().GetPeriodSummary(_today, _today);

            var inflows  = summary["payment_inflows"]  as Dictionary<string, decimal> ?? new Dictionary<string, decimal>();
            var outflows = summary["payment_outflows"] as Dictionary<string, decimal> ?? new Dictionary<string, decimal>();

            decimal rawCashIn  = Convert.ToDecimal(db.FetchScalar("SELECT COALESCE(SUM(amount),0) FROM sale_payments"))
                              + Convert.ToDecimal(db.FetchScalar("SELECT COALESCE(SUM(amount),0) FROM repair_payments"));
            decimal rawCashOut = Convert.ToDecimal(db.FetchScalar("SELECT COALESCE(SUM(amount),0) FROM expenses WHERE COALESCE(is_deleted,0)=0"))
                               + Convert.ToDecimal(db.FetchScalar("SELECT COALESCE(SUM(amount),0) FROM supplier_transactions WHERE transaction_type='payment'"))
                               + Convert.ToDecimal(db.FetchScalar("SELECT COALESCE(SUM(amount),0) FROM employee_salary_transactions WHERE transaction_type='salary'"))
                               + Convert.ToDecimal(db.FetchScalar("SELECT COALESCE(SUM(cash_refund),0) FROM returns"));

            Assert.Equal(rawCashIn,  inflows.Values.Sum());
            Assert.Equal(rawCashOut, outflows.Values.Sum());
        }
    }
}
