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
            DatabaseManager.Instance.Execute(@"
                INSERT INTO repair_orders
                    (order_number, user_id, order_status, total_amount, paid_amount, remaining_amount,
                     intake_date, created_at, updated_at)
                VALUES
                    ('MNT-0001', 1, 'received', 500, 0, 500,
                     datetime('now'), datetime('now'), datetime('now'))");

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
            DatabaseManager.Instance.Execute(@"
                INSERT INTO repair_orders
                    (order_number, user_id, order_status, total_amount, paid_amount, remaining_amount,
                     intake_date, created_at, updated_at)
                VALUES
                    ('MNT-0002', 1, 'received', 200, 0, 200,
                     datetime('now'), datetime('now'), datetime('now'))");

            var repo = new ReportRepository();
            string today = DateTime.Today.ToString("yyyy-MM-dd");

            var result = repo.GetPeriodSummary(today, today);

            Assert.NotNull(result);
            Assert.True(result.ContainsKey("maintenance_total"));
            Assert.Equal(200m, SafeConvert.ToDecimal(result["maintenance_total"]));
        }
    }
}
