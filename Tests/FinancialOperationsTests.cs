using System;
using System.Collections.Generic;
using System.Linq;
using AlJohary.ServiceHub.Infrastructure.Data;
using AlJohary.ServiceHub.Infrastructure.Persistence;
using AlJohary.ServiceHub.Shared.Helpers;
using Xunit;

namespace AlJohary.ServiceHub.Tests
{
    // Covers the restructured reports area:
    //   - daily/monthly KPI card values (read from the summary that powers the cards-only pages)
    //   - the new GetFinancialOperations audit log (وارد/صادر/خصم/التأثير الصافي)
    //   - reconciliation between the operations log and the KPI summary.
    [Collection("Database")]
    public class FinancialOperationsTests : IDisposable
    {
        private readonly string _today = DateTime.Today.ToString("yyyy-MM-dd");
        private readonly string _ts = DateTime.Today.AddHours(10).ToString("yyyy-MM-dd HH:mm:ss");

        public FinancialOperationsTests()
        {
            DatabaseManager.Instance.InitializeForTests();
        }

        public void Dispose() { }

        // ---- seed helpers ----

        private long SeedSaleWithPayment(string invoice, decimal total, decimal profit, decimal paid, string method)
        {
            var db = DatabaseManager.Instance;
            long saleId = db.ExecuteAndGetId(@"
                INSERT INTO sales (invoice_number, sale_type, user_id, total_amount, paid_amount, remaining_amount, profit, payment_method, sale_date)
                VALUES (@inv, 'cash', 1, @total, @paid, 0, @profit, @method, @d)",
                new Dictionary<string, object>
                {
                    { "@inv", invoice }, { "@total", total }, { "@paid", paid },
                    { "@profit", profit }, { "@method", method }, { "@d", _ts }
                });
            if (paid > 0)
                db.Execute(@"INSERT INTO sale_payments (sale_id, payment_method, amount, payment_date)
                             VALUES (@s, @method, @amount, @d)",
                    new Dictionary<string, object>
                    {
                        { "@s", saleId }, { "@method", method }, { "@amount", paid }, { "@d", _ts }
                    });
            return saleId;
        }

        private long SeedRepairWithPayment(string orderNumber, decimal laborCost, decimal amount, string method = "نقدي")
        {
            var db = DatabaseManager.Instance;
            long orderId = db.ExecuteAndGetId(@"
                INSERT INTO repair_orders (order_number, customer_name, user_id, order_status, total_amount, paid_amount, remaining_amount, intake_date, created_at, updated_at)
                VALUES (@o, 'عميل صيانة', 1, 'received', @t, 0, @t, datetime('now'), datetime('now'), datetime('now'))",
                new Dictionary<string, object> { { "@o", orderNumber }, { "@t", laborCost } });
            if (laborCost > 0)
                db.Execute(@"INSERT INTO repair_devices (order_id, device_type, reported_issue, labor_cost, device_status, created_at)
                             VALUES (@o, 'laptop', 'issue', @labor, 'received', datetime('now'))",
                    new Dictionary<string, object> { { "@o", orderId }, { "@labor", laborCost } });
            db.Execute(@"INSERT INTO repair_payments (order_id, amount, payment_method, payment_date, user_id)
                         VALUES (@o, @amount, @method, @d, 1)",
                new Dictionary<string, object> { { "@o", orderId }, { "@amount", amount }, { "@method", method }, { "@d", _ts } });
            return orderId;
        }

        private void SeedExpense(decimal amount, string method, bool deleted = false)
        {
            DatabaseManager.Instance.Execute(@"
                INSERT INTO expenses (description, amount, category, payment_method, expense_date, user_id, is_deleted)
                VALUES ('مصروف', @amount, 'فئة', @method, @d, 1, @del)",
                new Dictionary<string, object>
                {
                    { "@amount", amount }, { "@method", method }, { "@d", _ts }, { "@del", deleted ? 1 : 0 }
                });
        }

        private int SeedSupplier(string name = "مورد")
        {
            return (int)DatabaseManager.Instance.ExecuteAndGetId(
                "INSERT INTO suppliers (name, total_debt) VALUES (@n, 0)",
                new Dictionary<string, object> { { "@n", name } });
        }

