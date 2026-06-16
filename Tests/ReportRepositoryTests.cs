using System;
using System.Collections.Generic;
using AlJohary.ServiceHub.Infrastructure.Data;
using AlJohary.ServiceHub.Infrastructure.Persistence;
using AlJohary.ServiceHub.Shared.Helpers;
using Xunit;

namespace AlJohary.ServiceHub.Tests
{
    [Collection("Database")]
    public class ReportRepositoryTests : IDisposable
    {
        public ReportRepositoryTests()
        {
            DatabaseManager.Instance.InitializeForTests();
        }

        public void Dispose() { }

        [Fact]
        public void GetDailySummary_WithRepairOrder_DoesNotThrowAndReturnsMaintTotal()
        {
            string transactionDate = DateTime.Today.AddHours(10).ToString("yyyy-MM-dd HH:mm:ss");
            long orderId = DatabaseManager.Instance.ExecuteAndGetId(@"
                INSERT INTO repair_orders
                    (order_number, user_id, order_status, total_amount, paid_amount, remaining_amount,
                     intake_date, created_at, updated_at)
                VALUES
                    ('MNT-0001', 1, 'received', 500, 0, 500,
                     datetime('now'), datetime('now'), datetime('now'))");
            DatabaseManager.Instance.Execute(@"
                INSERT INTO repair_payments (order_id, amount, payment_method, payment_date, user_id)
                VALUES (@orderId, 500, 'نقدي', @paymentDate, 1)",
                new Dictionary<string, object> { { "@orderId", orderId }, { "@paymentDate", transactionDate } });

            var repo = new ReportRepository();
            string today = DateTime.Today.ToString("yyyy-MM-dd");

            var result = repo.GetDailySummary(today);

            Assert.NotNull(result);
            Assert.True(result.ContainsKey("maintenance_total"),
                "summary must include maintenance_total key (regression: column was wrongly named order_date)");
            Assert.Equal(500m, SafeConvert.ToDecimal(result["maintenance_total"]));
        }

        [Fact]
        public void GetPeriodSummary_WithRepairOrder_DoesNotThrow()
        {
            string transactionDate = DateTime.Today.AddHours(10).ToString("yyyy-MM-dd HH:mm:ss");
            long orderId = DatabaseManager.Instance.ExecuteAndGetId(@"
                INSERT INTO repair_orders
                    (order_number, user_id, order_status, total_amount, paid_amount, remaining_amount,
                     intake_date, created_at, updated_at)
                VALUES
                    ('MNT-0002', 1, 'received', 200, 0, 200,
                     datetime('now'), datetime('now'), datetime('now'))");
            DatabaseManager.Instance.Execute(@"
                INSERT INTO repair_payments (order_id, amount, payment_method, payment_date, user_id)
                VALUES (@orderId, 200, 'نقدي', @paymentDate, 1)",
                new Dictionary<string, object> { { "@orderId", orderId }, { "@paymentDate", transactionDate } });

            var repo = new ReportRepository();
            string today = DateTime.Today.ToString("yyyy-MM-dd");

            var result = repo.GetPeriodSummary(today, today);

            Assert.NotNull(result);
            Assert.True(result.ContainsKey("maintenance_total"));
            Assert.Equal(200m, SafeConvert.ToDecimal(result["maintenance_total"]));
        }

        [Fact]
        public void GetPeriodSummary_WithEmployeeSalaryTransactions_CalculatesSalaryProfitAndCashFlow()
        {
            var db = DatabaseManager.Instance;
            string transactionDate = DateTime.Today.AddHours(10).ToString("yyyy-MM-dd HH:mm:ss");
            long employeeId = db.ExecuteAndGetId(@"
                INSERT INTO employees (full_name, base_salary, is_active)
                VALUES ('موظف تقرير', 4000, 1)");

            db.Execute(@"
                INSERT INTO employee_salary_transactions
                    (employee_id, transaction_type, amount, payment_method, transaction_date, created_by)
                VALUES
                    (@employeeId, 'salary', 1000, 'نقدي', @transactionDate, 1),
                    (@employeeId, 'deduction', 200, NULL, @transactionDate, 1)",
                new Dictionary<string, object> { { "@employeeId", employeeId }, { "@transactionDate", transactionDate } });

            var repo = new ReportRepository();
            string today = DateTime.Today.ToString("yyyy-MM-dd");

            var result = repo.GetPeriodSummary(today, today);
            var outflows = Assert.IsType<Dictionary<string, decimal>>(result["payment_outflows"]);

            Assert.Equal(1000m, SafeConvert.ToDecimal(result["total_salary_payments"]));
            Assert.Equal(200m, SafeConvert.ToDecimal(result["total_employee_deductions"]));
            Assert.Equal(800m, SafeConvert.ToDecimal(result["net_salary_expense"]));
            Assert.Equal(-800m, SafeConvert.ToDecimal(result["net_profit"]));
            Assert.Equal(-1000m, SafeConvert.ToDecimal(result["net_cash_flow"]));
            Assert.True(outflows.ContainsKey(PaymentMethods.Cash));
            Assert.Equal(1000m, outflows[PaymentMethods.Cash]);
        }
    }
}
