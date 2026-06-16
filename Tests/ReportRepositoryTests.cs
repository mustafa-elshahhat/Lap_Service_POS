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

        private static long CreateRepairOrder(string orderNumber, string status = "received", DateTime? deliveryDate = null)
        {
            return DatabaseManager.Instance.ExecuteAndGetId(@"
                INSERT INTO repair_orders
                    (order_number, user_id, order_status, total_amount, paid_amount, remaining_amount,
                     intake_date, delivery_date, created_at, updated_at)
                VALUES
                    (@orderNumber, 1, @status, 0, 0, 0,
                     datetime('now'), @deliveryDate, datetime('now'), datetime('now'))",
                new Dictionary<string, object>
                {
                    { "@orderNumber", orderNumber },
                    { "@status", status },
                    { "@deliveryDate", deliveryDate?.ToString("yyyy-MM-dd HH:mm:ss") }
                });
        }

        private static void AddDevice(long orderId, decimal laborCost)
        {
            DatabaseManager.Instance.Execute(@"
                INSERT INTO repair_devices
                    (order_id, device_type, reported_issue, labor_cost, device_status, created_at)
                VALUES (@orderId, 'laptop', 'repair', @laborCost, 'received', datetime('now'))",
                new Dictionary<string, object>
                {
                    { "@orderId", orderId },
                    { "@laborCost", laborCost }
                });
        }

        private static void AddPart(long orderId, decimal totalCost, decimal purchaseCost, int quantity = 1)
        {
            long deviceId = DatabaseManager.Instance.ExecuteAndGetId(@"
                INSERT INTO repair_devices
                    (order_id, device_type, reported_issue, labor_cost, device_status, created_at)
                VALUES (@orderId, 'laptop', 'parts device', 0, 'received', datetime('now'))",
                new Dictionary<string, object> { { "@orderId", orderId } });

            DatabaseManager.Instance.Execute(@"
                INSERT INTO repair_parts
                    (device_id, order_id, part_name, quantity, unit_cost, total_cost, purchase_cost, is_from_inventory, created_at)
                VALUES (@deviceId, @orderId, 'part', @quantity, @unitCost, @totalCost, @purchaseCost, 0, datetime('now'))",
                new Dictionary<string, object>
                {
                    { "@deviceId", deviceId },
                    { "@orderId", orderId },
                    { "@quantity", quantity },
                    { "@unitCost", totalCost / quantity },
                    { "@totalCost", totalCost },
                    { "@purchaseCost", purchaseCost }
                });
        }

        private static void AddPayment(long orderId, decimal amount, DateTime paymentDate)
        {
            DatabaseManager.Instance.Execute(@"
                INSERT INTO repair_payments (order_id, amount, payment_method, payment_date, user_id)
                VALUES (@orderId, @amount, 'نقدي', @paymentDate, 1)",
                new Dictionary<string, object>
                {
                    { "@orderId", orderId },
                    { "@amount", amount },
                    { "@paymentDate", paymentDate.ToString("yyyy-MM-dd HH:mm:ss") }
                });
        }

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

        [Fact]
        public void MaintenanceProfit_NotDeliveredLaborFullPayment_RecognizedByPaymentDate()
        {
            DateTime paymentDate = DateTime.Today.AddHours(10);
            long orderId = CreateRepairOrder("MNT-PROFIT-001");
            AddDevice(orderId, 350m);
            AddPayment(orderId, 350m, paymentDate);

            var result = new ReportRepository().GetDailySummary(DateTime.Today.ToString("yyyy-MM-dd"));

            Assert.Equal(350m, SafeConvert.ToDecimal(result["maintenance_total"]));
            Assert.Equal(350m, SafeConvert.ToDecimal(result["maintenance_profit"]));
        }

        [Fact]
        public void MaintenanceProfit_DeliveredLaborFullPayment_RecognizedByPaymentDate()
        {
            DateTime paymentDate = DateTime.Today.AddHours(10);
            long orderId = CreateRepairOrder("MNT-PROFIT-002", "delivered", DateTime.Today.AddHours(12));
            AddDevice(orderId, 350m);
            AddPayment(orderId, 350m, paymentDate);

            var result = new ReportRepository().GetDailySummary(DateTime.Today.ToString("yyyy-MM-dd"));

            Assert.Equal(350m, SafeConvert.ToDecimal(result["maintenance_total"]));
            Assert.Equal(350m, SafeConvert.ToDecimal(result["maintenance_profit"]));
        }

        [Fact]
        public void MaintenanceProfit_LaborPartialPayment_RecognizesProportionalProfit()
        {
            DateTime paymentDate = DateTime.Today.AddHours(10);
            long orderId = CreateRepairOrder("MNT-PROFIT-003");
            AddDevice(orderId, 350m);
            AddPayment(orderId, 100m, paymentDate);

            var result = new ReportRepository().GetDailySummary(DateTime.Today.ToString("yyyy-MM-dd"));

            Assert.Equal(100m, SafeConvert.ToDecimal(result["maintenance_total"]));
            Assert.Equal(100m, SafeConvert.ToDecimal(result["maintenance_profit"]));
        }

        [Fact]
        public void MaintenanceProfit_PartsMarginFullPayment_RecognizesLaborPlusMargin()
        {
            DateTime paymentDate = DateTime.Today.AddHours(10);
            long orderId = CreateRepairOrder("MNT-PROFIT-004");
            AddDevice(orderId, 50m);
            AddPart(orderId, totalCost: 300m, purchaseCost: 200m);
            AddPayment(orderId, 350m, paymentDate);

            var result = new ReportRepository().GetDailySummary(DateTime.Today.ToString("yyyy-MM-dd"));

            Assert.Equal(350m, SafeConvert.ToDecimal(result["maintenance_total"]));
            Assert.Equal(150m, SafeConvert.ToDecimal(result["maintenance_profit"]));
        }

        [Fact]
        public void MaintenanceProfit_PartsMarginPartialPayment_RecognizesProportionalProfit()
        {
            DateTime paymentDate = DateTime.Today.AddHours(10);
            long orderId = CreateRepairOrder("MNT-PROFIT-005");
            AddDevice(orderId, 50m);
            AddPart(orderId, totalCost: 300m, purchaseCost: 200m);
            AddPayment(orderId, 175m, paymentDate);

            var result = new ReportRepository().GetDailySummary(DateTime.Today.ToString("yyyy-MM-dd"));

            Assert.Equal(175m, SafeConvert.ToDecimal(result["maintenance_total"]));
            Assert.Equal(75m, SafeConvert.ToDecimal(result["maintenance_profit"]));
        }

        [Fact]
        public void MaintenanceProfit_PaymentOnlyWithoutLaborOrProfitableParts_IsZero()
        {
            DateTime paymentDate = DateTime.Today.AddHours(10);
            long orderId = CreateRepairOrder("MNT-PROFIT-006");
            AddPayment(orderId, 350m, paymentDate);

            var result = new ReportRepository().GetDailySummary(DateTime.Today.ToString("yyyy-MM-dd"));

            Assert.Equal(350m, SafeConvert.ToDecimal(result["maintenance_total"]));
            Assert.Equal(0m, SafeConvert.ToDecimal(result["maintenance_profit"]));
        }

        [Fact]
        public void MaintenanceProfit_MultiplePayments_RecognizesEachPaymentOnce()
        {
            DateTime day1 = DateTime.Today.AddDays(-1).AddHours(10);
            DateTime day2 = DateTime.Today.AddHours(10);
            string day1Text = day1.ToString("yyyy-MM-dd");
            string day2Text = day2.ToString("yyyy-MM-dd");
            long orderId = CreateRepairOrder("MNT-PROFIT-007");
            AddDevice(orderId, 350m);
            AddPayment(orderId, 100m, day1);
            AddPayment(orderId, 250m, day2);

            var repo = new ReportRepository();
            var day1Result = repo.GetDailySummary(day1Text);
            var day2Result = repo.GetDailySummary(day2Text);
            var fullPeriod = repo.GetPeriodSummary(day1Text, day2Text);

            Assert.Equal(100m, SafeConvert.ToDecimal(day1Result["maintenance_profit"]));
            Assert.Equal(250m, SafeConvert.ToDecimal(day2Result["maintenance_profit"]));
            Assert.Equal(350m, SafeConvert.ToDecimal(fullPeriod["maintenance_profit"]));
        }

        [Fact]
        public void MaintenanceProfit_DeliveryDateOutsidePeriod_DoesNotPreventPaymentProfit()
        {
            DateTime paymentDate = DateTime.Today.AddHours(10);
            long orderId = CreateRepairOrder("MNT-PROFIT-008", "delivered", DateTime.Today.AddDays(-5));
            AddDevice(orderId, 350m);
            AddPayment(orderId, 350m, paymentDate);

            var result = new ReportRepository().GetDailySummary(DateTime.Today.ToString("yyyy-MM-dd"));

            Assert.Equal(350m, SafeConvert.ToDecimal(result["maintenance_total"]));
            Assert.Equal(350m, SafeConvert.ToDecimal(result["maintenance_profit"]));
        }

        [Fact]
        public void MaintenanceProfit_PaymentDateOutsidePeriod_DoesNotAppearEvenIfDeliveredInsidePeriod()
        {
            DateTime paymentDate = DateTime.Today.AddDays(-1).AddHours(10);
            long orderId = CreateRepairOrder("MNT-PROFIT-009", "delivered", DateTime.Today.AddHours(12));
            AddDevice(orderId, 350m);
            AddPayment(orderId, 350m, paymentDate);

            var result = new ReportRepository().GetDailySummary(DateTime.Today.ToString("yyyy-MM-dd"));

            Assert.Equal(0m, SafeConvert.ToDecimal(result["maintenance_total"]));
            Assert.Equal(0m, SafeConvert.ToDecimal(result["maintenance_profit"]));
        }
    }
}