        private void SeedSupplierTransaction(int supplierId, string type, decimal amount, string method)
        {
            DatabaseManager.Instance.Execute(@"
                INSERT INTO supplier_transactions (supplier_id, transaction_type, amount, transaction_date, payment_method, created_by)
                VALUES (@s, @type, @amount, @d, @method, 1)",
                new Dictionary<string, object>
                {
                    { "@s", supplierId }, { "@type", type }, { "@amount", amount }, { "@d", _ts }, { "@method", method }
                });
        }

        private int SeedEmployee(string name = "موظف")
        {
            return (int)DatabaseManager.Instance.ExecuteAndGetId(
                "INSERT INTO employees (full_name, base_salary, is_active) VALUES (@n, 3000, 1)",
                new Dictionary<string, object> { { "@n", name } });
        }

        private void SeedSalaryTransaction(int employeeId, string type, decimal amount, string method)
        {
            DatabaseManager.Instance.Execute(@"
                INSERT INTO employee_salary_transactions (employee_id, transaction_type, amount, payment_method, transaction_date, created_by)
                VALUES (@e, @type, @amount, @method, @d, 1)",
                new Dictionary<string, object>
                {
                    { "@e", employeeId }, { "@type", type }, { "@amount", amount }, { "@method", method }, { "@d", _ts }
                });
        }

        private void SeedCashRefund(string returnNumber, decimal total, decimal cashRefund, string method)
        {
            var db = DatabaseManager.Instance;
            // Backing sale only satisfies the returns.sale_id FK; it carries NO payment so the
            // refund is the only money movement this helper introduces.
            long saleId = SeedSaleWithPayment("INV-FOR-" + returnNumber, total: 0, profit: 0, paid: 0, method: method);
            db.Execute(@"INSERT INTO returns (return_number, sale_id, user_id, total_amount, cash_refund, debt_deduction, payment_method, return_date)
                         VALUES (@r, @s, 1, @total, @refund, 0, @method, @d)",
                new Dictionary<string, object>
                {
                    { "@r", returnNumber }, { "@s", saleId }, { "@total", total },
                    { "@refund", cashRefund }, { "@method", method }, { "@d", _ts }
                });
        }

        private List<Dictionary<string, object>> Ops() =>
            new ReportRepository().GetFinancialOperations(_today, _today);

        private static decimal Sum(IEnumerable<Dictionary<string, object>> rows, string col) =>
            rows.Sum(r => SafeConvert.ToDecimal(r[col]));

        // ---- 1. Daily report card data ----
        [Fact]
        public void DailyReport_CardValues_AreCorrect()
        {
            SeedSaleWithPayment("INV-1", total: 16000, profit: 0, paid: 16000, method: "نقدي");
            SeedRepairWithPayment("MNT-1", laborCost: 0, amount: 350);
            int sup = SeedSupplier();
            SeedSupplierTransaction(sup, "payment", 15230, "نقدي");
            SeedExpense(200, "نقدي");
            int emp = SeedEmployee();
            SeedSalaryTransaction(emp, "salary", 1000, "نقدي");
            SeedSalaryTransaction(emp, "deduction", 200, "خصم");

            var s = new ReportRepository().GetDailySummary(_today);

            Assert.Equal(16000m, SafeConvert.ToDecimal(s["gross_sales"]));
            Assert.Equal(350m, SafeConvert.ToDecimal(s["maintenance_total"]));
            Assert.Equal(15230m, SafeConvert.ToDecimal(s["total_supplier_payments"]));
            Assert.Equal(200m, SafeConvert.ToDecimal(s["total_expenses"]));
            Assert.Equal(800m, SafeConvert.ToDecimal(s["net_salary_expense"]));        // إجمالي الرواتب card
            Assert.Equal(200m, SafeConvert.ToDecimal(s["total_employee_deductions"])); // خصومات الموظفين card
        }

        // ---- 2. Net salary; deduction is not cash inflow ----
        [Fact]
        public void NetSalary_DeductionNotCashInflow()
        {
            int emp = SeedEmployee();
            SeedSalaryTransaction(emp, "salary", 1000, "نقدي");
            SeedSalaryTransaction(emp, "deduction", 200, "خصم");

            var s = new ReportRepository().GetDailySummary(_today);
            Assert.Equal(800m, SafeConvert.ToDecimal(s["net_salary_expense"]));
            Assert.Equal(200m, SafeConvert.ToDecimal(s["total_employee_deductions"]));

            var inflows = (Dictionary<string, decimal>)s["payment_inflows"];
            Assert.False(inflows.ContainsKey("خصم"));
            Assert.Equal(0m, inflows.Values.Sum());

            // In the operations log the deduction lands in خصم/تسوية, never in وارد.
            var deduction = Ops().Single(r => SafeConvert.ToString(r["OperationType"]) == "خصم موظف");
            Assert.Equal(0m, SafeConvert.ToDecimal(deduction["MoneyIn"]));
            Assert.Equal(200m, SafeConvert.ToDecimal(deduction["Deduction"]));
            Assert.Equal(200m, SafeConvert.ToDecimal(deduction["NetEffect"]));
        }

