using System;
using System.Collections.Generic;
using AlJohary.ServiceHub.Infrastructure.Data;
using AlJohary.ServiceHub.Infrastructure.Persistence;
using Xunit;

namespace AlJohary.ServiceHub.Tests
{
    [Collection("Database")]
    public class RepairRepositoryTests : IDisposable
    {
        public RepairRepositoryTests()
        {
            DatabaseManager.Instance.InitializeForTests();
        }

        public void Dispose() { }

        [Fact]
        public void RecalculateOrderTotals_PaymentBeforeDevices_RemainingIsNeverNegative()
        {
            var db = DatabaseManager.Instance;
            long orderId = db.ExecuteAndGetId(@"
                INSERT INTO repair_orders
                    (order_number, user_id, order_status, total_amount, paid_amount, remaining_amount,
                     intake_date, created_at, updated_at)
                VALUES
                    ('MNT-0001', 1, 'received', 0, 0, 0,
                     datetime('now'), datetime('now'), datetime('now'))");

            db.Execute(@"
                INSERT INTO repair_payments (order_id, amount, payment_method, payment_date, created_at)
                VALUES (@id, 100, 'نقدي', datetime('now'), datetime('now'))",
                new Dictionary<string, object> { { "@id", orderId } });

            var repo = new RepairRepository();
            repo.RecalculateOrderTotals(orderId);

            var order = repo.GetOrder(orderId);
            Assert.NotNull(order);
            Assert.True(order.RemainingAmount >= 0,
                $"remaining_amount must not be negative; was {order.RemainingAmount}");
            Assert.Equal(0m, order.RemainingAmount);
        }

        [Fact]
        public void RecalculateOrderTotals_PaymentWithDevice_RemainingIsCorrect()
        {
            var db = DatabaseManager.Instance;
            long orderId = db.ExecuteAndGetId(@"
                INSERT INTO repair_orders
                    (order_number, user_id, order_status, total_amount, paid_amount, remaining_amount,
                     intake_date, created_at, updated_at)
                VALUES
                    ('MNT-0002', 1, 'received', 0, 0, 0,
                     datetime('now'), datetime('now'), datetime('now'))");

            db.Execute(@"
                INSERT INTO repair_devices
                    (order_id, device_type, reported_issue, labor_cost, device_status, created_at)
                VALUES (@id, 'laptop', 'screen broken', 500, 'received', datetime('now'))",
                new Dictionary<string, object> { { "@id", orderId } });

            db.Execute(@"
                INSERT INTO repair_payments (order_id, amount, payment_method, payment_date, created_at)
                VALUES (@id, 200, 'نقدي', datetime('now'), datetime('now'))",
                new Dictionary<string, object> { { "@id", orderId } });

            var repo = new RepairRepository();
            repo.RecalculateOrderTotals(orderId);

            var order = repo.GetOrder(orderId);
            Assert.NotNull(order);
            Assert.Equal(500m, order.TotalAmount);
            Assert.Equal(200m, order.PaidAmount);
            Assert.Equal(300m, order.RemainingAmount);
        }
    }
}