        // ---- 3. Cash net ----
        [Fact]
        public void CashNet_Reconciles()
        {
            SeedSaleWithPayment("INV-C", 1000, 0, 1000, "نقدي");
            SeedExpense(100, "نقدي");
            int sup = SeedSupplier();
            SeedSupplierTransaction(sup, "payment", 200, "نقدي");
            int emp = SeedEmployee();
            SeedSalaryTransaction(emp, "salary", 300, "نقدي");
            SeedCashRefund("RET-C", 50, 50, "نقدي");

            var s = new ReportRepository().GetDailySummary(_today);
            var inflows = (Dictionary<string, decimal>)s["payment_inflows"];
            var outflows = (Dictionary<string, decimal>)s["payment_outflows"];

            decimal cashNet = inflows[PaymentMethods.Cash] - outflows[PaymentMethods.Cash];
            Assert.Equal(350m, cashNet); // 1000 in - (100+200+300+50) out = 350
        }

        // ---- 4. Instapay net ----
        [Fact]
        public void InstapayNet_Reconciles()
        {
            SeedSaleWithPayment("INV-IP", 800, 0, 800, PaymentMethods.InstaPay);
            SeedExpense(100, PaymentMethods.InstaPay);
            int sup = SeedSupplier();
            SeedSupplierTransaction(sup, "payment", 50, PaymentMethods.InstaPay);

            var s = new ReportRepository().GetDailySummary(_today);
            var inflows = (Dictionary<string, decimal>)s["payment_inflows"];
            var outflows = (Dictionary<string, decimal>)s["payment_outflows"];

            Assert.Equal(800m - 150m, inflows[PaymentMethods.InstaPay] - outflows[PaymentMethods.InstaPay]);
        }

        // ---- 5. Wallet net ----
        [Fact]
        public void WalletNet_Reconciles()
        {
            SeedRepairWithPayment("MNT-W", 0, 600, PaymentMethods.EWallet);
            SeedExpense(120, PaymentMethods.EWallet);
            int emp = SeedEmployee();
            SeedSalaryTransaction(emp, "salary", 80, PaymentMethods.EWallet);

            var s = new ReportRepository().GetDailySummary(_today);
            var inflows = (Dictionary<string, decimal>)s["payment_inflows"];
            var outflows = (Dictionary<string, decimal>)s["payment_outflows"];

            Assert.Equal(600m - 200m, inflows[PaymentMethods.EWallet] - outflows[PaymentMethods.EWallet]);
        }

        // ---- 6. Maintenance: collection + profit + appears as وارد ----
        [Fact]
        public void Maintenance_CollectionAndProfit_AndLoggedAsMoneyIn()
        {
            SeedRepairWithPayment("MNT-P", laborCost: 350, amount: 350);

            var s = new ReportRepository().GetDailySummary(_today);
            Assert.Equal(350m, SafeConvert.ToDecimal(s["maintenance_total"]));   // تحصيل الصيانة
            Assert.Equal(350m, SafeConvert.ToDecimal(s["maintenance_profit"]));  // ربح الصيانة (المصنعية)

            var row = Ops().Single(r => SafeConvert.ToString(r["OperationType"]) == "تحصيل صيانة");
            Assert.Equal(350m, SafeConvert.ToDecimal(row["MoneyIn"]));
            Assert.Equal(0m, SafeConvert.ToDecimal(row["MoneyOut"]));
        }

        // ---- 7. Supplier payment shows; purchase does not appear as cash out ----
        [Fact]
        public void Supplier_PaymentLogged_PurchaseNotShownAsCashOut()
        {
            int sup = SeedSupplier();
            SeedSupplierTransaction(sup, "payment", 500, "نقدي");
            SeedSupplierTransaction(sup, "purchase", 9999, "نقدي");

            var s = new ReportRepository().GetDailySummary(_today);
            Assert.Equal(500m, SafeConvert.ToDecimal(s["total_supplier_payments"]));

            var ops = Ops();
            Assert.Single(ops, r => SafeConvert.ToString(r["OperationType"]) == "دفع مورد");
            Assert.DoesNotContain(ops, r => SafeConvert.ToDecimal(r["MoneyOut"]) == 9999m);
        }

        // ---- 8. Expense: non-deleted shows; soft-deleted excluded ----
        [Fact]
        public void Expense_SoftDeleted_ExcludedFromLogAndTotals()
        {
            SeedExpense(200, "نقدي", deleted: false);
            SeedExpense(777, "نقدي", deleted: true);

            var s = new ReportRepository().GetDailySummary(_today);
            Assert.Equal(200m, SafeConvert.ToDecimal(s["total_expenses"]));

            var ops = Ops();
            Assert.Single(ops, r => SafeConvert.ToString(r["OperationType"]) == "مصروف");
            Assert.DoesNotContain(ops, r => SafeConvert.ToDecimal(r["MoneyOut"]) == 777m);
        }

        // ---- 9. Cash refund appears as صادر and reduces method net ----
        [Fact]
        public void CashRefund_AppearsAsMoneyOut()
        {
            SeedCashRefund("RET-9", total: 300, cashRefund: 300, method: "نقدي");

            var refund = Ops().Single(r => SafeConvert.ToString(r["OperationType"]) == "استرداد نقدي");
            Assert.Equal(300m, SafeConvert.ToDecimal(refund["MoneyOut"]));
            Assert.Equal(-300m, SafeConvert.ToDecimal(refund["NetEffect"]));

            var s = new ReportRepository().GetDailySummary(_today);
            var outflows = (Dictionary<string, decimal>)s["payment_outflows"];
            Assert.Equal(300m, outflows[PaymentMethods.Cash]);
        }

        // ---- 10. Operations log: each movement once + reconciles with KPI cards ----
        [Fact]
        public void OperationsLog_ReconcilesWithSummary_AndNoDuplicates()
        {
            SeedSaleWithPayment("INV-X", 1000, 0, 1000, "نقدي");
            SeedRepairWithPayment("MNT-X", 0, 350, PaymentMethods.InstaPay);
            SeedExpense(100, "نقدي");
            int sup = SeedSupplier();
            SeedSupplierTransaction(sup, "payment", 200, "نقدي");
            SeedSupplierTransaction(sup, "purchase", 5000, "نقدي"); // must NOT appear
            int emp = SeedEmployee();
            SeedSalaryTransaction(emp, "salary", 300, "نقدي");
            SeedSalaryTransaction(emp, "deduction", 50, "خصم");
            SeedCashRefund("RET-X", 80, 80, PaymentMethods.EWallet);

            var ops = Ops();
            var s = new ReportRepository().GetDailySummary(_today);
            var inflows = (Dictionary<string, decimal>)s["payment_inflows"];
            var outflows = (Dictionary<string, decimal>)s["payment_outflows"];

            // Per-method MoneyIn / MoneyOut totals reconcile with the KPI summary breakdowns.
            foreach (var method in new[] { PaymentMethods.Cash, PaymentMethods.InstaPay, PaymentMethods.EWallet })
            {
                decimal inFromOps = Sum(ops.Where(r => SafeConvert.ToString(r["PaymentMethod"]) == method), "MoneyIn");
                decimal outFromOps = Sum(ops.Where(r => SafeConvert.ToString(r["PaymentMethod"]) == method), "MoneyOut");
                decimal inFromSummary = inflows.TryGetValue(method, out var iv) ? iv : 0m;
                decimal outFromSummary = outflows.TryGetValue(method, out var ov) ? ov : 0m;
                Assert.Equal(inFromSummary, inFromOps);
                Assert.Equal(outFromSummary, outFromOps);
            }

            // Each money movement appears exactly once; purchase excluded.
            Assert.Single(ops, r => SafeConvert.ToString(r["OperationType"]) == "بيع");
            Assert.Single(ops, r => SafeConvert.ToString(r["OperationType"]) == "تحصيل صيانة");
            Assert.Single(ops, r => SafeConvert.ToString(r["OperationType"]) == "مصروف");
            Assert.Single(ops, r => SafeConvert.ToString(r["OperationType"]) == "دفع مورد");
            Assert.Single(ops, r => SafeConvert.ToString(r["OperationType"]) == "راتب");
            Assert.Single(ops, r => SafeConvert.ToString(r["OperationType"]) == "خصم موظف");
            Assert.Single(ops, r => SafeConvert.ToString(r["OperationType"]) == "استرداد نقدي");
            Assert.DoesNotContain(ops, r => SafeConvert.ToDecimal(r["MoneyOut"]) == 5000m);

            // Deductions are cost reducers, never counted as وارد.
            Assert.Equal(0m, Sum(ops.Where(r => SafeConvert.ToString(r["OperationType"]) == "خصم موظف"), "MoneyIn"));
        }

        // ---- Unknown/blank payment method surfaces clearly, never silently نقدي ----
        [Fact]
        public void BlankPaymentMethod_ShownAsUndetermined()
        {
            // Sale payment with empty payment_method.
            SeedSaleWithPayment("INV-BLANK", 500, 0, 500, "");

            var row = Ops().Single(r => SafeConvert.ToString(r["OperationType"]) == "بيع");
            Assert.Equal("غير محدد", SafeConvert.ToString(row["PaymentMethod"]));
        }

        // ---- Non-canonical (NULL/empty/unknown) methods land in other bucket and reconcile ----
        [Fact]
        public void NonCanonicalMethods_AppearInOtherBucket_AndInflowsReconcile()
        {
            var db = DatabaseManager.Instance;
            string ts = _ts;

            // Canonical methods
            SeedSaleWithPayment("INV-CC", total: 1000, profit: 0, paid: 1000, method: PaymentMethods.Cash);
            SeedSaleWithPayment("INV-CI", total: 500, profit: 0, paid: 500, method: PaymentMethods.InstaPay);

            // Empty method
            long sidEmpty = db.ExecuteAndGetId(@"
                INSERT INTO sales (invoice_number, sale_type, user_id, total_amount, paid_amount, remaining_amount, payment_method, sale_date)
                VALUES ('INV-EMP', 'cash', 1, 200, 200, 0, @pm, @d)",
                new Dictionary<string, object> { { "@pm", "" }, { "@d", ts } });
            db.Execute("INSERT INTO sale_payments (sale_id, payment_method, amount, payment_date) VALUES (@s, @pm, 200, @d)",
                new Dictionary<string, object> { { "@s", sidEmpty }, { "@pm", "" }, { "@d", ts } });

            // NULL method
            long sidNull = db.ExecuteAndGetId(@"
                INSERT INTO sales (invoice_number, sale_type, user_id, total_amount, paid_amount, remaining_amount, payment_method, sale_date)
                VALUES ('INV-NUL', 'cash', 1, 300, 300, 0, NULL, @d)",
                new Dictionary<string, object> { { "@d", ts } });
            db.Execute("INSERT INTO sale_payments (sale_id, payment_method, amount, payment_date) VALUES (@s, NULL, 300, @d)",
                new Dictionary<string, object> { { "@s", sidNull }, { "@d", ts } });

            // Non-canonical method string
            long sidOther = db.ExecuteAndGetId(@"
                INSERT INTO sales (invoice_number, sale_type, user_id, total_amount, paid_amount, remaining_amount, payment_method, sale_date)
                VALUES ('INV-OTH', 'cash', 1, 150, 150, 0, 'unknown_wallet', @d)",
                new Dictionary<string, object> { { "@d", ts } });
            db.Execute("INSERT INTO sale_payments (sale_id, payment_method, amount, payment_date) VALUES (@s, 'unknown_wallet', 150, @d)",
                new Dictionary<string, object> { { "@s", sidOther }, { "@d", ts } });

            var s = new ReportRepository().GetDailySummary(_today);
            var inflows = (Dictionary<string, decimal>)s["payment_inflows"];

            Assert.Equal(1000m, inflows[PaymentMethods.Cash]);
            Assert.Equal(500m, inflows[PaymentMethods.InstaPay]);

            // NULL and empty string coalesce to "غير محدد" (other/unknown bucket)
            string undetermined = "غير محدد";
            Assert.True(inflows.ContainsKey(undetermined));
            Assert.Equal(500m, inflows[undetermined]); // 200 empty + 300 NULL

            // Non-canonical string 'unknown_wallet' appears as its own bucket
            Assert.True(inflows.ContainsKey("unknown_wallet"));
            Assert.Equal(150m, inflows["unknown_wallet"]);

            // All inflows reconcile by summing every bucket
            decimal totalInflows = inflows.Values.Sum();
            Assert.Equal(2150m, totalInflows);
        }
    }
}
